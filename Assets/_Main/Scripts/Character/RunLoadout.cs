using System;
using Deeper.Player;
using UnityEngine;

namespace Deeper.Character
{
    /// <summary>
    /// The one choice a run is made of: which weapon it is played with. Picked in the Hub before
    /// descending and locked until the run ends — that lock is a locked design decision (GDD
    /// §Player, CORE_SYSTEMS §1), and it is what lets upgrades, the Ultimate Gauge and boss
    /// weapon-checks assume a stable weapon for the whole descent.
    ///
    /// This deliberately replaces the old inventory: there is no carried list, no runtime
    /// swapping, and no armor. The protagonist is a single fixed character, so there is no body
    /// choice either. Cosmetic gear is planned post-launch and would layer on top of this without
    /// reintroducing mid-run swapping.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class RunLoadout : MonoBehaviour
    {
        [Tooltip("Weapon chosen in the Hub. Locked for the whole run.")]
        [SerializeField] private WeaponDefinition weapon;

        private PlayerStats _stats;

        /// <summary>Raised whenever the weapon is set, so views can re-resolve their art.</summary>
        public event Action LoadoutChanged;

        public WeaponDefinition Weapon => weapon;

        private PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());

        private void Awake()
        {
            // Re-apply whatever was authored in the inspector so stats match the starting loadout
            // even when no Hub screen ran (test scenes, playing the room scene directly).
            if (weapon != null) Stats.SetSource(weapon, weapon.Modifiers);
        }

        /// <summary>
        /// Sets the weapon for the coming run. Intended to be called from the Hub before the
        /// descent starts — calling it mid-run would break the weapon lock that the upgrade pools
        /// and boss weapon-checks are built on.
        /// </summary>
        public void SetWeapon(WeaponDefinition value)
        {
            if (weapon == value) return;

            if (weapon != null) Stats.RemoveSource(weapon);
            weapon = value;
            if (weapon != null) Stats.SetSource(weapon, weapon.Modifiers);

            LoadoutChanged?.Invoke();
        }

        [ContextMenu("Log Loadout")]
        private void LogLoadout()
        {
            Debug.Log($"Weapon: {(weapon != null ? weapon.DisplayName : "<none>")}", this);
        }
    }
}
