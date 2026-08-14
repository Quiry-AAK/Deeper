using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Damages whatever it is touching. This is how an enemy hurts the player before it has an attack
    /// of its own — the training dummy uses it so that taking damage is testable, and the Cave
    /// Crawler's lunge will reuse it.
    ///
    /// Deliberately carries no cooldown of its own: the target's own invulnerability window is the
    /// rate limit, so two enemies leaning on the player cannot stack into instant death, and there is
    /// only one place to retune how often contact can hurt.
    ///
    /// **This depends on the target's Rigidbody2D never sleeping**, which is why `Player.prefab` sets
    /// Sleeping Mode to Never Sleep. Unity stops delivering <c>OnCollisionStay2D</c> once a body
    /// sleeps, and a player standing still against an enemy puts hers to sleep in about a second —
    /// measured: 0 damage over 1372 frames while the colliders were provably still touching, then 8
    /// damage the instant the body was woken. The failure looks like contact damage working while
    /// you walk and randomly stopping when you stand, which reads as an engine glitch rather than a
    /// sleep rule. Do not "optimise" that prefab setting back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContactDamage : MonoBehaviour
    {
        [Tooltip("Damage per touch. BALANCE §5's Cave Crawler deals 8.")]
        [SerializeField] private float damage = 8f;

        [Tooltip("Layers this can damage. Player only — an enemy should not chip the walls or the " +
                 "enemy standing next to it.")]
        [SerializeField] private LayerMask targetLayers = 1 << 6;

        /// <summary>
        /// Sets the per-touch damage. Enemies push BALANCE §5's number in from their
        /// <c>EnemyDefinition</c> rather than having it retyped onto the prefab.
        /// </summary>
        public void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Stay as well as Enter: standing against an enemy has to keep hurting, or the player
            // could park inside one and take a single hit for the whole fight.
            TryDamage(collision.collider);
        }

        private void TryDamage(Collider2D other)
        {
            // Measured, not assumed: switching this component off did NOT stop the collision
            // callbacks — a disabled ContactDamage still dealt full damage on contact. Unity only
            // honours the enabled flag for components that have one of the standard enableable
            // callbacks (Start/Update/…), and this class has none of them. Checking it here is what
            // makes the Inspector checkbox mean what every reader will assume it means.
            if (!isActiveAndEnabled) return;

            if (other == null) return;
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            Damageable target = other.GetComponentInParent<Damageable>();
            if (target != null) target.TakeDamage(damage);
        }
    }
}
