using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// Keeps the HUD canvas on a **whole-number** scale factor.
    ///
    /// This is the one thing standing between the HUD art and mush. `CanvasScaler`'s
    /// `ScaleWithScreenSize` produces a fractional factor at any window that is not exactly the
    /// reference resolution — 0.45 in a 906x463 editor Game view — and a fractional factor resamples
    /// point-filtered art off its own grid. Every detail smaller than the error disappears: the 1px
    /// bevel, the rivets, the bars' segment ticks, and most visibly the bitmap font, which rendered
    /// "74 / 128" as "r4 / 128" because the 7's top stroke fell between two screen pixels. The HUD
    /// looked flat and untextured — indistinguishable from the plain-outline chrome it replaced —
    /// and the art was never the problem.
    ///
    /// Whole-number scaling trades proportion for sharpness, and for pixel art that is the right
    /// trade: below the reference height the HUD occupies a larger share of the screen rather than
    /// being resampled, and above it the factor steps 2x, 3x, keeping that share constant again.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class PixelPerfectHUDScale : MonoBehaviour
    {
        [Tooltip("Screen height at which the factor steps up by one. The HUD art is authored at " +
                 "half its on-screen size, so 540 makes the factor 2 at the 1080p design target — " +
                 "which is what stops the HUD dominating a smaller window, where it falls to 1.")]
        [SerializeField] private int referenceHeight = 540;

        [Tooltip("Ceiling on the factor, so an unusually tall display cannot blow the HUD up to " +
                 "fill the screen.")]
        [SerializeField] private int maxScale = 4;

        private CanvasScaler _scaler;
        private int _lastHeight;

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _lastHeight = 0;   // force a recompute; a pooled or re-enabled canvas may have moved
            Apply();
        }

        private void Update()
        {
            // Polled rather than event-driven: Unity raises nothing for a window resize, and the
            // editor Game view changes size as the layout is dragged around.
            if (Screen.height != _lastHeight) Apply();
        }

        private void Apply()
        {
            if (_scaler == null) _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null) return;

            _lastHeight = Screen.height;

            // ConstantPixelSize, or the scaler recomputes a fractional factor and overwrites this.
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.scaleFactor = Factor(_lastHeight);
        }

        /// <summary>Whole-number factor for a screen height. Never below 1 — a half-size HUD is
        /// worse than one that takes up more of a small window.</summary>
        private int Factor(int screenHeight)
        {
            if (referenceHeight <= 0) return 1;
            return Mathf.Clamp(screenHeight / referenceHeight, 1, Mathf.Max(1, maxScale));
        }
    }
}
