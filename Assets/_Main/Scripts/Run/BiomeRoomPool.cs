using System;
using System.Collections.Generic;
using UnityEngine;
using Deeper.Rooms;

namespace Deeper.Run
{
    /// <summary>
    /// Everything a floor of one biome draws from: its rooms, its look, and how long a floor is.
    ///
    /// There are deliberately **no weights**. CORE_SYSTEMS §8 describes an unweighted reshuffling
    /// bag — "the pool shuffles, is drawn through without immediate repeats, and reshuffles once
    /// exhausted" — and a layout that should come up twice as often is listed twice. That is what a
    /// bag is; adding a weight field would be inventing a mechanic the design does not have.
    /// </summary>
    [CreateAssetMenu(fileName = "Pool_", menuName = "Deeper/Rooms/Biome Room Pool", order = 4)]
    public sealed class BiomeRoomPool : ScriptableObject
    {
        /// <summary>A room that fills one named slot on a floor rather than being drawn from the bag.</summary>
        [Serializable]
        public sealed class RoleRoom
        {
            [Tooltip("Which slot on a floor this room fills.")]
            public RoomRole role;

            [Tooltip("The room: its layout prefab, and the fights allowed in it. One arena prefab " +
                     "serves all three biomes' Mini-Bosses — what differs is the encounter here.")]
            public RoomOption room = new RoomOption();
        }

        [Tooltip("The biome's hand-built layouts. LEVEL_DESIGN §2 budgets 6 Combat Rooms per biome.")]
        [SerializeField] private RoomOption[] options;

        [Tooltip("Looks this biome's rooms may be dressed in. One theme is a biome that always " +
                 "looks the same; several are what makes a repeated layout read as a new room.")]
        [SerializeField] private RoomTheme[] themes;

        [Tooltip("Fewest rooms on a floor. LEVEL_DESIGN §5 says 3-5.")]
        [SerializeField] private int minRoomsPerFloor = 3;

        [Tooltip("Most rooms on a floor. LEVEL_DESIGN §5 says 3-5.")]
        [SerializeField] private int maxRoomsPerFloor = 5;

        [Tooltip("Rooms that fill a named slot instead of being drawn: the Mini-Boss arena that " +
                 "ends every 5th floor, the two Floor 16 arenas. A role with no entry here is not " +
                 "an error — FloorLoader warns and draws a Combat Room, which is how a role can be " +
                 "named before its room exists.")]
        [SerializeField] private RoleRoom[] specialRooms = new RoleRoom[0];

        public IList<RoomOption> Options { get { return options; } }
        public RoomTheme[] Themes { get { return themes; } }
        public int MinRoomsPerFloor { get { return Mathf.Max(1, minRoomsPerFloor); } }
        public int MaxRoomsPerFloor { get { return Mathf.Max(MinRoomsPerFloor, maxRoomsPerFloor); } }

        /// <summary>
        /// The room filling <paramref name="role"/>, or null if this biome authors none.
        ///
        /// Null is a supported answer, not a failure — see <see cref="RoomRole"/>. It replaced a
        /// single `miniBossRoom` field, which could only ever express one special room and had
        /// already forced `FloorLoader` to hardcode which floor it belonged on.
        /// </summary>
        public RoomOption RoomFor(RoomRole role)
        {
            if (specialRooms == null) return null;

            foreach (RoleRoom entry in specialRooms)
            {
                if (entry == null || entry.role != role) continue;
                if (entry.room == null || entry.room.prefab == null) continue;

                return entry.room;
            }

            return null;
        }
    }
}
