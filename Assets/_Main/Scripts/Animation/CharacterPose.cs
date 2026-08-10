namespace Deeper.Animation
{
    /// <summary>
    /// Animation states the character rig can be in. Explicit values are stable — sprite sets
    /// and save data key off them. Attack/Dash/Hit/Death states join this enum when the Attack
    /// State Machine lands (Milestone 1); they are intentionally absent rather than stubbed.
    /// </summary>
    public enum CharacterState
    {
        Idle = 0,
        Move = 1,
    }

    /// <summary>
    /// Full 8-way facing, per GDD §Player's 8-directional movement.
    ///
    /// Only five directions are ever drawn: Down, Up, Side, Down-diagonal and Up-diagonal. The
    /// left-hand halves reuse their right-hand art mirrored (<see cref="FacingExtensions.IsMirrored"/>),
    /// which is what keeps this within ART_DIRECTION §3's "8-directional, can mirror for 4 base
    /// directions" budget instead of authoring eight separate sets.
    ///
    /// Values are stable — do not renumber.
    /// </summary>
    public enum Facing
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3,
        DownLeft = 4,
        DownRight = 5,
        UpLeft = 6,
        UpRight = 7,
    }

    /// <summary>The five authored art rows the eight facings collapse onto.</summary>
    public enum FacingArt
    {
        Down = 0,
        Up = 1,
        Side = 2,
        DownDiagonal = 3,
        UpDiagonal = 4,
    }

    public static class FacingExtensions
    {
        /// <summary>True when this facing reuses its mirrored counterpart's art.</summary>
        public static bool IsMirrored(this Facing facing)
        {
            return facing == Facing.Left || facing == Facing.DownLeft || facing == Facing.UpLeft;
        }

        /// <summary>Collapses a facing onto the art row that draws it.</summary>
        public static FacingArt ToArt(this Facing facing)
        {
            switch (facing)
            {
                case Facing.Down: return FacingArt.Down;
                case Facing.Up: return FacingArt.Up;
                case Facing.Left:
                case Facing.Right: return FacingArt.Side;
                case Facing.DownLeft:
                case Facing.DownRight: return FacingArt.DownDiagonal;
                default: return FacingArt.UpDiagonal;
            }
        }
    }
}
