using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Turns every PNG under <c>Art/Environment/&lt;biome&gt;/</c> into an imported sprite and a
    /// matching <see cref="Tile"/> asset in <c>Data/Tiles/&lt;biome&gt;/</c>.
    ///
    /// It exists because <c>Data/Tiles/</c> previously held two hand-made tile assets with **no
    /// generator at all** — so the set could not be reproduced, reviewed or extended without
    /// clicking, which is the same argument `RoomLayout` and `BuildRoomPrefab` already make one
    /// level down.
    ///
    /// It also fixes the trap every new PNG in this project walks into: Unity's auto-import guesses
    /// 100 PPU with bilinear filtering, and the rejected `UpperCaves_Wang16.png` is the standing
    /// example of what that leaves behind.
    ///
    /// **Collider type is decided by the file name**, and that is deliberate rather than clever:
    /// anything named `Wall_*` collides, everything else does not. A floor or decor tile that ships
    /// with a collider can trap an enemy — `EnemyChase` has no pathfinding — in a room that then
    /// never unlocks, so the safe answer is the default and the dangerous one has to be asked for.
    /// </summary>
    public static class BuildRoomTiles
    {
        private const string ArtRoot = "Assets/_Main/Art/Environment";
        private const string TileRoot = "Assets/_Main/Data/Tiles";
        private const string WallPrefix = "Wall_";
        private const int TileSize = 32;   // ART_DIRECTION §1

        [MenuItem("Deeper/Generate Room Tiles")]
        public static void Generate()
        {
            if (!Directory.Exists(ArtRoot))
            {
                Debug.LogError("No " + ArtRoot + " folder.");
                return;
            }

            int made = 0;

            foreach (string biomeDir in Directory.GetDirectories(ArtRoot))
            {
                string biome = Path.GetFileName(biomeDir);
                string outDir = TileRoot + "/" + biome;
                if (!AssetDatabase.IsValidFolder(outDir)) AssetDatabase.CreateFolder(TileRoot, biome);

                foreach (string file in Directory.GetFiles(biomeDir, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    int index = assetPath.IndexOf("Assets/");
                    if (index > 0) assetPath = assetPath.Substring(index);

                    ApplyImportSettings(assetPath);

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null)
                    {
                        Debug.LogWarning("No sprite at " + assetPath + " — is it sliced Multiple?");
                        continue;
                    }

                    // ART_DIRECTION §1 locks a tile at 32x32, so anything else in this folder is
                    // not a tile — the recoloured 16-tile Wang sheets live alongside these at
                    // 128x128 and were briefly turned into single enormous "tiles" before this
                    // guard existed. Checking the size is more durable than checking the name.
                    if (sprite.rect.width != TileSize || sprite.rect.height != TileSize)
                    {
                        Debug.Log("Skipping " + Path.GetFileName(assetPath) + ": " +
                                  sprite.rect.width + "x" + sprite.rect.height + ", not a " +
                                  TileSize + "px tile.");
                        continue;
                    }

                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    string tilePath = outDir + "/Tile_" + name + ".asset";

                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    bool isNew = tile == null;
                    if (isNew) tile = ScriptableObject.CreateInstance<Tile>();

                    tile.sprite = sprite;
                    tile.colliderType = name.StartsWith(WallPrefix)
                        ? Tile.ColliderType.Grid
                        : Tile.ColliderType.None;

                    if (isNew) AssetDatabase.CreateAsset(tile, tilePath);
                    else EditorUtility.SetDirty(tile);

                    made++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generate Room Tiles: " + made + " tile asset(s) under " + TileRoot + ".");
        }

        /// <summary>ART_DIRECTION §1's contract. Copied from PlaceholderRoomArt so both agree.</summary>
        private static void ApplyImportSettings(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;

            bool alreadyRight =
                importer.textureType == TextureImporterType.Sprite &&
                Mathf.Approximately(importer.spritePixelsPerUnit, 32f) &&
                importer.filterMode == FilterMode.Point &&
                importer.textureCompression == TextureImporterCompression.Uncompressed;

            if (alreadyRight) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            importer.SaveAndReimport();
        }
    }
}
