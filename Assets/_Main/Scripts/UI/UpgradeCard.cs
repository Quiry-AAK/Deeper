using System;
using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// One card on the offer screen — icon-led: a top row carrying the category glyph and the tier
    /// pip badge, the unique icon under it, the name, then a compressed description block in place
    /// of the tier word and the full prose description.
    ///
    /// This is the view half of the split <c>StatBar</c> established: the binder
    /// (<see cref="UpgradeOffer"/>) holds the sources and decides what is on offer, this holds the
    /// pixels. It knows how to draw an upgrade or a Curse and nothing about where either came from.
    ///
    /// It draws both because they are the same card shape with a different border colour and an
    /// extra cost line — exactly what ART_DIRECTION §5 asks for, "a 4th Curse card visually distinct
    /// with a red/black treatment so it's never confused with a normal offer". A second component
    /// would duplicate the whole layout to change one tint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Widgets")]
        [Tooltip("The card frame, tinted to the tier. This is the border ART_DIRECTION §5 colour-codes.")]
        [SerializeField] private Image frame;

        [Tooltip("The dark plate behind the icon. Separate from the frame so the tier tint does not " +
                 "wash the icon's own colours.")]
        [SerializeField] private Image socket;

        [SerializeField] private Image icon;

        [Tooltip("Common/Rare/Epic/Legendary/Curse as a small pip badge, tinted by the tier palette " +
                 "— replaces the spelled-out tier word, which was redundant with the already " +
                 "tier-coloured frame.")]
        [SerializeField] private Image tierBadge;

        [Tooltip("The effect-category glyph (Cat_Health, Cat_Damage, ...). Untinted — it has its " +
                 "own drawn colour, unlike the tier badge.")]
        [SerializeField] private Image categoryBadge;

        [SerializeField] private Text nameLabel;

        [Tooltip("The compressed description — \"All attacks +3\", detail then value so it reads " +
                 "like a sentence. Wraps, and its box is the card's whole lower half. For a Curse " +
                 "this is the upside only; the cost goes below, inside the same box.")]
        [SerializeField] private Text summaryLabel;

        [Tooltip("Curse only — a small crimson rule+triangle marking where the cost line starts.")]
        [SerializeField] private Image costMark;

        [Tooltip("A Curse's downside, compressed, in the Curse red. Blank on an upgrade. Held apart " +
                 "because every Curse is a trade, and one line carrying both halves lets the eye " +
                 "skip the half that hurts.")]
        [SerializeField] private Text costLabel;

        [SerializeField] private Button button;

        [Header("Look")]
        [SerializeField] private TierPalette palette;

        [SerializeField] private CategoryGlyphs categoryGlyphs;
        [SerializeField] private TierBadges tierBadges;

        [Tooltip("Alpha the frame is drawn at when this card is not the one under the cursor. The " +
                 "hovered card comes up to full, which is the only hover affordance the card has — " +
                 "there is no scale or motion, because a pixel-art card scaled by a fraction of a " +
                 "pixel resamples off its own grid.")]
        [Range(0f, 1f)]
        [SerializeField] private float restAlpha = 0.82f;

        /// <summary>Raised when this card is chosen. The binder decides what that means.</summary>
        public event Action<UpgradeCard> Picked;

        /// <summary>What this card is currently offering, or null when it holds a Curse.</summary>
        public UpgradeDefinition Upgrade { get; private set; }

        /// <summary>The Curse this card is offering, or null when it holds an upgrade.</summary>
        public CurseDefinition Curse { get; private set; }

        private void Reset()
        {
            palette = TierPalette.Default;
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Pick);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Pick);
        }

        /// <summary>Draws an upgrade offer.</summary>
        public void Bind(UpgradeDefinition upgrade)
        {
            Upgrade = upgrade;
            Curse = null;

            if (upgrade == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            Color tint = palette.ColourOf(upgrade.Tier);

            Paint(tint, tierBadges.Of(upgrade.Tier));
            SetIcon(upgrade.Icon);

            if (nameLabel != null) nameLabel.text = Caps(upgrade.DisplayName);

            DrawSummary(upgrade.Summary, sharedWithCost: false);

            // Cleared rather than hidden: the object stays enabled so the card's text block keeps
            // the same height whichever kind of offer it is holding.
            if (costMark != null) costMark.enabled = false;
            if (costLabel != null) costLabel.text = string.Empty;
        }

        /// <summary>Draws the always-visible 4th slot's Curse (CORE_SYSTEMS §9).</summary>
        public void Bind(CurseDefinition curse)
        {
            Curse = curse;
            Upgrade = null;

            if (curse == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            Paint(palette.Curse, tierBadges.Curse);
            SetIcon(curse.Icon);

            if (nameLabel != null) nameLabel.text = Caps(curse.DisplayName);

            DrawSummary(curse.UpsideSummary, sharedWithCost: true);

            // Drawn near-white (see HUDFrameArt.NearWhite) and tinted here, the same trick the tier
            // badge uses, so one texture stays reusable if the Curse red ever retunes.
            if (costMark != null)
            {
                costMark.enabled = true;
                costMark.color = palette.Curse;
            }

            if (costLabel != null)
            {
                costLabel.text = Caps(curse.CostLine);
                costLabel.color = palette.Curse;
            }
        }

        /// <summary>Empties and hides the card.</summary>
        public void Clear()
        {
            Upgrade = null;
            Curse = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Chooses this card. Wired onto the <see cref="button"/>, and public so a probe can drive it
        /// — a simulated pointer click is the one input this environment cannot reliably deliver
        /// (Engineering/01-VERIFICATION.md §2), and a card nobody can press is a card nobody can test.
        /// </summary>
        [ContextMenu("Pick")]
        public void Pick()
        {
            if (Upgrade == null && Curse == null) return;

            if (Picked != null) Picked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHighlighted(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false);
        }

        /// <summary>
        /// Whether the pointer is over this card. Handled here rather than through the Button's own
        /// ColorTint transition, which multiplies its tint into the frame's colour and would drag
        /// every tier's border toward the same grey on hover.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (frame == null) return;

            Color colour = frame.color;
            colour.a = highlighted ? 1f : restAlpha;
            frame.color = colour;
        }

        /// <summary>Frame tint plus the tier pip badge — the two things that say "how rare".</summary>
        private void Paint(Color tint, Sprite badge)
        {
            if (frame != null)
            {
                Color frameColour = tint;
                frameColour.a = restAlpha;
                frame.color = frameColour;
            }

            if (tierBadge == null) return;

            tierBadge.sprite = badge;

            // Null-sprite guard — see SetIcon. A badge left unmapped for some future tier must not
            // draw a solid white pip.
            tierBadge.enabled = badge != null;

            Color badgeColour = tint;
            badgeColour.a = 1f;
            tierBadge.color = badgeColour;
        }

        /// <summary>
        /// The category glyph plus the combined description — the two things that say "what it
        /// does". Shared by both <c>Bind</c> overloads so an upgrade and a Curse's upside draw
        /// identically.
        ///
        /// <paramref name="sharedWithCost"/> is the whole difference between them: a Curse's cost
        /// mark and cost line sit in the lower part of this same block, so its upside is pinned to
        /// the top instead of centred.
        ///
        /// <b>The font size is never touched here.</b> The label draws at the one size
        /// <c>BuildUpgradePanel</c> authored it at, on every card. It used to switch up to 14 for
        /// any line that fit two 10-character lines, which drew the cards in one offer at two
        /// different sizes side by side (owner, 2026-09-18: "the font sizes are changing"). Only
        /// whole multiples of the face's native 7 stay on the pixel grid, and 14 fits so few of the
        /// authored lines that the one size everything shares is 7. Lines are held to that size's
        /// capacity by <c>BuildUpgradeAssets</c>'s fit check instead.
        /// </summary>
        private void DrawSummary(EffectSummary summary, bool sharedWithCost)
        {
            if (categoryBadge != null)
            {
                Sprite glyph = categoryGlyphs.Of(summary.Category);
                categoryBadge.sprite = glyph;

                // Null-sprite guard — see SetIcon. A category with no glyph yet must not draw a
                // solid white box over the card.
                categoryBadge.enabled = glyph != null;
            }

            if (summaryLabel == null) return;

            summaryLabel.alignment = sharedWithCost ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            summaryLabel.text = Caps(summary.Line);
        }

        /// <summary>
        /// Uppercases a card label. The HUD face is an uppercase bitmap, and every lowercase letter
        /// aliases onto its capital — <b>except <c>x</c>, which is authored for real</b> because
        /// "2x damage taken" and "3 damage x 3 ticks" need a multiplier sign. That one exception
        /// made <c>Max HP</c> draw as <c>MAx HP</c>, a small letter in the middle of a 14pt line
        /// that reads as a typo, and the same in the names Executioner and Explosive Finish.
        ///
        /// Done here rather than by authoring those four strings with a capital X, which would put
        /// <c>EXecutioner</c> into the data every prose reader shares. Everywhere else it changes
        /// nothing — the face was already drawing those letters as capitals.
        ///
        /// <b><c>ToUpperInvariant</c>, never <c>ToUpper</c>.</b> This project is developed under a
        /// Turkish locale, where <c>ToUpper</c> maps <c>i</c> to the dotted <c>İ</c> — which is not
        /// in <c>PixelFontGlyphs.Order</c> and would render as a hole in every word containing an i.
        /// </summary>
        private static string Caps(string text)
        {
            return text != null ? text.ToUpperInvariant() : string.Empty;
        }

        private void SetIcon(Sprite sprite)
        {
            if (icon == null) return;

            icon.sprite = sprite;

            // An Image with a null sprite draws a white box rather than nothing — the defect that
            // shipped as a solid white weapon slot. The tier-coloured frame is the fallback.
            icon.enabled = sprite != null;

            if (socket != null) socket.enabled = true;
        }
    }
}
