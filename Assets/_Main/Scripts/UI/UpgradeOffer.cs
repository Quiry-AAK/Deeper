using System.Collections.Generic;
using Deeper.Character;
using Deeper.Core;
using Deeper.Player;
using Deeper.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The level-up upgrade offer (GDD §Core Loop 4, CORE_SYSTEMS §12) — "the game pauses and
    /// presents an upgrade offer: 3 randomized upgrades drawn from the full shared + weapon pool in
    /// one weighted draw, plus a visible 4th Curse option".
    ///
    /// This is the binder half of the split: it owns the sources, decides what is on offer, and hands
    /// each <see cref="UpgradeCard"/> something to draw.
    ///
    /// <b>What it deliberately does not have:</b> a reroll and a skip. Neither appears in any design
    /// doc, and adding either would be inventing a mechanic (Design Rules 11/12). The Curse is
    /// declinable because §3 says it is — by taking one of the three upgrades instead — but the three
    /// upgrades themselves are not.
    ///
    /// <b>Every 5th level should be an Evolution offer</b> (CORE_SYSTEMS §13) and is not: the two
    /// Evolution choices per weapon are listed as an open item in §13 itself, so there is no content
    /// to present. A normal offer is shown instead, and that divergence is recorded in the change
    /// brief rather than papered over with invented Evolutions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeOffer : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The whole screen. Starts inactive: it pauses the game and gates player input " +
                 "while open, so a panel that opened itself on Play would freeze the run.")]
        [SerializeField] private GameObject panel;

        [Tooltip("The three upgrade cards, left to right.")]
        [SerializeField] private UpgradeCard[] cards = new UpgradeCard[0];

        [Tooltip("CORE_SYSTEMS §9's always-visible 4th slot. Held separately from the three because " +
                 "it is drawn from a different pool and sits apart on the row.")]
        [SerializeField] private UpgradeCard curseCard;

        [Tooltip("The single centred card a Secret Vault grant is presented on. Its own "
                 + "card rather than one of the three: a grant has nothing to choose "
                 + "between, and reusing a row slot would leave it off-centre with three "
                 + "empty gaps beside it.")]
        [SerializeField] private UpgradeCard grantCard;

        [SerializeField] private Text headerLabel;
        [SerializeField] private Text subheaderLabel;

        [Tooltip("Full-screen flash on the pick. ART_DIRECTION §6 asks for a red flash on a Curse " +
                 "and white/gold on a normal upgrade, and this is the whole of that effect.")]
        [SerializeField] private Image flash;

        [Header("Sources — found by tag when empty")]
        [SerializeField] private PlayerXP experience;
        [SerializeField] private RunUpgrades upgrades;
        [SerializeField] private RunCurses curses;
        [SerializeField] private RunLoadout loadout;

        [Header("Pools")]
        [SerializeField] private UpgradePool pool;
        [SerializeField] private CursePool cursePool;

        [Tooltip("Which BALANCE §13 weight row to draw with. Floors and biomes do not exist yet, so " +
                 "this is fixed at Biome 1 (Upper Caves) rather than derived from a depth nothing " +
                 "tracks. It becomes a read off the run when floor loading is built.")]
        [SerializeField] private int biome = 1;

        [Header("Pause")]
        [SerializeField] private RunPause pause;

        [Header("Level-up beat — invented numbers (owner, 2026-09-18)")]
        [Tooltip("Real seconds the fight takes to slow to a stop after a level-up, instead of the " +
                 "cut to a frozen panel the owner found jarring. The pause holds from the first " +
                 "frame of it — input off, HitStop refused — only the clock winds down gradually.")]
        [SerializeField, Min(0f)] private float levelUpSlowMo = 0.45f;

        [Tooltip("Real seconds between the level-up and the panel starting its entrance. Coupled to " +
                 "LevelUpVFX on the player, which plays its burst over this window: shorten one " +
                 "without the other and the cards arrive over the burst or long after it.")]
        [SerializeField, Min(0f)] private float levelUpRevealDelay = 0.6f;

        [SerializeField] private OfferReveal reveal;

        [Header("Look")]
        [Tooltip("Seconds the pick flash takes to fade. Unscaled — the game is still paused for the " +
                 "first part of it.")]
        [SerializeField] private float flashFade = 0.35f;

        [SerializeField] private TierPalette palette;

        private readonly List<UpgradeDefinition> _offered = new List<UpgradeDefinition>();
        private readonly List<UpgradeDefinition> _pendingGrants = new List<UpgradeDefinition>();

        private int _pendingLevels;
        private bool _holdingPause;
        private float _flashAlpha;

        private bool _beatRunning;
        private float _revealAt;

        /// <summary>Whether the panel has already made its entrance during the current hold.</summary>
        private bool _dealtThisHold;

        /// <summary>Whether the offer is on screen.</summary>
        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Reset()
        {
            palette = TierPalette.Default;
        }

        private void Awake()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                if (experience == null) experience = player.GetComponentInChildren<PlayerXP>(true);
                if (upgrades == null) upgrades = player.GetComponentInChildren<RunUpgrades>(true);
                if (curses == null) curses = player.GetComponentInChildren<RunCurses>(true);
                if (loadout == null) loadout = player.GetComponentInChildren<RunLoadout>(true);
            }

            if (pause == null) pause = FindFirstObjectByType<RunPause>();
            if (reveal == null) reveal = GetComponent<OfferReveal>();

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null) cards[i].Picked += HandlePicked;
            }

            if (curseCard != null) curseCard.Picked += HandlePicked;
            if (grantCard != null) grantCard.Picked += HandlePicked;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null) cards[i].Picked -= HandlePicked;
            }

            if (curseCard != null) curseCard.Picked -= HandlePicked;
            if (grantCard != null) grantCard.Picked -= HandlePicked;
        }

        private void OnEnable()
        {
            if (experience != null) experience.LeveledUp += HandleLeveledUp;

            _beatRunning = false;
            Close();
        }

        private void OnDisable()
        {
            if (experience != null) experience.LeveledUp -= HandleLeveledUp;

            // Never leave the game paused because this object went away with the panel open.
            Release();
        }

        private void Update()
        {
            // Unscaled, because the pause the beat holds is winding the scaled clock to zero.
            if (_beatRunning && Time.unscaledTime >= _revealAt)
            {
                _beatRunning = false;
                ShowNext();
            }

            if (_flashAlpha <= 0f || flash == null) return;

            // Unscaled: the pick happens while the game is still paused, and a flash on scaled time
            // would sit at full brightness until something else resumed the clock.
            _flashAlpha -= flashFade > 0f ? Time.unscaledDeltaTime / flashFade : 1f;

            Color colour = flash.color;
            colour.a = Mathf.Max(0f, _flashAlpha);
            flash.color = colour;

            if (_flashAlpha <= 0f) flash.enabled = false;
        }

        /// <summary>
        /// Presents an upgrade the run has already been given, with no choice attached — the Secret
        /// Vault's guaranteed Relic (CORE_SYSTEMS §8, change brief: "<c>VaultReward.Granted</c> fires
        /// with the upgrade, so the real panel takes that seam over later").
        ///
        /// The vault still does the granting. With exactly one guaranteed Legendary there is nothing
        /// to choose between, so this is the ceremony around a grant that the payout otherwise
        /// happens without — not a one-card draw.
        /// </summary>
        public void PresentGrant(UpgradeDefinition granted)
        {
            if (granted == null) return;

            _pendingGrants.Add(granted);

            // No beat of its own: the vault's payout is already a moment the player walked up to,
            // not an interruption. If a level-up beat is running, the grant waits in the queue for it.
            if (!IsOpen && !_beatRunning) ShowNext();
        }

        /// <summary>
        /// Queues one offer. Public so a probe and the sandbox's debug menu can open the panel
        /// without farming XP first.
        /// </summary>
        [ContextMenu("Offer")]
        public void Enqueue()
        {
            _pendingLevels++;

            if (!IsOpen && !_beatRunning) ShowNext();
        }

        private void HandleLeveledUp(int level)
        {
            // Queued, never "open the panel now". PlayerXP.Add loops its level-up check, so one large
            // XP drop raises this several times synchronously before it returns — a wave clear that
            // covers two levels would otherwise open the panel twice in the same frame and throw the
            // first offer away unseen. Those later raises land inside the beat the first one started,
            // so a multi-level drop gets one slow-down and one burst, then its offers in sequence.
            _pendingLevels++;

            if (IsOpen || _beatRunning) return;

            BeginBeat();
        }

        /// <summary>
        /// The level-up beat: the pause is taken now, easing the fight to a stop, and the panel
        /// comes up once <see cref="levelUpRevealDelay"/> has passed. LevelUpVFX plays its burst on
        /// the player off the same event over the same window.
        ///
        /// The hold is taken here, on the killing blow's own call stack, and not when the panel
        /// opens. That ordering is what keeps HitStop out: AttackHitbox raises Landed — which asks
        /// for the freeze — only after TakeDamage, and so this, has returned (see HitStop's class
        /// comment), and a pause already holding is what it checks.
        /// </summary>
        private void BeginBeat()
        {
            if (!_holdingPause && pause != null)
            {
                pause.Push(levelUpSlowMo);
                _holdingPause = true;
            }

            _beatRunning = true;
            _revealAt = Time.unscaledTime + levelUpRevealDelay;
        }

        private void ShowNext()
        {
            if (_pendingGrants.Count > 0)
            {
                UpgradeDefinition granted = _pendingGrants[0];
                _pendingGrants.RemoveAt(0);
                ShowGrant(granted);
                return;
            }

            if (_pendingLevels > 0)
            {
                _pendingLevels--;
                ShowOffer();
                return;
            }

            Close();
        }

        private void ShowOffer()
        {
            _offered.Clear();

            if (pool != null)
            {
                UpgradeDefinition[] weaponEntries = loadout != null && loadout.Weapon != null
                    ? loadout.Weapon.WeaponUpgrades
                    : null;

                IReadOnlyList<UpgradeDefinition> taken = upgrades != null ? upgrades.Taken : null;

                pool.Draw(_offered, cards.Length, weaponEntries, taken, biome);
            }

            if (grantCard != null) grantCard.Clear();

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                cards[i].Bind(i < _offered.Count ? _offered[i] : null);
            }

            if (curseCard != null)
            {
                CurseDefinition curse = cursePool != null
                    ? cursePool.Draw(curses != null ? curses.Taken : null)
                    : null;

                // Null is a real outcome — the Curse pool is eight entries and a long run exhausts
                // it. The slot goes away rather than showing an empty card.
                curseCard.Bind(curse);
            }

            int level = experience != null ? experience.Level : 1;

            SetHeader("LEVEL " + level, "CHOOSE AN UPGRADE");
            Open();
        }

        private void ShowGrant(UpgradeDefinition granted)
        {
            // The row empties and the centred grant card carries it alone. The Curse slot is not
            // drawn either: a guaranteed Relic is not an offer, and pairing it with a Curse would
            // turn a reward into a decision the design never asked for.
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null) cards[i].Clear();
            }

            if (curseCard != null) curseCard.Clear();
            if (grantCard != null) grantCard.Bind(granted);

            SetHeader("RELIC RECOVERED", "CLAIM IT");
            Open();
        }

        private void HandlePicked(UpgradeCard card)
        {
            if (!IsOpen || card == null) return;

            // Nothing is taken while the cards are still arriving. The level can come up on a swing
            // the player is still mashing, and a click landing as a card rises under the cursor
            // would pick it unseen. Checked here rather than on the Button so UpgradeCard.Pick —
            // the probe's path — obeys the same rule.
            if (reveal != null && !reveal.IsSettled) return;

            if (card.Curse != null)
            {
                if (curses != null) curses.Add(card.Curse);
                Flash(palette.Curse);
            }
            else if (card.Upgrade != null)
            {
                // The seam RunUpgrades was built for. It applies the pick's StatModifiers through
                // PlayerStats, which covers the numeric upgrades; the behavioural ones carry no
                // modifiers and land as data until the damage pipeline has hooks for them.
                //
                // A grant re-presented here is already held by the run, and Add refuses duplicates,
                // so claiming one is a no-op rather than a double-apply.
                if (upgrades != null) upgrades.Add(card.Upgrade);

                // ART_DIRECTION §6: white/gold for a normal pick, red for a Curse. The gold is the
                // Legendary border colour, so the two golds in the UI are the same gold.
                Flash(palette.Legendary);
            }

            // Hidden without releasing the pause, because ShowNext may put another offer straight
            // back up. Popping and re-pushing between two queued level-ups would resume the game for
            // a frame AND make RunPause capture the cursor state it had just forced itself, so the
            // hardware cursor would be left visible over the fight afterwards.
            Hide();
            ShowNext();
        }

        private void SetHeader(string header, string subheader)
        {
            if (headerLabel != null) headerLabel.text = header;
            if (subheaderLabel != null) subheaderLabel.text = subheader;
        }

        private void Flash(Color colour)
        {
            if (flash == null) return;

            colour.a = 1f;

            flash.color = colour;
            flash.enabled = true;
            _flashAlpha = 1f;
        }

        private void Open()
        {
            if (panel == null) return;

            // Full entrance only when the panel is coming up over the fight. The next of several
            // queued offers finds the scrim already up and only re-deals its cards. Read before the
            // panel is switched on — HandlePicked hides it for exactly one call before this.
            bool full = !_dealtThisHold;

            panel.SetActive(true);
            if (reveal != null) reveal.Play(full);

            _dealtThisHold = true;

            if (_holdingPause || pause == null) return;

            pause.Push();
            _holdingPause = true;
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Close()
        {
            Hide();
            Release();
        }

        private void Release()
        {
            _dealtThisHold = false;

            if (!_holdingPause) return;

            _holdingPause = false;
            if (pause != null) pause.Pop();
        }
    }
}
