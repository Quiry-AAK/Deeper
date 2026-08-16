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

            var byColumn = new Dictionary<int, List<int>>();
            foreach (Vector3 cell in RoomLayout.Markers(map, 'D'))
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

                var go = new GameObject(x == 0 ? "DoorWest" : "DoorEast");
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
