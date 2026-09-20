using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// The Curses this run has taken (CORE_SYSTEMS §9), the mirror of <see cref="RunUpgrades"/>.
    ///
    /// It records and nothing else. <see cref="RunUpgrades"/> applies each pick through
    /// <c>PlayerStats.SetSource</c> because most of its Commons are pure numbers; every Curse in
    /// CONTENT_DESIGN §3 is behavioural, so there is nothing here for the stat pipeline to carry.
    /// This exists so a taken Curse is *held* by the run — the offer panel would otherwise drop the
    /// pick on the floor, and the HUD strip would show the player choosing a Curse and receiving
    /// nothing at all, which reads as a bug rather than as unbuilt content.
    ///
    /// A separate component rather than a second list on <see cref="RunUpgrades"/>: the two are
    /// different types drawn from different pools with different take-limits, and one class holding
    /// both would need "and" to describe it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunCurses : MonoBehaviour
    {
        [Tooltip("Taken at the start of a run before anything is drawn. For testing the strip; " +
                 "normally empty.")]
        [SerializeField] private CurseDefinition[] startingCurses = new CurseDefinition[0];

        private readonly List<CurseDefinition> _taken = new List<CurseDefinition>();

        /// <summary>Raised whenever the list changes, for the HUD to redraw.</summary>
        public event Action Changed;

        /// <summary>In the order they were taken.</summary>
        public IReadOnlyList<CurseDefinition> Taken { get { return _taken; } }

        public int Count { get { return _taken.Count; } }

        private void OnEnable()
        {
            // A run's Curses are a run's — rebuilt from scratch rather than carried across a reload,
            // the same rule RunUpgrades follows.
            _taken.Clear();

            for (int i = 0; i < startingCurses.Length; i++) Add(startingCurses[i]);

            if (_taken.Count == 0 && Changed != null) Changed();
        }

        /// <summary>
        /// Takes a Curse. Refuses duplicates for the reason <c>RunUpgrades.Add</c> does: taking one
        /// twice would read as the second pick doing nothing, and whether Curses stack is a design
        /// question CONTENT_DESIGN has not answered.
        /// </summary>
        public bool Add(CurseDefinition curse)
        {
            if (curse == null || _taken.Contains(curse)) return false;

            _taken.Add(curse);

            if (Changed != null) Changed();
            return true;
        }

        /// <summary>Drops everything, for a new descent.</summary>
        public void Clear()
        {
            _taken.Clear();

            if (Changed != null) Changed();
        }
    }
}
