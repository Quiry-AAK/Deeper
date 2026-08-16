using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `WaveRoom_UpperCaves_02` — the Upper Caves **Wave Room**, 1 of the 6
    /// Combat Rooms LEVEL_DESIGN §2 asks the biome for and the single flagged one MVP §55 caps it
    /// at for MVP. "Wave Room" is not a room type: it is the same `CombatRoom` prefab whose
    /// `WaveSpawner` holds 3 batches instead of 1 (CORE_SYSTEMS §8).
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    /// </summary>
    public static class Layout_UpperCaves_02
    {
        /// <summary>
        /// Top row first, so the string reads the way the room looks. Row 0 of the string is the
        /// room's TOP row (y = Height-1).
        ///
        /// **32x18, larger than room 01's 28x16**, per LEVEL_DESIGN §4: a Wave Room "needs larger
        /// open space… a cramped room makes batch 2+ spawn-camping trivial rather than tactical".
        ///
        /// The two positioning zones §2 asks for are a difference in *character*, not just floor
        /// area — a bigger empty box is still one zone. West is an open hall with two posts, for
        /// Bow kiting and the Greatsword's whiff-punish room; east is a seven-post field to weave
        /// through and break line of sight in.
        ///
        /// Every post is an isolated single tile with at least 4 tiles of clearance. `EnemyChase`
        /// is straight-line steering with no pathfinding, so a concave pocket traps an enemy
        /// permanently and the room never unlocks.
        ///
        /// Ten spawn markers, deliberately spread to the edges — which only became authorable when
        /// `WaveSpawner` started choosing the farthest marker still inside the arriving enemy's
        /// aggro radius. Under the old fixed cycling order the outer ones would have been dead
        /// drops (brief §13.1). Every floor tile has at least one marker within 10 units, the
        /// tightest radius in the roster (Cave Crawler), so the cycling fallback is a safety net
        /// rather than a live path.
        /// </summary>
        public static readonly string[] Map =
        {
            "################################",   // y = 17
            "#..............==..............#",
            "#...0..........==....O....O....#",
            "#..........8...==..............#",
            "#..............==.......1......#",
            "#......O.......==..............#",
            "#..............==....cc...O....#",
            "#...6..........==....cc........#",
            "D......P.......==.....2........D",   // y = 9
            "D..............==..............D",   // y = 8
            "#..............==....O....O....#",
            "#......O.......==..............#",
            "#..............==.......3......#",
            "#...4......9...==..............#",
            "#..............==....O....O....#",
            "#..............==..............#",
            "#....5.........==......7.......#",
            "################################",   // y = 0
        };

        public const int Width = 32;
        public const int Height = 18;

        [MenuItem("Deeper/Build Wave Room Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_UpperCaves_02")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
