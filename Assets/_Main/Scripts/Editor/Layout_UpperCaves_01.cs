using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `CombatRoom_UpperCaves_01` — the first Combat Room, 1 of the 6 that
    /// LEVEL_DESIGN §2 asks Upper Caves for. A standard (single-wave) room.
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths so
    /// every room is stamped by the same rules. See its legend before editing.
    /// </summary>
    public static class Layout_UpperCaves_01
    {
        /// <summary>
        /// Top row first, so the string reads the way the room looks. Row 0 of the string is the
        /// room's TOP row (y = Height-1).
        ///
        /// Doors are west and east because LEVEL_DESIGN §1's floors are a linear left-to-right
        /// sequence. The trigger band sits on the room's half-way line rather than just inside the
        /// entry door, because the enemies' aggro radii are 10-12 and a lock sprung at the doorway
        /// would leave the far half of the room standing still.
        /// </summary>
        public static readonly string[] Map =
        {
            "############################",   // y = 15
            "#............==............#",
            "#............==.......3....#",
            "#.......0....==..cc........#",
            "#............==..cc.O......#",
            "#......O.....==........O...#",
            "#............==............#",
            "D.5.P........==....2.......D",   // y = 8
            "D............==............D",   // y = 7
            "#......O.....==........O...#",
            "#............==............#",
            "#............==.....O......#",
            "#.......1....==............#",
            "#............==.......4....#",
            "#............==............#",
            "############################",   // y = 0
        };

        public const int Width = 28;
        public const int Height = 16;

        [MenuItem("Deeper/Build Combat Room Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_UpperCaves_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
