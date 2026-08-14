using Deeper.Character;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// One enemy's row of BALANCE.md §5, as an authored asset.
    ///
    /// Enemy stats are a *table* in the design docs, which is the case CLAUDE.md names for data
    /// assets over hardcoded values — retuning the Upper Caves roster after a playtest should be
    /// four assets, not four prefabs and a recompile (Design Rule 8).
    ///
    /// It also makes the Elite exactly what ART_DIRECTION §4 says an Elite is: the Deep Warden is
    /// the Tunnel Brute prefab pointed at a different one of these, plus a palette tint. No new
    /// class, no new art.
    ///
    /// **HP, move speed and damage are locked** (BALANCE §5). Everything else here — the phase
    /// durations, the cooldown, and every engagement distance — is invented: no design doc gives an
    /// enemy a windup time, an attack range or an aggro radius. Those fields say so individually.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Deeper/Enemies/Enemy", order = 2)]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id for save data. Falls back to the asset name when left empty.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("CONTENT_DESIGN §5's Behavior column, so the intent sits next to the numbers.")]
        [SerializeField, TextArea(2, 4)] private string description;

        [Header("Stats — BALANCE §5, locked")]
        [Tooltip("Cave Crawler 20, Rock Slinger 15, Tunnel Brute 60, Deep Warden 100.")]
        [SerializeField] private float maxHealth = 20f;

        [Tooltip("Units/sec. Cave Crawler 3.5, Rock Slinger 2.5, Brute and Warden 2.0. The " +
                 "player's base is 5.0, so nothing in Biome 1 outruns her.")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Header("Attack — only Damage comes from BALANCE §5")]
        [Tooltip("Damage is BALANCE §5's column and is locked.\n\n" +
                 "Windup/Active/Recovery are NOT in any design doc — they are first-pass values. " +
                 "Windup is the telegraph the player reads and dodges, so it is the number most " +
                 "worth retuning: too short and the attack is unfair, too long and the enemy is " +
                 "harmless.")]
        [SerializeField]
        private AttackTiming attack = new AttackTiming
        {
            Windup = 0.35f, Active = 0.18f, Recovery = 0.45f, Damage = 8f,
        };

        [Tooltip("Seconds after an attack finishes before another may start. No design doc " +
                 "specifies enemy attack cadence — first-pass.")]
        [SerializeField] private float cooldown = 1.2f;

        [Header("Engagement — no design doc specifies any of these")]
        [Tooltip("Distance at which this enemy notices the player. First-pass; generous, because " +
                 "a room the player can stand in the corner of and ignore is worse than one that " +
                 "commits too early.")]
        [SerializeField] private float aggroRadius = 10f;

        [Tooltip("Distance within which it will start an attack. First-pass.")]
        [SerializeField] private float attackRange = 1.6f;

        [Tooltip("Stops walking closer than this. Keeps a melee enemy from burrowing into the " +
                 "player's collider, and is what makes a ranged enemy hold its firing line.")]
        [SerializeField] private float stopDistance = 0.9f;

        [Tooltip("Backs away when the player gets closer than this. 0 means it never retreats, " +
                 "which is every melee enemy — this is what makes the Rock Slinger kite without " +
                 "needing a movement class of its own.")]
        [SerializeField] private float retreatDistance;

        public string Id { get { return string.IsNullOrEmpty(id) ? name : id; } }
        public string DisplayName { get { return string.IsNullOrEmpty(displayName) ? name : displayName; } }
        public string Description { get { return description; } }

        public float MaxHealth { get { return maxHealth; } }
        public float MoveSpeed { get { return moveSpeed; } }
        public AttackTiming Attack { get { return attack; } }
        public float Cooldown { get { return cooldown; } }

        public float AggroRadius { get { return aggroRadius; } }
        public float AttackRange { get { return attackRange; } }
        public float StopDistance { get { return stopDistance; } }
        public float RetreatDistance { get { return retreatDistance; } }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
    }
}
