using Deeper.Core;
using UnityEngine;

namespace Deeper.Player
{
    /// <summary>
    /// The afterimage streak behind a Dig-Dash — ART_DIRECTION §6 lists "Dig-Dash trail" as
    /// must-have MVP VFX.
    ///
    /// It stamps copies of whatever sprite the body is currently drawing, behind her, fading out.
    /// That is what makes a 0.18s dash legible: the movement alone is over before the eye tracks it,
    /// and the trail is what leaves the shape of where she went.
    ///
    /// **Known duplication, recorded rather than hidden.** <c>AuraVisuals</c> already contains an
    /// afterimage system for the Ultimate buff, but it is coupled to <c>UltimateBuff</c> and
    /// <c>AttackStateMachine</c> — the same coupling ART_DIRECTION §4 flags as blocking the Elite
    /// aura. Deepening that coupling to add a dash would mean rewriting shipped Ultimate code. One
    /// afterimage component both call is the right end state and is a follow-up, not this pass.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DashTrail : MonoBehaviour
    {
        [Header("Look")]
        [Tooltip("Afterimages stamped across the dash. More reads as a smear, fewer as a stutter.")]
        [SerializeField] private int imageCount = 5;

        [Tooltip("Seconds each afterimage takes to fade out. Longer than the dash itself, so the " +
                 "tail is still visible once she has arrived.")]
        [SerializeField] private float fade = 0.22f;

        [Tooltip("Alpha the newest afterimage starts at. She should read as solid and the trail as " +
                 "a ghost — at 1 the trail competes with the character.")]
        [Range(0f, 1f)]
        [SerializeField] private float startAlpha = 0.45f;

        [Tooltip("Tint applied to the afterimages. Cool grey-violet rather than orange-red or " +
                 "cyan-white: ART_DIRECTION §2 reserves both of those for danger telegraphs, and a " +
                 "player movement power reading in hazard colours is exactly the conflict §2 flags " +
                 "on the Ultimate's arcs.")]
        [SerializeField] private Color tint = new Color(0.72f, 0.70f, 0.92f, 1f);

        [Header("Refs — found anywhere on the player rig when empty")]
        [Tooltip("The renderer the afterimages copy. Her body layer — the weapon and cosmetics are " +
                 "deliberately not traced, or the trail turns into unreadable clutter.")]
        [SerializeField] private SpriteRenderer bodyRenderer;

        private SpriteRenderer[] _images;
        private float[] _remaining;
        private Transform _pool;

        private float _dashRemaining;
        private float _nextStampAt;
        private float _stampInterval;
        private int _next;

        private void Awake()
        {
            bodyRenderer = RigRefs.Find(this, bodyRenderer);

            // A loose root rather than a child, for the same reason HitVFX does it: afterimages are
            // world-space marks of where she *was*, and parenting them to her would drag the whole
            // trail along behind her as she moves.
            var holder = new GameObject("DashTrail Pool");
            _pool = holder.transform;

            int count = Mathf.Max(1, imageCount);
            _images = new SpriteRenderer[count];
            _remaining = new float[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Afterimage");
                go.transform.SetParent(_pool, false);

                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                _images[i] = renderer;
            }
        }

        private void OnDestroy()
        {
            if (_pool != null) Destroy(_pool.gameObject);
        }

        /// <summary>Starts stamping afterimages for the length of a dash.</summary>
        public void Play(float dashDuration)
        {
            if (dashDuration <= 0f || bodyRenderer == null) return;

            _dashRemaining = dashDuration;
            _stampInterval = dashDuration / Mathf.Max(1, imageCount);
            _nextStampAt = 0f;
        }

        private void Update()
        {
            // Unscaled throughout: a dash into a hitstop freeze would otherwise leave its trail
            // hanging in the air for real seconds while the game ran at 2% speed.
            float dt = Time.unscaledDeltaTime;

            if (_dashRemaining > 0f)
            {
                _dashRemaining -= dt;
                _nextStampAt -= dt;

                if (_nextStampAt <= 0f)
                {
                    Stamp();
                    _nextStampAt = _stampInterval;
                }
            }

            for (int i = 0; i < _images.Length; i++)
            {
                if (_remaining[i] <= 0f) continue;

                _remaining[i] -= dt;

                if (_remaining[i] <= 0f)
                {
                    _images[i].enabled = false;
                    continue;
                }

                Color c = tint;
                c.a = startAlpha * Mathf.Clamp01(_remaining[i] / fade);
                _images[i].color = c;
            }
        }

        private void Stamp()
        {
            if (bodyRenderer == null || bodyRenderer.sprite == null) return;

            // Ring buffer, oldest reused — the same shape HitVFX uses. With the pool sized to the
            // image count there is nothing to steal in practice.
            int slot = _next;
            _next = (_next + 1) % _images.Length;

            SpriteRenderer image = _images[slot];
            image.sprite = bodyRenderer.sprite;
            image.flipX = bodyRenderer.flipX;

            // Copied from the rig's SortingGroup, not from the body renderer, and that distinction
            // is the whole thing: inside a SortingGroup a renderer keeps its own authored layer and
            // order (Default/0 here) while the GROUP is what actually draws (Actors, at the order
            // YDepthSort is driving from her feet). Reading the renderer put the trail on Default
            // at order -1 — above the Walls tilemap at -10, so a dash past a wall drew the trail
            // on top of it, and below every enemy rather than interleaved with them by depth.
            //
            // One order behind the group, so the trail never draws over the character it trails.
            var group = bodyRenderer.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
            image.sortingLayerID = group != null ? group.sortingLayerID : bodyRenderer.sortingLayerID;
            image.sortingOrder = (group != null ? group.sortingOrder : bodyRenderer.sortingOrder) - 1;

            image.transform.SetPositionAndRotation(bodyRenderer.transform.position,
                                                   bodyRenderer.transform.rotation);
            image.transform.localScale = bodyRenderer.transform.lossyScale;

            Color c = tint;
            c.a = startAlpha;
            image.color = c;

            image.enabled = true;
            _remaining[slot] = fade;
        }
    }
}
