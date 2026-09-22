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
        private static readonly Color32 Field     = new Color32(16, 15, 22, 190);

        /// <summary>
        /// The icon-led card's tier pips and cost mark are drawn in this near-white rather than the
        /// steel ramp above, so <c>Image.color</c> can tint each one to its own tier colour at
        /// runtime — the same trick the bar fills already rely on, one texture reused under
        /// whatever colour the palette gives it.
        /// </summary>
        private static readonly Color32 NearWhite = new Color32(235, 235, 238, 255);

        /// <summary>
        /// The offer card's interior. Lighter and more opaque than <c>Field</c>, which is
        /// tuned to sit over the play area: the card sits over the offer screen's own dark
        /// scrim instead, and at Field's value the two composited to within a shade of each
        /// other. The card read as an empty outline with nothing inside it.
        /// </summary>
        private static readonly Color32 CardField = new Color32(27, 26, 36, 232);

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

            // The offer card. Authored at half its on-screen size like everything else here,
            // so 152x164 draws as 304x328 at the 1080p reference. Wide enough that a 19-
            // character line of the 5x7 face fits between the borders, which is what sets the
            // width — a narrower card would wrap "REFLECT 25% OF DAMAGE TAKEN" into four lines.
            //
            // Border 6, not 4: at 4 the plate band is a bright hairline around a very large
            // shape and the card reads as a thin outline rather than as a piece of the same chrome
            // the bars are made of. A bar gets away with 3 because it is 18 tall.
            Write("HUD_Card", Card(168, 196, 6, 6));

            // The hover popup behind a taken pick's name and description. Same plate as the offer
            // card, so the two read as one family, and the same 168 width — which is what carries
            // BuildUpgradePanel's 21-characters-per-line budget over to it, since both draw the
            // same face at HUDLayout.BodyText with the same 10-unit padding.
            //
            // 86 tall is measured, not chosen: 10 of padding, a 16-unit header at TitleText, a
            // 4-unit gap, a 45-unit body block (five lines at the face's 9-unit line spacing) and
            // 10 of padding again. Five lines is one more than the longest authored description
            // needs (Upgrade_ThousandCuts, 62 characters, wraps to four), and exactly what a Curse
            // needs for its two-line upside stacked over its three-line cost.
            Write("HUD_Tooltip", Card(168, 86, 6, 6));

            // The offer card's icon socket. Border 4 makes the hole exactly 64, which is a
            // 128px icon at the canvas's 2x — the same arithmetic HUD_SlotSquare does one
            // size down. Upgrade icons are authored at 128 for this box.
            Write("HUD_SlotIcon", Slot(72, 4, 3, true));

            // The offer card's category-glyph socket, heading the card's top row. Border 3 makes
            // the hole exactly 32, which is a 64px Cat_* glyph at the canvas's 2x — the same
            // arithmetic as HUD_SlotIcon at half the size, and paired with
            // BuildUpgradePanel.CategorySlotSize/CategorySlotBorder. 3 rather than 4 so the 38
            // total fits between the card's border and the icon slot. No rivets: at border 3 a
            // stud at the usual 2px inset would land on the channel wall, not the plate.
            Write("HUD_SlotGlyph", Slot(38, 3, 2, false));

            // The icon-led card's tier pip badge, replacing the spelled-out tier word. One
            // pip per rarity step rather than four separate shapes, so "how rare" reads as a
            // count the same way the HP bar's segments do. Curse gets a crossed bar instead of
            // pips — it has no rarity to count, and the mark is meant to read as "the odd one
            // out" rather than "worth N".
            //
            // 64x16, twice the strip these used to be: the badge now shares the card's top row with
            // the 32-unit category glyph instead of hiding in a corner behind it, and at 32x8 it
            // read as a sliver beside a shape four times its area. An Image stretches a sprite to
            // its rect, so the size is re-emitted here rather than set only in
            // BuildUpgradePanel.TierBadgeSize — those two numbers move together.
            Write("HUD_TierCommon", TierPips(64, 16, 1));
            Write("HUD_TierRare", TierPips(64, 16, 2));
            Write("HUD_TierEpic", TierPips(64, 16, 3));
            Write("HUD_TierLegendary", TierPips(64, 16, 4));
            Write("HUD_TierCurse", TierCross(64, 16));

            // The Curse card's cost-line marker: a short rule with a small triangle where it
            // starts. Drawn near-white like the tier pips, and tinted to the Curse red at bind
            // time rather than baked in, so the one texture stays reusable if that red ever
            // retunes.
            Write("HUD_CostMark", CostMark(24, 8));

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

        /// <summary>
        /// The offer card: a riveted plate with a sunken translucent field for the card's contents.
        ///
        /// Translucent rather than opaque for the reason <see cref="Banner"/> is — the offer sits
        /// over the room the player is standing in, and four opaque slabs across the middle of the
        /// screen read as a different application rather than as a pause in this one. The full-screen
        /// scrim behind them is what makes the text legible; the card only has to be darker than it.
        /// </summary>
        private static Texture2D Card(int width, int height, int border, int chamfer)
        {
            var px = NewPixels(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Chamfered(x, y, width, height, chamfer)) continue;

                    int depth = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));

                    // The innermost border ring is an inverted bevel, so the field reads as sunk
                    // into the plate rather than painted onto it. Same trick as Slot.
                    Set(px, width, height, x, y,
                        depth > border ? CardField
                        : depth == border ? ChannelWall(x <= border || y <= border)
                        : PlateAt(x, y, width, height));
                }
            }

            // Inset 4 rather than the 2 a Slot uses: the chamfer eats the outer two pixels of each
            // corner, and Rivet only draws over existing ink, so a stud out there is silently
            // dropped instead of drawn.
            Rivet(px, width, height, 4, 4);
            Rivet(px, width, height, width - 6, 4);
            Rivet(px, width, height, 4, height - 6);
            Rivet(px, width, height, width - 6, height - 6);

            return Bake(px, width, height);
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

        /// <summary>A row of <paramref name="pips"/> filled squares, evenly spaced. The offer
        /// card's tier badge — near-white so <c>Image.color</c> tints it per <c>TierPalette</c>.</summary>
        private static Texture2D TierPips(int width, int height, int pips)
        {
            var px = NewPixels(width, height);

            const int marginX = 2, marginY = 1, gap = 2;
            int usable = width - marginX * 2 - gap * (pips - 1);
            int pipWidth = Mathf.Max(1, usable / pips);
            int pipHeight = height - marginY * 2;

            for (int p = 0; p < pips; p++)
            {
                int x0 = marginX + p * (pipWidth + gap);
                for (int y = 0; y < pipHeight; y++)
                {
                    for (int x = 0; x < pipWidth; x++)
                    {
                        Set(px, width, height, x0 + x, marginY + y, NearWhite);
                    }
                }
            }

            return Bake(px, width, height);
        }

        /// <summary>The Curse tier badge: a crossed bar rather than a pip count, drawn the same
        /// near-white as <see cref="TierPips"/> so it tints to the Curse red the same way.</summary>
        private static Texture2D TierCross(int width, int height)
        {
            var px = NewPixels(width, height);

            int cx = width / 2, cy = height / 2;
            int reach = height / 2 - 1;

            // A rule the badge's full width, struck through by the X. The X alone is as tall as the
            // badge but only as wide, so in the 64-unit box it left a small mark floating in empty
            // space while Common's single pip filled the same box edge to edge — two tiers of the
            // same badge reading at wildly different weights. Margin 2 is TierPips' own marginX, so
            // the rule ends exactly where a pip row does.
            const int margin = 2;

            for (int x = margin; x < width - margin; x++)
            {
                Set(px, width, height, x, cy, NearWhite);
                Set(px, width, height, x, cy - 1, NearWhite);
            }

            for (int d = -reach; d <= reach; d++)
            {
                Set(px, width, height, cx + d, cy + d, NearWhite);
                Set(px, width, height, cx + d, cy + d - 1, NearWhite);   // 2px thick
                Set(px, width, height, cx + d, cy - d, NearWhite);
                Set(px, width, height, cx + d, cy - d - 1, NearWhite);
            }

            return Bake(px, width, height);
        }

        /// <summary>A short rule with a small triangle at its head, marking where a Curse card's
        /// cost line starts. Near-white, tinted to the Curse red at bind time rather than baked
        /// in — see <see cref="NearWhite"/>.</summary>
        private static Texture2D CostMark(int width, int height)
        {
            var px = NewPixels(width, height);

            int ruleY = height / 2;
            const int triangleWidth = 6;

            for (int x = triangleWidth; x < width; x++) Set(px, width, height, x, ruleY, NearWhite);

            for (int x = 0; x < triangleWidth; x++)
            {
                int half = Mathf.Min(x, triangleWidth - 1 - x);
                for (int y = ruleY - half; y <= ruleY + half; y++) Set(px, width, height, x, y, NearWhite);
            }

            return Bake(px, width, height);
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
