using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The run's upgrade strip, down the left edge — one slot per upgrade taken.
    ///
    /// **Owner-directed, and in no design doc.** GDD §UI lists no in-run readout of what a run is
    /// carrying; ART_DIRECTION §5 only covers the *offer* screen. It is here because a roguelike run
    /// is defined by its picks and the player has no other way to check what they took twenty
    /// minutes ago.
    ///
    /// Held deliberately faint (<see cref="dim"/>). It is a reference you consult, not a readout you
    /// track — at full opacity a growing column down the side of the screen competes with the fight
    /// for attention, which is exactly what the corner-anchored HUD in §5 is arranged to avoid.
    ///
    /// The slots are pre-built by <c>BuildRunHUD</c> and switched on as upgrades arrive rather than
    /// instantiated on the fly: a level-up already pauses and opens a panel, and allocating there is
    /// avoidable work at the one moment the frame budget is already spent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeListHUD : MonoBehaviour
    {
        [Header("Source — found by tag when empty")]
        [SerializeField] private RunUpgrades upgrades;

        [Header("Widgets")]
        [Tooltip("The whole slot object, switched on when an upgrade fills it. Separate from the " +
                 "frame below because a slot is a socket, an icon and a frame — hiding only the " +
                 "frame would leave its socket sitting there as an empty hole.")]
        [SerializeField] private GameObject[] slotRoots = new GameObject[0];

        [Tooltip("The frame Image of each slot, tinted to the upgrade's tier. Same order as slotRoots.")]
        [SerializeField] private Image[] slots = new Image[0];

        [Tooltip("The upgrade's own art, drawn inside its slot. Same length and order as slots.")]
        [SerializeField] private Image[] icons = new Image[0];

        [Tooltip("Shows '+3' when a run carries more upgrades than there are slots, so the strip " +
                 "never silently lies about how many were taken.")]
        [SerializeField] private Text overflowLabel;

        [Header("Look")]
        [Tooltip("Alpha the whole strip is drawn at. It is a reference, not a live readout, and at " +
                 "full opacity it competes with the fight.")]
        [Range(0f, 1f)]
        [SerializeField] private float dim = 0.65f;

        [Tooltip("ART_DIRECTION §5's offer-card colour coding, reused here so a tier reads the " +
                 "same in the strip as it did on the card it was picked from.")]
        [SerializeField] private Color common = new Color(0.78f, 0.79f, 0.82f, 1f);
        [SerializeField] private Color rare = new Color(0.42f, 0.60f, 0.86f, 1f);
        [SerializeField] private Color epic = new Color(0.66f, 0.45f, 0.86f, 1f);
        [SerializeField] private Color legendary = new Color(0.90f, 0.74f, 0.36f, 1f);

        private void Awake()
        {
            if (upgrades == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) upgrades = player.GetComponentInChildren<RunUpgrades>(true);
            }
        }

        private void OnEnable()
        {
            if (upgrades != null) upgrades.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (upgrades != null) upgrades.Changed -= Refresh;
        }

        private void Refresh()
        {
            int count = upgrades != null ? upgrades.Count : 0;

            for (int i = 0; i < slotRoots.Length; i++)
            {
                bool filled = i < count;

                // The whole slot object goes off, not just its colour: an empty slot outline down
                // the side of the screen would read as something the player is missing.
                if (slotRoots[i] != null) slotRoots[i].SetActive(filled);
                if (!filled) continue;

                UpgradeDefinition upgrade = upgrades.Taken[i];

                if (i < slots.Length && slots[i] != null)
                {
                    Color tint = ColourOf(upgrade.Tier);
                    tint.a = dim;
                    slots[i].color = tint;
                }

                if (i >= icons.Length || icons[i] == null) continue;

                icons[i].sprite = upgrade.Icon;

                // An Image with a null sprite draws a white box, so the icon is switched off rather
                // than left blank. No upgrade art exists yet; the tier-coloured slot is the fallback.
                icons[i].enabled = upgrade.Icon != null;
                icons[i].color = new Color(1f, 1f, 1f, dim);
            }

            if (overflowLabel == null) return;

            int hidden = count - slotRoots.Length;
            overflowLabel.text = hidden > 0 ? "+" + hidden : string.Empty;

            Color labelColour = overflowLabel.color;
            labelColour.a = dim;
            overflowLabel.color = labelColour;
        }

        private Color ColourOf(UpgradeTier tier)
        {
            switch (tier)
            {
                case UpgradeTier.Rare: return rare;
                case UpgradeTier.Epic: return epic;
                case UpgradeTier.Legendary: return legendary;
                default: return common;
            }
        }
    }
}
