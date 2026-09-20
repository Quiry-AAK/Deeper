using Deeper.Core;
using Deeper.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The Death / Victory screen — GDD §UI's "depth reached, Shards earned, run time, weapon used,
    /// Return to Hub button".
    ///
    /// **One screen for both endings, differing in its title and its colour.** GDD §Game Loop 7 has
    /// two outcomes and they report the same five numbers; two panels would be one layout maintained
    /// twice, and the Shard award is identical either way (BALANCE §14 pays on "death or victory"
    /// with no distinction).
    ///
    /// It draws and it navigates. The award itself is <see cref="RunEnd"/>'s, computed and paid
    /// before this is ever shown — the same split as `UltimateGauge` and `UltimateGaugeHUD`, and
    /// what lets a probe check the payout in a scene with no canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunSummaryPanel : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("The run this reports on. Found in the scene when empty.")]
        [SerializeField] private RunEnd run;

        [Header("Parts — wired by Deeper/Build Run Summary Panel")]
        [Tooltip("Switched on when the run ends. The component lives on the root so it can still " +
                 "hear the event while this is hidden.")]
        [SerializeField] private GameObject panel;

        [SerializeField] private Text title;
        [SerializeField] private Text depthValue;
        [SerializeField] private Text levelsValue;
        [SerializeField] private Text shardsValue;
        [SerializeField] private Text timeValue;
        [SerializeField] private Text weaponValue;
        [SerializeField] private Button returnButton;

        [Header("Wording")]
        [SerializeField] private string diedTitle = "YOU DIED";
        [SerializeField] private string victoryTitle = "DESCENT COMPLETE";

        [Tooltip("ART_DIRECTION §2 reserves orange-red for danger. A death is the one screen in the " +
                 "game where that is the subject rather than a telegraph.")]
        [SerializeField] private Color diedTint = new Color(0.85f, 0.32f, 0.24f, 1f);

        [SerializeField] private Color victoryTint = new Color(0.96f, 0.83f, 0.45f, 1f);

        [Header("Return")]
        [Tooltip("Scene name, never a build index: indices renumber whenever Build Settings are " +
                 "reordered and the failure is silent — you return to the wrong scene. Same rule " +
                 "HubDescent follows in the other direction.")]
        [SerializeField] private string hubScene = "HubScene";

        [Tooltip("Held while this is up. Popped on the way out, because timeScale 0 survives a " +
                 "scene load and arriving in the Hub frozen looks exactly like a hang.")]
        [SerializeField] private RunPause pause;

        private bool _held;

        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Awake()
        {
            if (run == null) run = FindFirstObjectByType<RunEnd>();
            if (pause == null) pause = FindFirstObjectByType<RunPause>();

            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (run != null) run.Ended += Show;
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToHub);
        }

        private void OnDisable()
        {
            if (run != null) run.Ended -= Show;
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToHub);
        }

        /// <summary>
        /// Fills the screen in and shows it. Public so a probe can see the layout without ending a
        /// run — simulated key presses never reach play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        public void Show(RunEnd.Summary summary)
        {
            bool won = summary.Outcome == RunEnd.Outcome.Victory;

            if (title != null)
            {
                title.text = won ? victoryTitle : diedTitle;
                title.color = won ? victoryTint : diedTint;
            }

            SetValue(depthValue, summary.DepthReached.ToString());
            SetValue(levelsValue, "+" + summary.LevelsGained);
            SetValue(shardsValue, summary.Shards.ToString());
            SetValue(timeValue, Clock(summary.Seconds));
            SetValue(weaponValue, string.IsNullOrEmpty(summary.Weapon) ? "-" : summary.Weapon.ToUpperInvariant());

            if (panel != null) panel.SetActive(true);

            // RunEnd already pushed the pause before raising. This second hold is deliberate and is
            // what makes the two independent: RunEnd's is released by whatever ends the run, and a
            // panel that did not hold its own would be dismissable by anything that happened to Pop.
            // Refcounted, so both holds have to lift before input comes back.
            if (pause != null && !_held)
            {
                pause.Push();
                _held = true;
            }
        }

        /// <summary>Back to the camp with the Shards already banked (GDD §Game Loop 8).</summary>
        public void ReturnToHub()
        {
            if (_held && pause != null)
            {
                pause.Pop();
                _held = false;
            }

            // Belt and braces, and not redundant: RunPause is refcounted, so an upgrade offer that
            // was open when she died still holds one and Pop above would leave the scale at zero.
            // A run that ends must never be able to freeze the Hub.
            Time.timeScale = 1f;

            if (string.IsNullOrEmpty(hubScene))
            {
                Debug.LogError(name + ": no Hub scene named, so Return goes nowhere.", this);
                return;
            }

            SceneManager.LoadScene(hubScene);
        }

        private static void SetValue(Text label, string value)
        {
            if (label != null) label.text = value;
        }

        /// <summary>
        /// m:ss. Not h:mm:ss — BALANCE §8 targets a 30–60 minute run, so the hours column would be a
        /// zero on every screen anybody ever sees.
        /// </summary>
        private static string Clock(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }
    }
}
