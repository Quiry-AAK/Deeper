using Deeper.Animation;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Plays the Death clip, stops the enemy doing anything else, and despawns it.
    ///
    /// Without this a killed enemy stands at 0 HP with a live collider — blocking movement,
    /// swallowing swings, and never freeing its slot in <c>TestSpawner</c>'s alive count, so the
    /// sandbox fills up with corpses and refuses to spawn more.
    ///
    /// This is the half of <c>TrainingDummy</c> that real enemies do NOT reuse: the dummy stands
    /// back up on purpose, so a tuning pass never runs out of things to hit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyDeath : MonoBehaviour
    {
        [Tooltip("Seconds the corpse stays before it is destroyed. The Death clip is played over " +
                 "slightly longer than this — a clip that ends first hands the pose back to Idle, " +
                 "so the enemy would snap upright for a frame before vanishing.")]
        [SerializeField] private float despawnDelay = 0.45f;

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Damageable health;
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private SpriteAnimationView view;

        [Tooltip("Switched off the moment it dies, so a corpse stops blocking movement and " +
                 "stops absorbing hits meant for something still alive.")]
        [SerializeField] private Collider2D body;

        [Tooltip("Switched off on death. Left running, the corpse keeps walking and attacking " +
                 "through its own death animation.")]
        [SerializeField] private EnemyChase chase;
        [SerializeField] private TelegraphedAttack attack;

        private void Awake()
        {
            health = RigRefs.Find(this, health);
            characterAnimator = RigRefs.Find(this, characterAnimator);
            view = RigRefs.Find(this, view);
            chase = RigRefs.Find(this, chase);
            attack = RigRefs.Find(this, attack);

            if (body == null) body = GetComponentInParent<Collider2D>();
        }

        private void OnEnable()
        {
            if (health != null) health.Died += HandleDied;

            // Undo the corpse. A pooled instance comes back with its collider, chase and attack
            // still switched off from the life that ended, so without this the second use of any
            // enemy is an inert prop that cannot be hit, cannot move and cannot attack.
            if (body != null) body.enabled = true;
            if (chase != null) chase.enabled = true;
            if (attack != null) attack.enabled = true;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (body != null) body.enabled = false;
            if (chase != null) chase.enabled = false;
            if (attack != null) attack.enabled = false;

            PlayDeathClip();

            // Back to the pool rather than destroyed, so a fight that kills forty Cave Crawlers
            // allocates the handful it has on screen at once and no more. Falls back to Destroy for
            // an enemy that was plain-instantiated, so nothing leaks either way.
            PooledActor pooled = GetComponentInParent<PooledActor>();
            if (pooled != null) pooled.Release(despawnDelay);
            else Destroy(gameObject, despawnDelay);
        }

        private void PlayDeathClip()
        {
            if (characterAnimator == null) return;

            int frames = view != null && view.AnimationSet != null
                ? view.AnimationSet.FrameCount(CharacterState.Death, characterAnimator.Facing)
                : 0;

            if (frames <= 0) return;

            characterAnimator.PlayAction(CharacterState.Death, despawnDelay * 1.5f, frames);
        }
    }
}
