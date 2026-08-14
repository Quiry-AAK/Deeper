using Deeper.Combat;
using Deeper.Core;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// The Tunnel Brute's attack, inherited unchanged by the Deep Warden: a radial hit around its
    /// own feet that knocks the player back (CONTENT_DESIGN §5, "telegraphed overhead slam with
    /// knockback").
    ///
    /// A circle rather than a directional arc on purpose — the slam's whole shape is "get out of
    /// the ring", and the aim it locked at wind-up is expressed as *where it walked to*, not as
    /// where the damage lands. That also makes it the one attack in Biome 1 the player cannot beat
    /// by standing behind it.
    ///
    /// The Brute carries no <see cref="ContactDamage"/>: all 15 comes from here. Giving it both
    /// would double-dip and delete the safe window beside it that LEVEL_DESIGN §2 asks Combat
    /// Rooms to preserve for the Greatsword's whiff-punish.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverheadSlam : MonoBehaviour
    {
        [Tooltip("World-unit radius of the slam, measured from this object. Kept larger than the " +
                 "Brute's attack range so stepping back the moment you see the wind-up is not " +
                 "enough — you have to leave the ring.")]
        [SerializeField] private float radius = 2.2f;

        [Tooltip("Who this can hit. Player only — a Brute should not slam the Crawler beside it.")]
        [SerializeField] private LayerMask targetLayers = 1 << 6;

        [Tooltip("Speed of the push, in units/sec, decaying to zero over Knockback Time. No " +
                 "design doc quantifies the Brute's knockback — first-pass.")]
        [SerializeField] private float knockbackSpeed = 12f;

        [Tooltip("Seconds the push lasts. Short: long enough to read as impact, not long enough " +
                 "to take control away.")]
        [SerializeField] private float knockbackTime = 0.3f;

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;
        [SerializeField] private TelegraphedAttack attack;

        private readonly Collider2D[] _overlaps = new Collider2D[8];

        private void Awake()
        {
            enemy = RigRefs.Find(this, enemy);
            attack = RigRefs.Find(this, attack);
        }

        private void OnEnable()
        {
            if (attack != null) attack.ActiveOpened += Slam;
        }

        private void OnDisable()
        {
            if (attack != null) attack.ActiveOpened -= Slam;
        }

        private void Slam()
        {
            if (enemy == null || enemy.Definition == null) return;

            float damage = enemy.Definition.Attack.Damage;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(targetLayers);

            // useTriggers stays false: the player is hit through her capsule, and a trigger volume
            // on her rig is not a body to slam.
            filter.useTriggers = false;

            int hits = Physics2D.OverlapCircle(transform.position, radius, filter, _overlaps);

            for (int i = 0; i < hits; i++)
            {
                Collider2D other = _overlaps[i];
                if (other == null) continue;

                Damageable target = other.GetComponentInParent<Damageable>();
                if (target == null || !target.TakeDamage(damage)) continue;

                // Only knock back what the hit actually landed on. A target inside its own
                // invulnerability window took nothing, and shoving it anyway would let a pair of
                // Brutes chain-push the player across the room while dealing no damage.
                PlayerController pushed = other.GetComponentInParent<PlayerController>();
                if (pushed == null) continue;

                Vector2 away = (Vector2)pushed.transform.position - (Vector2)transform.position;
                if (away.sqrMagnitude < 0.0001f) away = Vector2.up;

                pushed.ApplyKnockback(away.normalized * knockbackSpeed, knockbackTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // The damage volume is invisible otherwise, which makes "why did that miss" guesswork.
            Gizmos.color = new Color(0.85f, 0.85f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
