using System;
using UnityEngine;

namespace Deeper.Animation
{
    /// <summary>
    /// Owns the character's animation pose — state, facing and a free-running frame counter — and
    /// announces changes. It does not touch renderers itself: every layer of the rig reads this
    /// one pose, which is what keeps body and equipment frame-locked instead of each layer running
    /// its own clock.
    ///
    /// Deliberately not a Unity <c>Animator</c>: a paper-doll rig would need one controller per
    /// layer kept in sync, where this needs one integer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        // Divisible by every clip length 1..10, so wrapping never lands mid-cycle.
        private const int FrameWrap = 5040;

        [Tooltip("Playback rate for all states.")]
        [SerializeField] private float framesPerSecond = 8f;

        [SerializeField] private Facing startingFacing = Facing.Down;

        private float _timer;

        private CharacterState _actionState;
        private float _actionElapsed;
        private float _actionDuration;
        private int _actionFrames;

        // Phase-aligned playback. When set, the clip's frames are distributed by meaning
        // (windup poses / the strike / the follow-through) rather than spread evenly.
        private bool _phaseAligned;
        private float _phaseWindup;
        private float _phaseActive;
        private int _strikeFrame;

        private bool _facingOverridden;
        private bool _reversePlayback;

        /// <summary>True while the walk cycle is playing backwards (backpedalling).</summary>
        public bool ReversePlayback { get { return _reversePlayback; } }

        /// <summary>
        /// The frame index views should draw. Negated while backpedalling — SpriteAnimationSet
        /// wraps negative indices, so this walks the clip in reverse with no extra art.
        /// </summary>
        public int PlaybackFrame { get { return _reversePlayback ? -Frame : Frame; } }

        /// <summary>
        /// Points her at an explicit facing, taking direction away from <see cref="SetMotion"/>.
        /// Mouse aim calls this every frame; once used, movement no longer steers her.
        /// </summary>
        public void SetFacing(Facing facing)
        {
            _facingOverridden = true;

            if (facing == Facing) return;

            Facing = facing;
            PoseChanged?.Invoke();
        }

        /// <summary>Hands direction back to movement — for cutscenes or a gamepad-only mode.</summary>
        public void ClearFacingOverride()
        {
            _facingOverridden = false;
        }

        public CharacterState State { get; private set; }
        public Facing Facing { get; private set; }
        public int Frame { get; private set; }

        /// <summary>True while a one-shot action (an attack) owns the pose.</summary>
        public bool InAction => _actionDuration > 0f;

        /// <summary>Right-hand art is the authored side, so the left-hand facings are mirrored.</summary>
        public bool FlipX => Facing.IsMirrored();

        /// <summary>Raised whenever state, facing or frame changes — never per-frame otherwise.</summary>
        public event Action PoseChanged;

        private void Awake() => Facing = startingFacing;

        /// <summary>
        /// Drops any action clip that was still playing.
        ///
        /// This matters for pooled actors: an enemy released partway through its Death clip never
        /// runs <c>Awake</c> again, so it would come back out of the pool still holding the Death
        /// pose and play out the rest of it while very much alive.
        /// </summary>
        private void OnEnable() => CancelAction();

        /// <summary>
        /// Feeds the intended movement direction. Zero means idle; facing is kept from the last
        /// non-zero direction so the character doesn't snap back to Down when stopping.
        /// </summary>
        public void SetMotion(Vector2 direction)
        {
            bool moving = direction.sqrMagnitude > 0.0001f;
            bool changed = false;

            // When something else owns facing (mouse aim), movement must not fight it for
            // direction — it only decides Idle vs Move, and whether the walk plays reversed.
            if (moving && !_facingOverridden)
            {
                Facing next = FromDirection(direction);

                if (next != Facing)
                {
                    Facing = next;
                    changed = true;
                }
            }

            if (moving && _facingOverridden)
            {
                // Backpedalling: moving against the way she faces. Playing the walk cycle in
                // reverse reads as stepping backwards and costs no extra art. Without it,
                // strafing away from the cursor looks like moonwalking.
                bool back = Vector2.Dot(direction.normalized, Facing.ToVector()) < -0.35f;
                if (back != _reversePlayback)
                {
                    _reversePlayback = back;
                    changed = true;
                }
            }
            else if (_reversePlayback)
            {
                _reversePlayback = false;
                changed = true;
            }

            // An action owns the state until it finishes; facing still tracks input so the
            // player can aim mid-swing without the attack animation being cut short.
            if (!InAction)
            {
                CharacterState nextState = moving ? CharacterState.Move : CharacterState.Idle;
                if (nextState != State)
                {
                    State = nextState;
                    Frame = 0;
                    _timer = 0f;
                    changed = true;
                }
            }

            if (changed) PoseChanged?.Invoke();
        }

        /// <summary>
        /// Plays a one-shot action clip stretched across <paramref name="duration"/> seconds,
        /// then hands the pose back to <see cref="SetMotion"/>.
        ///
        /// Attack clips cannot use the shared <see cref="framesPerSecond"/>: BALANCE §2 gives the
        /// Katana's Basic Attack 0.36s in total, which is shorter than its four frames would take
        /// at 8fps. The action's own duration drives its frame rate instead.
        /// </summary>
        public void PlayAction(CharacterState state, float duration, int frameCount)
        {
            if (duration <= 0f || frameCount <= 0) return;

            _actionState = state;
            _actionDuration = duration;
            _actionFrames = frameCount;
            _actionElapsed = 0f;
            _phaseAligned = false;

            State = state;
            Frame = 0;
            _timer = 0f;
            PoseChanged?.Invoke();
        }

