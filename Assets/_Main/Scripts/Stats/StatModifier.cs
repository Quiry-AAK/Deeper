using System;
using UnityEngine;

namespace Deeper.Stats
{
    /// <summary>
    /// The stat vocabulary shared by every system that can change a player number.
    /// Deliberately mirrors the Hub Core Stats table (BALANCE.md §15) plus the flat
    /// damage-reduction the survivability upgrades use (Iron Skin / Second Skin), so
    /// equipment, run upgrades (Milestone 4) and Hub stats (Milestone 6) all speak the
    /// same language instead of each inventing their own.
    /// </summary>
    public enum StatType
    {
        MaxHP = 0,
        MoveSpeed = 1,
        DashCooldown = 2,
        DashDistance = 3,
        DamageBonus = 4,
        UltimateGaugeGain = 5,
        OreGain = 6,
        DamageReduction = 7,
    }

    /// <summary>
    /// Flat modifiers are summed into the base value; percent modifiers are summed with each
    /// other and applied once afterwards — BALANCE.md's "percentages are additive unless noted".
    /// </summary>
    public enum ModifierKind
    {
        Flat = 0,
        Percent = 1,
    }

    [Serializable]
    public struct StatModifier
    {
        [SerializeField] private StatType stat;
        [SerializeField] private ModifierKind kind;
        [SerializeField] private float value;

        public StatModifier(StatType stat, ModifierKind kind, float value)
        {
            this.stat = stat;
            this.kind = kind;
            this.value = value;
        }

        public StatType Stat => stat;
        public ModifierKind Kind => kind;

        /// <summary>Flat: raw units. Percent: fraction, so 0.1f is +10% and -0.2f is -20%.</summary>
        public float Value => value;

        public override string ToString()
        {
            return kind == ModifierKind.Percent
                ? $"{stat} {value:+0.##%;-0.##%;+0%}"
                : $"{stat} {value:+0.##;-0.##;+0}";
        }
    }
}
