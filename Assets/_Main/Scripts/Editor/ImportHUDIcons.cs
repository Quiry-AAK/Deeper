using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Applies the sprite import contract to the HUD's generated icons — every
    /// <c>HUD_Icon*.png</c> in <c>Art/UI/</c>.
    ///
    /// **This folder had no importer at all**, which is why it exists. `HUDFrameArt` sets the
    /// contract on the chrome *it draws*, and `ImportUpgradeIcons` sets it on `Art/UI/Icons/`, but
    /// the weapon and dash icons sitting between them were imported by hand in some earlier pass —
    /// so the next generated icon dropped into this folder would silently arrive at Unity's
    /// defaults, 100 PPU with bilinear filtering, and read as a blurry half-size smudge in its slot.
    /// That is the same trap `BuildRoomTiles` was written to close for the environment art.
    ///
    /// It deliberately covers the whole `HUD_Icon*` set rather than one file: a tool that only fixes
    /// the icon that prompted it is a tool that has to be edited every time, which is the thing this
    /// project keeps deciding not to build.
    ///
    /// **64x64 is the size that matters here**, and it is not arbitrary. The HUD draws an icon into
    /// a 32-unit authored square, and the canvas scales by 2 at the 1080p reference
    /// (<see cref="Deeper.UI.PixelPerfectHUDScale"/>), so a 64px source lands 1:1 on screen. The
    /// 128px upgrade icons are a different case — they are drawn much larger on the offer cards.
    /// </summary>
    public static class ImportHUDIcons
    {
        private const string Folder = "Assets/_Main/Art/UI";
        private const string Prefix = "HUD_Icon";
        private const int Expected = 64;

        [MenuItem("Deeper/Import HUD Icons")]
        public static void Import()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning("No " + Folder + " — nothing to import.");
                return;
            }

            int done = 0;

            foreach (string file in Directory.GetFiles(Folder, Prefix + "*.png",
                                                       SearchOption.TopDirectoryOnly))
            {
                string path = file.Replace('\\', '/');
                int index = path.IndexOf("Assets/");
                if (index > 0) path = path.Substring(index);

                if (Apply(path)) done++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Import HUD Icons: " + done + " icon(s) in " + Folder + ".");
        }

        private static bool Apply(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;             // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();

            // Warned, not corrected. A wrong-sized icon cannot be fixed by an importer — rescaling
            // point-filtered art off its own grid is the defect, not the cure — so it has to be
            // regenerated at the right canvas.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && (sprite.rect.width != Expected || sprite.rect.height != Expected))
            {
                Debug.LogWarning(Path.GetFileName(path) + " is " + sprite.rect.width + "x" +
                                 sprite.rect.height + ", not " + Expected + "x" + Expected +
                                 ". It will not land 1:1 on the HUD's pixel grid — regenerate it.",
                                 sprite);
            }

            return true;
        }
    }
}
