using Deeper.Run;
using Deeper.UI;
using Deeper.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds the run's pause screen: a scrim, Resume and Abandon Run, and the grid of everything
    /// this run is carrying, with a hover popup naming each pick.
    ///
    /// A fourth root under the same canvas, beside <see cref="BuildRunHUD"/>'s,
    /// <see cref="BuildUpgradePanel"/>'s and <see cref="BuildRunSummaryPanel"/>'s, for the reason
    /// those files already give — `BuildRunHUD` deletes and rebuilds `RunHUD` wholesale, so anything
    /// built inside it vanishes on the next HUD tweak. All four share <see cref="HUDLayout"/> for
    /// the canvas contract, so they stay on one pixel grid.
    ///
    /// <b>Why this screen exists at all:</b> the readout of a run's picks used to be a strip down
    /// the left edge of the play screen, and a run takes an uncapped number of upgrades, so that
    /// column either grew off the screen or lied through its overflow count. The owner's call was to
    /// take it off the play screen entirely and show it where the game is already stopped. GDD §UI
    /// lists no pause screen — see the change brief.
    ///
    /// <b>Every number here is authored at half its on-screen size</b>, the rule
    /// `PixelPerfectHUDScale` imposes: the canvas scales by a whole number from a 540 reference. The
    /// sizes are chosen against the SHORT window rather than the reference one — below 1080 the
    /// factor clamps to 1, so a 906x463 Game view gives 906x463 units of canvas and the whole screen
    /// has to fit inside that.
    /// </summary>
    public static class BuildPauseMenu
    {
        private const string RootName = "PauseMenu";

        /// <summary>
        /// The grid, in slots. 48 is not a guess: <c>RunUpgrades.Add</c> refuses duplicates and the
        /// authored content is 38 upgrades and 8 Curses, so 46 is the most a run can physically
        /// reach. The overflow label below it stays anyway, as the thing that tells the truth if
        /// that content ever grows past this grid.
        /// </summary>
        private const int Columns = 8;
        private const int Rows = 6;

        /// <summary>
        /// The same square slot the weapon and dash use. Its 4-unit border makes the hole exactly
        /// 32, which draws a 128px upgrade icon at an exact 2:1 — an integer downsample, which is
        /// the whole point. The offer card gives the same icon a 64-unit hole and draws it 1:1;
        /// anything between the two resamples point-filtered art off its own grid.
        /// </summary>
        private static readonly Vector2 SlotSize = new Vector2(40f, 40f);

        private const float SlotBorder = 4f;
        private const float SlotGap = 6f;

        /// <summary>Button column width, matching the summary screen's Return button so the two
        /// modal screens are made of the same parts.</summary>
        private static readonly Vector2 ButtonSize = new Vector2(140f, 28f);

        private const float ButtonGap = 14f;

        /// <summary>Between the button column and the grid beside it.</summary>
        private const float ColumnGap = 24f;

        /// <summary>
        /// How far below screen centre the content band sits, leaving the header its room. The band
        /// is <see cref="GridSize"/> tall, so at -18 it spans +117 to -153 and the header at +150
        /// clears it — which is the constraint that binds at the 463-unit short window.
        /// </summary>
        private const float ContentDrop = -18f;

        private const float HeaderY = 150f;

        private static readonly Color Ink = new Color(0.88f, 0.89f, 0.93f, 1f);
        private static readonly Color Muted = new Color(0.60f, 0.62f, 0.69f, 1f);

        private static float GridWidth { get { return Columns * SlotSize.x + (Columns - 1) * SlotGap; } }
        private static float GridHeight { get { return Rows * SlotSize.y + (Rows - 1) * SlotGap; } }

        private static Vector2 GridSize { get { return new Vector2(GridWidth, GridHeight); } }

        private static float ContentWidth { get { return ButtonSize.x + ColumnGap + GridWidth; } }

        [MenuItem("Deeper/Build Pause Menu")]
        public static void Build()
        {
            Canvas canvas = HUDLayout.FindOrCreateCanvas();

            Transform existing = canvas.transform.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            RectTransform root = HUDLayout.NewRect(RootName, canvas.transform);
            HUDLayout.Stretch(root);

            // Over the run HUD and the offer, under the summary screen. BuildRunScene builds the
            // four in that order for exactly this reason: once a run is over there is nothing left
            // to pause, so the summary must be the one on top.
            root.SetAsLastSibling();

            // The panel is what toggles; the components live on the root so their Update and their
            // Awake keep running while it is hidden.
            RectTransform panel = HUDLayout.NewRect("Panel", root);
            HUDLayout.Stretch(panel);

            BuildScrim(panel);
            BuildHeader(panel);

            Button resume = BuildButton(panel, "Resume", "RESUME", ContentDrop + (ButtonSize.y + ButtonGap) * 0.5f);
            Button abandon = BuildButton(panel, "Abandon", "ABANDON RUN", ContentDrop - (ButtonSize.y + ButtonGap) * 0.5f);

            UpgradeListHUD list = BuildGrid(panel);

            // Built last, so the popup is the panel's last child and draws over every slot in the
            // grid rather than under the ones below it. The component goes on the root, outside the
            // panel, so it is never disabled - its Awake is what hides the popup in the first place.
            HUDLayout.Wire(list, "tooltip", HUDLayout.AddUpgradeTooltip(root, panel, panel));

            var menu = root.gameObject.AddComponent<PauseMenu>();
            HUDLayout.Wire(menu, "panel", panel.gameObject);
            HUDLayout.Wire(menu, "resumeButton", resume);
            HUDLayout.Wire(menu, "abandonButton", abandon);
            HUDLayout.Wire(menu, "abandonLabel", abandon.GetComponentInChildren<Text>(true));
            HUDLayout.Wire(menu, "pause", HUDLayout.EnsureRunPause(canvas));
            HUDLayout.Wire(menu, "run", Object.FindFirstObjectByType<RunEnd>());
            HUDLayout.Wire(menu, "offer", Object.FindFirstObjectByType<UpgradeOffer>(FindObjectsInactive.Include));
            HUDLayout.Wire(menu, "summary", Object.FindFirstObjectByType<RunSummaryPanel>(FindObjectsInactive.Include));

            // Starts hidden. It takes a RunPause hold when it opens, so a screen that came up on
            // Play would leave her unable to move until something closed it.
            panel.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("Built " + RootName + " under " + HUDLayout.CanvasName + ".", canvas);
            Selection.activeGameObject = root.gameObject;
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// The full-screen dim. Clickable, and that is the only reason it is: a click that lands
        /// between the buttons must be swallowed here rather than falling through to the world,
        /// where the katana would swing at whatever is under the cursor.
        /// </summary>
        private static void BuildScrim(RectTransform parent)
        {
            RectTransform scrim = HUDLayout.NewRect("Scrim", parent);
            HUDLayout.Stretch(scrim);
            HUDLayout.AddImage(scrim, null, new Color(0.02f, 0.02f, 0.03f, 0.82f), clickable: true);
        }

        private static void BuildHeader(RectTransform parent)
        {
            RectTransform rect = HUDLayout.NewRect("Header", parent);
            HUDLayout.AnchorCentreOffset(rect, new Vector2(ContentWidth, 20f), new Vector2(0f, HeaderY));

            Text text = HUDLayout.AddTextIn(rect, "PAUSED", HUDLayout.TitleText, TextAnchor.MiddleCenter);
            text.color = Ink;
        }

        /// <summary>
        /// One button, built the way <see cref="BuildRunSummaryPanel"/> builds its Return button —
        /// the square slot stretched into a face, tinted, with UGUI's own colour transition. Copied
        /// in shape rather than shared, because the two screens are laid out by different tools and
        /// a shared helper would make one of them depend on the other.
        /// </summary>
        private static Button BuildButton(RectTransform parent, string name, string label, float y)
        {
            RectTransform rect = HUDLayout.NewRect(name, parent);
            HUDLayout.AnchorCentreOffset(rect, ButtonSize,
                                         new Vector2(-ContentWidth * 0.5f + ButtonSize.x * 0.5f, y));

            Image face = HUDLayout.AddImage(rect, HUDLayout.Load("HUD_SlotSquare"),
                                            new Color(0.24f, 0.26f, 0.32f, 1f), clickable: true);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;

            var colours = button.colors;
            colours.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colours.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colours;

            RectTransform textRect = HUDLayout.NewRect("Label", rect);
            HUDLayout.Stretch(textRect);
            Text text = HUDLayout.AddTextIn(textRect, label, HUDLayout.BodyText, TextAnchor.MiddleCenter);
            text.color = Ink;

            return button;
        }

        /// <summary>
        /// The grid of taken picks. Slots are pre-built and switched on as picks arrive rather than
        /// instantiated, so a level-up — which already pauses and opens a panel — does no allocation
        /// on top of that.
        /// </summary>
        private static UpgradeListHUD BuildGrid(RectTransform parent)
        {
            Sprite slotArt = HUDLayout.Load("HUD_SlotSquare");

            RectTransform group = HUDLayout.NewRect("Upgrades", parent);
            HUDLayout.AnchorCentreOffset(group, GridSize,
                                         new Vector2(ContentWidth * 0.5f - GridWidth * 0.5f, ContentDrop));

            int count = Columns * Rows;
            var slotRoots = new GameObject[count];
            var frames = new Image[count];
            var icons = new Image[count];
            var picks = new Object[count];

            for (int i = 0; i < count; i++)
            {
                int column = i % Columns;
                int row = i / Columns;

                HUDLayout.PickSlot slot = HUDLayout.AddPickSlot(group, "Slot" + i, slotArt,
                                                                SlotSize, SlotBorder);
                HUDLayout.AnchorTopLeft(slot.Rect, SlotSize,
                                        new Vector2(column * (SlotSize.x + SlotGap),
                                                    -row * (SlotSize.y + SlotGap)));

                slotRoots[i] = slot.Root;
                frames[i] = slot.Frame;
                icons[i] = slot.Icon;
                picks[i] = slot.Pick;
            }

            RectTransform overflow = HUDLayout.NewRect("Overflow", group);
            HUDLayout.AnchorTopLeft(overflow, new Vector2(GridWidth, 14f),
                                    new Vector2(0f, -GridHeight - 6f));
            Text overflowLabel = HUDLayout.AddTextIn(overflow, string.Empty, HUDLayout.BodyText,
                                                     TextAnchor.MiddleRight);
            overflowLabel.color = Muted;

            var hud = group.gameObject.AddComponent<UpgradeListHUD>();
            HUDLayout.Wire(hud, "overflowLabel", overflowLabel);
            HUDLayout.Wire(hud, "upgrades", HUDLayout.PlayerPart<RunUpgrades>());
            HUDLayout.Wire(hud, "curses", HUDLayout.PlayerPart<RunCurses>());
            HUDLayout.Wire(hud, "dim", 1f);
            HUDLayout.WireArray(hud, "slotRoots", slotRoots);
            HUDLayout.WireArray(hud, "slots", frames);
            HUDLayout.WireArray(hud, "icons", icons);
            HUDLayout.WireArray(hud, "picks", picks);

            HUDLayout.WireTierPalette(hud);
            return hud;
        }

    }
}
