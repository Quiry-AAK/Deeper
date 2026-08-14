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

            [Tooltip("For attack clips: the frame where the blade actually connects, one entry " +
                     "per authored direction in FacingArt order (Down, Up, Side, DownDiagonal, " +
                     "UpDiagonal). Leave empty, or 0, for the middle frame.\n\n" +
                     "This is per-direction because the generated art is: the five directions " +
                     "were drawn as five different swings, and the cut lands on a different " +
                     "frame in each. Side cuts on frame 2 while UpDiagonal is still winding up " +
                     "and does not cut until frame 3, so a single shared index fires hitstop and " +
                     "camera shake before the blade has moved in some directions.")]
            public int[] StrikeFrames;
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

        /// <summary>
        /// The frame where this clip's blade connects, for the given facing. Falls back to the
        /// middle frame when unauthored, which is the shape most attack clips have.
        /// </summary>
        public int StrikeFrame(CharacterState state, Facing facing)
        {
            int count = FrameCount(state, facing);
            if (count <= 0) return 0;

            int fallback = count / 2;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].State != state) continue;

                int[] strikes = clips[i].StrikeFrames;
                int index = (int)facing.ToArt();
                if (strikes == null || index >= strikes.Length) return fallback;

                int strike = strikes[index];
                return strike > 0 && strike < count ? strike : fallback;
            }

            return fallback;
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
