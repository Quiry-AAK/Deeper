using System;
using Deeper.Upgrades;
using UnityEngine;

namespace Deeper.UI
{
    /// <summary>
    /// The icon-led offer card's two sprite lookups — which category glyph, which tier pip badge —
    /// mirroring <see cref="TierPalette"/>'s shape: a serialized struct rather than a static table,
    /// so the art stays swappable in the Inspector without a recompile, and one foldout each on
    /// <see cref="UpgradeCard"/> rather than seventeen loose <c>Sprite</c> fields.
    ///
    /// Each lookup is an explicit <c>switch</c>, never a two-step ternary — CLAUDE.md's enum-ternary
    /// trap applies here exactly as it does everywhere else in this project: a category or tier
    /// added later must show up as a deliberate <c>default</c> case, not silently inherit whichever
    /// branch happened to be written last.
    /// </summary>
    [Serializable]
    public struct CategoryGlyphs
    {
        public Sprite Health;
        public Sprite Defense;
        public Sprite Damage;
        public Sprite OnHit;
        public Sprite Movement;
        public Sprite Dash;
        public Sprite Experience;
        public Sprite HeavyStrike;
        public Sprite Combo;
        public Sprite Gauge;
        public Sprite Ultimate;
        public Sprite Utility;

        /// <summary>
        /// Keyed by enum name to <c>Cat_&lt;name&gt;.png</c> under <c>Art/UI/Icons/</c> —
        /// <see cref="Deeper.EditorTools.BuildUpgradePanel"/> wires each field from that same file
        /// name, so the enum value, this switch and the art file all move together as one coupling.
        /// </summary>
        public Sprite Of(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.Health: return Health;
                case UpgradeCategory.Defense: return Defense;
                case UpgradeCategory.Damage: return Damage;
                case UpgradeCategory.OnHit: return OnHit;
                case UpgradeCategory.Movement: return Movement;
                case UpgradeCategory.Dash: return Dash;
                case UpgradeCategory.Experience: return Experience;
                case UpgradeCategory.HeavyStrike: return HeavyStrike;
                case UpgradeCategory.Combo: return Combo;
                case UpgradeCategory.Gauge: return Gauge;
                case UpgradeCategory.Ultimate: return Ultimate;
                case UpgradeCategory.Utility: return Utility;

                // Explicit, not a fall-through default: a category added later must show up as a
                // deliberate choice here rather than silently drawing no glyph and no warning.
                default: return null;
            }
        }
    }

    /// <summary>
    /// The tier pip badges drawn geometry in <see cref="Deeper.EditorTools.HUDFrameArt"/>
    /// (<c>HUD_TierCommon/Rare/Epic/Legendary/Curse</c>) — near-white so <see cref="UpgradeCard"/>
    /// tints each one through <see cref="TierPalette"/>, the same way the card frame already does.
    /// </summary>
    [Serializable]
    public struct TierBadges
    {
        public Sprite Common;
        public Sprite Rare;
        public Sprite Epic;
        public Sprite Legendary;
        public Sprite Curse;

        public Sprite Of(UpgradeTier tier)
        {
            switch (tier)
            {
                case UpgradeTier.Rare: return Rare;
                case UpgradeTier.Epic: return Epic;
                case UpgradeTier.Legendary: return Legendary;
                case UpgradeTier.Common: return Common;

                // Explicit, not a fall-through default: a tier added later must show up as a
                // deliberate choice here rather than silently drawing no badge.
                default: return null;
            }
        }
    }
}
