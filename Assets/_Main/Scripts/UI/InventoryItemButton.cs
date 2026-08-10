using System;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// One clickable row in the inventory screen. The same row shape backs both the fixed
    /// equipped-slot rows and the runtime-instantiated carried rows — only the payload and the
    /// click meaning differ, so there is one row component rather than two near-identical ones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryItemButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private Text detail;

        /// <summary>Raised with this row when its button is clicked.</summary>
        public event Action<InventoryItemButton> Clicked;

        /// <summary>Whatever the owner bound to this row (a slot, an item, …).</summary>
        public object Payload { get; private set; }

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(RaiseClicked);

            LegacyUIFont.EnsureFont(label);
            LegacyUIFont.EnsureFont(detail);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(RaiseClicked);
        }

        private void RaiseClicked() => Clicked?.Invoke(this);

        public void Bind(object payload, Sprite sprite, string labelText, string detailText, bool interactable)
        {
            Payload = payload;

            if (label != null) label.text = labelText;
            if (detail != null) detail.text = detailText;

            if (icon != null)
            {
                icon.sprite = sprite;
                // No art exists yet; keep the slot readable rather than showing a white box.
                icon.enabled = sprite != null;
            }

            if (button != null) button.interactable = interactable;
        }
    }
}
