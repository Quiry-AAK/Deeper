using Deeper.Player;
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

        private VaultDoor _vault;
        private VaultReward _reward;
        private RunKeys _keys;

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

                string line = "ROOM " + room.State +
                              "   WAVE " + room.Wave + "/" + room.WaveCount +
                              "   LEFT " + room.EnemiesAlive;

                // Only inside a vault, for the reason the wave counter is hidden outside a Wave
                // Room: a readout that always shows every system's state is one nobody reads.
                if (_vault != null)
                {
                    line += "   KEYS " + (_keys != null ? _keys.SecretKeys.ToString() : "?") +
                            "   VAULT " + (_vault.IsUnlocked ? "OPEN" : "LOCKED");
                }

                if (_reward != null)
                {
                    line += "   PAYOUT " + (_reward.HasPaid
                        ? "taken"
                        : (_reward.Payout != null ? _reward.Payout.DisplayName : "none authored"));
                }

                return line;
            }
        }

        private void Awake()
        {
            if (room == null) room = FindFirstObjectByType<CombatRoom>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _keys = player.GetComponentInChildren<RunKeys>(true);

            BindVault();
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
            BindVault();
        }

        /// <summary>
        /// Picks up the vault parts of whichever room just loaded, and drops them for a room that
        /// has none. Searched rather than wired because the room is instantiated at runtime — the
        /// same reason <see cref="Bind"/> exists at all.
        /// </summary>
        private void BindVault()
        {
            _vault = room != null ? room.GetComponentInChildren<VaultDoor>(true) : null;
            _reward = room != null ? room.GetComponentInChildren<VaultReward>(true) : null;
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
