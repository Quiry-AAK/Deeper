using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `CombatRoom_UpperCaves_03` — Combat Room 2 of 6.
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    /// See its legend before editing, and <see cref="Layout_UpperCaves_01"/> for why every
    /// standard Combat Room is 16x10 rather than the 28x16 the first one shipped at.
    ///
    /// **Its character:** the pinch. Two posts stagger the middle so the open west half and the cluttered east half are entered through different lanes, rather than the room being one box.
    ///
    /// Posts sit two clear cells off every wall and off each other. `EnemyChase` is
    /// straight-line steering with no pathfinding, so a post tucked against a wall forms a
    /// concave pocket that traps an enemy — and the room only unlocks when every enemy is dead.
    /// </summary>
    public static class Layout_UpperCaves_03
    {
        public static readonly string[] Map =
        {
            "################",   // y = 9
            "#....==...0....#",   // y = 8
            "#....==........#",   // y = 7
            "#....==.O...1..#",   // y = 6
            "D..P.==........D",   // y = 5
            "D....==..cc....D",   // y = 4
            "#..2.==..cc.O..#",   // y = 3
            "#....==........#",   // y = 2
            "#..3.==..4..5..#",   // y = 1
            "################",   // y = 0
        };

        public const int Width = 16;
        public const int Height = 10;

        [MenuItem("Deeper/Build Combat Room 03 Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_UpperCaves_03")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
