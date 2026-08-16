using Deeper.Combat;
using UnityEngine;

namespace Deeper.UI
{
    /// <summary>
    /// The player's HP bar — ART_DIRECTION §5 puts it top-left, GDD §UI lists it first.
    ///
    /// Until now the only way to read her health was the test overlay's debug text, which is
    /// deleted with the harness. This is the shipped element.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarHUD : MonoBehaviour
    {
        [Header("Source — found by tag when empty")]
        [SerializeField] private Damageable health;

        [Header("Widgets")]
        [SerializeField] private StatBar bar;

        [Header("Colours")]
        [Tooltip("Deep crimson, deliberately NOT the orange-red ART_DIRECTION §2 reserves for " +
                 "Upper Caves danger telegraphs — the reservation explicitly covers UI chrome, and " +
                 "a health bar sharing a hazard colour is the confusion the rule exists to stop.")]
        [SerializeField] private Color healthy = new Color(0.62f, 0.16f, 0.22f, 1f);

        [Tooltip("Shown below the low-health threshold, so 'nearly dead' reads without reading the " +
                 "number.")]
        [SerializeField] private Color critical = new Color(0.80f, 0.24f, 0.24f, 1f);

        [Range(0f, 1f)]
        [SerializeField] private float criticalBelow = 0.3f;

        private void Awake()
        {
            if (health == null)
            {
                // By tag, like CameraRig and TestControls do: FindFirstObjectByType<Damageable>
                // would happily return a training dummy or whichever enemy loaded first.
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) health = player.GetComponentInChildren<Damageable>(true);
            }
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.Damaged += HandleDamaged;
            Refresh();

            // Snapped rather than lerped on bind, or the bar animates up from empty every time the
            // scene loads and reads as the player spawning hurt.
            if (bar != null) bar.SnapToTarget();
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= HandleDamaged;
        }

        private void Update()
        {
            // Polled as well as event-driven: Damaged fires on damage, but healing (TestControls'
            // F2, and later any HP upgrade) moves MaxHealth and Health with no event at all.
            Refresh();
        }

        private void HandleDamaged(float amount)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (bar == null || health == null) return;

            bar.SetValue(health.Health, health.MaxHealth);
            bar.SetFillColor(health.Normalized <= criticalBelow ? critical : healthy);
        }
    }
}
