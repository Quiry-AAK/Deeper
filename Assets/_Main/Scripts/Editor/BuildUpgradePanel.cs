using Deeper.Character;
using Deeper.Player;
using Deeper.UI;
using Deeper.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds the level-up offer screen: a scrim, a header, three upgrade cards, the Curse card and
    /// the centred card a Secret Vault grant is presented on (ART_DIRECTION §5, GDD §UI).
    ///
    /// A separate tool from <see cref="BuildRunHUD"/>, and a separate root under the same canvas.
    /// <c>BuildRunHUD</c> deletes and rebuilds <c>RunHUD</c> wholesale every time it runs, so a panel
    /// built inside it would vanish on the next HUD tweak. Both tools share
    /// <see cref="HUDLayout"/> for the canvas contract, so the two roots stay on one pixel grid.
    ///
    /// <b>Every number here is authored at half its on-screen size</b> — the canvas scales by a whole
    /// number from a 540 reference, so a 152-unit card draws 304px wide at 1080p. The sizes are also
    /// chosen against the <i>short</i> window rather than the reference one: below 1080 the factor
    /// clamps to 1, so a 906x463 Game view gives 906x463 units of canvas and the 662-unit card row
    /// has to fit inside that.
    /// </summary>
    public static class BuildUpgradePanel
    {
        private const string RootName = "UpgradePanel";

        /// <summary>
        /// Card size. Neither number is taste. The width is set by text: 168 minus 10 units of
        /// padding a side leaves 148, and at the pixel face's 7-unit monospaced advance that is 21
        /// characters per line at <c>HUDLayout.BodyText</c>, which fits the longest authored detail
        /// word combined with its value at that size. The height is set by the stack below it — one
        /// top row carrying the category glyph and the tier badge side by side, the 72-unit icon
        /// socket, the name, and a 48-unit description block the Curse's cost mark and cost line
        /// share — 196 is what that comes to with the padding. Held at 196 through the owner's
        /// mockup pass: pairing the two badges onto one row freed the band the description block
        /// grew into, so nothing here had to grow.
        /// </summary>
        private static readonly Vector2 CardSize = new Vector2(168f, 196f);

        private const float CardGap = 12f;

        /// <summary>
        /// Wider than <see cref="CardGap"/>, because ART_DIRECTION §5 wants the Curse "never
        /// confused with a normal offer". The red frame does most of that; the gap is what keeps it
        /// from reading as the fourth item in a list of four.
        /// </summary>
        private const float CurseGap = 30f;

        /// <summary>How far below screen centre the card row sits, leaving the header its room.</summary>
        private const float RowDrop = -20f;

        /// <summary>Card padding, measured from the card's edge — so 4 units inside HUD_Card's
        /// 6-unit frame border.</summary>
        private const float Pad = 10f;

        // ---- Icon-led layout, top to bottom: the badge row, the icon slot, the name, then the
        // description block. Every y below is measured DOWN from the card's top edge. ----

        /// <summary>
        /// The effect-category glyph's socket, HUD_SlotGlyph — the glyph is the thing worth seeing
        /// first, and it sat bare on the card field until the owner called it "a bit naked"
        /// (2026-09-18). Built like the icon slot below it, socket plus steel frame, so the two
        /// read as one family.
        ///
        /// 38 units with a 3-unit border, so the hole is exactly 32: the Cat_*.png glyphs' 64px at
        /// the canvas's 2x, drawn 1:1. Not the HUD's 40-unit HUD_SlotSquare, which has the same
        /// hole: at 40 it has nowhere to go that clears both the card's border and the icon slot.
        /// Both numbers are named on both sides, as <see cref="SlotBorder"/> is.
        /// </summary>
        private static readonly Vector2 CategorySlotSize = new Vector2(38f, 38f);

        private const float CategorySlotBorder = 3f;

        /// <summary>
        /// The socket's top-left corner, on both axes — pinned to the card's corner rather than
        /// centred, so it heads a row with the tier badge beside it (owner's mockup, 2026-09-18).
        /// 8 leaves one unit of field inside HUD_Card's 6-unit border and its channel wall, and puts
        /// the socket's right edge at 46, two units clear of the icon slot, which starts at x 48.
        /// </summary>
        private const float CategorySlotInset = 8f;

        /// <summary>
        /// The tier pip badge, sharing the top row with the category glyph. Twice the 32x8 strip it
        /// was: at the old size it read as a sliver of chrome beside a 32-unit glyph, and the owner's
        /// mockup asks for a tier-coloured line with real presence. An Image draws a sprite at
        /// whatever rect size it is given, so the art is <b>re-emitted</b> at 64x16 by
        /// <c>HUDFrameArt</c> rather than stretched to fit — this number and that one move together.
        /// </summary>
        private static readonly Vector2 TierBadgeSize = new Vector2(64f, 16f);

        /// <summary>Gap between the category glyph's socket and the tier badge on the top row.</summary>
        private const float BadgeGap = 8f;

        /// <summary>
        /// The tier badge's x/y, derived rather than typed, so resizing either piece keeps the pair
        /// on one row: the badge starts past the glyph's socket, and is centred against the socket's
        /// taller band rather than aligned to its top edge.
        /// </summary>
        private static float TierBadgeX { get { return CategorySlotInset + CategorySlotSize.x + BadgeGap; } }

        private static float TierBadgeY
        {
            get { return CategorySlotInset + (CategorySlotSize.y - TierBadgeSize.y) * 0.5f; }
        }

        /// <summary>The dark plate behind both sockets' art, under a translucent frame hole.</summary>
        private static readonly Color SocketColour = new Color(0.07f, 0.07f, 0.09f, 0.7f);

        private const float SlotY = 46f;

        private const float NameY = 122f;

        /// <summary>
        /// The description block — "&lt;Detail&gt; &lt;Value&gt;", e.g. "ALL ATTACKS +3" — and the
        /// whole lower half of the card. <b>It wraps</b>, which the single line it replaced did not:
        /// at 148 units and <see cref="HUDLayout.BodyText"/>'s 7-unit advance a non-wrapping line
        /// holds 21 characters, and the seven authored entries over 30 characters were drawing some
        /// 30 units past each edge of the card.
        ///
        /// <b>One size, <see cref="HUDLayout.BodyText"/>, on every card.</b> It used to step up to
        /// <see cref="HUDLayout.TitleText"/> for any line short enough, which put two sizes side by
        /// side in one offer; see <c>UpgradeCard.DrawSummary</c>. What fits at that one size is
        /// published below (<see cref="SummaryLines"/> and its siblings) for
        /// <c>BuildUpgradeAssets</c> to check every authored line against.
        ///
        /// <see cref="CostMarkY"/> and <see cref="CostY"/> sit <i>inside</i> this block rather than
        /// under it. The three rects overlap on purpose: an upgrade owns all 48 units and centres
        /// its line in them, a Curse pins its upside to the top and leaves the lower 30 to the mark
        /// and cost line. One fixed block both kinds of card draw into beats resizing a rect at bind
        /// time, which would put layout arithmetic in a view that has none.
        /// </summary>
        private const float SummaryY = 138f;
        private const float SummaryHeight = 48f;

        /// <summary>Every text box on the card is the card minus a padding a side.</summary>
        private static float LabelWidth { get { return CardSize.x - Pad * 2f; } }

        private static readonly Vector2 CostMarkSize = new Vector2(24f, 8f);
        private const float CostMarkY = 156f;

        private const float CostY = 166f;
        private const float CostHeight = 20f;

        // ---- What each text box holds at HUDLayout.BodyText, which is the face's native size, so
        // PixelFontArt's own metrics apply 1:1. Read by BuildUpgradeAssets's fit check: derived
        // from the rects above rather than typed there, so resizing a box moves its budget too. ----

        /// <summary>Characters per line of any text box on the card: 148 / 7 = 21. The name is
        /// one such line and does not wrap.</summary>
        internal static int LineChars { get { return Mathf.FloorToInt(LabelWidth / PixelFontArt.Advance); } }

        /// <summary>Lines an upgrade's description may wrap to — the whole 48-unit block: 5.</summary>
        internal static int SummaryLines { get { return LinesIn(SummaryHeight); } }

        /// <summary>Lines a Curse's upside may wrap to — only down to its cost mark, not the whole
        /// block: 2.</summary>
        internal static int CurseUpsideLines { get { return LinesIn(CostMarkY - SummaryY); } }

        /// <summary>
        /// Lines a Curse's cost may wrap to — its own 20-unit box: 2. The rect is the limit rather
        /// than the card's inner edge a few units lower, because a third line ends 1 unit past
        /// HUD_Card's 6-unit border — Greed's Toll's 41-character cost line did exactly that.
        /// </summary>
        internal static int CostLines { get { return LinesIn(CostHeight); } }

        /// <summary>Whole lines of body text a box <paramref name="height"/> units tall holds —
        /// every line but the last needs its leading, the last only its ink.</summary>
        private static int LinesIn(float height)
        {
            return Mathf.FloorToInt((height - PixelFontArt.LineInk) / PixelFontArt.LineSpacing) + 1;
        }

        /// <summary>
        /// The icon socket, HUD_SlotIcon: 72 units with a 4-unit border, so the hole is 64 and
        /// a 128px icon lands on it 1:1 at the canvas's 2x. Both numbers are named on both
        /// sides — HUDFrameArt draws the slot and must not depend on this file.
        /// </summary>
        private static readonly Vector2 SlotSize = new Vector2(72f, 72f);

        private const float SlotBorder = 4f;

        [MenuItem("Deeper/Build Upgrade Panel")]
        public static void Build()
        {
            Canvas canvas = HUDLayout.FindOrCreateCanvas();

            Transform existing = canvas.transform.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            RectTransform root = HUDLayout.NewRect(RootName, canvas.transform);
            HUDLayout.Stretch(root);

            // Sibling order is draw order, and this must cover the run HUD. BuildRunHUD pushes its
            // own root to first for the same reason, so the two menu items can be run in either
            // order without one ending up on top of the other.
            root.SetAsLastSibling();

            // The panel object is what gets switched on and off; the component lives on the root, so
            // its Update keeps running to fade the pick flash after the panel itself is hidden.
            RectTransform panel = HUDLayout.NewRect("Panel", root);
            HUDLayout.Stretch(panel);

            CanvasGroup scrim = BuildScrim(panel);

            // One group over both lines, so the entrance fades them as one heading. Stretched, so
            // the two lines keep the centre-relative offsets they had when they sat on the panel.
            RectTransform heading = HUDLayout.NewRect("Heading", panel);
            HUDLayout.Stretch(heading);
            CanvasGroup headingGroup = AddFadeGroup(heading);

            // Above the card row, and clear of it at the SHORT window as well as the tall one:
            // below 1080 the canvas factor clamps to 1, so a 906x463 Game view has only 231
            // units above centre to fit the header into.
            Text header = BuildHeader(heading, "Header", HUDLayout.TitleText, 20f, 112f,
                                      new Color(0.90f, 0.90f, 0.93f, 1f));
            Text subheader = BuildHeader(heading, "Subheader", HUDLayout.BodyText, 11f, 92f,
                                         new Color(0.62f, 0.64f, 0.70f, 1f));

            RectTransform row = HUDLayout.NewRect("Cards", panel);
            HUDLayout.AnchorCentreOffset(row, new Vector2(RowWidth(), CardSize.y), new Vector2(0f, RowDrop));

            var cards = new Object[3];
            for (int i = 0; i < 3; i++) cards[i] = BuildCard(row, "Card" + i, CardX(i));

            UpgradeCard curse = BuildCard(row, "CurseCard", CardX(3));

            // Its own card, centred on the screen rather than in the row. A Secret Vault grant is one
            // card with nothing to choose between it and anything else, and reusing a row slot would
            // put that card 91 units off-centre with three empty gaps beside it.
            UpgradeCard grant = BuildCard(panel, "GrantCard", 0f, centred: true);

            // On the ROOT, after Panel — not inside it. UpgradeOffer hides Panel the moment a card
            // is picked, and a flash built inside it was switched off on the very frame it was
            // meant to show: the last pick of every offer went through with no flash at all.
            Image flash = BuildFlash(root);

            // The entrance, in deal order: the row left to right, the Curse, then the grant card.
            var reveal = root.gameObject.AddComponent<OfferReveal>();
            HUDLayout.Wire(reveal, "scrim", scrim);
            HUDLayout.Wire(reveal, "heading", headingGroup);
            HUDLayout.WireArray(reveal, "cards", new Object[]
            {
                CardRect(cards[0]), CardRect(cards[1]), CardRect(cards[2]), CardRect(curse), CardRect(grant),
            });

            UpgradeOffer offer = root.gameObject.AddComponent<UpgradeOffer>();
            HUDLayout.Wire(offer, "panel", panel.gameObject);
            HUDLayout.WireArray(offer, "cards", cards);
            HUDLayout.Wire(offer, "curseCard", curse);
            HUDLayout.Wire(offer, "grantCard", grant);
            HUDLayout.Wire(offer, "headerLabel", header);
            HUDLayout.Wire(offer, "subheaderLabel", subheader);
            HUDLayout.Wire(offer, "flash", flash);
            HUDLayout.Wire(offer, "experience", HUDLayout.PlayerPart<PlayerXP>());
            HUDLayout.Wire(offer, "upgrades", HUDLayout.PlayerPart<RunUpgrades>());
            HUDLayout.Wire(offer, "curses", HUDLayout.PlayerPart<RunCurses>());
            HUDLayout.Wire(offer, "loadout", HUDLayout.PlayerPart<RunLoadout>());
            HUDLayout.Wire(offer, "pool", AssetDatabase.LoadAssetAtPath<UpgradePool>(
                               "Assets/_Main/Data/Upgrades/UpgradePool_Shared.asset"));
            HUDLayout.Wire(offer, "cursePool", AssetDatabase.LoadAssetAtPath<CursePool>(
                               "Assets/_Main/Data/Curses/CursePool.asset"));
            HUDLayout.Wire(offer, "pause", HUDLayout.EnsureRunPause(canvas));
            HUDLayout.Wire(offer, "reveal", reveal);

            // The offer reads the palette too, for the pick flash colours.
            WirePalette(offer);

            // Starts hidden. It pauses the game and gates player input while open, so a panel that
            // came up on Play would leave her unable to move until something closed it.
            panel.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("Built the upgrade offer under " + HUDLayout.CanvasName + "/" + RootName + ".", canvas);
            Selection.activeGameObject = root.gameObject;
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// The full-screen dim behind the cards.
        ///
        /// It is a raycast target, and that is the only reason it is clickable: a click that lands
        /// between two cards must be swallowed here rather than falling through to the world, where
        /// the katana would swing at whatever is under the cursor.
        /// </summary>
        private static CanvasGroup BuildScrim(RectTransform parent)
        {
            RectTransform scrim = HUDLayout.NewRect("Scrim", parent);
            HUDLayout.Stretch(scrim);
            HUDLayout.AddImage(scrim, null, new Color(0.02f, 0.02f, 0.03f, 0.78f), clickable: true);

            // Faded by OfferReveal. Alpha only: a CanvasGroup's blocksRaycasts stays on, so the
            // scrim swallows clicks from its first frame even while it is still invisible.
            return AddFadeGroup(scrim);
        }

        /// <summary>
        /// The one thing OfferReveal animates besides position. A CanvasGroup rather than each
        /// Image's colour, because the cards set their own frame alpha on hover and a fade written
        /// into the same colour would fight it.
        /// </summary>
        private static CanvasGroup AddFadeGroup(RectTransform rect)
        {
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        private static RectTransform CardRect(Object card)
        {
            return ((UpgradeCard)card).GetComponent<RectTransform>();
        }

        private static Text BuildHeader(RectTransform parent, string name, int size, float height,
                                        float y, Color colour)
        {
            RectTransform rect = HUDLayout.NewRect(name, parent);
            HUDLayout.AnchorCentreOffset(rect, new Vector2(RowWidth(), height), new Vector2(0f, y));

            Text text = HUDLayout.AddTextIn(rect, string.Empty, size, TextAnchor.MiddleCenter);
            text.color = colour;
            return text;
        }

        /// <summary>
        /// One card. Children are added in draw order, and the frame goes <b>first</b> — unlike the
        /// run HUD's upgrade slots, where the frame is added last to cover its socket. This frame has
        /// a translucent interior, so drawing it over the text would tint every word on the card.
        /// </summary>
        private static UpgradeCard BuildCard(RectTransform parent, string name, float x, bool centred = false)
        {
            RectTransform card = HUDLayout.NewRect(name, parent);

            if (centred) HUDLayout.AnchorCentreOffset(card, CardSize, new Vector2(0f, RowDrop));
            else HUDLayout.AnchorCentreOffset(card, CardSize, new Vector2(x, 0f));

            Sprite frameArt = HUDLayout.Load("HUD_Card");
            RectTransform frameRect = HUDLayout.NewRect("Frame", card);
            HUDLayout.Stretch(frameRect);

            // The one clickable graphic on the card. The pointer hits this and UGUI bubbles the event
            // up to the Button and the UpgradeCard on the card root.
            Image frame = HUDLayout.AddImage(frameRect, frameArt, Color.white, clickable: true);

            RectTransform slot = HUDLayout.NewRect("Slot", card);
            HUDLayout.AnchorTopCentre(slot, SlotSize, new Vector2(0f, -SlotY));

            RectTransform socketRect = HUDLayout.NewRect("Socket", slot);
            HUDLayout.Inset(socketRect, Vector4.one * SlotBorder);
            Image socket = HUDLayout.AddImage(socketRect, null, SocketColour);

            // 72 minus a 4-unit border each side is a 64-unit hole, which draws a 128px icon 1:1
            // at the canvas's 2x. Upgrade icons are authored at 128 for exactly this box — and
            // 128 also lands on an exact quarter in the run HUD's 16-unit strip slot.
            RectTransform iconRect = HUDLayout.NewRect("Icon", slot);
            HUDLayout.Inset(iconRect, Vector4.one * SlotBorder);
            Image icon = HUDLayout.AddImage(iconRect, null, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            RectTransform slotFrame = HUDLayout.NewRect("SlotFrame", slot);
            HUDLayout.Stretch(slotFrame);
            HUDLayout.AddImage(slotFrame, HUDLayout.Load("HUD_SlotIcon"), Color.white);

            // The top row, left to right: the category glyph in its socket heads it, the tier badge
            // sits beside it. The socket is the icon slot's shape one size down — plate, glyph, then
            // the frame over both. Untinted glyph — it has its own drawn colour; the badge is
            // tinted at bind time, so it starts white here.
            RectTransform categorySlot = HUDLayout.NewRect("CategorySlot", card);
            HUDLayout.AnchorTopLeft(categorySlot, CategorySlotSize,
                                    new Vector2(CategorySlotInset, -CategorySlotInset));

            RectTransform categorySocket = HUDLayout.NewRect("Socket", categorySlot);
            HUDLayout.Inset(categorySocket, Vector4.one * CategorySlotBorder);
            HUDLayout.AddImage(categorySocket, null, SocketColour);

            RectTransform categoryRect = HUDLayout.NewRect("CategoryBadge", categorySlot);
            HUDLayout.Inset(categoryRect, Vector4.one * CategorySlotBorder);
            Image categoryBadge = HUDLayout.AddImage(categoryRect, null, Color.white);
            categoryBadge.enabled = false;

            RectTransform categoryFrame = HUDLayout.NewRect("SlotFrame", categorySlot);
            HUDLayout.Stretch(categoryFrame);
            HUDLayout.AddImage(categoryFrame, HUDLayout.Load("HUD_SlotGlyph"), Color.white);

            RectTransform tierRect = HUDLayout.NewRect("TierBadge", card);
            HUDLayout.AnchorTopLeft(tierRect, TierBadgeSize, new Vector2(TierBadgeX, -TierBadgeY));
            Image tierBadge = HUDLayout.AddImage(tierRect, null, Color.white);
            tierBadge.enabled = false;

            // The stack, measured off the top edge per the layout table in SummaryY's doc comment.
            // Name sits directly under the icon slot; the description block takes everything below
            // it, and wraps — see SummaryY for the overflow that non-wrapping line was causing.
            Text title = Label(card, "Name", 11f, -NameY, TextAnchor.MiddleCenter, false);
            Text summary = Label(card, "Summary", SummaryHeight, -SummaryY, TextAnchor.MiddleCenter, true);

            // Curse only. Built on every card so the height stays identical whichever kind of
            // offer it holds (same reasoning the old body/cost boxes used); UpgradeCard disables
            // both for an upgrade pick.
            RectTransform costMarkRect = HUDLayout.NewRect("CostMark", card);
            HUDLayout.AnchorTopCentre(costMarkRect, CostMarkSize, new Vector2(0f, -CostMarkY));
            Image costMark = HUDLayout.AddImage(costMarkRect, null, Color.white);
            costMark.enabled = false;

            Text cost = Label(card, "Cost", CostHeight, -CostY, TextAnchor.UpperCenter, true);
            cost.color = TierPalette.Default.Curse;

            AddFadeGroup(card);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            // No built-in transition: ColorTint multiplies into targetGraphic.color and would drag
            // every tier's border toward the same grey on hover. UpgradeCard does the highlight.
            button.transition = Selectable.Transition.None;

            var view = card.gameObject.AddComponent<UpgradeCard>();
            HUDLayout.Wire(view, "frame", frame);
            HUDLayout.Wire(view, "socket", socket);
            HUDLayout.Wire(view, "icon", icon);
            HUDLayout.Wire(view, "tierBadge", tierBadge);
            HUDLayout.Wire(view, "categoryBadge", categoryBadge);
            HUDLayout.Wire(view, "nameLabel", title);
            HUDLayout.Wire(view, "summaryLabel", summary);
            HUDLayout.Wire(view, "costMark", costMark);
            HUDLayout.Wire(view, "costLabel", cost);
            HUDLayout.Wire(view, "button", button);

            WirePalette(view);
            WireCardArt(view);

            return view;
        }

        /// <summary>A card text box. Always <see cref="HUDLayout.BodyText"/> — every line on the
        /// card shares one size, and the fit budgets above are computed for exactly that size.</summary>
        private static Text Label(RectTransform card, string name, float height, float y,
                                  TextAnchor align, bool wrap)
        {
            RectTransform rect = HUDLayout.NewRect(name, card);
            HUDLayout.AnchorTopCentre(rect, new Vector2(LabelWidth, height), new Vector2(0f, y));

            return HUDLayout.AddTextIn(rect, string.Empty, HUDLayout.BodyText, align, wrap);
        }

        /// <summary>
        /// The pick flash (ART_DIRECTION §6). Built disabled and never a raycast target — it covers
        /// the whole screen, and a transparent one that ate clicks would make the panel unusable.
        /// </summary>
        private static Image BuildFlash(RectTransform parent)
        {
            RectTransform rect = HUDLayout.NewRect("Flash", parent);
            HUDLayout.Stretch(rect);

            Image flash = HUDLayout.AddImage(rect, null, new Color(1f, 1f, 1f, 0f));
            flash.enabled = false;
            return flash;
        }

        /// <summary>
        /// Writes ART_DIRECTION §5's colours onto a card. They are serialized rather than const, so
        /// the tool has to put them there — a struct field left at its C# default would be five
        /// invisible blacks.
        /// </summary>
        private static void WirePalette(Object view)
        {
            TierPalette palette = TierPalette.Default;

            var so = new SerializedObject(view);
            SerializedProperty prop = so.FindProperty("palette");
            prop.FindPropertyRelative("Common").colorValue = palette.Common;
            prop.FindPropertyRelative("Rare").colorValue = palette.Rare;
            prop.FindPropertyRelative("Epic").colorValue = palette.Epic;
            prop.FindPropertyRelative("Legendary").colorValue = palette.Legendary;
            prop.FindPropertyRelative("Curse").colorValue = palette.Curse;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Writes the two icon-led lookups onto a card: the tier pip badges (drawn geometry, next
        /// to the other frame art in <c>Art/UI/</c>) and the category glyphs (generated art, in
        /// <c>Art/UI/Icons/</c> beside the unique upgrade icons). Keyed by field name == enum name
        /// == file name, so the coupling <see cref="CategoryGlyphs"/>'s doc comment describes holds
        /// all the way from the art folder to here.
        /// </summary>
        private static void WireCardArt(Object view)
        {
            var so = new SerializedObject(view);

            SerializedProperty badges = so.FindProperty("tierBadges");
            badges.FindPropertyRelative("Common").objectReferenceValue = HUDLayout.Load("HUD_TierCommon");
            badges.FindPropertyRelative("Rare").objectReferenceValue = HUDLayout.Load("HUD_TierRare");
            badges.FindPropertyRelative("Epic").objectReferenceValue = HUDLayout.Load("HUD_TierEpic");
            badges.FindPropertyRelative("Legendary").objectReferenceValue = HUDLayout.Load("HUD_TierLegendary");
            badges.FindPropertyRelative("Curse").objectReferenceValue = HUDLayout.Load("HUD_TierCurse");

            SerializedProperty glyphs = so.FindProperty("categoryGlyphs");
            glyphs.FindPropertyRelative("Health").objectReferenceValue = LoadIcon("Cat_Health");
            glyphs.FindPropertyRelative("Defense").objectReferenceValue = LoadIcon("Cat_Defense");
            glyphs.FindPropertyRelative("Damage").objectReferenceValue = LoadIcon("Cat_Damage");
            glyphs.FindPropertyRelative("OnHit").objectReferenceValue = LoadIcon("Cat_OnHit");
            glyphs.FindPropertyRelative("Movement").objectReferenceValue = LoadIcon("Cat_Movement");
            glyphs.FindPropertyRelative("Dash").objectReferenceValue = LoadIcon("Cat_Dash");
            glyphs.FindPropertyRelative("Experience").objectReferenceValue = LoadIcon("Cat_Experience");
            glyphs.FindPropertyRelative("HeavyStrike").objectReferenceValue = LoadIcon("Cat_HeavyStrike");
            glyphs.FindPropertyRelative("Combo").objectReferenceValue = LoadIcon("Cat_Combo");
            glyphs.FindPropertyRelative("Gauge").objectReferenceValue = LoadIcon("Cat_Gauge");
            glyphs.FindPropertyRelative("Ultimate").objectReferenceValue = LoadIcon("Cat_Ultimate");
            glyphs.FindPropertyRelative("Utility").objectReferenceValue = LoadIcon("Cat_Utility");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The category glyphs live beside the unique upgrade icons in <c>Art/UI/Icons/</c>, not
        /// next to the drawn HUD chrome <see cref="HUDLayout.Load"/> reads from — its own loader for
        /// the same reason <c>BuildUpgradeAssets.Icon</c> has one.
        /// </summary>
        private static Sprite LoadIcon(string file)
        {
            string path = "Assets/_Main/Art/UI/Icons/" + file + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                Debug.LogWarning("Missing category glyph " + path + " — the card will build without it.");
            }

            return sprite;
        }

        // ---------------------------------------------------------------- arithmetic

        private static float RowWidth()
        {
            return CardSize.x * 4f + CardGap * 2f + CurseGap;
        }

        /// <summary>Centre of card <paramref name="index"/>, with 3 being the Curse.</summary>
        private static float CardX(int index)
        {
            float left = -RowWidth() * 0.5f + CardSize.x * 0.5f;
            float step = CardSize.x + CardGap;

            return index < 3
                ? left + step * index
                : left + step * 2f + CardSize.x + CurseGap;
        }
    }
}
