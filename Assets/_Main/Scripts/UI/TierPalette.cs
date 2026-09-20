using System;
using Deeper.Upgrades;
using UnityEngine;

namespace Deeper.UI
{
    /// <summary>
    /// ART_DIRECTION §5's offer-card colour coding, in one place: "Common = white/gray border,
    /// Rare = blue, Epic = purple, Legendary/Relic = gold, 4th Curse card visually distinct with a
    /// red/black treatment".
    ///
    /// A serialized struct rather than a static table, because these are look values and BALANCE's
    /// numbers are not the only placeholders in the project — they stay tunable in the Inspector.
    /// A struct rather than five loose fields on each component, because the offer card and the run
    /// strip must agree: the strip's colours were copied from the card spec precisely so a tier reads
    /// the same in both, and two hand-maintained copies of a colour table drift the way two copies of
    /// an art fallback already did.
    /// </summary>
    [Serializable]
    public struct TierPalette
    {
        public Color Common;
        public Color Rare;
        public Color Epic;
        public Color Legendary;

        [Tooltip("The Curse card's red. ART_DIRECTION §5 asks for red/black; §2 reserves ORANGE-red " +
                 "exclusively for hazard telegraphs and that reservation covers UI chrome, so this " +
                 "is the crimson the health bar already uses rather than the hazard accent.")]
        public Color Curse;

        /// <summary>The shipped values. Used as the default on every serialized field.</summary>
        public static TierPalette Default
        {
            get
            {
                return new TierPalette
                {
                    Common = new Color(0.78f, 0.79f, 0.82f, 1f),
                    Rare = new Color(0.42f, 0.60f, 0.86f, 1f),
                    Epic = new Color(0.66f, 0.45f, 0.86f, 1f),
                    Legendary = new Color(0.90f, 0.74f, 0.36f, 1f),
                    Curse = new Color(0.76f, 0.25f, 0.31f, 1f),
                };
            }
        }

        public Color ColourOf(UpgradeTier tier)
        {
            switch (tier)
            {
                case UpgradeTier.Rare: return Rare;
                case UpgradeTier.Epic: return Epic;
                case UpgradeTier.Legendary: return Legendary;
                case UpgradeTier.Common: return Common;

                // Explicit, not a fall-through default: a tier added later must show up as a
                // deliberate choice here rather than silently inheriting Common's grey.
                default: return Common;
            }
        }
    }
}
