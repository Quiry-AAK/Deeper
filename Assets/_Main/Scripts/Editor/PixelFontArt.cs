using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Packs <see cref="PixelFontGlyphs"/> into an atlas and builds the HUD's font asset
    /// (`Deeper/Generate HUD Font`), writing `Art/UI/HUD_Font.png` and `HUD_Font.fontsettings`.
    ///
    /// **Why a bitmap font at all.** Every other pixel in this HUD is authored on a grid; the text
    /// was Unity's built-in `LegacyRuntime.ttf`, which is a vector face rendered with anti-aliasing.
    /// Nice frames with soft grey letters on top read as a programmer UI no matter how good the
    /// frames are, and it was the loudest remaining "this is not a pixel game" tell.
    ///
    /// **The glyphs are packed at 1x, so the font's native size is 7 and text shares the chrome's
    /// pixel grid.** They were packed at 2x at first, back when the canvas drew the HUD at 1:1 —
    /// that made text twice as coarse as the frames around it, one HUD with two pixel grids in it.
    /// The canvas now scales the whole HUD by 2 (<see cref="Deeper.UI.PixelPerfectHUDScale"/>), so a
    /// 7px face lands on screen at 14px exactly as before, and every pixel in the HUD — chrome,
    /// icons and letters — is now the same size. The HUD asks for 7 (native, no scaling) and 14 for
    /// the one oversized label, which is 2x native and therefore integral either way.
    ///
    /// Text still needs its 1px drop shadow to sit over the world; <see cref="BuildRunHUD"/> adds it.
    /// </summary>
    public static class PixelFontArt
    {
        private const string Folder = "Assets/_Main/Art/UI/";
        private const string TextureName = "HUD_Font";
        private const string FontName = "HUD_Font";

        /// <summary>Whole-number upscale of the authored glyphs. 1 — the canvas does the scaling
        /// now, and doing it twice is what made text coarser than the chrome. See the summary.</summary>
        private const int Scale = 1;

        /// <summary>Clear pixels around every cell, so point sampling at a UV edge can never pull a
        /// neighbouring glyph's ink into the quad.</summary>
        private const int Padding = 1;

        private const int Columns = 16;

        private static int GlyphWidth => PixelFontGlyphs.Width * Scale;
        private static int GlyphHeight => PixelFontGlyphs.Height * Scale;
        private static int CellWidth => GlyphWidth + Padding * 2;
        private static int CellHeight => GlyphHeight + Padding * 2;

        /// <summary>Monospaced on purpose: a HUD is mostly numbers that change every frame, and
        /// proportional digits make "100 / 128" shuffle sideways as it counts down.</summary>
        private static int Advance => CellWidth;

        [MenuItem("Deeper/Generate HUD Font")]
        public static void Generate()
        {
            string order = PixelFontGlyphs.Order;
            if (order.Length != PixelFontGlyphs.Rows.Length)
            {
                Debug.LogError("PixelFontGlyphs.Order has " + order.Length + " entries but Rows has " +
                               PixelFontGlyphs.Rows.Length + ". They index each other — fix the table.");
                return;
            }

            int rows = Mathf.CeilToInt(order.Length / (float)Columns);
            int width = Columns * CellWidth;
            int height = rows * CellHeight;

            Texture2D atlas = PackAtlas(order, width, height);
            string texturePath = Folder + TextureName + ".png";
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(texturePath, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            ImportAtlas(texturePath);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogError("The font atlas did not import as a Texture2D — no font was built.");
                return;
            }

            BuildFontAsset(texture, order, width, height);

            AssetDatabase.SaveAssets();

            // **Reimport, or the font is invisible.** Assigning characterInfo to a Font that is
            // already loaded updates the serialized array but does NOT rebuild the internal glyph
            // lookup: the array reads back perfectly while GetCharacterInfo returns false for every
            // character, so Text lays out quads with zeroed UVs and the whole HUD renders wordless.
            // This only bites on a *re-run* — the first generation creates the asset, and creating
            // it builds the lookup — which makes it exactly the kind of failure that ships.
            AssetDatabase.ImportAsset(Folder + FontName + ".fontsettings", ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            Debug.Log("Generated " + FontName + " (" + order.Length + " glyphs, " + width + "x" + height +
                      ", native size " + GlyphHeight + "). Re-run Deeper/Build Run HUD to apply it.");
        }

        // ---------------------------------------------------------------- atlas

        private static Texture2D PackAtlas(string order, int width, int height)
        {
            var pixels = new Color32[width * height];   // zeroed = transparent
            var ink = new Color32(255, 255, 255, 255);  // white, so Text.color tints it directly

            for (int i = 0; i < order.Length; i++)
            {
                string[] glyph = PixelFontGlyphs.Rows[i];
                int originX = i % Columns * CellWidth + Padding;
                int originY = i / Columns * CellHeight + Padding;

                for (int row = 0; row < PixelFontGlyphs.Height; row++)
                {
                    for (int column = 0; column < PixelFontGlyphs.Width; column++)
                    {
                        if (glyph[row][column] != '#') continue;

                        for (int sy = 0; sy < Scale; sy++)
                        {
                            for (int sx = 0; sx < Scale; sx++)
                            {
                                int x = originX + column * Scale + sx;
                                int y = originY + row * Scale + sy;

                                // Authored top-down; SetPixels32 stores bottom-up.
                                pixels[(height - 1 - y) * width + x] = ink;
                            }
                        }
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// The atlas imports as a plain Texture, not a Sprite: a font's material samples it
        /// directly, and a Sprite import would hand back a rect the font knows nothing about.
        /// </summary>
        private static void ImportAtlas(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            // The atlas is not power-of-two and must not be rescaled to one — that would move every
            // glyph off its cell and resample the ink.
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);

            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- font asset

        /// <summary>
        /// Creates or updates the font asset in place. Updating rather than recreating keeps its
        /// GUID, so every scene and prefab already pointing at the font survives a regeneration.
        /// </summary>
        private static void BuildFontAsset(Texture2D atlas, string order, int width, int height)
        {
            string path = Folder + FontName + ".fontsettings";
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);

            if (font == null)
            {
                font = new Font(FontName);
                AssetDatabase.CreateAsset(font, path);
            }

            Material material = FindMaterial(path);
            if (material == null)
            {
                material = new Material(FontShader()) { name = FontName + " Material" };
                AssetDatabase.AddObjectToAsset(material, font);
            }

            material.shader = FontShader();
            material.mainTexture = atlas;

            font.material = material;
            font.characterInfo = BuildCharacters(order, width, height);

            // lineHeight, fontSize and ascent have no public setters on Font, so they go through
            // the serialized object — the same values the font's own inspector writes.
            var serialized = new SerializedObject(font);
            SetSerialized(serialized, "m_FontSize", GlyphHeight);
            SetSerialized(serialized, "m_Ascent", GlyphHeight);
            // Two authored pixels of leading, which is 2 * Scale on screen.
            SetSerialized(serialized, "m_LineSpacing", GlyphHeight + 2 * Scale);
            SetSerialized(serialized, "m_CharacterSpacing", 0);
            SetSerialized(serialized, "m_CharacterPadding", 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(material);
        }

        private static CharacterInfo[] BuildCharacters(string order, int width, int height)
        {
            var characters = new List<CharacterInfo>(order.Length * 2);
            var claimed = new HashSet<char>();

            for (int i = 0; i < order.Length; i++)
            {
                characters.Add(Describe(order[i], i, width, height));
                claimed.Add(order[i]);
            }

            // Lowercase aliases onto the same cells. Only uppercase is authored (the HUD is written
            // in caps), and without these a stray lowercase letter would render as nothing at all
            // rather than as the wrong case — except where the table already draws its own, which
            // 'x' does, because "COMBO x7" reads wrong with a capital.
            for (int i = 0; i < order.Length; i++)
            {
                char upper = order[i];
                if (upper < 'A' || upper > 'Z') continue;

                char lower = char.ToLowerInvariant(upper);
                if (claimed.Contains(lower)) continue;

                characters.Add(Describe(lower, i, width, height));
                claimed.Add(lower);
            }

            return characters.ToArray();
        }

        /// <summary>One glyph's metrics and its corner UVs into the atlas.</summary>
        private static CharacterInfo Describe(char character, int cell, int width, int height)
        {
            int left = cell % Columns * CellWidth + Padding;
            int top = cell / Columns * CellHeight + Padding;

            // The atlas was written top-down, so a row's V is measured from the far edge.
            float u0 = left / (float)width;
            float u1 = (left + GlyphWidth) / (float)width;
            float v0 = (height - (top + GlyphHeight)) / (float)height;
            float v1 = (height - top) / (float)height;

            return new CharacterInfo
            {
                index = character,
                advance = Advance,
                bearing = 0,
                glyphWidth = GlyphWidth,
                glyphHeight = GlyphHeight,

                // Every glyph sits on the baseline and none descends below it, which is what lets
                // the whole set share one box.
                minX = 0,
                maxX = GlyphWidth,
                minY = 0,
                maxY = GlyphHeight,

                uvBottomLeft = new Vector2(u0, v0),
                uvBottomRight = new Vector2(u1, v0),
                uvTopLeft = new Vector2(u0, v1),
                uvTopRight = new Vector2(u1, v1)
            };
        }

        private static Material FindMaterial(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Material material) return material;
            }

            return null;
        }

        private static Shader FontShader()
        {
            // UI/Default rather than GUI/Text Shader: it is the shader UGUI batches text with, and
            // it respects Mask and RectMask2D, which the older text shader does not.
            Shader shader = Shader.Find("UI/Default");
            return shader != null ? shader : Shader.Find("GUI/Text Shader");
        }

        private static void SetSerialized(SerializedObject serialized, string field, float value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("Font has no serialized field '" + field + "' — its metrics may be " +
                                 "wrong. Check the field name against a Font asset's YAML.");
                return;
            }

            if (property.propertyType == SerializedPropertyType.Integer) property.intValue = (int)value;
            else property.floatValue = value;
        }
    }
}
