using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// How a room joins the floor: the way in, the way on, and where the player stands when she is
    /// placed here rather than walking in. GDD's phrase for a floor is "connected linearly
    /// downward", and this is the per-room half of that.
    ///
    /// Rooms are mounted physically adjacent rather than teleported between, and this component is
    /// what makes the alignment expressible. Placing the next room at
    ///
    ///     next.position = current.EastAnchor.position + Vector3.right - next.WestAnchor.localPosition
    ///
    /// lines the two doorways up on both axes, and because it is written in terms of door positions
    /// rather than room widths it needs no special case for rooms of different heights: the 32x18
    /// Wave Room, whose west door sits a tile higher than the 28x16 Combat Room's, comes out shifted
    /// down by exactly that tile.
    ///
    /// Adjacency is also what lets the floor loader stay small. There is no arrival trigger and no
    /// exit volume, because the next room's own <see cref="RoomEntry"/> band already is one; and the
    /// engineering plan's "load and spring must not share a frame" trap cannot fire, because she has
    /// to walk the length of a room to reach the next band.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomConnection : MonoBehaviour
    {
        [Header("Doors")]
        [Tooltip("The way in. Null on a room that can only be entered by being placed in it.")]
        [SerializeField] private RoomDoor west;

        [Tooltip("The way on. Null on a dead end — the Secret Vault is authored as one, so a floor " +
                 "that still has rooms to come must not draw it.")]
        [SerializeField] private RoomDoor east;

        [Header("Where she starts")]
        [Tooltip("The map's 'P'. Used when she is placed into this room rather than walking in — " +
                 "the run's very first room.")]
        [SerializeField] private Transform arrival;

        public RoomDoor West { get { return west; } }
        public RoomDoor East { get { return east; } }
        public Transform Arrival { get { return arrival; } }

        /// <summary>Whether a floor can be continued through this room.</summary>
        public bool HasExit { get { return east != null; } }

        /// <summary>Where the next room's west doorway has to meet. Null on a dead end.</summary>
        public Transform EastAnchor { get { return east != null ? east.transform : null; } }

        /// <summary>This room's own doorway, measured in its local space.</summary>
        public Transform WestAnchor { get { return west != null ? west.transform : null; } }
    }
}
