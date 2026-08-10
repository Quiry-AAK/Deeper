namespace Deeper.Equipment
{
    /// <summary>
    /// The five gear slots. Explicit values are stable indices: <see cref="EquipmentInventory"/>
    /// uses them to index its backing array and save data will persist them, so entries must not
    /// be reordered or renumbered once content exists.
    /// </summary>
    public enum EquipmentSlot
    {
        Head = 0,
        Chest = 1,
        Legs = 2,
        Feet = 3,
        Weapon = 4,
    }
}
