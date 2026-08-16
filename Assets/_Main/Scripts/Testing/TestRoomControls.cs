using Deeper.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Testing
{
    /// <summary>
    /// Puts the room back to the start so the fight can be run again.
    ///
    /// TEST-ONLY — this belongs to `TestScene` and must never end up in a real room. It exists
    /// separately from <see cref="CombatRoom"/> because that is shipped content and must not read
    /// <see cref="Keyboard"/>: debug keys stay out of the player's action map, and out of the
    /// classes the game actually ships.
    ///
    /// Also supplies the room's line for <see cref="TestOverlay"/>'s readout, for the same reason
    /// that overlay exists at all — without it "is the room locked, and how many are left" is only
    /// answerable by counting bodies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestRoomControls : MonoBehaviour
    {
        [Header("Keys")]
        [Tooltip("Re-arms the room: releases every enemy it spawned, reopens both doors, back to " +
                 "Armed. RoomEntry also listens on trigger-stay, so pressing this while standing " +
                 "on the entry band restarts the fight immediately; anywhere else, walk in again.\n\n" +
                 "F12 is the last free function key — the next addition needs a modifier chord or " +
                 "a real debug menu.")]
        [SerializeField] private Key rearmKey = Key.F12;

        [Header("Refs — wired in the scene")]
        [Tooltip("The room being tested. Re-pointed by TestRoomSelector every time it swaps a " +
                 "room in, so this is a starting value rather than a fixed one.")]
        [SerializeField] private CombatRoom room;

        /// <summary>Lines for the on-screen legend, built from the same key field the input reads.</summary>
        public string[] Legend
        {
            get { return new[] { "[" + rearmKey + "] Re-arm room" }; }
        }

        /// <summary>One line for the status readout: state, wave, and how many are left.</summary>
        public string Status
        {
            get
            {
                if (room == null) return "ROOM none";

                return "ROOM " + room.State +
                       "   WAVE " + room.Wave + "/" + room.WaveCount +
                       "   LEFT " + room.EnemiesAlive;
            }
        }

        private void Awake()
        {
            if (room == null) room = FindFirstObjectByType<CombatRoom>();
        }

        /// <summary>
        /// Points these controls at a different room. Called by <see cref="TestRoomSelector"/> as it
        /// swaps rooms in, because a room is now instantiated at runtime rather than mounted in the
        /// scene — an Inspector reference cannot survive that, and searching on every frame to find
        /// the live one would hide a leaked room instead of showing it.
        /// </summary>
        public void Bind(CombatRoom target)
        {
            room = target;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (rearmKey != Key.None && keyboard[rearmKey].wasPressedThisFrame) Rearm();
        }

        /// <summary>Public so a probe can call it — simulated key presses never reach play mode
        /// (Engineering/01-VERIFICATION.md §2).</summary>
        [ContextMenu("Re-arm Room")]
        public void Rearm()
        {
            if (room != null) room.Arm();
        }
    }
}
