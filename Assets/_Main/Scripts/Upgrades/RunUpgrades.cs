using System;
using System.Collections.Generic;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// The upgrades this run is carrying (CORE_SYSTEMS §9).
    ///
    /// A run accumulates upgrades and never loses them, so this is a list that only grows and is
    /// cleared when a new descent starts. It applies each one through <c>PlayerStats.SetSource</c>
    /// keyed by the upgrade's id, which is what lets the weapon, the run's upgrades and the Hub's
    /// Core Stats all modify the same numbers without overwriting each other.
    ///
    /// **What draws the offer is not here and is Milestone 4.** The weighted pool, the three-card
    /// panel, the Curse slot and the Evolution milestones do not exist; <see cref="Add"/> is the
    /// seam they will call. Built now because the HUD strip that shows a run's upgrades would
    /// otherwise be decoration, the same call the XP bar's <c>PlayerXP</c> made.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunUpgrades : MonoBehaviour
    {
        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private PlayerStats stats;

        [Tooltip("Taken at the start of a run before anything is drawn. For testing the strip and " +
                 "for a future starting-relic; normally empty.")]
        [SerializeField] private UpgradeDefinition[] startingUpgrades = new UpgradeDefinition[0];

        private readonly List<UpgradeDefinition> _taken = new List<UpgradeDefinition>();

        /// <summary>Raised whenever the list changes, for the HUD to redraw.</summary>
        public event Action Changed;

        /// <summary>In the order they were taken — the strip reads oldest-first.</summary>
        public IReadOnlyList<UpgradeDefinition> Taken { get { return _taken; } }

        public int Count { get { return _taken.Count; } }

        private void Awake()
        {
            stats = RigRefs.Find(this, stats);
        }

        private void OnEnable()
        {
            // A run's upgrades are a run's, so this rebuilds from scratch rather than carrying a
            // previous descent's list across a scene reload.
            _taken.Clear();

            for (int i = 0; i < startingUpgrades.Length; i++) Add(startingUpgrades[i]);

            if (_taken.Count == 0 && Changed != null) Changed();
        }

        /// <summary>
        /// Takes an upgrade and applies its modifiers. Milestone 4's offer panel calls this.
        ///
        /// Duplicates are refused rather than stacked: <c>SetSource</c> is keyed by the upgrade's
        /// id, so taking one twice would silently *replace* its own contribution and read as the
        /// second pick doing nothing. Stacking upgrades are a design question CONTENT_DESIGN has
        /// not answered, and guessing it here would be inventing a rule.
        /// </summary>
        public bool Add(UpgradeDefinition upgrade)
        {
            if (upgrade == null || _taken.Contains(upgrade)) return false;

            _taken.Add(upgrade);

            if (stats != null && upgrade.Modifiers != null && upgrade.Modifiers.Length > 0)
            {
                stats.SetSource(upgrade.Id, upgrade.Modifiers);
            }

            if (Changed != null) Changed();
            return true;
        }

        /// <summary>Drops everything, for a new descent.</summary>
        public void Clear()
        {
            if (stats != null)
            {
                for (int i = 0; i < _taken.Count; i++) stats.RemoveSource(_taken[i].Id);
            }

            _taken.Clear();

            if (Changed != null) Changed();
        }
    }
}
