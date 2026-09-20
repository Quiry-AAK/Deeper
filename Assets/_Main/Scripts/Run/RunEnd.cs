using System;
using Deeper.Character;
using Deeper.Combat;
using Deeper.Core;
using Deeper.Meta;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Run
{
    /// <summary>
    /// Ends the run — both ways it can end — and pays out.
    ///
    /// GDD §Game Loop 7 gives exactly two outcomes: "defeat the boss and escape (win), or reach 0 HP
    /// (die)", and §Death says a death is "immediate run end, return to Hub". They differ in one
    /// word on a screen and in nothing else, which is why one class owns both rather than a
    /// `PlayerDeathHandler` and a `VictoryHandler` that would each have to compute the same award.
    ///
    /// **It computes and pays; it does not draw.** <see cref="Deeper.UI.RunSummaryPanel"/> is the
    /// screen. The split is the one this project already draws between `UltimateGauge` and
    /// `UltimateGaugeHUD`: a run can end in a scene with no HUD in it, and a probe can read the
    /// award without a canvas existing.
    ///
    /// **Shards are paid once, here, and nowhere else** (GDD §Currency, BALANCE §14) — there is no
    /// in-level pickup object and never was. <see cref="ShardBank"/>'s header has named this call
    /// site as the seam it was written for since the Hub shipped.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunEnd : MonoBehaviour
    {
        /// <summary>Which of GDD §Game Loop 7's two endings happened.</summary>
        public enum Outcome
        {
            Died,
            Victory,
        }

        /// <summary>Everything the run-end screen shows, and everything a probe needs to check it.</summary>
        public struct Summary
        {
            public Outcome Outcome;
            public int DepthReached;
            public int LevelsGained;
            public int Shards;
            public float Seconds;
            public string Weapon;
        }

        [Header("Sources — wired in the scene")]
        [Tooltip("The run being played. Its RunCompleted is the victory half; its Floor is the " +
                 "depth the award is computed from.")]
        [SerializeField] private FloorLoader floor;

        [Tooltip("Hers. Its Died is the death half. Found by tag when empty.")]
        [SerializeField] private Damageable health;

        [Tooltip("Levels gained is her level minus the one she started at.")]
        [SerializeField] private PlayerXP experience;

        [Tooltip("Named on the summary. GDD §UI lists the weapon used on the death screen.")]
        [SerializeField] private RunLoadout loadout;

        [Header("Payout")]
        [Tooltip("Where Shards are paid. BALANCE §14 awards them once, at run end, from Levels " +
                 "Gained and Depth Reached.")]
        [SerializeField] private ShardBank bank;

        [Tooltip("BALANCE §14: Shards = (LevelsGained x 15) + (DepthReached x 10). Serialized " +
                 "rather than const because §14 calls the multiplier a first-pass placeholder that " +
                 "needs playtesting against the 30-60 min run length.")]
        [SerializeField] private int shardsPerLevel = 15;

        [SerializeField] private int shardsPerFloor = 10;

        [Header("Presentation")]
        [Tooltip("Held while the summary is up, so she cannot swing, walk or aim behind it and the " +
                 "hardware cursor comes back for the button. Refcounted — the upgrade offer may " +
                 "still be closing.")]
        [SerializeField] private RunPause pause;

        [Tooltip("Seconds between the killing blow and the screen. Long enough for the death clip " +
                 "and the last hitstop to read as the end of a fight rather than as a cut.")]
        [SerializeField] private float delaySeconds = 1.1f;

        [SerializeField] private string playerTag = "Player";

        /// <summary>
        /// Raised once, with everything the screen shows. The panel subscribes; so does a probe.
        /// </summary>
        public event Action<Summary> Ended;

        private float _startedAt;
        private int _startingLevel = 1;
        private bool _ended;
        private float _openAt;
        private bool _opening;

        /// <summary>What the run ended as. Meaningless until <see cref="HasEnded"/>.</summary>
        public Summary Result { get; private set; }

        public bool HasEnded { get { return _ended; } }

        private void Awake()
        {
            if (health == null || experience == null || loadout == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    if (health == null) health = player.GetComponent<Damageable>();
                    if (experience == null) experience = player.GetComponentInChildren<PlayerXP>(true);
                    if (loadout == null) loadout = player.GetComponent<RunLoadout>();
                }
            }

            if (pause == null) pause = FindFirstObjectByType<RunPause>();
            if (floor == null) floor = FindFirstObjectByType<FloorLoader>();
        }

        private void OnEnable()
        {
            if (health != null) health.Died += HandleDied;
            if (floor != null) floor.RunCompleted += HandleRunCompleted;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
            if (floor != null) floor.RunCompleted -= HandleRunCompleted;
        }

        private void Start()
        {
            // Start, not Awake: her level is read here as the baseline, and Hub Core Stats will one
            // day set a starting level before the first frame. Unscaled, because a run's length is
            // wall-clock time and hitstop and the upgrade screen both stop the scaled clock — a
            // player who spent two minutes reading cards did not descend two minutes faster.
            _startedAt = Time.unscaledTime;
            _startingLevel = experience != null ? experience.Level : 1;
        }

        private void Update()
        {
            if (!_opening || Time.unscaledTime < _openAt) return;

            _opening = false;
            Open();
        }

        private void HandleDied()
        {
            Finish(Outcome.Died);
        }

        private void HandleRunCompleted()
        {
            Finish(Outcome.Victory);
        }

        /// <summary>
        /// Ends the run. Public and context-menu'd so a probe can reach a victory without fighting
        /// sixteen floors of bosses to get there — simulated key presses never reach play mode
        /// (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        public void Finish(Outcome outcome)
        {
            if (_ended) return;
            _ended = true;

            int depth = floor != null ? floor.Floor : 0;
            int levels = experience != null ? Mathf.Max(0, experience.Level - _startingLevel) : 0;

            var summary = new Summary
            {
                Outcome = outcome,
                DepthReached = depth,
                LevelsGained = levels,
                Shards = (levels * shardsPerLevel) + (depth * shardsPerFloor),
                Seconds = Time.unscaledTime - _startedAt,
                Weapon = loadout != null && loadout.Weapon != null ? loadout.Weapon.DisplayName : "-",
            };

            Result = summary;

            // Paid before the screen, not by it. A player who alt-F4s on the summary still earned
            // them, and a screen that has not been built yet must not be able to swallow a payout.
            if (bank != null) bank.Add(summary.Shards);

            Debug.Log(string.Format(
                "Run ended: {0}. Depth {1}, +{2} level(s), {3} Shard(s), {4:0.0}s, {5}.",
                outcome, summary.DepthReached, summary.LevelsGained, summary.Shards,
                summary.Seconds, summary.Weapon), this);

            // Delayed on the UNSCALED clock, so the killing blow's hitstop still plays out
            // underneath. A coroutine would do the same job; a timer in Update survives this
            // component being disabled and re-enabled by a scene reload mid-delay, which a coroutine
            // does not.
            _openAt = Time.unscaledTime + delaySeconds;
            _opening = true;
        }

        [ContextMenu("Finish — Died")]
        private void FinishDied() { Finish(Outcome.Died); }

        [ContextMenu("Finish — Victory")]
        private void FinishVictory() { Finish(Outcome.Victory); }

        private void Open()
        {
            // Pause first, raise second. RunPause cancels any in-flight HitStop, and that coroutine
            // restores normalScale unconditionally when it expires — the killing blow that ends a
            // run is exactly the hit that freezes, so without the cancel the screen would un-pause
            // itself about 50ms after opening. Same defect the upgrade offer hit.
            if (pause != null) pause.Push();

            if (Ended != null) Ended(Result);
        }
    }
}
