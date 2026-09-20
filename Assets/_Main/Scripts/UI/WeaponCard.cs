using System;
using Deeper.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// One weapon on the rack's screen — icon, name, description, and the border that shows which
    /// one the run is currently locked to.
    ///
    /// The view half of the split <see cref="UpgradeCard"/> and <see cref="UpgradeOffer"/> already
    /// use: this draws a <see cref="WeaponDefinition"/> and reports a click, and knows nothing about
    /// where the list came from or what picking one does.
    ///
    /// It draws <c>WeaponDefinition.Icon</c> and never <c>BodyLayer</c>. They are different assets
    /// and confusing them is a mistake this project has already made once: the body layer draws the
    /// weapon *as held*, in her arm position, so using it here renders a tiny figure instead of a
    /// weapon.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponCard : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("Tinted to show selection. The card's whole selected/unselected state is this " +
                 "border, because three cards on screen at once need one difference, not four.")]
        [SerializeField] private Image frame;

        [SerializeField] private Image icon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Button button;

        [Header("Look")]
        [Tooltip("Border colour of the weapon this run is locked to.")]
        [SerializeField] private Color selectedTint = new Color(0.98f, 0.82f, 0.35f);

        [Tooltip("Border colour of the other two.")]
        [SerializeField] private Color restTint = new Color(0.45f, 0.44f, 0.52f);

        /// <summary>Raised when this card is clicked.</summary>
        public event Action<WeaponCard> Picked;

        /// <summary>What this card is drawing. Null on a card with no weapon bound.</summary>
        public WeaponDefinition Weapon { get; private set; }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Pick);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Pick);
        }

        /// <summary>Draws <paramref name="weapon"/>, or hides the card when it is null.</summary>
        public void Bind(WeaponDefinition weapon)
        {
            Weapon = weapon;

            if (weapon == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            LegacyUIFont.EnsureFont(nameLabel);
            LegacyUIFont.EnsureFont(bodyLabel);

            if (nameLabel != null) nameLabel.text = weapon.DisplayName.ToUpperInvariant();
            if (bodyLabel != null) bodyLabel.text = weapon.Description;

            if (icon != null)
            {
                icon.sprite = weapon.Icon;

                // Hidden rather than left showing whatever the template had, so a weapon with no
                // icon authored reads as missing art instead of as another weapon's.
                icon.enabled = weapon.Icon != null;
            }

            SetSelected(false);
        }

        /// <summary>Paints the border for whether this is the run's weapon.</summary>
        public void SetSelected(bool selected)
        {
            if (frame != null) frame.color = selected ? selectedTint : restTint;
        }

        /// <summary>
        /// Chooses this card. Wired onto the button, and public so a probe can drive it — simulated
        /// key input never reaches play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Pick")]
        public void Pick()
        {
            if (Weapon == null) return;
            if (Picked != null) Picked(this);
        }
    }
}
