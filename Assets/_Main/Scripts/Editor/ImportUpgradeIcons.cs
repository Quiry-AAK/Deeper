using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Applies the sprite import contract to every generated upgrade icon
    /// (<c>Art/UI/Icons/</c>) — ART_DIRECTION §1, and the same settings
    /// <see cref="HUDFrameArt"/> writes onto the drawn chrome next door.
    ///
    /// A menu item rather than an <c>AssetPostprocessor</c>: a postprocessor would apply itself
    /// invisibly to anything dropped in the folder, and the rest of this project's art pipeline is
    /// menu items whose effect you can see happening. Idempotent — it only reimports files whose
    /// settings are actually wrong, so re-running it on a clean folder does nothing and says so.
    ///
    /// <b>Point filter and uncompressed are the load-bearing pair.</b> Bilinear filtering blurs a
    /// 128px icon the moment it is drawn at anything but its authored size, and DXT compression puts
    /// colours in the file that are not in the forced palette — which would quietly undo the whole
    /// reason the palette is forced.
    /// </summary>
    public static class ImportUpgradeIcons
    {
        private const string Folder = "Assets/_Main/Art/UI/Icons";

        [MenuItem("Deeper/Import Upgrade Icons")]
        public static void Import()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning("No " + Folder + " — nothing to import.");
                return;
            }

            string[] files = Directory.GetFiles(Folder, "*.png", SearchOption.TopDirectoryOnly);
            int changed = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (!NeedsWork(importer)) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                // Alignment lives in the sprite settings, not on the importer field alone — set both
                // or the pivot silently stays at whatever the last import left.
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log(changed == 0
                ? "Upgrade icons: all " + files.Length + " already on the import contract."
                : "Upgrade icons: reimported " + changed + " of " + files.Length + " onto the " +
                  "import contract (Sprite/Single, 32 PPU, Point, uncompressed, Center pivot).");
        }

        private static bool NeedsWork(TextureImporter importer)
        {
            return importer.textureType != TextureImporterType.Sprite
                   || importer.spriteImportMode != SpriteImportMode.Single
                   || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f)
                   || importer.filterMode != FilterMode.Point
                   || importer.mipmapEnabled
                   || !importer.alphaIsTransparency
                   || importer.textureCompression != TextureImporterCompression.Uncompressed;
        }
    }
}
