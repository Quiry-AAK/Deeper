using System.Collections;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Freezes time for a few tens of milliseconds on impact. This is the single largest
    /// contributor to a melee hit feeling like it connected with something solid — without it a
    /// fast weapon reads as swinging through air no matter how good the animation is.
    ///
    /// Runs on unscaled time so the freeze can un-freeze itself, and refuses to stack: a longer
    /// request extends an active freeze rather than queueing a second one, so a 3-hit chain does
    /// not compound into a visible stall.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStop : MonoBehaviour
    {
        [Tooltip("Seconds of freeze for a normal hit. 0.04-0.08 is the usual range; longer starts " +
                 "reading as lag rather than impact.")]
        [SerializeField] private float defaultDuration = 0.05f;

        [Tooltip("Time scale during the freeze. 0 is a hard stop; a small value keeps a hint of motion.")]
        [SerializeField, Range(0f, 0.5f)] private float frozenScale = 0.02f;

        [Tooltip("Turn off to disable hitstop wholesale while tuning other feel systems.")]
        [SerializeField] private bool enableHitStop = true;

        [Tooltip("Time scale to return to. A FIXED value, deliberately — see Freeze().")]
        [SerializeField] private float normalScale = 1f;

        private Coroutine _running;
        private float _endsAt;

        private void OnEnable()
        {
            // Self-heal a stuck freeze. A domain reload — which is what happens whenever scripts
            // recompile while the editor is in play mode — kills the running coroutine without
            // running its restore, leaving the whole game permanently in slow motion with no
            // freeze active to end it. Anything at or below the frozen scale here is that state,
            // because nothing else sets a scale that low.
            if (Time.timeScale <= frozenScale + 0.0001f) Time.timeScale = normalScale;
        }

        /// <summary>Freezes for the default duration.</summary>
        public void Freeze()
        {
            Freeze(defaultDuration);
        }

        /// <summary>Freezes for <paramref name="duration"/> seconds of real time.</summary>
        public void Freeze(float duration)
        {
            if (!enableHitStop || duration <= 0f) return;

            float wouldEndAt = Time.unscaledTime + duration;

            if (_running != null)
            {
                // Extend rather than restart, so chained hits never shorten an active freeze.
                if (wouldEndAt > _endsAt) _endsAt = wouldEndAt;
                return;
            }

            // Restore to a FIXED normal, never to whatever the scale happens to be right now.
            // Sampling it looks harmless and is not: if anything else has already frozen time —
            // a second HitStop in the scene, a pause, another effect — this captures the FROZEN
            // value and then "restores" the game to it permanently. That is a slow-motion bug
            // that survives until the scene is reloaded, and it is very hard to trace back here.
            _endsAt = wouldEndAt;
            _running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Time.timeScale = frozenScale;

            while (Time.unscaledTime < _endsAt)
            {
                yield return null;
            }

            Time.timeScale = normalScale;
            _running = null;
        }

        private void OnDisable()
        {
            // Never leave the game frozen because this object went away mid-freeze.
            if (_running == null) return;

            StopCoroutine(_running);
            _running = null;
            Time.timeScale = normalScale;
        }
    }
}
