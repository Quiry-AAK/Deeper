using System.Collections.Generic;
using Deeper.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Assembles a whole room prefab from its ASCII map — hierarchy, tilemaps, doors, entry volume,
    /// spawn markers and the wired components — and saves it.
    ///
    /// Exists for the reason <see cref="BuildRunHUD"/> does: a prefab assembled by dragging is one
    /// nobody can reproduce, review or diff. The first Combat Room was built by hand, and every
    /// number in it (which sorting order the floor draws on, how big the entry volume is, which
    /// layer it sits on) then lived only inside a 6,500-line YAML file. Here the same facts are a
    /// dozen readable lines, and the four remaining Upper Caves layouts cost a map and a menu item.
    ///
    /// The map is the authored part and lives in `Layout_UpperCaves_*.cs`; this only derives from
    /// it. Everything positional — door centres, the entry footprint, marker placement — is read
    /// out of the map rather than typed twice, which is what stops a room and its map disagreeing.
    /// </summary>
    public static class BuildRoomPrefab
    {
        /// <summary>One enemy type and how many of it, as an asset path so the spec stays readable.</summary>
        public struct GroupSpec
        {
            public string PrefabPath;
            public int Count;

            public GroupSpec(string prefabPath, int count)
            {
                PrefabPath = prefabPath;
                Count = count;
            }
        }

        /// <summary>One batch. One of these is a standard Combat Room; 2-3 make it a Wave Room.</summary>
        public struct WaveSpec
        {
            public GroupSpec[] Groups;

            public WaveSpec(params GroupSpec[] groups)
            {
                Groups = groups;
            }
        }

        private const string RoomFolder = "Assets/_Main/Prefabs/Rooms";
        private const string DoorSpritePath = "Assets/_Main/Art/Placeholder/Rooms/Door.png";
        private const string VaultDoorSpritePath = "Assets/_Main/Art/Placeholder/Rooms/VaultDoor.png";
        private const string PedestalSpritePath = "Assets/_Main/Art/Placeholder/Rooms/Pedestal.png";
        private const string TelegraphSpritePath = "Assets/_Main/Art/Placeholder/VFX/SpawnBurst.png";

        private const string Crawler = "Assets/_Main/Prefabs/Enemies/CaveCrawler.prefab";
        private const string Slinger = "Assets/_Main/Prefabs/Enemies/RockSlinger.prefab";
        private const string Brute = "Assets/_Main/Prefabs/Enemies/TunnelBrute.prefab";

        // Floor under walls, both under everything else: Actors is the layer characters draw on and
        // a room that shared it would sort against them per-tile. Matched to room 01 exactly.
        private const int FloorSortingOrder = -20;
        private const int WallSortingOrder = -10;
        private const int DoorSortingOrder = -5;

        /// <summary>
        /// The Wave Room's encounter. 12 enemies over 3 batches, 260 HP against room 01's 150 —
        /// 1.73x, which puts it in BALANCE §8's 60-100s band given room 01 targets 30-60s.
        ///
        /// Peak concurrency is 6 (one straggler plus a five-enemy batch), the same density room 01
        /// already proved, so this does not widen ART_DIRECTION §105's open question about Wave Room
        /// screen clarity. No Deep Warden: it is the Elite tied to the unbuilt secret-key drop.
        /// </summary>
        [MenuItem("Deeper/Build Wave Room Prefab")]
        private static void BuildWaveRoom()
        {
            Build(Layout_UpperCaves_02.Map, "WaveRoom_UpperCaves_02", new[]
            {
                new WaveSpec(new GroupSpec(Crawler, 4)),
                new WaveSpec(new GroupSpec(Crawler, 3), new GroupSpec(Slinger, 2)),
                new WaveSpec(new GroupSpec(Brute, 1), new GroupSpec(Slinger, 2)),
            });
        }

        /// <summary>
        /// The Secret Vault's guard — the cost the owner chose for it (2026-08-16), replacing the
        /// risk half CORE_SYSTEMS §8 lost when the Rising Hazard was cut.
        ///
        /// One wave, not two: §8 caps flagged Wave Rooms at 1-2 per biome's pool and the Upper
        /// Caves' one is already `WaveRoom_UpperCaves_02`. 6 enemies for **190 HP** — 1.27x room
        /// 01's 150 and well under the Wave Room's 260 — spent on two Brutes rather than more
        /// bodies, so the vault is a harder fight at the same peak concurrency of 6 that room 01
        /// already proved readable. BALANCE has no Secret Vault row at all; see the change brief.
        /// </summary>
        [MenuItem("Deeper/Build Secret Vault Prefab")]
        private static void BuildSecretVault()
        {
            Build(Layout_SecretVault_01.Map, "SecretVault_UpperCaves_01", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 2), new GroupSpec(Slinger, 2), new GroupSpec(Crawler, 2)),
            });
        }

        private static void Build(string[] map, string roomName, WaveSpec[] encounter)
        {
            if (!RoomLayout.Validate(map, roomName)) return;

            GameObject root = new GameObject(roomName);

            try
            {
                Grid grid = root.AddComponent<Grid>();
                grid.cellSize = Vector3.one;

                CombatRoom room = root.AddComponent<CombatRoom>();

                Transform tiles = Group(root.transform, "Tiles");
                Tilemap floor = BuildTilemap(tiles, "Floor", FloorSortingOrder, false);
                Tilemap walls = BuildTilemap(tiles, "Walls", WallSortingOrder, true);
                RoomLayout.Paint(floor, walls, map);

                RoomDoor[] doors = BuildDoors(root.transform, map);

                // Vault doors and the pedestal are built from the same pass for every room: a map
                // with no `V` or `T` cells produces neither, so there is one builder rather than a
                // room-type branch that could drift. Note the vault door is deliberately NOT in the
                // array wired to CombatRoom.doors below — Arm() opens everything in that list, which
                // would unlock the vault every time the room re-armed.
                BuildVaultDoors(root.transform, map, room);
                BuildPedestal(root.transform, map, room);

                BuildEntry(root.transform, map, room);
                WaveSpawner spawner = BuildEncounter(root.transform, map, encounter);
                BuildPlayerStart(root.transform, map);

                SerializedObject so = new SerializedObject(room);
                so.FindProperty("encounter").objectReferenceValue = spawner;
                SetArray(so.FindProperty("doors"), doors);
                so.ApplyModifiedPropertiesWithoutUndo();

                string path = RoomFolder + "/" + roomName + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();

                Debug.Log("Built " + path + ": " + doors.Length + " doors, " + encounter.Length +
                          " wave(s), " + spawner.transform.Find("SpawnPoints").childCount + " markers.",
                          AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
            finally
            {
                // The scene copy is scaffolding; leaving it behind is the "stray (Clone) root" trap
                // in a different costume, and it would be saved into TestScene by the next Ctrl+S.
                Object.DestroyImmediate(root);
            }
        }

        private static Tilemap BuildTilemap(Transform parent, string name, int sortingOrder, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Tilemap map = go.AddComponent<Tilemap>();
            TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;

            if (collide) go.AddComponent<TilemapCollider2D>();

            return map;
        }

        /// <summary>
        /// One door per column carrying `D`, sized to the run of cells it fills. West and east
        /// because LEVEL_DESIGN §1's floors are a linear left-to-right sequence.
        /// </summary>
        private static RoomDoor[] BuildDoors(Transform root, string[] map)
        {
            Transform group = Group(root, "Doors");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DoorSpritePath);

            return BuildDoorColumns(group, map, 'D', sprite, true);
        }

        /// <summary>
        /// The key-gated door in a Secret Vault's interior wall (CORE_SYSTEMS §8). Built exactly
        /// like a floor door, because it *is* a <see cref="RoomDoor"/> — plus a child volume
        /// carrying the lock that decides when it opens.
        ///
        /// Returns nothing on purpose: these must never reach <c>CombatRoom.doors</c>.
        /// </summary>
        private static void BuildVaultDoors(Transform root, string[] map, CombatRoom room)
        {
            if (RoomLayout.Markers(map, 'V').Count == 0) return;

            Transform group = Group(root, "VaultDoors");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VaultDoorSpritePath);

            RoomDoor[] doors = BuildDoorColumns(group, map, 'V', sprite, false);
            for (int i = 0; i < doors.Length; i++) BuildLock(doors[i], room);
        }

        /// <summary>
        /// The volume that reads the player's key. Its own child object so it can sit on layer 8
        /// `RoomTrigger` while the door keeps its solid barrier on Default — a trigger on the door
        /// object itself would be on Default, where it would eat every rock thrown through the
        /// doorway.
        /// </summary>
        private static void BuildLock(RoomDoor door, CombatRoom room)
        {
            var go = new GameObject("Lock");
            go.transform.SetParent(door.transform, false);
            go.layer = 8;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();

            // Wider and taller than the 1x2 doorway deliberately: the door's barrier stops her
            // *before* the gap, so a volume the size of the gap is one she can never reach.
            box.size = new Vector2(2f, 2.4f);
            box.isTrigger = true;

            VaultDoor vault = go.AddComponent<VaultDoor>();
            SerializedObject so = new SerializedObject(vault);
            so.FindProperty("door").objectReferenceValue = door;
            so.FindProperty("room").objectReferenceValue = room;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The Secret Vault's payout, as an object standing in the room rather than an invisible
        /// rule — without something on screen, "the fight ended and a Legendary appeared" reads as
        /// the room paying out for no reason.
        /// </summary>
        private static void BuildPedestal(Transform root, string[] map, CombatRoom room)
        {
            Vector3 position;
            if (!RoomLayout.TryGetMarker(map, 'T', out position)) return;

            var go = new GameObject("Pedestal");
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PedestalSpritePath);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = DoorSortingOrder;

            VaultReward reward = go.AddComponent<VaultReward>();
            SerializedObject so = new SerializedObject(reward);
            so.FindProperty("room").objectReferenceValue = room;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RoomDoor[] BuildDoorColumns(Transform group, string[] map, char symbol,
                                                   Sprite sprite, bool floorDoor)
        {
            var byColumn = new Dictionary<int, List<int>>();
            foreach (Vector3 cell in RoomLayout.Markers(map, symbol))
            {
                int x = Mathf.FloorToInt(cell.x);
                if (!byColumn.ContainsKey(x)) byColumn[x] = new List<int>();
                byColumn[x].Add(Mathf.FloorToInt(cell.y));
            }

            var doors = new List<RoomDoor>();
            var columns = new List<int>(byColumn.Keys);
            columns.Sort();

            foreach (int x in columns)
            {
                List<int> rows = byColumn[x];
                rows.Sort();

                int height = rows[rows.Count - 1] - rows[0] + 1;
                if (height != 2)
                {
                    // Door.png is authored 32x64 = 1x2 world units and drawn Simple, so it cannot
                    // stretch to fit. A taller gap needs new art, not a taller collider.
                    Debug.LogError("Door at column " + x + " is " + height + " tall; the door art is 2.");
                }

                var go = new GameObject(floorDoor ? (x == 0 ? "DoorWest" : "DoorEast") : "VaultDoor");
                go.transform.SetParent(group, false);
                go.transform.localPosition = new Vector3(x + 0.5f, rows[0] + height * 0.5f, 0f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = DoorSortingOrder;

                BoxCollider2D barrier = go.AddComponent<BoxCollider2D>();
                barrier.size = new Vector2(1f, height);

                RoomDoor door = go.AddComponent<RoomDoor>();
                SerializedObject so = new SerializedObject(door);
                so.FindProperty("sprite").objectReferenceValue = sr;
                so.FindProperty("barrier").objectReferenceValue = barrier;
                so.ApplyModifiedPropertiesWithoutUndo();

                doors.Add(door);
            }

            return doors.ToArray();
        }

        /// <summary>
        /// The trigger band, sized to the `=` footprint. **Layer 8 `RoomTrigger`, never Default** —
        /// `ThrownRock.blockingLayers` is Default, so a Default-layer volume across the room eats
        /// every rock a Slinger throws through it.
        /// </summary>
        private static void BuildEntry(Transform root, string[] map, CombatRoom room)
        {
            List<Vector3> cells = RoomLayout.Markers(map, '=');
            if (cells.Count == 0)
            {
                Debug.LogError("Map has no '=' entry band; the room could never be sprung.");
                return;
            }

            Bounds bounds = new Bounds(cells[0], Vector3.zero);
            for (int i = 1; i < cells.Count; i++) bounds.Encapsulate(cells[i]);

            var go = new GameObject("Entry");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0f);
            go.layer = 8;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(bounds.size.x + 1f, bounds.size.y + 1f);
            box.isTrigger = true;

            RoomEntry entry = go.AddComponent<RoomEntry>();
            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("room").objectReferenceValue = room;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WaveSpawner BuildEncounter(Transform root, string[] map, WaveSpec[] encounter)
        {
            Transform group = Group(root, "Encounter");

            SpawnTelegraph telegraph = group.gameObject.AddComponent<SpawnTelegraph>();
            SerializedObject tso = new SerializedObject(telegraph);
            SerializedProperty frames = tso.FindProperty("frames");
            frames.arraySize = 1;
            frames.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(TelegraphSpritePath);
            tso.ApplyModifiedPropertiesWithoutUndo();

            WaveSpawner spawner = group.gameObject.AddComponent<WaveSpawner>();

            Transform points = Group(group, "SpawnPoints");
            var markers = new List<Transform>();

            // Named by index only, never by the enemy that starts there: since WaveSpawner began
            // choosing its marker against the player's position, no marker belongs to a type.
            for (char digit = '0'; digit <= '9'; digit++)
            {
                List<Vector3> found = RoomLayout.Markers(map, digit);
                if (found.Count == 0) continue;

                var go = new GameObject("Spawn_" + digit);
                go.transform.SetParent(points, false);
                go.transform.localPosition = found[0];
                markers.Add(go.transform);
            }

            SerializedObject so = new SerializedObject(spawner);
            SetArray(so.FindProperty("spawnPoints"), markers.ToArray());
            so.FindProperty("telegraph").objectReferenceValue = telegraph;

            SerializedProperty waves = so.FindProperty("waves");
            waves.arraySize = encounter.Length;

            for (int w = 0; w < encounter.Length; w++)
            {
                SerializedProperty groups = waves.GetArrayElementAtIndex(w).FindPropertyRelative("groups");
                groups.arraySize = encounter[w].Groups.Length;

                for (int g = 0; g < encounter[w].Groups.Length; g++)
                {
                    GroupSpec spec = encounter[w].Groups[g];
                    SerializedProperty element = groups.GetArrayElementAtIndex(g);

                    element.FindPropertyRelative("prefab").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
                    element.FindPropertyRelative("count").intValue = spec.Count;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }

        private static void BuildPlayerStart(Transform root, string[] map)
        {
            Vector3 position;
            if (!RoomLayout.TryGetMarker(map, RoomLayout.PlayerStart, out position))
            {
                Debug.LogError("Map has no 'P' player start.");
                return;
            }

            var go = new GameObject("PlayerStart");
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
