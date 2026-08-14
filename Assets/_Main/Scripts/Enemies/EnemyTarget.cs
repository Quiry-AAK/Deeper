using Deeper.Core;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Who this enemy is fighting, how far away they are and which way they lie.
    ///
    /// Split out because two components need the same three answers — <see cref="EnemyChase"/> to
    /// decide where to walk and <see cref="TelegraphedAttack"/> to decide whether to commit. The
    /// alternative is each doing its own tag lookup and its own distance maths, which puts "who am
    /// I fighting" inside a movement class and leaves a future stationary enemy unable to reach it.
    ///
    /// The player is found by TAG, not by searching for a component: a
    /// <c>FindFirstObjectByType&lt;Damageable&gt;()</c> would cheerfully return another enemy, which
    /// is the same trap <c>TestControls</c> documents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTarget : MonoBehaviour
    {
        [Tooltip("Tag identifying what this enemy hunts.")]
        [SerializeField] private string targetTag = "Player";

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Enemy enemy;

        private Transform _target;

        /// <summary>The target, or null when there is none in the scene.</summary>
        public Transform Target
        {
            get
            {
                // Re-found when missing rather than cached once: in the sandbox the player can be
                // reset or respawned after an enemy already exists, and an enemy that gave up
                // looking would stand still for the rest of the session.
                if (_target == null)
                {
                    GameObject found = GameObject.FindGameObjectWithTag(targetTag);
                    _target = found != null ? found.transform : null;
                }

                return _target;
            }
        }

        /// <summary>Distance to the target, or <see cref="float.PositiveInfinity"/> without one.</summary>
        public float Distance
        {
            get
            {
                Transform target = Target;
                return target == null
                    ? float.PositiveInfinity
                    : Vector2.Distance(transform.position, target.position);
            }
        }

        /// <summary>Unit vector toward the target, or zero without one.</summary>
        public Vector2 Direction
        {
            get
            {
                Transform target = Target;
                if (target == null) return Vector2.zero;

                Vector2 delta = (Vector2)target.position - (Vector2)transform.position;
                return delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector2.zero;
            }
        }

        /// <summary>True when the target exists and is inside this enemy's aggro radius.</summary>
        public bool Acquired
        {
            get
            {
                EnemyDefinition definition = enemy != null ? enemy.Definition : null;
                if (definition == null) return false;

                return Distance <= definition.AggroRadius;
            }
        }

        private void Awake()
        {
            enemy = RigRefs.Find(this, enemy);
        }
    }
}
