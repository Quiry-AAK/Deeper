using System;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// One batch of arrivals. One wave is a standard Combat Room; two or three make it a Wave Room
    /// (CORE_SYSTEMS §8), which is why nothing anywhere stores an "is wave room" bool —
    /// <see cref="CombatRoom.IsWaveRoom"/> derives it from the count.
    ///
    /// Extracted from <see cref="WaveSpawner"/> alongside <see cref="SpawnGroup"/>; the same
    /// serialisation note applies, and the reason is written there.
    /// </summary>
    [Serializable]
    public sealed class Wave
    {
        [Tooltip("Enemy types and counts in this batch. Group order no longer decides where " +
                 "anything starts — each spawn picks its own marker against the player's " +
                 "position and that enemy's aggro radius.")]
        public SpawnGroup[] groups;
    }
}
