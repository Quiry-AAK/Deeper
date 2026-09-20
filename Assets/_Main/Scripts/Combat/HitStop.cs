using System.Collections;
using Deeper.Core;
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
    ///
    /// <b>Refuses to start while <see cref="RunPause"/> is holding.</b> Found live, 2026-09-17: a
    /// killing blow that levels the player up fires <c>Damageable.Died</c> — which cascades through
    /// <c>XPReward</c> → <c>PlayerXP.Add</c> → <c>UpgradeOffer.Open</c> → <c>RunPause.Push</c>,
    /// synchronously, all before <c>AttackHitbox.Sweep</c>'s own <c>TakeDamage</c> call returns —
    /// and only afterward does <c>AttackHitbox</c> fire <c>Landed</c>, which is what reaches
    /// <c>AttackStateMachine.HandleLanded</c> and calls <see cref="Freeze(float)"/> for that same
    /// hit. Without the guard below, that freeze starts AFTER the pause and stomps its
    /// <c>Time.timeScale = 0</c> with <see cref="frozenScale"/>; ~50ms later <see cref="Run"/> ends
    /// and hands the game back to <see cref="normalScale"/> = 1 while the offer panel is still open
    /// and <c>RunPause</c> still thinks it holds — the game visibly un-pauses itself underneath the
    /// panel. <c>RunPause.Push</c>'s own <see cref="Cancel"/> call only protects the other direction
    /// (a freeze from an EARLIER hit still in flight when the pause starts); it does nothing for one
    /// that starts a moment later in the same call chain, from the very hit that caused the pause.
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

        [Header("Refs — found in the scene when empty")]
        [Tooltip("RunPause is a scene object, not part of the player rig, so this cannot be wired " +
                 "in the Inspector on the prefab — resolved by FindFirstObjectByType the same way " +
                 "UpgradeOffer and TestConfigHUD resolve their own RunPause reference. Checked by " +
                 "Freeze() so a killing blow's hitstop can never stomp a pause the same hit caused " +
                 "— see the class doc comment.")]
        [SerializeField] private RunPause pause;

        private Coroutine _running;
        private float _endsAt;

        private void OnEnable()
        {
            if (pause == null) pause = FindFirstObjectByType<RunPause>();

            // Self-heal a stuck freeze. A domain reload — which is what happens whenever scripts
            // recompile while the editor is in play mode — kills the running coroutine without
            // running its restore, leaving the whole game permanently in slow motion with no
            // freeze active to end it. Anything at or below the frozen scale here is that state,
            // because nothing else sets a scale that low.
            // The `> 0f` guard is not redundant: a deliberate hard pause (RunPause) sets exactly 0,
            // which also satisfies "at or below the frozen scale". Without it, anything that
            // re-enables this component while a modal panel is open — a scene load, a recompile —
            // resumes the game underneath the panel.
            if (Time.timeScale > 0f && Time.timeScale <= frozenScale + 0.0001f) Time.timeScale = normalScale;
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

            // RunPause already owns Time.timeScale while it holds — a freeze started here would
            // overwrite its 0 now and hand the game back to normalScale when it ends, seconds
            // before the panel actually closes. See the class doc comment for the exact ordering
            // that makes this reachable from a perfectly ordinary killing blow.
            if (pause != null && pause.IsPaused) return;

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

        /// <summary>
        /// Ends any freeze now and returns time to normal. <c>RunPause</c> calls this on the way into
        /// a pause: this coroutine runs on unscaled time, so a freeze still in flight would finish
        /// during the pause and write <c>normalScale</c> over the zero, resuming the game behind the
        /// open panel.
        /// </summary>
        public void Cancel()
        {
            if (_running == null) return;

            StopCoroutine(_running);
            _running = null;
            Time.timeScale = normalScale;
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
            Cancel();
        }
    }
}
