using UnityEngine;

namespace Deeper.UI
{
    /// <summary>
    /// The upgrade offer's entrance: the scrim fades up, the heading follows, and the cards rise into
    /// place one after another. Before this the panel appeared in full on the frame of the killing
    /// blow, and the owner's note was that the cut was "a bit shocking" (2026-09-18).
    ///
    /// The view half again, as <see cref="UpgradeCard"/> is to <see cref="UpgradeOffer"/>: the offer
    /// decides <i>when</i> the panel shows and what is on it, this only moves the pixels in.
    ///
    /// <b>No scaling, anywhere.</b> A card scaled by a fraction resamples its point-filtered art off
    /// its own grid, which is why <see cref="UpgradeCard"/> has no hover scale either. Motion is a
    /// rise snapped to whole canvas units and a <see cref="CanvasGroup"/> fade, neither of which ever
    /// puts a texel between two screen pixels.
    ///
    /// Unscaled time throughout — it plays while <see cref="Deeper.Core.RunPause"/> holds the game at
    /// zero, and on scaled time it would never start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OfferReveal : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The full-screen dim. Faded rather than switched, and it stays a raycast target " +
                 "the whole time, so a click during the entrance is swallowed rather than landing " +
                 "on a card still on its way in.")]
        [SerializeField] private CanvasGroup scrim;

        [Tooltip("The header and subheader together.")]
        [SerializeField] private CanvasGroup heading;

        [Tooltip("Every card, in the order they are dealt — the three upgrades left to right, the " +
                 "Curse, then the centred grant card. Each needs a CanvasGroup. Cards the offer has " +
                 "switched off are skipped, so the stagger closes up around them.")]
        [SerializeField] private RectTransform[] cards = new RectTransform[0];

        [Header("Timing — unscaled seconds, all invented")]
        [SerializeField] private float scrimFade = 0.2f;

        [Tooltip("When the heading starts fading in, measured from the start of the entrance.")]
        [SerializeField] private float headingDelay = 0.08f;

        [SerializeField] private float headingFade = 0.15f;

        [Tooltip("When the first card starts rising, measured from the start of the entrance.")]
        [SerializeField] private float firstCardDelay = 0.14f;

        [Tooltip("Gap between one card starting and the next. Short enough that the row reads as " +
                 "one deal rather than four separate arrivals.")]
        [SerializeField] private float cardStagger = 0.06f;

        [Tooltip("Seconds each card takes to rise and fade in.")]
        [SerializeField] private float cardFade = 0.18f;

        [Header("Motion")]
        [Tooltip("Canvas units each card rises through. Snapped to whole units every frame; at the " +
                 "canvas's usual 2x that is 2px a step, which reads as a slide rather than a stutter.")]
        [SerializeField, Min(0f)] private float cardRise = 10f;

        private CanvasGroup[] _groups;
        private Vector2[] _home;
        private int[] _slot;

        private bool _playing;
        private bool _full;
        private float _startedAt;
        private float _endsAt;

        /// <summary>
        /// Whether the entrance has finished. The offer refuses picks until it has — a player
        /// mashing attack when the level came up would otherwise pick whatever card rose under the
        /// cursor.
        /// </summary>
        public bool IsSettled { get { return !_playing; } }

        private void Awake()
        {
            _groups = new CanvasGroup[cards.Length];
            _home = new Vector2[cards.Length];
            _slot = new int[cards.Length];

            // The layout BuildUpgradePanel authored, captured once before anything has moved a card.
            // Never re-read at the start of a deal: a deal interrupted part-way would capture the
            // card mid-rise and every later one would come to rest short of its slot.
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                _groups[i] = cards[i].GetComponent<CanvasGroup>();
                _home[i] = cards[i].anchoredPosition;
            }
        }

        private void OnDisable()
        {
            Finish();
        }

        /// <summary>
        /// Starts the entrance. <paramref name="full"/> fades the scrim up from nothing; without it
        /// the scrim is already up — the next of several queued offers — and only the heading and
        /// the cards re-deal, so the change of offer is visible without the screen flickering dim.
        /// </summary>
        public void Play(bool full)
        {
            // A deal skips the scrim by starting its clock at the heading, so the cards keep the same
            // spacing behind the heading whichever kind of entrance this is.
            float skip = full ? 0f : headingDelay;

            _startedAt = Time.unscaledTime - skip;

            int dealt = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                bool live = cards[i] != null && cards[i].gameObject.activeSelf;
                _slot[i] = live ? dealt++ : -1;
            }

            float cardsEnd = firstCardDelay + Mathf.Max(0, dealt - 1) * cardStagger + cardFade;
            float end = Mathf.Max(cardsEnd, headingDelay + headingFade);
            if (full) end = Mathf.Max(end, scrimFade);

            _endsAt = _startedAt + end;
            _playing = true;
            _full = full;

            if (scrim != null) scrim.alpha = full ? 0f : 1f;

            Apply(skip);
        }

        /// <summary>Puts everything at rest immediately. Public so a probe can skip the wait.</summary>
        [ContextMenu("Finish")]
        public void Finish()
        {
            _playing = false;

            if (scrim != null) scrim.alpha = 1f;
            if (heading != null) heading.alpha = 1f;

            if (_groups == null) return;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                cards[i].anchoredPosition = _home[i];
                if (_groups[i] != null) _groups[i].alpha = 1f;
            }
        }

        private void Update()
        {
            if (!_playing) return;

            if (Time.unscaledTime >= _endsAt)
            {
                Finish();
                return;
            }

            Apply(Time.unscaledTime - _startedAt);
        }

        private void Apply(float t)
        {
            if (_full && scrim != null) scrim.alpha = Progress(t, 0f, scrimFade);
            if (heading != null) heading.alpha = Progress(t, headingDelay, headingFade);

            for (int i = 0; i < cards.Length; i++)
            {
                if (_slot[i] < 0 || cards[i] == null) continue;

                float p = Progress(t, firstCardDelay + _slot[i] * cardStagger, cardFade);

                // Ease-out: fast off the mark, settling into place. Whole units only — see the class
                // comment on why nothing here may land between pixels.
                float eased = 1f - (1f - p) * (1f - p);
                float drop = Mathf.Round(cardRise * (1f - eased));

                cards[i].anchoredPosition = _home[i] - new Vector2(0f, drop);
                if (_groups[i] != null) _groups[i].alpha = p;
            }
        }

        /// <summary>0 before <paramref name="start"/>, 1 after it plus <paramref name="length"/>.</summary>
        private static float Progress(float t, float start, float length)
        {
            return length > 0f ? Mathf.Clamp01((t - start) / length) : (t >= start ? 1f : 0f);
        }
    }
}
