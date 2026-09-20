using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `FinalBossArena_02` - Floor 16's second fight, **Zyno, the true Final
    /// Boss** (CONTENT_DESIGN section 5, GDD section Game Loop 6).
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **A dead end - no east door, and that is what ends the run.** It is the only room the floor
    /// loader mounts knowing nothing follows it; clearing it raises `FloorLoader.RunCompleted` and
    /// the summary opens on a victory. Giving this room an east door would leave the player in a
    /// corridor with no next room, which the loader reports as a drawing error rather than as a win.
    ///
    /// Three posts rather than the Warden's four, arranged asymmetrically, so the second fight of
    /// the floor does not read as the same room dressed twice. Same rule as everywhere else: each is
    /// an isolated tile, two clear cells off every wall and off every other post, and none sits in
    /// the lane between the spawn marker and the entry band.
    ///
    /// WARNING: **Zyno's MVP fight is a palette swap of an existing Mini-Boss** (CONTENT_DESIGN
    /// section 5, BALANCE section 6) and this arena is its placeholder. See the change brief.
    /// </summary>
    public static class Layout_FinalBossArena_02
    {
        public static readonly string[] Map =
        {
            "######################",   // y = 15
            "#....==..............#",
            "#....==..............#",
            "#....==......O.......#",
            "#....==.O............#",
            "#....==..............#",
            "#....==..............#",
            "D....==......0.......#",   // y = 8
            "D..P.==..............#",   // y = 7
            "#....==..............#",
            "#....==..............#",
            "#....==......O.......#",
            "#....==..............#",
            "#....==..............#",
            "#....==..............#",
            "######################",   // y = 0
        };

        public const int Width = 22;
        public const int Height = 16;

        [MenuItem("Deeper/Build True Final Boss Arena Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_FinalBossArena_02")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
