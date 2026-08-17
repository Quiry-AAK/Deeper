using Deeper.Combat;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Drops a Secret Vault key when this dies — CORE_SYSTEMS §8's "granted by defeating a rare
    /// elite spawn earlier in that biome", which is the Deep Warden in the Upper Caves
    /// (CONTENT_DESIGN §5).
    ///
    /// Credited directly on the killing blow rather than dropped as a pickup, exactly like
    /// <see cref="XPReward"/> and for the same reason: there is no pickup system in the project at
    /// all, and building the first one here would be a second objective smuggled into a room type.
    /// §8 says "granted by defeating", which this is literally. When XP orbs land, a key on the
    /// ground is the same job and this is the half that gets replaced.
    ///
    /// Its own component rather than a field on <c>Enemy</c> so only the elite carries the coupling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeyReward : MonoBehaviour
    {
        [Tooltip("Keys granted on death. 1 elite, 1 vault — CORE_SYSTEMS §8 gives no drop rate, " +
                 "and a chance to drop would make the elite's whole reward invisible on the roll " +
                 "that fails. INVENTED; see the change brief.")]
        [SerializeField] private int secretKeys = 1;

        [Header("Refs — on this object")]
        [Tooltip("GetComponent, never RigRefs: a pooled enemy's transform.root is the spawner, so a " +
                 "rig-wide search could credit a sibling enemy's death.")]
        [SerializeField] private Damageable health;

        private RunKeys _keys;

        private void Awake()
        {
            if (health == null) health = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            // Resolved per life rather than cached across pool reuse, the reason XPReward gives: a
            // recycled enemy never runs Awake again, and the player could have been reloaded since.
            if (_keys == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _keys = player.GetComponentInChildren<RunKeys>(true);
            }

            if (health != null) health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (_keys != null) _keys.GrantSecretKeys(secretKeys);
        }
    }
}
