using System.Collections;
using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// A target that exists only to be hit.
    ///
    /// TEST-ONLY. It stands back up instead of dying, so a tuning pass on hitbox reach, hitstop and
    /// gauge fill never runs out of things to swing at. Nothing here should be reused by real enemies
    /// — <see cref="Damageable"/> and <see cref="ContactDamage"/> are the reusable halves, and this
    /// class is only the respawn behaviour that makes them testable without an enemy roster.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Damageable health;

        [Tooltip("Switched off while it is down. Disabled rather than destroyed so the same dummy " +
                 "can stand back up; leaving the collider live would let a corpse keep blocking " +
                 "movement and swallowing swings.")]
        [SerializeField] private Collider2D body;

        [SerializeField] private GameObject visual;

        [Tooltip("Seconds before it stands back up at full health.")]
        [SerializeField] private float respawnDelay = 2f;

        private void Awake()
        {
            health = RigRefs.Find(this, health);
        }

        private void OnEnable()
        {
            if (health != null) health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            StartCoroutine(StandBackUp());
        }

        private IEnumerator StandBackUp()
        {
            if (body != null) body.enabled = false;
            if (visual != null) visual.SetActive(false);

            // Real time, not scaled. A respawn timer running on scaled time drifts further behind
            // the longer a tuning session goes, because every hit spends a chunk of the delay
            // frozen at 2% speed inside hitstop.
            yield return new WaitForSecondsRealtime(respawnDelay);

            if (health != null) health.Refill();
            if (body != null) body.enabled = true;
            if (visual != null) visual.SetActive(true);
        }
    }
}
