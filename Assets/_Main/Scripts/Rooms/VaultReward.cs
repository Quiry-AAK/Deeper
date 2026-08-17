using System;
using Deeper.Character;
using Deeper.Upgrades;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// What a Secret Vault pays out — CORE_SYSTEMS §8's "guaranteed Legendary-tier upgrade offer",
    /// which is the equipped weapon's Relic (CONTENT_DESIGN §4: one per weapon, only offered when
    /// that weapon is equipped, never in a normal draw).
    ///
    /// **It grants rather than offers, and that is a recorded divergence** (change brief). The
    /// three-card offer panel is Milestone 4 and does not exist; with exactly one guaranteed
    /// Legendary there is nothing to choose between, so a one-card panel would be ceremony around a
    /// grant. <see cref="Granted"/> is the seam the real offer takes over — it fires with the
    /// upgrade, so a panel can present it instead of this handing it straight to the run.
    ///
    /// The vault never names a relic: it asks the run's weapon for its own. That is why adding the
    /// Bow's Deadeye's Promise later needs no change here — the same rule ChargeSpec and
    /// UltimateBuffSpec already follow, where per-weapon behaviour is data on the weapon asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VaultReward : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("The fight guarding the payout. Wired, never searched — a vault that found " +
                 "somebody else's room would pay out on their kills.")]
        [SerializeField] private CombatRoom room;

        [Tooltip("How the player is found. Room code cannot use RigRefs.Find, which searches from " +
                 "transform.root and would resolve against the room itself.")]
        [SerializeField] private string playerTag = "Player";

        /// <summary>
        /// Raised once with what was paid out. Milestone 4's offer panel attaches here; nothing
        /// else does yet, which makes this the same shape as <c>CombatRoom.Cleared</c>.
        /// </summary>
        public event Action<UpgradeDefinition> Granted;

        private bool _paid;

        /// <summary>Whether this vault has already paid out in the current cycle.</summary>
        public bool HasPaid { get { return _paid; } }

        /// <summary>
        /// What this vault would pay the player standing in it right now, or null when she carries
        /// no weapon or the weapon has no relic authored. Read by the test overlay so "what is in
        /// here" is answerable before opening it.
        /// </summary>
        public UpgradeDefinition Payout
        {
            get
            {
                RunLoadout loadout = FindLoadout();
                if (loadout == null || loadout.Weapon == null) return null;

                return loadout.Weapon.Relic.Offer;
            }
        }

        private void OnEnable()
        {
            _paid = false;

            if (room != null) room.StateChanged += HandleRoomState;
        }

        private void OnDisable()
        {
            if (room != null) room.StateChanged -= HandleRoomState;
        }

        /// <summary>
        /// Hands over the weapon's Relic. Public and context-menu'd so a probe can drive it —
        /// simulated key presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// Returns whether anything actually changed hands.
        /// </summary>
        [ContextMenu("Grant Payout")]
        public bool Grant()
        {
            if (_paid) return false;

            UpgradeDefinition offer = Payout;
            if (offer == null)
            {
                // Loud, because a vault that silently pays nothing is indistinguishable from one
                // that has not been reached yet — and the most likely cause is an unauthored relic
                // on a weapon asset, which no compile error can catch.
                Debug.LogWarning(name + ": no relic authored on the run's weapon; vault paid nothing.", this);
                return false;
            }

            RunUpgrades upgrades = FindUpgrades();
            if (upgrades == null) return false;

            // Add refuses duplicates, so a second vault in one run pays nothing rather than
            // silently re-applying the same relic. That is the run's rule, not the vault's.
            if (!upgrades.Add(offer)) return false;

            _paid = true;
            if (Granted != null) Granted(offer);
            return true;
        }

        private void HandleRoomState(RoomState state)
        {
            switch (state)
            {
                // Re-arming the room re-arms the prize, so the sandbox can run the vault twice.
                case RoomState.Armed:
                    _paid = false;
                    break;

                case RoomState.Cleared:
                    Grant();
                    break;

                default:
                    break;
            }
        }

        private RunLoadout FindLoadout()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            return player != null ? player.GetComponentInChildren<RunLoadout>(true) : null;
        }

        private RunUpgrades FindUpgrades()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            return player != null ? player.GetComponentInChildren<RunUpgrades>(true) : null;
        }
    }
}
