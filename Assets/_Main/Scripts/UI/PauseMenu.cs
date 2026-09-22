using Deeper.Core;
using Deeper.Run;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The run's pause screen (owner, 2026-09-20): Resume, Abandon Run, and the grid of everything
    /// this run is carrying.
    ///
    /// <b>It exists because the upgrade readout had to leave the play screen.</b> A run takes an
    /// uncapped number of upgrades, so a strip down the side of the screen either grows off it or
    /// starts lying about how many were taken. The picks belong somewhere the game is already
    /// stopped; this is that place, and the level-up offer is the other one. GDD §UI lists no pause
    /// screen — see the change brief.
    ///
    /// <b>The key is read off the keyboard device, not through an InputAction.</b>
    /// <see cref="RunPause"/> disables the whole Player action map while it holds, so a key bound
    /// through the asset would switch itself off the instant this menu opened and there would be no
    /// way back out. <c>TestConfigHUD</c>'s toggle has the same shape for the same reason.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The whole screen, switched on and off. Separate from this component's object " +
                 "because a script on the thing it hides stops running its own Update and could " +
                 "never re-open itself — the same two-object rule the wave indicator follows.")]
        [SerializeField] private GameObject panel;

        [SerializeField] private Button resumeButton;

        [SerializeField] private Button abandonButton;

        [Tooltip("Swapped to the confirm wording on the first press of Abandon, so ending a run " +
                 "always takes two deliberate clicks.")]
        [SerializeField] private Text abandonLabel;

        [Header("Input")]
        [Tooltip("Opens and closes the menu. Read straight off Keyboard.current — see the class " +
                 "comment for why this cannot be an InputAction.")]
        [SerializeField] private Key toggleKey = Key.Escape;

        [Header("Refs")]
        [Tooltip("Holds the game paused while the menu is open. Shared with the offer screen and " +
                 "the death screen, and refcounted there, so two panels cannot un-pause each other.")]
        [SerializeField] private RunPause pause;

        [Tooltip("Abandoning ends the run through this, exactly as dying does.")]
        [SerializeField] private RunEnd run;

        [Tooltip("Checked before opening. A level-up is a forced choice, and a menu over it would " +
                 "let the player walk away from a card they have to pick.")]
        [SerializeField] private UpgradeOffer offer;

        [Tooltip("Checked before opening. Once the run is over there is nothing left to pause.")]
        [SerializeField] private RunSummaryPanel summary;

        [Header("Wording")]
        [SerializeField] private string abandonText = "ABANDON RUN";

        [SerializeField] private string abandonConfirmText = "CONFIRM?";

        private bool _holdingPause;
        private bool _confirmingAbandon;

        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Awake()
        {
            if (pause == null) pause = FindFirstObjectByType<RunPause>();
            if (run == null) run = FindFirstObjectByType<RunEnd>();
            if (offer == null) offer = FindFirstObjectByType<UpgradeOffer>(FindObjectsInactive.Include);
            if (summary == null) summary = FindFirstObjectByType<RunSummaryPanel>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            // Subscribed here rather than wired by the layout tool: UnityEvent listeners added from
            // an editor script are runtime-only and do not serialise, so a button wired that way
            // does nothing the moment the scene is reloaded. RunSummaryPanel's Return button has
            // the same shape for the same reason.
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (abandonButton != null) abandonButton.onClick.AddListener(Abandon);

            // The hold is kept across Abandon and released here instead, when the summary screen
            // actually opens. RunEnd.Finish defers that by its own delay on the unscaled clock so
            // the killing blow's hitstop can play out — popping at the click would hand the player
            // a live game for that window, with her own death screen arriving on top of it.
            if (run != null) run.Ended += OnRunEnded;
        }

        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (abandonButton != null) abandonButton.onClick.RemoveListener(Abandon);

            if (run != null) run.Ended -= OnRunEnded;

            // Unconditionally, on the way out. A leaked hold leaves the player frozen and unable to
            // move for the rest of the session, which reads as a bug in the input system rather
            // than as a panel that forgot to clean up.
            ReleasePause();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || toggleKey == Key.None) return;

            if (!keyboard[toggleKey].wasPressedThisFrame) return;

            if (IsOpen) SetOpen(false);
            else if (CanOpen) SetOpen(true);
        }

        /// <summary>Whether anything else modal is already up. Both panels expose their own
        /// <c>IsOpen</c>, so this asks them rather than tracking a second copy of that state.</summary>
        private bool CanOpen
        {
            get
            {
                if (offer != null && offer.IsOpen) return false;
                if (summary != null && summary.IsOpen) return false;
                return true;
            }
        }

        /// <summary>Public so a probe can drive it — simulated key presses never reach play mode
        /// here (Engineering/01-VERIFICATION.md §2), so a menu only a key can open is a menu
        /// nothing can test.</summary>
        [ContextMenu("Toggle")]
        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (panel == null) return;

            panel.SetActive(open);

            // Reset every time, not only on close: a menu re-opened later must not still be one
            // click away from ending the run.
            SetConfirmingAbandon(false);

            if (open) HoldPause();
            else ReleasePause();
        }

        /// <summary>Wired onto the Resume button.</summary>
        public void Resume()
        {
            SetOpen(false);
        }

        /// <summary>
        /// Ends the run. The first press only arms it; the second goes through.
        ///
        /// Abandoning is owner-directed and in no design doc — it finishes as a <i>death</i>, so
        /// BALANCE §14's Shard award for the depth reached is paid exactly as it would have been
        /// had she died there. See the change brief.
        /// </summary>
        [ContextMenu("Abandon")]
        public void Abandon()
        {
            if (!_confirmingAbandon)
            {
                SetConfirmingAbandon(true);
                return;
            }

            SetConfirmingAbandon(false);

            // The panel goes, the hold stays. OnRunEnded releases it once the summary screen is up
            // and holding its own — see OnEnable.
            if (panel != null) panel.SetActive(false);

            if (run != null) run.Finish(RunEnd.Outcome.Died);
            else ReleasePause();
        }

        private void OnRunEnded(RunEnd.Summary result)
        {
            ReleasePause();
        }

        private void SetConfirmingAbandon(bool confirming)
        {
            _confirmingAbandon = confirming;

            if (abandonLabel != null) abandonLabel.text = confirming ? abandonConfirmText : abandonText;
        }

        private void HoldPause()
        {
            if (_holdingPause || pause == null) return;

            _holdingPause = true;
            pause.Push();
        }

        private void ReleasePause()
        {
            if (!_holdingPause) return;

            _holdingPause = false;
            if (pause != null) pause.Pop();
        }
    }
}
