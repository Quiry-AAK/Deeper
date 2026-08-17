using UnityEditor;
using UnityEngine;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The authored map of `SecretVault_UpperCaves_01` — the Secret Vault (CORE_SYSTEMS §8,
    /// LEVEL_DESIGN §2). **One layout, reused across all three biomes** with tile dressing swapped
    /// per biome, which is why it is named for the type and not for the Upper Caves alone.
    ///
    /// Only the map lives here; <see cref="RoomLayout"/> owns the painting and the marker maths.
    ///
    /// **22x16, the smallest room in the game.** LEVEL_DESIGN §2 asks a vault for function over
    /// layout novelty and gives no size; a Combat Room is 28x16 and the Wave Room 32x18, and this
    /// sits deliberately under both — a vault is one fight in one chamber, not a hall.
    ///
    /// **It is a dead end, with one floor door.** §8 calls a Secret Floor a detour off the route
    /// rather than a room on it, so there is nothing to walk through to. The antechamber west of
    /// the interior wall is the part of the room a player without a key ever sees.
    ///
    /// The lock is the `V` gap in that interior wall, and the vault chamber is everything east of
    /// it. Two consequences worth knowing before moving anything:
    ///
    /// - **The interior wall is why the door seals during the fight.** `EnemyChase` is straight-line
    ///   steering with no pathfinding, so a guard following her back through a 1-wide doorway jams
    ///   on that wall and the room never unlocks. `VaultDoor` shuts on `RoomState.Fighting` for
    ///   exactly this reason; widening the gap is not a substitute.
    /// - **Every post is an isolated single tile**, the same rule rooms 01 and 02 are built to, because
    ///   a concave pocket traps an enemy permanently. Measured off the map below: the four posts sit
    ///   **5 tiles from each other** and **no closer than 2 floor tiles to any wall**, so nothing here
    ///   is a pocket — the tightest gap, between the east posts and the east wall, is a 2-wide lane
    ///   that steering walks straight down.
    ///
    /// The two positioning zones §2 asks for are a difference in character, not floor area: the
    /// chamber's middle band is an open lane for Bow kiting and the Greatsword's whiff-punish room,
    /// and the four posts give the Katana line-of-sight breaks to close through. Six spawn markers,
    /// four at the chamber's corners and two mid-edge; the farthest is **9.0 units** from the entry
    /// band she springs the fight on, inside even the Cave Crawler's 10-unit aggro radius, so
    /// `WaveSpawner`'s cycling fallback is unreachable in this room.
    /// </summary>
    public static class Layout_SecretVault_01
    {
        /// <summary>
        /// Top row first, so the string reads the way the room looks. Row 0 of the string is the
        /// room's TOP row (y = Height-1).
        ///
        /// `D` floor door (west, the only one)   `V` key-gated vault door   `T` the payout pedestal
        /// `=` entry band, inside the chamber so the fight springs on entering the vault, not the room
        /// </summary>
        public static readonly string[] Map =
        {
            "######################",   // y = 15
            "#.......#.=..........#",
            "#.......#.=4...0.....#",
            "#.......#.=..........#",
            "#.......#.=........1.#",
            "#.......#.=..O....O..#",
            "#.......#.=..........#",
            "#.......#.=..........#",   // y = 8
            "D..P....V.=........T.#",   // y = 7  — floor door, vault door and pedestal on one line
            "D.......V.=..........#",   // y = 6
            "#.......#.=..O....O..#",
            "#.......#.=........3.#",
            "#.......#.=..........#",
            "#.......#.=5...2.....#",
            "#.......#.=..........#",
            "######################",   // y = 0
        };

        public const int Width = 22;
        public const int Height = 16;

        [MenuItem("Deeper/Build Secret Vault Layout")]
        private static void PaintSelection()
        {
            if (!RoomLayout.Validate(Map, "Layout_SecretVault_01")) return;

            RoomLayout.PaintInto(Selection.activeGameObject, Map);
        }
    }
}
