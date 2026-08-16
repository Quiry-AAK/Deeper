using Deeper.Stats;
using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// One entry from the upgrade pool (CONTENT_DESIGN §1, CORE_SYSTEMS §9) as authored data.
    ///
    /// **This is the data shape only — the pool is not built.** CORE_SYSTEMS §9's weighted draw
    /// across the shared and weapon-specific pools, the three-card offer, the Curse slot and the
    /// Evolution milestones are all Milestone 4. What exists here is what the in-run HUD needs to
    /// show the upgrades a run is actually carrying, plus enough to make taking one *do* something
    /// rather than be a label.
    ///
    /// Effects are expressed as <see cref="StatModifier"/>s so they run through the shared
    /// <c>PlayerStats</c> pipeline every other stat source uses. That covers the numeric upgrades —
    /// Vitality, Fleet Foot, Quickstep and the rest of the Common tier. It deliberately does **not**
    /// cover the behavioural ones (Thorns, Explosive Finish, Bleeding Strikes): those need hooks in
    /// the damage pipeline, they are Milestone 4's problem, and giving them a half-answer here would
    /// be inventing design.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "Deeper/Upgrade", order = 0)]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Tooltip("Stable key. Used as the PlayerStats source key, so it must be unique.")]
        [SerializeField] private string id = "Upgrade_";

        [SerializeField] private string displayName = "Upgrade";

        [TextArea]
        [Tooltip("The effect line, as CONTENT_DESIGN writes it.")]
        [SerializeField] private string description = "";

        [Tooltip("CONTENT_DESIGN §1's tier. Drives the HUD's border colour — ART_DIRECTION §5 " +
                 "assigns Common white/grey, Rare blue, Epic purple, Legendary/Relic gold.")]
        [SerializeField] private UpgradeTier tier = UpgradeTier.Common;

        [Tooltip("Shown in the run's upgrade strip. Optional — the strip falls back to a plain " +
                 "tier-coloured slot, which is what every entry does until upgrade art exists.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Applied through PlayerStats.SetSource under this upgrade's id, so it stacks with " +
                 "the weapon and the Hub stats instead of overwriting them.")]
        [SerializeField] private StatModifier[] modifiers = new StatModifier[0];

        public string Id { get { return id; } }
        public string DisplayName { get { return displayName; } }
        public string Description { get { return description; } }
        public UpgradeTier Tier { get { return tier; } }
        public Sprite Icon { get { return icon; } }
        public StatModifier[] Modifiers { get { return modifiers; } }
    }

    /// <summary>
    /// CONTENT_DESIGN §1's rarity tiers. Values are stable — authored assets serialise by integer.
    /// </summary>
    public enum UpgradeTier
    {
        Common = 0,
        Rare = 1,
        Epic = 2,

        /// <summary>Reserved for the per-weapon Relics (CONTENT_DESIGN §4); never drawn from the
        /// normal weighted pool.</summary>
        Legendary = 3,
    }
}
