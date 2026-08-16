using UnityEngine;

namespace Deeper.Animation
{
    /// <summary>
    /// Animation states the character rig can be in. Explicit values are stable — sprite sets
    /// and save data key off them.
    ///
    /// The three attack states mirror the weapon actions in BALANCE.md §2. They carry art only;
    /// the IDLE→WINDUP→ACTIVE→RECOVERY phase timing that drives them belongs to the Attack State
    /// Machine, which does not exist yet. Note that a clip's frame count and its action's duration
    /// are unrelated — Katana Basic runs 0.36s total, which is shorter than four frames at the
    /// animator's fixed 8fps, so the state machine must drive attack frame timing rather than
    /// letting the free-running counter do it.
    ///
    /// Hit remains intentionally absent rather than stubbed. Death and Dash are no longer absent —
    /// see <see cref="Death"/> and <see cref="Dash"/>.
    /// </summary>
    public enum CharacterState
    {
        Idle = 0,
        Move = 1,
        BasicAttack = 2,
        HeavyStrike = 3,
        Ultimate = 4,

        /// <summary>Second and third hits of the Basic chain. Each is its own animation rather
        /// than a replay of <see cref="BasicAttack"/> — repeating one clip reads as a stutter,
        /// not a combo. Heavy chain hits still reuse their base clip (ART_DIRECTION §46).</summary>
        BasicAttack2 = 5,
        BasicAttack3 = 6,

        /// <summary>
        /// The wind-up an enemy plays before it commits to an attack. ART_DIRECTION §4 budgets it
        /// as a state of its own for enemies (3 frames), because GDD §Combat makes reading the
        /// telegraph the thing the fight is built on — a player who cannot see the commitment
        /// coming has nothing to dodge.
        ///
        /// The player has no Telegraph art and needs none: her wind-up is the first frames of the
        /// attack clip itself, which is what <c>AttackStateMachine</c>'s phase-aligned playback
        /// spreads across the Windup phase.
        /// </summary>
        Telegraph = 7,

        /// <summary>
        /// Dying. Added for enemies, which have a 3-frame Death clip in ART_DIRECTION §4's budget.
        ///
        /// This state was previously listed as deliberately absent, on the reasoning that nothing
        /// died yet. Enemies now do. **The player still has no Death art and no death handling** —
        /// <c>Damageable.Died</c> fires on her with no subscriber — so this state exists for
        /// enemies only until player death / run-end is built.
        /// </summary>
        Death = 8,

        /// <summary>
        /// The Dig-Dash (GDD §Player, BALANCE §1). ART_DIRECTION §3 budgets it at 4 frames and marks
        /// it **shared across all weapons** — it is authored once on the body set, not per weapon.
        ///
        /// Appended rather than inserted, like every value above: this enum serialises by integer,
        /// so renumbering would silently repoint every sprite set and every saved reference.
        ///
        /// Player-only. Enemies have no dash and <c>Resolve</c> returning null for them is correct.
        /// </summary>
        Dash = 9,

        /// <summary>
        /// The unique strike that comes out of a Dig-Dash (owner-directed). A Basic Attack pressed
        /// during the dash or just after it lands is this move instead, so the dash reads as an
        /// approach rather than only as a dodge.
        ///
        /// **In no design doc.** ART_DIRECTION §3's player budget lists Basic / Heavy / Ultimate /
        /// Dig-Dash and nothing between them; this is a fourth weapon action. Recorded in the
        /// change brief. Falls back to the <see cref="BasicAttack"/> clip when unauthored, so a
        /// weapon without the art still swings rather than freezing.
        /// </summary>
        DashAttack = 10,

        /// <summary>
        /// The held pose while a Heavy Strike is charging (owner-directed) — blade up, feet braced.
        ///
        /// This is the only **looping** action clip on the rig. Every other one is a fixed-duration
        /// one-shot, because an attack has a known length; a charge lasts exactly as long as the
        /// player holds the button, so it plays through <c>CharacterAnimator.PlayLoop</c> and is
        /// ended by the release rather than by a timer.
        /// </summary>
        HeavyCharge = 11,

        /// <summary>
        /// The released swing of a fully-charged Heavy Strike. Separate art from
        /// <see cref="HeavyStrike"/> so the payoff looks like more than the tap did — a charge that
        /// released into the identical animation would read as the hold having done nothing.
        ///
        /// Falls back to <see cref="HeavyStrike"/> art when unauthored, which is also
        /// ART_DIRECTION §3's own rule for Heavy chain extensions.
        /// </summary>
        HeavyCharged = 12,
    }

    public static class CharacterStateExtensions
    {
        /// <summary>
        /// The clip to draw when this state has no art authored on a sprite set, or the state
        /// itself when it is the base case.
        ///
        /// **One definition, deliberately.** Both the thing that picks a clip's frame count
        /// (<c>AttackStateMachine</c>) and the thing that resolves its sprite
        /// (<c>CharacterLayerView</c>) need this answer, and two copies of it would drift the
        /// moment a state was added to only one of them — the frame count would then come from a
        /// different clip than the one being drawn.
        ///
        /// Every state added after the original three has an older sibling that reads acceptably
        /// in its place, which is what lets a new action ship as working code before its animation
        /// exists. A blank sprite is far worse than a repeated one.
        /// </summary>
        public static CharacterState FallbackArt(this CharacterState state)
        {
            switch (state)
            {
                // The Basic chain's later hits, and the Dash Attack, are all cuts from the same
                // stance — the base swing stands in for any of them.
                case CharacterState.BasicAttack2:
                case CharacterState.BasicAttack3:
                case CharacterState.DashAttack:
                    return CharacterState.BasicAttack;

                // ART_DIRECTION §3 already makes replaying the base Heavy the rule for chain
                // extensions; a charge hold and a charged release are the same case.
                case CharacterState.HeavyCharge:
                case CharacterState.HeavyCharged:
                    return CharacterState.HeavyStrike;

                // A dash is 0.18s, so an unbound clip would blink her out for about three frames
                // at speed — which reads as a bug in the dash rather than as missing art.
                case CharacterState.Dash:
                    return CharacterState.Move;

                default: return state;
            }
        }
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

        /// <summary>
        /// Unit vector for a facing. Diagonals are normalized so a lunge or dash travels the same
        /// distance in every direction — an unnormalized diagonal would move ~1.41x too far.
        /// </summary>
        public static Vector2 ToVector(this Facing facing)
        {
            const float D = 0.70710678f;   // 1/sqrt(2)

            switch (facing)
            {
                case Facing.Down: return new Vector2(0f, -1f);
                case Facing.Up: return new Vector2(0f, 1f);
                case Facing.Left: return new Vector2(-1f, 0f);
                case Facing.Right: return new Vector2(1f, 0f);
                case Facing.DownLeft: return new Vector2(-D, -D);
                case Facing.DownRight: return new Vector2(D, -D);
                case Facing.UpLeft: return new Vector2(-D, D);
                default: return new Vector2(D, D);
            }
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
