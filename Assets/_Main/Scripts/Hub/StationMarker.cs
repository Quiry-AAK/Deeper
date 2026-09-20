using UnityEngine;

namespace Deeper.Hub
{
    /// <summary>
    /// The floating mark that says a camp fixture can be used — a chevron from across the camp, the
    /// <c>E</c> keycap once she is close enough to press it.
    ///
    /// **This answers a different question from <see cref="Deeper.UI.HubPromptHUD"/>, which is why
    /// both exist.** The prompt line answers "use it now, and here is the verb", and it can only do
    /// that once she is already standing in the trigger. This answers "that thing is usable at all",
    /// from anywhere in the camp — without it the weapon rack is indistinguishable from the crates
    /// until you walk into it. Scenery deliberately gets no marker: the marker *is* the distinction.
    ///
    /// A world <c>SpriteRenderer</c> rather than a world-space canvas, because the HUD is authored
    /// at half size under <c>PixelPerfectHUDScale</c>'s whole-number canvas factor and a UGUI element
    /// out here would be fighting that scale for no gain. As a plain sprite it sorts by the same
    /// rule as everything else in the camp.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StationMarker : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("The station this marks. Wired by BuildHubScene; a marker with none just idles.")]
        [SerializeField] private HubStation station;

        [Header("Art")]
        [Tooltip("Shown from a distance — a chevron pointing at the fixture.")]
        [SerializeField] private Sprite idleSprite;

        [Tooltip("Shown in range — the key she can actually press.")]
        [SerializeField] private Sprite readySprite;

        [Tooltip("Dimmed out of range, so three markers across the camp read as hints rather than " +
                 "as three things demanding attention.")]
        [SerializeField] private Color idleTint = new Color(1f, 1f, 1f, 0.55f);

        [SerializeField] private Color readyTint = Color.white;

        [Header("Motion")]
        [Tooltip("How far it rises and falls, in world units. Small: the bob is what makes the eye " +
                 "catch it, not the size.")]
        [SerializeField] private float bobHeight = 0.09f;

        [SerializeField] private float bobSpeed = 2.2f;

        [Tooltip("Seconds of phase offset, so several markers in one camp are not in lockstep. " +
                 "Set per marker by BuildHubScene.")]
        [SerializeField] private float bobPhase;

        private SpriteRenderer _renderer;
        private Vector3 _rest;
        private bool _wasReady;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Rest position is captured here rather than in Awake, because the bob writes to the same
        /// transform every frame: a recycled instance that re-read its position on enable would
        /// otherwise capture wherever the bob happened to leave it and drift upward on every reuse.
        /// Nothing pools the camp today, but this is the project's standing rule for pooled actors
        /// and the marker is exactly the shape of component that gets reused later.
        /// </summary>
        private void OnEnable()
        {
            _rest = transform.localPosition;

            // Force the first Apply to run rather than trusting the serialized sprite: a marker
            // built ready-looking would sit showing a keycap she cannot press.
            _wasReady = true;
            Apply(false);
        }

        private void Update()
        {
            bool ready = station != null && station.PlayerInRange;
            if (ready != _wasReady) Apply(ready);

            // Unscaled, so the bob does not freeze into a still frame the moment a panel pauses the
            // camp. The whole point of the motion is to read as alive.
            float t = (Time.unscaledTime + bobPhase) * bobSpeed;

            transform.localPosition = _rest + new Vector3(0f, Mathf.Sin(t) * bobHeight, 0f);
        }

        private void Apply(bool ready)
        {
            _wasReady = ready;

            if (_renderer == null) return;

            Sprite sprite = ready ? readySprite : idleSprite;
            if (sprite != null) _renderer.sprite = sprite;

            _renderer.color = ready ? readyTint : idleTint;
        }
    }
}
