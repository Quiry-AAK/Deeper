using Deeper.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Core
{
    /// <summary>
    /// Stops the game for a modal panel — the level-up upgrade offer (CORE_SYSTEMS §12: "on
    /// level-up: game pauses, upgrade panel opens") and the sandbox's debug menu.
    ///
    /// It owns three things that must move together, because any one of them alone is a bug:
    ///
    /// 1. <c>Time.timeScale</c>. Everything in the HUD already animates on unscaled time
    ///    (<c>StatBar</c>, <c>ExperienceBarHUD</c>, <c>UltimateGaugeHUD</c>), so bars keep moving
    ///    behind the panel for free.
    /// 2. The <b>Player action map</b>. Zeroing time is not enough: every player system reads its
    ///    <c>InputAction</c> straight off the shared asset and UGUI's EventSystem is nowhere in that
    ///    path, so without this a click on a card also swings the katana. <c>TestConfigHUD</c> found
    ///    this first; the recipe here is its recipe.
    /// 3. The hardware <b>cursor</b>, which <c>PlayerAim</c> hides while a reticle is drawn — a
    ///    modal panel under a hidden cursor has buttons and nothing to click them with.
    ///
    /// <b>Refcounted, and that is the point of it being one object.</b> Two panels open at once used
    /// to double-toggle: the first to close re-enabled input underneath the second, leaving the debug
    /// menu clickable and the player walking around behind it.
    ///
    /// <b>A pause can ease in</b> (<see cref="Push(float)"/>) — the level-up beat slows the fight to a
    /// stop instead of cutting it (owner, 2026-09-18). The ramp lives here rather than in the caller
    /// because it is still this object writing <c>Time.timeScale</c>, and <see cref="IsPaused"/> is
    /// true from the first frame of it. That is load-bearing: <c>HitStop.Freeze</c> refuses while
    /// paused, so the killing blow that raised the level-up cannot start a freeze halfway down the
    /// ramp and later "restore" the game to full speed under the panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunPause : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("The shared action asset every player system binds against. Disabling its Player " +
                 "map is what actually stops her acting while a panel is open.")]
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private string actionMapName = "Player";

        [Header("Refs")]
        [Tooltip("Cancelled on the way into a pause. HitStop's coroutine runs on UNSCALED time, so " +
                 "a freeze still in flight when the panel opens would finish ~50ms later and write " +
                 "timeScale back to 1 — un-pausing the panel underneath the player. This is not a " +
                 "corner case: the killing blow that grants the XP is exactly the hit that freezes.")]
        [SerializeField] private HitStop hitStop;

        [Tooltip("Time scale to return to. A FIXED value, never the scale sampled on the way in — " +
                 "sampling captures whatever froze time first and 'restores' the game to it forever.")]
        [SerializeField] private float normalScale = 1f;

        [Header("Ease in")]
        [Tooltip("Shape of an eased pause: time scale = normal × (1 − progress)^this. 2 drops fast " +
                 "and spends the tail crawling, which reads as time stopping rather than as the " +
                 "game lagging; over a 0.45s ease only about 0.15s of game time passes. 1 is a " +
                 "straight line.")]
        [SerializeField, Min(0.1f)] private float easeExponent = 2f;

        private int _holds;
        private bool _cursorWasVisible;

        private bool _easing;
        private float _easeStartedAt;
        private float _easeDuration;

        /// <summary>
        /// Whether anything is currently holding the game paused. True for the whole of an eased
        /// entry too, not only once time reaches zero — see the class comment.
        /// </summary>
        public bool IsPaused { get { return _holds > 0; } }

        private void Awake()
        {
            if (hitStop == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) hitStop = player.GetComponentInChildren<HitStop>(true);
            }
        }

        /// <summary>Takes a hold on the pause. Every call must be matched by a <see cref="Pop"/>.</summary>
        public void Push()
        {
            Push(0f);
        }

        /// <summary>
        /// Takes a hold, slowing the game to a stop over <paramref name="easeIn"/> real seconds rather
        /// than stopping it at once. Input and the cursor still switch on the first frame — a player
        /// mashing attack through the slow-down would otherwise queue swings into the panel.
        ///
        /// Only the hold that starts a pause can ease it. One arriving while the game is already
        /// paused, or already easing, cannot make it slower; an instant one arriving mid-ease (the
        /// debug menu, the death screen) stops it now, because whoever asked for that wants it now.
        /// </summary>
        public void Push(float easeIn)
        {
            _holds++;

            if (_holds > 1)
            {
                if (easeIn <= 0f && _easing) StopEase(0f);
                return;
            }

            if (hitStop != null) hitStop.Cancel();

            if (easeIn > 0f)
            {
                _easing = true;
                _easeStartedAt = Time.unscaledTime;
                _easeDuration = easeIn;
                Time.timeScale = normalScale;
            }
            else
            {
                Time.timeScale = 0f;
            }

            InputActionMap map = PlayerMap();
            if (map != null) map.Disable();

            _cursorWasVisible = Cursor.visible;
            Cursor.visible = true;
        }

        /// <summary>Releases one hold. The game resumes when the last one is gone.</summary>
        public void Pop()
        {
            if (_holds == 0) return;

            _holds--;
            if (_holds > 0) return;

            StopEase(normalScale);

            InputActionMap map = PlayerMap();
            if (map != null) map.Enable();

            Cursor.visible = _cursorWasVisible;
        }

        private void Update()
        {
            if (!_easing) return;

            // Unscaled, obviously — this is the thing driving the scaled clock toward zero, and on
            // scaled time the ramp would itself slow down as it went and never quite arrive.
            float progress = _easeDuration > 0f
                ? (Time.unscaledTime - _easeStartedAt) / _easeDuration
                : 1f;

            if (progress >= 1f)
            {
                StopEase(0f);
                return;
            }

            Time.timeScale = normalScale * Mathf.Pow(1f - progress, easeExponent);
        }

        /// <summary>Ends any ramp in flight and lands the scale on a FIXED value (01-VERIFICATION §8).</summary>
        private void StopEase(float scale)
        {
            _easing = false;
            Time.timeScale = scale;
        }

        private void OnDisable()
        {
            // Unconditionally, on the way out. A leaked hold leaves the game frozen with the player
            // unable to move for the rest of the session, which looks exactly like a bug in the input
            // system rather than a panel that forgot to clean up.
            _easing = false;
            if (_holds == 0) return;

            _holds = 0;
            Time.timeScale = normalScale;

            InputActionMap map = PlayerMap();
            if (map != null) map.Enable();

            Cursor.visible = true;
        }

        private InputActionMap PlayerMap()
        {
            return inputActions != null ? inputActions.FindActionMap(actionMapName, false) : null;
        }
    }
}
