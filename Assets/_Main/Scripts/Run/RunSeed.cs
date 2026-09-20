using UnityEngine;

namespace Deeper.Run
{
    /// <summary>
    /// The run's one source of randomness.
    ///
    /// Deliberately not <see cref="UnityEngine.Random"/>: that is global process state shared with
    /// VFX, <see cref="Deeper.Rooms.SpawnTelegraph"/> and anything else that draws from it, so a
    /// seed set there is not a seed, it is a suggestion.
    ///
    /// **This is not the "seeded runs" feature.** MVP §Post-MVP lists daily/weekly seeded runs and
    /// ghost replay as out of scope, and this is not them: there is no seed UI, nothing saved, and
    /// nothing shown to a player. It is a number logged at the start of a run so that a floor-order
    /// bug can be reproduced by typing it back into the Inspector, which is the difference between
    /// a bug you can fix and one you can only describe.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunSeed : MonoBehaviour
    {
        [Tooltip("0 picks an arbitrary seed each run. Any other value reproduces that run's room " +
                 "order and dressing exactly — set it to a logged seed to chase a bug.")]
        [SerializeField] private int seed;

        [Tooltip("Logs the seed at the start of every run. Leave on: a seed nobody wrote down is " +
                 "the same as no seed.")]
        [SerializeField] private bool logSeed = true;

        private System.Random _selection;
        private int _current;

        /// <summary>The seed actually in use, which differs from the field when it is 0.</summary>
        public int Seed { get { return _current; } }

        /// <summary>
        /// The run's stream: room order, how many rooms a floor has, which encounter each draws.
        ///
        /// Dressing does NOT come from here directly — each room gets its own stream seeded from a
        /// single <c>Next()</c> of this one. That costs four lines and prevents the specific bug
        /// that makes seeded repro worthless in practice: changing how much a room scatters would
        /// otherwise consume a different number of draws and reshuffle the whole floor.
        /// </summary>
        public System.Random Selection
        {
            get
            {
                if (_selection == null) NewRun();
                return _selection;
            }
        }

        [ContextMenu("New Run")]
        public void NewRun()
        {
            _current = seed != 0 ? seed : System.Environment.TickCount & 0x7FFFFFFF;
            _selection = new System.Random(_current);

            if (logSeed) Debug.Log("Run seed " + _current, this);
        }
    }
}
