using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `FinalBossArena_01` - Floor 16's first fight, **The Depth Warden, her
    /// father** (CONTENT_DESIGN section 5, GDD section Game Loop 6).
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **22x16 - 38x19 world units, the largest room in the game**, which is the whole point of
    /// Floor 16. See <see cref="Layout_MiniBossArena_01"/> for why boss arenas do not follow the
    /// shrink the Combat Rooms took.
    ///
    /// **It keeps an east door**, unlike every other boss room: beating the Warden does not end the
    /// floor - Zyno is fought immediately after, in <see cref="Layout_FinalBossArena_02"/>. That
    /// door is the only thing in the layout expressing "two fights, in this order".
    ///
    /// WARNING: **the arena LEVEL_DESIGN section 6 specifies does not exist yet.** Section 6 calls
    /// Floor 16 "the only room in the game that changes its own geometry mid-fight"; this is a flat
    /// box that does not. Recorded in the change brief rather than silently treated as the design.
    /// </summary>
    public static class Layout_FinalBossArena_01
    {
        public static readonly string[] Map =
        {
            "######################",   // y = 15
            "#....==..............#",
            "#....==..............#",
            "#....==...O......O...#",
            "#....==..............#",
            "#....==..............#",
            "#....==..............#",
            "D....==......0.......D",   // y = 8
            "D..P.==..............D",   // y = 7
            "#....==..............#",
            "#....==..............#",
            "#....==...O......O...#",
            "#....==..............#",
            "#....==..............#",
            "#....==..............#",
            "######################",   // y = 0
        };

        public const int Width = 22;
        public const int Height = 16;

        [MenuItem("Deeper/Build Final Boss Arena Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_FinalBossArena_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
