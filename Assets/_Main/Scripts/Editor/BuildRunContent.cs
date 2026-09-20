using System.Collections.Generic;
using Deeper.Enemies;
using Deeper.Rooms;
using Deeper.Run;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Writes everything a sixteen-floor descent is made of that is not a room prefab: the three
    /// biome pools, the run plan that sequences them, and the five placeholder bosses.
    ///
    /// **Committed as a tool for the reason <see cref="UpgradeCatalog"/> gives.** These are eleven
    /// ScriptableObjects wired to each other by GUID; hand-edited as YAML they are something nobody
    /// can reproduce, review or diff, and re-authoring them after a room prefab is rebuilt is
    /// forty Inspector drags. Here the whole run is a table you can read.
    ///
    /// It is **idempotent and non-destructive of identity**: an asset that already exists is written
    /// through in place, so every GUID survives and nothing referencing these breaks.
    /// </summary>
    public static class BuildRunContent
    {
        private const string EnemyFolder = "Assets/_Main/Data/Enemies";
        private const string EncounterFolder = "Assets/_Main/Data/Encounters";
        private const string PoolFolder = "Assets/_Main/Data/Rooms";
        private const string RunFolder = "Assets/_Main/Data/Run";
        private const string BossPrefabFolder = "Assets/_Main/Prefabs/Enemies";
        private const string RoomPrefabFolder = "Assets/_Main/Prefabs/Rooms";

        /// <summary>
        /// Every placeholder boss is a variant of this. CONTENT_DESIGN §5 describes all three
        /// Mini-Bosses and the Depth Warden as slow, high-HP, telegraphed slammers, which is exactly
        /// what the Tunnel Brute already is — and the project has done this once before: the Deep
        /// Warden Elite is this prefab with a different <see cref="EnemyDefinition"/> and a tint
        /// (ART_DIRECTION §4). A boss is the same trick at a bigger scale.
        /// </summary>
        private const string BrutePrefab = "Assets/_Main/Prefabs/Enemies/TunnelBrute.prefab";

        /// <summary>
        /// One placeholder boss. **HP is the only number here that is design** — BALANCE §6 gives a
        /// row for every one of them. Everything else is the Tunnel Brute's, bumped, and is recorded
        /// in the change brief as invented.
        /// </summary>
        private struct BossSpec
        {
            public string Id;
            public string Name;
            public string Description;
            public float Health;
            public Color Tint;

            public BossSpec(string id, string name, float health, Color tint, string description)
            {
                Id = id;
                Name = name;
                Health = health;
                Tint = tint;
                Description = description;
            }
        }

        /// <summary>
        /// BALANCE §6's table, verbatim in the HP column. Zyno's row says "reuses an existing
        /// Mini-Boss's HP/phases for MVP (specific choice TBD)" — the Molten Sentinel's 600 is that
        /// choice, made here and recorded in the change brief because a TBD cannot be built.
        ///
        /// **None of them has phases or a weapon-check moment**, which §6 and CONTENT_DESIGN §5 both
        /// specify for every one. There is no boss phase system; these are single-wave encounters
        /// against a large enemy. That is the gap this pass knowingly leaves.
        /// </summary>
        private static readonly BossSpec[] Bosses =
        {
            new BossSpec("CollapsedKing", "The Collapsed King", 350f,
                         new Color(0.78f, 0.80f, 0.86f, 1f),
                         "Biome 1 Mini-Boss. PLACEHOLDER: a scaled Tunnel Brute. Its ground-slam " +
                         "AoE phase and the Greatsword rubble-shield weapon-check (CONTENT_DESIGN " +
                         "§5) are not built."),

            new BossSpec("DrownedCustodian", "The Drowned Custodian", 450f,
                         new Color(0.45f, 0.72f, 0.80f, 1f),
                         "Biome 2 Mini-Boss. PLACEHOLDER: a scaled Tunnel Brute. Its homing " +
                         "projectiles and the rising water its design was built on (cut, " +
                         "CORE_SYSTEMS §7) are not built."),

            new BossSpec("MoltenSentinel", "The Molten Sentinel", 600f,
                         new Color(0.72f, 0.42f, 0.34f, 1f),
                         "Biome 3 Mini-Boss. PLACEHOLDER: a scaled Tunnel Brute. Its geyser " +
                         "eruptions are not built. Tint kept off the orange-red ART_DIRECTION §2 " +
                         "reserves for danger telegraphs."),

            new BossSpec("DepthWarden", "The Depth Warden", 1200f,
                         new Color(0.52f, 0.44f, 0.68f, 1f),
                         "Floor 16, first fight — her father (GDD §Narrative). PLACEHOLDER: a " +
                         "scaled Tunnel Brute. Its three phases and Phase 3 weapon-check are not " +
                         "built."),

            new BossSpec("Zyno", "Zyno", 600f,
                         new Color(0.34f, 0.30f, 0.42f, 1f),
                         "Floor 16, second fight — the true Final Boss. PLACEHOLDER: a scaled " +
                         "Tunnel Brute. BALANCE §6 leaves its HP as a Mini-Boss's, choice TBD; the " +
                         "Molten Sentinel's 600 is that choice."),
        };

        /// <summary>
        /// **Invented, every one.** No design doc gives a boss a move speed, a cooldown or an
        /// engagement distance — the same hole `EnemyDefinition`'s own header records for the basic
        /// roster. These are the Tunnel Brute's numbers with the reach and the aggro opened up for a
        /// body half again as large in a room twice as big.
        ///
        /// The aggro radius is the one that is not taste: at 24 it covers a Mini-Boss arena corner
        /// to corner (34x17 world units), so the boss always engages from its single marker. At the
        /// Brute's 12 it would stand still until she walked most of the way to it.
        /// </summary>
        private const float BossMoveSpeed = 2.1f;
        private const float BossCooldown = 2.2f;
        private const float BossAggroRadius = 24f;
        private const float BossAttackRange = 2.6f;
        private const float BossStopDistance = 1.6f;

        /// <summary>Half again as large, so a boss reads as one before it has done anything.</summary>
        private const float BossScale = 1.6f;

        [MenuItem("Deeper/Build Run Content")]
        public static void Build()
        {
            var bossPrefabs = new Dictionary<string, GameObject>();
            var bossEncounters = new Dictionary<string, EncounterDefinition>();

            foreach (BossSpec spec in Bosses)
            {
                EnemyDefinition definition = WriteBossDefinition(spec);
                GameObject prefab = WriteBossPrefab(spec, definition);
                if (prefab == null) continue;

                bossPrefabs[spec.Id] = prefab;
                bossEncounters[spec.Id] = WriteBossEncounter(spec, prefab);
            }

            // The bag's contents: LEVEL_DESIGN §2's 6 Combat Rooms plus the biome's one flagged Wave
            // Room. No encounters listed against them, which means "use the fight authored on the
            // prefab" — six layouts each with their own composition is already six different fights,
            // and a second layer of encounter assets would only re-roll them.
            RoomOption[] upperCavesRooms =
            {
                Room("CombatRoom_UpperCaves_01"),
                Room("CombatRoom_UpperCaves_03"),
                Room("CombatRoom_UpperCaves_04"),
                Room("CombatRoom_UpperCaves_05"),
                Room("CombatRoom_UpperCaves_06"),
                Room("CombatRoom_UpperCaves_07"),
                Room("WaveRoom_UpperCaves_02"),
            };

            GameObject miniArena = LoadRoom("MiniBossArena_01");
            GameObject finalArena = LoadRoom("FinalBossArena_01");
            GameObject trueFinalArena = LoadRoom("FinalBossArena_02");
            GameObject vault = LoadRoom("SecretVault_UpperCaves_01");

            BiomeRoomPool caves = WritePool(
                "Pool_UpperCaves", upperCavesRooms,
                new[] { "Theme_UpperCaves_Gravel", "Theme_UpperCaves_OreVein", "Theme_UpperCaves_Collapsed" },
                Special(RoomRole.MiniBoss, miniArena, bossEncounters, "CollapsedKing"),
                Special(RoomRole.SecretVault, vault, null, null));

            // Biomes 2 and 3 run the Upper Caves' layouts and roster under their own theme. That is
            // the owner's placeholder (2026-09-08) and it is expressed HERE, in data, rather than as
            // a fallback in the loader — when Flooded Tunnels gets its own rooms and enemies, this
            // array changes and no code does.
            BiomeRoomPool flooded = WritePool(
                "Pool_FloodedTunnels", upperCavesRooms,
                new[] { "Theme_FloodedTunnels_Wet" },
                Special(RoomRole.MiniBoss, miniArena, bossEncounters, "DrownedCustodian"),
                Special(RoomRole.SecretVault, vault, null, null));

            BiomeRoomPool molten = WritePool(
                "Pool_MoltenDepths", upperCavesRooms,
                new[] { "Theme_MoltenDepths_Basalt" },
                Special(RoomRole.MiniBoss, miniArena, bossEncounters, "MoltenSentinel"),
                Special(RoomRole.SecretVault, vault, null, null),
                Special(RoomRole.FinalBoss, finalArena, bossEncounters, "DepthWarden"),
                Special(RoomRole.TrueFinalBoss, trueFinalArena, bossEncounters, "Zyno"));

            WriteRunPlan(caves, flooded, molten);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Build Run Content: 3 biome pools, 1 run plan, " + Bosses.Length +
                      " placeholder boss(es).");
        }

        // ------------------------------------------------------------------ bosses

        private static EnemyDefinition WriteBossDefinition(BossSpec spec)
        {
            string path = EnemyFolder + "/Enemy_" + spec.Id + ".asset";
            EnemyDefinition asset = Ensure<EnemyDefinition>(path);

            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = spec.Id;
            so.FindProperty("displayName").stringValue = spec.Name;
            so.FindProperty("description").stringValue = spec.Description;
            so.FindProperty("maxHealth").floatValue = spec.Health;
            so.FindProperty("moveSpeed").floatValue = BossMoveSpeed;
            so.FindProperty("cooldown").floatValue = BossCooldown;
            so.FindProperty("aggroRadius").floatValue = BossAggroRadius;
            so.FindProperty("attackRange").floatValue = BossAttackRange;
            so.FindProperty("stopDistance").floatValue = BossStopDistance;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// A **prefab variant** of the Tunnel Brute, not a copy. `SaveAsPrefabAsset` on a prefab
        /// instance produces one, which is the same relationship `DeepWarden.prefab` already has —
        /// so a fix to the Brute's rig reaches every boss, and the variant holds only what differs:
        /// its definition, its tint and its scale.
        /// </summary>
        private static GameObject WriteBossPrefab(BossSpec spec, EnemyDefinition definition)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(BrutePrefab);
            if (source == null)
            {
                Debug.LogError("No Tunnel Brute at " + BrutePrefab + ", so no boss can be built.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);

            try
            {
                instance.name = spec.Id;

                Enemy enemy = instance.GetComponent<Enemy>();
                if (enemy != null)
                {
                    var so = new SerializedObject(enemy);
                    so.FindProperty("definition").objectReferenceValue = definition;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // The root, so the collider grows with the art. A boss whose hitbox stayed the
                // Brute's would be a large sprite you can walk through the edges of.
                instance.transform.localScale = Vector3.one * BossScale;

                foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.color = spec.Tint;
                }

                return PrefabUtility.SaveAsPrefabAsset(instance, BossPrefabFolder + "/" + spec.Id + ".prefab");
            }
            finally
            {
                // The scene copy is scaffolding, and leaving it behind is the "stray (Clone) root"
                // trap in a different costume — it would be saved into whatever scene is open by the
                // next Ctrl+S. Same reason BuildRoomPrefab destroys its own.
                Object.DestroyImmediate(instance);
            }
        }

        private static EncounterDefinition WriteBossEncounter(BossSpec spec, GameObject prefab)
        {
            string path = EncounterFolder + "/Encounter_Boss_" + spec.Id + ".asset";
            EncounterDefinition asset = Ensure<EncounterDefinition>(path);

            var so = new SerializedObject(asset);
            SerializedProperty waves = so.FindProperty("waves");

            // One wave of one enemy. A boss fight IS a `CombatRoom` whose encounter happens to hold
            // a single large thing — no boss room class, and the room's own lock-until-cleared is
            // already the whole lifecycle.
            waves.arraySize = 1;
            SerializedProperty groups = waves.GetArrayElementAtIndex(0).FindPropertyRelative("groups");
            groups.arraySize = 1;

            SerializedProperty group = groups.GetArrayElementAtIndex(0);
            group.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            group.FindPropertyRelative("count").intValue = 1;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ------------------------------------------------------------------ pools

        private struct SpecialSpec
        {
            public RoomRole Role;
            public GameObject Prefab;
            public EncounterDefinition Encounter;
        }

        private static SpecialSpec Special(RoomRole role, GameObject prefab,
                                           Dictionary<string, EncounterDefinition> encounters, string bossId)
        {
            EncounterDefinition encounter = null;
            if (encounters != null && bossId != null) encounters.TryGetValue(bossId, out encounter);

            return new SpecialSpec { Role = role, Prefab = prefab, Encounter = encounter };
        }

        private static BiomeRoomPool WritePool(string name, RoomOption[] rooms, string[] themeNames,
                                               params SpecialSpec[] specials)
        {
            string path = PoolFolder + "/" + name + ".asset";
            BiomeRoomPool asset = Ensure<BiomeRoomPool>(path);

            var so = new SerializedObject(asset);

            SerializedProperty options = so.FindProperty("options");
            options.arraySize = rooms.Length;
            for (int i = 0; i < rooms.Length; i++)
            {
                SerializedProperty entry = options.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefab").objectReferenceValue = rooms[i].prefab;
                entry.FindPropertyRelative("encounters").arraySize = 0;
            }

            SerializedProperty themes = so.FindProperty("themes");
            themes.arraySize = themeNames.Length;
            for (int i = 0; i < themeNames.Length; i++)
            {
                themes.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<RoomTheme>(
                        "Assets/_Main/Data/Themes/" + themeNames[i] + ".asset");
            }

            SerializedProperty rooms3 = so.FindProperty("specialRooms");
            rooms3.arraySize = specials.Length;
            for (int i = 0; i < specials.Length; i++)
            {
                SerializedProperty entry = rooms3.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("role").enumValueIndex = (int)specials[i].Role;

                SerializedProperty room = entry.FindPropertyRelative("room");
                room.FindPropertyRelative("prefab").objectReferenceValue = specials[i].Prefab;

                SerializedProperty encounters = room.FindPropertyRelative("encounters");
                encounters.arraySize = specials[i].Encounter != null ? 1 : 0;
                if (specials[i].Encounter != null)
                {
                    encounters.GetArrayElementAtIndex(0).objectReferenceValue = specials[i].Encounter;
                }
            }

            so.FindProperty("minRoomsPerFloor").intValue = 3;
            so.FindProperty("maxRoomsPerFloor").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// GDD §Game Loop 3's descent: "Biome 1 (floors 1–5) → Mini-Boss 1 → Biome 2 (6–10) →
        /// Mini-Boss 2 → Biome 3 (11–15) → Mini-Boss 3 → Floor 16: Final Boss".
        ///
        /// Molten Depths appears twice on purpose — once for its own floors 11–15, once to carry
        /// Floor 16, which belongs to no biome and needs a pool to draw its two arenas from.
        /// </summary>
        private static void WriteRunPlan(BiomeRoomPool caves, BiomeRoomPool flooded, BiomeRoomPool molten)
        {
            RunPlan plan = Ensure<RunPlan>(RunFolder + "/RunPlan.asset");

            var so = new SerializedObject(plan);
            SerializedProperty stages = so.FindProperty("stages");
            stages.arraySize = 4;

            SetStage(stages.GetArrayElementAtIndex(0), caves, 5);
            SetStage(stages.GetArrayElementAtIndex(1), flooded, 10);
            SetStage(stages.GetArrayElementAtIndex(2), molten, 15);
            SetStage(stages.GetArrayElementAtIndex(3), molten, 16);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(plan);
        }

        private static void SetStage(SerializedProperty stage, BiomeRoomPool pool, int throughFloor)
        {
            stage.FindPropertyRelative("pool").objectReferenceValue = pool;
            stage.FindPropertyRelative("throughFloor").intValue = throughFloor;
        }

        // ------------------------------------------------------------------ plumbing

        private static RoomOption Room(string prefabName)
        {
            return new RoomOption { prefab = LoadRoom(prefabName) };
        }

        private static GameObject LoadRoom(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RoomPrefabFolder + "/" + prefabName + ".prefab");

            if (prefab == null)
            {
                Debug.LogError("No room prefab named " + prefabName + ". Run " +
                               "Deeper/Build All Room Prefabs first.");
            }

            return prefab;
        }

        /// <summary>
        /// Loads the asset at <paramref name="path"/> or creates it. Written through in place when
        /// it exists, so its GUID survives and every pool, plan and prefab already pointing at it
        /// keeps pointing at it — the whole reason this tool can be re-run.
        /// </summary>
        private static T Ensure<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
