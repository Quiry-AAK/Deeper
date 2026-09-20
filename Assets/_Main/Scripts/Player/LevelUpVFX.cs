using Deeper.Core;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// The burst of light on her when she levels up — the visible half of the level-up beat (owner,
    /// 2026-09-18). <c>UpgradeOffer</c> eases the fight to a stop off the same
    /// <see cref="PlayerXP.LeveledUp"/> event and brings the panel up once this has had its moment;
    /// before this there was nothing between the killing blow and a frozen full-screen panel.
    ///
    /// Not in ART_DIRECTION §6's must-have list; an owner-directed addition, recorded in the change
    /// brief.
    ///
    /// A flipbook on a child renderer rather than anything procedural: the frames are generated art
    /// (<c>Art/VFX/LevelUp.png</c>, imported and wired by <c>Deeper/Import Level-Up VFX</c>). With no
    /// frames it does nothing, which is how the beat shipped and was verified before the art existed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelUpVFX : MonoBehaviour
    {
        [Header("Art")]
        [Tooltip("The burst, in play order. Sliced from Art/VFX/LevelUp.png as LevelUp_0_<col>.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Playback rate. Unscaled — the burst plays while the pause is winding the game " +
                 "clock down to zero, and on scaled time it would slow to a halt with everything " +
                 "else. The clip's length (frames ÷ this) should roughly match UpgradeOffer's " +
                 "level-up reveal delay, so the cards arrive as the light dies away.")]
        [SerializeField, Min(1f)] private float framesPerSecond = 14f;

        [Tooltip("Frames at the end of the clip over which the burst fades to nothing. The " +
                 "generated clip's floor ring holds at full strength through its last frames, and " +
                 "without this it would blink off when the clip ends. Alpha only — a fade never " +
                 "moves a pixel, so it cannot resample the art off its grid.")]
        [SerializeField, Min(0f)] private float fadeOutFrames = 4f;

        [Header("Refs")]
        [Tooltip("The renderer the burst draws on — a child of Visual, inside her SortingGroup. Its " +
                 "sorting order puts it BEHIND her body (0), deliberately: a glow drawn over her is " +
                 "the aura-flame mistake again, burying the silhouette it is celebrating. Inside the " +
                 "group it still depth-sorts with her against enemies and walls.")]
        [SerializeField] private SpriteRenderer burstRenderer;

        [Tooltip("Found anywhere on the player rig when empty. Always populated on the rig, so " +
                 "RigRefs.Find is safe for it.")]
        [SerializeField] private PlayerXP experience;

        private float _startedAt;
        private bool _playing;

        private void Awake()
        {
            experience = RigRefs.Find(this, experience);

            if (burstRenderer != null) burstRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (experience != null) experience.LeveledUp += HandleLeveledUp;
        }

        private void OnDisable()
        {
            if (experience != null) experience.LeveledUp -= HandleLeveledUp;

            Stop();
        }

        /// <summary>Plays the burst. Context-menued so it can be looked at without levelling.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (burstRenderer == null || frames == null || frames.Length == 0) return;

            _startedAt = Time.unscaledTime;
            _playing = true;

            burstRenderer.sprite = frames[0];
            SetAlpha(1f);
            burstRenderer.enabled = true;
        }

        private void HandleLeveledUp(int level)
        {
            // Not restarted by a second raise. One big XP drop levels several times inside the same
            // call, and restarting on each would leave only the last one visible — no different on
            // screen, but a raise a moment later would snap a half-played burst back to its start.
            if (_playing) return;

            Play();
        }

        private void Update()
        {
            if (!_playing) return;

            float position = (Time.unscaledTime - _startedAt) * framesPerSecond;
            int frame = Mathf.FloorToInt(position);

            if (frame >= frames.Length)
            {
                Stop();
                return;
            }

            burstRenderer.sprite = frames[frame];

            float fadeFrom = frames.Length - fadeOutFrames;
            SetAlpha(fadeOutFrames > 0f ? Mathf.Clamp01(1f - (position - fadeFrom) / fadeOutFrames) : 1f);
        }

        private void SetAlpha(float alpha)
        {
            Color colour = burstRenderer.color;
            colour.a = alpha;
            burstRenderer.color = colour;
        }

        private void Stop()
        {
            _playing = false;
            if (burstRenderer != null) burstRenderer.enabled = false;
        }
    }
}
