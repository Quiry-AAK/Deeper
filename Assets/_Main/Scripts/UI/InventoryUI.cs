using System;
using System.Collections.Generic;
using System.Text;
using Deeper.Equipment;
using Deeper.Player;
using Deeper.Stats;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The inventory screen: five equipped-slot rows on the left, carried gear on the right, and
    /// a live stat readout so the effect of a swap is visible immediately.
    ///
    /// Clicking an equipped row unequips it; clicking a carried row wears it. Both go through
    /// <see cref="EquipmentInventory"/> — this class never touches <see cref="PlayerStats"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Left empty, the first EquipmentInventory in the scene is used.")]
        [SerializeField] private EquipmentInventory inventory;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private bool startVisible = true;
        [SerializeField] private Key toggleKey = Key.I;

        [Header("Equipped rows — one per slot, in EquipmentSlot order")]
        [SerializeField] private InventoryItemButton[] equippedRows = new InventoryItemButton[EquipmentInventory.SlotCount];

        [Header("Carried list")]
        [SerializeField] private RectTransform carriedContent;
        [Tooltip("Inactive row cloned per carried item. Must live under Carried Content.")]
        [SerializeField] private InventoryItemButton carriedRowTemplate;

        [Header("Readout")]
        [SerializeField] private Text statsReadout;

        private static readonly StatType[] AllStats = (StatType[])Enum.GetValues(typeof(StatType));

        private readonly List<InventoryItemButton> _carriedRows = new List<InventoryItemButton>();
        private readonly StringBuilder _readoutBuilder = new StringBuilder(256);
        private PlayerStats _stats;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<EquipmentInventory>();

            if (inventory == null)
            {
                Debug.LogError($"{nameof(InventoryUI)}: no {nameof(EquipmentInventory)} found; the screen will stay empty.", this);
                enabled = false;
                return;
            }

            _stats = inventory.GetComponent<PlayerStats>();

            if (carriedRowTemplate != null) carriedRowTemplate.gameObject.SetActive(false);
            LegacyUIFont.EnsureFont(statsReadout);

            for (int i = 0; i < equippedRows.Length; i++)
            {
                if (equippedRows[i] != null) equippedRows[i].Clicked += OnEquippedRowClicked;
            }

            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            if (inventory == null) return;

            inventory.EquipmentChanged += OnEquipmentChanged;
            inventory.CarriedChanged += Refresh;
            if (_stats != null) _stats.Changed += RefreshReadout;

            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.EquipmentChanged -= OnEquipmentChanged;
                inventory.CarriedChanged -= Refresh;
            }

            if (_stats != null) _stats.Changed -= RefreshReadout;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < equippedRows.Length; i++)
            {
                if (equippedRows[i] != null) equippedRows[i].Clicked -= OnEquippedRowClicked;
            }

            for (int i = 0; i < _carriedRows.Count; i++)
            {
                if (_carriedRows[i] != null) _carriedRows[i].Clicked -= OnCarriedRowClicked;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame) ToggleVisible();
        }

        public void ToggleVisible() => SetVisible(panelRoot == null || !panelRoot.activeSelf);

        public void SetVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);
        }

        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentDefinition previous, EquipmentDefinition current)
        {
            Refresh();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            RefreshEquipped();
            RefreshCarried();
            RefreshReadout();
        }

        private void RefreshEquipped()
        {
            for (int i = 0; i < equippedRows.Length && i < EquipmentInventory.SlotCount; i++)
            {
                InventoryItemButton row = equippedRows[i];
                if (row == null) continue;

                var slot = (EquipmentSlot)i;
                EquipmentDefinition item = inventory.GetEquipped(slot);

                row.Bind(
                    slot,
                    item != null ? item.Icon : null,
                    slot.ToString(),
                    item != null ? item.DisplayName : "— empty —",
                    item != null);
            }
        }

        private void RefreshCarried()
        {
            if (carriedContent == null || carriedRowTemplate == null) return;

            IReadOnlyList<EquipmentDefinition> carried = inventory.Carried;

            while (_carriedRows.Count < carried.Count)
            {
                InventoryItemButton row = Instantiate(carriedRowTemplate, carriedContent);
                row.name = $"CarriedRow_{_carriedRows.Count}";
                row.Clicked += OnCarriedRowClicked;
                _carriedRows.Add(row);
            }

            for (int i = 0; i < _carriedRows.Count; i++)
            {
                InventoryItemButton row = _carriedRows[i];

                if (i >= carried.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                EquipmentDefinition item = carried[i];
                row.gameObject.SetActive(true);
                row.Bind(item, item.Icon, item.DisplayName, item.Slot.ToString(), true);
            }
        }

        private void RefreshReadout()
        {
            if (statsReadout == null || _stats == null) return;

            _readoutBuilder.Clear();
            _readoutBuilder.AppendLine("STATS");

            for (int i = 0; i < AllStats.Length; i++)
            {
                StatType stat = AllStats[i];
                _readoutBuilder.AppendLine($"{stat}: {_stats.Get(stat):0.##}");
            }

            statsReadout.text = _readoutBuilder.ToString();
        }

        private void OnEquippedRowClicked(InventoryItemButton row)
        {
            if (row.Payload is EquipmentSlot slot) inventory.Unequip(slot);
        }

        private void OnCarriedRowClicked(InventoryItemButton row)
        {
            if (row.Payload is EquipmentDefinition item) inventory.Equip(item);
        }
    }
}
