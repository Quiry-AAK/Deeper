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
        /// sequence.
        ///
        /// **16x10, down from the 28x16 this room shipped at** (owner, 2026-09-08: "rooms are a lot
        /// big… we only use first half of the room"). The projection is why the old number felt
        /// enormous: an isometric `w x h` map draws a diamond `(w+h)` wide by `(w+h)/2` tall, so
        /// 28x16 was **44x22 world units** against a camera that shows 28.4x16 — a room and a half.
        /// 16x10 is 26x13, which fits the view with margin. Size is chosen per room from what is in
        /// it; the Wave Room is bigger because it holds twice the fight.
        ///
        /// The trigger band sits **five cells inside the west door** rather than on the room's
        /// half-way line, which is where it sat when the room was twice this size. The old placement
        /// existed because aggro radii are 10-12 and a lock sprung at the doorway left the far half
        /// standing still; at 26 units across the whole room is inside that radius, and
        /// `WaveSpawner` already picks the farthest marker still within an arriving enemy's. Putting
        /// it back at the middle of *this* room would spring the fight with two thirds of the floor
        /// behind her — the complaint, rebuilt at a smaller scale.
        ///
        /// Both posts sit two clear cells off every wall. `EnemyChase` is straight-line steering
        /// with no pathfinding, so a post tucked against a wall forms a concave pocket that traps an
        /// enemy permanently — and the room only unlocks when every enemy is dead.
        ///
        /// Two spawn markers (2 and 4) are west of the band so a fight can arrive behind her. In a
        /// room this size every marker is inside every enemy's aggro radius, which is what makes
        /// that safe.
        /// </summary>
        public static readonly string[] Map =
        {
            "################",   // y = 9
            "#.....==.0.....#",
            "#.....==....1..#",
            "#...O.==.......#",
            "D..P..==cc.O...D",   // y = 5
            "D.....==cc.....D",   // y = 4
            "#..2..==....3..#",
            "#.....==.......#",
            "#...4.==..5....#",
            "################",   // y = 0
        };

        public const int Width = 16;
        public const int Height = 10;

        [MenuItem("Deeper/Build Combat Room Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_UpperCaves_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
