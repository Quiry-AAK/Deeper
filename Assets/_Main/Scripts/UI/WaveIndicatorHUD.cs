using Deeper.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// "WAVE 2/3" — GDD §UI: *"shown only inside Wave Rooms"*.
    ///
    /// The "only inside Wave Rooms" half is the whole point. A standard Combat Room is one batch, so
    /// a permanent "WAVE 1/1" would be noise on every room in the game and would stop the element
    /// meaning anything on the one room where it does. `CombatRoom.IsWaveRoom` is derived from the
    /// wave array, so this cannot disagree with the encounter it is describing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveIndicatorHUD : MonoBehaviour
    {
        [Header("Source — found in the scene when empty")]
        [Tooltip("The room being fought. A floor loader will need to repoint this as rooms load; " +
                 "there is one room today, so it is found once.")]
        [SerializeField] private CombatRoom room;

        [Header("Widgets")]
        [Tooltip("Switched off wholesale outside a Wave Room, so the layout does not hold an empty " +
                 "gap for it.")]
        [SerializeField] private GameObject group;

        [SerializeField] private Text label;

        private void Awake()
        {
            if (room == null) room = FindFirstObjectByType<CombatRoom>();
            LegacyUIFont.EnsureFont(label);
        }

        private void Update()
        {
            if (group == null) return;

            bool show = room != null && room.IsWaveRoom && room.State == RoomState.Fighting;
            if (group.activeSelf != show) group.SetActive(show);

            if (!show || label == null) return;

            label.text = "WAVE " + room.Wave + " / " + room.WaveCount;
        }
    }
}
