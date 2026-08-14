using Deeper.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// HUD readout for the Ultimate Gauge and the Katana's Combo Counter.
    ///
    /// Uses legacy <c>UnityEngine.UI</c> rather than TextMeshPro because TMP's essential resources
    /// aren't imported in this project (see the engineering plan's known constraints). It will be
    /// replaced wholesale by the real UI art pass (ART_DIRECTION §5); this exists so the gauge is
    /// visible and testable now.
    ///
    /// ART_DIRECTION §6 lists "Ultimate Gauge full pulse (on the HUD element itself)" as a
    /// must-have VFX — that pulse is <see cref="fullPulseSpeed"/> here, driven in code rather than
    /// as a sprite effect so it costs no art budget.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UltimateGaugeHUD : MonoBehaviour
    {
        [Header("Source — found in the scene when empty")]
        [SerializeField] private UltimateGauge gauge;
        [SerializeField] private ComboCounter combo;

        [Header("Widgets")]
        [Tooltip("Image with Type=Filled. Its fillAmount tracks the gauge 0-1.")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;
        [SerializeField] private Text comboLabel;

        [Header("Colours")]
        [SerializeField] private Color chargingColor = new Color(0.42f, 0.55f, 0.72f, 1f);
        [SerializeField] private Color fullColor = new Color(0.85f, 0.65f, 0.40f, 1f);

        [Tooltip("Pulses per second while the gauge is full. ART_DIRECTION §6 must-have VFX.")]
        [SerializeField] private float fullPulseSpeed = 2.5f;

        private void Awake()
        {
            if (gauge == null) gauge = FindFirstObjectByType<UltimateGauge>();
            if (combo == null) combo = FindFirstObjectByType<ComboCounter>();
        }

        private void OnEnable()
        {
            if (gauge != null)
            {
                gauge.Changed += HandleGaugeChanged;
                HandleGaugeChanged(gauge.Normalized);
            }

            if (combo != null)
            {
                combo.StacksChanged += HandleStacksChanged;
                HandleStacksChanged(combo.Stacks);
            }
        }

        private void OnDisable()
        {
            if (gauge != null) gauge.Changed -= HandleGaugeChanged;
            if (combo != null) combo.StacksChanged -= HandleStacksChanged;
        }

        private void Update()
        {
            if (fillImage == null || gauge == null) return;

            if (gauge.IsFull)
            {
                // Ping-pong the alpha so "ready" reads at a glance without needing a sprite.
                float t = Mathf.PingPong(Time.unscaledTime * fullPulseSpeed, 1f);
                Color c = fullColor;
                c.a = Mathf.Lerp(0.55f, 1f, t);
                fillImage.color = c;
            }
            else
            {
                fillImage.color = chargingColor;
            }
        }

        private void HandleGaugeChanged(float normalized)
        {
            if (fillImage != null) fillImage.fillAmount = normalized;

            if (label != null)
            {
                label.text = gauge != null && gauge.IsFull
                    ? "ULTIMATE  [R]"
                    : Mathf.RoundToInt(normalized * 100f) + "%";
            }
        }

        private void HandleStacksChanged(int stacks)
        {
            if (comboLabel == null) return;

            comboLabel.enabled = stacks > 0;
            if (stacks <= 0) return;

            float bonus = combo != null ? (combo.DamageMultiplier - 1f) * 100f : 0f;
            comboLabel.text = "COMBO x" + stacks + "   +" + Mathf.RoundToInt(bonus) + "%";
        }
    }
}
