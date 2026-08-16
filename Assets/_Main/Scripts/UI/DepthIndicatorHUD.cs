using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The floor / depth readout — GDD §UI's *"current floor/depth indicator"*.
    ///
    /// **It has no data source.** There is no floor system, no run, and no descent: rooms do not
    /// load in sequence yet, `CombatRoom.Cleared` has no subscriber, and nothing anywhere counts a
    /// floor. The number below is authored, not measured.
    ///
    /// It exists so the HUD's layout is the real one rather than one that has to be re-laid-out when
    /// floors land — the element is the cheap half, and discovering the top-right is crowded is
    /// better done now than after the descent is built. <see cref="SetFloor"/> is the seam the floor
    /// loader calls; nothing calls it today.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DepthIndicatorHUD : MonoBehaviour
    {
        [Header("Widgets")]
        [SerializeField] private Text label;

        [Header("Placeholder")]
        [Tooltip("Shown until a floor system exists. GDD's run is 16 floors, so 1 is the honest " +
                 "value for a sandbox that never descends.")]
        [SerializeField] private int floor = 1;

        [Tooltip("Total floors in a run, for the 'n / 16' readout. GDD §Boss puts the Final Boss " +
                 "on floor 16.")]
        [SerializeField] private int totalFloors = 16;

        private void Awake()
        {
            LegacyUIFont.EnsureFont(label);
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>The hook the floor loader will call. Deliberately the only way in — nothing
        /// should be reading a floor number off this component.</summary>
        public void SetFloor(int value)
        {
            floor = Mathf.Max(1, value);
            Refresh();
        }

        private void Refresh()
        {
            if (label != null) label.text = "FLOOR " + floor + " / " + totalFloors;
        }
    }
}
