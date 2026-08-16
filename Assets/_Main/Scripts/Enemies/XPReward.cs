using Deeper.Combat;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Enemies
{
    /// <summary>
    /// Grants the player XP when this dies (CORE_SYSTEMS §12, "Enemies drop XP on death").
    ///
    /// **This is not the drop CORE_SYSTEMS eventually wants.** The design implies physical XP orbs —
    /// BALANCE §9's "Insight Magnet" upgrade prices an *orb pull radius* — so XP is meant to be a
    /// pickup the player walks over, not a number credited on the killing blow. Orbs need a
    /// spawnable pickup, a magnet and a collection radius; this credits directly so the XP bar has a
    /// real source now, and it is the half that gets replaced when orbs are built.
    ///
    /// Its own component rather than a field on <c>Enemy</c> so the coupling to the player rig lives
    /// in one opt-in place instead of inside the class every enemy needs for its stats.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XPReward : MonoBehaviour
    {
        [Tooltip("XP granted on death. **Invented** — BALANCE §16 records that per-enemy XP drop " +
                 "values are unresolved, and that they only make sense tuned together with the " +
                 "level curve on PlayerXP.")]
        [SerializeField] private float xp = 3f;

        [Header("Refs — on this object")]
        [Tooltip("GetComponent, never RigRefs: a pooled enemy's transform.root is the spawner, so a " +
                 "rig-wide search could credit a sibling enemy's death.")]
        [SerializeField] private Damageable health;

        private PlayerXP _player;

        private void Awake()
        {
            if (health == null) health = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            // Resolved per life rather than cached across pool reuse: a recycled enemy never runs
            // Awake again, and the player it was found for could have been reloaded since.
            if (_player == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _player = player.GetComponentInChildren<PlayerXP>(true);
            }

            if (health != null) health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (_player != null) _player.Add(xp);
        }
    }
}
