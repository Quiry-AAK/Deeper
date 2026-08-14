using UnityEngine;

namespace Deeper.Core
{
    /// <summary>
    /// Finds another part of a rig when an inspector slot was left empty.
    ///
    /// The player's scripts are grouped onto child objects — Visual, Combat, Aim — so a plain
    /// <c>GetComponent</c> only ever sees the group it was called from, and the Combat group asking
    /// for the animator on Visual would come back empty. Every reference is wired in the prefab, so
    /// this is a safety net rather than the normal path; what it buys is that moving a script from
    /// one group to another stays a drag in the Hierarchy instead of a code change.
    /// </summary>
    public static class RigRefs
    {
        /// <summary>
        /// Returns <paramref name="assigned"/> when it is set, otherwise searches the whole rig
        /// <paramref name="from"/> belongs to. Returns null if the rig has no such component — the
        /// caller decides whether that is fatal, since some references are genuinely optional.
        /// </summary>
        public static T Find<T>(Component from, T assigned) where T : Component
        {
            if (assigned != null) return assigned;
            if (from == null) return null;

            // Searched from the top of the rig down so sibling groups are reachable, and including
            // inactive objects because a disabled group is still part of the rig.
            return from.transform.root.GetComponentInChildren<T>(true);
        }
    }
}
