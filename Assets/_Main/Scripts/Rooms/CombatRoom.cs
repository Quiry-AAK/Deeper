using System;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>Where a room is in its one and only cycle.</summary>
    public enum RoomState
    {
        /// <summary>Waiting to be walked into. Doors open, nothing spawned.</summary>
        Armed,

        /// <summary>Locked, with enemies in it.</summary>
        Fighting,

        /// <summary>Everything is dead and the doors are open again.</summary>
        Cleared,
    }

    /// <summary>
    /// A room that locks you in until you have killed everything in it — CORE_SYSTEMS §8's
    /// "Combat Rooms lock entry/exit doors until all spawned enemies are defeated", and the first
    /// room type in every design doc that lists them.
    ///
    /// It owns the lifecycle and nothing else: <see cref="WaveSpawner"/> decides what the fight is,
    /// <see cref="RoomDoor"/> decides what a shut door looks like, <see cref="RoomEntry"/> decides
    /// when the player has walked in. This class only says when each of those happens.
    ///
    /// The `IsWaveRoom` flag §8 describes is a *derived* property here rather than a serialized
    /// bool. A bool sitting beside the spawner's wave array is a second source of truth that can
    /// disagree with it; asking the array how many waves it holds cannot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatRoom : MonoBehaviour
    {
        [Header("Encounter")]
        [Tooltip("The wave spawner under this room. Wired, never searched — a room that found " +
                 "somebody else's spawner would unlock on their kills.")]
        [SerializeField] private WaveSpawner encounter;

        [Header("Doors")]
        [Tooltip("Every door this room shuts. Entry AND exit both close (CORE_SYSTEMS §8) — an " +
                 "exit-only lock lets the player stroll back out of her own ambush.")]
        [SerializeField] private RoomDoor[] doors;

        /// <summary>
        /// Raised once, the frame the last enemy dies. This is the hook the floor loader will use to
        /// advance to the next room — still unbuilt, still deliberately the only line written for
        /// something that does not exist.
        /// </summary>
        public event Action Cleared;

        /// <summary>
        /// Raised on every transition, with the state just entered. Added for the Secret Vault,
        /// whose key-gated inner door has to seal when the fight starts and unseal when it ends —
        /// <see cref="Cleared"/> alone cannot express "the room just locked", and polling
        /// <see cref="State"/> every frame would hide the transition rather than report it.
        ///
        /// Separate from <see cref="Cleared"/> rather than replacing it: that one is a named
        /// contract the floor loader is written against, and collapsing the two would make every
        /// future subscriber filter for the one transition it cares about.
        /// </summary>
        public event Action<RoomState> StateChanged;

        private RoomState _state = RoomState.Armed;

        public RoomState State { get { return _state; } }

        /// <summary>CORE_SYSTEMS §8's flagged variant, read off the data instead of duplicated as a bool.</summary>
        public bool IsWaveRoom { get { return WaveCount > 1; } }

        public int WaveCount { get { return encounter != null ? encounter.WaveCount : 0; } }

        /// <summary>1-based, for a "Wave 2/3" readout.</summary>
        public int Wave { get { return encounter != null ? encounter.WaveIndex : 0; } }

        public int EnemiesAlive { get { return encounter != null ? encounter.Alive : 0; } }

        private void OnEnable()
        {
            if (encounter != null) encounter.AllWavesCleared += HandleAllWavesCleared;
        }

        private void OnDisable()
        {
            if (encounter != null) encounter.AllWavesCleared -= HandleAllWavesCleared;
        }

        private void Start()
        {
            // Start, not Awake or OnEnable: Arm reaches into the spawner, whose pools are built in
            // its own Awake, and Awake order between two objects is undefined. A room is never
            // pooled, so Start runs exactly once and this is not the OnEnable-reset case.
            Arm();
        }

        /// <summary>
        /// Empties the room, opens the doors and waits to be walked into again.
        ///
        /// Public and context-menu'd so a tester and a probe can re-run the fight — simulated key
        /// presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Arm")]
        public void Arm()
        {
            // Unconditional, in every state. Re-arming mid-fight has to take the current wave with
            // it, or those enemies stay alive in the room and their later deaths count against the
            // next fight.
            if (encounter != null) encounter.Clear();

            SetDoors(false);
            SetState(RoomState.Armed);
        }

        /// <summary>Called by <see cref="RoomEntry"/> when the player crosses into the room.</summary>
        public void PlayerEntered()
        {
            if (_state != RoomState.Armed) return;

            // State first, doors second, the fight last. Begin can raise AllWavesCleared
            // synchronously — an empty or misauthored wave array clears the moment it starts — and
            // the handler's Fighting guard has to already be satisfied when that happens, or the
            // room would lock itself and never open.
            SetState(RoomState.Fighting);
            SetDoors(true);

            if (encounter != null) encounter.Begin();
        }

        private void HandleAllWavesCleared()
        {
            if (_state != RoomState.Fighting) return;

            SetState(RoomState.Cleared);
            SetDoors(false);

            // The doors open on the killing blow, not once the bodies are gone: EnemyDeath holds a
            // corpse for its death animation before returning it to the pool, so for about half a
            // second the room is open with enemies still visibly falling over in it. That is the
            // right feel and is not a bug.
            if (Cleared != null) Cleared();
        }

        private void SetState(RoomState next)
        {
            _state = next;
            if (StateChanged != null) StateChanged(next);
        }

        private void SetDoors(bool shut)
        {
            if (doors == null) return;

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;

                if (shut) doors[i].Close();
                else doors[i].Open();
            }
        }
    }
}
