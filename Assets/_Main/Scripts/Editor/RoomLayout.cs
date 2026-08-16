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
                    if (At(map, x, y) == symbol) found.Add(new Vector3(x + 0.5f, y + 0.5f, 0f));
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
            if (map == null || map.Length == 0)
            {
                Debug.LogError(name + ": map is empty.");
                return false;
            }

            int width = map[0].Length;
            const string legend = "#.OD=cP0123456789";

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
