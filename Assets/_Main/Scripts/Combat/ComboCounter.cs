using System;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// The Katana's Signature Trait (BALANCE.md §3): landed hits stack a damage bonus that resets
    /// the moment the player misses or takes damage.
    ///
    /// Values are serialized rather than const so BALANCE's placeholders can be retuned without a
    /// recompile (Design Rule 8). The Ultimate reads <see cref="Stacks"/> when it fires — Combo
    /// Finisher is "40 damage + 5 per Combo Counter stack consumed".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ComboCounter : MonoBehaviour
    {
        [Header("BALANCE.md §3 — Katana")]
        [Tooltip("Damage bonus per stack, as a fraction. 0.02 = +2%.")]
        [SerializeField] private float bonusPerStack = 0.02f;

        [Tooltip("Base stack cap. 10 stacks = +20% damage.")]
        [SerializeField] private int stackCap = 10;

        [Tooltip("Combo Overflow upgrade lets stacks climb past the cap; off at base.")]
        [SerializeField] private bool allowOverflow;

        [Tooltip("Flow State upgrade removes the reset-on-damage rule; off at base.")]
        [SerializeField] private bool keepStacksOnDamage;

        [Header("Refs — found anywhere on the player rig when empty")]
        [Tooltip("The player's own health. Taking a hit is what resets the combo (BALANCE §3).")]
        [SerializeField] private Damageable owner;

        /// <summary>Raised whenever the stack count changes, for the HUD.</summary>
        public event Action<int> StacksChanged;

        public int Stacks { get; private set; }

        public int StackCap { get { return stackCap; } }

        /// <summary>Current damage multiplier, e.g. 1.20 at 10 stacks.</summary>
        public float DamageMultiplier { get { return 1f + Stacks * bonusPerStack; } }

        private void Awake()
        {
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

        /// <summary>A hit connected. Adds a stack, capped unless Combo Overflow is active.</summary>
        public void OnHitLanded()
        {
            int next = Stacks + 1;
            if (!allowOverflow && next > stackCap) next = stackCap;
            Set(next);
        }

        /// <summary>An attack finished without connecting. BALANCE: resets instantly on miss.</summary>
        public void OnMissed()
        {
            Set(0);
        }

        /// <summary>The player took damage. Resets instantly unless Flow State is active.</summary>
        public void OnDamageTaken()
        {
            if (keepStacksOnDamage) return;
            Set(0);
        }

        /// <summary>Consumes the whole combo, returning what was spent — the Ultimate needs this.</summary>
        public int Consume()
        {
            int spent = Stacks;
            Set(0);
            return spent;
        }

        // The amount is ignored: any hit resets the combo, however small.
        private void HandleOwnerDamaged(float amount)
        {
            OnDamageTaken();
        }

        private void Set(int value)
        {
            if (value < 0) value = 0;
            if (value == Stacks) return;

            Stacks = value;
            StacksChanged?.Invoke(Stacks);
        }
    }
}
