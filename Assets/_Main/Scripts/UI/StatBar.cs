using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// A framed bar with a fill and a label. Pure view — it knows nothing about what it is showing.
    ///
    /// HP, XP and the Ultimate Gauge are three different sources drawn the same way, and the drift
    /// between three hand-maintained copies of "set a fillAmount and format a label" is exactly the
    /// kind of duplication that makes a HUD look assembled rather than designed. The binder
    /// components (<see cref="HealthBarHUD"/> and friends) hold the source; this holds the pixels.
    ///
    /// The fill lags the value rather than snapping, because a bar that jumps gives no sense of how
    /// big the hit was — the eye reads the *travel*, not the endpoints.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatBar : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The art frame. Purely decorative — the fill is a separate Image beneath it.")]
        [SerializeField] private Image frame;

        [Tooltip("Image with Type = Filled, Fill Method = Horizontal. Its fillAmount is the value.")]
        [SerializeField] private Image fill;

        [Tooltip("Drawn behind the fill so an empty bar reads as an empty socket rather than a hole.")]
        [SerializeField] private Image socket;

        [Tooltip("Optional chase bar, drawn between the socket and the fill. Leave empty on bars " +
                 "that only ever climb — an XP bar has nothing to lose.")]
        [SerializeField] private Image ghost;

        [SerializeField] private Text label;

        [Header("Feel")]
        [Tooltip("Seconds for the fill to catch up to a changed value. 0 snaps. Kept short — this " +
                 "is readability, not animation.")]
        [SerializeField] private float lerpTime = 0.12f;

        [Tooltip("Seconds the chase bar holds at the old value before it starts draining, so the " +
                 "size of a hit is legible as a block rather than only as a movement.")]
        [SerializeField] private float ghostHoldTime = 0.35f;

        [Tooltip("How fast the chase bar drains once it lets go, as a fraction of the whole bar " +
                 "per second.")]
        [SerializeField] private float ghostDrainSpeed = 0.6f;

        [Tooltip("Colour of the fill. Set per bar, not per state.")]
        [SerializeField] private Color fillColor = Color.white;

        [Tooltip("Deliberately quieter than the fill: a bright chase bar is louder than the health " +
                 "still standing behind it, which inverts what the bar is for. Not the reserved " +
                 "hazard orange-red either (ART_DIRECTION §2) — this sits on the HP bar.")]
        [SerializeField] private Color ghostColor = new Color(0.69f, 0.52f, 0.54f, 1f);

        private float _target;
        private float _shown;
        private float _ghost;
        private float _ghostHoldRemaining;

        private void Awake()
        {
            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.color = fillColor;
            }

            if (ghost != null)
            {
                ghost.type = Image.Type.Filled;
                ghost.fillMethod = Image.FillMethod.Horizontal;
                ghost.color = ghostColor;
            }

            LegacyUIFont.EnsureFont(label);
        }

        /// <summary>Sets the bar from a 0–1 value.</summary>
        public void SetNormalized(float normalized)
        {
            float next = Mathf.Clamp01(normalized);

            // Restarted on every drop, not only on the first: taking three hits in a second should
            // leave one chase bar behind the newest of them, not one still draining from the oldest.
            if (next < _target) _ghostHoldRemaining = ghostHoldTime;

            _target = next;
        }

        /// <summary>Sets the bar from a current/max pair, and writes "current / max" into the label.</summary>
        public void SetValue(float current, float max)
        {
            SetNormalized(max > 0f ? current / max : 0f);
            SetLabel(Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max));
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        /// <summary>Recolours the fill — the Ultimate's full-pulse drives this.</summary>
        public void SetFillColor(Color colour)
        {
            fillColor = colour;
            if (fill != null) fill.color = colour;
        }

        /// <summary>Jumps straight to the target, skipping the lag. Used when the HUD first binds,
        /// so the bar does not animate up from zero on the frame the scene loads.</summary>
        public void SnapToTarget()
        {
            _shown = _target;
            _ghost = _target;
            _ghostHoldRemaining = 0f;

            if (fill != null) fill.fillAmount = _shown;
            if (ghost != null) ghost.fillAmount = _ghost;
        }

        private void Update()
        {
            if (fill == null) return;

            // Unscaled: hitstop freezes scaled time to 2% on every landed hit, and a health bar that
            // stops moving during the freeze is at its least readable exactly when it matters.
            float delta = Time.unscaledDeltaTime;

            _shown = lerpTime <= 0f
                ? _target
                : Mathf.MoveTowards(_shown, _target, delta / lerpTime);

            fill.fillAmount = _shown;

            if (ghost != null) ghost.fillAmount = TrackGhost(delta);
        }

        /// <summary>
        /// The chase bar: it holds where the fill used to be, then drains down to it, so the eye
        /// reads the *size* of the hit as a block rather than having to catch the fill moving.
        /// It never lags upward — healing should show as gained immediately.
        /// </summary>
        private float TrackGhost(float delta)
        {
            if (_shown >= _ghost)
            {
                _ghost = _shown;
                _ghostHoldRemaining = 0f;
            }
            else if (_ghostHoldRemaining > 0f)
            {
                _ghostHoldRemaining -= delta;
            }
            else
            {
                _ghost = Mathf.MoveTowards(_ghost, _shown, ghostDrainSpeed * delta);
            }

            return _ghost;
        }
    }
}
