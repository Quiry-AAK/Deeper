using System.Collections.Generic;
using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// The shared upgrade pool and the weighted draw that fills an offer (CORE_SYSTEMS §9,
    /// BALANCE §13).
    ///
    /// CORE_SYSTEMS §9 is specific about the shape, and it is worth restating because the obvious
    /// implementation is the wrong one: the offer is **one draw across the combined pool** — this
    /// asset's shared entries plus the equipped weapon's sub-pool — and it is **not tier-gated**, so
    /// a Common, a Rare and an Epic can all appear in the same offer. There is no floor gate and no
    /// "pick a tier for the run".
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradePool_", menuName = "Deeper/Upgrade Pool", order = 2)]
    public sealed class UpgradePool : ScriptableObject
    {
        /// <summary>BALANCE §13's rarity weights for one biome.</summary>
        [System.Serializable]
        public struct TierWeights
        {
            [Tooltip("BALANCE §13: 65 in Biome 1, 55 in Biome 2, 45 in Biome 3.")]
            public float Common;
            [Tooltip("30 / 35 / 40.")]
            public float Rare;
            [Tooltip("5 / 10 / 15.")]
            public float Epic;
        }

        [Tooltip("CONTENT_DESIGN §1's shared pool — offered regardless of which weapon is equipped.")]
        [SerializeField] private UpgradeDefinition[] shared = new UpgradeDefinition[0];

        [Tooltip("BALANCE §13, one row per biome, in order. The biome number is 1-based and clamped, " +
                 "so a depth past the authored rows keeps the last biome's weights rather than " +
                 "silently drawing nothing.")]
        [SerializeField] private TierWeights[] weightsPerBiome = new TierWeights[0];

        // Reused across draws. An offer opens on a level-up, which is a pause — but it is also the
        // one frame where the panel build, the strip refresh and any in-flight hitstop land together,
        // and allocating scratch lists there is avoidable work at exactly the wrong moment.
        private readonly List<UpgradeDefinition> _candidates = new List<UpgradeDefinition>();
        private readonly List<UpgradeDefinition> _tierBucket = new List<UpgradeDefinition>();

        public IReadOnlyList<UpgradeDefinition> Shared { get { return shared; } }

        /// <summary>
        /// Fills <paramref name="into"/> with up to <paramref name="count"/> distinct offers.
        ///
        /// <paramref name="weaponEntries"/> is the equipped weapon's sub-pool (CONTENT_DESIGN §2),
        /// which lives on the weapon asset so "only offered when that weapon is equipped" is true by
        /// construction rather than by a filter somebody has to remember to write.
        /// </summary>
        public void Draw(List<UpgradeDefinition> into, int count,
                         IList<UpgradeDefinition> weaponEntries,
                         IReadOnlyList<UpgradeDefinition> taken,
                         int biome)
        {
            if (into == null) return;

            into.Clear();
            _candidates.Clear();

            Collect(shared, taken, into);
            if (weaponEntries != null) Collect(weaponEntries, taken, into);

            TierWeights weights = WeightsFor(biome);

            for (int i = 0; i < count && _candidates.Count > 0; i++)
            {
                UpgradeDefinition pick = DrawOne(weights);
                if (pick == null) break;

                into.Add(pick);
                _candidates.Remove(pick);
            }
        }

        /// <summary>BALANCE §13's row for a biome, clamped to what is authored.</summary>
        public TierWeights WeightsFor(int biome)
        {
            if (weightsPerBiome == null || weightsPerBiome.Length == 0) return new TierWeights { Common = 1f };

            int index = Mathf.Clamp(biome - 1, 0, weightsPerBiome.Length - 1);
            return weightsPerBiome[index];
        }

        private void Collect(IList<UpgradeDefinition> source, IReadOnlyList<UpgradeDefinition> taken,
                             List<UpgradeDefinition> alreadyOffered)
        {
            for (int i = 0; i < source.Count; i++)
            {
                UpgradeDefinition entry = source[i];
                if (entry == null || _candidates.Contains(entry)) continue;

                // BALANCE §13: "Legendary (Relics) are excluded entirely — guaranteed-drop only."
                // Filtered here rather than left out of the arrays, so a relic can still sit in a
                // pool for the Hub's Relic Vault guarantee to find later without leaking into a draw.
                if (entry.Tier == UpgradeTier.Legendary) continue;

                if (Holds(taken, entry)) continue;
                if (alreadyOffered != null && alreadyOffered.Contains(entry)) continue;

                if (!PrerequisiteMet(entry, taken)) continue;
                if (Excluded(entry, taken)) continue;

                _candidates.Add(entry);
            }
        }

        /// <summary>
        /// Whether a run already holds an entry.
        ///
        /// Hand-written because <c>IReadOnlyList</c> has no <c>Contains</c> and the LINQ extension
        /// allocates an enumerator on every call — this runs once per pool entry per draw. The list
        /// is a run's picks, a few dozen at the very most, so a loop is the cheap answer as well as
        /// the plain one.
        /// </summary>
        private static bool Holds(IReadOnlyList<UpgradeDefinition> taken, UpgradeDefinition entry)
        {
            if (taken == null) return false;

            for (int i = 0; i < taken.Count; i++)
            {
                if (taken[i] == entry) return true;
            }

            return false;
        }

        private static bool PrerequisiteMet(UpgradeDefinition entry, IReadOnlyList<UpgradeDefinition> taken)
        {
            if (entry.Requires == null) return true;

            return Holds(taken, entry.Requires);
        }

        private static bool Excluded(UpgradeDefinition entry, IReadOnlyList<UpgradeDefinition> taken)
        {
            UpgradeDefinition[] excludes = entry.Excludes;
            if (excludes == null || taken == null) return false;

            for (int i = 0; i < excludes.Length; i++)
            {
                if (excludes[i] != null && Holds(taken, excludes[i])) return true;
            }

            return false;
        }

        /// <summary>
        /// Picks a tier by weight, then an entry uniformly inside it.
        ///
        /// **Not the same as weighting every entry by its tier**, which is the obvious version and
        /// gets §13's table wrong: with 14 Commons and 2 Epics in the candidate set, per-entry
        /// weighting turns "Epic 5%" into 5x2 / (65x14 + 30x7 + 5x2) — about 1%. Choosing the tier
        /// first makes the published percentage the actual percentage no matter how many entries
        /// each tier happens to hold, which matters because the pool grows unevenly as content lands.
        ///
        /// A tier with no candidates left is skipped rather than rolled and dropped, so a late run
        /// that has taken every Common still gets three cards instead of one.
        /// </summary>
        private UpgradeDefinition DrawOne(TierWeights weights)
        {
            float total = 0f;

            for (int t = 0; t <= (int)UpgradeTier.Epic; t++)
            {
                if (HasTier((UpgradeTier)t)) total += WeightOf(weights, (UpgradeTier)t);
            }

            if (total <= 0f)
            {
                // Every remaining candidate sits in a tier with zero weight. Uniform is the only
                // honest answer left, and it beats handing back empty cards.
                return _candidates[Random.Range(0, _candidates.Count)];
            }

            float roll = Random.value * total;

            for (int t = 0; t <= (int)UpgradeTier.Epic; t++)
            {
                UpgradeTier tier = (UpgradeTier)t;
                if (!HasTier(tier)) continue;

                roll -= WeightOf(weights, tier);
                if (roll > 0f) continue;

                FillBucket(tier);
                return _tierBucket[Random.Range(0, _tierBucket.Count)];
            }

            // Floating-point slack only — the loop above consumes the whole total.
            return _candidates[_candidates.Count - 1];
        }

        private static float WeightOf(TierWeights weights, UpgradeTier tier)
        {
            switch (tier)
            {
                case UpgradeTier.Common: return Mathf.Max(0f, weights.Common);
                case UpgradeTier.Rare: return Mathf.Max(0f, weights.Rare);
                case UpgradeTier.Epic: return Mathf.Max(0f, weights.Epic);

                // Legendary never reaches here — Collect drops it — but an explicit branch beats a
                // default that would quietly hand a newly added tier the Common weight.
                default: return 0f;
            }
        }

        private bool HasTier(UpgradeTier tier)
        {
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Tier == tier) return true;
            }

            return false;
        }

        private void FillBucket(UpgradeTier tier)
        {
            _tierBucket.Clear();

            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Tier == tier) _tierBucket.Add(_candidates[i]);
            }
        }
    }
}
