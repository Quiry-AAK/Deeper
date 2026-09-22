using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The popup a taken-pick icon raises on hover: the upgrade's name as a header, its description
    /// underneath (owner, 2026-09-20).
    ///
    /// This is where the words went when the readout became icons alone. <c>UpgradeDefinition</c>
    /// already carries the full authored prose CONTENT_DESIGN writes — the same string
    /// <c>UpgradeStatusReport</c> reads — so nothing new is authored for it. The offer card's
    /// compressed <c>EffectSummary</c> line is deliberately NOT what is shown here: that line exists
    /// because a card is 21 characters wide, and the popup has the room the card never did.
    ///
    /// A Curse shows its upside and then its cost, the cost in the Curse red, held apart for the
    /// reason <c>CurseDefinition</c> holds the two fields apart: every Curse is a trade, and one
    /// block carrying both lets the eye skip the half that hurts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeTooltip : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The whole popup, switched off when nothing is hovered. Separate from this " +
                 "component's own object so the component keeps running while it is hidden — a " +
                 "script on the object it hides could never show itself again.")]
        [SerializeField] private GameObject panel;

        [SerializeField] private Text headerLabel;

        [Tooltip("The upgrade's description, or a Curse's upside.")]
        [SerializeField] private Text bodyLabel;

        [Tooltip("A Curse's cost only, drawn in the Curse red under the upside. Switched off for " +
                 "an upgrade, which has no second half.")]
        [SerializeField] private Text costLabel;

        [Header("Placement")]
        [Tooltip("The rect the popup is positioned inside and clamped against — normally the panel " +
                 "it belongs to, stretched over the whole canvas.")]
        [SerializeField] private RectTransform bounds;

        [Tooltip("Gap between the hovered slot and the popup beside it.")]
        [SerializeField] private float gap = 8f;

        [Tooltip("How close the popup may come to the edge of its bounds before being pushed back " +
                 "in. Matches the HUD's own screen margin.")]
        [SerializeField] private float edgePadding = 14f;

        [Header("Look")]
        [Tooltip("ART_DIRECTION §5's tier colours, shared with the offer card so a tier reads the " +
                 "same in the popup as it did on the card it was picked from.")]
        [SerializeField] private TierPalette palette;

        [Tooltip("The description's colour. Paler than the header, which carries the tier.")]
        [SerializeField] private Color bodyColour = new Color(0.80f, 0.81f, 0.84f, 1f);

        private RectTransform _rect;
        private UpgradeSlot _shownFor;

        private void Reset()
        {
            palette = TierPalette.Default;
        }

        private void Awake()
        {
            _rect = panel != null ? panel.GetComponent<RectTransform>() : null;
            Hide();
        }

        /// <summary>
        /// Subscribes to every slot in a readout.
        ///
        /// Driven from <see cref="UpgradeListHUD"/> rather than each slot finding a popup itself:
        /// both panels that carry a readout have their own popup, and a slot searching the scene
        /// would find whichever happened to be first.
        /// </summary>
        public void Watch(UpgradeSlot[] slots)
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                // Removed first, so re-wiring a readout cannot subscribe the same slot twice. A
                // doubled handler is invisible until the day one of them is removed and the popup
                // half-unsubscribes.
                slots[i].Hovered -= Show;
                slots[i].Unhovered -= Dismiss;
                slots[i].Hovered += Show;
                slots[i].Unhovered += Dismiss;
            }
        }

        public void Show(UpgradeSlot slot)
        {
            if (slot == null || panel == null) return;

            if (slot.Upgrade != null) Fill(slot.Upgrade);
            else if (slot.Curse != null) Fill(slot.Curse);
            else return;

            _shownFor = slot;
            panel.SetActive(true);
            PlaceBeside(slot.GetComponent<RectTransform>());
        }

        /// <summary>
        /// Closes the popup, but only when <paramref name="slot"/> is the one it is showing.
        ///
        /// The pointer crossing from one icon straight onto its neighbour raises the new slot's
        /// enter BEFORE the old slot's exit, so an unconditional hide here would shut the popup
        /// that had just opened and the player would see it flicker off along a row of icons.
        /// </summary>
        public void Dismiss(UpgradeSlot slot)
        {
            if (_shownFor != null && _shownFor != slot) return;

            Hide();
        }

        public void Hide()
        {
            _shownFor = null;
            if (panel != null) panel.SetActive(false);
        }

        private void Fill(UpgradeDefinition upgrade)
        {
            SetText(headerLabel, upgrade.DisplayName, palette.ColourOf(upgrade.Tier));
            SetText(bodyLabel, upgrade.Description, bodyColour);

            if (costLabel != null) costLabel.gameObject.SetActive(false);
        }

        private void Fill(CurseDefinition curse)
        {
            SetText(headerLabel, curse.DisplayName, palette.Curse);
            SetText(bodyLabel, curse.Upside, bodyColour);

            if (costLabel == null) return;

            costLabel.gameObject.SetActive(true);
            SetText(costLabel, curse.Downside, palette.Curse);
        }

        private static void SetText(Text label, string content, Color colour)
        {
            if (label == null) return;

            // Upper-cased for the reason the cards are: the 5x7 face has one case and draws lower
            // case as capitals anyway, so doing it here lets the authored strings stay readable in
            // the Inspector instead of being SHOUTED in the asset.
            label.text = content != null ? content.ToUpperInvariant() : string.Empty;
            label.color = colour;
        }

        /// <summary>
        /// Puts the popup beside the hovered slot, on whichever side it fits, clamped inside
        /// <see cref="bounds"/>.
        ///
        /// It flips rather than only clamping: a slot on the right-hand edge of the grid has no room
        /// to its right, and a popup that was merely clamped would slide back over the very icon the
        /// player is pointing at.
        /// </summary>
        private void PlaceBeside(RectTransform slot)
        {
            if (slot == null || _rect == null || bounds == null) return;

            Vector2 half = _rect.rect.size * 0.5f;
            Vector2 slotHalf = slot.rect.size * 0.5f;
            Vector2 limit = bounds.rect.size * 0.5f - half - Vector2.one * edgePadding;

            // Both rects are on the same canvas with no rotation, so the slot's centre in the
            // bounds' own space is a plain transform of its position. No screen-point round trip:
            // that needs the canvas's camera, and a Screen Space - Overlay canvas has none.
            Vector2 centre = bounds.InverseTransformPoint(slot.position);

            float right = centre.x + slotHalf.x + gap + half.x;
            float left = centre.x - slotHalf.x - gap - half.x;

            Vector2 position;
            position.x = right <= limit.x ? right : left;
            position.y = centre.y;

            // Still clamped after the flip, as a backstop: in a window too narrow for the popup on
            // either side, over the icon beats off the screen.
            position.x = Mathf.Clamp(position.x, -limit.x, limit.x);
            position.y = Mathf.Clamp(position.y, -limit.y, limit.y);

            _rect.anchoredPosition = position;
        }
    }
}
