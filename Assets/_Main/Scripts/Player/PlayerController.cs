using Deeper.Animation;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Player
{
    /// <summary>
    /// 8-directional top-down movement, per GDD §Player. Speed is read from
    /// <see cref="PlayerStats"/> every frame, so equipment and (later) upgrades and Hub stats change
    /// it without touching this class.
    ///
    /// GDD §Player specifies a *fixed* speed with no acceleration curve; the short ramp below is
    /// owner-directed and diverges from it, recorded in <c>Docs/00-DESIGN_CHANGE_BRIEF.md</c> §7d.
    ///
    /// Input comes from the <c>.inputactions</c> asset rather than hardcoded keys, so rebinding
    /// stays a data change (see the Milestone 1 input risk note in the engineering plan).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("Attacks drive movement instead of input: the character lunges into the cut and " +
                 "cannot be steered mid-swing. Replaces the earlier root-in-place behaviour, which " +
                 "made a fast weapon feel weightless.")]
        [SerializeField] private AttackStateMachine attacks;

        [Tooltip("The Dig-Dash owns movement while it runs, the same way an attack does — it " +
                 "publishes a velocity and this controller applies it.")]
        [SerializeField] private DigDash dash;

        [Header("Feel — momentum")]
        [Tooltip("Seconds to reach full speed from a standstill. Owner-directed: the GDD specified " +
                 "no acceleration curve, but flat velocity reads robotic. Kept short so the " +
                 "controls stay crisp, which is what that rule was protecting.")]
        [SerializeField] private float accelerationTime = 0.055f;

        [Tooltip("Seconds to coast to a stop after releasing input. Slightly longer than " +
                 "acceleration — that asymmetry is what makes movement feel weighty rather than " +
                 "floaty.")]
        [SerializeField] private float decelerationTime = 0.085f;

        private Vector2 _velocity;
        private Vector2 _velocitySmoothing;

        private Rigidbody2D _body;
        private InputAction _moveAction;
        private Vector2 _input;

        private Vector2 _knockback;
        private float _knockbackRemaining;
        private float _knockbackDuration;

        /// <summary>Current movement input, already clamped to length 1.</summary>
        public Vector2 MoveInput => _input;

        /// <summary>True while a knockback is still carrying her.</summary>
        public bool IsKnockedBack => _knockbackRemaining > 0f;

        /// <summary>
        /// Pushes her, overriding input and any attack lunge until it decays.
        ///
        /// This has to be velocity the controller applies, not an <c>AddForce</c> on the rigidbody:
        /// <see cref="FixedUpdate"/> writes <c>linearVelocity</c> outright every tick, so an
        /// impulse is erased on the same physics step it was added — the knockback would look like
        /// it simply did not happen.
        ///
        /// The Tunnel Brute's slam is the first caller (CONTENT_DESIGN §5, "telegraphed overhead
        /// slam with knockback"). BALANCE gives no magnitude for it, so the numbers live on the
        /// slam.
        /// </summary>
        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            if (duration <= 0f) return;

            _knockback = velocity;
            _knockbackDuration = duration;
            _knockbackRemaining = duration;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            stats = RigRefs.Find(this, stats);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            attacks = RigRefs.Find(this, attacks);
            dash = RigRefs.Find(this, dash);

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerController)}: no input asset assigned; the player will not move.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            _moveAction = map != null ? map.FindAction(moveActionName, false) : null;

            if (_moveAction == null)
            {
                Debug.LogError($"{nameof(PlayerController)}: action '{actionMapName}/{moveActionName}' not found in {inputActions.name}.", this);
            }
        }

        private void OnEnable()
        {
            if (_moveAction != null) _moveAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null) _moveAction.Disable();

            // Don't leave the body drifting if this is disabled mid-stride.
            if (_body != null) _body.linearVelocity = Vector2.zero;
            _input = Vector2.zero;
            _velocity = Vector2.zero;
            _velocitySmoothing = Vector2.zero;
            _knockbackRemaining = 0f;
        }

        private void Update()
        {
            Vector2 raw = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            // Clamp rather than normalize: keeps analog sticks analog while making sure a
            // diagonal on the keyboard isn't faster than a cardinal.
            _input = Vector2.ClampMagnitude(raw, 1f);

            // Skipped while dashing. PlayAction already stops SetMotion overwriting the *state*,
            // but SetMotion also updates facing and playback direction — and the dash locked its
            // direction at launch, so letting movement input keep steering the facing would draw
            // her turning while she travels in a straight line.
            bool dashing = dash != null && dash.IsDashing;
            if (characterAnimator != null && !dashing) characterAnimator.SetMotion(_input);
        }

        private void FixedUpdate()
        {
            // Knockback outranks both input and the attack lunge, and is checked first for that
            // reason: being slammed should interrupt a swing, not lose a tug-of-war with it.
            if (_knockbackRemaining > 0f)
            {
                _knockbackRemaining -= Time.fixedDeltaTime;

                // Ease-out, matching the attack lunge — a flat push that stops dead reads as a
                // teleport, where a decaying one reads as being knocked off balance.
                float remaining = Mathf.Clamp01(_knockbackRemaining / _knockbackDuration);
                _velocity = _knockback * remaining;
                _velocitySmoothing = Vector2.zero;
                _body.linearVelocity = _velocity;
                return;
            }

            // The dash sits below knockback and above the attack lunge, and both halves of that are
            // forced. Below knockback, because a dash that shrugged off a Brute slam would be
            // i-frames *plus* immunity to displacement, which no design doc grants. Above the
            // lunge, because CORE_SYSTEMS §2 requires the dash to cancel an attack in Recovery —
            // if the lunge branch ran first it would still own velocity on the frame the dash
            // starts, and the cancel would look like it did nothing.
            if (dash != null && dash.IsDashing)
            {
                _velocity = dash.DashVelocity;
                _velocitySmoothing = Vector2.zero;
                _body.linearVelocity = _velocity;
                return;
            }

            // Once an attack is COMMITTED the swing owns movement: the lunge carries the character
            // forward and decays to zero, so late in an attack she coasts to a stop rather than
            // snapping still.
            //
            // IsCommitted rather than IsAttacking, and that is still right even though a charge now
            // roots her (owner, 2026-08-16): the rooting is done by chargeMoveScale = 0 further
            // down, NOT by treating Charging as committed. Taking this branch while Charging would
            // hand movement to LungeVelocity, whose ease-out reads _elapsed — still zero during a
            // hold — and so reports peak lunge speed for the entire charge.
            if (attacks != null && attacks.IsCommitted)
            {
                _velocity = attacks.LungeVelocity;
                _velocitySmoothing = Vector2.zero;
                _body.linearVelocity = _velocity;
                return;
            }

            float speed = stats != null ? stats.MoveSpeed : 0f;
            if (attacks != null) speed *= attacks.MoveSpeedScale;

            Vector2 target = _input * speed;

            // Ramping in and out rather than snapping is most of what separates "crisp" from
            // "robotic". Stopping is given the longer time so she carries a little momentum.
            float smoothing = _input.sqrMagnitude > 0.0001f ? accelerationTime : decelerationTime;
            _velocity = smoothing <= 0f
                ? target
                : Vector2.SmoothDamp(_velocity, target, ref _velocitySmoothing, smoothing,
                                     Mathf.Infinity, Time.fixedDeltaTime);

            _body.linearVelocity = _velocity;
        }
    }
}
