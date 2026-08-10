using System;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// Switches between mutually exclusive content panels. Only one tab's content is active at a
    /// time, so views inside an inactive tab stay disabled and refresh themselves when shown.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TabGroup : MonoBehaviour
    {
        [Serializable]
        private struct Tab
        {
            public Button Button;
            public GameObject Content;
        }

        [SerializeField] private Tab[] tabs = new Tab[0];
        [SerializeField] private int defaultTab;
        [SerializeField] private Color selectedColor = new Color(0.20f, 0.34f, 0.50f, 1f);
        [SerializeField] private Color unselectedColor = new Color(0.13f, 0.13f, 0.17f, 1f);

        /// <summary>Index of the visible tab, or -1 before the first selection.</summary>
        public int ActiveTab { get; private set; } = -1;

        public event Action<int> TabChanged;

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i; // captured per iteration, not shared across the loop
                Button button = tabs[i].Button;
                if (button != null) button.onClick.AddListener(() => Select(index));
            }

            Select(defaultTab);
        }

        public void Select(int index)
        {
            if (tabs.Length == 0) return;

            index = Mathf.Clamp(index, 0, tabs.Length - 1);
            if (index == ActiveTab) return;

            ActiveTab = index;

            for (int i = 0; i < tabs.Length; i++)
            {
                bool isActive = i == index;

                if (tabs[i].Content != null) tabs[i].Content.SetActive(isActive);

                Button button = tabs[i].Button;
                if (button != null && button.targetGraphic != null)
                {
                    button.targetGraphic.color = isActive ? selectedColor : unselectedColor;
                }
            }

            TabChanged?.Invoke(index);
        }
    }
}
