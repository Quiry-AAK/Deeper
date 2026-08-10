using Deeper.Animation;
using Deeper.Stats;
using UnityEngine;

namespace Deeper.Equipment
{
    /// <summary>
    /// One wearable gear entry, authored as an asset so pieces can be added and retuned without
    /// code changes. The asset itself is the item's identity — <see cref="EquipmentInventory"/>
    /// keys stat modifiers off the asset reference, so a given asset can be equipped or carried
    /// only once at a time. Stackable duplicates would need a runtime instance wrapper.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor_", menuName = "Deeper/Equipment/Armor Piece", order = 0)]
    public class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id for save data. Falls back to the asset name when left empty.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;

        [Header("Slot")]
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Chest;

        [Header("Presentation — generated art drops in here later")]
        [Tooltip("Shown in the inventory screen.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Per-state/direction art layered onto the character rig. Preferred over Body Layer.")]
        [SerializeField] private SpriteAnimationSet animationSet;
        [Tooltip("Single static fallback used only when Animation Set is empty.")]
        [SerializeField] private Sprite bodyLayer;

        [Header("Effects")]
        [SerializeField] private StatModifier[] modifiers = new StatModifier[0];

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public SpriteAnimationSet AnimationSet => animationSet;
        public Sprite BodyLayer => bodyLayer;
        public StatModifier[] Modifiers => modifiers;

        public virtual EquipmentSlot Slot => slot;

        /// <summary>Only weapon assets may occupy the Weapon slot.</summary>
        protected virtual bool AllowsWeaponSlot => false;

        protected void ForceSlot(EquipmentSlot value) => slot = value;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;

            if (!AllowsWeaponSlot && slot == EquipmentSlot.Weapon)
            {
                Debug.LogWarning($"{name}: the Weapon slot is reserved for WeaponDefinition assets. Reset to Chest.", this);
                slot = EquipmentSlot.Chest;
            }
        }
    }
}
