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
    /// <summary>
    /// The phases every weapon action runs through (CORE_SYSTEMS §1), plus
    /// <see cref="Charging"/>. Values are stable — do not renumber.
    /// </summary>
    public enum AttackPhase
    {
        Idle = 0,
        Windup = 1,
        Active = 2,
        Recovery = 3,

        /// <summary>
        /// Holding a chargeable Heavy Strike (owner-directed). Sits BEFORE Windup, not inside it:
        /// CORE_SYSTEMS §5b describes the Bow's Charge Shot as "holding extends the Windup phase",
        /// and this is the same idea — the hold is the anticipation, so the Windup that follows a
        /// release shortens as the charge fills instead of being paid for twice.
        ///
        /// It is the one attack phase the player can still aim and dash out of
        /// (<see cref="IsCommitted"/>). She does not walk through it — a charge roots her
        /// (owner, 2026-08-16) — so aiming and the Dig-Dash are the whole of what a hold leaves
        /// her.
        /// </summary>
        Charging = 4,
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
    /// <see cref="CanCancel"/> exposes the Dash-Attack Cancel window.
    ///
    /// Two owner-directed moves sit on top of that shape, both recorded in the change brief:
    /// **Dash Attack**, a fourth <see cref="AttackAction"/> that an LClick becomes inside the
    /// Dig-Dash's follow-up window, and the **Heavy Strike charge**, a
    /// <see cref="AttackPhase.Charging"/> hold in front of the Windup that scales the swing's
    /// damage, reach and impact with how long the button was held. Whether a weapon charges at all
    /// is <see cref="ChargeSpec"/> data on the weapon, so this class still never branches on
    /// weapon type.
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

        [Tooltip("Longest of the four. The Dash Attack's whole identity is closing distance — she " +
                 "is already moving when it starts, and a short step here would read as the dash " +
                 "stopping dead before the swing.")]
        [SerializeField] private float dashAttackLunge = 1.4f;

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

        [Header("Charge — Heavy Strike (owner-directed)")]
        [Tooltip("Fraction of her normal walk speed while charging. **0 — she is rooted** " +
                 "(owner, 2026-08-16). This was 0.45 on the reasoning that rooting her inside a " +
                 "locked room makes holding the button a punishment; the owner overruled it, so a " +
                 "charge now costs position and the Dig-Dash is the way out of one. She still aims " +
                 "through it, and she decelerates to a stop rather than snapping.")]
        [SerializeField, Range(0f, 1f)] private float chargeMoveScale;

        [Tooltip("Charge fraction at which the release plays the unique HeavyCharged clip instead " +
                 "of the ordinary HeavyStrike one. Below it, a barely-held Heavy should look " +
                 "exactly like a tapped one, because that is what it is.")]
        [SerializeField, Range(0f, 1f)] private float chargedClipThreshold = 0.6f;

        [Tooltip("Lunge, hitstop and camera shake multiplier at full charge. One number for all " +
                 "three on purpose — they are the same sensation, and letting them drift apart is " +
                 "how a heavy hit starts feeling loud instead of heavy.")]
        [SerializeField] private float chargedImpactScale = 1.6f;

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

        [Tooltip("Read to decide whether a Basic Attack press comes out as the Dash Attack. The " +
                 "reference runs both ways — DigDash reads this class for the Dash-Attack Cancel " +
                 "— which is fine for two serialized fields and keeps both rules where they belong.")]
        [SerializeField] private DigDash dash;

        [Tooltip("The rig drawing the character. Read for real clip lengths and strike frames — " +
                 "guessing them puts the damage window on the wrong frame of the swing.")]
        [SerializeField] private CharacterLayerView characterView;

        [Tooltip("Hitstop per action. Heavier actions freeze longer — that difference is most of " +
                 "what makes a Heavy read as heavier than a Basic.")]
        [SerializeField] private float basicHitStop = 0.045f;
        [SerializeField] private float heavyHitStop = 0.085f;
        [SerializeField] private float ultimateHitStop = 0.11f;
        [SerializeField] private float dashAttackHitStop = 0.06f;

        [Tooltip("Camera shake per action, in world units. Kept small — shake should punctuate a " +
                 "hit, not obscure the fight.")]
        [SerializeField] private float basicShake = 0.045f;
        [SerializeField] private float heavyShake = 0.11f;
        [SerializeField] private float ultimateShake = 0.16f;
        [SerializeField] private float dashAttackShake = 0.07f;

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
        private float _chargeHeld;

        /// <summary>
        /// Velocity the player should be moving at right now because of the attack, in world
        /// units/sec. Zero when not attacking. <c>PlayerController</c> applies this instead of
        /// input velocity, so the attack owns movement without this class touching the rigidbody.
        /// </summary>
        public Vector2 LungeVelocity
        {
            get
            {
                // IsCommitted, not IsAttacking: while Charging _elapsed is still zero, so the
                // ease-out below would report peak lunge speed for the whole hold.
                if (!IsCommitted) return Vector2.zero;

                float window = _timing.Total * lungeFraction;
                if (window <= 0f || _elapsed >= window) return Vector2.zero;

                float distance = LungeFor(_action) * ChargedImpact;

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

        /// <summary>
        /// True once the action is committed — Windup, Active or Recovery — and false while
        /// Charging.
        ///
        /// This is the property movement and aim should read, not <see cref="IsAttacking"/>.
        /// Charging is an attack in progress but not a commitment: the player is still choosing
        /// where it goes, so <c>PlayerAim</c> keeps turning her to the cursor and the Dig-Dash can
        /// still cancel out of it.
        ///
        /// **She no longer walks during a charge** (owner, 2026-08-16) — but that is
        /// <see cref="chargeMoveScale"/> set to 0, not a change here. Folding Charging into this
        /// property would also freeze her aim, make the charge undashable, and hand movement to
        /// <see cref="LungeVelocity"/>, which reports peak lunge speed while <c>_elapsed</c> is
        /// zero. Rooting her is a speed of zero, not a commitment.
        /// </summary>
        public bool IsCommitted
        {
            get { return Phase != AttackPhase.Idle && Phase != AttackPhase.Charging; }
        }

        /// <summary>How full the current Heavy Strike charge is, 0–1. Zero for every other action.</summary>
        public float Charge { get; private set; }

        /// <summary>Multiplier <c>PlayerController</c> applies to her walk speed. Slowed while charging.</summary>
        public float MoveSpeedScale
        {
            get { return Phase == AttackPhase.Charging ? chargeMoveScale : 1f; }
        }

        /// <summary>
        /// How much harder a charged hit lands, 1 when uncharged. Applied to the lunge, the
        /// hitstop and the camera shake so all three grow together.
        /// </summary>
        private float ChargedImpact
        {
            get
            {
                if (_action != AttackAction.Heavy || Charge <= 0f) return 1f;
                return Mathf.Lerp(1f, chargedImpactScale, Charge);
            }
        }

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

        /// <summary>
        /// The button that continues this action's chain. The Dash Attack answers to the Basic
        /// button, not its own — it has no key of its own, and returning the Ultimate's action
        /// here (which the old two-step ternary did) would have let R buffer a chain into it.
        /// </summary>
        private InputAction ActionFor(AttackAction action)
        {
            if (action == AttackAction.Heavy) return _heavy;
            if (action == AttackAction.Ultimate) return _ultimate;
            return _basic;
        }

        private float LungeFor(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basicLunge;
                case AttackAction.Heavy: return heavyLunge;
                case AttackAction.DashAttack: return dashAttackLunge;
                default: return ultimateLunge;
            }
        }

        private float HitStopFor(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basicHitStop;
                case AttackAction.Heavy: return heavyHitStop;
                case AttackAction.DashAttack: return dashAttackHitStop;
                default: return ultimateHitStop;
            }
        }

        private float ShakeFor(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basicShake;
                case AttackAction.Heavy: return heavyShake;
                case AttackAction.DashAttack: return dashAttackShake;
                default: return ultimateShake;
            }
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
            dash = RigRefs.Find(this, dash);

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
                else if (Pressed(_basic)) Begin(BasicOrDashAttack());
                return;
            }

            // Charging runs on its own clock and does not advance the action. Everything below
            // this line assumes _elapsed is walking through Windup/Active/Recovery, which it is
            // not while the button is still down.
            if (Phase == AttackPhase.Charging)
            {
                AdvanceCharge();
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

        private static bool Held(InputAction action)
        {
            return action != null && action.IsPressed();
        }

        /// <summary>
        /// Which move an LClick press becomes. Inside the Dig-Dash's follow-up window it is the
        /// Dash Attack; otherwise the ordinary Basic. The player never presses a different key —
        /// GDD §Controls has no spare one, and asking for a chord would make an approach move into
        /// a dexterity test.
        /// </summary>
        private AttackAction BasicOrDashAttack()
        {
            return dash != null && dash.InDashAttackWindow
                ? AttackAction.DashAttack
                : AttackAction.Basic;
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

            if (hitStop != null) hitStop.Freeze(HitStopFor(action) * ChargedImpact);

            if (_cameraRig == null) _cameraRig = FindFirstObjectByType<CameraRig>();
            if (_cameraRig != null) _cameraRig.Shake(ShakeFor(action) * ChargedImpact);
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
            Charge = 0f;

            // The window is spent on the press, not on the hit. Left open, a Dash Attack's own
            // recovery still sits inside it, so the follow-up press would come out as a second
            // Dash Attack instead of continuing the Basic chain.
            if (action == AttackAction.DashAttack && dash != null) dash.ConsumeDashAttackWindow();

            if (action == AttackAction.Heavy && weapon.HeavyCharge.Enabled && Held(_heavy))
            {
                BeginCharge();
                return true;
            }

            StartHit();
            return true;
        }

        /// <summary>The charge spec of whatever the run is carrying. Disabled when there is no weapon.</summary>
        private ChargeSpec WeaponCharge
        {
            get
            {
                WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;
                return weapon != null ? weapon.HeavyCharge : default(ChargeSpec);
            }
        }

        private void BeginCharge()
        {
            Phase = AttackPhase.Charging;
            _elapsed = 0f;
            _chargeHeld = 0f;
            Charge = 0f;
            _activeOpened = false;
            _connectedThisHit = false;

            if (characterAnimator == null) return;

            int frames = characterView != null && characterView.BodyAnimation != null
                ? characterView.BodyAnimation.FrameCount(CharacterState.HeavyCharge, characterAnimator.Facing)
                : 0;

            // No charge art: hold the first frame of the Heavy clip instead of playing nothing.
            // A charge that draws her standing idle gives the player no reason to believe the
            // button is doing anything.
            if (frames <= 0)
            {
                characterAnimator.PlayLoop(CharacterState.HeavyStrike, ChargeCycle, 1);
                return;
            }

            characterAnimator.PlayLoop(CharacterState.HeavyCharge, ChargeCycle, frames);
        }

        private void AdvanceCharge()
        {
            ChargeSpec spec = WeaponCharge;

            _chargeHeld += Time.deltaTime;
            Charge = spec.MaxHoldTime > 0f ? Mathf.Clamp01(_chargeHeld / spec.MaxHoldTime) : 1f;

            // Held at full rather than auto-fired. The release is the player's timing decision —
            // firing it for them would take the one thing a charge is actually for.
            if (Held(_heavy)) return;

            ReleaseCharge();
        }

        /// <summary>
        /// Fires the charged Heavy. The hold has already served as the anticipation, so the
        /// authored Windup collapses toward <see cref="ChargeSpec.ReleaseWindup"/> as the charge
        /// fills — at zero charge the action is BALANCE §2's Heavy Strike untouched, and at full
        /// charge the blade is already up and comes down immediately.
        /// </summary>
        private void ReleaseCharge()
        {
            WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;
            if (weapon == null)
            {
                Stop();
                return;
            }

            ChargeSpec spec = weapon.HeavyCharge;

            _timing = Scaled(weapon.GetAttackTiming(AttackAction.Heavy));
            _timing.Windup = Mathf.Lerp(_timing.Windup, spec.ReleaseWindup, Charge);
            _timing.Damage *= Mathf.Lerp(1f, spec.DamageMultiplier, Charge);

            StartHit();
        }

        /// <summary>
        /// Seconds for one pass of the charge loop. Not serialized: it is a breathing rate, not a
        /// tuning knob — the clip only has to look alive while the player holds it.
        /// </summary>
        private const float ChargeCycle = 0.5f;

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

            // A Heavy held past the threshold releases into its own bigger swing. Below it, a
            // barely-held Heavy must look exactly like a tapped one — it is one.
            if (_action == AttackAction.Heavy && Charge >= chargedClipThreshold)
            {
                state = CharacterState.HeavyCharged;
            }

            // Frame count MUST come from the set that actually draws her. The weapon's set has no
            // attack clips (the sword is baked into the body art), and a hardcoded guess that
            // overshoots makes the clip wrap and restart partway through the action — it reads as
            // the attack trying to fire a second time and being cut off.
            int frames = 0;
            CharacterState artState = state;
            if (characterView != null && characterView.BodyAnimation != null)
            {
                frames = characterView.BodyAnimation.FrameCount(state, characterAnimator.Facing);

                // A state with no art authored yet falls back to its older sibling, so match the
                // length of whatever is actually going to be drawn.
                if (frames <= 0)
                {
                    artState = state.FallbackArt();
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
            _chargeHeld = 0f;
            Charge = 0f;
            if (characterAnimator != null) characterAnimator.CancelAction();
            Finished?.Invoke();
        }
    }
}
