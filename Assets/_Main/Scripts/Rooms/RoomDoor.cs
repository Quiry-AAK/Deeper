using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// One door in a room's wall. Open, and the gap is a hole you can walk through; closed, and it
    /// is wall.
    ///
    /// This is the visible half of CORE_SYSTEMS §8's room lock — <see cref="CombatRoom"/> decides
    /// when, this decides what that looks like. One door per object rather than a single component
    /// driving several, because a door is a thing in a place: it has its own position, its own
    /// collider bounds and its own sprite, and a room with three exits should read as three objects
    /// in the Hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomDoor : MonoBehaviour
    {
        [Header("Refs — on this object")]
        [Tooltip("Hidden while open, so an open doorway reads as the hole it is rather than as a " +
                 "door you can walk through.")]
        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Must NOT be a trigger. It blocks the player and the enemies alike — nothing " +
                 "should be able to leave a locked room, in either direction.")]
        [SerializeField] private Collider2D barrier;

        private void Awake()
        {
            // GetComponent, never RigRefs.Find: the layout already pins both of these to this
            // object, and a search from transform.root would happily return the OTHER door's
            // sprite — every door in the room would then draw and hide the same one.
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            if (barrier == null) barrier = GetComponent<Collider2D>();
        }

        public bool IsOpen { get { return barrier != null && !barrier.enabled; } }

        [ContextMenu("Open")]
        public void Open()
        {
            SetShut(false);
        }

        [ContextMenu("Close")]
        public void Close()
        {
            SetShut(true);
        }

        private void SetShut(bool shut)
        {
            // No tween. A door that slides shut over a quarter-second is a quarter-second in which
            // "am I locked in" is ambiguous, and the room's whole job is to make that unambiguous.
            // If it ever wants animating, the state change stays here and the art follows it.
            if (barrier != null) barrier.enabled = shut;
            if (sprite != null) sprite.enabled = shut;
        }
    }
}
