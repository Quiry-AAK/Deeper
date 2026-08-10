using System;
using System.Collections.Generic;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Equipment
{
    /// <summary>
    /// Holds the player's five equipped slots plus a carried list, and keeps
    /// <see cref="PlayerStats"/> in sync as gear moves between them.
    ///
    /// Equipping into an occupied slot swaps: the displaced piece returns to the carried list
    /// rather than being destroyed, so no item is ever lost by equipping over it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class EquipmentInventory : MonoBehaviour
    {
        public const int SlotCount = 5;

        [Tooltip("Equipped in order on Awake. Pieces displaced by a later entry fall into Carried.")]
        [SerializeField] private EquipmentDefinition[] startingEquipment = new EquipmentDefinition[0];

        [Tooltip("Gear held but not worn.")]
        [SerializeField] private List<EquipmentDefinition> carried = new List<EquipmentDefinition>();

        private readonly EquipmentDefinition[] _equipped = new EquipmentDefinition[SlotCount];
        private PlayerStats _stats;

        /// <summary>Raised as (slot, previous item or null, new item or null).</summary>
        public event Action<EquipmentSlot, EquipmentDefinition, EquipmentDefinition> EquipmentChanged;

        /// <summary>Raised whenever the carried list gains or loses an entry.</summary>
        public event Action CarriedChanged;

        public IReadOnlyList<EquipmentDefinition> Carried => carried;

        private PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());

        public EquipmentDefinition GetEquipped(EquipmentSlot slot) => _equipped[(int)slot];

        public bool IsEquipped(EquipmentDefinition item)
        {
            if (item == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_equipped[i] == item) return true;
            }

            return false;
        }

        private void Awake()
        {
            for (int i = 0; i < startingEquipment.Length; i++)
            {
                Equip(startingEquipment[i]);
            }
        }

        /// <summary>
        /// Wears <paramref name="item"/> in its own slot. Any piece already there is unequipped
        /// into the carried list first. Returns false if the item is null or already worn.
        /// </summary>
        public bool Equip(EquipmentDefinition item)
        {
            if (item == null) return false;

            EquipmentSlot slot = item.Slot;
            EquipmentDefinition previous = _equipped[(int)slot];
            if (previous == item) return false;

            if (previous != null)
            {
                Stats.RemoveSource(previous);
                carried.Add(previous);
            }

            _equipped[(int)slot] = item;
            Stats.SetSource(item, item.Modifiers);
            carried.Remove(item);

            EquipmentChanged?.Invoke(slot, previous, item);
            CarriedChanged?.Invoke();
            return true;
        }

        /// <summary>Removes whatever is in <paramref name="slot"/> into the carried list.</summary>
        public EquipmentDefinition Unequip(EquipmentSlot slot)
        {
            EquipmentDefinition previous = _equipped[(int)slot];
            if (previous == null) return null;

            _equipped[(int)slot] = null;
            Stats.RemoveSource(previous);
            carried.Add(previous);

            EquipmentChanged?.Invoke(slot, previous, null);
            CarriedChanged?.Invoke();
            return previous;
        }

        /// <summary>Picks an item up into the carried list without wearing it.</summary>
        public bool AddToCarried(EquipmentDefinition item)
        {
            if (item == null || carried.Contains(item) || IsEquipped(item)) return false;

            carried.Add(item);
            CarriedChanged?.Invoke();
            return true;
        }

        /// <summary>Drops an item that is being carried. Worn gear must be unequipped first.</summary>
        public bool RemoveFromCarried(EquipmentDefinition item)
        {
            if (item == null || !carried.Remove(item)) return false;

            CarriedChanged?.Invoke();
            return true;
        }

        [ContextMenu("Log Equipment")]
        private void LogEquipment()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                EquipmentDefinition item = _equipped[i];
                Debug.Log($"{(EquipmentSlot)i}: {(item != null ? item.DisplayName : "<empty>")}", this);
            }

            Debug.Log($"Carried: {carried.Count}", this);
        }
    }
}
