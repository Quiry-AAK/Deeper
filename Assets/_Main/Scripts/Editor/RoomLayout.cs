using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Turns a room's authored ASCII map into the two things derived from it: the painted tilemaps
    /// and the world positions of its markers.
    ///
    /// **This is not a room generator.** LEVEL_DESIGN §1 is explicit that rooms are hand-built, not
    /// procedural — the maps in `Layout_UpperCaves_*.cs` *are* the hand-built part, and a human
    /// editing those characters is how a room changes. What this removes is stamping hundreds of
    /// tiles by hand every time a post moves two squares, which happens on every tuning pass.
    ///
    /// Keeping the marker positions derived from the same string is what stops a map and its prefab
    /// drifting apart: move a `0` in the map, repaint, and the spawn point follows.
    ///
    /// The map itself lives in a per-room file rather than here, because the second room needed the
    /// same painting and marker code against a different map. Copying this file per room is what
    /// would let two rooms' painting rules diverge — the exact drift the tool exists to prevent.
    ///
    /// Committed for the reason <see cref="PlaceholderEnemySheets"/> gives — a one-shot generator is
    /// a tool you pay for twice.
    /// </summary>
    public static class RoomLayout
    {
        /// <summary>
        /// The legend every room's map is written in. Shared deliberately: a per-room legend is a
        /// per-room painting rule, and then `O` is a wall in one room and cover in another.
        ///
        /// `#` wall   `.` floor   `O` interior post (wall)   `D` door gap (floor; a RoomDoor fills it)
        /// `=` entry trigger footprint (floor)   `c` reserved cracked-tile zone (floor, nothing built)
        /// `P` player start   `0`-`9` spawn points
        /// `V` key-gated vault door gap (floor; a RoomDoor plus a VaultDoor lock fill it)
        /// `T` the Secret Vault's payout pedestal (floor)
        /// </summary>
        public const char Wall = '#';
        public const char Post = 'O';
        public const char PlayerStart = 'P';

        private const string FloorTilePath = "Assets/_Main/Data/Tiles/Tile_Floor.asset";
        private const string WallTilePath = "Assets/_Main/Data/Tiles/Tile_Wall.asset";

        /// <summary>
        /// Paints <paramref name="map"/> onto the Floor and Walls tilemaps under
        /// <paramref name="room"/>, and reports what it did. Returns false with a logged error when
        /// the room is not shaped the way every room prefab is, so a caller fails loudly.
        /// </summary>
        public static bool PaintInto(GameObject room, string[] map)
        {
            if (room == null)
            {
                Debug.LogError("Select the room object (the one with the Grid) first.");
                return false;
            }

            Tilemap floor = FindChildTilemap(room.transform, "Floor");
            Tilemap walls = FindChildTilemap(room.transform, "Walls");

            if (floor == null || walls == null)
            {
                Debug.LogError(room.name + " needs child tilemaps named Floor and Walls.", room);
                return false;
            }

            Paint(floor, walls, map);
            EditorUtility.SetDirty(floor);
            EditorUtility.SetDirty(walls);

            Debug.Log("Painted " + room.name + ": " + walls.GetUsedTilesCount() + " wall tiles, " +
                      floor.GetUsedTilesCount() + " floor tiles.", room);
            return true;
        }

        /// <summary>
        /// Stamps the map onto the two tilemaps. Floor is painted across the whole footprint and
        /// walls drawn over the top of it, which is how the existing test room is built — a wall
        /// with a hole in the floor behind it shows through as void the moment anything is
        /// destructible.
        /// </summary>
        public static void Paint(Tilemap floor, Tilemap walls, string[] map)
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FloorTilePath);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);

            floor.ClearAllTiles();
            walls.ClearAllTiles();

            int height = Height(map);
            int width = Width(map);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);

                    floor.SetTile(cell, floorTile);
                    if (IsWall(At(map, x, y))) walls.SetTile(cell, wallTile);
                }
            }

            floor.CompressBounds();
            walls.CompressBounds();
        }

        /// <summary>
        /// World position of the single cell carrying <paramref name="symbol"/>, at the cell's
        /// centre. Returns false when the map has no such character, so a caller wiring a marker
        /// fails loudly instead of silently placing it at the origin.
        /// </summary>
        public static bool TryGetMarker(string[] map, char symbol, out Vector3 position)
        {
            List<Vector3> found = Markers(map, symbol);

            position = found.Count > 0 ? found[0] : Vector3.zero;
            return found.Count > 0;
        }

        /// <summary>
        /// A room's isometric cell footprint in world units: a 2:1 diamond, 64x32 px at 32 PPU.
        ///
        /// This is the classic pixel-art isometric ratio because it is exact — every diamond edge is
        /// a clean two-across-one-down staircase. A true 30 degree isometric needs anti-aliased
        /// diagonals, which do not exist at this resolution.
        /// </summary>
        public static readonly Vector3 CellSize = new Vector3(2f, 1f, 1f);

        /// <summary>
        /// The world-space centre of a cell's diamond.
        ///
        /// Every position in a room passes through here — doors, the entry band, spawn markers, the
        /// player start — which is what makes the projection a single decision rather than a
        /// conversion sprinkled through the builder. It matches Unity's own isometric
        /// <c>Grid.CellToWorld</c> plus half a cell, so a room's colliders land exactly on the
        /// diamonds its tilemap draws.
        /// </summary>
        public static Vector3 CellCentre(int x, int y)
        {
            return new Vector3(
                (x - y) * CellSize.x * 0.5f,
                (x + y) * CellSize.y * 0.5f + CellSize.y * 0.5f,
                0f);
        }

        /// <summary>Every cell carrying <paramref name="symbol"/>, bottom-up then left-to-right.</summary>
        public static List<Vector3> Markers(string[] map, char symbol)
        {
            var found = new List<Vector3>();

            int height = Height(map);
            int width = Width(map);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (At(map, x, y) == symbol) found.Add(CellCentre(x, y));
                }
            }

            return found;
        }

        /// <summary>The cell coordinates carrying <paramref name="symbol"/>, unprojected.</summary>
        public static List<Vector2Int> MarkerCells(string[] map, char symbol)
        {
            var found = new List<Vector2Int>();

            for (int y = 0; y < Height(map); y++)
            {
                for (int x = 0; x < Width(map); x++)
                {
                    if (At(map, x, y) == symbol) found.Add(new Vector2Int(x, y));
                }
            }

            return found;
        }

        /// <summary>Map character at a cell, with y measured up from the room's bottom row.</summary>
        public static char At(string[] map, int x, int y)
        {
            return map[Height(map) - 1 - y][x];
        }

        /// <summary>Row 0 of a map string is the room's TOP row, so it reads the way it looks.</summary>
        public static int Height(string[] map)
        {
            return map.Length;
        }

        public static int Width(string[] map)
        {
            return map.Length > 0 ? map[0].Length : 0;
        }

        /// <summary>
        /// Every row the same length, and no character outside the legend. Called by each layout's
        /// menu item before it paints: a map is edited by hand, and a row one character short
        /// silently shifts everything on it by one tile rather than throwing.
        /// </summary>
        public static bool Validate(string[] map, string name)
        {
            return Validate(map, name, null);
        }

        /// <summary>
        /// As above, plus <paramref name="extraLegend"/> — characters legal in this map only.
        ///
        /// The Hub's camp is drawn on the same grid by the same projection, but its cells mean
        /// different things (a weapon rack, a shaft, a patch of grass) and none of them belongs in a
        /// Combat Room. Extending the shared legend with them instead would give every room in the
        /// game a silently-legal `M`, which is the drift the legend's own comment warns about:
        /// share the *rules*, not the vocabulary of one map.
        /// </summary>
        public static bool Validate(string[] map, string name, string extraLegend)
        {
            if (map == null || map.Length == 0)
            {
                Debug.LogError(name + ": map is empty.");
                return false;
            }

            int width = map[0].Length;
            string legend = "#.OD=cPVT0123456789" + (extraLegend ?? string.Empty);

            for (int row = 0; row < map.Length; row++)
            {
                if (map[row].Length != width)
                {
                    Debug.LogError(name + ": row " + row + " is " + map[row].Length +
                                   " characters, expected " + width + ".");
                    return false;
                }

                for (int column = 0; column < width; column++)
                {
                    char symbol = map[row][column];
                    if (legend.IndexOf(symbol) >= 0) continue;

                    Debug.LogError(name + ": unknown map character '" + symbol + "' at row " + row +
                                   ", column " + column + ".");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reports interior posts that could trap an enemy, and returns whether the map is clean.
        ///
        /// **This is the one authoring mistake that ends a run silently.** `EnemyChase` is
        /// straight-line steering with no pathfinding, so an enemy that walks into a concave pocket
        /// stays in it — and a Combat Room only unlocks when every enemy is dead, so the player is
        /// sealed in a room with an enemy she cannot reach and no door. Nothing errors, nothing
        /// warns, and the only symptom is a fight that never ends.
        ///
        /// A single isolated post is convex and can never form a pocket. The rule is therefore:
        /// no post touching a wall or another post (8-way), two clear cells to any solid on each
        /// axis so steering never has to thread a one-wide lane, and three cells between posts.
        ///
        /// Checked rather than trusted, because it is invisible in the map string: two posts that
        /// look comfortably apart on adjacent rows are one cell apart on the grid.
        /// </summary>
        public static bool ValidatePosts(string[] map, string name)
        {
            int height = Height(map);
            int width = Width(map);
            var posts = new List<Vector2Int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (At(map, x, y) == Post) posts.Add(new Vector2Int(x, y));
                }
            }

            bool clean = true;

            foreach (Vector2Int post in posts)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        // Two steps, not one: a post one clear cell off a wall leaves a lane an
                        // enemy steering straight at the player can wedge itself into.
                        for (int step = 1; step <= 2; step++)
                        {
                            // Diagonals only need to be clear at one step; a diagonal gap of two is
                            // a corner, not a pocket.
                            if (step == 2 && dx != 0 && dy != 0) continue;

                            if (!IsSolid(map, post.x + dx * step, post.y + dy * step)) continue;

                            Debug.LogError(name + ": the post at (" + post.x + ", " + post.y +
                                           ") is " + step + " cell(s) from solid ground. An enemy " +
                                           "can be trapped there, and a Combat Room with a trapped " +
                                           "enemy never unlocks.");
                            clean = false;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < posts.Count; i++)
            {
                for (int j = i + 1; j < posts.Count; j++)
                {
                    int gap = Mathf.Max(Mathf.Abs(posts[i].x - posts[j].x),
                                        Mathf.Abs(posts[i].y - posts[j].y));
                    if (gap >= 3) continue;

                    Debug.LogError(name + ": the posts at (" + posts[i].x + ", " + posts[i].y +
                                   ") and (" + posts[j].x + ", " + posts[j].y + ") are " + gap +
                                   " cell(s) apart. Two posts that close read as one concave shape.");
                    clean = false;
                }
            }

            return clean;
        }

        /// <summary>Whether a cell blocks movement. Out of bounds counts as solid: it is outside the room.</summary>
        private static bool IsSolid(string[] map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width(map) || y >= Height(map)) return true;

            return IsWall(At(map, x, y));
        }

        private static bool IsWall(char symbol)
        {
            // A door gap is floor: the gap is the hole, and a RoomDoor object fills it when shut.
            // Painting a wall tile there would leave the room permanently sealed with a door drawn
            // on top of it.
            return symbol == Wall || symbol == Post;
        }

        private static Tilemap FindChildTilemap(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Tilemap>() : null;
        }
    }
}
