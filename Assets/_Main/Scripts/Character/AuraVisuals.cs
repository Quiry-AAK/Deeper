using Deeper.Combat;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Character
{
    /// <summary>
    /// The Ultimate's aura: procedural pixel-art flame, plus the afterimage trail her attacks
    /// leave while it burns.
    ///
    /// One extra sprite per layer — one for her body, one for the katana. Each is a copy of the
    /// sprite the rig is already drawing, scaled up for reach, rendered through
    /// <c>Materials/AuraSilhouette.shader</c>, which uses only the shape and generates the fire
    /// itself: noise multiplied by a radial particle falloff, scrolling upward, snapped to the
    /// game's pixel grid.
    ///
    /// Nothing here needs art, and it stays correct for every pose, frame and facing — including
    /// animations added later.
    ///
    /// Approaches built and removed, recorded so none is retried: a ring of offset sprite copies
    /// (dilates and fills in, reading as a blob, and cost 84 renderers), baked silhouette-edge
    /// sheets (popped frame to frame on 4-frame pixel animation), a stack of three eroded layers
    /// (superseded by the radial falloff, which does the same job in one), and a pre-rendered
    /// flame loop (`auraflame.py` still generates `VFX/Aura_Flame.png` if it is wanted again).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AuraVisuals : MonoBehaviour
    {
        [Header("Refs — found anywhere on the player rig when empty")]
        [SerializeField] private UltimateBuff buff;
        [SerializeField] private AttackStateMachine attacks;

        [Tooltip("The renderer drawing her body. The aura and trail both copy whatever sprite " +
                 "this is showing.\n\n" +
                 "During an attack the blade is baked into this sprite, so the body aura already " +
                 "covers the katana — which is why the weapon layer needs no aura of its own.")]
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Header("Aura")]
        [Tooltip("Material using Deeper/AuraSilhouette. Assign the asset so every flame setting — " +
                 "Radial Falloff and Coverage (how far out the fire reaches before it fades), " +
                 "Flame Size, Rise Speed, Erosion, Colour Steps — is tweakable in the inspector " +
                 "and live in play mode.\n\n" +
                 "Left empty, one is built at runtime from shader defaults, which works but hides " +
                 "all of those knobs.")]
        [SerializeField] private Material auraMaterial;

        [SerializeField] private Color auraColour = new Color(0.85f, 0.24f, 0.92f, 1f);

        [Tooltip("SIZE OF THE AURA SPRITE, as a multiple of her own. 1 = exactly her size, 3 = " +
                 "three times as big. This is the knob for how large the aura is.\n\n" +
                 "The shader's Radial Falloff fades the fire out before it reaches this edge, so " +
                 "raising the size alone spreads the same fire over a bigger area rather than " +
                 "simply making it bigger — pair it with Coverage on the material.")]
        [UnityEngine.Serialization.FormerlySerializedAs("silhouetteScale")]
        [SerializeField, Range(1f, 6f)] private float auraSize = 2.6f;

        [Tooltip("Aura opacity.")]
        [SerializeField, Range(0f, 1f)] private float silhouetteStrength = 0.85f;

        [Header("Attack trail")]
        [SerializeField] private float trailRate = 26f;
        [SerializeField] private float trailFade = 0.22f;
        [SerializeField, Range(0f, 1f)] private float trailAlpha = 0.5f;
        [SerializeField] private int trailCount = 10;

        private SpriteRenderer _bodyAura;
        private SpriteRenderer[] _trail;
        private float[] _trailAge;
        private int _next;
        private float _dropTimer;
        private Material _additive;
        private Material _auraMat;
        private bool _ownsAuraMat;
        private MaterialPropertyBlock _mpb;

        // Where each sprite's art actually sits in its cell, measured once. She is NOT centred:
        // a scaled copy grown about the pivot would slide away from her as it grows.
        private readonly System.Collections.Generic.Dictionary<Sprite, Vector2> _centres =
            new System.Collections.Generic.Dictionary<Sprite, Vector2>();

        private void Awake()
        {
            buff = RigRefs.Find(this, buff);
            attacks = RigRefs.Find(this, attacks);

            if (bodyRenderer == null)
            {
                Debug.LogError($"{nameof(AuraVisuals)}: no body renderer assigned; aura disabled.", this);
                enabled = false;
                return;
            }

            var additiveShader = Shader.Find("Sprites/Default");
            _additive = new Material(additiveShader) { name = "AuraAdditive (runtime)" };
            _additive.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
            _additive.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _additive.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

            if (auraMaterial != null)
            {
                // Used directly, not copied, so inspector edits apply live while playing.
                _auraMat = auraMaterial;
                _ownsAuraMat = false;
            }
            else
            {
                var auraShader = Shader.Find("Deeper/AuraSilhouette");
                if (auraShader == null)
                {
                    Debug.LogError($"{nameof(AuraVisuals)}: no aura material assigned and shader " +
                                   "'Deeper/AuraSilhouette' not found; the aura will not draw.", this);
                    enabled = false;
                    return;
                }

                _auraMat = new Material(auraShader) { name = "AuraSilhouette (runtime)" };
                _ownsAuraMat = true;
            }

            _mpb = new MaterialPropertyBlock();

            // Behind her, so she reads as a silhouette against the fire.
            _bodyAura = Spawn("BodyAura", bodyRenderer.sortingOrder - 4, _auraMat);

            _trail = new SpriteRenderer[Mathf.Max(1, trailCount)];
            _trailAge = new float[_trail.Length];
            for (int i = 0; i < _trail.Length; i++)
            {
                _trail[i] = Spawn("AuraTrail" + i, bodyRenderer.sortingOrder - 1, _additive);
                _trailAge[i] = -1f;
            }
        }

        private SpriteRenderer Spawn(string name, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bodyRenderer.transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sortingLayerID = bodyRenderer.sortingLayerID;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        private void OnDestroy()
        {
            if (_additive != null) Destroy(_additive);

            // Only destroy a material we built. Destroying the assigned asset would delete it
            // from the project.
            if (_ownsAuraMat && _auraMat != null) Destroy(_auraMat);
        }

        /// <summary>
        /// Offset from a sprite's pivot to the centre of its opaque art, mirrored to match the
        /// renderer. Scaled copies grow about the pivot, and she is not drawn at hers, so
        /// without this the aura visibly drifts off to one side as it swells.
        /// </summary>
        private Vector2 DrawnOffset(SpriteRenderer src)
        {
            var sprite = src.sprite;
            if (sprite == null) return Vector2.zero;

            if (!_centres.TryGetValue(sprite, out Vector2 centre))
            {
                centre = Vector2.zero;
                try
                {
                    var rect = sprite.textureRect;
                    int w = Mathf.RoundToInt(rect.width);
                    int h = Mathf.RoundToInt(rect.height);
                    var px = sprite.texture.GetPixels(
                        Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h);

                    int minX = int.MaxValue, maxX = int.MinValue;
                    int minY = int.MaxValue, maxY = int.MinValue;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if (px[y * w + x].a > 0.15f)
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }

                    if (minX <= maxX)
                        centre = new Vector2(
                            ((minX + maxX) * 0.5f - w * 0.5f) / sprite.pixelsPerUnit,
                            ((minY + maxY) * 0.5f - h * 0.5f) / sprite.pixelsPerUnit);
                }
                catch (System.Exception)
                {
                    // Texture not marked readable. Unity raises ArgumentException here, not
                    // UnityException — catching only the latter let it escape and take the rest
                    // of LateUpdate with it. Degrade to no correction; the result is cached
                    // either way, so this cannot throw more than once per sprite.
                    centre = Vector2.zero;
                }

                _centres[sprite] = centre;
            }

            if (src.flipX) centre.x = -centre.x;
            return centre;
        }

        private void LateUpdate()
        {
            // LateUpdate so the rig has already resolved this frame's sprite; copying it in
            // Update would trail the character by a frame.
            bool active = buff != null && buff.IsActive;

            if (!active)
            {
                DrawAura(_bodyAura, null, 0f, 0f, 0f);
            }
            else
            {
                // Fade out over the buff's last moments so it does not vanish on a hard cut.
                float tail = Mathf.Clamp01(buff.Normalized * 6f);

                DrawAura(_bodyAura, bodyRenderer, auraSize, silhouetteStrength * tail, 0f);
            }

            UpdateTrail(active);
        }

        private void DrawAura(SpriteRenderer dst, SpriteRenderer src, float scale, float alpha, float seed)
        {
            if (dst == null) return;

            if (src == null || src.sprite == null || alpha <= 0.004f)
            {
                if (dst.enabled) dst.enabled = false;
                return;
            }

            dst.enabled = true;
            dst.sprite = src.sprite;
            dst.flipX = src.flipX;

            var sprite = src.sprite;
            var tr = sprite.textureRect;
            var tex = sprite.texture;

            dst.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_Seed", seed);

            // The sprite's rect within its sheet. WITHOUT THIS the shader's UVs address the whole
            // sheet, so a sub-sprite only spans a slice of them — the noise barely varies and the
            // aura draws as a solid shape rather than flames.
            _mpb.SetVector("_SpriteRect", new Vector4(
                tr.x / tex.width, tr.y / tex.height,
                tr.width / tex.width, tr.height / tex.height));

            // Pixel grid across this quad, in GAME pixels. The quad spans
            // (rect / pixelsPerUnit * scale) world units, and one game pixel is 1/pixelsPerUnit
            // of a world unit, so the count is simply rect * scale. Feeding a fixed number
            // instead would make the aura's pixels grow with the reach and stop matching the art.
            _mpb.SetVector("_Pixels", new Vector4(tr.width * scale, tr.height * scale, 0f, 0f));

            dst.SetPropertyBlock(_mpb);

            // Scaling happens about the pivot, and her art is not centred on it, so the copy is
            // shifted back by the offset it would otherwise drift by.
            Vector2 d = DrawnOffset(src);
            dst.transform.position = src.transform.position + (Vector3)(d * (1f - scale));
            dst.transform.localScale = Vector3.one * scale;

            var c = auraColour;
            c.a = Mathf.Clamp01(alpha);
            dst.color = c;
        }

        private void UpdateTrail(bool active)
        {
            bool swinging = active && attacks != null && attacks.IsAttacking && bodyRenderer.sprite != null;

            if (swinging)
            {
                _dropTimer -= Time.deltaTime;
                if (_dropTimer <= 0f)
                {
                    _dropTimer = trailRate > 0f ? 1f / trailRate : 0.05f;
                    Drop();
                }
            }
            else
            {
                _dropTimer = 0f;
            }

            for (int i = 0; i < _trail.Length; i++)
            {
                if (_trailAge[i] < 0f) continue;

                _trailAge[i] += Time.deltaTime;
                float t = trailFade > 0f ? _trailAge[i] / trailFade : 1f;
                if (t >= 1f)
                {
                    _trailAge[i] = -1f;
                    _trail[i].enabled = false;
                    continue;
                }

                var c = auraColour;
                c.a = trailAlpha * (1f - t);
                _trail[i].color = c;
            }
        }

        private void Drop()
        {
            var sr = _trail[_next];

            // Afterimages stay where they were dropped, so they read as a trail she has moved
            // out of. Unparented here because they must not follow the rig.
            sr.transform.SetParent(null, false);
            sr.transform.position = bodyRenderer.transform.position;
            sr.transform.rotation = bodyRenderer.transform.rotation;
            sr.transform.localScale = bodyRenderer.transform.lossyScale;

            sr.sprite = bodyRenderer.sprite;
            sr.flipX = bodyRenderer.flipX;
            sr.enabled = true;

            _trailAge[_next] = 0f;
            _next = (_next + 1) % _trail.Length;
        }
    }
}
