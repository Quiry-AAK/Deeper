using System;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// The keys this run is carrying — CORE_SYSTEMS §8's "a `SecretKey` flag on the player's
    /// inventory", granted by defeating a rare elite earlier in the biome.
    ///
    /// **Deliberately not called Inventory.** The engineering plan sketched
    /// `Scripts/Player/Inventory.cs`, but the inventory system was deleted at the owner's direction
    /// in favour of the locked design (one weapon, no carried list), and reviving that name would
    /// read as it coming back. This holds one count and nothing else; a second key type gets its own
    /// field here, not a bag.
    ///
    /// Run-scoped like <c>RunUpgrades</c>: reset in <c>OnEnable</c> so a scene reload starts a fresh
    /// descent rather than inheriting the last one's keys.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunKeys : MonoBehaviour
    {
        [Tooltip("Secret Vault keys held at the start of a run. Normally 0 — the Deep Warden drops " +
                 "them (KeyReward). Non-zero is a testing shortcut for reaching a vault without " +
                 "fighting an elite first.")]
        [SerializeField] private int startingSecretKeys;

        /// <summary>Raised whenever the count moves, for the HUD and the test overlay.</summary>
        public event Action Changed;

        public int SecretKeys { get; private set; }

        public bool HasSecretKey { get { return SecretKeys > 0; } }

        private void OnEnable()
        {
            SecretKeys = startingSecretKeys;
            if (Changed != null) Changed();
        }

        /// <summary>
        /// Grants keys. Public and context-menu'd because a probe drives it directly — simulated key
        /// presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Grant Secret Key")]
        public void GrantSecretKey()
        {
            GrantSecretKeys(1);
        }

        public void GrantSecretKeys(int count)
        {
            if (count <= 0) return;

            SecretKeys += count;
            if (Changed != null) Changed();
        }

        /// <summary>
        /// Spends one key if there is one. Returns whether it was spent, so the caller can tell
        /// "opened it" from "walked into a locked door" — a void Spend() would make a vault door
        /// with no key look identical to one that consumed nothing.
        /// </summary>
        public bool TryConsumeSecretKey()
        {
            if (SecretKeys <= 0) return false;

            SecretKeys--;
            if (Changed != null) Changed();
            return true;
        }
    }
}
