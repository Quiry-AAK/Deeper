using UnityEngine;
using Deeper.Enemies;

namespace Deeper.Rooms
{
    /// <summary>
    /// One authored fight: which enemies arrive, how many, and in how many batches.
    ///
    /// This exists so a room's fight is *content* rather than prefab data. A layout used to carry
    /// exactly one encounter baked into its prefab, which meant the same room played identically
    /// every time it was drawn; a <see cref="Deeper.Run.FloorLoader"/> now picks one of these per
    /// mount, so six layouts and three encounters each read as eighteen fights.
    ///
    /// Every number about an encounter is DERIVED here, never typed. A serialized "HP budget" field
    /// beside the wave array is a second source of truth that can disagree with it — the exact trap
    /// <see cref="CombatRoom.IsWaveRoom"/> avoids by deriving from the wave count instead of storing
    /// a flag.
    /// </summary>
    [CreateAssetMenu(fileName = "Encounter_", menuName = "Deeper/Rooms/Encounter", order = 3)]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [Tooltip("One element is a standard Combat Room; two or three make it a Wave Room " +
                 "(CORE_SYSTEMS §8). BALANCE §8 targets 30–60s to clear a standard room and " +
                 "60–100s for a wave room.")]
        [SerializeField] private Wave[] waves;

        /// <summary>
        /// The authored batches. Handed to <see cref="WaveSpawner.SetEncounter"/> by reference and
        /// never mutated — the spawner only ever reads it, so every room drawing this encounter
        /// shares one array instead of copying it per mount.
        /// </summary>
        public Wave[] Waves { get { return waves; } }

        public int WaveCount { get { return waves != null ? waves.Length : 0; } }

        /// <summary>Summed <see cref="EnemyDefinition.MaxHealth"/> of everything this spawns.</summary>
        public float TotalHealth
        {
            get
            {
                float total = 0f;
                ForEachGroup(delegate(SpawnGroup group, EnemyDefinition definition)
                {
                    if (definition != null) total += definition.MaxHealth * Mathf.Max(0, group.count);
                });
                return total;
            }
        }

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                ForEachGroup(delegate(SpawnGroup group, EnemyDefinition definition)
                {
                    total += Mathf.Max(0, group.count);
                });
                return total;
            }
        }

        /// <summary>
        /// The biggest single batch. Not the same as "most enemies on screen at once", which also
        /// depends on <c>WaveSpawner.nextWaveAtRemaining</c> — a spawner-side pacing threshold this
        /// asset cannot see. Named for what it actually measures rather than what it approximates.
        /// </summary>
        public int LargestWave
        {
            get
            {
                int largest = 0;
                if (waves == null) return 0;

                foreach (Wave wave in waves)
                {
                    if (wave == null || wave.groups == null) continue;

                    int size = 0;
                    foreach (SpawnGroup group in wave.groups)
                    {
                        if (group != null) size += Mathf.Max(0, group.count);
                    }

                    if (size > largest) largest = size;
                }

                return largest;
            }
        }

        /// <summary>
        /// On demand rather than in <c>OnValidate</c>: this logs, and a log that fires on every
        /// keystroke in the Inspector is one nobody reads.
        /// </summary>
        [ContextMenu("Log Budget")]
        public void LogBudget()
        {
            int unresolved = 0;
            ForEachGroup(delegate(SpawnGroup group, EnemyDefinition definition)
            {
                if (definition == null) unresolved++;
            });

            Debug.Log(string.Format(
                "{0}: {1} waves, {2} enemies, {3} HP, largest wave {4}{5}",
                name, WaveCount, TotalEnemies, TotalHealth, LargestWave,
                unresolved > 0 ? "  — " + unresolved + " group(s) have no EnemyDefinition" : ""), this);
        }

        /// <summary>
        /// Reads each group's stats off the prefab asset itself. <c>GetComponent</c> on a prefab root
        /// resolves that prefab's own serialized references, which is the same thing
        /// <c>WaveSpawner.AggroRadiusOf</c> already relies on to place a spawn.
        /// </summary>
        private void ForEachGroup(System.Action<SpawnGroup, EnemyDefinition> visit)
        {
            if (waves == null) return;

            foreach (Wave wave in waves)
            {
                if (wave == null || wave.groups == null) continue;

                foreach (SpawnGroup group in wave.groups)
                {
                    if (group == null || group.prefab == null) continue;

                    Enemy enemy = group.prefab.GetComponent<Enemy>();
                    visit(group, enemy != null ? enemy.Definition : null);
                }
            }
        }
    }
}
