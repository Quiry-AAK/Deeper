using System.Collections.Generic;
using Deeper.Rooms;
using UnityEngine;

namespace Deeper.Testing
{
    /// <summary>
    /// Which room is currently in the sandbox. Picks one from a list of room prefabs, drops the
    /// previous one, and puts the player on the new room's start marker.
    ///
    /// TEST-ONLY — this belongs to `TestScene`. It is deliberately **not** the floor loader:
    /// `CombatRoom.Cleared` is still the hook that will advance a real floor, and nothing here
    /// sequences anything or draws from a bag. This only answers "show me that room", which is the
    /// question a sandbox with more than one authored layout starts needing.
    ///
    /// One room exists at a time rather than all of them side by side, because the room prefabs
    /// carry their own tilemaps and six of them mounted at once is six tilemap sets in the scene —
    /// and because the harness's status readout only honestly describes one room.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestRoomSelector : MonoBehaviour
    {
        [Tooltip("The room prefabs this sandbox can show, in menu order. Adding a layout here is " +
                 "all it takes to make it testable.")]
        [SerializeField] private GameObject[] roomPrefabs;

        [Tooltip("Where a loaded room is parented. The `Level` object in TestScene, so a room sits " +
                 "where the hand-mounted one used to.")]
        [SerializeField] private Transform mountPoint;

        [Tooltip("Which room is loaded when Play starts. -1 loads nothing.")]
        [SerializeField] private int startIndex;

        [Header("Refs — wired in the scene")]
        [Tooltip("Re-pointed at each room as it loads, so the overlay's status line follows " +
                 "whichever room is actually live.")]
        [SerializeField] private TestRoomControls roomControls;

        private GameObject _current;
        private int _currentIndex = -1;

        public int Count { get { return roomPrefabs != null ? roomPrefabs.Length : 0; } }
        public int CurrentIndex { get { return _currentIndex; } }

        /// <summary>The loaded room, or null before anything is loaded.</summary>
        public CombatRoom Current
        {
            get { return _current != null ? _current.GetComponent<CombatRoom>() : null; }
        }

        public string NameAt(int index)
        {
            if (roomPrefabs == null || index < 0 || index >= roomPrefabs.Length) return "-";

            return roomPrefabs[index] != null ? roomPrefabs[index].name : "(empty)";
        }

        private void Start()
        {
            // Start, not Awake: the room's own Start calls Arm(), which reaches into a WaveSpawner
            // whose pools are built in its Awake. Loading from Awake would race that.
            if (startIndex >= 0) Load(startIndex);
        }

        /// <summary>
        /// Swaps in room <paramref name="index"/>. Public so a probe can drive it — simulated key
        /// presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        public void Load(int index)
        {
            if (roomPrefabs == null || index < 0 || index >= roomPrefabs.Length) return;
            if (roomPrefabs[index] == null) return;

            Unload();

            _current = Instantiate(roomPrefabs[index], mountPoint != null ? mountPoint : transform);

            // Instantiate appends "(Clone)", and a stray "(Clone)" root is exactly what the
            // verification pass looks for to spot a leak. Naming it after the prefab keeps that
            // check meaningful.
            _current.name = roomPrefabs[index].name;
            _currentIndex = index;

            if (roomControls != null) roomControls.Bind(Current);

            MovePlayerToStart();
        }

        [ContextMenu("Reload Current")]
        public void Reload()
        {
            if (_currentIndex >= 0) Load(_currentIndex);
        }

        public void Unload()
        {
            if (_current != null) Destroy(_current);

            _current = null;
            _currentIndex = -1;

            if (roomControls != null) roomControls.Bind(null);
        }

        /// <summary>
        /// Puts her on the new room's `PlayerStart`. Without this a room swap leaves her standing
        /// wherever the previous layout put her, which in a smaller room is inside a wall.
        /// </summary>
        private void MovePlayerToStart()
        {
            if (_current == null) return;

            Transform start = _current.transform.Find("PlayerStart");
            if (start == null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Moved through the body, not the transform, for the reason TestControls.ResetPlayer
            // gives: writing transform.position on a rigidbody leaves the physics position behind
            // for a frame, which reads as a snap-back.
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = start.position;
            else player.transform.position = start.position;
        }
    }
}
