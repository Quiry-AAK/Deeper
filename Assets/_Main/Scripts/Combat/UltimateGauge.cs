using System;
using Deeper.Character;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Resource gate for the Ultimate, replacing a cooldown entirely (CORE_SYSTEMS §4). Fills on
    /// landed hits and on hits taken, drains fully on use, and is the only thing that makes
    /// <c>Ultimate()</c> legal.
    ///
    /// Fill rates are per weapon *and* per action, so they live here as a small table rather than
    /// being hardcoded per weapon — the same component serves the Bow and Greatsword once those
    /// exist.
    ///
    /// **Two owner-directed divergences from locked design**, both recorded in the engineering plan
    /// rather than silently applied: the rates are a flat 1% where BALANCE §4 authors 8/15 per
    /// Katana action, and taking damage fills the gauge at base where CORE_SYSTEMS §4 has that as
    /// the "Gauge: Vengeance" upgrade only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UltimateGauge : MonoBehaviour
    {
        [Serializable]
        private struct FillRate
        {
            public WeaponType Weapon;
            [Tooltip("Percent added per landed Basic Attack.")]
            public float Basic;
            [Tooltip("Percent added per landed Heavy Strike.")]
            public float Heavy;
        }

        [Header("Gauge gain — OWNER-DIRECTED, diverges from BALANCE.md §4")]
        [Tooltip("Percent added per landed hit. 1% flat for every weapon and action, by owner " +
                 "direction. BALANCE §4 authors these as Katana 8/15, Bow 6/15 and Greatsword " +
                 "10/20 — that table is recorded in the engineering plan, not lost.")]
        [SerializeField] private FillRate[] fillRates =
        {
            new FillRate { Weapon = WeaponType.Katana,     Basic = 1f, Heavy = 1f },
            new FillRate { Weapon = WeaponType.Bow,        Basic = 1f, Heavy = 1f },
            new FillRate { Weapon = WeaponType.Greatsword, Basic = 1f, Heavy = 1f },
        };

        [Tooltip("Percent added when the player is hit, so defensive play still builds toward an " +
                 "Ultimate. Owner-directed base behaviour; CORE_SYSTEMS §4 has gain-on-damage as " +
                 "the 'Gauge: Vengeance' upgrade instead.")]
        [SerializeField] private float gainOnDamageTaken = 1f;

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private RunLoadout loadout;

        [Tooltip("Read for the UltimateGaugeGain multiplier — the Hub Core Stat and upgrade knob " +
                 "for every source of fill.")]
        [SerializeField] private PlayerStats stats;

        [Tooltip("The player's own health. Taking a hit feeds the gauge through this.")]
        [SerializeField] private Damageable owner;

        private const float Max = 100f;

        /// <summary>Raised with the 0–1 fill fraction whenever it changes.</summary>
        public event Action<float> Changed;

        /// <summary>Raised the moment the gauge reaches full — ART_DIRECTION §6 wants a HUD pulse here.</summary>
        public event Action Filled;

        public float Value { get; private set; }

        public float Normalized { get { return Value / Max; } }

        public bool IsFull { get { return Value >= Max; } }

        private void Awake()
        {
            loadout = RigRefs.Find(this, loadout);
            stats = RigRefs.Find(this, stats);
            owner = RigRefs.Find(this, owner);
        }

        private void OnEnable()
        {
            if (owner != null) owner.Damaged += HandleOwnerDamaged;
        }

        private void OnDisable()
        {
            if (owner != null) owner.Damaged -= HandleOwnerDamaged;
        }

        /// <summary>Adds this weapon's gauge gain for a landed hit of the given action.</summary>
        public void OnHitLanded(AttackAction action)
        {
            // The Ultimate itself never feeds the gauge it spends.
            if (action == AttackAction.Ultimate) return;

            WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;
            if (weapon == null) return;

            float gain = 0f;
            for (int i = 0; i < fillRates.Length; i++)
            {
                if (fillRates[i].Weapon != weapon.WeaponType) continue;
                gain = action == AttackAction.Heavy ? fillRates[i].Heavy : fillRates[i].Basic;
                break;
            }

            Add(gain);
        }

        /// <summary>The player was hit. A flat percentage, deliberately not scaled by how hard.</summary>
        public void OnDamageTaken()
        {
            Add(gainOnDamageTaken);
        }

        public void Add(float percent)
        {
            if (percent <= 0f || IsFull) return;

            // UltimateGaugeGain is the Hub Core Stat and upgrade multiplier named for exactly this
            // (CONTENT_DESIGN §Core Stats). Applying it here rather than at each call site is what
            // keeps one place governing every source of fill.
            float gain = percent * (stats != null ? stats.UltimateGaugeGain : 1f);
            if (gain <= 0f) return;

            Value = Mathf.Min(Max, Value + gain);
            Changed?.Invoke(Normalized);

            // Reaching here means the gauge was not full on entry, so full now means just-filled.
            if (IsFull) Filled?.Invoke();
        }

        /// <summary>Spends the gauge. Returns false when not full, which is what gates the Ultimate.</summary>
        public bool TrySpend()
        {
            if (!IsFull) return false;

            Value = 0f;
            Changed?.Invoke(Normalized);
            return true;
        }

        // The damage amount is ignored on purpose: gain-on-damage is a flat percentage, so a chip
        // hit and a boss slam are worth the same. Scaling it by damage would make the gauge fill
        // fastest exactly when the player is closest to dying.
        private void HandleOwnerDamaged(float amount)
        {
            OnDamageTaken();
        }

        [ContextMenu("Fill Gauge")]
        private void DebugFill()
        {
            Add(Max);
        }
    }
}
