using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The readout of everything a run is carrying — one socket per upgrade and Curse taken.
    ///
    /// **Owner-directed, and in no design doc.** GDD §UI lists no in-run readout of a run's picks;
    /// ART_DIRECTION §5 only covers the *offer* screen. It is here because a roguelike run is
    /// defined by its picks and the player has no other way to check what they took twenty minutes
    /// ago.
    ///
    /// **It is not on the play screen, and that is the point** (owner, 2026-09-20). It used to be a
    /// strip down the left edge under the health bar. A run takes an uncapped number of upgrades,
    /// so that column either grew off the screen or started lying through its overflow count. Two
    /// panels mount this instead, both of them places where the game is already stopped: the pause
    /// menu, as a grid, and the level-up offer, as a compact strip along its top band.
    ///
    /// **Icons alone.** The name and the description live in <see cref="UpgradeTooltip"/>, raised
    /// by hovering a socket — which is what lets one row hold a dozen picks instead of three.
    ///
    /// The slots are pre-built by the layout tool and switched on as picks arrive rather than
    /// instantiated on the fly: a level-up already pauses and opens a panel, and allocating there
    /// is avoidable work at the one moment the frame budget is already spent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeListHUD : MonoBehaviour
    {
        [Header("Sources — found by tag when empty")]
        [SerializeField] private RunUpgrades upgrades;

        [Tooltip("Curses appear in the strip after the upgrades, in the Curse red. Without this a " +
                 "player who took a Curse sees the panel close and nothing change anywhere, which " +
                 "reads as the pick having failed rather than as an effect that is not built yet.")]
        [SerializeField] private RunCurses curses;

        [Header("Widgets")]
        [Tooltip("The whole slot object, switched on when a pick fills it. Separate from the " +
                 "frame below because a slot is a socket, an icon and a frame — hiding only the " +
                 "frame would leave its socket sitting there as an empty hole.")]
        [SerializeField] private GameObject[] slotRoots = new GameObject[0];

        [Tooltip("The frame Image of each slot, tinted to the pick's tier. Same order as slotRoots.")]
        [SerializeField] private Image[] slots = new Image[0];

        [Tooltip("The pick's own art, drawn inside its slot. Same length and order as slots.")]
        [SerializeField] private Image[] icons = new Image[0];

        [Tooltip("Which pick each slot is currently showing, so a hover can name it. Same " +
                 "length and order as slots.")]
        [SerializeField] private UpgradeSlot[] picks = new UpgradeSlot[0];

        [Tooltip("The popup those slots raise on hover. One per panel — this readout is mounted " +
                 "twice, and a slot finding its own popup would find whichever was first in the " +
                 "scene rather than the one beside it.")]
        [SerializeField] private UpgradeTooltip tooltip;

        [Tooltip("Shows '+3' when a run carries more picks than there are slots, so the readout " +
                 "never silently lies about how many were taken.")]
        [SerializeField] private Text overflowLabel;

        [Header("Look")]
        [Tooltip("Alpha the whole readout is drawn at. 1 on the pause menu, where it is the thing " +
                 "the player came to look at; held back on the offer screen, where the cards are. " +
                 "It was 0.65 everywhere while this lived over the play area and had to stay out " +
                 "of the fight's way — neither panel it mounts on now has a fight behind it.")]
        [Range(0f, 1f)]
        [SerializeField] private float dim = 1f;

        [Tooltip("ART_DIRECTION §5's offer-card colour coding, shared with the offer card so a tier " +
                 "reads the same in the strip as it did on the card it was picked from.")]
        [SerializeField] private TierPalette palette;

        private void Reset()
        {
            palette = TierPalette.Default;
        }

        private void Awake()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (upgrades == null) upgrades = player.GetComponentInChildren<RunUpgrades>(true);
            if (curses == null) curses = player.GetComponentInChildren<RunCurses>(true);
        }

        private void OnEnable()
        {
            // Here rather than in Awake: a panel that is rebuilt or re-enabled hands out fresh
            // slots, and Watch is written to be safe to call twice.
            if (tooltip != null) tooltip.Watch(picks);

            if (upgrades != null) upgrades.Changed += Refresh;
            if (curses != null) curses.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (upgrades != null) upgrades.Changed -= Refresh;
            if (curses != null) curses.Changed -= Refresh;
        }

        private void Refresh()
        {
            int upgradeCount = upgrades != null ? upgrades.Count : 0;
            int curseCount = curses != null ? curses.Count : 0;
            int count = upgradeCount + curseCount;

            for (int i = 0; i < slotRoots.Length; i++)
            {
                bool filled = i < count;

                // The whole slot object goes off, not just its colour: an empty slot outline down
                // the side of the screen would read as something the player is missing.
                if (slotRoots[i] != null) slotRoots[i].SetActive(filled);

                if (!filled)
                {
                    // Emptied as well as hidden. A hidden slot cannot be hovered, but one switched
                    // back on by a later pick would carry the previous run's binding into the frame
                    // before Refresh reaches it.
                    if (i < picks.Length && picks[i] != null) picks[i].Clear();
                    continue;
                }

                // Upgrades first, then Curses. Two lists rather than one because they are different
                // types held by different components — the run's Curses carry no stat modifiers, so
                // merging them into RunUpgrades would mean a list whose entries mean two things.
                bool isCurse = i >= upgradeCount;

                Sprite art = isCurse ? curses.Taken[i - upgradeCount].Icon : upgrades.Taken[i].Icon;
                Color tint = isCurse ? palette.Curse : palette.ColourOf(upgrades.Taken[i].Tier);

                if (i < picks.Length && picks[i] != null)
                {
                    if (isCurse) picks[i].Show(curses.Taken[i - upgradeCount]);
                    else picks[i].Show(upgrades.Taken[i]);
                }

                if (i < slots.Length && slots[i] != null)
                {
                    tint.a = dim;
                    slots[i].color = tint;
                }

                if (i >= icons.Length || icons[i] == null) continue;

                icons[i].sprite = art;

                // An Image with a null sprite draws a white box, so the icon is switched off rather
                // than left blank. The tier-coloured slot is the fallback for anything unauthored.
                icons[i].enabled = art != null;
                icons[i].color = new Color(1f, 1f, 1f, dim);
            }

            if (overflowLabel == null) return;

            int hidden = count - slotRoots.Length;
            overflowLabel.text = hidden > 0 ? "+" + hidden : string.Empty;

            Color labelColour = overflowLabel.color;
            labelColour.a = dim;
            overflowLabel.color = labelColour;
        }
    }
}
