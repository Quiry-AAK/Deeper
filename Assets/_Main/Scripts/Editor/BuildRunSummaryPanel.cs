using Deeper.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds the Death / Victory screen: a scrim, a title, the five figures GDD §UI asks for, and
    /// the Return to Hub button.
    ///
    /// A third root under the same canvas, beside <see cref="BuildRunHUD"/>'s and
    /// <see cref="BuildUpgradePanel"/>'s, for the reason that file already gives — `BuildRunHUD`
    /// deletes and rebuilds `RunHUD` wholesale, so anything built inside it vanishes on the next HUD
    /// tweak. All three share <see cref="HUDLayout"/> for the canvas contract, so they stay on one
    /// pixel grid.
    ///
    /// <b>Every number here is authored at half its on-screen size</b>, the rule
    /// `PixelPerfectHUDScale` imposes: the canvas scales by a whole number from a 540 reference, so a
    /// 240-unit card draws 480px wide at 1080p, and nothing finer than 1 authored unit can exist.
    /// Sizes are chosen against the SHORT window too — below 1080 the factor clamps to 1, so a
    /// 906x463 Game view gives 463 units of canvas height and the whole panel has to fit in it.
    /// </summary>
    public static class BuildRunSummaryPanel
    {
        private const string RootName = "RunSummaryPanel";

        /// <summary>
        /// The card. 240 wide because the longest line on it is the label column plus the value
        /// column: "SHARDS EARNED" is 13 characters at the pixel face's 7-unit advance = 91 units,
        /// the value column is 60, and 240 leaves 18 units of padding a side around the pair.
        ///
        /// 212 tall is the stack: title (26) + rule (6) + five rows at 18 (90) + the button (28) +
        /// the gaps between them. Under the 463-unit short-window height with room to spare, which
        /// is the constraint that actually binds.
        /// </summary>
        private static readonly Vector2 CardSize = new Vector2(240f, 212f);

        private const float Pad = 18f;

        /// <summary>One row of the 5x7 face plus its leading. Matches the upgrade card's line rhythm.</summary>
        private const float RowHeight = 18f;

        private static readonly Color Ink = new Color(0.88f, 0.89f, 0.93f, 1f);
        private static readonly Color Muted = new Color(0.60f, 0.62f, 0.69f, 1f);

        [MenuItem("Deeper/Build Run Summary Panel")]
        public static void Build()
        {
            Canvas canvas = HUDLayout.FindOrCreateCanvas();

            Transform existing = canvas.transform.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            RectTransform root = HUDLayout.NewRect(RootName, canvas.transform);
            HUDLayout.Stretch(root);

            // Last sibling, so it covers the HUD and the upgrade offer both. A run can end with an
            // offer still fading out, and the summary is the screen that has to win.
            root.SetAsLastSibling();

            RectTransform panel = HUDLayout.NewRect("Panel", root);
            HUDLayout.Stretch(panel);

            BuildScrim(panel);

            RectTransform card = HUDLayout.NewRect("Card", panel);
            HUDLayout.Centre(card, CardSize);
            HUDLayout.AddImage(card, HUDLayout.Load("HUD_Card"), Color.white, clickable: true);

            float y = -Pad;

            RectTransform titleRect = HUDLayout.NewRect("Title", card);
            HUDLayout.AnchorTopCentre(titleRect, new Vector2(CardSize.x - Pad * 2f, 26f), new Vector2(0f, y));
            Text title = HUDLayout.AddTextIn(titleRect, "YOU DIED", HUDLayout.TitleText, TextAnchor.MiddleCenter);
            y -= 26f + 8f;

            BuildRule(card, y);
            y -= 10f;

            Text depth = BuildRow(card, "Depth", "DEPTH REACHED", ref y);
            Text levels = BuildRow(card, "Levels", "LEVELS GAINED", ref y);
            Text shards = BuildRow(card, "Shards", "SHARDS EARNED", ref y);
            Text time = BuildRow(card, "Time", "RUN TIME", ref y);
            Text weapon = BuildRow(card, "Weapon", "WEAPON", ref y);

            Button back = BuildButton(card, "ReturnButton", "RETURN TO HUB");

            RunSummaryPanel summary = root.gameObject.AddComponent<RunSummaryPanel>();
            HUDLayout.Wire(summary, "panel", panel.gameObject);
            HUDLayout.Wire(summary, "title", title);
            HUDLayout.Wire(summary, "depthValue", depth);
            HUDLayout.Wire(summary, "levelsValue", levels);
            HUDLayout.Wire(summary, "shardsValue", shards);
            HUDLayout.Wire(summary, "timeValue", time);
            HUDLayout.Wire(summary, "weaponValue", weapon);
            HUDLayout.Wire(summary, "returnButton", back);
            HUDLayout.Wire(summary, "pause", HUDLayout.EnsureRunPause(canvas));

            // Hidden as authored. RunSummaryPanel.Awake hides it too, but a panel left visible in the
            // saved scene is a panel covering the Game view every time anybody opens it.
            panel.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas);
            Debug.Log("Built " + RootName + " under " + canvas.name + ".", canvas);
        }

        /// <summary>
        /// Full-screen dim, and **clickable on purpose**. It is what stops a click landing on the
        /// world behind: the Player action map is disabled by `RunPause`, but a UGUI raycast that
        /// passes straight through still reaches anything else with a collider under the cursor.
        /// </summary>
        private static void BuildScrim(RectTransform parent)
        {
            RectTransform scrim = HUDLayout.NewRect("Scrim", parent);
            HUDLayout.Stretch(scrim);
            HUDLayout.AddImage(scrim, null, new Color(0.02f, 0.02f, 0.04f, 0.72f), clickable: true);
        }

        private static void BuildRule(RectTransform card, float y)
        {
            RectTransform rule = HUDLayout.NewRect("Rule", card);
            HUDLayout.AnchorTopCentre(rule, new Vector2(CardSize.x - Pad * 2f, 1f), new Vector2(0f, y));
            HUDLayout.AddImage(rule, null, new Color(0.35f, 0.36f, 0.42f, 1f));
        }

        /// <summary>
        /// One label/value pair. Two labels rather than one formatted string, because the value is
        /// the only part that changes and a single centred line would make the numbers jitter
        /// left and right as their width changed.
        /// </summary>
        private static Text BuildRow(RectTransform card, string name, string label, ref float y)
        {
            RectTransform row = HUDLayout.NewRect(name, card);
            HUDLayout.AnchorTopCentre(row, new Vector2(CardSize.x - Pad * 2f, RowHeight), new Vector2(0f, y));

            RectTransform left = HUDLayout.NewRect("Label", row);
            HUDLayout.Stretch(left);
            Text caption = HUDLayout.AddTextIn(left, label, HUDLayout.BodyText, TextAnchor.MiddleLeft);
            caption.color = Muted;

            RectTransform right = HUDLayout.NewRect("Value", row);
            HUDLayout.Stretch(right);
            Text value = HUDLayout.AddTextIn(right, "-", HUDLayout.BodyText, TextAnchor.MiddleRight);
            value.color = Ink;

            y -= RowHeight;
            return value;
        }

        private static Button BuildButton(RectTransform card, string name, string label)
        {
            RectTransform rect = HUDLayout.NewRect(name, card);
            HUDLayout.AnchorBottomCentre(rect, new Vector2(140f, 28f), new Vector2(0f, Pad));

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
    }
}
