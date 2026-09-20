namespace Deeper.Run
{
    /// <summary>
    /// What slot a room fills on a floor.
    ///
    /// **A role is a property of the floor plan, not of the room prefab** — which is why this lives
    /// beside <see cref="FloorLoader"/> rather than in `Scripts/Rooms/`, and why no room component
    /// carries one. The same `MiniBossArena_01` prefab is drawn as all three biomes' Mini-Boss with
    /// only its encounter swapped; a `role` field on the prefab would have to be three values at
    /// once.
    ///
    /// This exists so the loader stops special-casing one room type. It used to ask
    /// `if (isLastOfFloor &amp;&amp; floor % 5 == 0)` for the Mini-Boss and nothing else, and every room
    /// type still to come — the Trapped Soul (CORE_SYSTEMS §14), the Secret Vault detour (§8) — would
    /// have added another branch beside it. With a role, adding one is a value here, a `RoleRoom`
    /// entry on the biome's pool, and one line in <see cref="FloorLoader"/>'s `RoleFor`.
    ///
    /// **Unauthored roles are not an error.** <see cref="BiomeRoomPool.RoomFor"/> returns null for
    /// anything the biome has no prefab for, and the loader warns and draws an ordinary Combat Room
    /// instead. That is what lets a role be declared here before the room it names exists — the
    /// Trapped Soul and the Secret Vault are both in that state today.
    /// </summary>
    public enum RoomRole
    {
        /// <summary>An ordinary fight, drawn from the biome's bag. Every room that is not one of the below.</summary>
        Combat = 0,

        /// <summary>Ends every 5th floor (LEVEL_DESIGN §6). One arena per biome, boss swapped by encounter.</summary>
        MiniBoss = 1,

        /// <summary>Floor 16's first fight — the Depth Warden, her father (CONTENT_DESIGN §5).</summary>
        FinalBoss = 2,

        /// <summary>Floor 16's second fight — Zyno. Clearing this is what wins a run.</summary>
        TrueFinalBoss = 3,

        /// <summary>
        /// The key-gated detour (CORE_SYSTEMS §8). **Declared, never drawn.** The room is built and
        /// its layout is a dead end, and §8's "detour" needs the branch LEVEL_DESIGN §1 rules out —
        /// a design question, recorded in the change brief, not a gap in this file.
        /// </summary>
        SecretVault = 4,

        /// <summary>The bound soul (CORE_SYSTEMS §14). Declared; no room exists yet.</summary>
        TrappedSoul = 5,
    }
}
