using System;
using Deeper.Animation;
using Deeper.Character;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Runs an enemy's attack through IDLE → WINDUP → ACTIVE → RECOVERY (CORE_SYSTEMS §1) and
    /// raises <see cref="ActiveOpened"/> when the hit becomes live. What actually happens then is
    /// the per-enemy component's business — a lunge, a thrown rock, a slam.
    ///
    /// This is the enemy-side twin of <c>AttackStateMachine</c>, deliberately not that class:
    /// that one resolves an input asset, a RunLoadout, the Ultimate Gauge, the Combo Counter,
    /// PlayerStats, the camera rig and the layered character view. Making seven dependencies
    /// optional to serve an enemy would do far more damage than a small separate timer.
    ///
    /// **The aim is locked when the wind-up starts and never updated.** That is the entire dodge
    /// contract: the telegraph is a promise about where the hit is going, and an attack that
    /// tracks the player during its own wind-up cannot be dodged, only out-ranged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TelegraphedAttack : MonoBehaviour
    {
        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;
        [SerializeField] private EnemyTarget target;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("Read for real clip lengths. Guessing a frame count higher than the clip has " +
                 "makes it wrap and visibly restart partway through the attack.")]
        [SerializeField] private SpriteAnimationView view;

        private float _elapsed;
        private float _readyAt;
        private bool _activeOpened;

        /// <summary>Raised once per attack, the moment the hit becomes live.</summary>
        public event Action ActiveOpened;

        public AttackPhase Phase { get; private set; }

        /// <summary>Direction this attack committed to, locked at the start of the wind-up.</summary>
        public Vector2 AimDirection { get; private set; }

        public bool IsAttacking { get { return Phase != AttackPhase.Idle; } }

        private AttackTiming Timing
        {
            get
            {
                return enemy != null && enemy.Definition != null
                    ? enemy.Definition.Attack
                    : default(AttackTiming);
            }
        }

        private void Awake()
        {
            enemy = RigRefs.Find(this, enemy);
            target = RigRefs.Find(this, target);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            view = RigRefs.Find(this, view);
        }

        private void OnDisable()
        {
            // Cleared so a disabled-and-re-enabled enemy cannot resume half-way through a swing
            // with a stale aim.
            Phase = AttackPhase.Idle;
            _elapsed = 0f;
            _activeOpened = false;
        }

        /// <summary>
        /// Clears the cooldown left over from a previous life.
        ///
        /// <see cref="_readyAt"/> is an absolute <c>Time.time</c>, and a recycled enemy keeps it —
        /// so an enemy that died mid-cooldown would come back out of the pool unable to attack for
        /// the remainder of it, and one that died just after attacking would look broken for two
        /// full seconds.
        /// </summary>
        private void OnEnable()
        {
            _readyAt = 0f;
        }

        private void Update()
        {
            if (Phase == AttackPhase.Idle)
            {
                if (ShouldBegin()) Begin();
                return;
            }

            AttackTiming timing = Timing;
            _elapsed += Time.deltaTime;

            // Fired on CROSSING the wind-up, not on sampling into Active: a frame longer than
            // windup+active would otherwise skip the damage window entirely, which reads as the
            // enemy attacking and the hit simply not happening.
            if (!_activeOpened && _elapsed >= timing.Windup)
            {
                _activeOpened = true;
                OpenActive(timing);
            }

            Phase = PhaseAt(_elapsed, timing);

            if (_elapsed >= timing.Total) Finish();
        }

        private bool ShouldBegin()
        {
            if (Time.time < _readyAt) return false;

            EnemyDefinition definition = enemy != null ? enemy.Definition : null;
            if (definition == null || target == null || !target.Acquired) return false;

            return target.Distance <= definition.AttackRange;
        }

        /// <summary>
        /// Starts an attack regardless of range or cooldown. Public because simulated key presses
        /// never reach play mode, so a probe has to drive the attack directly
        /// (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Begin")]
        public void Begin()
        {
            if (Phase != AttackPhase.Idle) return;

            AttackTiming timing = Timing;
            if (timing.Total <= 0f) return;

            AimDirection = target != null && target.Direction != Vector2.zero
                ? target.Direction
                : (characterAnimator != null ? characterAnimator.Facing.ToVector() : Vector2.down);

            _elapsed = 0f;
            _activeOpened = false;
            Phase = AttackPhase.Windup;

            Play(CharacterState.Telegraph, timing.Windup);
        }

        private void OpenActive(AttackTiming timing)
        {
            // The attack clip covers Active AND Recovery, so its first frame — the impact pose —
            // spans the whole damage window. The wind-up is a clip of its own, which is why
            // enemies need none of the player's phase-aligned frame pinning.
            Play(CharacterState.BasicAttack, timing.Active + timing.Recovery);

            ActiveOpened?.Invoke();
        }

        private void Finish()
        {
            Phase = AttackPhase.Idle;
            _elapsed = 0f;
            _activeOpened = false;

            EnemyDefinition definition = enemy != null ? enemy.Definition : null;
            _readyAt = Time.time + (definition != null ? definition.Cooldown : 1f);
        }

        private static AttackPhase PhaseAt(float t, AttackTiming timing)
        {
            if (t < timing.Windup) return AttackPhase.Windup;
            if (t < timing.Windup + timing.Active) return AttackPhase.Active;
            return AttackPhase.Recovery;
        }

        private void Play(CharacterState state, float duration)
        {
            if (characterAnimator == null || duration <= 0f) return;

            int frames = view != null && view.AnimationSet != null
                ? view.AnimationSet.FrameCount(state, characterAnimator.Facing)
                : 0;

            if (frames <= 0) return;

            characterAnimator.PlayAction(state, duration, frames);
        }
    }
}
