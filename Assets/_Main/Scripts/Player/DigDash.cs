using Deeper.Animation;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Player
{
    /// <summary>
    /// The Dig-Dash — a short dash with invulnerability frames (GDD §Player, CORE_SYSTEMS §2,
    /// BALANCE §1), travelling the way the movement keys point (see <see cref="ResolveDirection"/>,
    /// owner-directed) rather than the way the cursor does.
    ///
    /// This is the player's **entire** defensive option. BALANCE's preamble states that mitigation
    /// is exclusively Dig-Dash i-frames, Hyper Armor, Iron Skin and Second Skin, and of those only
    /// this one is in MVP scope — so until this exists there is no dodge button in a game whose
    /// rooms lock you in with six enemies.
    ///
    /// It also carries the **Dash-Attack Cancel** (CORE_SYSTEMS §2): during Recovery only, dashing
    /// cancels the remaining lockout. Not during Windup or Active, which is what keeps the
    /// Greatsword's whiff-punish window real.
    ///
    /// **Movement is exposed, not applied.** Like <c>AttackStateMachine.LungeVelocity</c>, this
    /// publishes <see cref="DashVelocity"/> and <see cref="PlayerController"/> writes it to the
    /// rigidbody. Doing it here would not work: the controller writes <c>linearVelocity</c> outright
    /// every physics tick, so an <c>AddForce</c> or a transform write is erased on the same step.
    /// </summary>
    /// <remarks>
    /// Runs before <c>AttackStateMachine</c> on purpose. That class buffers a chain continuation
    /// from any Attack press during Recovery, and if it polled first a dash pressed in the same
    /// window would find the attack already advanced into Windup — where the cancel is illegal —
    /// and be silently swallowed by the buffered chain. Dash-cancelling has to beat the buffer.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class DigDash : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";

        [Tooltip("GDD §Controls assigns LShift to Dig-Dash. This is the renamed 'Sprint' action " +
                 "from Unity's input template, which already carried that binding and which no " +
                 "code ever read.")]
        [SerializeField] private string dashActionName = "Dash";

        [Tooltip("Read here rather than taken from PlayerController.MoveInput, and that is not " +
                 "duplication for its own sake: this component runs at execution order -10, so it " +
                 "would see the controller's PREVIOUS frame of input and a direction tapped on the " +
                 "same frame as the dash key would be missed.")]
        [SerializeField] private string moveActionName = "Move";

        [Header("Feel")]
        [Tooltip("Seconds the dash takes to cover its full distance. **Invented — BALANCE §1 gives " +
                 "a distance and a cooldown but no duration.** 0.18s puts the average speed above " +
                 "3x her walk, which is what makes it read as a dash rather than a sprint.")]
        [SerializeField] private float duration = 0.18f;

        [Tooltip("Seconds of invulnerability, from the moment the dash starts. BALANCE §1 gives " +
                 "0.25s — deliberately longer than the dash itself, so there is a little grace on " +
                 "landing rather than a frame-perfect window.")]
        [SerializeField] private float iFrameDuration = 0.25f;

        [Tooltip("Seconds AFTER the dash lands during which a Basic Attack press comes out as the " +
                 "unique Dash Attack instead (owner-directed). The window also covers the dash " +
                 "itself, so an attack pressed mid-dash is buffered into it rather than swallowed " +
                 "— demanding the player wait for the dash to finish before pressing would make " +
                 "the move a reaction test rather than an approach.")]
        [SerializeField] private float dashAttackWindow = 0.35f;

        [Header("Refs — found anywhere on the player rig when empty")]
        [Tooltip("Distance and cooldown are read from here every dash, never cached, so the " +
                 "Long Dash and Quickstep upgrades and the Hub's Dash Cooldown stat all apply " +
                 "without this class changing.")]
        [SerializeField] private PlayerStats stats;

        [SerializeField] private Damageable health;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("Read for the Dash-Attack Cancel. Dashing during Recovery calls Stop() on it.")]
        [SerializeField] private AttackStateMachine attacks;

        [Tooltip("Optional. The afterimage trail ART_DIRECTION §6 lists as must-have MVP VFX.")]
        [SerializeField] private DashTrail trail;

        private InputAction _dashAction;
        private InputAction _moveAction;

        private Vector2 _direction;
        private float _remaining;
        private float _distance;
        private float _readyAt;
        private float _dashAttackUntil;

        /// <summary>True while the dash is carrying her. <see cref="PlayerController"/> and
        /// <see cref="PlayerAim"/> both check this to stop fighting the pose and the facing.</summary>
        public bool IsDashing { get { return _remaining > 0f; } }

        public bool IsReady { get { return Time.time >= _readyAt; } }

        /// <summary>
        /// True while a Basic Attack press should come out as <see cref="AttackAction.DashAttack"/>.
        /// Spans the dash itself plus <see cref="dashAttackWindow"/> after it.
        /// </summary>
        public bool InDashAttackWindow { get { return Time.time <= _dashAttackUntil; } }

        /// <summary>
        /// Closes the window, called by the state machine when it actually starts a Dash Attack.
        ///
        /// Without this the window outlives the move it triggers: a Dash Attack runs ~0.36s, well
        /// inside a window that opened up to 0.53s ago, so the follow-up press would come out as a
        /// second Dash Attack instead of continuing the normal Basic chain.
        /// </summary>
        public void ConsumeDashAttackWindow()
        {
            _dashAttackUntil = 0f;
        }

        /// <summary>0 when just used, 1 when ready again. For a future HUD element.</summary>
        public float CooldownNormalized
        {
            get
            {
                float cooldown = CurrentCooldown;
                if (cooldown <= 0f) return 1f;

                return Mathf.Clamp01(1f - (_readyAt - Time.time) / cooldown);
            }
        }

        /// <summary>
        /// Velocity the dash wants right now, for <see cref="PlayerController"/> to apply.
        ///
        /// Ease-out on the same curve as the attack lunge and the Brute's knockback: peak at the
        /// start, settling to zero. The integral over the window is the authored distance, so
        /// BALANCE §1's 3.0 units is what actually gets travelled.
        /// </summary>
        public Vector2 DashVelocity
        {
            get
            {
                if (_remaining <= 0f || duration <= 0f) return Vector2.zero;

                // Sampled half a physics step back, because FixedUpdate has already advanced
                // _remaining to the END of the step the controller is about to integrate over. On
                // a linear ramp the midpoint is the exact average across the step, so the sum lands
                // on the authored distance instead of ~11% short of it.
                float remaining = Mathf.Min(duration, _remaining + Time.fixedDeltaTime * 0.5f);

                float t = 1f - Mathf.Clamp01(remaining / duration);
                return _direction * (2f * _distance / duration * (1f - t));
            }
        }

        private float CurrentCooldown { get { return stats != null ? stats.DashCooldown : 0f; } }

        private void Awake()
        {
            stats = RigRefs.Find(this, stats);
            health = RigRefs.Find(this, health);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            attacks = RigRefs.Find(this, attacks);

            // GetComponentInChildren, not RigRefs, because this one is optional: RigRefs searches
            // from transform.root, and an empty optional field is exactly how the Cave Crawler once
            // adopted a sibling's contact damage. Scoped to this rig it cannot reach anything else.
            if (trail == null) trail = GetComponentInChildren<DashTrail>(true);

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(DigDash)}: no input asset assigned; the dash key will do nothing.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            _dashAction = map != null ? map.FindAction(dashActionName, false) : null;
            _moveAction = map != null ? map.FindAction(moveActionName, false) : null;

            if (_moveAction == null)
            {
                Debug.LogWarning($"{nameof(DigDash)}: action '{actionMapName}/{moveActionName}' not " +
                                 $"found; the dash will fall back to dashing where she is facing.", this);
            }

            if (_dashAction == null)
            {
                Debug.LogError($"{nameof(DigDash)}: action '{actionMapName}/{dashActionName}' not " +
                               $"found in {inputActions.name}.", this);
            }
        }

        private void OnEnable()
        {
            if (_dashAction != null) _dashAction.Enable();
            if (_moveAction != null) _moveAction.Enable();

            // Absolute-time cooldown, so a disable/enable cycle would otherwise leave her locked
            // out for however long the object was off.
            _readyAt = 0f;
            _remaining = 0f;
            _dashAttackUntil = 0f;
        }

        private void OnDisable()
        {
            if (_dashAction != null) _dashAction.Disable();
            if (_moveAction != null) _moveAction.Disable();

            _remaining = 0f;
            _dashAttackUntil = 0f;
        }

        private void Update()
        {
            // Input only. WasPressedThisFrame is a per-render-frame edge and would be missed
            // entirely on a physics step, so this half cannot move to FixedUpdate.
            if (_remaining > 0f) return;

            if (_dashAction != null && _dashAction.WasPressedThisFrame()) TryDash();
        }

        /// <summary>
        /// Advances the dash on the physics clock.
        ///
        /// **This tick must live here and not in <c>Update</c>, and it is not a style choice.**
        /// <see cref="PlayerController"/> integrates <see cref="DashVelocity"/> on the physics step.
        /// While the timer ran on the render frame, every step inside a physics catch-up burst read
        /// the same <c>_remaining</c> and therefore ran at full dash speed — one stalled frame
        /// carried her 13.2 units for an authored 3.0, measured, and the overshoot scaled with
        /// whatever the frame rate happened to be. The timer and the thing integrating it have to
        /// share a clock.
        /// </summary>
        private void FixedUpdate()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.fixedDeltaTime;
            if (_remaining < 0f) _remaining = 0f;
        }

        /// <summary>
        /// Dashes if she can, and reports whether she did.
        ///
        /// Public and context-menu'd because simulated key presses never reach play mode
        /// (Engineering/01-VERIFICATION.md §2) — every automated check drives the dash through here.
        /// </summary>
        [ContextMenu("Dash")]
        public bool TryDash()
        {
            if (IsDashing || !IsReady) return false;
            if (health != null && !health.IsAlive) return false;

            // The Dash-Attack Cancel. CanCancel is true for the whole Recovery phase and false
            // during Windup and Active (BALANCE §2), so that half is the entire documented rule —
            // Stop() already resets phase, chain state and the animator's action.
            //
            // Charging is added to it, and this matters MORE since a charge started rooting her
            // (owner, 2026-08-16). A charge is not a commitment: she keeps her aim through it, and
            // the dash is now the only way to leave the spot she is holding on. Being unable to
            // dash out of one would turn holding the button into a trap rather than a choice.
            if (attacks != null)
            {
                if (attacks.CanCancel || attacks.Phase == AttackPhase.Charging) attacks.Stop();
                else if (attacks.IsAttacking) return false;   // committed: Windup or Active
            }

            _direction = ResolveDirection();

            // Read fresh every dash rather than cached, so upgrades and Hub stats land untouched.
            _distance = stats != null ? stats.DashDistance : 0f;
            _remaining = duration;
            _readyAt = Time.time + CurrentCooldown;

            _dashAttackUntil = Time.time + duration + dashAttackWindow;

            if (health != null) health.GrantInvulnerability(iFrameDuration);

            // A fixed-duration one-shot, like an attack — not the free-running 8fps counter, which
            // would show whatever frame it happened to be on when the dash started.
            if (characterAnimator != null)
            {
                characterAnimator.PlayAction(CharacterState.Dash, duration, DashFrames);
            }

            if (trail != null) trail.Play(duration);

            return true;
        }

        /// <summary>
        /// Which way the dash travels: **the movement keys, not the cursor** (owner-directed).
        ///
        /// GDD §Player says "short dash in facing direction", and that was written before facing
        /// belonged to the mouse. With mouse aim, "facing" is wherever the cursor is, so holding S
        /// to back away from something and pressing dash threw her *into* it. Reading the movement
        /// keys instead makes the dash go where the player is already asking to go, which is what
        /// the original line meant when movement still owned facing. Recorded in the change brief.
        ///
        /// Standing still falls back to facing, so a dash with no direction held is still an
        /// advance toward the cursor rather than nothing.
        /// </summary>
        private Vector2 ResolveDirection()
        {
            if (_moveAction != null)
            {
                Vector2 move = _moveAction.ReadValue<Vector2>();

                // Normalized, not raw: an analog stick at half deflection must still dash the full
                // authored distance, and DashVelocity multiplies this by the distance directly.
                if (move.sqrMagnitude > 0.0001f) return move.normalized;
            }

            return characterAnimator != null ? characterAnimator.Facing.ToVector() : Vector2.down;
        }

        /// <summary>ART_DIRECTION §3's Dig-Dash budget. Not serialized: it is the art contract, and
        /// a mismatch here would desync the clip from its own sheet.</summary>
        private const int DashFrames = 4;
    }
}
