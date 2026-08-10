using UnityEngine;

namespace Deeper.Equipment
{
    /// <summary>The three weapons from GDD §Player. Values are stable — do not reorder.</summary>
    public enum WeaponType
    {
        Katana = 0,
        Bow = 1,
        Greatsword = 2,
    }

    /// <summary>
    /// A weapon gear entry. Always occupies <see cref="EquipmentSlot.Weapon"/>.
    ///
    /// This asset is the intended home for the per-weapon Windup/Active/Recovery timing data
    /// (BALANCE.md §2) once the Attack State Machine exists, so <c>GetAttackTiming()</c> reads
    /// authored data instead of hardcoded per-weapon constants. It carries no combat behaviour
    /// yet — equipping a weapon here changes stats and the sprite layer only.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Deeper/Equipment/Weapon", order = 1)]
    public sealed class WeaponDefinition : EquipmentDefinition
    {
        [Header("Weapon")]
        [SerializeField] private WeaponType weaponType = WeaponType.Katana;

        public WeaponType WeaponType => weaponType;

        public override EquipmentSlot Slot => EquipmentSlot.Weapon;

        protected override bool AllowsWeaponSlot => true;

        protected override void OnValidate()
        {
            ForceSlot(EquipmentSlot.Weapon);
            base.OnValidate();
        }
    }
}
