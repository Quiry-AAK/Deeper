using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `SecretVault_UpperCaves_01` - the Secret Vault (CORE_SYSTEMS section 8,
    /// LEVEL_DESIGN section 2). **One layout, reused across all three biomes** with tile dressing
    /// swapped per biome, which is why it is named for the type and not for the Upper Caves alone.
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **16x10, down from 22x16** (owner, 2026-09-08; see <see cref="Layout_UpperCaves_01"/> for the
    /// projection arithmetic). It matches a standard Combat Room's footprint rather than undercutting
    /// it, because the antechamber and the interior wall spend five of the sixteen columns before the
    /// chamber starts - a vault narrower than a Combat Room would leave a chamber too small to fight
    /// six guards in. LEVEL_DESIGN section 2 asks a vault for function over layout novelty and gives
    /// no size.
    ///
    /// **It is a dead end, with one floor door.** Section 8 calls a Secret Floor a detour off the
    /// route rather than a room on it, so there is nothing to walk through to - which is also why the
    /// floor pool must never draw it (`RoomBag.Draw(needsExit: true)` refuses). The antechamber west
    /// of the interior wall is the part of the room a player without a key ever sees.
    ///
    /// The lock is the `V` gap in that interior wall, and the vault chamber is everything east of it.
    /// Two consequences worth knowing before moving anything:
    ///
    /// - **The interior wall is why the door seals during the fight.** `EnemyChase` is straight-line
    ///   steering with no pathfinding, so a guard following her back through a 1-wide doorway jams on
    ///   that wall and the room never unlocks. `VaultDoor` shuts on `RoomState.Fighting` for exactly
    ///   this reason; widening the gap is not a substitute.
    /// - **Both posts are isolated single tiles**, two clear cells off every wall and three off each
    ///   other, because a concave pocket traps an enemy permanently.
    ///
    /// The entry band sits east of the vault door, so the fight springs on entering the chamber and
    /// never on entering the antechamber.
    /// </summary>
    public static class Layout_SecretVault_01
    {
        public static readonly string[] Map =
        {
            "################",   // y = 9
            "#....#.=.......#",
            "#....#.=.0..1..#",
            "#....#.=..O....#",
            "D..P.V.=...4.T.#",   // y = 5
            "D....V.=....5..#",   // y = 4
            "#....#.=..O....#",
            "#....#.=.2..3..#",
            "#....#.=.......#",
            "################",   // y = 0
        };

        public const int Width = 16;
        public const int Height = 10;

        [MenuItem("Deeper/Build Secret Vault Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_SecretVault_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
