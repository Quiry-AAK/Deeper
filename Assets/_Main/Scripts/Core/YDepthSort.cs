using UnityEngine;
using UnityEngine.Rendering;

namespace Deeper.Core
{
    /// <summary>
    /// Draws whoever is lower on screen in front, so overlapping actors read as having depth.
    ///
    /// This drives <see cref="SortingGroup.sortingOrder"/> from the actor's **root** position, which
    /// is at its feet. The project-wide Custom Axis transparency sort (Graphics settings) already
    /// sorts by Y, but a Sorting Group takes its distance from the **combined bounds of its
    /// renderers** — the middle of the sprite, not the feet. Measured: with her root at y=9.5, well
    /// behind a dummy at y=8.5, she still drew in front, because her artwork's centre sat lower.
    /// That difference is invisible today only because every rig happens to offset its art by the
    /// same +0.75; the first enemy drawn at a different height would sort as if it were standing
    /// somewhere it is not.
    ///
    /// Sorting order beats the sort axis, so setting it here is what makes the result exact rather
    /// than approximate — and inspectable, since the resulting order is visible in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SortingGroup))]
    public sealed class YDepthSort : MonoBehaviour
    {
        [Tooltip("World units per sorting step. Actors within one step of each other count as being " +
                 "on the same level, and Priority decides who is in front. A band rather than exact " +
                 "float equality is deliberate: two actors standing side by side are never going to " +
                 "share a Y to the last decimal, but they should still read as level.")]
        [SerializeField] private float step = 0.1f;

        [Tooltip("Wins ties against anything on the same level. The player is 1 and enemies are 0, " +
                 "so she is never swallowed by something she is standing level with.")]
        [SerializeField] private int priority;

        [SerializeField] private SortingGroup group;

        private int _lastOrder = int.MinValue;

        private void Awake()
        {
            if (group == null) group = GetComponent<SortingGroup>();
        }

        // LateUpdate, so this runs after movement has settled for the frame — sorting from a
        // position the actor has already left produces a one-frame flicker on every direction change.
        private void LateUpdate()
        {
            if (group == null) return;

            // Doubled so there is a free slot between adjacent levels for Priority to occupy without
            // colliding with the next step down.
            int order = -Mathf.RoundToInt(transform.position.y / Mathf.Max(0.0001f, step)) * 2 + priority;
            if (order == _lastOrder) return;

            _lastOrder = order;
            group.sortingOrder = order;
        }
    }
}
