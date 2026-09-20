using System;
using UnityEngine;

namespace Deeper.Meta
{
    /// <summary>
    /// How many Shards the player has — the permanent currency spent in the Hub (GDD §Progression).
    ///
    /// **This is a seam, not a save file, and the difference matters.** Milestone 6 owns
    /// `Scripts/Meta/SaveData.cs`, which will hold Shards alongside Hub stat ranks, Weapon Mastery
    /// counters and discovered Relics, and will actually persist. This holds one number so the Hub
    /// has something real to read, and so the run-end award and the shrine's purchases each have one
    /// obvious place to call when they are written. When `SaveData` arrives it should back this,
    /// not replace it — every caller already speaks to <see cref="Add"/> and <see cref="TrySpend"/>.
    ///
    /// **Nothing awards Shards yet.** GDD §Currency and BALANCE §14 compute them once at run end
    /// from Levels Gained and Depth Reached; there is no run end, so the balance only moves if
    /// something calls in or you type a number into the asset. That is why <see cref="Grant"/>
    /// exists as a context menu — the counter can be seen working without a run to finish.
    ///
    /// Same editor/build behaviour as <see cref="Deeper.Run.RunConfig"/>, relied on for the same
    /// reason: a runtime write to a ScriptableObject sticks for the editor session and is discarded
    /// in a build. Convenient while building the Hub, and honest about being temporary — a player
    /// who closes a build and reopens it is *meant* to keep their Shards, and this would not do it.
    /// </summary>
    [CreateAssetMenu(fileName = "ShardBank", menuName = "Deeper/Meta/Shard Bank", order = 11)]
    public sealed class ShardBank : ScriptableObject
    {
        [Tooltip("Shards held. Authored here until a run end exists to award them.")]
        [SerializeField] private int balance;

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        public int Balance { get { return balance; } }

        /// <summary>
        /// Pays Shards in. The seam the run-end award will call once BALANCE §14's formula has
        /// somewhere to run.
        /// </summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;

            balance += amount;
            Raise();
        }

        /// <summary>
        /// Takes Shards out, refusing rather than going negative — a purchase that cannot be
        /// afforded must fail loudly at the till, not leave a debt nothing in the design describes.
        /// Returns whether it went through.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > balance) return false;

            balance -= amount;
            Raise();
            return true;
        }

        [ContextMenu("Grant 250 Shards")]
        private void Grant()
        {
            Add(250);
            Debug.Log("ShardBank: balance is now " + balance + ".", this);
        }

        private void Raise()
        {
            if (BalanceChanged != null) BalanceChanged(balance);
        }
    }
}
