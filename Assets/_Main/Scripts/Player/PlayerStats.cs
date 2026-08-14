using System;
using System.Collections.Generic;
using Deeper.Stats;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// Aggregates the player's stat values from a base line plus any number of registered
    /// modifier sources. Equipment is the first consumer; run upgrades and Hub Core Stats are
    /// meant to register through the same <see cref="SetSource"/> path rather than each system
    /// mutating player numbers directly.
    ///
    /// Base values are BALANCE.md placeholders and live in the inspector, not in code, so they
    /// can be retuned without a recompile (Design Rule 8).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Base values — BALANCE.md §1 (placeholders)")]
        [SerializeField] private float baseMaxHP = 100f;
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float baseDashCooldown = 1.2f;
        [SerializeField] private float baseDashDistance = 3f;

        [Header("Bonuses layered on top of weapon/base numbers")]
        [SerializeField] private float baseDamageBonus;
        [Tooltip("Multiplier applied to Ultimate Gauge gain per landed hit. 1 = unmodified.")]
        [SerializeField] private float baseUltimateGaugeGain = 1f;
        [Tooltip("Multiplier applied to Ore pickups. 1 = unmodified.")]
        [SerializeField] private float baseOreGain = 1f;
        [Tooltip("Flat damage subtracted per hit taken, as Iron Skin / Second Skin describe it.")]
        [SerializeField] private float baseDamageReduction;

        [Tooltip("Multiplier on attack phase speed. 1 = unmodified. Not a BALANCE stat — added " +
                 "for the owner-directed buff Ultimate.")]
        [SerializeField] private float baseAttackSpeed = 1f;

        private static readonly StatType[] AllStats = (StatType[])Enum.GetValues(typeof(StatType));

        private readonly Dictionary<object, StatModifier[]> _sources = new Dictionary<object, StatModifier[]>();
        private readonly Dictionary<StatType, float> _values = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _flat = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _percent = new Dictionary<StatType, float>();
        private bool _dirty = true;

        /// <summary>Raised whenever a modifier source is added, replaced or removed.</summary>
        public event Action Changed;

        public float MaxHP => Get(StatType.MaxHP);
        public float MoveSpeed => Get(StatType.MoveSpeed);
        public float DashCooldown => Get(StatType.DashCooldown);
        public float DashDistance => Get(StatType.DashDistance);
        public float DamageBonus => Get(StatType.DamageBonus);
        public float UltimateGaugeGain => Get(StatType.UltimateGaugeGain);
        public float OreGain => Get(StatType.OreGain);
        public float DamageReduction => Get(StatType.DamageReduction);
        public float AttackSpeed => Get(StatType.AttackSpeed);

        public float Get(StatType stat)
        {
            if (_dirty) Rebuild();
            return _values.TryGetValue(stat, out float value) ? value : GetBase(stat);
        }

        /// <summary>
        /// Registers or replaces a bundle of modifiers under <paramref name="key"/>. The key is
        /// the owning object (an equipment asset, an upgrade, a curse), so the same owner can be
        /// removed later without tracking individual modifiers.
        /// </summary>
        public void SetSource(object key, StatModifier[] modifiers)
        {
            if (key == null) return;

            if (modifiers == null || modifiers.Length == 0)
            {
                RemoveSource(key);
                return;
            }

            _sources[key] = modifiers;
            MarkDirty();
        }

        public void RemoveSource(object key)
        {
            if (key != null && _sources.Remove(key)) MarkDirty();
        }

        public bool HasSource(object key) => key != null && _sources.ContainsKey(key);

        private void MarkDirty()
        {
            _dirty = true;
            Changed?.Invoke();
        }

        private void Rebuild()
        {
            _dirty = false;
            _flat.Clear();
            _percent.Clear();

            foreach (StatModifier[] bundle in _sources.Values)
            {
                if (bundle == null) continue;

                for (int i = 0; i < bundle.Length; i++)
                {
                    StatModifier modifier = bundle[i];
                    Dictionary<StatType, float> target = modifier.Kind == ModifierKind.Percent ? _percent : _flat;
                    target.TryGetValue(modifier.Stat, out float running);
                    target[modifier.Stat] = running + modifier.Value;
                }
            }

            for (int i = 0; i < AllStats.Length; i++)
            {
                StatType stat = AllStats[i];
                _flat.TryGetValue(stat, out float flat);
                _percent.TryGetValue(stat, out float percent);
                _values[stat] = (GetBase(stat) + flat) * (1f + percent);
            }
        }

        private float GetBase(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHP: return baseMaxHP;
                case StatType.MoveSpeed: return baseMoveSpeed;
                case StatType.DashCooldown: return baseDashCooldown;
                case StatType.DashDistance: return baseDashDistance;
                case StatType.DamageBonus: return baseDamageBonus;
                case StatType.UltimateGaugeGain: return baseUltimateGaugeGain;
                case StatType.OreGain: return baseOreGain;
                case StatType.DamageReduction: return baseDamageReduction;
                case StatType.AttackSpeed: return baseAttackSpeed;
                default: return 0f;
            }
        }

        private void OnValidate()
        {
            // Recompute lazily; don't raise Changed from OnValidate — listeners may not exist yet.
            _dirty = true;
        }
    }
}
