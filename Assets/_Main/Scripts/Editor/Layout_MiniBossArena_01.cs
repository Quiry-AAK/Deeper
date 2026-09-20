using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `MiniBossArena_01` - the arena that ends every 5th floor
    /// (LEVEL_DESIGN section 6). **One layout, reused by all three Mini-Bosses**, dressed per biome:
    /// the same argument the Secret Vault already makes, that the fight is what differs, not the box.
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **20x14 - 34x17 world units, the second largest room in the game.** Section 6 asks for a
    /// "large open arena... must accommodate the boss's full attack radius plus room for the player
    /// to kite", and that is the one place the shrink which took Combat Rooms to 16x10 must not
    /// follow: a boss you cannot back away from is a boss you cannot fight.
    ///
    /// **Four posts, all off the centre line.** The boss spawns at the single marker and walks
    /// straight at her - `EnemyChase` has no pathfinding - so the lane between that marker and the
    /// entry band is deliberately empty. The posts sit above and below it, where they are cover to
    /// break line of sight against rather than an obstacle the boss can jam on.
    ///
    /// **One spawn marker, not several.** A boss arrives in the middle of its own arena; the
    /// spread-marker placement that makes a Combat Room's fight vary would only make a boss appear
    /// somewhere different each time, for no gain. Its `EnemyDefinition` carries a generous aggro
    /// radius so it always engages from there.
    /// </summary>
    public static class Layout_MiniBossArena_01
    {
        public static readonly string[] Map =
        {
            "####################",   // y = 13
            "#....==............#",
            "#....==............#",
            "#....==...O....O...#",
            "#....==............#",
            "#....==............#",
            "D..P.==......0.....D",   // y = 7
            "D....==............D",   // y = 6
            "#....==............#",
            "#....==............#",
            "#....==...O....O...#",
            "#....==............#",
            "#....==............#",
            "####################",   // y = 0
        };

        public const int Width = 20;
        public const int Height = 14;

        [MenuItem("Deeper/Build Mini-Boss Arena Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_MiniBossArena_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
