using System;
using System.Collections.Generic;
using Deeper.Animation;
using Deeper.Character;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// The volume a swing actually hits. Opens when <see cref="AttackStateMachine.ActivePhaseOpened"/>
    /// fires, stays live for the action's authored Active duration, and reports back what it found so
    /// the feel layer can react to contact instead of to the swing.
    ///
    /// Placed and sized from the character's facing rather than as a child collider, because the five
    /// directions were drawn as five different swings — one fixed child collider would sit in the
    /// wrong place for four of them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackHitbox : MonoBehaviour
    {
        [Header("Reach — must match where the swing animation's arc lands")]
        [Tooltip("Horizontal reach of the hit volume's centre, in world units. The slash arc is " +
                 "drawn inside the attack frames, so this is the one number keeping damage where " +
                 "the blade visibly goes; if it drifts, hits land where nothing is happening.")]
        [SerializeField] private float forwardOffset = 0.55f;

        [Tooltip("Vertical reach. Larger than horizontal on purpose — her sprite is ~1.44 units " +
                 "tall but ~1 wide and the transform sits at chest height, so an equal offset puts " +
                 "the volume inside her own body.")]
        [SerializeField] private float verticalOffset = 1.05f;

        [Tooltip("Radius of the hit volume per action, in world units. Heavier actions reach wider.")]
        [SerializeField] private float basicRadius = 0.8f;
        [SerializeField] private float heavyRadius = 1f;
        [SerializeField] private float ultimateRadius = 1.3f;

        [Header("Targets")]
        [Tooltip("Layers a swing can hit. Enemy only — leaving Default in would make every swing " +
                 "connect with the walls.")]
        [SerializeField] private LayerMask targetLayers = 1 << 7;

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private AttackStateMachine attacks;
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private RunLoadout loadout;
        [SerializeField] private ComboCounter combo;
        [SerializeField] private PlayerStats stats;

        [Tooltip("This rig's own health, so a swing can never hit its owner.")]
        [SerializeField] private Damageable owner;

        // Reused rather than allocated per sweep: this runs every frame of every Active window.
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private readonly HashSet<Damageable> _alreadyHit = new HashSet<Damageable>();
        private ContactFilter2D _filter;

        private AttackAction _action;
        private Vector2 _offset;
        private float _radius;
        private float _damage;
        private float _remaining;
        private bool _open;
        private bool _connected;

        /// <summary>
        /// A target was hit. Raised once per target, so a swing that catches three enemies raises it
        /// three times — the listener decides whether that is three events or one landed hit.
        /// </summary>
        public event Action<AttackAction, Damageable, float> Landed;

        /// <summary>The Active window closed having touched nothing. A whiff.</summary>
        public event Action<AttackAction> Missed;

        private void Awake()
        {
            attacks = RigRefs.Find(this, attacks);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            loadout = RigRefs.Find(this, loadout);
            combo = RigRefs.Find(this, combo);
            stats = RigRefs.Find(this, stats);
            owner = RigRefs.Find(this, owner);

            // useTriggers stays false: a swing should connect with a body, not with a pickup volume
            // or a hazard's trigger. SetLayerMask is what switches layer filtering on.
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(targetLayers);
        }

        private void OnEnable()
        {
            if (attacks != null) attacks.ActivePhaseOpened += HandleActivePhase;
        }

        private void OnDisable()
        {
            if (attacks != null) attacks.ActivePhaseOpened -= HandleActivePhase;
            _open = false;
        }

        private void HandleActivePhase(AttackAction action, AttackTiming timing)
        {
            WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;

            // A buff-shaped Ultimate is not a strike at all — the Katana's grants a timed self-buff
            // — so it opens no volume and counts as neither a hit nor a whiff. Without this guard it
            // would also deal its authored 40 damage on top of the buff.
            if (action == AttackAction.Ultimate && weapon != null && weapon.UltimateShape == UltimateKind.Buff)
            {
                return;
            }

            _action = action;
            _radius = action == AttackAction.Basic ? basicRadius
                : action == AttackAction.Heavy ? heavyRadius : ultimateRadius;
            _remaining = Mathf.Max(0f, timing.Active);
            _open = true;
            _connected = false;
            _alreadyHit.Clear();

            // Direction is locked for the whole window, matching the lunge and the clip. A volume
            // that swivelled mid-swing would let the player sweep a full circle out of one press.
            Vector2 direction = characterAnimator != null ? characterAnimator.Facing.ToVector() : Vector2.down;
            float depth = Mathf.Abs(direction.y);
            _offset = direction * Mathf.Lerp(forwardOffset, verticalOffset, depth);

            // Damage is fixed at open, before this swing's own combo stack is added, so a hit is
            // worth what the counter said when the blade started moving.
            float multiplier = combo != null ? combo.DamageMultiplier : 1f;

            // DamageBonus is a Flat modifier that carries a FRACTION rather than flat damage — see
            // the comment where UltimateBuff registers it, and why a Percent modifier cannot work on
            // a stat whose base is 0.
            float bonus = stats != null ? stats.DamageBonus : 0f;
            _damage = timing.Damage * multiplier * (1f + bonus);

            // Sweep immediately. The Katana's Basic Active window is 0.08s — about five frames — so
            // waiting for the next Update would throw away the frame the blade actually lands on.
            Sweep();
        }

        private void Update()
        {
            if (!_open) return;

            // Scaled time, unlike the VFX timers: the hit window is gameplay, and hitstop is meant
            // to freeze gameplay. An unscaled window would keep dealing damage during the freeze.
            _remaining -= Time.deltaTime;
            Sweep();

            if (_remaining > 0f) return;

            _open = false;
            if (!_connected) Missed?.Invoke(_action);
        }

        private void Sweep()
        {
            Vector2 centre = (Vector2)transform.position + _offset;
            int count = Physics2D.OverlapCircle(centre, _radius, _filter, _overlaps);

            for (int i = 0; i < count; i++)
            {
                // GetComponentInParent, because a collider usually sits on the body while health
                // sits on the rig root.
                Damageable target = _overlaps[i] != null
                    ? _overlaps[i].GetComponentInParent<Damageable>()
                    : null;

                if (target == null || target == owner) continue;

                // One chance per target per swing. Marked before the damage call, so a target that
                // refuses (dead, or in its own i-frames) is not retried for the rest of the window.
                if (!_alreadyHit.Add(target)) continue;

                if (!target.TakeDamage(_damage)) continue;

                _connected = true;
                Landed?.Invoke(_action, target, _damage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_open) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)transform.position + _offset, _radius);
        }
    }
}
