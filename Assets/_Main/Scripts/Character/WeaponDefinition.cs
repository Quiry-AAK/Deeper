using System;
using Deeper.Animation;
using Deeper.Stats;
using UnityEngine;

namespace Deeper.Character
{
    /// <summary>The three weapons from GDD §Player. Values are stable — do not reorder.</summary>
    public enum WeaponType
    {
        Katana = 0,
        Bow = 1,
        Greatsword = 2,
    }

    /// <summary>The three actions every weapon exposes (CORE_SYSTEMS §1).</summary>
    public enum AttackAction
    {
        Basic = 0,
        Heavy = 1,
        Ultimate = 2,
    }

    /// <summary>
    /// One action's phase timings, straight from BALANCE.md §2. Authored per weapon so the
    /// Attack State Machine reads data instead of branching on weapon type.
    /// </summary>
    [Serializable]
    public struct AttackTiming
    {
        [Tooltip("Seconds before the hit becomes active.")]
        public float Windup;
        [Tooltip("Seconds the hit is live.")]
        public float Active;
        [Tooltip("Seconds of lockout after the hit. Dash-Attack Cancel is legal for this whole phase.")]
        public float Recovery;
        [Tooltip("Flat damage per hit (BALANCE §2). No crit system exists anywhere in the game.")]
        public float Damage;

        public float Total { get { return Windup + Active + Recovery; } }

        /// <summary>Which animation state this action drives.</summary>
        public static CharacterState StateFor(AttackAction action)
        {
            return StateFor(action, 0);
        }

        /// <summary>
        /// Which animation state drives hit <paramref name="chainIndex"/> of an action.
        ///
        /// The Basic chain has three distinct clips so the combo reads as three different cuts.
        /// The Heavy chain deliberately replays its one clip — ART_DIRECTION §46 makes that the
        /// rule for Heavy specifically, to stop the upgrade pool exploding the animation budget.
        /// </summary>
        public static CharacterState StateFor(AttackAction action, int chainIndex)
        {
            switch (action)
            {
                case AttackAction.Basic:
                    if (chainIndex <= 0) return CharacterState.BasicAttack;
                    return chainIndex == 1 ? CharacterState.BasicAttack2 : CharacterState.BasicAttack3;
                case AttackAction.Heavy: return CharacterState.HeavyStrike;
                default: return CharacterState.Ultimate;
            }
        }
    }

    /// <summary>
    /// One selectable weapon, chosen in the Hub and **locked for the whole run** (GDD §Player,
    /// CORE_SYSTEMS §1). There is no mid-run weapon switching and no inventory holding it — the
    /// run's choice lives on <see cref="RunLoadout"/>.
    ///
    /// This asset is the intended home for the per-weapon Windup/Active/Recovery timing data
    /// (BALANCE.md §2) once the Attack State Machine exists, so <c>GetAttackTiming()</c> reads
    /// authored data instead of hardcoded per-weapon constants. It carries no combat behaviour
    /// yet — selecting a weapon changes stats and the drawn layer only.
    /// </summary>
    /// <summary>
    /// What a weapon's Ultimate actually does.
    ///
    /// CORE_SYSTEMS §4 assumed every Ultimate is an attack, and <c>IWeapon.Ultimate()</c> was
    /// going to be written on that assumption. The Katana's is a buff (owner-directed), so the
    /// shape has to be **weapon data** rather than baked into the interface — otherwise the
    /// interface can only express one of the two and the second weapon breaks it.
    /// </summary>
    public enum UltimateKind
    {
        /// <summary>Deals damage, using the Ultimate's <see cref="AttackTiming"/> like any hit.</summary>
        Attack = 0,

        /// <summary>Grants <see cref="UltimateBuffSpec"/> for a duration. Deals no damage itself.</summary>
        Buff = 1,
    }

