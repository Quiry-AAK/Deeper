using System.Collections.Generic;
using UnityEngine;
using Deeper.Rooms;

namespace Deeper.Run
{
    /// <summary>
    /// CORE_SYSTEMS §8's reshuffling bag: "the pool shuffles, is drawn through without immediate
    /// repeats, and reshuffles once exhausted."
    ///
    /// A bag rather than an independent random pick per room, because independent picks cluster —
    /// three identical rooms in a row is unremarkable to a coin and infuriating to a player. Drawing
    /// through a shuffled pool guarantees the whole pool is seen before anything repeats.
    ///
    /// Plain C#, not a MonoBehaviour: it owns no scene state and its correctness is exactly the kind
    /// of thing worth being able to exercise without an editor.
    /// </summary>
    public sealed class RoomBag
    {
        private readonly System.Random _random;
        private readonly List<RoomOption> _remaining = new List<RoomOption>();
        private IList<RoomOption> _source;
        private RoomOption _last;

        public RoomBag(System.Random random)
        {
            _random = random;
        }

        /// <summary>How many draws are left before the bag reshuffles.</summary>
        public int Remaining { get { return _remaining.Count; } }

        public void Fill(IList<RoomOption> options)
        {
            _source = options;
            _last = null;
            Reshuffle();
        }

        /// <summary>
        /// The next room.
        ///
        /// <paramref name="needsExit"/> is a plain bool rather than a predicate because it is not a
        /// design rule at all — it is the geometric fact that a room with no east door cannot be
        /// walked out of, so a floor with rooms still to come must not draw one. Every *design* rule
        /// about ordering lives in <see cref="FloorLoader"/>, where it is readable as such.
        /// </summary>
        public RoomOption Draw(bool needsExit)
        {
            if (_source == null || _source.Count == 0)
            {
                Debug.LogError("RoomBag drawn from an empty pool.");
                return null;
            }

            // Two passes: the first refuses an immediate repeat, the second allows it. A pool with
            // one usable layout would otherwise never return anything, and §8 asks for "without
            // immediate repeats" as a preference of the shuffle, not a guarantee it can't honour.
            for (int pass = 0; pass < 2; pass++)
            {
                bool allowRepeat = pass == 1;

                for (int refill = 0; refill < 2; refill++)
                {
                    if (_remaining.Count == 0) Reshuffle();

                    for (int i = 0; i < _remaining.Count; i++)
                    {
                        RoomOption candidate = _remaining[i];
                        if (candidate == null || candidate.prefab == null) continue;
                        if (!allowRepeat && candidate == _last) continue;
                        if (needsExit && !HasExit(candidate)) continue;

                        _remaining.RemoveAt(i);
                        _last = candidate;
                        return candidate;
                    }

                    // Nothing in what is left fits; empty the bag so the refill above reshuffles.
                    _remaining.Clear();
                }
            }

            Debug.LogError(needsExit
                ? "RoomBag has no layout with an east door, so the floor cannot continue. Every " +
                  "non-final room needs one; the Secret Vault is authored as a dead end."
                : "RoomBag has no usable layout — every option is missing its prefab.");
            return null;
        }

        private void Reshuffle()
        {
            _remaining.Clear();
            foreach (RoomOption option in _source) _remaining.Add(option);

            // Fisher-Yates against the run's stream, so a logged seed reproduces the floor order.
            for (int i = _remaining.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                RoomOption swap = _remaining[i];
                _remaining[i] = _remaining[j];
                _remaining[j] = swap;
            }
        }

        private static bool HasExit(RoomOption option)
        {
            RoomConnection connection = option.prefab.GetComponent<RoomConnection>();
            return connection != null && connection.HasExit;
        }
    }
}
