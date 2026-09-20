using UnityEngine;

namespace Deeper.Upgrades
{
    /// <summary>
    /// One entry from the Curse pool (CONTENT_DESIGN §3, CORE_SYSTEMS §9) as authored data.
    ///
    /// A Curse is the always-visible 4th slot on the offer screen — "high-risk, high-reward, never
    /// mandatory". It is a separate type from <see cref="UpgradeDefinition"/> rather than a tier on
    /// it, for three reasons the design states outright: Curses are drawn from their own pool at a
    /// flat uniform weight (BALANCE §13 excludes them from the rarity table), they are never one of
    /// the three upgrade cards, and CONTENT_DESIGN §3 scopes how many can be *taken* separately from
    /// how many upgrades can. A `tier: Curse` enum value would have to be excluded by hand at every
    /// one of those three points.
    ///
    /// **It carries no <c>StatModifier</c>s and that is not an omission.** All eight Curses in §3 are
    /// behavioural — "take double damage", "healing effects are reduced", "Hyper Armor disabled" —
    /// and every one of them needs a hook in the damage pipeline that does not exist. Giving them a
    /// half-answer as stat modifiers would silently change the numbers by the wrong amount.
    /// </summary>
    [CreateAssetMenu(fileName = "Curse_", menuName = "Deeper/Curse", order = 1)]
    public sealed class CurseDefinition : ScriptableObject
    {
        [Tooltip("Stable key. Unique, and the same shape as an upgrade's id.")]
        [SerializeField] private string id = "Curse_";

        [SerializeField] private string displayName = "Curse";

        [TextArea]
        [Tooltip("What the player gains, as CONTENT_DESIGN §3 writes it.")]
        [SerializeField] private string upside = "";

        [TextArea]
        [Tooltip("What it costs. Held apart from the upside deliberately: every Curse is a trade, " +
                 "and one sentence carrying both lets the eye skip the half that hurts.")]
        [SerializeField] private string downside = "";

        [Tooltip("Shown on the Curse card and in the run's upgrade strip.")]
        [SerializeField] private Sprite icon;

        [Tooltip("The icon-led card's compressed line for the upside only. The downside stays a " +
                 "plain string (below), never a second EffectSummary — a Curse card must not show " +
                 "two competing numeric callouts.")]
        [SerializeField] private EffectSummary upsideSummary;

        [Tooltip("The Curse card's short cost line, under the crimson cost mark. Compresses " +
                 "downside above; kept as a separate field rather than reusing downside verbatim " +
                 "because the cost line has to fit the card's cost box.")]
        [SerializeField] private string costLine = "";

        public string Id { get { return id; } }
        public string DisplayName { get { return displayName; } }
        public string Upside { get { return upside; } }
        public string Downside { get { return downside; } }
        public Sprite Icon { get { return icon; } }
        public EffectSummary UpsideSummary { get { return upsideSummary; } }
        public string CostLine { get { return costLine; } }
    }
}
