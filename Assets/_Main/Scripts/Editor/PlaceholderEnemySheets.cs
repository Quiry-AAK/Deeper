using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Generates the flat-colour placeholder sheets for the Biome 1 enemies, then imports and
    /// slices them to the enemy sheet contract.
    ///
    /// **This lives in the repo on purpose.** The tool that made the training dummy's placeholder
    /// was never committed — it ran once in a scratch directory and was gone, so regenerating that
    /// art meant rebuilding the tool from its output. Placeholder art gets regenerated often
    /// (every silhouette tweak, every added frame), and a one-shot script is a tool you pay for
    /// twice.
    ///
    /// It also removes the need to hand-author `.meta` files. A single sprite's meta can be copied
    /// by hand; a 12-row sheet needs 39 `internalIDToNameTable` entries whose ids the animation
    /// assets then reference, and Unity's own importer is the only thing that generates those
    /// correctly.
    ///
    /// This is programmer art, deliberately NOT routed through the `deeper-art` skill — it exists
    /// to make movement, facing and the telegraph readable before real art lands, not to set style.
    /// </summary>
    public static class PlaceholderEnemySheets
    {
        private const int CellWidth = 32;
        private const int CellHeight = 48;
        private const int Columns = 4;
        private const int Rows = 12;

        private const string OutputFolder = "Assets/_Main/Art/Placeholder/Enemies";

        /// <summary>Clips in sheet row order. Frame counts are ART_DIRECTION §4's enemy ceilings.</summary>
        private enum Clip { IdleMove = 0, Telegraph = 1, Attack = 2, Death = 3 }

        private static readonly int[] ClipFrames = { 4, 3, 3, 3 };

        /// <summary>Authored directions. Enemies draw three; the diagonals reuse Side at bind time.</summary>
        private enum Dir { Down = 0, Up = 1, Side = 2 }

        // Biome 1 is cool greys and muted browns (ART_DIRECTION §2). Orange-red is absent by
        // rule, not by taste: it is reserved exclusively for hazard telegraphs, so an enemy
        // wearing it would read as the rockfall front.
        private static readonly Color32 TelegraphTint = new Color32(200, 192, 180, 255);

        private enum Tone { Shadow = 0, Base = 1, Highlight = 2 }

        /// <summary>How a block reacts to the frame transforms.</summary>
        private enum Part
        {
            Body = 0,
            /// <summary>Steps side to side on the walk cycle.</summary>
            Leg = 1,
            /// <summary>Reaches further than the body on an attack — arms, claws, the sling.</summary>
            Limb = 2,
            /// <summary>Dropped on the Up row, because that is the back of the head.</summary>
            Eye = 3,
        }

        private struct Block
        {
            public int X0, Y0, X1, Y1;   // inclusive, cell-local, y measured up from the cell floor
            public Tone Tone;
            public Part Part;

            public Block(int x0, int y0, int x1, int y1, Tone tone, Part part = Part.Body)
            {
                X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; Tone = tone; Part = part;
            }
        }

        private struct Enemy
        {
            public string Name;
            public Color32 Shadow, Base, Highlight;
            public Block[] Blocks;
        }

        [MenuItem("Deeper/Generate Placeholder Enemy Sheets")]
        public static void Generate()
        {
            foreach (Enemy enemy in Roster())
            {
                string path = OutputFolder + "/" + enemy.Name + ".png";
                File.WriteAllBytes(path, Render(enemy));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ApplyImportSettings(path, enemy.Name);
            }

            WriteRock();

            AssetDatabase.Refresh();
            Debug.Log($"{nameof(PlaceholderEnemySheets)}: wrote 3 sheets + the thrown rock to {OutputFolder}.");
        }

        /// <summary>
        /// The Rock Slinger's projectile — a single 16x16 sprite, not a sheet. It never animates
        /// and never faces a direction, so the whole row/column contract would be ceremony.
        /// </summary>
        private static void WriteRock()
        {
            const int size = 16;
            var pixels = new Color32[size * size];

            Color32 stone = new Color32(96, 101, 112, 255);
            Color32 shade = new Color32(62, 66, 75, 255);
            Color32 lit = new Color32(134, 140, 152, 255);

            // A lumpy blob rather than a circle: at 16px a perfect circle reads as a UI dot, and
            // the rock has to be recognisable as debris at combat speed.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 7.5f) / 6f;
                    float dy = (y - 7.5f) / 5.5f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;

                    // Light from the upper left, the fixed key direction for every asset.
                    pixels[y * size + x] = (x - y) < -3 ? lit : (x - y) > 3 ? shade : stone;
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string path = OutputFolder + "/ThrownRock.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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

        // ---------------------------------------------------------------- roster

        /// <summary>
        /// The three basics. Silhouettes differ in mass and proportion, not only in colour —
        /// ART_DIRECTION §1 requires an enemy be identifiable filled solid black, and a set of
        /// same-shaped blocks in different greys fails that test at combat speed.
        /// </summary>
        private static Enemy[] Roster()
        {
            return new[]
            {
                // Low, wide and splayed: reads as something running along the floor at you.
                new Enemy
                {
                    Name = "CaveCrawler",
                    Shadow = new Color32(74, 55, 41, 255),
                    Base = new Color32(107, 81, 64, 255),
                    Highlight = new Color32(138, 107, 82, 255),
                    Blocks = new[]
                    {
                        new Block(2, 2, 8, 5, Tone.Shadow, Part.Leg),
                        new Block(23, 2, 29, 5, Tone.Shadow, Part.Leg),
                        new Block(3, 6, 9, 8, Tone.Shadow, Part.Leg),
                        new Block(22, 6, 28, 8, Tone.Shadow, Part.Leg),
                        new Block(6, 4, 25, 15, Tone.Base),
                        new Block(8, 13, 23, 18, Tone.Highlight),
                        new Block(12, 16, 19, 22, Tone.Base),
                        new Block(9, 12, 12, 16, Tone.Base, Part.Limb),
                        new Block(19, 12, 22, 16, Tone.Base, Part.Limb),
                        new Block(13, 18, 14, 19, Tone.Shadow, Part.Eye),
                        new Block(17, 18, 18, 19, Tone.Shadow, Part.Eye),
                    },
                },

                // Small, narrow and upright, with one arm cocked back holding a rock — the
                // silhouette carries the threat, since a thrower reads as harmless otherwise.
                new Enemy
                {
                    Name = "RockSlinger",
                    Shadow = new Color32(94, 99, 109, 255),
                    Base = new Color32(138, 143, 153, 255),
                    Highlight = new Color32(176, 181, 190, 255),
                    Blocks = new[]
                    {
                        new Block(12, 0, 15, 10, Tone.Shadow, Part.Leg),
                        new Block(17, 0, 20, 10, Tone.Shadow, Part.Leg),
                        new Block(11, 9, 20, 26, Tone.Base),
                        new Block(12, 19, 19, 25, Tone.Highlight),
                        new Block(13, 26, 19, 33, Tone.Base),
                        new Block(8, 18, 11, 27, Tone.Base, Part.Limb),
                        new Block(20, 20, 23, 32, Tone.Base, Part.Limb),
                        new Block(21, 32, 25, 36, Tone.Highlight, Part.Limb),
                        new Block(14, 29, 15, 30, Tone.Shadow, Part.Eye),
                        new Block(17, 29, 18, 30, Tone.Shadow, Part.Eye),
                    },
                },

                // Tall, broad and top-heavy with a small head — the overhead slam has to be
                // legible from the shoulders alone.
                new Enemy
                {
                    Name = "TunnelBrute",
                    Shadow = new Color32(46, 50, 59, 255),
                    Base = new Color32(74, 79, 90, 255),
                    Highlight = new Color32(106, 113, 128, 255),
                    Blocks = new[]
                    {
                        new Block(8, 0, 14, 15, Tone.Shadow, Part.Leg),
                        new Block(18, 0, 24, 15, Tone.Shadow, Part.Leg),
                        new Block(5, 13, 27, 33, Tone.Base),
                        new Block(7, 25, 25, 32, Tone.Highlight),
                        new Block(3, 30, 29, 36, Tone.Base),
                        new Block(13, 34, 19, 42, Tone.Base),
                        new Block(1, 16, 6, 32, Tone.Base, Part.Limb),
                        new Block(26, 16, 31, 32, Tone.Base, Part.Limb),
                        new Block(14, 38, 15, 39, Tone.Shadow, Part.Eye),
                        new Block(17, 38, 18, 39, Tone.Shadow, Part.Eye),
                    },
                },
            };
        }

        // ---------------------------------------------------------------- rendering

        private static byte[] Render(Enemy enemy)
        {
            int width = Columns * CellWidth;
            int height = Rows * CellHeight;
            var pixels = new Color32[width * height];   // starts fully transparent

            for (int clip = 0; clip < ClipFrames.Length; clip++)
            {
                for (int dir = 0; dir < 3; dir++)
                {
                    int row = clip * 3 + dir;
                    for (int frame = 0; frame < ClipFrames[clip]; frame++)
                    {
                        DrawCell(pixels, width, enemy, (Clip)clip, (Dir)dir, frame, row, frame);
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static void DrawCell(
            Color32[] pixels, int sheetWidth, Enemy enemy,
            Clip clip, Dir dir, int frame, int row, int column)
        {
            // Row 0 is the TOP row by contract, but texture memory starts at the bottom-left.
            int originX = column * CellWidth;
            int originY = (Rows - 1 - row) * CellHeight;

            // Which way this row's art faces, in cell pixels. Side art always faces right; the
            // left-hand facings are flipX at runtime (FacingExtensions.IsMirrored).
            int faceX = dir == Dir.Side ? 1 : 0;
            int faceY = dir == Dir.Down ? -1 : dir == Dir.Up ? 1 : 0;

            float squash = 1f, alpha = 1f, tint = 0f;
            int bodyX = 0, bodyY = 0, limbReach = 0, legStep = 0;

            switch (clip)
            {
                case Clip.IdleMove:
                    // A four-step leg cycle, not a two-step alternation: alternating every frame
                    // made frames 0 and 2 identical, so a 4-frame walk only ever showed 3 poses.
                    bodyY = new[] { 0, 1, 0, -1 }[frame];
                    legStep = new[] { -1, 0, 1, 0 }[frame];
                    break;

                case Clip.Telegraph:
                    // Rears back AWAY from where it is about to strike, and brightens. The
                    // brighten is a pale warm grey rather than the obvious hazard orange-red,
                    // which ART_DIRECTION §2 reserves for the Hazard Front.
                    int back = new[] { 0, 2, 3 }[frame];
                    bodyX = -faceX * back;
                    bodyY = -faceY * back;
                    tint = new[] { 0f, 0.35f, 0.7f }[frame];
                    break;

                case Clip.Attack:
                    // Frame 0 is the impact pose, not a wind-up. The wind-up is the Telegraph
                    // clip's job now, and the Attack clip is played across Active+Recovery with
                    // frames spread evenly — so whatever is on frame 0 is what covers the whole
                    // damage window. The rest is follow-through settling back toward neutral.
                    int reach = new[] { 4, 2, 1 }[frame];
                    bodyX = faceX * reach;
                    bodyY = faceY * reach;
                    limbReach = reach;      // limbs travel twice as far as the body
                    break;

                case Clip.Death:
                    squash = new[] { 0.75f, 0.45f, 0.2f }[frame];
                    alpha = new[] { 1f, 0.8f, 0.55f }[frame];
                    tint = -0.35f;          // negative darkens
                    break;
            }

            foreach (Block block in enemy.Blocks)
            {
                if (block.Part == Part.Eye && dir != Dir.Down) continue;   // Up/Side hide the face

                int x0 = block.X0, x1 = block.X1, y0 = block.Y0, y1 = block.Y1;

                // Side view is a profile: the body is narrower and shifted toward the facing.
                if (dir == Dir.Side)
                {
                    x0 = Compress(x0);
                    x1 = Compress(x1);
                }

                int dx = bodyX, dy = bodyY;
                if (block.Part == Part.Leg) dx += legStep;
                if (block.Part == Part.Limb)
                {
                    dx += faceX * limbReach;
                    dy += faceY * limbReach;
                }

                y0 = Mathf.RoundToInt(y0 * squash);
                y1 = Mathf.RoundToInt(y1 * squash);

                Color32 colour = Shade(enemy, block.Tone, dir, tint, alpha);
                Fill(pixels, sheetWidth, originX, originY,
                     x0 + dx, y0 + dy, x1 + dx, y1 + dy, colour);
            }
        }

        /// <summary>Squeezes an x toward the cell's centre line, turning a front view into a profile.</summary>
        private static int Compress(int x)
        {
            return Mathf.RoundToInt(16 + (x - 16) * 0.7f) + 2;
        }

        private static Color32 Shade(Enemy enemy, Tone tone, Dir dir, float tint, float alpha)
        {
            Color colour = tone == Tone.Shadow ? enemy.Shadow
                : tone == Tone.Highlight ? enemy.Highlight : enemy.Base;

            // The Up row is the actor's back, which faces away from the fixed upper-left key
            // light, so it sits a step darker than the front.
            if (dir == Dir.Up) colour = Color.Lerp(colour, Color.black, 0.18f);

            if (tint > 0f) colour = Color.Lerp(colour, TelegraphTint, tint);
            else if (tint < 0f) colour = Color.Lerp(colour, Color.black, -tint);

            colour.a = alpha;
            return colour;
        }

        /// <summary>
        /// Fills a block in cell-local coordinates, clipped to the cell.
        ///
        /// Clipping to the CELL rather than to the texture is the point: a telegraph leaning back
        /// or an attack reaching forward pushes blocks past the cell edge, and an unclipped write
        /// lands in the neighbouring frame — which shows up as a stray limb in an unrelated
        /// animation rather than as an error.
        /// </summary>
        private static void Fill(
            Color32[] pixels, int sheetWidth, int originX, int originY,
            int x0, int y0, int x1, int y1, Color32 colour)
        {
            x0 = Mathf.Max(x0, 0);
            y0 = Mathf.Max(y0, 0);
            x1 = Mathf.Min(x1, CellWidth - 1);
            y1 = Mathf.Min(y1, CellHeight - 1);

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    pixels[(originY + y) * sheetWidth + originX + x] = colour;
                }
            }
        }

        // ---------------------------------------------------------------- import

        private static void ApplyImportSettings(string path, string enemyName)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;              // ART_DIRECTION §1
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // AuraVisuals reads pixels off the sheets it draws, and the character sheets are
            // readable for that reason. Enemies have no aura, so this stays off — a readable
            // texture keeps a second copy in CPU memory for nothing.
            importer.isReadable = false;

            Slice(importer, enemyName);

            importer.SaveAndReimport();
        }

        /// <summary>
        /// Cuts the sheet into named sub-sprites.
        ///
        /// Only cells that carry a frame are sliced — the 3-frame clips leave column 3 unused, and
        /// slicing an empty cell produces a sprite with no mesh that exists only to be skipped.
        /// That makes 39 sub-sprites per enemy, not 48.
        /// </summary>
        private static void Slice(TextureImporter importer, string enemyName)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();

            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            var names = new List<SpriteNameFileIdPair>();

            for (int clip = 0; clip < ClipFrames.Length; clip++)
            {
                for (int dir = 0; dir < 3; dir++)
                {
                    int row = clip * 3 + dir;

                    for (int column = 0; column < ClipFrames[clip]; column++)
                    {
                        var rect = new SpriteRect
                        {
                            name = $"{enemyName}_{row}_{column}",
                            spriteID = GUID.Generate(),
                            alignment = SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                            rect = new Rect(
                                column * CellWidth,
                                (Rows - 1 - row) * CellHeight,
                                CellWidth,
                                CellHeight),
                        };

                        rects.Add(rect);
                        names.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
                    }
                }
            }

            provider.SetSpriteRects(rects.ToArray());

            // Without this table the sub-sprite file ids are regenerated on every reimport, and
            // every SpriteAnimationSet pointing at them silently goes null
            // (Engineering/01-VERIFICATION.md §6).
            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(names);

            provider.Apply();
        }
    }
}
