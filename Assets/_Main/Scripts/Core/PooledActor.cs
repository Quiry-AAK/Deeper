using System.Collections;
using UnityEngine;

namespace Deeper.Core
{
    /// <summary>
    /// Lets a pooled instance put itself away.
    ///
    /// Anything that used to end its own life with <c>Destroy(gameObject)</c> — a dead enemy, a
    /// spent projectile — calls <see cref="Release"/> instead and does not need to know which pool
    /// it came from, or whether it came from one at all.
    ///
    /// **Added by <see cref="ActorPool"/> at runtime, not authored on the prefab.** The pool is the
    /// only thing that can bind it correctly, and a prefab carrying an unbound one would look in the
    /// Inspector like it pools when it does not. Anything instantiated the ordinary way has no
    /// <see cref="PooledActor"/> at all, and the callers below fall back to <c>Destroy</c> — so
    /// pooling stays an optimisation, never a thing you can forget and get a leak from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledActor : MonoBehaviour
    {
        private ActorPool _pool;
        private Coroutine _pending;

        /// <summary>The pool this came from, or null if it was plain-instantiated.</summary>
        public ActorPool Pool { get { return _pool; } }

        internal void Bind(ActorPool pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// Puts this back in its pool immediately, or destroys it when there is no pool.
        /// Safe to call twice — the pool ignores anything already returned.
        /// </summary>
        public void Release()
        {
            CancelPending();

            if (_pool != null) _pool.Release(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>
        /// Releases after <paramref name="delay"/> seconds — a death animation finishing, a
        /// projectile timing out.
        ///
        /// Unscaled, like every other timer in the feel layer: hitstop runs the clock at 2%, so a
        /// scaled 0.45 s corpse would linger for real seconds every time a kill triggered a freeze.
        /// </summary>
        public void Release(float delay)
        {
            if (delay <= 0f) { Release(); return; }

            CancelPending();
            _pending = StartCoroutine(ReleaseAfter(delay));
        }

        private IEnumerator ReleaseAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _pending = null;
            Release();
        }

        private void CancelPending()
        {
            if (_pending == null) return;

            StopCoroutine(_pending);
            _pending = null;
        }

        private void OnDisable()
        {
            // A coroutine on a deactivated object is dead but not forgotten: without this, an
            // instance released early keeps a stale handle and the next life's timed release is
            // cancelled by a coroutine that can never run.
            _pending = null;
        }
    }
}
