using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Draws the HUD's frame chrome — the bars, the weapon and dash slots, the level badge, the
    /// upgrade slots and the wave banner — and writes them to <c>Art/UI/</c>.
    ///
    /// **Procedural rather than generated, and that is a deliberate split.** What matters about
    /// these pieces is an exact border width, a bevel that lands on whole pixels, and — the part
    /// nothing else can guarantee — an interior that is **fully transparent**, because
    /// <see cref="BuildRunHUD"/> measures that hole to place each bar's fill. A generative model
    /// gives none of those reliably: asked for empty slim frames it returned a heart, a money bag
    /// and a lightning bolt in a palette the game does not use. Icons, which have material and form
    /// to interpret, go the other way and are generated through the `deeper-art` skill.
    ///
    /// **The chrome is an iron plate, not an outline.** An earlier pass drew each frame as a
    /// four-pixel band of one flat grey, which read as a wireframe rather than as a pixel-art HUD.
    /// What was added: a real bevel (one lit pixel on the top and left faces, near-black on the
    /// bottom and right), an inverted bevel around each channel so a bar reads as sunk into the
    /// plate, solid riveted end caps on the bars, and segment ticks on HP. What was deliberately
    /// *not* added is size — the owner's earlier "too much" was about a 448x129 bar eating a fifth
    /// of the screen, and every piece here keeps its slim footprint.
    ///
    /// **Everything is authored at HALF the size it displays at**, because the canvas scales the HUD
    /// by a whole number and that number is 2 at the 1080p reference (see
    /// <see cref="Deeper.UI.PixelPerfectHUDScale"/>). Authoring at full size instead forced a factor
    /// of 1, which made the HUD twice as prominent as intended on any window below 1080 — the owner's
    /// "you made UI bigger in low resolutions". At half size the on-screen result at 1080p is
    /// identical and a small window simply gets a smaller HUD, still perfectly sharp. The practical
    /// consequence for anyone editing these numbers: **a 1px detail here is a 2px detail on screen**,
    /// and there is no room for a detail finer than that.
    ///
    /// Re-run `Deeper/Generate HUD Frames` after changing any constant, then re-run
    /// `Deeper/Build Run HUD` so the layout picks the new sizes up.
    /// </summary>
    public static class HUDFrameArt
    {
        private const string Folder = "Assets/_Main/Art/UI/";

        /// <summary>How far a segment tick reaches into the channel from each wall.</summary>
        private const int TickLength = 2;

        // Cool steel, torch-lit — ART_DIRECTION §1's neutral stone-grey base. No hazard accent
        // appears anywhere in the chrome (§2 reserves those for danger telegraphs); every colour in
        // the HUD comes from the fills, which is also what makes the fills the thing the eye finds.
        private static readonly Color32 Outline = new Color32(12, 11, 18, 255);
        private static readonly Color32 Steel5 = new Color32(178, 182, 194, 255);   // lit edge, 1px
        private static readonly Color32 Steel4 = new Color32(134, 139, 154, 255);
        private static readonly Color32 Steel3 = new Color32(98, 102, 118, 255);
        private static readonly Color32 Steel2 = new Color32(70, 73, 90, 255);      // plate body
        private static readonly Color32 Steel1 = new Color32(46, 48, 62, 255);
        private static readonly Color32 Steel0 = new Color32(28, 29, 38, 255);      // shaded edge
        private static readonly Color32 Recess = new Color32(18, 17, 26, 255);
        private static readonly Color32 RivetLit = new Color32(198, 202, 212, 255);
        private static readonly Color32 RivetDark = new Color32(30, 31, 42, 255);
        private static readonly Color32 Field = new Color32(16, 15, 22, 190);       // translucent

        [MenuItem("Deeper/Generate HUD Frames")]
        public static void Generate()
        {
            // Health: eight segments, so HP reads as a quantity and not only as a length.
            Write("HUD_BarSlim", Bar(160, 18, 3, 7, 8, 2));

            // The Ultimate gets no segments on purpose. ART_DIRECTION §5 wants it to read as
            // *filling* toward a spend, and ticks turn a continuous resource into a counted one.
            Write("HUD_BarSlimUltimate", Bar(150, 15, 3, 6, 0, 2));
            Write("HUD_BarSlimXP", Bar(120, 11, 2, 5, 0, 1));

            // One fill column per bar, sized to that bar's own channel. Shared art stretched to
            // three different heights is the one place point-filtered pixels get resampled by a
            // non-integer factor, and the bright top row is exactly what that would smear.
            Write("HUD_BarSlim_Fill", FillColumn(18 - 3 * 2));
            Write("HUD_BarSlimUltimate_Fill", FillColumn(15 - 3 * 2));
            Write("HUD_BarSlimXP_Fill", FillColumn(11 - 2 * 2));

            // Weapon and dash share the SAME square slot (owner-directed): beside a square weapon
            // slot a circle reads as a different kind of element rather than as its pair. The 4px
            // border makes the hole exactly 32, which is a 64px icon at the canvas's 2x — so the
            // icons stay authored at 64 and still land 1:1 on screen.
            Write("HUD_SlotSquare", Slot(40, 4, 2, true));

            // Upgrade slots are small and quiet — a running record, not a live readout.
            Write("HUD_SlotUpgrade", Slot(22, 2, 1, false));

            // The badge keeps the hexagon: it is the one shape in the HUD that is neither a bar nor
            // a slot, which is what makes "level" findable at a glance. Sized so a two-digit level
            // still clears the walls, and given a dark field so the number sits in a recess rather
            // than floating on the world.
            Write("HUD_SlotHex", HexPlate(28, 2));

            // Kept, unused by the current layout: what a round dash slot would need if that
            // owner decision is ever revisited. Regenerating them keeps the kit one style.
            Write("HUD_SlotRound", RoundSlot(40, 3));
            Write("HUD_Disc", Disc(34));

            // The wave banner sits over the middle of the play area, so its field is translucent —
            // an opaque plaque there hides whatever walks behind it.
            Write("HUD_Banner", Banner(134, 19, 2, 5));

            // HUD_IconDash is deliberately NOT written here. The drawn chevrons this file used to
            // emit were rejected by the owner, and the replacement is generated art — re-adding a
            // Write for it would silently clobber that PNG the next time anyone ran this menu item.

            AssetDatabase.Refresh();
            Debug.Log("Generated the HUD frames into " + Folder + ". Re-run Deeper/Build Run HUD to lay them out.");
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// A bar: solid riveted end caps clamping a sunken channel, with optional segment ticks.
        /// </summary>
        private static Texture2D Bar(int width, int height, int border, int cap, int segments, int chamfer)
        {
            var px = NewPixels(width, height);
            int hx0 = cap, hx1 = width - 1 - cap;
            int hy0 = border, hy1 = height - 1 - border;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Chamfered(x, y, width, height, chamfer)) continue;
                    if (x >= hx0 && x <= hx1 && y >= hy0 && y <= hy1) continue;   // channel

                    if (x >= hx0 - 1 && x <= hx1 + 1 && y >= hy0 - 1 && y <= hy1 + 1)
                    {
                        Set(px, width, height, x, y, ChannelWall(x < hx0 || y < hy0));
                        continue;
                    }

                    Set(px, width, height, x, y, PlateAt(x, y, width, height));
                }
            }

            // Segment ticks: 2px spurs hanging off each channel wall. They cross the fill, because
            // the frame Image draws over it, which is what makes them read as divisions of the bar.
            //
            // None of them may land on the frame's exact centre column: BuildRunHUD.MeasureHole
            // reads the channel's *height* down that column, and a tick there reports a channel
            // TickLength px short, which sits every fill high inside its frame. An even segment count always puts
            // one there, so the whole set is nudged clear rather than that divider being dropped —
            // a missing middle mark out of eight is obvious, two pixels of phase is not.
            int span = hx1 - hx0 + 1;
            int nudge = 0;

            for (int i = 1; i < segments; i++)
            {
                if (Mathf.Abs(TickX(hx0, span, segments, i, 0) - width / 2) > 1) continue;
                nudge = 2;
                break;
            }

            for (int i = 1; i < segments; i++)
            {
                int tx = TickX(hx0, span, segments, i, nudge);
                for (int d = 0; d < TickLength; d++)
                {
                    Set(px, width, height, tx, hy0 + d, Recess);
                    Set(px, width, height, tx, hy1 - d, Recess);
                }
            }

            Rivet(px, width, height, cap / 2, height / 2);
            Rivet(px, width, height, width - 1 - cap / 2, height / 2);
            return Bake(px, width, height);
        }

        private static int TickX(int channelLeft, int span, int segments, int index, int nudge)
        {
            return channelLeft + Mathf.RoundToInt(index * span / (float)segments) + nudge;
        }

        /// <summary>A square slot with a hollow interior, and studs at the corners.</summary>
        private static Texture2D Slot(int size, int border, int chamfer, bool rivets)
        {
            var px = NewPixels(size, size);
            int lo = border, hi = size - 1 - border;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Chamfered(x, y, size, size, chamfer)) continue;
                    if (x >= lo && x <= hi && y >= lo && y <= hi) continue;

                    int depth = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    Set(px, size, size, x, y, depth == border - 1
                        ? ChannelWall(x < lo || y < lo)
                        : PlateAt(x, y, size, size));
                }
            }

            if (rivets)
            {
                Rivet(px, size, size, 2, 2);
                Rivet(px, size, size, size - 4, 2);
                Rivet(px, size, size, 2, size - 4);
                Rivet(px, size, size, size - 4, size - 4);
            }

            return Bake(px, size, size);
        }

        /// <summary>
        /// A pointy-top hexagonal plate for the level badge.
        ///
        /// Ring depth comes from the hexagon's own distance metric — the largest of the three
        /// opposing-edge projections — rather than from a circle, so the bevel runs parallel to each
        /// edge instead of drifting away from it at the corners.
        /// </summary>
        private static Texture2D HexPlate(int size, int border)
        {
            var px = NewPixels(size, size);
            float centre = (size - 1) * 0.5f;
            float apothem = size * 0.5f - 0.5f;
            const float Cos30 = 0.8660254f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;   // top-down, so negative dy is the lit half

                    float hex = Mathf.Max(Mathf.Abs(dx),
                                Mathf.Max(Mathf.Abs(dy * Cos30 + dx * 0.5f),
                                          Mathf.Abs(dy * Cos30 - dx * 0.5f)));
                    if (hex > apothem) continue;

                    int depth = Mathf.FloorToInt(apothem - hex);
                    Color32 colour = depth >= border ? Field
                                   : depth == border - 1 ? ChannelWall(dy < 0f)
                                   : Plate(x, y, depth, dy < 0f, dy > 0f);
                    Set(px, size, size, x, y, colour);
                }
            }

            return Bake(px, size, size);
        }

        /// <summary>A round slot. Same plate profile, measured by radius instead of edge distance.</summary>
        private static Texture2D RoundSlot(int size, int border)
        {
            var px = NewPixels(size, size);
            float centre = (size - 1) * 0.5f;
            float outer = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > outer - 0.5f) continue;

                    int depth = Mathf.FloorToInt(outer - 0.5f - r);
                    if (depth >= border) continue;

                    bool lit = dy < 0f && dx < 0f;
                    Set(px, size, size, x, y, depth == border - 1
                        ? ChannelWall(lit)
                        : Plate(x, y, depth, lit, dy > 0f && dx > 0f));
                }
            }

            return Bake(px, size, size);
        }

        /// <summary>A flat white disc. Tinted in code, and radially filled for the dash cooldown.</summary>
        private static Texture2D Disc(int size)
        {
            var px = NewPixels(size, size);
            float centre = (size - 1) * 0.5f;
            float radius = size * 0.5f - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    Set(px, size, size, x, y, new Color32(255, 255, 255, 255));
                }
            }

            return Bake(px, size, size);
        }

        /// <summary>A wide plate with a translucent field, for text that sits over the play area.</summary>
        private static Texture2D Banner(int width, int height, int border, int chamfer)
        {
            var px = NewPixels(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Chamfered(x, y, width, height, chamfer)) continue;

                    int depth = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    Set(px, width, height, x, y,
                        depth >= border ? Field : PlateAt(x, y, width, height));
                }
            }

            return Bake(px, width, height);
        }

        /// <summary>
        /// The 1-pixel-wide column a bar's fill is drawn from, multiplied by that bar's colour.
        ///
        /// A flat fill reads as a coloured rectangle; the specular row one pixel below the top is
        /// what makes it read as something *in* the channel. One pixel wide because the Image
        /// stretches horizontally, and a single column survives that exactly.
        /// </summary>
        private static Texture2D FillColumn(int height)
        {
            var px = NewPixels(1, height);

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)Mathf.Max(height - 1, 1);
                byte v = y == 0 ? (byte)210
                       : y == 1 ? (byte)255
                       : t < 0.55f ? (byte)216
                       : t < 0.80f ? (byte)186
                       : (byte)150;
                Set(px, 1, height, 0, y, new Color32(v, v, v, 255));
            }

            return Bake(px, 1, height);
        }

        // ---------------------------------------------------------------- plate profile

        /// <summary>
        /// One pixel of the raised plate, by how deep into the border it sits.
        ///
        /// Light comes from the top-left, matching the world sprites: one bright pixel on the top
        /// and left faces, near-black on the bottom and right, a dark body between them. A first
        /// pass ran the whole ramp bright and checkered the body ring end to end — at 4x that read
        /// as woven mesh rather than iron, and on screen it competed with the bars it frames.
        /// </summary>
        private static Color32 Plate(int x, int y, int depth, bool lit, bool dark)
        {
            if (depth <= 0) return Outline;
            if (depth == 1) return lit ? Steel5 : dark ? Steel0 : Steel2;
            if (depth == 2) return lit ? Steel3 : dark ? Steel0 : Steel1;

            // The one dithered ring, and only on the lit edge: it fades the highlight into the body
            // the way a bevel does on a curved surface. Two flat tones meeting leave a visible step.
            if (depth == 3 && lit && (x + y) % 2 == 0) return Steel3;
            return Steel2;
        }

        /// <summary>Plate colour for a rectangular piece, working out which face the pixel is on.</summary>
        private static Color32 PlateAt(int x, int y, int width, int height)
        {
            int fromLeft = x, fromRight = width - 1 - x, fromTop = y, fromBottom = height - 1 - y;
            int depth = Mathf.Min(Mathf.Min(fromLeft, fromRight), Mathf.Min(fromTop, fromBottom));

            bool touchesLit = depth == fromTop || depth == fromLeft;
            bool touchesDark = depth == fromBottom || depth == fromRight;

            // A pixel on both (the top-right and bottom-left corners) gets neither, so the two
            // faces meet in the mid tone instead of one of them winning the whole corner.
            return Plate(x, y, depth, touchesLit && !touchesDark, touchesDark && !touchesLit);
        }

        /// <summary>
        /// The single pixel of wall around a sunken channel — an *inverted* bevel, dark on the
        /// channel's top and left walls and lit on its bottom and right. That inversion is the whole
        /// reason a bar reads as cut into the plate rather than punched through it.
        /// </summary>
        private static Color32 ChannelWall(bool topOrLeft)
        {
            return topOrLeft ? Recess : Steel4;
        }

        /// <summary>
        /// A stud: one lit pixel with one shadowed pixel below-right, so it reads as domed rather
        /// than punched.
        ///
        /// Two pixels, not the 3x3 dome an earlier pass drew. The whole kit is authored at half the
        /// size it displays at — the canvas scales it 2x — and at this size a 3x3 stud is a third of
        /// the width of a bar's end cap. One highlight and one shadow is the pixel-art idiom for a
        /// rivet at small scale, and it becomes a clean 2x2 pair on screen.
        /// </summary>
        private static void Rivet(Color32[] px, int width, int height, int cx, int cy)
        {
            Set(px, width, height, cx, cy, RivetLit, true);
            Set(px, width, height, cx + 1, cy + 1, RivetDark, true);
        }

        /// <summary>True where a corner is cut away. Cut square rather than drawn as a radius, so
        /// every pixel stays on-grid; at a 2-3px cut the eye reads it as rounded anyway.</summary>
        private static bool Chamfered(int x, int y, int width, int height, int chamfer)
        {
            int cornerX = Mathf.Min(x, width - 1 - x);
            int cornerY = Mathf.Min(y, height - 1 - y);
            return cornerX + cornerY < chamfer;
        }

        // ---------------------------------------------------------------- raster plumbing

        private static Color32[] NewPixels(int width, int height)
        {
            return new Color32[width * height];   // zeroed = transparent
        }

        /// <summary>
        /// Writes one pixel in **top-down** coordinates, which is how every routine above is
        /// written and how its comments read. <c>SetPixels32</c> stores bottom-up, so the flip
        /// lives here rather than in nine drawing loops.
        /// </summary>
        private static void Set(Color32[] px, int width, int height, int x, int y, Color32 colour,
                                bool onlyOverInk = false)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;

            int i = (height - 1 - y) * width + x;
            if (onlyOverInk && px[i].a == 0) return;
            px[i] = colour;
        }

        private static Texture2D Bake(Color32[] px, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Writes the PNG and applies the project's sprite import contract to it.</summary>
        private static void Write(string name, Texture2D tex)
        {
            string path = Folder + name + ".png";
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);

            importer.SaveAndReimport();
        }
    }
}
