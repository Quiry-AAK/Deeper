using System;
using Deeper.Animation;
using Deeper.CameraControl;
using Deeper.Character;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Combat
{
    /// <summary>The phases every weapon action runs through (CORE_SYSTEMS §1).</summary>
    public enum AttackPhase
    {
        Idle = 0,
        Windup = 1,
        Active = 2,
        Recovery = 3,
    }

    /// <summary>
    /// Drives IDLE → WINDUP → ACTIVE → RECOVERY → IDLE for whichever weapon the run is carrying,
    /// including multi-hit chains. Per-action timings are data on <see cref="WeaponDefinition"/>
    /// (BALANCE.md §2), so this class never branches on weapon type — which is what lets the same
    /// machine drive all three weapons once the Bow and Greatsword exist.
    ///
    /// **Chains** follow CORE_SYSTEMS §3: each hit re-enters Windup→Active→Recovery, and the chain
    /// breaks unless the player presses the same button again during the window. Chain hits replay
    /// the base animation rather than needing unique art (ART_DIRECTION §46), which is what keeps
    /// the upgrade pool from silently exploding the animation budget.
    ///
    /// The feel layer — gauge fill, combo stacks, hitstop, camera shake — hangs off
    /// <see cref="AttackHitbox"/>'s contact reports, not off the swing. It used to fire on the Active
    /// phase opening, because nothing existed to hit; that made every whiff stutter the screen and
    /// fill the gauge.
    ///
    /// <see cref="CanCancel"/> still exposes the Dash-Attack Cancel window ahead of the Dig-Dash
    /// existing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackStateMachine : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [Tooltip("Basic Attack — LClick per GDD §Controls.")]
        [SerializeField] private string basicActionName = "Attack";
        [Tooltip("Heavy Strike — RClick per GDD §Controls.")]
        [SerializeField] private string heavyActionName = "HeavyStrike";
        [Tooltip("Ultimate — R per GDD §Controls. Gauge-gated.")]
        [SerializeField] private string ultimateActionName = "Ultimate";

        [Header("Feel — lunge")]
        [Tooltip("World units travelled per hit. The character should step INTO the cut; standing " +
                 "still is what makes a fast weapon feel weightless.")]
        [SerializeField] private float basicLunge = 0.75f;
        [SerializeField] private float heavyLunge = 1.15f;
        [SerializeField] private float ultimateLunge = 0.9f;

        [Tooltip("Fraction of the action spent moving. Front-loaded so the lunge snaps during " +
                 "Windup/Active and has settled before Recovery ends.")]
        [SerializeField, Range(0.1f, 1f)] private float lungeFraction = 0.45f;

        [Header("Chains")]
        [Tooltip("Hits in the Basic Attack chain. OWNER-DIRECTED NEW DESIGN — no design doc " +
                 "describes a Basic chain; the Katana's documented trait is the Combo Counter.\n\n" +
                 "Two, not three: the chain loops, so hit 1 following hit 2 already reads as a " +
                 "third distinct cut. A third clip was judged unnecessary in play.")]
        [SerializeField] private int basicChainLength = 2;

        [Tooltip("Hits in the Heavy Strike chain. Base is 1; Twin Cut raises it to 2 and Triple " +
                 "Cut to 3 (CONTENT_DESIGN §2a). Upgrades set this at runtime.")]
        [SerializeField] private int heavyChainLength = 1;

        [Tooltip("Seconds into Recovery during which pressing again continues the chain.")]
        [SerializeField] private float chainWindow = 0.25f;

        [Header("Animation")]
        [Tooltip("Pin the strike frame to the Active window instead of spreading frames evenly " +
                 "across the action. Off = the old even spread, for comparison. Even spreading " +
                 "put the Katana's Heavy damage window on the frame where the sword is still " +
                 "raised overhead, so the hit did not read as connected to the swing.")]
        [SerializeField] private bool alignFramesToPhases = true;

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private RunLoadout loadout;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("Reports what each swing connected with. Everything below — gauge, combo, hitstop, " +
                 "shake — is driven by its contact reports rather than by the swing itself.")]
        [SerializeField] private AttackHitbox hitbox;

        [SerializeField] private UltimateGauge gauge;
        [SerializeField] private ComboCounter combo;
        [SerializeField] private HitStop hitStop;
        [SerializeField] private UltimateBuff ultimateBuff;
        [SerializeField] private PlayerStats stats;

        [Tooltip("The rig drawing the character. Read for real clip lengths and strike frames — " +
                 "guessing them puts the damage window on the wrong frame of the swing.")]
        [SerializeField] private CharacterLayerView characterView;

        [Tooltip("Hitstop per action. Heavier actions freeze longer — that difference is most of " +
                 "what makes a Heavy read as heavier than a Basic.")]
        [SerializeField] private float basicHitStop = 0.045f;
        [SerializeField] private float heavyHitStop = 0.085f;
        [SerializeField] private float ultimateHitStop = 0.11f;

        [Tooltip("Camera shake per action, in world units. Kept small — shake should punctuate a " +
                 "hit, not obscure the fight.")]
        [SerializeField] private float basicShake = 0.045f;
        [SerializeField] private float heavyShake = 0.11f;
        [SerializeField] private float ultimateShake = 0.16f;

        private CameraRig _cameraRig;

        private InputAction _basic;
        private InputAction _heavy;
        private InputAction _ultimate;

        private AttackAction _action;
        private AttackTiming _timing;
        private float _elapsed;
        private int _chainIndex;
        private bool _chainQueued;
        private bool _activeOpened;
        private bool _connectedThisHit;
        private Vector2 _lungeDirection;

        /// <summary>
        /// Velocity the player should be moving at right now because of the attack, in world
        /// units/sec. Zero when not attacking. <c>PlayerController</c> applies this instead of
        /// input velocity, so the attack owns movement without this class touching the rigidbody.
        /// </summary>
        public Vector2 LungeVelocity
        {
            get
            {
                if (Phase == AttackPhase.Idle) return Vector2.zero;

                float window = _timing.Total * lungeFraction;
                if (window <= 0f || _elapsed >= window) return Vector2.zero;

                float distance = _action == AttackAction.Basic ? basicLunge
                    : _action == AttackAction.Heavy ? heavyLunge : ultimateLunge;

                // Ease-out: fast off the mark, decaying to nothing. A flat velocity reads as a
                // slide; the decay is what makes it feel like a step.
                float t = _elapsed / window;
                float speed = 2f * distance / window * (1f - t);
                return _lungeDirection * speed;
            }
        }

        /// <summary>Raised when a hit becomes live. The damage pipeline attaches here.</summary>
        public event Action<AttackAction, AttackTiming> ActivePhaseOpened;

        /// <summary>Raised when a whole action (including its chain) finishes or is cancelled.</summary>
        public event Action Finished;

        public AttackPhase Phase { get; private set; }

        public bool IsAttacking { get { return Phase != AttackPhase.Idle; } }

        /// <summary>Zero-based index of the current hit within its chain.</summary>
        public int ChainIndex { get { return _chainIndex; } }

        /// <summary>Dash-Attack Cancel is legal for the whole Recovery phase (BALANCE §2).</summary>
        public bool CanCancel { get { return Phase == AttackPhase.Recovery; } }

        /// <summary>Upgrades call this — Twin Cut sets 2, Triple Cut sets 3.</summary>
        public void SetHeavyChainLength(int hits)
        {
            heavyChainLength = Mathf.Max(1, hits);
        }

        private int ChainLengthFor(AttackAction action)
        {
            if (action == AttackAction.Basic) return Mathf.Max(1, basicChainLength);
            if (action == AttackAction.Heavy) return Mathf.Max(1, heavyChainLength);
            return 1;
        }

        private InputAction ActionFor(AttackAction action)
        {
            if (action == AttackAction.Basic) return _basic;
            if (action == AttackAction.Heavy) return _heavy;
            return _ultimate;
        }

        private void Awake()
        {
            loadout = RigRefs.Find(this, loadout);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            hitbox = RigRefs.Find(this, hitbox);
            gauge = RigRefs.Find(this, gauge);
            combo = RigRefs.Find(this, combo);
            hitStop = RigRefs.Find(this, hitStop);
            ultimateBuff = RigRefs.Find(this, ultimateBuff);
            stats = RigRefs.Find(this, stats);
            characterView = RigRefs.Find(this, characterView);

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(AttackStateMachine)}: no input asset assigned; attacks will not fire.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            if (map == null)
            {
                Debug.LogError($"{nameof(AttackStateMachine)}: action map '{actionMapName}' not found in {inputActions.name}.", this);
                return;
            }

            _basic = map.FindAction(basicActionName, false);
            _heavy = map.FindAction(heavyActionName, false);
            _ultimate = map.FindAction(ultimateActionName, false);

            if (_basic == null)
            {
                Debug.LogError($"{nameof(AttackStateMachine)}: action '{actionMapName}/{basicActionName}' not found; Basic Attack is unbound.", this);
            }
        }

        private void OnEnable()
        {
            if (_basic != null) _basic.Enable();
            if (_heavy != null) _heavy.Enable();
            if (_ultimate != null) _ultimate.Enable();

            if (hitbox != null)
            {
                hitbox.Landed += HandleLanded;
                hitbox.Missed += HandleMissed;
            }
        }

        private void OnDisable()
        {
            if (_basic != null) _basic.Disable();
            if (_heavy != null) _heavy.Disable();
            if (_ultimate != null) _ultimate.Disable();

            if (hitbox != null)
            {
                hitbox.Landed -= HandleLanded;
                hitbox.Missed -= HandleMissed;
            }

            Stop();
        }

        private void Update()
        {
            if (Phase == AttackPhase.Idle)
            {
                if (Pressed(_ultimate)) Begin(AttackAction.Ultimate);
                else if (Pressed(_heavy)) Begin(AttackAction.Heavy);
                else if (Pressed(_basic)) Begin(AttackAction.Basic);
                return;
            }

            // A press anywhere in the action is remembered, so a chain input during Windup or
            // Active is not swallowed — buffering an input is far kinder than demanding the
            // player hit an exact window.
            if (Pressed(ActionFor(_action))) _chainQueued = true;

            _elapsed += Time.deltaTime;
            Phase = PhaseAt(_elapsed);

            // Fired on CROSSING the windup rather than on sampling into Active. Sampling once per
            // frame meant a frame longer than windup+active — 0.18s on the Katana's Basic, which a
            // hitch or a stalled main thread can exceed — skipped the Active phase entirely, so the
            // hit never became live. That was harmless while nothing could be hit; now it is a free
            // whiff and a lost damage window.
            if (!_activeOpened && _elapsed >= _timing.Windup)
            {
                _activeOpened = true;
                OpenActivePhase();
            }

            if (Phase == AttackPhase.Recovery && _chainQueued && CanChain())
            {
                float intoRecovery = _elapsed - (_timing.Windup + _timing.Active);
                if (intoRecovery <= chainWindow)
                {
                    AdvanceChain();
                    return;
                }
            }

            if (_elapsed >= _timing.Total) Stop();
        }

        private static bool Pressed(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        private bool CanChain()
        {
            return _chainIndex + 1 < ChainLengthFor(_action);
        }

        private AttackPhase PhaseAt(float t)
        {
            if (t < _timing.Windup) return AttackPhase.Windup;
            if (t < _timing.Windup + _timing.Active) return AttackPhase.Active;
            return AttackPhase.Recovery;
        }

        private void OpenActivePhase()
        {
            ActivePhaseOpened?.Invoke(_action, _timing);
        }

        /// <summary>
        /// A swing connected. Only the first target of a hit feeds these: an attack that catches
        /// three enemies is still one landed hit, or a crowd would fill the gauge and stack the combo
        /// several times off one press — and chain a hitstop freeze per body.
        /// </summary>
        private void HandleLanded(AttackAction action, Damageable target, float amount)
        {
            if (_connectedThisHit) return;
            _connectedThisHit = true;

            if (gauge != null) gauge.OnHitLanded(action);
            if (combo != null && action != AttackAction.Ultimate) combo.OnHitLanded();

            if (hitStop != null)
            {
                float freeze = action == AttackAction.Basic ? basicHitStop
                    : action == AttackAction.Heavy ? heavyHitStop : ultimateHitStop;
                hitStop.Freeze(freeze);
            }

            if (_cameraRig == null) _cameraRig = FindFirstObjectByType<CameraRig>();
            if (_cameraRig != null)
            {
                float shake = action == AttackAction.Basic ? basicShake
                    : action == AttackAction.Heavy ? heavyShake : ultimateShake;
                _cameraRig.Shake(shake);
            }
        }

        /// <summary>
        /// The hit's whole Active window closed without touching anything. BALANCE §3: the Combo
        /// Counter resets instantly on a miss. Nothing else happens — no gauge, no hitstop, no shake,
        /// which is the entire point of routing the feel layer through contact.
        /// </summary>
        private void HandleMissed(AttackAction action)
        {
            if (combo != null && action != AttackAction.Ultimate) combo.OnMissed();
        }

        /// <summary>Starts an action. Ignored while another is running — actions do not interrupt.</summary>
        public bool Begin(AttackAction action)
        {
            if (Phase != AttackPhase.Idle) return false;

            WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;
            if (weapon == null) return false;

            // CORE_SYSTEMS §4: the Ultimate is purely resource-gated, never a cooldown.
            if (action == AttackAction.Ultimate)
            {
                if (gauge == null || !gauge.TrySpend()) return false;
                if (combo != null) combo.Consume();

                // An Ultimate is either an attack or a buff, and which one is WEAPON DATA — the
                // Katana's is a buff (owner-directed) while CORE_SYSTEMS §4 assumes all of them
                // deal damage. Branching on the weapon here is what lets IWeapon.Ultimate() cover
                // both shapes in Milestone 2 instead of hardcoding one of them.
                if (weapon.UltimateShape == UltimateKind.Buff && ultimateBuff != null)
                {
                    ultimateBuff.Activate(weapon.UltimateBuff);
                }
            }

            _action = action;
            _timing = Scaled(weapon.GetAttackTiming(action));
            if (_timing.Total <= 0f) return false;

            _chainIndex = 0;
            _chainQueued = false;
            StartHit();
            return true;
        }

        /// <summary>
        /// Applies the player's attack speed to a weapon's authored timing.
        ///
        /// All three phases scale together, never just Recovery. The frame that draws the strike
        /// is pinned to the Active window, so speeding one phase without the others would slide
        /// the blade out of the damage window — the exact desync that made hits feel disconnected
        /// from the swing before frames were phase-aligned.
        /// </summary>
        private AttackTiming Scaled(AttackTiming timing)
        {
            float speed = stats != null ? stats.AttackSpeed : 1f;
            if (speed <= 0.01f || Mathf.Approximately(speed, 1f)) return timing;

            float k = 1f / speed;
            timing.Windup *= k;
            timing.Active *= k;
            timing.Recovery *= k;
            return timing;
        }

        private void AdvanceChain()
        {
            _chainIndex++;
            _chainQueued = false;
            StartHit();
        }

        private void StartHit()
        {
            _elapsed = 0f;
            Phase = AttackPhase.Windup;
            _activeOpened = false;
            _connectedThisHit = false;

            // Direction is locked per hit, not tracked live — a lunge that curves mid-swing feels
            // like steering a car, not committing to a cut. Each chain hit re-locks, so the player
            // can still redirect between hits.
            if (characterAnimator != null) _lungeDirection = characterAnimator.Facing.ToVector();

            if (characterAnimator == null) return;

            // Chain index picks the clip: the Basic chain is three distinct animations.
            CharacterState state = AttackTiming.StateFor(_action, _chainIndex);

            // Frame count MUST come from the set that actually draws her. The weapon's set has no
            // attack clips (the sword is baked into the body art), and a hardcoded guess that
            // overshoots makes the clip wrap and restart partway through the action — it reads as
            // the attack trying to fire a second time and being cut off.
            int frames = 0;
            CharacterState artState = state;
            if (characterView != null && characterView.BodyAnimation != null)
            {
                frames = characterView.BodyAnimation.FrameCount(state, characterAnimator.Facing);

                // A chain state with no art falls back to the base Basic clip, so match its length.
                if (frames <= 0 && (state == CharacterState.BasicAttack2 || state == CharacterState.BasicAttack3))
                {
                    artState = CharacterState.BasicAttack;
                    frames = characterView.BodyAnimation.FrameCount(artState, characterAnimator.Facing);
                }
            }

            if (frames <= 0)
            {
                Debug.LogWarning($"{nameof(AttackStateMachine)}: no frames for {state}; the attack will not animate.", this);
                return;
            }

            if (!alignFramesToPhases)
            {
                characterAnimator.PlayAction(state, _timing.Total, frames);
                return;
            }

            // Which frame lands the hit is a property of the art, not of the action, because the
            // five directions were drawn as five different swings — Side cuts on frame 2 while
            // UpDiagonal is still winding up and does not cut until frame 3. Reading it per
            // facing is what stops hitstop and camera shake firing before the blade has moved.
            int strike = characterView != null && characterView.BodyAnimation != null
                ? characterView.BodyAnimation.StrikeFrame(artState, characterAnimator.Facing)
                : frames / 2;

            characterAnimator.PlayAction(
                state, _timing.Windup, _timing.Active, _timing.Recovery, frames, strike);
        }

        /// <summary>Ends the current action. The Dash-Attack Cancel will call this during Recovery.</summary>
        public void Stop()
        {
            if (Phase == AttackPhase.Idle) return;

            Phase = AttackPhase.Idle;
            _elapsed = 0f;
            _chainIndex = 0;
            _chainQueued = false;
            _activeOpened = false;
            _connectedThisHit = false;
            if (characterAnimator != null) characterAnimator.CancelAction();
            Finished?.Invoke();
        }
    }
}
