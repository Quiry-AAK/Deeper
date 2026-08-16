using UnityEngine;

namespace Deeper.Rooms
{
    /// <summary>
    /// The mark that plays where an enemy is about to appear.
    ///
    /// Enemies used to arrive on a single frame with no warning, which made a wave — and especially
    /// a Wave Room's second batch — an ambush rather than something you could read. This gives the
    /// player the beat to react that LEVEL_DESIGN §4 asks spawn placement to preserve.
    ///
    /// **This is not in any design doc.** ART_DIRECTION §6's MVP VFX list is hit-flash, gauge pulse,
    /// environmental telegraphs, Dig-Dash trail and the upgrade/curse flashes — a spawn effect is
    /// new scope, recorded in the change brief rather than assumed.
    ///
    /// Built as its own component rather than reusing <c>HitVFX</c>: that class is sealed, has no
    /// public play method, and its pool is a *stealing* ring buffer — a spawn effect sharing it
    /// would evict hit flashes in the middle of a fight, which is the one effect that must never
    /// drop a frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnTelegraph : MonoBehaviour
    {
        [Header("Look")]
        [Tooltip("Frames played once, in order, over the effect's lifetime. A single sprite is " +
                 "fine — it will just scale and fade.")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("Tint applied to every frame. Owner-directed: a spawn marker reads as a danger " +
                 "tell in the same class as an enemy attack tell, so it may use ART_DIRECTION §2's " +
                 "reserved orange-red accent. That widens what §2's reservation covers and is " +
                 "recorded in the change brief for the designer to confirm.")]
        [SerializeField] private Color tint = new Color(1f, 0.44f, 0.22f, 1f);

        [Tooltip("Seconds the mark is on screen. Should match the spawner's delay, or the enemy " +
                 "arrives before the warning has finished playing.")]
        [SerializeField] private float lifetime = 0.5f;

        [Tooltip("Scale at the start, growing to 1 across the lifetime. Under 1 it reads as " +
                 "something surfacing; over 1 as something collapsing inward.")]
        [SerializeField] private float startScale = 0.4f;

        [Header("Sorting")]
        [Tooltip("Default, not Overlay — this is a mark on the ground, so the player and the " +
                 "enemies must draw over it. Overlay would put a floor decal on top of everyone " +
                 "standing on it.")]
        [SerializeField] private string sortingLayer = "Default";

        [Tooltip("Above the Floor (-20) and Walls (-10) tilemaps, below anything on Actors.")]
        [SerializeField] private int sortingOrder;

        [Tooltip("How many marks can play at once. A wave spawns six at the same instant, so " +
                 "anything less than the largest batch will drop marks.")]
        [SerializeField] private int poolSize = 8;

        private SpriteRenderer[] _pool;
        private float[] _remaining;
        private Transform _holder;
        private int _next;

        private void Awake()
        {
            // A loose root, like HitVFX's pool: a mark belongs to the place it was played, not to
            // whatever spawned it, and parenting would drag it around if the spawner ever moved.
            var holder = new GameObject("SpawnTelegraph Pool");
            _holder = holder.transform;

            int count = Mathf.Max(1, poolSize);
            _pool = new SpriteRenderer[count];
            _remaining = new float[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Telegraph");
                go.transform.SetParent(_holder, false);

                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                _pool[i] = renderer;
            }
        }

        private void OnDestroy()
        {
            if (_holder != null) Destroy(_holder.gameObject);
        }

        /// <summary>How long a mark plays for. The spawner reads this so the two cannot drift.</summary>
        public float Lifetime { get { return lifetime; } }

        /// <summary>Plays a mark at a world position. Does nothing if no frames are assigned.</summary>
        public void Play(Vector3 position)
        {
            if (frames == null || frames.Length == 0 || _pool == null) return;

            int slot = _next;
            _next = (_next + 1) % _pool.Length;

            SpriteRenderer mark = _pool[slot];
            mark.transform.position = position;
            mark.transform.localScale = Vector3.one * startScale;
            mark.sprite = frames[0];
            mark.color = tint;
            mark.enabled = true;

            _remaining[slot] = lifetime;
        }

        private void Update()
        {
            // Unscaled, like every other timer in the feel layer: a kill landing during the spawn
            // delay freezes scaled time to 2%, and a mark on scaled time would hang there for real
            // seconds after the enemy had already arrived.
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_remaining[i] <= 0f) continue;

                _remaining[i] -= dt;

                if (_remaining[i] <= 0f)
                {
                    _pool[i].enabled = false;
                    continue;
                }

                float elapsed = 1f - Mathf.Clamp01(_remaining[i] / lifetime);

                // Frame index tracks progress rather than a fixed fps, so the sheet always plays
                // exactly once across the lifetime however many frames it has.
                int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * frames.Length));
                _pool[i].sprite = frames[frame];

                _pool[i].transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, elapsed);

                Color c = tint;
                c.a = tint.a * (1f - elapsed * elapsed);   // holds, then drops away at the end
                _pool[i].color = c;
            }
        }
    }
}
