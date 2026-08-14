using System;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Health for anything that can be hit. The player and every enemy carry this same component, so
    /// the damage pipeline never has to ask who it just hit — <see cref="TakeDamage"/> is the one
    /// entry point, and <see cref="Damaged"/> is the one event the Ultimate Gauge, the Combo Counter
    /// and the hit flash all listen to (CORE_SYSTEMS §6).
    ///
    /// Max HP and damage reduction come from <see cref="PlayerStats"/> whenever the rig has one.
    /// That is deliberate: the player's HP has to track run upgrades and Hub Core Stats, and the
    /// alternative — a second set of health numbers living here — is exactly the duplicated stat
    /// pipeline PlayerStats exists to prevent. Enemies have no PlayerStats on their rig, so they
    /// fall back to the serialized value.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour
    {
        [Tooltip("Max HP, used when this rig has no PlayerStats. BALANCE §5's Cave Crawler is 20.\n\n" +
                 "On an enemy this is only a fallback: the value that actually applies is pushed " +
                 "in from its EnemyDefinition at Awake, so retuning an enemy's HP means editing " +
                 "the asset in Data/Enemies, not this field.")]
        [SerializeField] private float maxHealth = 20f;

        [Tooltip("Seconds of immunity after a hit lands. Leave at 0 for enemies — they should never " +
                 "get free frames. The player needs some, or standing against an enemy drains her " +
                 "at the physics tick rate instead of at the rate of its attacks.")]
        [SerializeField] private float invulnerabilityAfterHit;

        [Header("Refs — found anywhere on this rig when empty")]
        [Tooltip("When present, MaxHP and DamageReduction are read from the stat pipeline instead " +
                 "of from maxHealth above.")]
        [SerializeField] private PlayerStats stats;

        private float _health;
        private float _invulnerableUntil;

        /// <summary>Raised with the damage actually applied, after reduction. Never raised for 0.</summary>
        public event Action<float> Damaged;

        /// <summary>Raised once, the moment health reaches 0.</summary>
        public event Action Died;

        public float Health { get { return _health; } }

        public float MaxHealth { get { return stats != null ? stats.MaxHP : maxHealth; } }

        public float Normalized { get { return MaxHealth > 0f ? _health / MaxHealth : 0f; } }

        public bool IsAlive { get { return _health > 0f; } }

        private void Awake()
        {
            stats = RigRefs.Find(this, stats);

            // Filled here so this is never briefly at 0 HP: anything hit in the same frame it
            // spawned would read as already dead and silently refuse the hit.
            Refill();
        }

        private void Start()
        {
            // Filled again, because PlayerStats.MaxHP is only final once RunLoadout has registered
            // the weapon's modifiers, and Awake order between two components on one object is
            // undefined — so the Awake value can be a pre-loadout max. A no-op for enemies, whose
            // max never moves.
            //
            // Not yet handled: a mid-run Max HP upgrade (Milestone 4) raising MaxHealth after this
            // point. Current health would stay where it is rather than scaling with the new max.
            if (stats != null) Refill();
        }

        /// <summary>
        /// Applies damage. Returns false when nothing happened — already dead, or still inside the
        /// invulnerability window — so a caller can tell a real hit from a no-op and not count a
        /// swing into a corpse as having connected.
        /// </summary>
        public bool TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return false;

            // Unscaled, like every other timer in the feel layer (HitStop, HitVFX, CameraRig).
            // On scaled time a 0.6s window measured through a hitstop freeze would last tens of
            // real seconds, because the freeze runs the clock at 2% speed.
            if (Time.unscaledTime < _invulnerableUntil) return false;

            // BALANCE §10's Iron Skin is "-2 flat damage taken per hit", so reduction is flat and
            // subtracted per hit. Clamped at 0, not at 1: enough reduction should be able to shrug
            // a weak hit off completely, and a floor of 1 is a balance rule no design doc states.
            float reduction = stats != null ? stats.DamageReduction : 0f;
            float applied = Mathf.Max(0f, amount - reduction);
            if (applied <= 0f) return false;

            _health = Mathf.Max(0f, _health - applied);

            if (invulnerabilityAfterHit > 0f)
            {
                _invulnerableUntil = Time.unscaledTime + invulnerabilityAfterHit;
            }

            Damaged?.Invoke(applied);

            if (_health <= 0f) Died?.Invoke();
            return true;
        }

        /// <summary>
        /// Points this at a new maximum and fills to it. Enemies call this from their
        /// <c>EnemyDefinition</c>, so BALANCE §5's table lives in one asset per enemy rather than
        /// being retyped onto every prefab.
        ///
        /// **Refused when a <see cref="PlayerStats"/> is present.** The player's max HP is the
        /// output of the stat pipeline — run upgrades and Hub Core Stats feed it — and a second
        /// writer would silently win or lose depending on component order. Refusing here is what
        /// makes it impossible for enemy data to reach into the player's health by accident.
        ///
        /// Filling as part of setting is not a convenience: <see cref="Awake"/> already refilled
        /// against the serialized default, and Awake order between two components on one object is
        /// undefined, so a setter that only moved the ceiling would leave the enemy on whichever
        /// of the two values happened to run last.
        /// </summary>
        public void SetMaxHealth(float value)
        {
            if (stats != null)
            {
                Debug.LogWarning($"{nameof(Damageable)}: refused SetMaxHealth on a rig with " +
                                 "PlayerStats — max HP comes from the stat pipeline there.", this);
                return;
            }

            maxHealth = Mathf.Max(1f, value);
            Refill();
        }

        /// <summary>Restores to full and clears any active immunity. The training dummy respawns through this.</summary>
        [ContextMenu("Refill")]
        public void Refill()
        {
            _health = MaxHealth;
            _invulnerableUntil = 0f;
        }
    }
}
