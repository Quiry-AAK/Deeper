using System;
using UnityEngine;

namespace Deeper.Animation
{
    /// <summary>
    /// The per-piece art table: for each <see cref="CharacterState"/>, the frame list in each
    /// authored direction. One of these exists per equipment piece and one for the bare body, so
    /// every layer of the rig can be resolved from the same (state, facing, frame) pose and stay
    /// in lockstep by construction.
    ///
    /// Five directions are authored, not eight — Side, Down-diagonal and Up-diagonal are drawn
    /// facing right and mirrored for their left-hand facings (ART_DIRECTION §3).
    /// </summary>
    [CreateAssetMenu(fileName = "Anim_", menuName = "Deeper/Animation/Sprite Animation Set", order = 0)]
    public sealed class SpriteAnimationSet : ScriptableObject
    {
        [Serializable]
        private struct Clip
        {
            public CharacterState State;
            public Sprite[] Down;
            public Sprite[] Up;
            [Tooltip("Authored facing right; mirrored for Left.")]
            public Sprite[] Side;
            [Tooltip("Authored facing down-right; mirrored for down-left.")]
            public Sprite[] DownDiagonal;
            [Tooltip("Authored facing up-right; mirrored for up-left.")]
            public Sprite[] UpDiagonal;
        }

        [SerializeField] private Clip[] clips = new Clip[0];

        /// <summary>
        /// Resolves one layer's sprite. <paramref name="frame"/> is a free-running counter — it
        /// is wrapped against the clip length here, so callers never track per-clip frame counts.
        /// </summary>
        public Sprite Resolve(CharacterState state, Facing facing, int frame)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].State != state) continue;

                Sprite[] frames = SelectFrames(clips[i], facing.ToArt());
                if (frames == null || frames.Length == 0) return null;

                int index = frame % frames.Length;
                if (index < 0) index += frames.Length;
                return frames[index];
            }

            return null;
        }

        public int FrameCount(CharacterState state, Facing facing)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].State != state) continue;

                Sprite[] frames = SelectFrames(clips[i], facing.ToArt());
                return frames != null ? frames.Length : 0;
            }

            return 0;
        }

        private static Sprite[] SelectFrames(Clip clip, FacingArt art)
        {
            switch (art)
            {
                case FacingArt.Down: return clip.Down;
                case FacingArt.Up: return clip.Up;
                case FacingArt.Side: return clip.Side;
                case FacingArt.DownDiagonal: return clip.DownDiagonal;
                default: return clip.UpDiagonal;
            }
        }
    }
}
