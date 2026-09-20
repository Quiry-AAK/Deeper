using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Deeper.Rooms;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds one <see cref="RoomTheme"/> per floor/wall pair found in <c>Data/Tiles/&lt;biome&gt;/</c>.
    ///
    /// Data-driven from the art on disk rather than hand-assembled, for the reason every other
    /// builder in this folder exists: a set of assets wired by dragging is one nobody can reproduce,
    /// review or diff. Drop a new <c>Floor_Mossy.png</c> + <c>Wall_Mossy.png</c> into
    /// <c>Art/Environment/UpperCaves/</c>, run Generate Room Tiles then this, and a
    /// <c>Theme_UpperCaves_Mossy</c> exists.
    ///
    /// **Each theme keeps its own floor and wall.** Mixing every biome tile into every theme would
    /// average them into one look — the opposite of the point, which is that a repeated layout reads
    /// as a different room. Variety *within* a room comes from the per-cell quarter-turn and the
    /// decals, which are shared across a biome because debris is debris.
    /// </summary>
    public static class BuildRoomThemes
    {
        private const string TileRoot = "Assets/_Main/Data/Tiles";
        private const string ThemeRoot = "Assets/_Main/Data/Themes";

        /// <summary>
        /// Ambient light per biome — ART_DIRECTION §2's colour temperature as a mood wash, which is
        /// what style-guide §8 reserves Light2D for. Deliberately gentle: these must not tip into
        /// the reserved hazard accents (orange-red, pale cyan-white, bright yellow-orange), which
        /// belong to danger telegraphs alone.
        /// </summary>
        private static readonly Dictionary<string, Color> Ambient = new Dictionary<string, Color>
        {
            { "UpperCaves", new Color(1.00f, 0.98f, 0.96f) },
            { "FloodedTunnels", new Color(0.80f, 0.90f, 1.00f) },
            { "MoltenDepths", new Color(1.00f, 0.86f, 0.80f) },
        };

        [MenuItem("Deeper/Generate Room Themes")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(ThemeRoot))
            {
                AssetDatabase.CreateFolder("Assets/_Main/Data", "Themes");
            }

            int made = 0;

            foreach (string biomeDir in Directory.GetDirectories(TileRoot))
            {
                string biome = Path.GetFileName(biomeDir);

                var floors = new Dictionary<string, TileBase>();
                var walls = new Dictionary<string, TileBase>();
                var decals = new List<TileBase>();

                foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { biomeDir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    if (tile == null) continue;

                    string name = Path.GetFileNameWithoutExtension(path);   // Tile_Floor_Gravel
                    if (name.StartsWith("Tile_Floor_")) floors[name.Substring("Tile_Floor_".Length)] = tile;
                    else if (name.StartsWith("Tile_Wall_")) walls[name.Substring("Tile_Wall_".Length)] = tile;
                    else if (name.StartsWith("Tile_Decal_")) decals.Add(tile);
                }

                foreach (KeyValuePair<string, TileBase> floor in floors)
                {
                    TileBase wall;
                    if (!walls.TryGetValue(floor.Key, out wall))
                    {
                        // A floor with no matching wall borrows any wall from its biome rather than
                        // shipping a theme that paints nothing on the wall ring.
                        foreach (KeyValuePair<string, TileBase> any in walls) { wall = any.Value; break; }
                    }

                    if (wall == null)
                    {
                        Debug.LogWarning(biome + "/" + floor.Key + " has no wall tile; skipping.");
                        continue;
                    }

                    Write(biome, floor.Key, floor.Value, wall, decals);
                    made++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generate Room Themes: " + made + " theme(s) under " + ThemeRoot + ".");
        }

        private static void Write(string biome, string variant, TileBase floor, TileBase wall,
                                  List<TileBase> decals)
        {
            string path = ThemeRoot + "/Theme_" + biome + "_" + variant + ".asset";

            var theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(path);
            bool isNew = theme == null;
            if (isNew) theme = ScriptableObject.CreateInstance<RoomTheme>();

            var so = new SerializedObject(theme);
            SetArray(so.FindProperty("floorTiles"), new[] { floor });
            SetArray(so.FindProperty("wallTiles"), new[] { wall });
            SetArray(so.FindProperty("decorTiles"), decals.ToArray());
            so.FindProperty("rotateFloorTiles").boolValue = true;
            so.FindProperty("decorDensity").floatValue = 0.12f;

            Color ambient;
            so.FindProperty("ambientLight").colorValue =
                Ambient.TryGetValue(biome, out ambient) ? ambient : Color.white;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew) AssetDatabase.CreateAsset(theme, path);
            else EditorUtility.SetDirty(theme);
        }

        private static void SetArray(SerializedProperty property, IList<TileBase> values)
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
