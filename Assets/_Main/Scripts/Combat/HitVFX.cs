using Deeper.Character;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Flashes a burst at the point of contact. This is ART_DIRECTION §6's "weapon hit-flash", and it
    /// fires on <see cref="AttackHitbox.Landed"/> rather than on the swing — so a whiff shows nothing,
    /// matching hitstop, camera shake and gauge fill.
    ///
    /// One base sprite tinted and scaled per action, exactly as ART_DIRECTION §6 allows ("can share a
    /// base flash effect tinted per weapon color"), rather than one asset per weapon per action. The
    /// burst is deliberately pale gold and white: Upper Caves reserves orange-red for hazards
    /// (§2), so a warm-red impact would read as *danger* instead of as a landed hit.
    ///
    /// Note this is the opposite of the Combo Counter's rule — <see cref="AttackHitbox.Landed"/> is
    /// raised once per target, and every target should flash, so nothing here dedupes per swing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitVFX : MonoBehaviour
    {
        [Header("Art")]
        [Tooltip("The base burst. Authored white/pale-gold so it can be tinted per weapon.")]
        [SerializeField] private Sprite burst;

        [Tooltip("Multiplied into the burst. The per-weapon tint hook — white leaves it as authored.")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("Sorting layer for the flash. Kept off the actors' layer on purpose: YDepthSort " +
                 "drives actor sorting orders from their Y, so an actor low enough on screen would " +
                 "otherwise out-sort a fixed order and swallow the flash.")]
        [SerializeField] private string sortingLayer = "Overlay";

        [Tooltip("Order within the sorting layer above.")]
        [SerializeField] private int sortingOrder = 25;

        [Header("Size per action — heavier hits read bigger")]
        [Tooltip("Scale for a Basic Attack. The sprite is 64px at 32 PPU, so 1.0 covers 2 world " +
                 "units — far too big for a light hit, hence the fractions.")]
        [SerializeField] private float basicScale = 0.5f;
        [SerializeField] private float heavyScale = 0.85f;
        [SerializeField] private float ultimateScale = 1.05f;
        [SerializeField] private float dashAttackScale = 0.62f;

        [Header("Duration per action — heavier hits linger")]
        [Tooltip("Seconds the flash stays up for a Basic Attack. Short: a flash should punctuate, " +
                 "not hang around.")]
        [SerializeField] private float basicLifetime = 0.10f;
        [SerializeField] private float heavyLifetime = 0.16f;
        [SerializeField] private float ultimateLifetime = 0.22f;
        [SerializeField] private float dashAttackLifetime = 0.12f;

        [Header("Feel")]
        [Tooltip("Scale the burst starts at, as a fraction of full. Popping open from small is what " +
                 "makes it read as an impact rather than as a decal fading in.")]
        [SerializeField, Range(0.1f, 1f)] private float popFrom = 0.55f;

        [Tooltip("Fraction of the lifetime spent growing. The rest holds full size and fades.")]
        [SerializeField, Range(0.05f, 1f)] private float popFraction = 0.35f;

        [Tooltip("Random spin per flash, so repeated hits on the same enemy never look stamped.")]
        [SerializeField] private bool randomRotation = true;

        [Tooltip("Simultaneous flashes. One swing can catch several enemies, and each gets its own.")]
        [SerializeField, Range(1, 16)] private int poolSize = 6;

        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private AttackHitbox hitbox;

        private sealed class Flash
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Remaining;
            public float Lifetime;
            public float Scale;
        }

        private Flash[] _pool;
        private int _next;
        private Transform _container;

        private void Awake()
        {
            hitbox = RigRefs.Find(this, hitbox);

            // The pool lives OUTSIDE the player rig on purpose. Parented to the Combat group these
            // would ride along as she lunges away, dragging the impact off the enemy it belongs to;
            // a flash has to stay where the hit happened.
            _container = new GameObject("HitVFX Pool").transform;

            _pool = new Flash[Mathf.Max(1, poolSize)];
            for (int i = 0; i < _pool.Length; i++)
            {
                var go = new GameObject("HitFlash" + i);
                go.transform.SetParent(_container, false);

                var flash = new Flash();
                flash.Transform = go.transform;
                flash.Renderer = go.AddComponent<SpriteRenderer>();
                flash.Renderer.sprite = burst;
                flash.Renderer.sortingLayerName = sortingLayer;
                flash.Renderer.sortingOrder = sortingOrder;
                flash.Renderer.enabled = false;
                _pool[i] = flash;
            }
        }

        private void OnEnable()
        {
            if (hitbox != null) hitbox.Landed += HandleLanded;
        }

        private void OnDisable()
        {
            if (hitbox != null) hitbox.Landed -= HandleLanded;
        }

        private void OnDestroy()
        {
            // The pool is not a child of this rig, so it will not be cleaned up with it.
            if (_container != null) Destroy(_container.gameObject);
        }

        private float ScaleFor(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basicScale;
                case AttackAction.Heavy: return heavyScale;
                case AttackAction.DashAttack: return dashAttackScale;
                default: return ultimateScale;
            }
        }

        private float LifetimeFor(AttackAction action)
        {
            switch (action)
            {
                case AttackAction.Basic: return basicLifetime;
                case AttackAction.Heavy: return heavyLifetime;
                case AttackAction.DashAttack: return dashAttackLifetime;
                default: return ultimateLifetime;
            }
        }

        private void HandleLanded(AttackAction action, Damageable target, float amount)
        {
            if (burst == null || target == null) return;

            float scale = ScaleFor(action);
            float lifetime = LifetimeFor(action);

            if (lifetime <= 0f) return;

            // The centre of the DRAWN body, not the collider and not the transform origin. The origin
            // sits at an actor's feet, and in a top-down game the collider is a foot-level footprint
            // — anchoring to either puts the flash on the enemy's shins. Measured on the training
            // dummy: the collider centre lands 11px up a 48px sprite, i.e. in its bottom quarter.
            Vector3 point = target.transform.position;
            var drawn = target.GetComponentInChildren<SpriteRenderer>();
            if (drawn != null)
            {
                point = drawn.bounds.center;
            }
            else
            {
                var body = target.GetComponentInChildren<Collider2D>();
                if (body != null) point = body.bounds.center;
            }

            Flash flash = _pool[_next];
            _next = (_next + 1) % _pool.Length;

            flash.Transform.position = point;
            flash.Transform.rotation = randomRotation
                ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                : Quaternion.identity;

            flash.Scale = scale;
            flash.Lifetime = lifetime;
            flash.Remaining = lifetime;
            flash.Transform.localScale = Vector3.one * (scale * popFrom);
            flash.Renderer.sprite = burst;
            flash.Renderer.color = tint;
            flash.Renderer.sortingLayerName = sortingLayer;
            flash.Renderer.sortingOrder = sortingOrder;
            flash.Renderer.enabled = true;
        }

        private void Update()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                Flash flash = _pool[i];
                if (flash.Remaining <= 0f) continue;

                // Unscaled, so the flash is visible through the hitstop freeze that fires with it.
                // On scaled time the freeze would hold it on one frame and it would vanish the
                // instant the game resumed — the exact moment the player looks at it.
                flash.Remaining -= Time.unscaledDeltaTime;

                if (flash.Remaining <= 0f)
                {
                    flash.Renderer.enabled = false;
                    continue;
                }

                float elapsed = 1f - (flash.Remaining / flash.Lifetime);

                float grow = Mathf.Clamp01(elapsed / popFraction);
                flash.Transform.localScale = Vector3.one * (flash.Scale * Mathf.Lerp(popFrom, 1f, grow));

                Color c = tint;
                c.a = tint.a * (1f - elapsed);
                flash.Renderer.color = c;
            }
        }
    }
}
