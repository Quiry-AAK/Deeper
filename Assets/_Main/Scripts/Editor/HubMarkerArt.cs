using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Draws the two sprites that mark a usable camp fixture — a chevron seen from across the camp,
    /// and the <c>E</c> keycap it becomes once she is in range — and writes them to
    /// <c>Art/Environment/SurfaceCamp/Markers/</c>.
    ///
    /// **Drawn, not generated**, by the split CLAUDE.md already draws for the HUD: generate things
    /// with material, draw things with geometry. A chevron and a keycap are pure symbols with no
    /// material to interpret, and what matters about them is an exact 1px border and a letterform
    /// that matches the rest of the game's text — neither of which a generative model gives
    /// reliably. The <c>E</c> is literally the HUD font's own glyph, read straight out of
    /// <see cref="PixelFontGlyphs"/>, so the key on screen and the key in the prompt line are the
    /// same letter rather than two people's idea of an E.
    ///
    /// **These are world sprites, so they are authored at FULL size**, unlike everything in
    /// <see cref="HUDFrameArt"/>. That file's half-size rule exists because the HUD canvas scales by
    /// a whole number from a 540 reference; a world sprite at 32 PPU is 1:1 with world pixels and
    /// halving it would just make it small. The player is 32x48, so a 15px keycap reads as about a
    /// third of her height.
    ///
    /// **No hazard accents.** ART_DIRECTION §2 reserves orange-red, pale cyan-white and bright
    /// yellow-orange exclusively for danger telegraphs. An interaction prompt glowing in any of them
    /// would be speaking the danger language on the one screen in the game with no danger on it, so
    /// this is bone-white on near-black and nothing else.
    /// </summary>
    public static class HubMarkerArt
    {
        private const string Folder = "Assets/_Main/Art/Environment/SurfaceCamp/Markers/";

        /// <summary>Cool bone white. Matches the lit edge of the HUD's plate chrome.</summary>
        private static readonly Color32 Bone = new Color32(222, 226, 236, 255);

        /// <summary>The same near-black HUDFrameArt outlines with, so the two agree.</summary>
        private static readonly Color32 Outline = new Color32(12, 11, 18, 255);

        /// <summary>Keycap interior — dark enough that the bone letter carries on any background.</summary>
        private static readonly Color32 Cap = new Color32(27, 26, 36, 255);

        [MenuItem("Deeper/Generate Hub Markers")]
        public static void Generate()
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);

            Write("Marker_Idle", Chevron());
            Write("Marker_Use", Keycap('E'));

            AssetDatabase.Refresh();
            Debug.Log("Generate Hub Markers: wrote Marker_Idle and Marker_Use to " + Folder);
        }

        /// <summary>
        /// The at-a-distance mark: a solid chevron pointing down at the fixture it belongs to.
        ///
        /// Solid rather than a hollow V, which is the version that was drawn first — at 11px a
        /// two-pixel stroke with a hole in it reads as noise, and the whole job of this sprite is to
        /// be identifiable from the far side of the camp.
        /// </summary>
        private static Texture2D Chevron()
        {
            const int width = 11;
            const int height = 9;

            Color32[] px = NewPixels(width, height);

            // Rows 1..5 from the top, narrowing 9, 7, 5, 3, 1 about the centre column. Row 0 and the
            // last two rows are left clear for the outline pass to occupy.
            for (int row = 0; row < 5; row++)
            {
                int half = 4 - row;
                for (int x = 5 - half; x <= 5 + half; x++) Set(px, width, height, x, row + 1, Bone);
            }

            Ring(px, width, height, Outline);
            return Bake(px, width, height);
        }

        /// <summary>
        /// The in-range mark: the key you press, drawn as a physical cap.
        ///
        /// A cap rather than a bare letter because a lone glyph floating over a camp reads as debris;
        /// the border is what says "this is a button".
        /// </summary>
        private static Texture2D Keycap(char glyph)
        {
            const int size = 15;

            Color32[] px = NewPixels(size, size);

            // Body first, corners knocked off so the cap reads as rounded rather than as a box.
            for (int row = 1; row < size - 1; row++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    if (Corner(x, row, size)) continue;

                    bool border = x == 1 || x == size - 2 || row == 1 || row == size - 2;
                    Set(px, size, size, x, row, border ? Bone : Cap);
                }
            }

            // The HUD font's own glyph, centred. 5x7 in a 15x15 cap leaves an even margin on both
            // axes, which is why the cap is 15 and not 14 or 16.
            string[] rows = Glyph(glyph);
            if (rows != null)
            {
                for (int row = 0; row < PixelFontGlyphs.Height; row++)
                {
                    for (int x = 0; x < PixelFontGlyphs.Width; x++)
                    {
                        if (rows[row][x] != '#') continue;
                        Set(px, size, size, x + 5, row + 4, Bone);
                    }
                }
            }

            Ring(px, size, size, Outline);
            return Bake(px, size, size);
        }

        private static string[] Glyph(char c)
        {
            int index = PixelFontGlyphs.Order.IndexOf(c);
            if (index < 0 || index >= PixelFontGlyphs.Rows.Length)
            {
                Debug.LogWarning("HubMarkerArt: no '" + c + "' in the HUD font; the cap ships blank.");
                return null;
            }

            return PixelFontGlyphs.Rows[index];
        }

        /// <summary>Knocks the single corner pixel off each side of a rounded cap.</summary>
        private static bool Corner(int x, int row, int size)
        {
            bool left = x == 1;
            bool right = x == size - 2;
            bool top = row == 1;
            bool bottom = row == size - 2;

            return (left || right) && (top || bottom);
        }

        /// <summary>
        /// Surrounds every drawn pixel with <paramref name="colour"/>.
        ///
        /// Read from a copy, so the ring cannot grow into itself — outlining in place spreads one
        /// pixel further on every column and swallows a small sprite whole. This is the
        /// silhouette-safety rule from the style guide §5: these two float over dirt, grass and
        /// stone, and without a dark edge the bone would disappear against the wall's lit faces.
        /// </summary>
        private static void Ring(Color32[] px, int width, int height, Color32 colour)
        {
            var source = (Color32[])px.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (source[y * width + x].a > 0) continue;
                    if (!Neighbours(source, width, height, x, y)) continue;

                    px[y * width + x] = colour;
                }
            }
        }

        private static bool Neighbours(Color32[] px, int width, int height, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    if (px[ny * width + nx].a > 0) return true;
                }
            }

            return false;
        }

        private static Color32[] NewPixels(int width, int height)
        {
            var px = new Color32[width * height];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        /// <summary>Sets a pixel addressed from the TOP row, so the code reads the way the art looks.</summary>
        private static void Set(Color32[] px, int width, int height, int x, int rowFromTop, Color32 colour)
        {
            if (x < 0 || x >= width || rowFromTop < 0 || rowFromTop >= height) return;

            px[(height - 1 - rowFromTop) * width + x] = colour;
        }

        private static Texture2D Bake(Color32[] px, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static void Write(string name, Texture2D tex)
        {
            string path = Folder + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

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

            // Centre pivot: BuildHubScene places the marker by its own centre at a measured height
            // above the fixture, and a bottom pivot would make the two sprites — which differ in
            // height — hang at different distances as the marker swapped.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
