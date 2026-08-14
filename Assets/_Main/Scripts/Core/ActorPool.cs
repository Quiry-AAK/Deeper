using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Deeper.Core
{
    /// <summary>
    /// Reuses instances of one prefab instead of instantiating and destroying them.
    ///
    /// A Wave Room spawns enemies in batches for a whole floor and the Rock Slinger throws a rock
    /// every couple of seconds; doing that with <c>Instantiate</c>/<c>Destroy</c> allocates a rig
    /// and its components every time and hands the garbage collector a steady drip of dead
    /// GameObjects. In a game whose whole feel layer is measured in hitstop frames, a GC spike lands
    /// exactly where it is most visible.
    ///
    /// **One pool per prefab, owned by whatever spawns it** — the same shape `TestSpawner` already
    /// uses. There is deliberately no global registry: a pool nobody owns is a pool nobody clears.
    ///
    /// Built on <see cref="ObjectPool{T}"/> rather than a hand-rolled stack, per CLAUDE.md's "prefer
    /// the obvious Unity shape".
    ///
    /// **Reuse is not a fresh Instantiate.** A recycled instance never runs <c>Awake</c> again, so
    /// anything with state must reset it in <c>OnEnable</c> — releasing deactivates and getting
    /// reactivates, which is exactly what makes <c>OnEnable</c> the right hook.
    /// </summary>
    public sealed class ActorPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<GameObject> _pool;

        /// <summary>Instances currently out on loan. Iterated by <see cref="ReleaseAll"/>.</summary>
        private readonly List<GameObject> _live = new List<GameObject>();

        public ActorPool(GameObject prefab, Transform parent, int prewarm = 0, int maxSize = 64)
        {
            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<GameObject>(
                Create,
                actionOnGet: instance => instance.SetActive(true),
                actionOnRelease: instance => instance.SetActive(false),
                actionOnDestroy: instance => { if (instance != null) Object.Destroy(instance); },
                // Off: it throws when the same instance is released twice, and a dead enemy whose
                // death coroutine and whose Clear() both fire is an ordinary race, not a defect.
                collectionCheck: false,
                defaultCapacity: Mathf.Max(1, prewarm),
                maxSize: Mathf.Max(1, maxSize));

            Prewarm(prewarm);
        }

        /// <summary>How many instances are currently active in the scene.</summary>
        public int Live { get { return _live.Count; } }

        private GameObject Create()
        {
            GameObject instance = Object.Instantiate(_prefab, _parent);
            instance.name = _prefab.name + " (pooled)";

            // Bound here because the pool is the only thing that knows which pool this belongs to.
            PooledActor actor = instance.GetComponent<PooledActor>();
            if (actor == null) actor = instance.AddComponent<PooledActor>();
            actor.Bind(this);

            return instance;
        }

        private void Prewarm(int count)
        {
            if (count <= 0) return;

            // Built up front and put straight back, so the first wave of a fight does not pay for
            // instantiation at the exact moment the frame budget is tightest.
            var warmed = new GameObject[count];
            for (int i = 0; i < count; i++) warmed[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(warmed[i]);
        }

        public GameObject Get(Vector3 position)
        {
            GameObject instance = _pool.Get();

            // Positioned before anything gets a chance to run: the object is already active by the
            // time Get returns, so setting the position afterwards would give it one frame at the
            // last life's coordinates — visible as a corpse-blink at the previous kill site.
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;

            _live.Add(instance);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;
            if (!_live.Remove(instance)) return;   // already returned

            _pool.Release(instance);
        }

        /// <summary>Puts everything currently out back in the pool.</summary>
        public void ReleaseAll()
        {
            // Backwards, because Release mutates the list it is walking.
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                GameObject instance = _live[i];
                _live.RemoveAt(i);
                if (instance != null) _pool.Release(instance);
            }
        }
    }
}
