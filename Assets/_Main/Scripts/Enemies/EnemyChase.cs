using Deeper.Animation;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Walks the enemy toward its target, holds at <c>StopDistance</c>, and backs off below
    /// <c>RetreatDistance</c>.
    ///
    /// One class covers all four enemies because the Rock Slinger's kiting is the *same* behaviour
    /// with a non-zero retreat distance, not a different one — the functional differentiation
    /// Design Rule 4 asks for lives in the attack, not in the walking. Melee enemies leave retreat
    /// at 0 and simply never use that branch.
    ///
    /// Facing is driven toward the target every frame rather than by movement, because a melee
    /// enemy sitting at its stop distance is not moving and would otherwise keep whatever facing
    /// it arrived with while the player circled it — and then attack in that stale direction. A
    /// side effect worth having: with facing pinned to the target, walking away plays the walk
    /// cycle in reverse, so a retreating Slinger backpedals instead of turning its back.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyChase : MonoBehaviour
    {
        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;
        [SerializeField] private EnemyTarget target;

        [Tooltip("Movement yields while this is running: the wind-up and recovery plant the " +
                 "enemy, and the Active window belongs to whatever the attack does with it.")]
        [SerializeField] private TelegraphedAttack attack;

        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Feel — momentum")]
        [Tooltip("Seconds to reach full speed. Kept short: an enemy that ramps slowly reads as " +
                 "sliding on ice rather than as heavy.")]
        [SerializeField] private float accelerationTime = 0.12f;

        [Tooltip("Seconds to coast to a stop. Slightly longer than acceleration, matching the " +
                 "asymmetry the player controller uses to make movement feel weighty.")]
        [SerializeField] private float decelerationTime = 0.16f;

        private Rigidbody2D _body;
        private Vector2 _velocity;
        private Vector2 _velocitySmoothing;
        private Vector2 _moveDirection;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            enemy = RigRefs.Find(this, enemy);
            target = RigRefs.Find(this, target);
            attack = RigRefs.Find(this, attack);
            characterAnimator = RigRefs.Find(this, characterAnimator);
        }

        private void OnDisable()
        {
            // Death disables this; without zeroing, the corpse keeps drifting for a frame.
            if (_body != null) _body.linearVelocity = Vector2.zero;
            _velocity = Vector2.zero;
            _velocitySmoothing = Vector2.zero;
            _moveDirection = Vector2.zero;
        }

        private void Update()
        {
            if (characterAnimator == null) return;

            if (target != null && target.Acquired && target.Direction != Vector2.zero)
            {
                characterAnimator.SetFacing(CharacterAnimator.FromDirection(target.Direction));
            }
            else
            {
                characterAnimator.ClearFacingOverride();
            }

            characterAnimator.SetMotion(_moveDirection);
        }

        private void FixedUpdate()
        {
            AttackPhase phase = attack != null ? attack.Phase : AttackPhase.Idle;

            // The Active window is the attack's: a lunge writes velocity here, and anything else
            // simply coasts. Writing our own would fight it.
            if (phase == AttackPhase.Active) return;

            Vector2 desired = phase == AttackPhase.Idle ? DesiredVelocity() : Vector2.zero;

            _moveDirection = desired.sqrMagnitude > 0.000001f ? desired.normalized : Vector2.zero;

            float smoothing = desired.sqrMagnitude > 0.000001f ? accelerationTime : decelerationTime;
            _velocity = smoothing <= 0f
                ? desired
                : Vector2.SmoothDamp(_velocity, desired, ref _velocitySmoothing, smoothing,
                                     Mathf.Infinity, Time.fixedDeltaTime);

            _body.linearVelocity = _velocity;
        }

        private Vector2 DesiredVelocity()
        {
            EnemyDefinition definition = enemy != null ? enemy.Definition : null;
            if (definition == null || target == null || !target.Acquired) return Vector2.zero;

            float distance = target.Distance;

            if (distance > definition.StopDistance) return target.Direction * definition.MoveSpeed;
            if (distance < definition.RetreatDistance) return -target.Direction * definition.MoveSpeed;

            // Inside the band between retreat and stop: hold the line.
            return Vector2.zero;
        }
    }
}
