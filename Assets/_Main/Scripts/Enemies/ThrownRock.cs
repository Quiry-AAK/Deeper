using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// A rock in flight. Travels in a straight line, hurts the first thing it is allowed to hurt,
    /// and dies on a wall or on its own timer.
    ///
    /// It does NOT lead the target — it flies along the direction the Slinger committed to when its
    /// wind-up started. That is the dodge, and it is why the projectile is slower than the player.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ThrownRock : MonoBehaviour
    {
        [Tooltip("Seconds before it gives up and destroys itself. A rock thrown across an empty " +
                 "room otherwise flies forever, and nothing else removes it — TestSpawner.Clear " +
                 "only knows about the actors it spawned.")]
        [SerializeField] private float lifetime = 4f;

        [Tooltip("Layers it damages. Player only.")]
        [SerializeField] private LayerMask targetLayers = 1 << 6;

        [Tooltip("Layers that stop it dead. Default (0) is where the wall tilemap lives.")]
        [SerializeField] private LayerMask blockingLayers = 1;

        private Rigidbody2D _body;
        private PooledActor _pooled;
        private Vector2 _velocity;
        private float _damage;

        /// <summary>
        /// Resolved on demand, never cached in <c>Awake</c>: <see cref="ActorPool"/> attaches the
        /// <see cref="PooledActor"/> after <c>Instantiate</c> has already run Awake, so an Awake
        /// lookup finds nothing and the rock silently falls back to Destroy for its whole life.
        /// </summary>
        private PooledActor Pooled
        {
            get
            {
                if (_pooled == null) _pooled = GetComponent<PooledActor>();
                return _pooled;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>Sends it on its way. Called by <see cref="RockThrow"/> the frame it spawns.</summary>
        public void Launch(Vector2 velocity, float damage)
        {
            _velocity = velocity;
            _damage = damage;

            if (Pooled != null) Pooled.Release(lifetime);
            else Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// A recycled rock must sit still until it is launched. Without this it leaves the pool
        /// already carrying the last throw's velocity and travels for the frame between being
        /// activated and <see cref="Launch"/> being called.
        /// </summary>
        private void OnEnable()
        {
            _velocity = Vector2.zero;
            _damage = 0f;
        }

        private void FixedUpdate()
        {
            // MovePosition on a kinematic body rather than a velocity on a dynamic one: the rock
            // must not be shoved off course by walking into an enemy, and must not push anything
            // it passes.
            _body.MovePosition(_body.position + _velocity * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            int layer = 1 << other.gameObject.layer;

            if ((targetLayers.value & layer) != 0)
            {
                Damageable target = other.GetComponentInParent<Damageable>();
                if (target != null && target.TakeDamage(_damage))
                {
                    Spend();
                    return;
                }

                // Fell through on purpose: a rock that arrives during the player's invulnerability
                // window should keep flying rather than vanish having done nothing, or a second
                // Slinger's shot is silently eaten by the first one's.
                return;
            }

            if ((blockingLayers.value & layer) != 0) Spend();
        }

        private void Spend()
        {
            if (Pooled != null) Pooled.Release();
            else Destroy(gameObject);
        }
    }
}
