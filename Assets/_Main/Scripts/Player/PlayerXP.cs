using System;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// The run's experience and level (CORE_SYSTEMS §12).
    ///
    /// XP is the run's only resource — it is spent on nothing and exists purely to drive levelling,
    /// and a level-up is what opens the upgrade offer. **That offer is Milestone 4 and is not built
    /// here**; this raises <see cref="LeveledUp"/> and stops, so the HUD has something real to show
    /// instead of a decorative bar.
    ///
    /// **The curve is invented.** BALANCE §16 is titled "XP Curve (open)" and gives no numbers, and
    /// §16 also records that per-enemy XP drop values are unresolved — the two only make sense tuned
    /// together. Both ends are serialized so they can be retuned without a recompile (Design Rule 8).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerXP : MonoBehaviour
    {
        [Header("Curve — invented, BALANCE §16 is open")]
        [Tooltip("XP needed to reach level 2. Every later level costs this scaled by the exponent " +
                 "below.")]
        [SerializeField] private float baseXPToLevel = 10f;

        [Tooltip("Cost(level) = base × level^exponent. 1 would make every level cost the same; " +
                 "above 1 makes later levels progressively slower.")]
        [SerializeField] private float curveExponent = 1.35f;

        [Header("Refs — found anywhere on the player rig when empty")]
        [Tooltip("Supplies the XP-gain multiplier. NOTE: the stat is still called OreGain in code — " +
                 "the docs renamed Ore → XP (change brief §11) but the enum value and the " +
                 "serialized field were left alone, because renaming a serialized field silently " +
                 "drops its authored value.")]
        [SerializeField] private PlayerStats stats;

        private int _level = 1;
        private float _xpIntoLevel;

        /// <summary>Raised whenever XP or level moves, for the HUD to redraw.</summary>
        public event Action Changed;

        /// <summary>Raised with the new level. Milestone 4's upgrade offer attaches here.</summary>
        public event Action<int> LeveledUp;

        public int Level { get { return _level; } }

        /// <summary>XP earned into the current level, not the run total.</summary>
        public float XP { get { return _xpIntoLevel; } }

        public float XPToNextLevel { get { return CostOf(_level); } }

        public float Normalized
        {
            get
            {
                float cost = CostOf(_level);
                return cost > 0f ? Mathf.Clamp01(_xpIntoLevel / cost) : 0f;
            }
        }

        private void Awake()
        {
            stats = RigRefs.Find(this, stats);
        }

        /// <summary>
        /// Grants XP, scaled by the run's XP-gain stat, and levels up as many times as it covers.
        ///
        /// Public and context-menu'd so a probe and the test harness can drive it — nothing drops
        /// XP yet beyond <c>XPReward</c> on the enemy prefabs.
        /// </summary>
        public void Add(float amount)
        {
            if (amount <= 0f) return;

            float gain = stats != null ? stats.OreGain : 1f;
            _xpIntoLevel += amount * Mathf.Max(0f, gain);

            // A loop, not an if: a big late drop can cover more than one level, and BALANCE's
            // per-enemy values are unresolved, so nothing guarantees one drop is smaller than one
            // level. Guarded on a positive cost so a misauthored curve cannot spin forever.
            while (true)
            {
                float cost = CostOf(_level);
                if (cost <= 0f || _xpIntoLevel < cost) break;

                _xpIntoLevel -= cost;
                _level++;

                if (LeveledUp != null) LeveledUp(_level);
            }

            if (Changed != null) Changed();
        }

        [ContextMenu("Add 10 XP")]
        private void AddTen()
        {
            Add(10f);
        }

        /// <summary>XP required to leave <paramref name="level"/>.</summary>
        private float CostOf(int level)
        {
            return baseXPToLevel * Mathf.Pow(Mathf.Max(1, level), curveExponent);
        }
    }
}
