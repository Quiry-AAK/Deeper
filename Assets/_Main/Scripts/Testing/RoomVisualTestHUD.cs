using UnityEngine;
using UnityEngine.UI;

namespace Deeper.Testing
{
    /// <summary>
    /// The scene's one button and its readout. Separate from <see cref="RoomVisualTest"/> because
    /// that builds rooms and this draws a button — one job each, and the lab must stay drivable
    /// from a probe with no UI present at all.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomVisualTestHUD : MonoBehaviour
    {
        [SerializeField] private RoomVisualTest lab;
        [SerializeField] private Button reRollButton;
        [SerializeField] private Text readout;

        private void Awake()
        {
            if (lab == null) lab = FindFirstObjectByType<RoomVisualTest>();
            if (reRollButton != null && lab != null) reRollButton.onClick.AddListener(lab.ReRoll);
        }

        private void OnEnable()
        {
            if (lab != null) lab.Rolled += Show;
        }

        private void OnDisable()
        {
            if (lab != null) lab.Rolled -= Show;
        }

        private void Start()
        {
            if (lab != null) Show(lab.Summary);
        }

        private void Show(string summary)
        {
            if (readout != null) readout.text = summary;
        }
    }
}
