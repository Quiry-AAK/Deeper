using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `WaveRoom_UpperCaves_02` — the Upper Caves **Wave Room**, the single
    /// flagged one MVP §55 caps the biome at. "Wave Room" is not a room type: it is the same
    /// `CombatRoom` prefab whose `WaveSpawner` holds 3 batches instead of 1 (CORE_SYSTEMS §8).
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **20x12, down from 32x18** (owner, 2026-09-08). See <see cref="Layout_UpperCaves_01"/> for
    /// the projection arithmetic behind the shrink. It is still the biggest ordinary room in the
    /// biome and that is deliberate: 20x12 draws 32x16 world units against a camera that shows
    /// 28.4x16, so it is the one layout you cannot take in at a glance, where a standard Combat
    /// Room at 16x10 fits with margin. LEVEL_DESIGN §4 asks for exactly that difference — a Wave
    /// Room "needs larger open space… a cramped room makes batch 2+ spawn-camping trivial rather
    /// than tactical".
    ///
    /// The two positioning zones §2 asks for are a difference in *character*, not just floor area —
    /// a bigger empty box is still one zone. West is an open hall with two posts, for Bow kiting
    /// and the Greatsword's whiff-punish room; east is a four-post field to weave through and break
    /// line of sight in.
    ///
    /// Every post is an isolated single tile sitting two clear cells off every wall and off every
    /// other post. `EnemyChase` is straight-line steering with no pathfinding, so a concave pocket
    /// traps an enemy permanently and the room never unlocks.
    ///
    /// Ten spawn markers, spread to the edges — which only became authorable when `WaveSpawner`
    /// started choosing the farthest marker still inside the arriving enemy's aggro radius. Under
    /// the old fixed cycling order the outer ones would have been dead drops (brief §13.1).
    /// </summary>
    public static class Layout_UpperCaves_02
    {
        public static readonly string[] Map =
        {
            "####################",   // y = 11
            "#...0.==.3......4..#",
            "#.....==...........#",
            "#..O..==...O.......#",
            "#...1.==.cc..5.....#",
            "D.....==.cc....O...D",   // y = 6
            "D.P...==.6.........D",   // y = 5
            "#..O..==...O.....7.#",
            "#.....==.......O...#",
            "#...2.==...........#",
            "#.....==.8......9..#",
            "####################",   // y = 0
        };

        public const int Width = 20;
        public const int Height = 12;

        [MenuItem("Deeper/Build Wave Room Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_UpperCaves_02")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
