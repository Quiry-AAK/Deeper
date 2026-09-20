using System.Collections.Generic;
using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// The Curse pool (CONTENT_DESIGN §3), drawn for the offer screen's always-visible 4th slot.
    ///
    /// Flat and uniform on purpose — BALANCE §13: "Curses are drawn from their own pool (flat,
    /// uniform weight) and don't participate in this table." That is also why this is a separate
    /// asset from <see cref="UpgradePool"/> rather than a fourth tier inside it: sharing the type
    /// would mean carrying a rarity weight that every call site has to remember to ignore.
    /// </summary>
    [CreateAssetMenu(fileName = "CursePool", menuName = "Deeper/Curse Pool", order = 3)]
    public sealed class CursePool : ScriptableObject
    {
        [Tooltip("CONTENT_DESIGN §3's eight Curses.")]
        [SerializeField] private CurseDefinition[] entries = new CurseDefinition[0];

        private readonly List<CurseDefinition> _candidates = new List<CurseDefinition>();

        public IReadOnlyList<CurseDefinition> Entries { get { return entries; } }

        /// <summary>
        /// One Curse the run has not already taken, or null once they are all gone. Null is a real
        /// outcome the panel has to handle rather than an error: the pool is eight entries and a
        /// long run can exhaust it.
        /// </summary>
        public CurseDefinition Draw(IReadOnlyList<CurseDefinition> taken)
        {
            _candidates.Clear();

            for (int i = 0; i < entries.Length; i++)
            {
                CurseDefinition entry = entries[i];
                if (entry == null) continue;
                if (Holds(taken, entry)) continue;

                _candidates.Add(entry);
            }

            if (_candidates.Count == 0) return null;

            return _candidates[Random.Range(0, _candidates.Count)];
        }

        /// <summary>
        /// Whether a run already holds a Curse. <c>IReadOnlyList</c> has no <c>Contains</c>, and the
        /// LINQ extension allocates an enumerator per call.
        /// </summary>
        private static bool Holds(IReadOnlyList<CurseDefinition> taken, CurseDefinition entry)
        {
            if (taken == null) return false;

            for (int i = 0; i < taken.Count; i++)
            {
                if (taken[i] == entry) return true;
            }

            return false;
        }
    }
}