        /// <summary>
        /// Plays a one-shot action clip with its frames pinned to the attack's phases instead of
        /// spread evenly across the total duration.
        ///
        /// An attack clip's frames are not equal in meaning: the early ones are the wind-up, one
        /// frame is the strike itself, and the rest are follow-through. Spreading them evenly puts
        /// the blade wherever the arithmetic lands — measured on the Katana's Heavy (0.30 windup /
        /// 0.12 active / 0.35 recovery, four frames at 0.19s each), the damage window opened while
        /// the sword was still raised overhead, and the actual sweep happened during Recovery. That
        /// is why a hit reads as disconnected from the swing.
        ///
        /// Mapping frames onto the phases makes the strike land inside the Active window by
        /// construction, for every direction and every weapon, without touching BALANCE's timings.
        /// </summary>
        /// <param name="strikeFrame">Index of the frame where the blade connects.</param>
        public void PlayAction(
            CharacterState state, float windup, float active, float recovery, int frameCount, int strikeFrame)
        {
            float total = windup + active + recovery;
            if (total <= 0f || frameCount <= 0) return;

            PlayAction(state, total, frameCount);

            _phaseAligned = true;
            _phaseWindup = windup;
            _phaseActive = active;
            _strikeFrame = Mathf.Clamp(strikeFrame, 0, frameCount - 1);
        }

        /// <summary>Spreads <paramref name="count"/> frames across a phase, clamped to its last.</summary>
        private static int FrameInPhase(float elapsedInPhase, float phaseDuration, int first, int count)
        {
            if (count <= 1 || phaseDuration <= 0f) return first;

            int step = Mathf.FloorToInt(elapsedInPhase / phaseDuration * count);
            return first + Mathf.Clamp(step, 0, count - 1);
        }

        private int ActionFrameAt(float t)
        {
            if (!_phaseAligned)
            {
                return Mathf.Min(
                    _actionFrames - 1, Mathf.FloorToInt(t / _actionDuration * _actionFrames));
            }

            // Wind-up: every frame before the strike.
            if (t < _phaseWindup) return FrameInPhase(t, _phaseWindup, 0, _strikeFrame);

            // Active: the strike frame is held for the whole damage window, so the blade is
            // visibly where the hitbox is.
            float intoActive = t - _phaseWindup;
            if (intoActive < _phaseActive) return _strikeFrame;

            // Recovery: the follow-through, then back to the stance frame.
            //
            // Returning to frame 0 matters more than it sounds. Recovery is the longest phase
            // (0.35s on a Heavy), so whatever frame it parks on is what the attack *looks* like.
            // The five directions were drawn as five different swings and some of them end with
            // the blade carried around behind her back — held for a third of a second that reads
            // as a different move, or as the character freezing. Settling back to the stance
            // makes every direction finish the same way, with no extra art.
            float recovery = _actionDuration - _phaseWindup - _phaseActive;
            float intoRecovery = intoActive - _phaseActive;

            int follow = _actionFrames - _strikeFrame - 1;
            if (follow <= 0) return 0;

            // Split Recovery evenly between the follow-through frames and the settle.
            float followShare = recovery * follow / (follow + 1f);
            if (intoRecovery >= followShare) return 0;

            return FrameInPhase(intoRecovery, followShare, _strikeFrame + 1, follow);
        }

        /// <summary>Ends the current action early — the Dash-Attack Cancel needs this.</summary>
        public void CancelAction()
        {
            if (!InAction) return;

            _actionDuration = 0f;
            _actionFrames = 0;
            _actionElapsed = 0f;
            State = CharacterState.Idle;
            Frame = 0;
            PoseChanged?.Invoke();
        }

        /// <summary>
        /// Snaps a direction to one of eight facings by 45° octant, so a diagonal reads as a
        /// diagonal rather than collapsing onto the dominant axis.
        /// </summary>
        public static Facing FromDirection(Vector2 direction)
        {
            int octant = Mathf.RoundToInt(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f);
            if (octant < 0) octant += 8;

            switch (octant)
            {
                case 0: return Facing.Right;
                case 1: return Facing.UpRight;
                case 2: return Facing.Up;
                case 3: return Facing.UpLeft;
                case 4: return Facing.Left;
                case 5: return Facing.DownLeft;
                case 6: return Facing.Down;
                default: return Facing.DownRight;
            }
        }

        private void Update()
        {
            if (InAction)
            {
                _actionElapsed += Time.deltaTime;

                if (_actionElapsed >= _actionDuration)
                {
                    // Fall back to Idle; the next SetMotion call corrects it to Move if needed.
                    _actionDuration = 0f;
                    _actionFrames = 0;
                    State = CharacterState.Idle;
                    Frame = 0;
                    PoseChanged?.Invoke();
                    return;
                }

                int frame = ActionFrameAt(_actionElapsed);

                if (frame != Frame)
                {
                    Frame = frame;
                    PoseChanged?.Invoke();
                }

                return;
            }

            if (framesPerSecond <= 0f) return;

            _timer += Time.deltaTime;
            float step = 1f / framesPerSecond;
            bool advanced = false;

            while (_timer >= step)
            {
                _timer -= step;
                Frame = (Frame + 1) % FrameWrap;
                advanced = true;
            }

            if (advanced) PoseChanged?.Invoke();
        }
    }
}
