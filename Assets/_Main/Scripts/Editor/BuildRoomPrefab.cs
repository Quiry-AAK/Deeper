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
        private const int DecorSortingOrder = -15;
        private const int WallSortingOrder = -10;
        private const int DoorSortingOrder = -5;

        /// <summary>
        /// Room 01's encounter, transcribed from the prefab rather than newly designed: 6 enemies in
        /// one batch for 150 HP, the number every other room's budget is quoted against.
        ///
        /// This menu item did not exist until the floor loader needed it. Room 01 predates this
        /// builder and was assembled by hand, which made it the one room that could not be rebuilt
        /// when the pipeline gained a component — exactly the reproducibility argument
        /// `RoomLayout` and `BuildRunHUD` already make. Rebuilding does rename its spawn markers
        /// from `Spawn_0_Crawler` to `Spawn_0`; nothing reads those names (`WaveSpawner` holds the
        /// transforms directly), and marker order has not decided placement since the spawner began
        /// choosing against the player's position.
        /// </summary>
        [MenuItem("Deeper/Build Combat Room Prefab")]
        private static void BuildCombatRoom()
        {
            Build(Layout_UpperCaves_01.Map, "CombatRoom_UpperCaves_01", new[]
            {
                new WaveSpec(new GroupSpec(Crawler, 3), new GroupSpec(Slinger, 2), new GroupSpec(Brute, 1)),
            });
        }

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

        /// <summary>
        /// Combat Rooms 2-6 of LEVEL_DESIGN §2's six. Each is one batch of at most six bodies —
        /// the peak concurrency room 01 already proved readable — for **145-170 HP** against room
        /// 01's 150, so every one of them sits in BALANCE §8's 30-60s band.
        ///
        /// The composition is what differs, not the budget, and it is matched to the layout rather
        /// than rolled: the ranged-leaning fight goes in the room with the long approach, the swarm
        /// goes in the room ringed with cover to break line of sight against, and the two-Brute
        /// fights go in the rooms whose posts give her something to put between herself and a slam.
        /// A fight that ignored its room would make six layouts read as one.
        /// </summary>
        [MenuItem("Deeper/Build Combat Room 03 Prefab")]
        private static void BuildCombatRoom03()
        {
            // The pinch — two lanes, so a heavier five-body fight she has to split rather than
            // out-space. 170 HP.
            Build(Layout_UpperCaves_03.Map, "CombatRoom_UpperCaves_03", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 2), new GroupSpec(Crawler, 1), new GroupSpec(Slinger, 2)),
            });
        }

        [MenuItem("Deeper/Build Combat Room 04 Prefab")]
        private static void BuildCombatRoom04()
        {
            // West hall, east pocket — the longest approach in the biome, so the ranged-leaning
            // fight. 145 HP.
            Build(Layout_UpperCaves_04.Map, "CombatRoom_UpperCaves_04", new[]
            {
                new WaveSpec(new GroupSpec(Slinger, 3), new GroupSpec(Brute, 1), new GroupSpec(Crawler, 2)),
            });
        }

        [MenuItem("Deeper/Build Combat Room 05 Prefab")]
        private static void BuildCombatRoom05()
        {
            // The diagonal — a swarm with one anchor, which is the fight that most rewards using
            // the cracked patch as ground the player refuses rather than ground she avoids. 160 HP.
            Build(Layout_UpperCaves_05.Map, "CombatRoom_UpperCaves_05", new[]
            {
                new WaveSpec(new GroupSpec(Crawler, 5), new GroupSpec(Brute, 1)),
            });
        }

        [MenuItem("Deeper/Build Combat Room 06 Prefab")]
        private static void BuildCombatRoom06()
        {
            // The cracked heart — two Brutes to hold the middle and three Slingers to punish
            // standing still on it. 165 HP.
            Build(Layout_UpperCaves_06.Map, "CombatRoom_UpperCaves_06", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 2), new GroupSpec(Slinger, 3)),
            });
        }

        [MenuItem("Deeper/Build Combat Room 07 Prefab")]
        private static void BuildCombatRoom07()
        {
            // The cover ring — the most bodies, because three posts are what make a swarm
            // survivable. 155 HP.
            Build(Layout_UpperCaves_07.Map, "CombatRoom_UpperCaves_07", new[]
            {
                new WaveSpec(new GroupSpec(Crawler, 4), new GroupSpec(Brute, 1), new GroupSpec(Slinger, 1)),
            });
        }

        /// <summary>
        /// The three boss arenas.
        ///
        /// **They bake a single Tunnel Brute, and no boss ever fights it.** A room's encounter is
        /// content, not prefab data (<see cref="EncounterDefinition"/>): `FloorLoader` swaps in the
        /// one the pool's `RoomOption` carries as it mounts, which is how one Mini-Boss arena serves
        /// all three biomes' bosses. The baked fight only exists so the arena is playable on its own
        /// in the Room Lab, where nothing swaps anything — the same reason every other room ships
        /// with a fight authored on it.
        /// </summary>
        [MenuItem("Deeper/Build Mini-Boss Arena Prefab")]
        private static void BuildMiniBossArena()
        {
            Build(Layout_MiniBossArena_01.Map, "MiniBossArena_01", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 1)),
            });
        }

        [MenuItem("Deeper/Build Final Boss Arena Prefab")]
        private static void BuildFinalBossArena()
        {
            Build(Layout_FinalBossArena_01.Map, "FinalBossArena_01", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 1)),
            });
        }

        [MenuItem("Deeper/Build True Final Boss Arena Prefab")]
        private static void BuildTrueFinalBossArena()
        {
            Build(Layout_FinalBossArena_02.Map, "FinalBossArena_02", new[]
            {
                new WaveSpec(new GroupSpec(Brute, 1)),
            });
        }

        /// <summary>
        /// Every room in one press. There are ten of them now, and rebuilding them one menu item at
        /// a time after a change to the shared builder is how one room quietly stays a version
        /// behind the other nine.
        /// </summary>
        [MenuItem("Deeper/Build All Room Prefabs")]
        private static void BuildAllRooms()
        {
            BuildCombatRoom();
            BuildCombatRoom03();
            BuildCombatRoom04();
            BuildCombatRoom05();
            BuildCombatRoom06();
            BuildCombatRoom07();
            BuildWaveRoom();
            BuildSecretVault();
            BuildMiniBossArena();
            BuildFinalBossArena();
            BuildTrueFinalBossArena();

            Debug.Log("Build All Room Prefabs: 11 room prefab(s) rebuilt.");
        }

        private static void Build(string[] map, string roomName, WaveSpec[] encounter)
        {
            if (!RoomLayout.Validate(map, roomName)) return;

            // Reported, not fatal: a post one cell too close is a tuning mistake in a map somebody
            // is in the middle of editing, and refusing to build the room would hide the rest of
            // what changed. It logs an error, which is loud enough to stop a commit.
            RoomLayout.ValidatePosts(map, roomName);

            GameObject root = new GameObject(roomName);

            try
            {
                // Isometric, and the projection lives in RoomLayout.CellCentre so that doors, the
                // entry band, spawn markers and the player start all land on the same diamonds the
                // tilemap draws — one decision rather than a conversion sprinkled through here.
                Grid grid = root.AddComponent<Grid>();
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = RoomLayout.CellSize;

                CombatRoom room = root.AddComponent<CombatRoom>();

                Transform tiles = Group(root.transform, "Tiles");

                // Empty at build time. IsometricRoomView paints it on every enable, which is what
                // lets a room look different each time it is mounted without a collider moving.
                Tilemap floor = BuildTilemap(tiles, "Floor", FloorSortingOrder, false);
                floor.GetComponent<TilemapRenderer>().mode = TilemapRenderer.Mode.Individual;
                floor.GetComponent<TilemapRenderer>().sortOrder = TilemapRenderer.SortOrder.TopRight;

                // Collision only, never drawn. The walls you SEE are sprites the view spawns and
                // sorts; what you bump into is this tilemap, whose diamond-shaped Grid colliders
                // match the cells exactly. Separating them is what makes the visuals re-rollable.
                Tilemap walls = BuildTilemap(tiles, "Collision", WallSortingOrder, true);
                walls.GetComponent<TilemapRenderer>().enabled = false;

                RoomLayout.Paint(floor, walls, map);

                // The floor pass above is flat art on an isometric grid, so it is thrown away and
                // left to the view; only the collision cells matter from here.
                floor.ClearAllTiles();

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
                Transform arrival = BuildPlayerStart(root.transform, map);
                BuildConnection(root, map, doors, arrival);
                BuildIsometricView(root, map, grid, floor);

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

        /// <summary>
        /// One door object per doorway CELL, not per column.
        ///
        /// The flat version grouped a doorway into a 2-cell column and gave it one 1x2 collider,
        /// which only means anything on a square grid. On an isometric grid those two cells are two
        /// diamonds lying diagonally, so a single upright box covers neither. A door per cell is
        /// both simpler and correct — <c>CombatRoom.doors</c> is an array and opens or shuts all of
        /// them together, so a two-cell doorway just contributes two entries.
        /// </summary>
        private static RoomDoor[] BuildDoorColumns(Transform group, string[] map, char symbol,
                                                   Sprite sprite, bool floorDoor)
        {
            var doors = new List<RoomDoor>();
            List<Vector2Int> cells = RoomLayout.MarkerCells(map, symbol);
            if (cells.Count == 0) return doors.ToArray();

            // West and east are decided from the projected position, because "lower x" is what the
            // floor loader and RoomConnection actually compare — and on a diamond grid the cell's
            // column index and its screen x are not the same ordering.
            float middleX = 0f;
            foreach (Vector2Int cell in cells) middleX += RoomLayout.CellCentre(cell.x, cell.y).x;
            middleX /= cells.Count;

            int index = 0;
            foreach (Vector2Int cell in cells)
            {
                Vector3 centre = RoomLayout.CellCentre(cell.x, cell.y);

                string name = floorDoor
                    ? (centre.x <= middleX ? "DoorWest" : "DoorEast") + "_" + index
                    : "VaultDoor_" + index;
                index++;

                var go = new GameObject(name);
                go.transform.SetParent(group, false);
                go.transform.localPosition = centre;

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;

                // Actors, not Default: a door stands in the room and has to sort against whatever
                // walks past it, exactly like the wall blocks the view spawns.
                sr.sortingLayerName = "Actors";
                sr.sortingOrder = -Mathf.RoundToInt(centre.y / 0.1f) * 2;

                // Sized to the diamond rather than to a square cell. 0.8 of the footprint leaves the
                // gap passable at its corners while still plugging the opening.
                BoxCollider2D barrier = go.AddComponent<BoxCollider2D>();
                barrier.size = new Vector2(RoomLayout.CellSize.x * 0.8f, RoomLayout.CellSize.y * 0.8f);

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
        /// The trigger band, shaped to the `=` footprint. **Layer 8 `RoomTrigger`, never Default** —
        /// `ThrownRock.blockingLayers` is Default, so a Default-layer volume across the room eats
        /// every rock a Slinger throws through it.
        ///
        /// **A polygon, not a box, and that is a bug fix rather than a refinement.** On a diamond
        /// grid a column of cells is a 2:1 DIAGONAL in world space, so its axis-aligned bounding box
        /// is a rectangle about twice the band's area, reaching into both halves of the room. In the
        /// 16x10 Combat Rooms that box swallowed the player start: the run mounted its first room,
        /// `RoomEntry.OnTriggerStay2D` fired on the frame she was placed on the arrival marker, six
        /// enemies arrived on top of her, and she died without a key being pressed. The old 28x16
        /// layout cleared the box by half a tile, which is the only reason it had never shown up —
        /// and it is also why the fight there sprang earlier than the map looked like it should.
        ///
        /// The band's true shape is a parallelogram, because the grid is an affine map: cell (x, y)
        /// lands at `origin + x*(1, 0.5) + y*(-1, 0.5)`. Sweeping x and y each half a cell past their
        /// extremes gives its four corners exactly, with no fudge factor.
        /// </summary>
        private static void BuildEntry(Transform root, string[] map, CombatRoom room)
        {
            List<Vector2Int> cells = RoomLayout.MarkerCells(map, '=');
            if (cells.Count == 0)
            {
                Debug.LogError("Map has no '=' entry band; the room could never be sprung.");
                return;
            }

            int xMin = cells[0].x, xMax = cells[0].x;
            int yMin = cells[0].y, yMax = cells[0].y;

            foreach (Vector2Int cell in cells)
            {
                if (cell.x < xMin) xMin = cell.x;
                if (cell.x > xMax) xMax = cell.x;
                if (cell.y < yMin) yMin = cell.y;
                if (cell.y > yMax) yMax = cell.y;
            }

            var go = new GameObject("Entry");
            go.transform.SetParent(root, false);
            go.transform.localPosition = Vector3.zero;
            go.layer = 8;

            PolygonCollider2D area = go.AddComponent<PolygonCollider2D>();
            area.points = BandCorners(xMin - 0.5f, xMax + 0.5f, yMin - 0.5f, yMax + 0.5f);
            area.isTrigger = true;

            RoomEntry entry = go.AddComponent<RoomEntry>();
            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("room").objectReferenceValue = room;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The four world corners of a cell range, wound counter-clockwise.
        ///
        /// Written against the same basis vectors <see cref="RoomLayout.CellCentre"/> uses, so the
        /// volume lands on the diamonds the tilemap draws rather than near them. Fractional cell
        /// coordinates are meaningful here precisely because the projection is affine: half a cell
        /// past the last centre IS the edge of that cell.
        /// </summary>
        private static Vector2[] BandCorners(float xMin, float xMax, float yMin, float yMax)
        {
            return new[]
            {
                Corner(xMin, yMin),
                Corner(xMax, yMin),
                Corner(xMax, yMax),
                Corner(xMin, yMax),
            };
        }

        private static Vector2 Corner(float x, float y)
        {
            Vector3 origin = RoomLayout.CellCentre(0, 0);

            return new Vector2(
                origin.x + (x - y) * RoomLayout.CellSize.x * 0.5f,
                origin.y + (x + y) * RoomLayout.CellSize.y * 0.5f);
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

        private static Transform BuildPlayerStart(Transform root, string[] map)
        {
            Vector3 position;
            if (!RoomLayout.TryGetMarker(map, RoomLayout.PlayerStart, out position))
            {
                Debug.LogError("Map has no 'P' player start.");
                return null;
            }

            var go = new GameObject("PlayerStart");
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        /// <summary>
        /// Records which doorway is the way in and which is the way on, so the floor loader can butt
        /// two rooms together without knowing anything about either layout.
        ///
        /// Which is which is decided by position rather than by name, because
        /// <see cref="BuildDoorColumns"/> names every non-zero column "DoorEast" — a room with three
        /// floor-door columns would produce two objects with the same name, and a name lookup would
        /// silently pick whichever came first.
        /// </summary>
        private static void BuildConnection(GameObject root, string[] map, RoomDoor[] doors,
                                            Transform arrival)
        {
            RoomDoor west = null;
            RoomDoor east = null;

            // A doorway is several door objects — one per `D` cell — so "one door" has never meant
            // "one doorway". What separates them is the MAP COLUMN they were cut into, and this
            // reads that directly instead of inferring it from world positions.
            //
            // The world-position version this replaces got a dead end wrong, and silently. It split
            // the doors on the room's projected mid-line and then fell back to "if nothing landed
            // east of centre, use the far door anyway, or the bag would treat this as a dead end it
            // is not" — which is exactly backwards for a room that IS one. The Secret Vault has a
            // single west doorway of two cells; both sit left of the mid-line, the fallback fired,
            // and one of its own west doors came back as its exit. `RoomConnection.HasExit` was then
            // true for the one room in the game authored to be a detour, so `RoomBag.Draw(needsExit)`
            // — the only guard against mounting an unwalkable room mid-floor — would have drawn it
            // onto the route. Zyno's arena is authored the same way and would have hit it too.
            //
            // Columns cannot be wrong about this: one distinct `D` column is a dead end, two are a
            // through-room, and no projection is involved.
            List<Vector2Int> cells = RoomLayout.MarkerCells(map, 'D');

            if (doors != null && doors.Length > 0 && doors.Length == cells.Count)
            {
                int westColumn = cells[0].x;
                int eastColumn = cells[0].x;

                foreach (Vector2Int cell in cells)
                {
                    if (cell.x < westColumn) westColumn = cell.x;
                    if (cell.x > eastColumn) eastColumn = cell.x;
                }

                // The FIRST cell of each doorway, and consistently so: `MarkerCells` walks y upward,
                // so both anchors are the lower cell of their doorway. Adjacency is measured between
                // these two transforms, and picking the lower cell in one room and the upper in the
                // next would offset the whole floor by a tile.
                for (int i = 0; i < doors.Length; i++)
                {
                    if (west == null && cells[i].x == westColumn) west = doors[i];
                    if (east == null && eastColumn != westColumn && cells[i].x == eastColumn) east = doors[i];
                }
            }
            else if (doors != null && doors.Length > 0)
            {
                Debug.LogError(root.name + ": " + doors.Length + " door objects for " + cells.Count +
                               " 'D' cells. BuildDoorColumns and MarkerCells have drifted apart, so " +
                               "the room's connection cannot be resolved.", root);
            }

            RoomConnection connection = root.AddComponent<RoomConnection>();
            SerializedObject so = new SerializedObject(connection);
            so.FindProperty("west").objectReferenceValue = west;
            so.FindProperty("east").objectReferenceValue = east;
            so.FindProperty("arrival").objectReferenceValue = arrival;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the dresser and bakes the answer to "which cells may take a decal".
        ///
        /// Baked here rather than resolved at runtime because the map lives in an editor-only
        /// `Layout_*.cs` and a shipped component cannot read it — the same reason every other
        /// positional fact in this builder is derived from the map at build time instead of typed
        /// twice.
        /// </summary>
        /// <summary>
        /// Adds the component that draws the room, and bakes the map into it.
        ///
        /// The map is baked rather than looked up so a room prefab is self-contained — it carries
        /// its own shape and needs no asset resolved at runtime. Art sets are filled from
        /// `Art/Environment/&lt;biome&gt;/Iso/`, so dropping a new tile in and re-running this is the
        /// whole workflow for adding a look.
        /// </summary>
        private static void BuildIsometricView(GameObject root, string[] map, Grid grid, Tilemap floor)
        {
            var scenery = Group(root.transform, "Scenery");
            var view = root.AddComponent<IsometricRoomView>();

            var so = new SerializedObject(view);

            SerializedProperty rows = so.FindProperty("map");
            rows.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++) rows.GetArrayElementAtIndex(i).stringValue = map[i];

            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("floor").objectReferenceValue = floor;
            so.FindProperty("scenery").objectReferenceValue = scenery;

            SetArray(so.FindProperty("floorTiles"), IsoAssets<TileBase>("Tile_Iso_Floor_"));
            SetArray(so.FindProperty("wallBlocks"), IsoSprites("Wall_"));
            SetArray(so.FindProperty("props"), PropSprites());

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object[] IsoAssets<T>(string prefix) where T : Object
        {
            var found = new System.Collections.Generic.List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { "Assets/_Main/Data/Tiles" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Iso/")) continue;
                if (!System.IO.Path.GetFileName(path).StartsWith(prefix)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) found.Add(asset);
            }
            return found.ToArray();
        }

        private static Object[] IsoSprites(string prefix)
        {
            const string folder = "Assets/_Main/Art/Environment/UpperCaves/Iso";
            var found = new System.Collections.Generic.List<Object>();
            if (!System.IO.Directory.Exists(folder)) return found.ToArray();

            foreach (string file in System.IO.Directory.GetFiles(folder, prefix + "*.png"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(file));
                if (sprite != null) found.Add(sprite);
            }
            return found.ToArray();
        }

        private static Object[] PropSprites()
        {
            const string folder = "Assets/_Main/Art/Environment/UpperCaves";
            var found = new System.Collections.Generic.List<Object>();
            if (!System.IO.Directory.Exists(folder)) return found.ToArray();

            foreach (string file in System.IO.Directory.GetFiles(folder, "Prop_*.png"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(file));
                if (sprite != null) found.Add(sprite);
            }
            return found.ToArray();
        }

        private static string AssetPath(string file)
        {
            string path = file.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            int index = path.IndexOf("Assets/");
            return index > 0 ? path.Substring(index) : path;
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
