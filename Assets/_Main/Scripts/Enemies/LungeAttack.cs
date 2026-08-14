using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// The Cave Crawler's attack: a committed dash along the direction its wind-up promised
    /// (CONTENT_DESIGN §5, "walks directly at player, short telegraphed lunge attack").
    ///
    /// It deals no damage of its own. The Crawler's damage is <c>ContactDamage</c>, and the lunge's
    /// job is to close the gap fast enough that contact happens — which is what makes it a threat
    /// rather than something the player strolls away from at 5.0 units/sec.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LungeAttack : MonoBehaviour
    {
        [Tooltip("World units travelled over the Active window. At the Crawler's 0.18s Active " +
                 "that is 10 units/sec — twice the player's speed, so it closes — but it commits " +
                 "0.35s beforehand, so it can be side-stepped.")]
        [SerializeField] private float lungeDistance = 1.8f;

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;
        [SerializeField] private TelegraphedAttack attack;
        [SerializeField] private Rigidbody2D body;

        private float _remaining;

        private void Awake()
        {
            enemy = RigRefs.Find(this, enemy);
            attack = RigRefs.Find(this, attack);
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (attack != null) attack.ActiveOpened += Lunge;

            // A recycled Crawler must not inherit a half-finished lunge, or its first act on
            // respawn is to have its velocity zeroed mid-stride by the previous life's timer.
            _remaining = 0f;
        }

        private void OnDisable()
        {
            if (attack != null) attack.ActiveOpened -= Lunge;
        }

        private void Lunge()
        {
            if (body == null || enemy == null || enemy.Definition == null) return;

            float active = enemy.Definition.Attack.Active;
            if (active <= 0f) return;

            // Flat velocity across the window rather than an impulse: an impulse would be damped
            // by the rigidbody and the distance travelled would depend on mass, so retuning the
            // Crawler's weight would silently retune its reach.
            body.linearVelocity = attack.AimDirection * (lungeDistance / active);
            _remaining = active;
        }

        /// <summary>
        /// Stops the lunge when its window closes.
        ///
        /// This used to be left to <see cref="EnemyChase"/>, which overwrites velocity every tick
        /// once the attack leaves its Active phase — correct on the shipped prefab, and a trap
        /// anywhere else. Measured: with chase switched off for a test, a lunging Crawler kept the
        /// velocity forever and was 450 units away seconds later. A component that cannot stop
        /// what it started is only correct by the grace of its neighbour.
        /// </summary>
        private void FixedUpdate()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.fixedDeltaTime;
            if (_remaining > 0f) return;

            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}
