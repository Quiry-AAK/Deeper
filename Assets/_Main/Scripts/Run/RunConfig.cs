using Deeper.Character;
using UnityEngine;

namespace Deeper.Run
{
    /// <summary>
    /// What the Hub decided, carried into the run.
    ///
    /// A run is one weapon chosen in the Hub and locked for the descent (GDD §Player,
    /// CORE_SYSTEMS §1). The Hub and the run are different scenes, so that choice has to survive a
    /// scene load — and <see cref="Deeper.Character.RunLoadout"/> cannot carry it, because it lives
    /// on the player and the run scene instantiates its own.
    ///
    /// **An asset rather than a static field or a DontDestroyOnLoad object.** A static is invisible
    /// in the Inspector, which is the one thing this project asks of a reference (CLAUDE.md), and a
    /// survivor object is a second Player-adjacent thing to keep alive and reason about. An asset is
    /// just visible state you can look at while the game runs.
    ///
    /// **Known Unity behaviour, relied on deliberately:** a runtime write to a ScriptableObject
    /// sticks in the editor for the session and is discarded in a build. That is the behaviour we
    /// want in both places — in the editor, pressing Play straight into the run scene replays the
    /// last weapon picked in the Hub instead of forcing a Hub trip; in a build, every launch starts
    /// from the authored default and the player always comes through the Hub anyway. It is *not* a
    /// save file: permanent progression (Shards, Hub stat ranks) is Milestone 6's `SaveData` and
    /// does not belong here.
    /// </summary>
    [CreateAssetMenu(fileName = "RunConfig", menuName = "Deeper/Run/Run Config", order = 10)]
    public sealed class RunConfig : ScriptableObject
    {
        [Tooltip("The weapon the Hub's rack last locked in. Also the fallback when the run scene " +
                 "is played directly without a Hub trip.")]
        [SerializeField] private WeaponDefinition weapon;

        [Tooltip("Every weapon the rack offers, in display order. All three are unlocked from the " +
                 "start — GDD §Player is explicit that there is no weapon gating.")]
        [SerializeField] private WeaponDefinition[] available = new WeaponDefinition[0];

        /// <summary>The run's weapon. Null only if nothing has ever been chosen and no default was authored.</summary>
        public WeaponDefinition Weapon { get { return weapon; } }

        /// <summary>What the weapon rack lists.</summary>
        public WeaponDefinition[] Available { get { return available; } }

        /// <summary>
        /// Locks in the Hub's choice. Called by the weapon rack, before the descent — never
        /// mid-run, which would break the weapon lock the upgrade sub-pools and the boss
        /// weapon-checks are built on.
        /// </summary>
        public void Choose(WeaponDefinition value)
        {
            if (value == null) return;
            weapon = value;
        }
    }
}
