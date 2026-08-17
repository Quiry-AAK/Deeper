using System;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// The lock on a Secret Vault's inner door — CORE_SYSTEMS §8's "a locked door prefab requires a
    /// `SecretKey` flag on the player's inventory".
    ///
    /// It does not draw or block anything itself: <see cref="RoomDoor"/> is still what a shut door
    /// *is*, and this only decides when that door opens. Same split as <see cref="CombatRoom"/> and
    /// its doors, one level down — a second door class that re-implemented the collider and sprite
    /// toggle would be two answers to "what does shut look like".
    ///
    /// **This object is the key volume, not the door.** It is a child of the door so it can carry a
    /// trigger collider on layer 8 `RoomTrigger` while the door keeps its solid barrier on Default.
    /// Putting a trigger on the door object itself would put it on Default, where
    /// `ThrownRock.blockingLayers` would despawn every rock thrown through the doorway.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class VaultDoor : MonoBehaviour
    {
        [Tooltip("Who can unlock it. Layer 6 (Player), the same filter RoomEntry uses.")]
        [SerializeField] private LayerMask playerLayers = 1 << 6;

        [Tooltip("Whether opening spends the key. INVENTED — CORE_SYSTEMS §8 says the door " +
                 "'requires a SecretKey flag' and never says it is consumed, but a flag that " +
                 "survives opens every later vault for free, which deletes the elite's reason to " +
                 "exist. Serialized so the designer's answer is a checkbox, not a recompile.")]
        [SerializeField] private bool consumeKey = true;

        [Header("Refs — on the door above this volume")]
        [Tooltip("The door this lock opens. Wired, never searched across the room — a lock that " +
                 "found the wrong door would open the far side of the vault.")]
        [SerializeField] private RoomDoor door;

        [Tooltip("The room whose fight seals this door. Optional: without it the door simply stays " +
                 "open once unlocked.")]
        [SerializeField] private CombatRoom room;

        /// <summary>Raised the moment the key turns, for the payout and the test overlay.</summary>
        public event Action Unlocked;

        private bool _unlocked;

        public bool IsUnlocked { get { return _unlocked; } }

        private void Awake()
        {
            // GetComponentInParent, not RigRefs.Find: room code must never search from
            // transform.root, which for anything under a spawner is the spawner itself.
            if (door == null) door = GetComponentInParent<RoomDoor>();
            if (room == null) room = GetComponentInParent<CombatRoom>();
        }

        private void OnEnable()
        {
            Relock();

            if (room != null) room.StateChanged += HandleRoomState;
        }

        private void OnDisable()
        {
            if (room != null) room.StateChanged -= HandleRoomState;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryUnlock(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Stay as well as Enter, for RoomEntry's reason: re-arming the vault while she is
            // standing in the doorway would otherwise leave it locked until she walked away and
            // back, which is exactly the state you are in while tuning the lock.
            TryUnlock(other);
        }

        /// <summary>
        /// Puts the lock back on and shuts the door. Called when the room re-arms, so a re-run of
        /// the vault costs another key rather than standing open from the previous run.
        /// </summary>
        [ContextMenu("Relock")]
        public void Relock()
        {
            _unlocked = false;
            if (door != null) door.Close();
        }

        /// <summary>
        /// Opens it without a key. Test-only seam — the harness needs to reach the vault chamber
        /// while the key drop is being verified separately.
        /// </summary>
        [ContextMenu("Force Unlock")]
        public void ForceUnlock()
        {
            if (_unlocked) return;

            _unlocked = true;
            if (door != null) door.Open();
            if (Unlocked != null) Unlocked();
        }

        private void TryUnlock(Collider2D other)
        {
            if (_unlocked || other == null) return;
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            // GetComponentInParent: the whole player rig is on layer 6, so the collider that trips
            // this can be her reticle or her hitbox rather than the body carrying the keys.
            RunKeys keys = other.GetComponentInParent<RunKeys>();
            if (keys == null) return;

            if (consumeKey)
            {
                if (!keys.TryConsumeSecretKey()) return;
            }
            else if (!keys.HasSecretKey)
            {
                return;
            }

            ForceUnlock();
        }

        private void HandleRoomState(RoomState state)
        {
            switch (state)
            {
                // A switch with every case spelled out, never a ternary: a fourth RoomState would
                // silently inherit the last branch, which is the bug that gave the Dash Attack the
                // Ultimate's numbers in five places at once.
                case RoomState.Armed:
                    Relock();
                    break;

                // Sealed for the fight. This is the whole reason the vault's guards cannot be kited
                // back into the antechamber: EnemyChase has no pathfinding, so an enemy following
                // her through a 1-wide doorway jams on the interior wall and the room never unlocks.
                case RoomState.Fighting:
                    if (door != null) door.Close();
                    break;

                // Open again on the clear so she can walk back out. Guarded on _unlocked only
                // because a door that was never opened has nothing to reopen.
                case RoomState.Cleared:
                    if (_unlocked && door != null) door.Open();
                    break;

                default:
                    break;
            }
        }
    }
}
