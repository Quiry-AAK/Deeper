using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Writes the forced palette the upgrade icons are generated against
    /// (<c>Art/StyleAnchor/UI_IconPalette.png</c>), as a strip of 8x8 swatches.
    ///
    /// <b>Every colour here is one the game already draws.</b> That is the whole point, and it is
    /// the practice the environment pass arrived at the expensive way: PixelLab returns whatever
    /// palette it likes unless one is forced on it — asked for HUD chrome it produced "a heart, a
    /// money bag and a lightning bolt in a palette the game does not use" — and passing a swatch
    /// image as <c>color_image_base64</c> is what fixed it in one attempt. Inventing colours for this
    /// file would put the icons back in that failure mode with extra steps.
    ///
    /// Committed as code rather than as a hand-painted PNG for the reason the room map and the HUD
    /// layout are: a swatch strip nobody can diff is a palette nobody can check against its sources,
    /// and each row below names where its colours already appear.
    ///
    /// <b>No hazard accent appears here.</b> ART_DIRECTION §2 reserves orange-red, pale cyan-white
    /// and bright yellow-orange exclusively for danger telegraphs, and the change brief confirms the
    /// reservation covers UI. The crimson below is the health bar's, which was chosen for exactly
    /// this constraint.
    /// </summary>
    public static class IconPalette
    {
        private const string Folder = "Assets/_Main/Art/StyleAnchor";
        private const string Path = Folder + "/UI_IconPalette.png";
        private const int Swatch = 8;

        private static readonly Color32[] Swatches =
        {
            // Upper Caves cool greys — the seven already in UpperCaves_Palette.png, themselves taken
            // from Tile_Floor and Tile_Wall and extended a step each way.
            new Color32(0x1A, 0x18, 0x20, 255),
            new Color32(0x29, 0x26, 0x2E, 255),
            new Color32(0x33, 0x30, 0x38, 255),
            new Color32(0x45, 0x41, 0x4A, 255),
            new Color32(0x57, 0x52, 0x5C, 255),
            new Color32(0x75, 0x6E, 0x7C, 255),
            new Color32(0x8C, 0x85, 0x94, 255),

            // Muted browns and a sparse warm ochre — ART_DIRECTION §2's Upper Caves row. Leather,
            // hafts, bindings.
            new Color32(0x2E, 0x24, 0x1E, 255),
            new Color32(0x48, 0x38, 0x2C, 255),
            new Color32(0x60, 0x4E, 0x3A, 255),
            new Color32(0x7C, 0x66, 0x4C, 255),
            new Color32(0x96, 0x7E, 0x54, 255),
            new Color32(0xB0, 0x98, 0x6A, 255),

            // The HUD's own steel ramp (HUDFrameArt), so an icon of a blade or a buckle is made of
            // the same metal as the frame around it.
            new Color32(178, 182, 194, 255),
            new Color32(98, 102, 118, 255),
            new Color32(46, 48, 62, 255),

            // The four tier colours (ART_DIRECTION §5), already shipped in the run's upgrade strip.
            new Color32(199, 202, 209, 255),
            new Color32(107, 153, 219, 255),
            new Color32(168, 115, 219, 255),
            new Color32(230, 189, 92, 255),

            // The bar fills, which are where every non-neutral colour in the HUD already comes from:
            // health crimson, XP green, Ultimate blue, Combo amber, and the Curse card's red.
            new Color32(158, 41, 56, 255),
            new Color32(112, 158, 112, 255),
            new Color32(107, 140, 184, 255),
            new Color32(217, 166, 102, 255),
            new Color32(194, 64, 79, 255),
        };

        [MenuItem("Deeper/Generate Icon Palette")]
        public static void Generate()
        {
            int width = Swatches.Length * Swatch;
            var px = new Color32[width * Swatch];

            for (int i = 0; i < Swatches.Length; i++)
            {
                for (int y = 0; y < Swatch; y++)
                {
                    for (int x = 0; x < Swatch; x++)
                    {
                        px[y * width + i * Swatch + x] = Swatches[i];
                    }
                }
            }

            var tex = new Texture2D(width, Swatch, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();

            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Main/Art", "StyleAnchor");

            File.WriteAllBytes(Path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
            if (importer != null)
            {
                // A reference image, not a sprite: nothing in the game draws it. Point and
                // uncompressed anyway, so what is read back off disk is exactly what was written.
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Debug.Log("Wrote " + Swatches.Length + " swatches to " + Path +
                      ". Pass it as color_image_base64 when generating upgrade icons.");
        }
    }
}
