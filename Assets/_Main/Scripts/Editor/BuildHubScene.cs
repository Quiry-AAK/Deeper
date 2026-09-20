using System.Collections.Generic;
using Deeper.Core;
using Deeper.Hub;
using Deeper.Run;
using Deeper.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds `HubScene` — the surface camp the run starts from (GDD §Game Loop 1).
    ///
    /// **Generated, not hand-authored**, the same argument <see cref="BuildRoomPrefab"/>,
    /// <see cref="BuildRunHUD"/> and <see cref="BuildRoomVisualTestScene"/> already make: a scene
    /// assembled by dragging is one nobody can reproduce, review or diff. The camp's shape lives in
    /// <see cref="Layout_Hub_01"/> and everything below is the one pass that stamps it.
    ///
    /// It reuses the room pipeline where the pipeline is the *rules* — <see cref="RoomLayout"/>'s
    /// isometric projection and its wall painting — and not where the pipeline is a *combat room*.
    /// There is deliberately no <c>CombatRoom</c>, no <c>WaveSpawner</c> and no
    /// <c>IsometricRoomView</c> here. That last one is the interesting omission: its whole purpose
    /// is re-rolling a pooled room's visuals on every mount, and the camp is the one place in the
    /// game that must look the same every time you come back to it. It is also load bearing — each
    /// fixture's trigger volume has to sit exactly where its art is drawn, which a random scatter
    /// cannot promise.
    /// </summary>
    public static class BuildHubScene
    {
        private const string ScenePath = "Assets/_Main/Scenes/HubScene.unity";
        private const string PlayerPrefab = "Assets/_Main/Prefabs/Player.prefab";
        private const string CameraPrefab = "Assets/_Main/Prefabs/Rig/Main Camera.prefab";
        private const string LightPrefab = "Assets/_Main/Prefabs/Rig/Global Light 2D.prefab";
        private const string IsoFolder = "Assets/_Main/Art/Environment/SurfaceCamp/Iso/";
        private const string PropFolder = "Assets/_Main/Art/Environment/SurfaceCamp/Props/";
        private const string MarkerFolder = "Assets/_Main/Art/Environment/SurfaceCamp/Markers/";

        /// <summary>World units between a fixture's top edge and the mark floating over it.</summary>
        private const float MarkerGap = 0.35f;

        /// <summary>Ground pieces sit above the floor tilemap and below every actor.</summary>
        private const int GroundPieceSortingOrder = -15;

        /// <summary>
        /// Measured off Wall_Stone.png: its top face is a diamond centred on row 19 and the block
        /// body runs 28px below that, so the bottom face centres on row 47. The sprite imports with
        /// a Center pivot (row 32), so drawing it at the plain cell centre buries 15px of wall under
        /// the floor and leaves only 13px standing — the owner's "walls are tiny".
        /// </summary>
        private const float WallLift = 15f / 32f;

        /// <summary>The block's own height, so a second course stacks exactly on the first.</summary>
        private const float WallCourse = 28f / 32f;

        /// <summary>
        /// Courses per wall cell. Two puts the top at 1.75 world units against a 1.5-unit player —
        /// just over her head, which is what makes the camp read as enclosed rather than kerbed.
        /// </summary>
        private const int WallCourses = 2;

        /// <summary>
        /// Night beyond the camp. Not black: a flat black frame reads as a rendering failure rather
        /// than as sky, and the camp is outdoors. A deep desaturated indigo sits under the same cool
        /// wash <see cref="NightAmbient"/> puts on everything else, so the edge of the world looks
        /// like more night instead of a hole. Deliberately not one of ART_DIRECTION §2's reserved
        /// hazard accents, and dark enough that the bone markers and the fire still carry.
        /// </summary>
        private static readonly Color NightSky = new Color(0.09f, 0.105f, 0.185f);

        /// <summary>
        /// How many cells of unlit moorland are painted around the camp, past its walls.
        ///
        /// **A flat colour was not enough and the first attempt proved it.** The camp is a diamond
        /// floating in whatever the camera clears to, and the editor's Game view is currently
        /// 900x239 — about 60 world units across against a 14-unit camp — so most of the screen is
        /// that colour. Darkening it toward black just made the camp look like it was cut out and
        /// pasted onto nothing.
        ///
        /// Ground that keeps going is the honest fix: the camp is on a hillside at night, so beyond
        /// the wall there is more hillside, unlit. 22 cells covers the width of that viewport with
        /// room to spare, and a tilemap does not care how many cells it holds.
        /// </summary>
        private const int SurroundRadius = 22;

        /// <summary>
        /// The unlit moor's tint at the camp's edge. Dark enough to read as "outside the firelight"
        /// without going black — at black it stops being ground and becomes a hole again.
        /// </summary>
        private static readonly Color SurroundTint = new Color(0.34f, 0.36f, 0.46f);

        /// <summary>
        /// How many concentric bands the moor fades through. Each is its own tilemap with its own
        /// tint, because per-cell colour does not survive a scene reload — see PaintSurround.
        ///
        /// It fades on ALPHA rather than toward black, so what shows through is the sky colour
        /// rather than a second, darker void — which is the whole point of the exercise.
        /// </summary>
        private const int SurroundBands = 6;

        /// <summary>
        /// How much of a fixture's measured width becomes solid. Below 1 because a sprite's widest
        /// row includes overhang you should be able to walk under — a tent's guy ropes, the
        /// headframe's winch arm.
        /// </summary>
        private const float BlockerScale = 0.8f;

        /// <summary>How far beyond its own blocker a station can be used from, in world units.</summary>
        private const float StationReach = 1.2f;
        private const string ConfigPath = "Assets/_Main/Data/Run/RunConfig.asset";
        private const string ShardBankPath = "Assets/_Main/Data/Meta/ShardBank.asset";

        /// <summary>
        /// HUD_SlotSquare's border, so the socket lands exactly on the slot's transparent hole.
        ///
        /// A named constant duplicated from <c>BuildRunHUD.SlotBorder</c> rather than shared,
        /// which is the shape that file's own comment argues for: the art tool and each layout tool
        /// state the number independently so no one of them depends on another, and
        /// <c>HUDFrameArt</c> is the single place it is actually drawn.
        /// </summary>
        private const float SlotBorder = 4f;

        /// <summary>Layer 8. Trigger volumes never go on Default — see RoomEntry's note.</summary>
        private const int RoomTriggerLayer = 8;

        private const int GroundSortingOrder = -20;

        /// <summary>Under the camp's own ground, so the two never fight at the boundary.</summary>
        private const int SurroundSortingOrder = -30;

        /// <summary>
        /// The camp at night, applied to the Global Light 2D. Cool and fairly strong, because it is
        /// doing the work the tiles deliberately do not: they are authored at neutral daylight value
        /// so they read at full detail, and this is what makes it night. That split is style-guide
        /// §8's own recommendation — bake form, light mood — and asking PixelLab for "moonlit" art
        /// as well would darken everything twice.
        /// </summary>
        private static readonly Color NightAmbient = new Color(0.55f, 0.60f, 0.86f);

        /// <summary>
        /// One fixture in the camp. A table rather than a switch because every one of these is the
        /// same four decisions, and a switch would let two of them answer the fifth differently.
        /// </summary>
        private struct Fixture
        {
            public char Symbol;
            public string Name;
            public string Sprite;

            /// <summary>Empty when this is scenery. Anything else is a usable station's prompt.</summary>
            public string Prompt;

            /// <summary>Whether she is stopped by it. A lantern post is thin enough not to bother.</summary>
            public bool Blocks;

            /// <summary>
            /// World-unit shift from the cell centre. Zero for everything that stands on its own
            /// cell.
            /// </summary>
            public float OffsetY;

            /// <summary>
            /// Draw this fixture flat on the ground, under every actor, instead of Y-sorting it
            /// against them. Only the mine shaft's pit: it is a hole she stands *in*.
            /// </summary>
            public bool Ground;

            /// <summary>
            /// A second sprite drawn above the actors and Y-sorted at the same cell, or empty.
            ///
            /// This is what makes the mine shaft work. A hole in the ground and the tower over it
            /// are two different things at two different depths, and one sprite can only ever be at
            /// one depth — so she was always drawn either wholly in front of the whole structure or
            /// wholly behind it, and never inside it. Splitting the shipped art by rows was tried
            /// first and could not express it either: the tower's legs run down through the
            /// platform, so no horizontal cut separates them. They are two pieces of art now.
            /// </summary>
            public string Companion;
        }

        private static readonly Fixture[] Fixtures =
        {
            new Fixture { Symbol = 'W', Name = "WeaponRack", Sprite = "Prop_WeaponRack", Prompt = "CHOOSE WEAPON",  Blocks = true },
            new Fixture { Symbol = 'S', Name = "StatShrine", Sprite = "Prop_Shrine",     Prompt = "MINER'S SHRINE", Blocks = true },
            new Fixture { Symbol = 'C', Name = "Codex",      Sprite = "Prop_Tent",       Prompt = "READ THE CODEX", Blocks = true },
            new Fixture { Symbol = 'M', Name = "MineShaft",  Sprite = "Prop_ShaftPit",   Prompt = "DESCEND",        Blocks = true, Ground = true, Companion = "Prop_ShaftTower" },
            new Fixture { Symbol = 'f', Name = "Campfire",   Sprite = "Prop_Campfire",   Prompt = null,             Blocks = true },
            new Fixture { Symbol = 'c', Name = "Crates",     Sprite = "Prop_Crates",     Prompt = null,             Blocks = true },
            new Fixture { Symbol = 'l', Name = "Lantern",    Sprite = "Prop_Lantern",    Prompt = null,             Blocks = false },
        };

        /// <summary>
        /// Ground variants per map character, listed with repeats so the pick is **weighted**.
        ///
        /// Both halves of this were found by compositing the camp and looking at it. One tile
        /// stamped across an area turns its own texture into wallpaper — the first camp had a
        /// visible motif repeating in every dirt cell. An even mix of variants is worse in a
        /// different way: these differ in value as well as texture, so equal weights read as
        /// mismatched floor tiles rather than as ground. A dominant base with occasional darker
        /// patches is what reads as worn earth.
        /// </summary>
        private static readonly Dictionary<char, string[]> Ground = new Dictionary<char, string[]>
        {
            { '.', new[] { "Floor_Dirt", "Floor_Dirt", "Floor_Dirt", "Floor_Dirt",
                           "Floor_Earth", "Floor_Earth", "Floor_Earth", "Floor_Grit" } },
            { ',', new[] { "Floor_Grass", "Floor_Grass", "Floor_Grass", "Floor_Patchy" } },
        };

        [MenuItem("Deeper/Build Hub Scene")]
        public static void Build()
        {
            string[] map = Layout_Hub_01.Map;

            if (!RoomLayout.Validate(map, "Layout_Hub_01", Layout_Hub_01.Legend)) return;

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLight();
            GameObject camera = BuildCamera();
            BuildEventSystem();

            GameObject player = BuildPlayer(map);
            if (camera != null && player != null)
            {
                HUDLayout.Wire(camera.GetComponent<Deeper.CameraControl.CameraRig>(),
                               "target", player.transform);
            }

            Grid grid = BuildGrid();
            PaintSurround(grid, map);
            PaintGround(grid, map);
            BuildWalls(grid, map);

            Dictionary<char, HubStation> stations = BuildFixtures(grid, map);

            BuildInterface(stations);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();

            Debug.Log("Built " + ScenePath + " — " + stations.Count + " usable station(s).",
                      AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        // ---------------------------------------------------------------- scene rig

        private static void BuildLight()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefab);
            if (prefab == null) { Debug.LogError("No light prefab at " + LightPrefab); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            var light = instance.GetComponent<Light2D>();
            if (light != null) light.color = NightAmbient;
        }

        private static GameObject BuildCamera()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab);
            if (prefab == null) { Debug.LogError("No camera prefab at " + CameraPrefab); return null; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // Set on the INSTANCE, not the prefab. The camera rig is shared with TestScene and the
            // room lab, and a night sky is the Hub's decision — a cave has no sky at all.
            var camera = instance.GetComponent<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = NightSky;
            }

            return instance;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject BuildPlayer(string[] map)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (prefab == null) { Debug.LogError("No player prefab at " + PlayerPrefab); return null; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Vector3 start;
            if (!RoomLayout.TryGetMarker(map, RoomLayout.PlayerStart, out start))
            {
                Debug.LogWarning("The camp map has no 'P', so she spawns at the origin.");
                start = Vector3.zero;
            }

            instance.transform.position = start;
            return instance;
        }

        // ---------------------------------------------------------------- ground

        private static Grid BuildGrid()
        {
            var go = new GameObject("Camp", typeof(Grid));

            Grid grid = go.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = RoomLayout.CellSize;

            return grid;
        }

        /// <summary>
        /// Paints the two tilemaps: the one you see and the one you bump into.
        ///
        /// Collision comes from <see cref="RoomLayout.Paint"/> — the same call every room prefab
        /// makes, so the camp's walls collide by exactly the rules a room's do. Its floor pass is
        /// then thrown away and repainted here with the camp's own variants, which is also what
        /// <see cref="BuildRoomPrefab"/> does.
        /// </summary>
        /// <summary>
        /// The unlit hillside the camp sits on — grass tiles painted well past the walls and faded
        /// out with distance, so the camp is somewhere rather than a diamond in a void.
        ///
        /// Drawn under everything (<see cref="SurroundSortingOrder"/>) and carrying no collision at
        /// all: it is scenery beyond a wall she cannot cross anyway, and a collider out here would
        /// be thousands of cells of physics for nothing.
        ///
        /// **The fade is one tilemap per band, tinted through <c>Tilemap.color</c>, and that shape
        /// is forced.** The obvious version — one tilemap, <c>SetTileFlags(TileFlags.None)</c> then
        /// <c>SetColor</c> per cell — looks correct in the editor and then **silently loses every
        /// colour when the scene reloads into play mode**, because a <c>Tile</c> asset rewrites both
        /// the colour and the flags from itself in <c>GetTileData</c> on every refresh. The result
        /// was a full-bright green field filling the screen, which also read as though the scene had
        /// lost its lighting. Renderer-level tint is a serialized property of the component and
        /// survives, so the bands are real objects instead.
        /// </summary>
        private static void PaintSurround(Grid grid, string[] map)
        {
            int height = RoomLayout.Height(map);
            int width = RoomLayout.Width(map);

            var bands = new Tilemap[SurroundBands];

            for (int band = 0; band < SurroundBands; band++)
            {
                Tilemap moor = NewTilemap(grid.transform, "Surround_" + band,
                                          SurroundSortingOrder, false);

                TilemapRenderer renderer = moor.GetComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Individual;
                renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;

                // Squared, so the moor thins out quickly near the camp and lingers faintly beyond.
                float t = (band + 1) / (float)SurroundBands;
                Color tint = SurroundTint;
                tint.a = (1f - t) * (1f - t);

                moor.color = tint;
                bands[band] = moor;
            }

            for (int y = -SurroundRadius; y < height + SurroundRadius; y++)
            {
                for (int x = -SurroundRadius; x < width + SurroundRadius; x++)
                {
                    // The camp paints itself; this is only what lies outside it.
                    if (x >= 0 && x < width && y >= 0 && y < height) continue;

                    // How far outside the camp this cell is, in cells, on whichever axis is worse.
                    int outX = x < 0 ? -x : (x >= width ? x - width + 1 : 0);
                    int outY = y < 0 ? -y : (y >= height ? y - height + 1 : 0);

                    float distance = Mathf.Max(outX, outY) / (float)SurroundRadius;
                    int index = Mathf.Clamp(Mathf.FloorToInt(distance * SurroundBands),
                                            0, SurroundBands - 1);

                    TileBase tile = GroundFor(',', x, y);
                    if (tile == null) continue;

                    bands[index].SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            foreach (Tilemap band in bands) band.CompressBounds();
        }

        private static void PaintGround(Grid grid, string[] map)
        {
            Tilemap ground = NewTilemap(grid.transform, "Ground", GroundSortingOrder, false);

            // Per-tile, not chunked, matching the room builders. An isometric floor tile is a
            // diamond with a slab under it, so a near tile has to be drawn OVER the one behind to
            // hide that slab's edge, and chunked mode draws the whole map as one unordered mesh.
            TilemapRenderer renderer = ground.GetComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Individual;
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;

            // **Neither of the two lines above is what makes a floor look flat, and both were
            // suspected first.** The camp's first build came out as a field of raised blocks with
            // every slab edge showing — the exact symptom the room builders' comments blame on
            // chunked mode. It was not that: the built renderer read back mode Individual and
            // sortOrder TopRight, and flipping sortOrder to BottomLeft changed nothing on screen,
            // because URP's transparency sort axis is what actually orders these tiles.
            //
            // The cause was the ART, and it is a hard constraint on every isometric tile this
            // project ever generates. Profiling the PNG row by row: the top-face diamond spans 35
            // rows and is followed by a 20-row full-width SIDE WALL. Cells step 32px apart
            // vertically, so the tile in front covers only ~6 of those 20 rows and 14px of wall
            // stays visible on every single cell. PixelLab's `tile_shape` is the control:
            // "thick tile" and "block" carry a wall far too deep for a 2:1 grid, and floors must be
            // generated as "thin tile". Walls are the opposite case and stay blocks — a wall is
            // *meant* to read as raised.

            Tilemap collision = NewTilemap(grid.transform, "Collision", GroundSortingOrder, true);
            collision.GetComponent<TilemapRenderer>().enabled = false;

            RoomLayout.Paint(ground, collision, map);
            ground.ClearAllTiles();

            int height = RoomLayout.Height(map);
            int width = RoomLayout.Width(map);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TileBase tile = GroundFor(RoomLayout.At(map, x, y), x, y);
                    if (tile != null) ground.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            ground.CompressBounds();
        }

        /// <summary>
        /// The ground under one cell. Grass under the tent because it stands on the shelf; dirt
        /// under everything else, including the wall ring — a wall is not a floor, and a hole behind
        /// one shows as void the moment anything is destructible.
        /// </summary>
        private static TileBase GroundFor(char symbol, int x, int y)
        {
            // 'C' is the Codex tent, which stands on the grass shelf. This read 't' until the tent
            // stopped being scenery and became a station — a stale character left by the rename,
            // which quietly put the tent on bare dirt instead.
            char key = symbol == ',' || symbol == 'C' ? ',' : '.';

            string[] set;
            if (!Ground.TryGetValue(key, out set) || set.Length == 0) return null;

            // Hashed on the coordinate rather than drawn from Random: the camp is one authored
            // place, and a ground that reshuffled every time the scene was rebuilt would make every
            // rebuild a diff.
            int pick = Mathf.Abs((x * 73856093) ^ (y * 19349663)) % set.Length;

            return AssetDatabase.LoadAssetAtPath<Tile>(TilePath(set[pick]));
        }

        private static string TilePath(string sprite)
        {
            // Written by Deeper/Generate Isometric Tiles, which prefixes what it makes.
            return "Assets/_Main/Data/Tiles/SurfaceCamp/Iso/Tile_Iso_" + sprite + ".asset";
        }

        private static Tilemap NewTilemap(Transform parent, string name, int order, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Tilemap map = go.AddComponent<Tilemap>();
            TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = order;

            if (collide) go.AddComponent<TilemapCollider2D>();

            return map;
        }

        // ---------------------------------------------------------------- walls

        /// <summary>
        /// The wall you see, one sprite per cell — separate from the collision tilemap above, and
        /// for the reason <c>IsometricRoomView</c> gives: a wall block has to Y-sort against the
        /// player so she walks behind the far wall and in front of the near one, and a tilemap
        /// cannot sort per cell against a moving actor.
        /// </summary>
        private static void BuildWalls(Grid grid, string[] map)
        {
            Sprite block = AssetDatabase.LoadAssetAtPath<Sprite>(IsoFolder + "Wall_Stone.png");
            if (block == null)
            {
                Debug.LogWarning("No Wall_Stone.png in " + IsoFolder + " — the camp builds with no walls.");
                return;
            }

            Transform group = NewGroup(grid.transform, "Walls");

            int height = RoomLayout.Height(map);
            int width = RoomLayout.Width(map);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char symbol = RoomLayout.At(map, x, y);
                    if (symbol != RoomLayout.Wall && symbol != RoomLayout.Post) continue;

                    Vector3 at = RoomLayout.CellCentre(x, y);

                    // Lifted so the block STANDS on the floor instead of sinking into it, then
                    // stacked: one course is a kerb, two is a wall. Each course is a whole copy of
                    // the same sprite offset by exactly its own height, so the courses cannot
                    // mismatch and no second piece of art is needed.
                    for (int course = 0; course < WallCourses; course++)
                    {
                        Vector3 stacked = at + new Vector3(0f, WallLift + course * WallCourse, 0f);

                        // Sorted from the CELL, not from the lifted position. A raised course is
                        // not further back — it is the same cell, higher up — and sorting it by its
                        // own y would make the upper course of a near wall lose to the lower course
                        // of the wall behind it.
                        NewSprite("Wall", group, block, stacked, at.y);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- fixtures

        /// <summary>
        /// Places every fixture the map marks, and returns the usable ones by symbol so the
        /// interface can be wired to them.
        /// </summary>
        private static Dictionary<char, HubStation> BuildFixtures(Grid grid, string[] map)
        {
            Transform group = NewGroup(grid.transform, "Fixtures");
            var stations = new Dictionary<char, HubStation>();

            foreach (Fixture fixture in Fixtures)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PropFolder + fixture.Sprite + ".png");
                if (sprite == null)
                {
                    Debug.LogWarning("No " + fixture.Sprite + ".png in " + PropFolder + " — " +
                                     fixture.Name + " is not placed.");
                    continue;
                }

                List<Vector3> cells = RoomLayout.Markers(map, fixture.Symbol);
                string artPath = PropFolder + fixture.Sprite + ".png";

                // Placement, collision and reach all come from one measurement of the art, so a new
                // prop needs no hand-tuned numbers and none of the three can drift from the others.
                ArtBounds bounds = Measure(artPath);

                for (int i = 0; i < cells.Count; i++)
                {
                    string name = cells.Count > 1 ? fixture.Name + "_" + i : fixture.Name;

                    // Dropped by the art's own bottom padding. The pivot is the canvas bottom, but
                    // a generated prop is centred on a square canvas and carries a few transparent
                    // rows underneath — so pivoting there leaves it hovering. The shrine's padding
                    // is 13px, which is a visible 0.4 units of daylight under a stone obelisk.
                    Vector3 at = cells[i] + new Vector3(0f, fixture.OffsetY - bounds.Bottom, 0f);

                    GameObject go = NewSprite(name, group, sprite, at, cells[i].y);

                    if (fixture.Ground)
                    {
                        // Under every actor, so she walks over the hole rather than behind it.
                        SpriteRenderer flat = go.GetComponent<SpriteRenderer>();
                        flat.sortingLayerName = "Default";
                        flat.sortingOrder = GroundPieceSortingOrder;
                    }

                    // The part that stands up, Y-sorted at the same cell so she passes between its
                    // legs: in front of it from the south, behind it from the north.
                    // How high the mark has to float to clear whatever is actually there, measured
                    // in the fixture object's own frame. A companion stands at the ground piece's
                    // footprint centre, so its top is that much higher again — measuring from the
                    // ground piece alone put the shaft's mark inside the tower's roof.
                    float markerHeight = bounds.Top + MarkerGap;

                    if (!string.IsNullOrEmpty(fixture.Companion))
                    {
                        ArtBounds standing = Measure(PropFolder + fixture.Companion + ".png");
                        AddCompanion(group, name, fixture.Companion, cells[i], standing, bounds);

                        markerHeight = bounds.Bottom + bounds.Width * 0.25f
                                     - standing.Bottom + standing.Top + MarkerGap;
                    }

                    if (fixture.Blocks) AddBlocker(go.transform, bounds);

                    if (string.IsNullOrEmpty(fixture.Prompt)) continue;

                    HubStation station = AddStation(go.transform, fixture.Prompt, bounds);

                    // Marker height comes from whatever actually stands there, so it clears the
                    // tower rather than hovering inside it.
                    AddMarker(go.transform, markerHeight, station, stations.Count);

                    // Only the first of a repeated station is wired up. No station symbol appears
                    // twice in the camp today, and quietly wiring the last one would be a bug you
                    // would only find by pressing E at the wrong rack.
                    if (!stations.ContainsKey(fixture.Symbol)) stations.Add(fixture.Symbol, station);
                }
            }

            return stations;
        }

        /// <summary>
        /// The box she cannot walk through, roughly the cell's top face. One size for every fixture
        /// on purpose: a per-prop number is a per-prop thing to get wrong, and the camp is small
        /// enough that "you walk around it" is the whole requirement.
        /// </summary>
        private static void AddBlocker(Transform fixture, ArtBounds bounds)
        {
            var go = new GameObject("Blocker");
            go.transform.SetParent(fixture, false);
            go.transform.localPosition = new Vector3(0f, FootprintCentre(bounds), 0f);

            float width = Mathf.Max(0.6f, bounds.Width * BlockerScale);

            var box = go.AddComponent<BoxCollider2D>();

            // Depth is half the width because the ground plane is a 2:1 isometric diamond.
            box.size = new Vector2(width, width * 0.5f);
        }

        /// <summary>
        /// Where a fixture's base diamond is centred, as a local Y above the sprite's pivot.
        ///
        /// **This is the other half of why the colliders were wrong.** The sprite pivots at its
        /// canvas bottom, and for an isometric prop the lowest drawn pixel is the *bottom vertex* of
        /// its base — not the base's centre. A box centred on the pivot therefore sits half a
        /// diamond too far toward the camera, in front of the thing it is supposed to be inside. For
        /// a 2:1 diamond of width W the centre is W/4 above that vertex, plus whatever transparent
        /// padding the art carries underneath.
        /// </summary>
        private static float FootprintCentre(ArtBounds bounds)
        {
            return bounds.Bottom + bounds.Width * 0.25f;
        }

        /// <summary>
        /// The standing half of a two-part fixture — the mine shaft's tower over its pit.
        ///
        /// Drawn on the Actors layer and sorted from the shared cell, so the player Y-sorts against
        /// it normally and can stand between its legs. It carries no collider of its own: the ground
        /// piece owns the footprint, and two colliders on one fixture would disagree.
        ///
        /// **It stands on the ground piece's footprint CENTRE, not on the cell.** Both sprites
        /// bottom-pivot on their own lowest drawn pixel, and for the pit that pixel is the front
        /// vertex of the hole — so aligning the two by their baselines planted the tower at the near
        /// lip of the shaft rather than over it, which is what read as the shaft being misplaced.
        /// Standing it at the hole's centre is the same `Bottom + Width/4` the colliders use.
        /// </summary>
        private static void AddCompanion(Transform group, string name, string sprite, Vector3 cell,
                                         ArtBounds bounds, ArtBounds ground)
        {
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(PropFolder + sprite + ".png");
            if (art == null)
            {
                Debug.LogWarning("No " + sprite + ".png in " + PropFolder + " — " + name +
                                 " builds without the part that stands up.");
                return;
            }

            float feet = ground.Width * 0.25f - bounds.Bottom;

            NewSprite(name + "_Tower", group, art, cell + new Vector3(0f, feet, 0f), cell.y);
        }

        /// <summary>
        /// The point in the pit she climbs down at, as its own object so it is visible and movable
        /// in the Inspector rather than a number buried in the descent script.
        /// </summary>
        private static Transform AddShaftMouth(Transform fixture, ArtBounds pit)
        {
            var go = new GameObject("Mouth");
            go.transform.SetParent(fixture, false);

            // The centre of the hole, derived from the pit art the same way its collider is, so the
            // two cannot disagree about where the shaft actually is.
            go.transform.localPosition = new Vector3(0f, FootprintCentre(pit), 0f);
            return go.transform;
        }

        /// <summary>
        /// Hangs the floating "you can use this" mark above a fixture.
        ///
        /// Its height is **measured off the fixture's own art** rather than authored per prop: the
        /// fixtures pivot at their feet and range from a 64px campfire to a 160px headframe, so a
        /// shared constant would bury the marker in the headframe's roof and float it a metre above
        /// the rack. Measuring means a new fixture needs no new number.
        ///
        /// It measures the art's **opaque top, not its canvas**, and the difference is not academic:
        /// the first build used the canvas and the shaft's marker floated so far above the headframe
        /// it left the frame entirely. Generated props are centred on a square canvas with whatever
        /// transparent padding that leaves, so the canvas is a poor guide to where the object ends.
        /// </summary>
        private static void AddMarker(Transform fixture, float height, HubStation station, int index)
        {
            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerFolder + "Marker_Idle.png");
            Sprite ready = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerFolder + "Marker_Use.png");

            if (idle == null || ready == null)
            {
                Debug.LogWarning("No marker sprites in " + MarkerFolder + " — run " +
                                 "Deeper/Generate Hub Markers first. " + fixture.name +
                                 " builds without one.");
                return;
            }

            var go = new GameObject("Marker", typeof(SpriteRenderer));
            go.transform.SetParent(fixture, false);

            go.transform.localPosition = new Vector3(0f, height, 0f);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = idle;

            // Overlay, the layer HitVFX and the aim reticle already use, so the mark is never hidden
            // behind the fixture it belongs to or behind the player standing in front of it.
            renderer.sortingLayerName = "Overlay";
            renderer.sortingOrder = 30;

            // Unlit, so the camp's night ambient does not tint the mark blue and dim it along with
            // the world. It is an interface element that happens to live in the world — the one
            // thing here that should look the same at any time of day.
            Material unlit = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (unlit != null) renderer.sharedMaterial = unlit;

            var marker = go.AddComponent<StationMarker>();
            HUDLayout.Wire(marker, "station", station);
            HUDLayout.Wire(marker, "idleSprite", idle);
            HUDLayout.Wire(marker, "readySprite", ready);

            // Staggered, so three markers in one camp do not bob in lockstep and read as one
            // mechanism rather than three separate things.
            HUDLayout.Wire(marker, "bobPhase", index * 0.7f);
        }

        /// <summary>
        /// How tall a fixture actually draws, in world units, measured from its bottom edge to its
        /// topmost opaque pixel.
        ///
        /// Read from the PNG's bytes rather than through a <c>Sprite</c>, for the reason
        /// <see cref="BuildHubArt"/> does the same: it needs no readable import flag and cannot be
        /// thrown off by whatever the importer is doing. The sprites pivot bottom-centre, so a row
        /// <summary>
        /// Where a fixture's art actually is on its canvas, in world units measured up from the
        /// canvas bottom — which is where the sprite's BottomCenter pivot sits.
        /// </summary>
        private struct ArtBounds
        {
            /// <summary>Top of the drawn pixels. How high the fixture stands above its pivot.</summary>
            public float Top;

            /// <summary>
            /// Bottom of the drawn pixels. Almost never zero: generated props are centred on a
            /// square canvas, so each one carries a few pixels of transparent padding underneath.
            /// </summary>
            public float Bottom;

            /// <summary>Widest drawn row — the base of an isometric prop, so its ground footprint.</summary>
            public float Width;
        }

        /// <summary>
        /// Measures a fixture's art once, from the PNG's bytes.
        ///
        /// Read from the file rather than through a <c>Sprite</c>, for the reason
        /// <see cref="BuildHubArt"/> does the same: it needs no readable import flag and cannot be
        /// thrown off by whatever the importer is doing.
        ///
        /// Everything positional about a fixture comes from here — where it is planted, how big its
        /// collider is, how far it can be used from, and how high its marker floats — so those four
        /// cannot drift apart, and a new prop needs no hand-tuned numbers at all.
        /// </summary>
        private static ArtBounds Measure(string path)
        {
            var bounds = new ArtBounds();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!System.IO.File.Exists(path) ||
                    !texture.LoadImage(System.IO.File.ReadAllBytes(path)))
                {
                    return bounds;
                }

                Color32[] pixels = texture.GetPixels32();

                int top = -1;
                int bottom = -1;
                int widest = 0;

                for (int y = 0; y < texture.height; y++)
                {
                    int first = -1;
                    int last = -1;

                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[y * texture.width + x].a <= 8) continue;
                        if (first < 0) first = x;
                        last = x;
                    }

                    if (first < 0) continue;

                    if (bottom < 0) bottom = y;
                    top = y;

                    if (last - first + 1 > widest) widest = last - first + 1;
                }

                if (top < 0) return bounds;

                bounds.Top = (top + 1) / 32f;      // ART_DIRECTION §1's pixels per unit
                bounds.Bottom = bottom / 32f;
                bounds.Width = widest / 32f;
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static HubStation AddStation(Transform fixture, string prompt, ArtBounds bounds)
        {
            var go = new GameObject("Station");
            go.transform.SetParent(fixture, false);
            go.layer = RoomTriggerLayer;

            // Centred on the fixture's footprint, not on its pivot, for the reason the blocker is.
            go.transform.localPosition = new Vector3(0f, FootprintCentre(bounds), 0f);

            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;

            // Wider than the blocker, and derived from the same footprint so the gap between them
            // is constant no matter how big the fixture is. That is now load bearing: with reach
            // fixed at 1.6 while the blocker grew to match the art, the mine shaft's 4.75-unit
            // footprint would have held her further away than she could reach, and pressing E at
            // the shaft would simply never have worked.
            circle.radius = Mathf.Max(0.6f, bounds.Width * BlockerScale * 0.5f) + StationReach;

            HubStation station = go.AddComponent<HubStation>();

            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                HUDLayout.InputAssetPath);

            HUDLayout.Wire(station, "inputActions", actions);
            HUDLayout.Wire(station, "prompt", prompt);

            return station;
        }

        // ---------------------------------------------------------------- shared

        private static Transform NewGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>
        /// A world sprite that sorts by depth. Fixtures pivot at their feet (see
        /// <see cref="BuildHubArt"/>), so the plain cell centre plants them on the tile's top face
        /// with no per-prop offset.
        /// </summary>
        private static GameObject NewSprite(string name, Transform parent, Sprite sprite, Vector3 at)
        {
            return NewSprite(name, parent, sprite, at, at.y);
        }

        /// <summary>
        /// As above, but sorted from <paramref name="sortY"/> rather than from where it is drawn.
        ///
        /// The two come apart whenever a sprite is offset for looks without moving in the world: a
        /// stacked wall course is higher up the same cell, and the shaft's front lip is drawn at its
        /// own place but must sort as though it were nearer the camera.
        /// </summary>
        private static GameObject NewSprite(string name, Transform parent, Sprite sprite, Vector3 at,
                                            float sortY)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Actors";

            // YDepthSort's own formula (step 0.1, x2 to leave the priority slot free), so scenery
            // and actors are ordered by one rule. World y, not local: an actor's order is computed
            // from its world position, and comparing a local number against a world one is how
            // scenery ends up drawn in front of a player standing behind it.
            float worldY = parent != null ? parent.TransformPoint(new Vector3(0f, sortY, 0f)).y : sortY;
            renderer.sortingOrder = -Mathf.RoundToInt(worldY / 0.1f) * 2;

            return go;
        }

        // ---------------------------------------------------------------- interface

        private static void BuildInterface(Dictionary<char, HubStation> stations)
        {
            Canvas canvas = HUDLayout.FindOrCreateCanvas();
            RunPause pause = HUDLayout.EnsureRunPause(canvas);

            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("No RunConfig at " + ConfigPath + " — run Deeper/Build Run Config " +
                                 "first, or the rack will open empty.");
            }

            HubPromptHUD prompt = BuildPrompt(canvas, stations);
            BuildShardCounter(canvas);
            BuildWeaponPanel(canvas, stations, config, pause);

            // Built last so it is the last sibling, and sibling order is draw order — a fade that
            // anything can draw over is not a fade.
            Image fade = BuildFade(canvas);

            WireDescent(stations, config, prompt, fade);
            WireShrine(stations, prompt);
            WireCodex(stations, prompt);
        }

        /// <summary>
        /// The full-screen black the descent fades through.
        ///
        /// Starts fully transparent and disabled. <c>HubDescent</c> switches it on, and it is never
        /// switched off again because the scene load is what removes it — a fade that cleared itself
        /// would flash the camp back for a frame before the run appeared.
        /// </summary>
        private static Image BuildFade(Canvas canvas)
        {
            RectTransform rect = HUDLayout.NewRect("DescentFade", canvas.transform);
            HUDLayout.Stretch(rect);

            Image image = HUDLayout.AddImage(rect, null, new Color(0f, 0f, 0f, 0f));

            // Off until the descent starts. A transparent Image still costs a full-screen draw and,
            // more importantly, would eat clicks aimed at the weapon rack's cards underneath.
            image.enabled = false;

            return image;
        }

        /// <summary>
        /// The Shard total, top-right — the first item on GDD §UI's Hub Screen list.
        ///
        /// Built here and not in <c>BuildRunHUD</c> because it is **Hub-only**: ART_DIRECTION §5's
        /// in-run HUD has no Shard row, and rightly — they are awarded once at run end and spent
        /// between runs, so a counter during a descent would be a number that cannot change, watched
        /// by a player who cannot spend it.
        ///
        /// Laid out like the run HUD's dash and weapon slots, and for a reason that is a hard
        /// coupling rather than a style choice: <c>HUDFrameArt</c> draws <c>HUD_SlotSquare</c> with a
        /// 4px border so 40 − 8 lands on 32, which is the authored 64px icon at the canvas's 2×. The
        /// icon is centred at 32 rather than stretched, because a non-integer resample of
        /// point-filtered art loses its grid.
        /// </summary>
        private static void BuildShardCounter(Canvas canvas)
        {
            var bank = AssetDatabase.LoadAssetAtPath<Deeper.Meta.ShardBank>(ShardBankPath);
            if (bank == null)
            {
                Debug.LogWarning("No ShardBank at " + ShardBankPath + " — run Deeper/Build Shard " +
                                 "Bank first, or the counter will read zero and never move.");
            }

            RectTransform group = HUDLayout.NewRect("ShardCounter", canvas.transform);

            // Authored units are half their on-screen size (PixelPerfectHUDScale, 540 reference).
            HUDLayout.AnchorTopRight(group, new Vector2(104f, 40f), new Vector2(-10f, -10f));

            RectTransform slot = HUDLayout.NewRect("Slot", group);
            HUDLayout.Centre(slot, new Vector2(40f, 40f), new Vector2(-32f, 0f));
            HUDLayout.AddImage(slot, HUDLayout.Load("HUD_SlotSquare"), Color.white);

            // A dark socket inside the slot's hole, so the world does not show through the icon's
            // transparent pixels. Inset matches the frame's own border.
            RectTransform socket = HUDLayout.NewRect("Socket", slot);
            HUDLayout.Inset(socket, Vector4.one * SlotBorder);
            HUDLayout.AddImage(socket, HUDLayout.Load("HUD_Fill"), new Color(0.09f, 0.08f, 0.10f, 0.85f));

            RectTransform glyph = HUDLayout.NewRect("Glyph", slot);
            HUDLayout.Centre(glyph, new Vector2(32f, 32f));
            Image icon = HUDLayout.AddImage(glyph, HUDLayout.Load("HUD_IconShard"), Color.white);
            icon.preserveAspect = true;

            RectTransform amount = HUDLayout.NewRect("Amount", group);
            HUDLayout.Centre(amount, new Vector2(56f, 16f), new Vector2(24f, 0f));
            Text label = HUDLayout.AddTextIn(amount, "0", HUDLayout.TitleText, TextAnchor.MiddleRight);

            var counter = canvas.gameObject.AddComponent<ShardCounterHUD>();
            HUDLayout.Wire(counter, "bank", bank);
            HUDLayout.Wire(counter, "label", label);
        }

        private static HubPromptHUD BuildPrompt(Canvas canvas, Dictionary<char, HubStation> stations)
        {
            RectTransform group = HUDLayout.NewRect("HubPrompt", canvas.transform);

            // Authored units are half their on-screen size — PixelPerfectHUDScale scales the canvas
            // by a whole number from a 540 reference, so this 200x16 box is 400x32 at 1080p.
            HUDLayout.AnchorBottomCentre(group, new Vector2(200f, 16f), new Vector2(0f, 28f));

            Text label = HUDLayout.AddText(group, "", HUDLayout.BodyText, TextAnchor.MiddleCenter);

            var hud = canvas.gameObject.AddComponent<HubPromptHUD>();

            var all = new List<Object>();
            foreach (KeyValuePair<char, HubStation> pair in stations) all.Add(pair.Value);

            HUDLayout.WireArray(hud, "stations", all.ToArray());
            HUDLayout.Wire(hud, "group", group.gameObject);
            HUDLayout.Wire(hud, "label", label);

            return hud;
        }

        /// <summary>
        /// The rack's screen: a dimmed scrim, a header and three cards.
        ///
        /// Left inactive at the end, because it takes a <c>RunPause</c> hold when it opens — a panel
        /// that built itself active would freeze the camp the moment the scene loaded.
        /// </summary>
        private static void BuildWeaponPanel(Canvas canvas, Dictionary<char, HubStation> stations,
                                             RunConfig config, RunPause pause)
        {
            RectTransform panel = HUDLayout.NewRect("WeaponSelect", canvas.transform);
            HUDLayout.Stretch(panel);

            // Clickable, so the scrim eats clicks that miss a card rather than letting them fall
            // through to the camp behind it.
            HUDLayout.AddImage(panel, null, new Color(0.04f, 0.04f, 0.07f, 0.86f), true);

            RectTransform headerRect = HUDLayout.NewRect("Header", panel);
            HUDLayout.AnchorTopCentre(headerRect, new Vector2(240f, 20f), new Vector2(0f, 46f));
            Text header = HUDLayout.AddTextIn(headerRect, "CHOOSE YOUR WEAPON", HUDLayout.TitleText,
                                              TextAnchor.MiddleCenter);

            var cards = new List<Object>();
            const float cardWidth = 92f;
            const float cardHeight = 132f;
            const float gap = 12f;

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * (cardWidth + gap);
                cards.Add(BuildWeaponCard(panel, i, new Vector2(cardWidth, cardHeight), x));
            }

            RectTransform closeRect = HUDLayout.NewRect("Close", panel);
            HUDLayout.AnchorBottomCentre(closeRect, new Vector2(96f, 18f), new Vector2(0f, 34f));
            HUDLayout.AddImage(closeRect, null, new Color(0.18f, 0.18f, 0.24f, 1f), true);
            HUDLayout.AddText(closeRect, "BACK", HUDLayout.BodyText, TextAnchor.MiddleCenter);
            Button close = closeRect.gameObject.AddComponent<Button>();

            var select = canvas.gameObject.AddComponent<WeaponSelectPanel>();

            HubStation rack;
            if (stations.TryGetValue('W', out rack)) HUDLayout.Wire(select, "station", rack);

            HUDLayout.Wire(select, "panel", panel.gameObject);
            HUDLayout.WireArray(select, "cards", cards.ToArray());
            HUDLayout.Wire(select, "headerLabel", header);
            HUDLayout.Wire(select, "closeButton", close);
            HUDLayout.Wire(select, "config", config);
            HUDLayout.Wire(select, "loadout", HUDLayout.PlayerPart<Deeper.Character.RunLoadout>());
            HUDLayout.Wire(select, "pause", pause);

            panel.gameObject.SetActive(false);
        }

        private static WeaponCard BuildWeaponCard(RectTransform parent, int index, Vector2 size, float x)
        {
            RectTransform rect = HUDLayout.NewRect("Card" + index, parent);
            HUDLayout.Centre(rect, size, new Vector2(x, 0f));

            Image frame = HUDLayout.AddImage(rect, null, Color.white, true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            RectTransform body = HUDLayout.NewRect("Body", rect);
            HUDLayout.Stretch(body);
            HUDLayout.Inset(body, new Vector4(2f, 2f, 2f, 2f));
            HUDLayout.AddImage(body, null, new Color(0.10f, 0.10f, 0.14f, 1f));

            RectTransform iconRect = HUDLayout.NewRect("Icon", rect);
            HUDLayout.Centre(iconRect, new Vector2(64f, 64f), new Vector2(0f, 26f));
            Image icon = HUDLayout.AddImage(iconRect, null, Color.white);

            RectTransform nameRect = HUDLayout.NewRect("Name", rect);
            HUDLayout.Centre(nameRect, new Vector2(size.x - 8f, 12f), new Vector2(0f, -16f));
            Text nameLabel = HUDLayout.AddTextIn(nameRect, "", HUDLayout.BodyText, TextAnchor.MiddleCenter);

            RectTransform bodyRect = HUDLayout.NewRect("Description", rect);
            HUDLayout.Centre(bodyRect, new Vector2(size.x - 10f, 44f), new Vector2(0f, -42f));
            Text bodyLabel = HUDLayout.AddTextIn(bodyRect, "", HUDLayout.BodyText, TextAnchor.UpperCenter, true);

            var card = rect.gameObject.AddComponent<WeaponCard>();
            HUDLayout.Wire(card, "frame", frame);
            HUDLayout.Wire(card, "icon", icon);
            HUDLayout.Wire(card, "nameLabel", nameLabel);
            HUDLayout.Wire(card, "bodyLabel", bodyLabel);
            HUDLayout.Wire(card, "button", button);

            return card;
        }

        private static void WireDescent(Dictionary<char, HubStation> stations, RunConfig config,
                                        HubPromptHUD prompt, Image fade)
        {
            HubStation shaft;
            if (!stations.TryGetValue('M', out shaft)) return;

            var descent = shaft.gameObject.AddComponent<HubDescent>();
            HUDLayout.Wire(descent, "station", shaft);
            HUDLayout.Wire(descent, "config", config);
            HUDLayout.Wire(descent, "prompt", prompt);
            HUDLayout.Wire(descent, "fade", fade);

            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                HUDLayout.InputAssetPath);
            HUDLayout.Wire(descent, "inputActions", actions);

            HUDLayout.Wire(descent, "shaftMouth",
                           AddShaftMouth(shaft.transform.parent,
                                         Measure(PropFolder + "Prop_ShaftPit.png")));
        }

        /// <summary>
        /// The Codex is CORE_SYSTEMS §15's Memory Fragment archive and is not built — there are no
        /// fragments to bank yet. Same treatment as the shrine: placed, and honest about it.
        /// </summary>
        private static void WireCodex(Dictionary<char, HubStation> stations, HubPromptHUD prompt)
        {
            HubStation codex;
            if (!stations.TryGetValue('C', out codex)) return;

            var notice = codex.gameObject.AddComponent<HubNotice>();
            HUDLayout.Wire(notice, "station", codex);
            HUDLayout.Wire(notice, "prompt", prompt);
            HUDLayout.Wire(notice, "message", "NO MEMORIES RECOVERED YET.");
        }

        /// <summary>
        /// The shrine is Milestone 6's Hub Stat System and is not built. It is placed anyway, with a
        /// notice on it: a fixture the prompt says you can use, that then does nothing, reads as
        /// broken rather than as unfinished.
        /// </summary>
        private static void WireShrine(Dictionary<char, HubStation> stations, HubPromptHUD prompt)
        {
            HubStation shrine;
            if (!stations.TryGetValue('S', out shrine)) return;

            var notice = shrine.gameObject.AddComponent<HubNotice>();
            HUDLayout.Wire(notice, "station", shrine);
            HUDLayout.Wire(notice, "prompt", prompt);
            HUDLayout.Wire(notice, "message", "THE SHRINE IS COLD. NO SHARDS TO SPEND YET.");
        }

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene entry in scenes)
            {
                if (entry.path == ScenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
