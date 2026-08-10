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

        public CharacterState State { get; private set; }
        public Facing Facing { get; private set; }
        public int Frame { get; private set; }

        /// <summary>Right-hand art is the authored side, so the left-hand facings are mirrored.</summary>
        public bool FlipX => Facing.IsMirrored();

        /// <summary>Raised whenever state, facing or frame changes — never per-frame otherwise.</summary>
        public event Action PoseChanged;

        private void Awake() => Facing = startingFacing;

        /// <summary>
        /// Feeds the intended movement direction. Zero means idle; facing is kept from the last
        /// non-zero direction so the character doesn't snap back to Down when stopping.
        /// </summary>
        public void SetMotion(Vector2 direction)
        {
            bool moving = direction.sqrMagnitude > 0.0001f;
            bool changed = false;

            if (moving)
            {
                Facing next = FromDirection(direction);

                if (next != Facing)
                {
                    Facing = next;
                    changed = true;
                }
            }

            CharacterState nextState = moving ? CharacterState.Move : CharacterState.Idle;
            if (nextState != State)
            {
                State = nextState;
                Frame = 0;
                _timer = 0f;
                changed = true;
            }

            if (changed) PoseChanged?.Invoke();
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
