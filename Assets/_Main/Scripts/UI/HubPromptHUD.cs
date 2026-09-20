using Deeper.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// "E  CHOOSE WEAPON" — the line that tells her a camp fixture can be used, and the only UI the
    /// Hub needs before a panel opens.
    ///
    /// **The stations are wired in, not searched for.** `BuildHubScene` fills the array as it
    /// places them, so the HUD's list and the camp's fixtures are written by the same pass and
    /// cannot drift; a `FindObjectsByType` sweep would also pick up a station on a disabled object
    /// and quietly prompt for something that is not there.
    ///
    /// It doubles as the camp's one-line notice board (<see cref="Say"/>), because a second
    /// full-width label for "not built yet" would be a second thing to lay out and keep in sync
    /// with this one for no gain — a notice and a prompt are never both wanted at once.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubPromptHUD : MonoBehaviour
    {
        [Header("Sources — wired by BuildHubScene")]
        [Tooltip("Every station in the camp. The first one reporting the player in range wins, so " +
                 "fixtures should not have overlapping volumes.")]
        [SerializeField] private HubStation[] stations = new HubStation[0];

        [Header("Widgets")]
        [Tooltip("Hidden wholesale when there is nothing to say, so an empty label never eats a " +
                 "click or leaves a dark bar on screen.")]
        [SerializeField] private GameObject group;

        [SerializeField] private Text label;

        [Header("Text")]
        [Tooltip("Drawn before the station's own verb. Two spaces, because the bitmap face has no " +
                 "kerning and one reads as 'ECHOOSE'.")]
        [SerializeField] private string keyHint = "E  ";

        [Tooltip("How long a Say() notice holds the label before prompts take it back.")]
        [SerializeField] private float noticeSeconds = 2f;

        private string _notice;
        private float _noticeUntil;

        private void Awake()
        {
            LegacyUIFont.EnsureFont(label);
        }

        /// <summary>
        /// Puts a one-off line on screen — "The shrine is not built yet." Unscaled, so a notice
        /// raised as a modal panel opens still times out rather than hanging until it closes.
        /// </summary>
        public void Say(string message)
        {
            _notice = message;
            _noticeUntil = Time.unscaledTime + noticeSeconds;
        }

        private void Update()
        {
            if (group == null || label == null) return;

            string line = CurrentLine();
            bool show = !string.IsNullOrEmpty(line);

            if (group.activeSelf != show) group.SetActive(show);
            if (show) label.text = line;
        }

        private string CurrentLine()
        {
            if (Time.unscaledTime < _noticeUntil) return _notice;

            foreach (HubStation station in stations)
            {
                if (station == null || !station.PlayerInRange) continue;
                return keyHint + station.Prompt;
            }

            return null;
        }
    }
}
