using System;
using Deeper.Character;
using Deeper.Core;
using Deeper.Player;
using Deeper.Stats;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// The Katana Ultimate, as a timed self-buff rather than a damage move.
    ///
    /// OWNER-DIRECTED DESIGN CHANGE. CORE_SYSTEMS §4 and BALANCE §2 describe the Ultimate as an
    /// attack that deals damage (40 for the Katana). The owner's direction is that it is instead
    /// a short cast — she raises the katana — that grants an aura for a few seconds, during which
    /// she hits harder and every swing trails that aura. Recorded in Docs/00-DESIGN_CHANGE_BRIEF.md
    /// rather than silently diverging.
    ///
    /// Nothing here touches player numbers directly: the buff registers a named bundle of
    /// <see cref="StatModifier"/>s through <see cref="PlayerStats.SetSource"/> and removes it by
    /// the same key when it expires, which is the same pipeline run upgrades and Hub Core Stats
    /// will use. Re-casting refreshes the duration instead of stacking a second copy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UltimateBuff : MonoBehaviour
    {
        /// <summary>Key for this buff's modifier bundle. Re-using it is what prevents stacking.</summary>
        private static readonly object SourceKey = new object();

        [Tooltip("Used only if the run's weapon carries no buff spec of its own. The real values " +
                 "live on WeaponDefinition, so each weapon's Ultimate can differ without code.")]
        [SerializeField] private UltimateBuffSpec fallback = new UltimateBuffSpec
        {
            Duration = 8f, DamageBonus = 0.5f, AttackSpeedBonus = 0.4f, MoveSpeedBonus = 0.15f,
        };

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private PlayerStats stats;

        private float _remaining;
        private float _duration;

        /// <summary>True while the aura is up. The aura visuals and attack trails read this.</summary>
        public bool IsActive { get { return _remaining > 0f; } }

        /// <summary>Remaining fraction, 1 at cast down to 0. Drives the HUD and the aura's fade-out.</summary>
        public float Normalized { get { return _duration > 0f ? Mathf.Clamp01(_remaining / _duration) : 0f; } }

        /// <summary>Raised when the aura comes up or drops, so visuals can latch rather than poll.</summary>
        public event Action<bool> ActiveChanged;

        private void Awake()
        {
            stats = RigRefs.Find(this, stats);
        }

        /// <summary>Starts the aura with this weapon's values, or refreshes it if already up.</summary>
        public void Activate(UltimateBuffSpec spec)
        {
            bool was = IsActive;

            _duration = spec.Duration > 0f ? spec.Duration : fallback.Duration;
            _remaining = _duration;

            if (stats != null)
            {
                stats.SetSource(SourceKey, new[]
                {
                    // DamageBonus is FLAT, not Percent. It is itself a fraction whose base is 0,
                    // and PlayerStats computes (base + flat) * (1 + percent) — so a percent
                    // modifier on a zero base multiplies to zero and the buff would silently do
                    // nothing. Flat 0.5 is the +50% that was intended.
                    new StatModifier(StatType.DamageBonus, ModifierKind.Flat, spec.DamageBonus),

                    // These two have non-zero bases (1 and BALANCE's move speed), so percent is
                    // correct for them and matches "percentages are additive unless noted".
                    new StatModifier(StatType.AttackSpeed, ModifierKind.Percent, spec.AttackSpeedBonus),
                    new StatModifier(StatType.MoveSpeed, ModifierKind.Percent, spec.MoveSpeedBonus),
                });
            }

            if (!was) ActiveChanged?.Invoke(true);
        }

        /// <summary>Starts the aura using this component's fallback values.</summary>
        public void Activate() => Activate(fallback);

        private void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;
            if (_remaining > 0f) return;

            _remaining = 0f;
            if (stats != null) stats.RemoveSource(SourceKey);
            ActiveChanged?.Invoke(false);
        }

        private void OnDisable()
        {
            // Leaving a permanent damage bonus behind on a disabled component would be invisible
            // and very hard to trace later.
            if (!IsActive) return;

            _remaining = 0f;
            if (stats != null) stats.RemoveSource(SourceKey);
            ActiveChanged?.Invoke(false);
        }
    }
}
