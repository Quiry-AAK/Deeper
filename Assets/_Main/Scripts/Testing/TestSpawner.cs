using System.Collections.Generic;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Testing
{
    /// <summary>
    /// Puts test actors in the room on a key press, and takes them back out.
    ///
    /// TEST-ONLY — this belongs to `TestScene` and must never end up in a real room. One spawner
    /// per prefab: the training dummy has one, and each enemy has its own object with its own keys.
    /// That is deliberately not one spawner with a list of prefabs — the point of a sandbox is being
    /// able to say "three crawlers and nothing else" without editing a list mid-session, and
    /// per-prefab objects make that a checkbox in the Hierarchy.
    ///
    /// Actors come from an <see cref="ActorPool"/> rather than <c>Instantiate</c>/<c>Destroy</c>, so
    /// a long tuning session that spawns and kills hundreds of enemies reuses a handful of rigs
    /// instead of handing the garbage collector a steady drip of dead ones. One pool per spawner
    /// falls out of the one-spawner-per-prefab rule for free.
    ///
    /// Keys are read straight off <see cref="Keyboard"/> rather than through the project's
    /// `.inputactions` asset, on purpose: debug keys must not appear in the player's action map,
    /// which is shipped content and is where the rebinding UI will read from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestSpawner : MonoBehaviour
    {
        [Header("What")]
        [Tooltip("Prefab to spawn. Any actor prefab works — the dummy now, real enemies later.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Name used in the on-screen legend and status line. Plural reads better in both: " +
                 "'Dummies' gives '[F4] Spawn Dummies' and 'Dummies 3'.")]
        [SerializeField] private string displayName = "Dummies";

        [Header("Where")]
        [Tooltip("Spawn positions, used in order and then cycled. Leave empty to spawn on a ring " +
                 "around this object instead.")]
        [SerializeField] private Transform[] spawnPoints;

        [Tooltip("Ring radius used when no spawn points are wired. Keep it clear of the player's " +
                 "start, or a spawn lands on top of her and contact damage starts immediately.")]
        [SerializeField] private float ringRadius = 3.5f;

        [Header("How many")]
        [Tooltip("Spawned when Play starts. 0 leaves the room exactly as authored.")]
        [SerializeField] private int spawnOnStart;

        [Tooltip("Spawned per key press.")]
        [SerializeField] private int perPress = 1;

        [Tooltip("Refuses to spawn past this many alive. A held key would otherwise flood the " +
                 "room with hundreds of colliders and make the frame time — not the feel — the " +
                 "thing being measured.")]
        [SerializeField] private int maxAlive = 20;

        [Tooltip("Instances built up front and parked in the pool. 0 is right for a sandbox, where " +
                 "the first spawn is never the frame anyone is measuring; a real Wave Room wants " +
                 "this at its batch size so a wave costs no instantiation.")]
        [SerializeField] private int prewarm;

        [Header("Keys")]
        [SerializeField] private Key spawnKey = Key.F4;
        [SerializeField] private Key clearKey = Key.F5;

        private ActorPool _pool;

        /// <summary>
        /// Actors authored under this spawner in the scene. Tracked separately from the pool because
        /// they were never taken from it and must not be handed back to it.
        /// </summary>
        private readonly List<GameObject> _adopted = new List<GameObject>();

        private int _placement;

        /// <summary>How many of this spawner's actors are currently in the room.</summary>
        public int Alive
        {
            get
            {
                Prune();
                return (_pool != null ? _pool.Live : 0) + _adopted.Count;
            }
        }

        public string DisplayName { get { return displayName; } }

        /// <summary>Lines for the on-screen legend, built from the same key fields the input uses.</summary>
        public string[] Legend
        {
            get
            {
                return new[]
                {
                    "[" + spawnKey + "] Spawn " + displayName,
                    "[" + clearKey + "] Clear " + displayName,
                };
            }
        }

        private void Awake()
        {
            if (prefab == null) return;

            // Parented to this spawner so the Hierarchy shows who owns what, and so parked
            // instances sit under the object that will hand them out again.
            _pool = new ActorPool(prefab, transform, prewarm, Mathf.Max(1, maxAlive));
        }

        private void Start()
        {
            // Actors authored under this spawner in the scene count as its own. Without this the
            // panel would read "Dummy 0" with three of them standing in the room, and Clear would
            // leave exactly the ones the tester was looking at. Selected by Damageable so a spawn
            // point parked under here as a child marker is not mistaken for an actor.
            //
            // **Active children only.** Prewarmed pool instances are also children with a
            // Damageable, and they are sitting switched off waiting to be handed out — counting
            // those would report a room full of enemies that nobody can see.
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (!child.activeSelf) continue;
                if (child.GetComponent<Damageable>() != null) _adopted.Add(child);
            }

            if (spawnOnStart > 0) Spawn(spawnOnStart);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (spawnKey != Key.None && keyboard[spawnKey].wasPressedThisFrame) Spawn();
            if (clearKey != Key.None && keyboard[clearKey].wasPressedThisFrame) Clear();
        }

        /// <summary>Spawns <see cref="perPress"/> actors. Public so a probe can call it — simulated
        /// key presses never reach play mode (see Engineering/01-VERIFICATION.md §2).</summary>
        [ContextMenu("Spawn")]
        public void Spawn()
        {
            Spawn(perPress);
        }

        public void Spawn(int count)
        {
            if (prefab == null)
            {
                Debug.LogWarning(name + " has no prefab to spawn.", this);
                return;
            }

            for (int i = 0; i < count && Alive < maxAlive; i++)
            {
                _pool.Get(NextPosition());
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            // Pooled actors go back rather than dying, so Clear costs nothing and the next Spawn is
            // free. Adopted ones were never in the pool, so they are destroyed as before.
            if (_pool != null) _pool.ReleaseAll();

            for (int i = 0; i < _adopted.Count; i++)
            {
                if (_adopted[i] != null) Destroy(_adopted[i]);
            }

            _adopted.Clear();
        }

        private Vector3 NextPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[_placement++ % spawnPoints.Length];
                if (point != null) return point.position;
            }

            // Golden angle rather than an even division, so the ring stays spread out no matter how
            // many get spawned — an even division only looks right for the count it was written for.
            float angle = _placement++ * 2.39996f;
            return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
        }

        private void Prune()
        {
            for (int i = _adopted.Count - 1; i >= 0; i--)
            {
                if (_adopted[i] == null) _adopted.RemoveAt(i);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // The ring is invisible until something spawns on it, which makes "why did that land on
            // the player" a guessing game. Drawn only when selected so it never clutters the view.
            if (spawnPoints != null && spawnPoints.Length > 0) return;

            Gizmos.color = new Color(0.9f, 0.5f, 0.2f, 0.6f);
            Vector3 previous = transform.position + Vector3.right * ringRadius;
            for (int i = 1; i <= 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ringRadius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
