using Deeper.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The equipped weapon icon — GDD §UI, sitting beside the Ultimate Gauge bottom-centre per
    /// ART_DIRECTION §5.
    ///
    /// A run is one weapon locked for the whole descent (CORE_SYSTEMS §1), so this never changes
    /// mid-run today. It still listens for <see cref="RunLoadout.LoadoutChanged"/> because the Hub
    /// sets the weapon before the descent, and because the test scene swaps it by hand.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponIconHUD : MonoBehaviour
    {
        [Header("Source — found by tag when empty")]
        [SerializeField] private RunLoadout loadout;

        [Header("Widgets")]
        [SerializeField] private Image icon;
        [SerializeField] private Text nameLabel;

        private void Awake()
        {
            if (loadout == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) loadout = player.GetComponentInChildren<RunLoadout>(true);
            }

            LegacyUIFont.EnsureFont(nameLabel);
        }

        private void OnEnable()
        {
            if (loadout != null) loadout.LoadoutChanged += Refresh;

            // Refreshed even with no loadout, which is the case that used to leave a white box on
            // screen: Refresh is what *hides* the icon when there is no weapon, so returning early
            // here left the Image enabled with a null sprite and nothing to ever switch it off.
            Refresh();
        }

        private void OnDisable()
        {
            if (loadout != null) loadout.LoadoutChanged -= Refresh;
        }

        private void Refresh()
        {
            WeaponDefinition weapon = loadout != null ? loadout.Weapon : null;

            if (icon != null)
            {
                icon.sprite = weapon != null ? weapon.Icon : null;

                // Hidden rather than left showing a blank white box, which is what an Image with a
                // null sprite draws.
                icon.enabled = icon.sprite != null;
            }

            if (nameLabel != null) nameLabel.text = weapon != null ? weapon.DisplayName : string.Empty;
        }
    }
}
