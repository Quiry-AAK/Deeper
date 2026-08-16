using Deeper.Animation;
using Deeper.CameraControl;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Player
{
    /// <summary>
    /// Mouse aiming, Children of Morta style: WASD moves, the cursor decides which way she faces
    /// and where attacks land.
    ///
    /// This is what makes melee feel deliberate rather than incidental — you hit what you pointed
    /// at, not whatever happened to be in your path. It costs no extra animation, because aim
    /// resolves onto the same eight facings the rig already draws with five authored directions.
    ///
    /// The one thing it introduces is a movement/facing mismatch: strafing left while aiming right
    /// would show her striding right while sliding left. <see cref="CharacterAnimator"/> handles
    /// that by playing the walk cycle in reverse when she is backpedalling, which costs nothing
    /// and reads correctly. True sideways strafing would need its own art (~20 clips) and is not
    /// worth it until the game asks for it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAim : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [Tooltip("ABSOLUTE pointer position. Must not be 'Look' — that is bound to <Pointer>/delta " +
                 "in the default asset, which reads (0,0) whenever the mouse is still and pins the " +
                 "reticle to the bottom-left corner.")]
        [SerializeField] private string aimPointActionName = "AimPoint";

        [Tooltip("Gamepad stick aim. This one IS a direction rather than a position.")]
        [SerializeField] private string lookActionName = "Look";

        [Tooltip("Stick deflection needed before the gamepad takes over aiming from the mouse.")]
        [SerializeField, Range(0.1f, 0.9f)] private float stickDeadzone = 0.35f;

        [Header("Facing")]
        [Tooltip("Extra degrees the cursor must cross past a 45° boundary before she turns. " +
                 "Without this she flickers between two facings whenever the cursor sits near a " +
                 "boundary, because every tiny mouse jitter re-snaps her.")]
        [SerializeField, Range(0f, 20f)] private float facingHysteresis = 8f;

        [Tooltip("Do not turn at all while the cursor is this close, in world units. Aim direction " +
                 "is meaningless on top of her and would spin wildly.")]
        [SerializeField] private float minAimDistance = 0.35f;

        [Header("Reticle")]
        [Tooltip("Cursor drawn at the aim point. Optional — aiming works without it.")]
        [SerializeField] private SpriteRenderer reticle;

        [Tooltip("Hide the OS cursor while playing. OFF by default: if the reticle is ever wrong " +
                 "or invisible, hiding the real cursor leaves nothing to aim with at all. Turn on " +
                 "only once the reticle is confirmed tracking correctly.")]
        [SerializeField] private bool hideHardwareCursor;

        [Tooltip("Master switch. Off = facing follows movement like before, so mouse aim can be " +
                 "compared against the old behaviour without unwiring anything.")]
        [SerializeField] private bool useMouseAim = true;

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private AttackStateMachine attacks;

        [Tooltip("Facing is frozen while this is dashing — the dash locks its travel direction at " +
                 "launch, so the cursor must not keep turning her mid-dash.")]
        [SerializeField] private DigDash dash;

        [Tooltip("Freeze facing for the whole swing. A committed attack should land where it was " +
                 "aimed — letting the cursor steer her mid-swing means the animation, the lunge " +
                 "and the hitbox all pull apart from each other.")]
        [SerializeField] private bool lockFacingDuringAttack = true;

        private InputAction _aimPoint;
        private InputAction _look;
        private UnityEngine.Camera _camera;
        private CameraRig _rig;
        private Vector2 _aim = Vector2.right;

        /// <summary>Unit vector from the character toward the cursor.</summary>
        public Vector2 AimDirection { get { return _aim; } }

        /// <summary>World position the cursor is over, on the character's plane.</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>The eight-way facing the cursor resolves to, after hysteresis.</summary>
        public Facing AimFacing { get { return _facing; } }

        private Facing _facing = Facing.Down;
        private bool _hasFacing;

        /// <summary>
        /// Snaps the aim vector to a facing, but only once it has clearly left the current one.
        ///
        /// A plain 45°-octant snap flickers: with the cursor sitting near a boundary, sub-pixel
        /// mouse movement flips her back and forth every frame. Requiring the angle to exceed
        /// half an octant PLUS a margin before committing removes that entirely.
        /// </summary>
        private void UpdateFacing(Vector2 aim)
        {
            Facing candidate = CharacterAnimator.FromDirection(aim);

            if (!_hasFacing)
            {
                _facing = candidate;
                _hasFacing = true;
                return;
            }

            if (candidate == _facing) return;

            float current = Vector2.SignedAngle(_facing.ToVector(), aim);
            if (Mathf.Abs(current) > 22.5f + facingHysteresis) _facing = candidate;
        }

        private void Awake()
        {
            characterAnimator = RigRefs.Find(this, characterAnimator);
            attacks = RigRefs.Find(this, attacks);
            dash = RigRefs.Find(this, dash);
            _camera = UnityEngine.Camera.main;

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerAim)}: no input asset assigned; aiming will not work.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            _aimPoint = map != null ? map.FindAction(aimPointActionName, false) : null;
            _look = map != null ? map.FindAction(lookActionName, false) : null;

            if (_aimPoint == null)
            {
                Debug.LogError($"{nameof(PlayerAim)}: action '{actionMapName}/{aimPointActionName}' not found; mouse aiming is disabled.", this);
            }
        }

        private void OnEnable()
        {
            if (_aimPoint != null) _aimPoint.Enable();
            if (_look != null) _look.Enable();
            if (hideHardwareCursor && reticle != null) Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (_aimPoint != null) _aimPoint.Disable();
            if (_look != null) _look.Disable();
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!useMouseAim)
            {
                // Hand direction back to movement and hide the reticle, so the toggle is a true
                // revert rather than a half-state.
                if (characterAnimator != null) characterAnimator.ClearFacingOverride();
                if (reticle != null && reticle.enabled) reticle.enabled = false;
                return;
            }

            if (reticle != null && !reticle.enabled) reticle.enabled = true;

            if (_camera == null) _camera = UnityEngine.Camera.main;
            if (_camera == null) return;

            // Gamepad wins while the stick is pushed, otherwise the mouse drives aim.
            //
            // The device check is essential, not defensive: 'Look' is bound to BOTH
            // <Gamepad>/rightStick AND <Pointer>/delta. Mouse delta is measured in pixels, so any
            // mouse movement reads far above the deadzone — without this test, moving the mouse
            // looked like a slammed stick and aim snapped to the direction the mouse had just
            // travelled instead of where the cursor actually is.
            bool fromGamepad = _look != null
                && _look.activeControl != null
                && _look.activeControl.device is UnityEngine.InputSystem.Gamepad;

            Vector2 stick = fromGamepad ? _look.ReadValue<Vector2>() : Vector2.zero;
            bool usingStick = stick.sqrMagnitude >= stickDeadzone * stickDeadzone;

            if (usingStick)
            {
                _aim = stick.normalized;
                AimPoint = transform.position + (Vector3)(_aim * 2f);
            }
            else if (_aimPoint != null)
            {
                Vector2 screen = _aimPoint.ReadValue<Vector2>();

                // A zero read means no pointer has reported yet — using it would unproject to the
                // screen corner and slam the reticle into the bottom-left.
                if (screen.sqrMagnitude > 0.0001f)
                {
                    // Depth from the camera to her plane, so the point lands where she stands
                    // rather than on the near clip plane.
                    float depth = Mathf.Abs(_camera.transform.position.z - transform.position.z);
                    Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

                    // Undo camera shake, otherwise the reticle wobbles on every hit despite the
                    // pointer being perfectly still.
                    if (_rig == null) _rig = _camera.GetComponent<CameraRig>();
                    if (_rig != null) world -= _rig.ShakeOffset;

                    world.z = transform.position.z;
                    AimPoint = world;

                    Vector2 delta = (Vector2)(world - transform.position);

                    // Ignore the cursor when it is basically on top of her — the direction is
                    // meaningless there and she would spin.
                    if (delta.sqrMagnitude > minAimDistance * minAimDistance) _aim = delta.normalized;
                }
            }

            // The reticle keeps tracking the cursor during an attack — you can see where the next
            // swing will go even while this one is committed.
            if (reticle != null) reticle.transform.position = AimPoint;

            // Facing is frozen for the duration of a swing. Everything about an attack is decided
            // at its first frame — the clip's direction, the lunge vector, and later the hitbox —
            // so letting the cursor keep turning her mid-swing desynchronises all three.
            //
            // IsCommitted, not IsAttacking: a Heavy Strike charge decides none of those until it
            // is released, and aiming the charged swing while holding it is the entire reason to
            // hold. The freeze starts at the release.
            if (lockFacingDuringAttack && attacks != null && attacks.IsCommitted) return;

            // Frozen during a dash for the same reason, and it matters more here: the dash locks
            // its travel direction at launch, so a cursor that kept turning her would draw her
            // facing one way while she slides another.
            if (dash != null && dash.IsDashing) return;

            UpdateFacing(_aim);

            // Facing follows aim, not the movement vector. PlayerController still feeds motion for
            // the Idle/Move state; this overrides only the direction.
            if (characterAnimator != null) characterAnimator.SetFacing(_facing);
        }
    }
}
