using System;
using Deeper.Character;
using Deeper.Core;
using Deeper.Upgrades;
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

        [Tooltip("The run's weapon, which carries the relic that modifies this trait.")]
        [SerializeField] private RunLoadout loadout;

        [Tooltip("What the run is carrying, so this can tell whether the relic has been taken.")]
        [SerializeField] private RunUpgrades upgrades;

        /// <summary>Raised whenever the stack count changes, for the HUD.</summary>
        public event Action<int> StacksChanged;

        public int Stacks { get; private set; }

        public int StackCap { get { return stackCap; } }

        /// <summary>
        /// Whether the run carries this weapon's relic. Endless Edge (CONTENT_DESIGN §4) is the
        /// Katana's, and it is entirely expressed in this class's two existing knobs.
        /// </summary>
        public bool HasRelic { get; private set; }

        /// <summary>Per-stack bonus in force, which the relic reduces to pay for the missing cap.</summary>
        public float BonusPerStack
        {
            get { return HasRelic && Relic.ComboOverflow ? Relic.ComboBonusPerStack : bonusPerStack; }
        }

        /// <summary>Current damage multiplier, e.g. 1.20 at 10 stacks.</summary>
        public float DamageMultiplier { get { return 1f + Stacks * BonusPerStack; } }

        private RelicSpec Relic
        {
            get
            {
                return loadout != null && loadout.Weapon != null
                    ? loadout.Weapon.Relic
                    : default(RelicSpec);
            }
        }

        private void Awake()
        {
            // RigRefs.Find is legitimate here: all three live on the player rig and are always
            // populated, which is the only case it is safe for. It searches from transform.root,
            // so an optional field on anything spawned under a parent resolves against a sibling.
            owner = RigRefs.Find(this, owner);
            loadout = RigRefs.Find(this, loadout);
            upgrades = RigRefs.Find(this, upgrades);
        }

        private void OnEnable()
        {
            if (owner != null) owner.Damaged += HandleOwnerDamaged;

            if (upgrades != null) upgrades.Changed += RefreshRelic;
            RefreshRelic();
        }

        private void OnDisable()
        {
            if (owner != null) owner.Damaged -= HandleOwnerDamaged;
            if (upgrades != null) upgrades.Changed -= RefreshRelic;
        }

        /// <summary>
        /// Re-reads whether the run is carrying the weapon's relic.
        ///
        /// The trait asks the run, rather than the Secret Vault reaching in to switch it on. That
        /// keeps the vault ignorant of what any relic does — it hands over an upgrade and stops —
        /// and it means the same relic arriving from a Mini-Boss drop or a Hub guarantee needs no
        /// second wiring. The Greatsword's Mountain's Fall will read its own field the same way
        /// from UltimateGauge.
        /// </summary>
        private void RefreshRelic()
        {
            UpgradeDefinition offer = Relic.Offer;

            HasRelic = offer != null && upgrades != null && Carries(offer);

            // Stacks already banked are left alone. Taking Endless Edge mid-combo re-values the
            // stacks she has rather than clearing them, which is what "no cap" should feel like.
        }

        private bool Carries(UpgradeDefinition offer)
        {
            for (int i = 0; i < upgrades.Taken.Count; i++)
            {
                if (upgrades.Taken[i] == offer) return true;
            }

            return false;
        }

        /// <summary>
        /// A hit connected. Adds a stack, capped unless Combo Overflow is active — either from the
        /// upgrade of that name or from Endless Edge, which is the same effect bought differently.
        /// </summary>
        public void OnHitLanded()
        {
            bool uncapped = allowOverflow || (HasRelic && Relic.ComboOverflow);

            int next = Stacks + 1;
            if (!uncapped && next > stackCap) next = stackCap;
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
