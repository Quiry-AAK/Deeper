using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Imports the isometric tiles under <c>Art/Environment/&lt;biome&gt;/Iso/</c> and writes a
    /// <see cref="Tile"/> asset for each.
    ///
    /// Separate from <see cref="BuildRoomTiles"/> because that one enforces a 32×32 square — the
    /// guard that stops a Wang sheet becoming one enormous tile, and worth keeping. An isometric
    /// diamond is 64×64 on its canvas and is a different shape of thing entirely.
    /// </summary>
    public static class BuildIsometricArt
    {
        private const string ArtRoot = "Assets/_Main/Art/Environment";
        private const string IsoFolder = "Iso";
        private const string TileRoot = "Assets/_Main/Data/Tiles";

        [MenuItem("Deeper/Generate Isometric Tiles")]
        public static void Generate()
        {
            int made = 0;

            foreach (string biomeDir in Directory.GetDirectories(ArtRoot))
            {
                string isoDir = Path.Combine(biomeDir, IsoFolder);
                if (!Directory.Exists(isoDir)) continue;

                string biome = Path.GetFileName(biomeDir);
                string biomeTiles = TileRoot + "/" + biome;
                if (!AssetDatabase.IsValidFolder(biomeTiles)) AssetDatabase.CreateFolder(TileRoot, biome);

                // Isometric tiles live in their OWN folder and carry their own prefix. Both
                // generators name a tile after its PNG, and the isometric art reuses the flat art's
                // names (Floor_Gravel and so on) -- so writing them side by side silently
                // overwrote the flat Tile_Floor_Gravel with an isometric sprite. Nothing errored;
                // the flat tiles just quietly became the wrong shape.
                string outDir = biomeTiles + "/" + IsoFolder;
                if (!AssetDatabase.IsValidFolder(outDir)) AssetDatabase.CreateFolder(biomeTiles, IsoFolder);

                foreach (string file in Directory.GetFiles(isoDir, "*.png"))
                {
                    string path = ToAssetPath(file);
                    ApplyImportSettings(path);

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null) continue;

                    string name = Path.GetFileNameWithoutExtension(path);   // Floor_Gravel
                    string tilePath = outDir + "/Tile_Iso_" + name + ".asset";

                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    bool isNew = tile == null;
                    if (isNew) tile = ScriptableObject.CreateInstance<Tile>();

                    tile.sprite = sprite;

                    // None even for the wall block. In an isometric room the collision shape is the
                    // logical cell, not the drawn diamond, and a Grid collider on a 64px sprite in a
                    // 2×1 cell would extend well past the cell it belongs to.
                    tile.colliderType = Tile.ColliderType.None;

                    if (isNew) AssetDatabase.CreateAsset(tile, tilePath);
                    else EditorUtility.SetDirty(tile);
                    made++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generate Isometric Tiles: " + made + " tile(s).");
        }

        private static void ApplyImportSettings(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;              // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            // Centre pivot, because the diamond's widest row sits on the canvas's vertical centre —
            // measured, not assumed. That makes the sprite's centre the cell's centre, so the Grid's
            // own CellToWorld needs no correction.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        private static string ToAssetPath(string file)
        {
            string path = file.Replace('\\', '/');
            int index = path.IndexOf("Assets/");
            return index > 0 ? path.Substring(index) : path;
        }
    }
}
