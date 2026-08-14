using Deeper.Combat;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Which enemy this is. Holds the <see cref="EnemyDefinition"/> and, at Awake, pushes its
    /// numbers into the shared combat components.
    ///
    /// The push exists because <see cref="Damageable"/> and <see cref="ContactDamage"/> are used by
    /// the player too, so they cannot read an enemy asset without the dependency pointing the wrong
    /// way. Everything written specifically for enemies pulls instead — it reads
    /// <see cref="Definition"/> directly and needs no setter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Enemy : MonoBehaviour
    {
        [Tooltip("This enemy's row of BALANCE §5. Swapping this asset is what makes the Deep " +
                 "Warden a variant of the Tunnel Brute rather than a second class.")]
        [SerializeField] private EnemyDefinition definition;

        [Header("Refs — found on this object when empty")]
        [SerializeField] private Damageable health;

        [Tooltip("Optional — only the Cave Crawler hurts on touch. The Slinger's damage is its " +
                 "rock and the Brute's is its slam, so neither carries one.\n\n" +
                 "Resolved on this object only, never through RigRefs — see the note in Awake.")]
        [SerializeField] private ContactDamage contact;

        public EnemyDefinition Definition { get { return definition; } }

        private void Awake()
        {
            // Same-object lookup for the same reason as `contact` below: CLAUDE.md's prefab rule
            // puts Damageable on the actor root beside its collider, so the rig-wide search buys
            // nothing here and can only find the wrong rig.
            if (health == null) health = GetComponent<Damageable>();

            // Deliberately NOT RigRefs.Find. This field is legitimately empty on three of the four
            // enemies, and RigRefs searches from `transform.root` — which for anything spawned under
            // a parent (every enemy TestSpawner creates) is the SPAWNER, not the enemy. Measured: a
            // Slinger's Awake reached across and found the Cave Crawler's ContactDamage, then wrote
            // its own 6 into it; a Brute made it 15. The Crawler silently stopped dealing BALANCE
            // §5's 8 as soon as any other enemy spawned.
            //
            // ContactDamage always sits on the enemy root beside the collider it reads collisions
            // from, so the same object is the only correct place to look.
            if (contact == null) contact = GetComponent<ContactDamage>();

            if (definition == null)
            {
                Debug.LogError($"{nameof(Enemy)} on {name}: no definition assigned; this enemy " +
                               "keeps whatever placeholder numbers its prefab was authored with.", this);
                return;
            }

            if (health != null) health.SetMaxHealth(definition.MaxHealth);
            if (contact != null) contact.SetDamage(definition.Attack.Damage);
        }

        /// <summary>
        /// Brings a recycled enemy back at full health.
        ///
        /// A pooled instance never runs <c>Awake</c> again, so without this the second use of any
        /// enemy would come out of the pool still holding the 0 HP that killed it — dead on arrival,
        /// and dead again the instant anything touched it.
        /// </summary>
        private void OnEnable()
        {
            if (health != null) health.Refill();
        }
    }
}