    /// <summary>
    /// The payload of a buff-shaped Ultimate. Data, not code, so retuning it never needs a
    /// recompile (Design Rule 8) and each weapon can carry its own.
    ///
    /// All values are PLACEHOLDERS — BALANCE has no row for a buff Ultimate, because the docs
    /// describe Ultimates as damage moves.
    /// </summary>
    [Serializable]
    public struct UltimateBuffSpec
    {
        [Tooltip("Seconds the buff lasts.")]
        public float Duration;
        [Tooltip("Extra damage, as a fraction. 0.5 = +50%.")]
        public float DamageBonus;
        [Tooltip("Attack speed multiplier bonus, as a fraction. 0.4 = swings 40% faster.")]
        public float AttackSpeedBonus;
        [Tooltip("Extra move speed, as a fraction.")]
        public float MoveSpeedBonus;
    }

    [CreateAssetMenu(fileName = "Weapon_", menuName = "Deeper/Character/Weapon", order = 1)]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id for save data. Falls back to the asset name when left empty.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private WeaponType weaponType = WeaponType.Katana;

        [Header("Presentation")]
        [Tooltip("Shown on the Hub weapon-select screen and the HUD.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Per-state/direction art layered onto the character rig. Preferred over Body Layer.")]
        [SerializeField] private SpriteAnimationSet animationSet;
        [Tooltip("Single static fallback used only when Animation Set is empty.")]
        [SerializeField] private Sprite bodyLayer;

        [Header("Attack timings — BALANCE.md §2")]
        [SerializeField] private AttackTiming basic = new AttackTiming { Windup = 0.10f, Active = 0.08f, Recovery = 0.18f, Damage = 8f };
        [SerializeField] private AttackTiming heavy = new AttackTiming { Windup = 0.30f, Active = 0.12f, Recovery = 0.35f, Damage = 20f };
        [Tooltip("BALANCE §2 gives no Windup/Active/Recovery row for Ultimates — these are placeholders pending a design answer.")]
        [SerializeField] private AttackTiming ultimate = new AttackTiming { Windup = 0.25f, Active = 0.40f, Recovery = 0.45f, Damage = 40f };

        [Header("Ultimate shape")]
        [Tooltip("Whether this weapon's Ultimate deals damage or grants a buff. The Katana's is " +
                 "a Buff (owner-directed); CORE_SYSTEMS §4 and BALANCE §2 still describe an attack.")]
        [SerializeField] private UltimateKind ultimateKind = UltimateKind.Attack;

        [Tooltip("Only used when Ultimate Kind is Buff. Placeholders — BALANCE has no buff row.")]
        [SerializeField] private UltimateBuffSpec ultimateBuff = new UltimateBuffSpec
        {
            Duration = 8f, DamageBonus = 0.5f, AttackSpeedBonus = 0.4f, MoveSpeedBonus = 0.15f,
        };

        [Header("Effects")]
        [Tooltip("Per-attack damage is timing-table data (BALANCE §2), not a stat block — most weapons carry none.")]
        [SerializeField] private StatModifier[] modifiers = new StatModifier[0];

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public WeaponType WeaponType => weaponType;
        public Sprite Icon => icon;
        public SpriteAnimationSet AnimationSet => animationSet;
        public Sprite BodyLayer => bodyLayer;
        public StatModifier[] Modifiers => modifiers;

        /// <summary>Resolves the layer sprite for a pose, falling back to the static sprite.</summary>
        public Sprite Resolve(CharacterState state, Facing facing, int frame)
        {
            return animationSet != null ? animationSet.Resolve(state, facing, frame) : bodyLayer;
        }

        /// <summary>
        /// Whether this weapon's Ultimate is an attack or a buff. <c>IWeapon.Ultimate()</c> must
        /// branch on this rather than assuming an attack, or it can only express one of the two.
        /// </summary>
        public UltimateKind UltimateShape => ultimateKind;

        /// <summary>The buff payload. Meaningless unless <see cref="UltimateShape"/> is Buff.</summary>
        public UltimateBuffSpec UltimateBuff => ultimateBuff;

        /// <summary>
        /// Phase timings for one action. This is the seam <c>IWeapon.GetAttackTiming()</c> will
        /// sit on when the interface is generalized in Milestone 2.
        /// </summary>
        public AttackTiming GetAttackTiming(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basic;
                case AttackAction.Heavy: return heavy;
                default: return ultimate;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
    }
}
