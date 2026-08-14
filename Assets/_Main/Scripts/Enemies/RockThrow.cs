using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// The Rock Slinger's attack: one rock, launched along the direction its wind-up promised
    /// (CONTENT_DESIGN §5, "throws slow, telegraphed projectiles from range").
    ///
    /// The Slinger carries no <see cref="Deeper.Combat.ContactDamage"/> — all 6 of its damage is
    /// the rock. That is what separates it functionally from the Cave Crawler rather than only by
    /// its numbers (Design Rule 4): the Crawler punishes you for being near it, the Slinger
    /// punishes you for standing still far from it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RockThrow : MonoBehaviour
    {
        [Tooltip("The rock. Spawned at the scene root, never as a child of the Slinger — a " +
                 "parented projectile rides its thrower and disappears when the thrower dies.")]
        [SerializeField] private ThrownRock projectile;

        [Tooltip("Units/sec. Deliberately slower than the player's 5.0 so strafing beats it, and " +
                 "in family with BALANCE §6's 2.5 u/s boss projectile. No design doc gives the " +
                 "Slinger a speed — first-pass.")]
        [SerializeField] private float projectileSpeed = 4.5f;

        [Tooltip("How far in front of the Slinger the rock appears, so it does not spawn inside " +
                 "its own collider.")]
        [SerializeField] private float spawnOffset = 0.5f;

        [Tooltip("Rocks kept ready for reuse. Two covers a Slinger's own cadence — one in flight, " +
                 "one spare — without paying for an Instantiate mid-fight.")]
        [SerializeField] private int pooledRocks = 2;

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;
        [SerializeField] private TelegraphedAttack attack;

        private ActorPool _rocks;

        private void Awake()
        {
            enemy = RigRefs.Find(this, enemy);
            attack = RigRefs.Find(this, attack);
        }

        private void OnEnable()
        {
            if (attack != null) attack.ActiveOpened += Throw;
        }

        private void OnDisable()
        {
            if (attack != null) attack.ActiveOpened -= Throw;
        }

        private void Throw()
        {
            if (projectile == null || enemy == null || enemy.Definition == null) return;

            Vector2 aim = attack.AimDirection;
            if (aim == Vector2.zero) return;

            // Launched from the ground line, NOT lifted toward the raised arm. Y is a ground axis
            // in a top-down game, not a height: measured, a rock spawned 0.85 up travelled 0.85
            // north of the player for its whole flight and passed clean over her collider every
            // time. The rock is lifted on its prefab's Visual child instead, so the art sits at
            // arm height while the collider stays on the line between shooter and target — the
            // same split the character rigs use.
            Vector3 origin = transform.position + (Vector3)(aim * spawnOffset);

            // Built on the first throw rather than in Awake, so a Slinger that never gets in range
            // never allocates rocks at all. Parent is null on purpose: a rock parented to its
            // thrower rides it and vanishes with it, and pooled rocks must outlive the Slinger's
            // own trip back to the pool.
            if (_rocks == null)
            {
                _rocks = new ActorPool(projectile.gameObject, null, pooledRocks, maxSize: 16);
            }

            GameObject spawned = _rocks.Get(origin);
            ThrownRock rock = spawned.GetComponent<ThrownRock>();
            if (rock != null) rock.Launch(aim * projectileSpeed, enemy.Definition.Attack.Damage);
        }
    }
}
