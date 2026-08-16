using System;
using System.Collections.Generic;
using Deeper.Combat;
using Deeper.Core;
using Deeper.Enemies;
using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// The fight inside a room: which enemies, how many, in how many batches, and whether they are
    /// all dead yet.
    ///
    /// One batch is a standard Combat Room. Two or three make it a Wave Room — CORE_SYSTEMS §8
    /// calls that "a variant flag on Combat Room prefabs, not a new room type", so it is authored
    /// here as one more element in <see cref="waves"/> and needs no other change anywhere.
    ///
    /// This is the production sibling of the test-only `TestSpawner`, and the thing that spawner's
    /// own `prewarm` tooltip anticipated: "a real Wave Room wants this at its batch size so a wave
    /// costs no instantiation."
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        private sealed class SpawnGroup
        {
            [Tooltip("Enemy prefab. One pool is built per distinct prefab across every wave.")]
            public GameObject prefab;

            [Tooltip("How many of it in this batch.")]
            public int count = 1;
        }

        [Serializable]
        private sealed class Wave
        {
            [Tooltip("Enemy types and counts in this batch. Group order no longer decides where " +
                     "anything starts — each spawn picks its own marker against the player's " +
                     "position and that enemy's aggro radius.")]
            public SpawnGroup[] groups;
        }

        [Header("Encounter")]
        [Tooltip("One element is a standard Combat Room; two or three make it a Wave Room " +
                 "(CORE_SYSTEMS §8) with no other change. BALANCE §8 targets 30–60s to clear a " +
                 "standard room and 60–100s for a wave room.")]
        [SerializeField] private Wave[] waves;

        [Tooltip("Spawns the next batch once this many of the previous one are still alive. " +
                 "CORE_SYSTEMS §8's '~1 remaining' — driven off Damageable.Died and never a timer, " +
                 "so a player who stops killing cannot stall the room out. 0 waits for the batch " +
                 "to be completely dead; a value at or above the batch size stacks both at once.")]
        [SerializeField] private int nextWaveAtRemaining = 1;

        [Header("Where")]
        [Tooltip("The room's authored arrival markers. Order no longer matters: each spawn takes " +
                 "the point FARTHEST from the player that is still inside that enemy's aggro " +
                 "radius, so the set can be spread to the room's edges without any of them " +
                 "becoming a dead drop.")]
        [SerializeField] private Transform[] spawnPoints;

        [Tooltip("Used when no spawn points are wired: enemies land on a ring of this radius " +
                 "around this object.")]
        [SerializeField] private float ringRadius = 5f;

        [Tooltip("Who the arrivals are placed relative to. Found by tag, the way CameraRig and " +
                 "EnemyTarget find her — a room prefab cannot hold an Inspector reference to a " +
                 "player who lives outside it.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Aggro radius used when a spawned prefab carries no EnemyDefinition to read one " +
                 "from. Only reached by something that is not a normal enemy; the real radii live " +
                 "on the definitions in Data/Enemies/.")]
        [SerializeField] private float fallbackAggroRadius = 10f;

        [Header("Arrival")]
        [Tooltip("Plays a mark at each spawn point before the enemy arrives. Leave empty to have " +
                 "enemies appear instantly, as they did before.")]
        [SerializeField] private SpawnTelegraph telegraph;

        [Tooltip("Seconds between the mark appearing and the enemy arriving. Should match the " +
                 "telegraph's own lifetime, or the enemy lands before its warning has finished.")]
        [SerializeField] private float spawnDelay = 0.5f;

        /// <summary>Raised when the last enemy of the last wave dies.</summary>
        public event Action AllWavesCleared;

        private readonly Dictionary<GameObject, ActorPool> _pools = new Dictionary<GameObject, ActorPool>();

        /// <summary>
        /// Everything this spawner has handed out and not yet seen die.
        ///
        /// Tracked as health components rather than as a count, because <see cref="Damageable.Died"/>
        /// carries no sender — identity has to be recovered from state. That turns out to be the
        /// safer shape anyway: see <see cref="Prune"/>.
        /// </summary>
        private readonly List<Damageable> _alive = new List<Damageable>();

        private int _nextWave;
        private int _placement;
        private bool _running;

        private Transform _player;

        /// <summary>
        /// Points already handed out inside the batch currently spawning.
        ///
        /// Without this the whole batch stacks on ONE marker: every enemy in a wave resolves its
        /// position on the same frame, so "farthest point still inside aggro" returns the same
        /// winner every time. Cleared per wave rather than per encounter, so a later wave is free
        /// to reuse a marker the previous one took.
        /// </summary>
        private readonly List<Transform> _takenThisWave = new List<Transform>();

        /// <summary>
        /// Enemies whose telegraph is playing but which have not arrived yet.
        ///
        /// Without this the delay would break the room outright: <see cref="CheckProgress"/> runs
        /// synchronously at the end of <see cref="SpawnNextWave"/>, and with every spawn deferred
        /// the alive list is still empty at that moment — so the room would advance the wave, or
        /// declare itself **cleared and open the doors, before a single enemy existed.**
        /// </summary>
        private int _pending;

        public int WaveCount { get { return waves != null ? waves.Length : 0; } }

        /// <summary>1-based, for a "Wave 2/3" readout. 0 before <see cref="Begin"/>.</summary>
        public int WaveIndex { get { return _nextWave; } }

        /// <summary>
        /// How many enemies the room is still waiting on. Counts those whose telegraph is playing
        /// as well as those on the floor — from the room's point of view an enemy that has been
        /// promised is already part of the fight, and a readout that showed 0 during the spawn
        /// delay would look like the room had cleared itself.
        /// </summary>
        public int Alive
        {
            get
            {
                Prune();
                return _alive.Count + _pending;
            }
        }

        private void Awake()
        {
            BuildPools();
        }

        private void OnDisable()
        {
            // Handlers do not care whether the component is enabled, so leaving subscriptions live
            // on the way out means a kill during scene teardown runs this class's logic against a
            // half-destroyed room.
            Clear();
        }

        /// <summary>
        /// Starts the encounter from wave one. Public so a probe can run the fight without physics —
        /// simulated key presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Begin")]
        public void Begin()
        {
            Clear();

            _running = true;
            _nextWave = 0;
            SpawnNextWave();
        }

        /// <summary>
        /// Empties the room and forgets everything in it. Every path that removes an enemy goes
        /// through here — that invariant is what makes <see cref="Prune"/> sufficient, because
        /// <see cref="Damageable.Died"/> only ever fires from inside <c>TakeDamage</c> and an enemy
        /// released or deactivated any other way would never fire it at all.
        /// </summary>
        [ContextMenu("Clear")]
        public void Clear()
        {
            _running = false;

            // Cancels every telegraph still counting down. Without this a re-arm mid-delay lands an
            // enemy into a room that has already reopened its doors, and the count it registers
            // belongs to a fight that no longer exists.
            StopAllCoroutines();
            _pending = 0;

            // Unsubscribed BEFORE the pools are emptied, and this order matters. Releasing does not
            // fire Died, so anything still alive would keep its subscription; the same instance
            // then comes back out of the pool on the next fight and gets a second one, and the room
            // starts counting one kill as two.
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] != null) _alive[i].Died -= HandleAnyDied;
            }

            _alive.Clear();

            foreach (KeyValuePair<GameObject, ActorPool> entry in _pools) entry.Value.ReleaseAll();

            _nextWave = 0;
            _placement = 0;
            _takenThisWave.Clear();
        }

        private void BuildPools()
        {
            if (waves == null) return;

            // Sized from the data rather than from a serialized guess: a prefab's pool holds
            // exactly as many as the whole encounter can ever have out at once, and prewarms to
            // that, so no wave pays for instantiation at the moment the frame budget is tightest.
            var totals = new Dictionary<GameObject, int>();

            foreach (Wave wave in waves)
            {
                if (wave == null || wave.groups == null) continue;

                foreach (SpawnGroup group in wave.groups)
                {
                    if (group == null || group.prefab == null) continue;

                    int running;
                    totals.TryGetValue(group.prefab, out running);
                    totals[group.prefab] = running + Mathf.Max(0, group.count);
                }
            }

            foreach (KeyValuePair<GameObject, int> entry in totals)
            {
                int size = Mathf.Max(1, entry.Value);
                _pools[entry.Key] = new ActorPool(entry.Key, transform, size, size);
            }
        }

        private void SpawnNextWave()
        {
            if (waves == null || _nextWave >= waves.Length) return;

            Wave wave = waves[_nextWave];
            _nextWave++;

            // Per wave, not per encounter: this batch spreads across the markers, and the next one
            // is free to reuse whichever of them is best placed for wherever she has moved to.
            _takenThisWave.Clear();

            if (wave != null && wave.groups != null)
            {
                foreach (SpawnGroup group in wave.groups)
                {
                    if (group == null || group.prefab == null) continue;

                    for (int i = 0; i < group.count; i++) Spawn(group.prefab);
                }
            }

            // Checked immediately, so a wave that authored nothing advances or clears instead of
            // leaving the room locked forever waiting on enemies that were never made. Bounded by
            // waves.Length, so the mutual recursion with SpawnNextWave cannot run away.
            CheckProgress();
        }

        private void Spawn(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab)) return;

            // The position is taken now, not when the enemy arrives, so the mark and the enemy
            // land on the same spawn point — resolving it later would advance the cycling index
            // out of order once several spawns are in flight at once.
            Vector3 position = NextPosition(prefab);

            if (telegraph == null || spawnDelay <= 0f)
            {
                Arrive(prefab, position);
                return;
            }

            telegraph.Play(position);

            // Counted as pending before the wait, never after: CheckProgress runs synchronously at
            // the end of SpawnNextWave, and an unregistered in-flight spawn reads as an empty room.
            _pending++;
            StartCoroutine(ArriveAfterDelay(prefab, position));
        }

        private System.Collections.IEnumerator ArriveAfterDelay(GameObject prefab, Vector3 position)
        {
            // Unscaled, to match the telegraph it is timed against: a kill landing mid-delay freezes
            // scaled time to 2%, which would hold the enemy back for real seconds after its mark
            // had already faded.
            yield return new WaitForSecondsRealtime(spawnDelay);

            _pending--;
            Arrive(prefab, position);

            // Re-checked on arrival, because the wave that was waiting on this spawn could not
            // advance while it was pending.
            CheckProgress();
        }

        private void Arrive(GameObject prefab, Vector3 position)
        {
            ActorPool pool;
            if (!_pools.TryGetValue(prefab, out pool)) return;

            GameObject actor = pool.Get(position);

            // GetComponent, not GetComponentInChildren and never RigRefs: the prefab layout pins
            // Damageable to the actor root beside its collider. A search from transform.root would
            // start at the ROOM — every pooled enemy's root is the room, not itself — and would
            // include deactivated instances parked in the pool, so it could return a sibling
            // enemy's health. That is the same bug that once made every Cave Crawler adopt the
            // contact damage of whatever spawned after it.
            Damageable health = actor.GetComponent<Damageable>();
            if (health == null)
            {
                Debug.LogError(prefab.name + " has no Damageable on its root, so the room can " +
                               "never know it died and will stay locked.", this);
                return;
            }

            health.Died += HandleAnyDied;
            _alive.Add(health);
        }

        /// <summary>
        /// Where this enemy should arrive.
        ///
        /// LEVEL_DESIGN §4 asks for arrivals "at room edges, out of the player's immediate melee
        /// range at trigger time". `EnemyTarget.Acquired` gates *all* movement and attacking on the
        /// aggro radius, so an arrival beyond it is furniture until she happens to walk over —
        /// which is why the first room's markers had to be hand-tuned inwards (brief §13.1).
        ///
        /// Both are satisfied by taking the farthest authored point that is still inside the radius:
        /// as far from her as §4 wants, and never outside the range that makes an enemy inert. It
        /// matters most for waves 2-3, which arrive while she is already somewhere unpredictable.
        /// </summary>
        private Vector3 NextPosition(GameObject prefab)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform chosen = FarthestPointInAggro(prefab);
                if (chosen != null)
                {
                    _takenThisWave.Add(chosen);
                    return chosen.position;
                }

                // Nothing in range — she is further from every marker than this enemy can perceive.
                // The old cycling order is still the best available answer, and an arrival she has
                // to walk to is better than none at all.
                Transform point = spawnPoints[_placement++ % spawnPoints.Length];
                if (point != null) return point.position;
            }

            // Golden angle, the same fallback TestSpawner uses: an even division of the circle only
            // looks right for the count it was written for.
            float angle = _placement++ * 2.39996f;
            return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
        }

        private Transform FarthestPointInAggro(GameObject prefab)
        {
            Transform player = Player;
            if (player == null) return null;

            float radius = AggroRadiusOf(prefab);

            Transform best = Farthest(player.position, radius);
            if (best != null) return best;

            // Every in-range marker is already taken by this batch. Rotating through them again
            // beats stacking the remainder on one tile, which is what returning null here would do.
            if (_takenThisWave.Count == 0) return null;

            _takenThisWave.Clear();
            return Farthest(player.position, radius);
        }

        private Transform Farthest(Vector3 from, float radius)
        {
            Transform best = null;
            float bestDistance = -1f;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform point = spawnPoints[i];
                if (point == null || _takenThisWave.Contains(point)) continue;

                float distance = Vector2.Distance(point.position, from);
                if (distance > radius || distance <= bestDistance) continue;

                best = point;
                bestDistance = distance;
            }

            return best;
        }

        private float AggroRadiusOf(GameObject prefab)
        {
            // Read off the prefab rather than the spawned instance: the position has to be decided
            // before anything is taken out of the pool, and a telegraphed arrival decides it a
            // whole spawnDelay before the enemy exists at all.
            Enemy enemy = prefab.GetComponent<Enemy>();
            EnemyDefinition definition = enemy != null ? enemy.Definition : null;

            return definition != null ? definition.AggroRadius : fallbackAggroRadius;
        }

        private Transform Player
        {
            get
            {
                // Re-found when missing rather than cached once, for the reason EnemyTarget gives:
                // in the sandbox she can be reset or respawned after the room already exists.
                if (_player == null)
                {
                    GameObject found = GameObject.FindGameObjectWithTag(playerTag);
                    _player = found != null ? found.transform : null;
                }

                return _player;
            }
        }

        private void HandleAnyDied()
        {
            Prune();
            CheckProgress();
        }

        /// <summary>
        /// Drops everything dead or destroyed off the tracked list, unsubscribing as it goes.
        ///
        /// Deliberately state-driven rather than a counter you decrement per death. Damageable.Died
        /// carries no sender, so a counter has no way to tell which enemy it was told about — and a
        /// subscription that leaked (the one real hazard of pooling) would decrement twice for one
        /// kill and open the doors a kill early. Removing by <c>IsAlive</c> is idempotent: a
        /// duplicate notification finds nothing left to remove and changes nothing.
        /// </summary>
        private void Prune()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                Damageable health = _alive[i];
                if (health != null && health.IsAlive) continue;

                if (health != null) health.Died -= HandleAnyDied;
                _alive.RemoveAt(i);
            }
        }

        private void CheckProgress()
        {
            if (!_running) return;

            // Nothing advances or clears while a telegraph is still counting down. An in-flight
            // spawn is an enemy that exists as far as the room is concerned — without this the
            // first check of a delayed wave sees an empty list and opens the doors immediately.
            if (_pending > 0) return;

            if (_alive.Count > nextWaveAtRemaining) return;

            if (_nextWave < waves.Length)
            {
                SpawnNextWave();
                return;
            }

            if (_alive.Count > 0) return;

            // Latched off before the event, so a kill that somehow arrives afterwards — a leaked
            // subscription, or an enemy the test harness spawned into the same room — cannot raise
            // a second clear on a room that has already opened its doors.
            _running = false;
            if (AllWavesCleared != null) AllWavesCleared();
        }

        private void OnDrawGizmosSelected()
        {
            // Spawn markers are empty transforms and therefore invisible, which turns "why did it
            // appear there" into a guessing game. Same reason TestSpawner draws its ring.
            Gizmos.color = new Color(0.9f, 0.5f, 0.2f, 0.7f);

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Gizmos.DrawWireSphere(transform.position, ringRadius);
                return;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null) Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
            }
        }
    }
}
