using Deeper.Core;
using UnityEngine;

namespace Deeper.Animation
{
    /// <summary>
    /// Draws an actor that is a single sprite, from the same (state, facing, frame) pose the
    /// layered player rig uses.
    ///
    /// This is the enemy-side counterpart to <c>CharacterVisuals</c>, and deliberately not a
    /// subclass of <c>CharacterLayerView</c>: that base requires a <c>RunLoadout</c> and draws a
    /// weapon layer from it, falling back to the first one in the scene when its own rig has
    /// none. An enemy inheriting it would silently draw **the player's katana** on top of itself.
    /// One sprite with no loadout is a different shape, not a variation on the paper-doll rig.
    ///
    /// Enemies author three directions rather than the player's five (Down, Up, Side), with the
    /// diagonal slots in <see cref="SpriteAnimationSet"/> pointed at the same frames as Side. That
    /// is an authoring decision only — nothing here or in <see cref="FacingExtensions"/> knows how
    /// many directions were drawn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteAnimationView : MonoBehaviour
    {
        [Tooltip("The renderer this draws to.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Per-state/direction art. Lives on the prefab rather than on EnemyDefinition so " +
                 "an Elite — which is a palette swap of an existing enemy (ART_DIRECTION §4) — " +
                 "keeps its parent's art while swapping only the stat asset.")]
        [SerializeField] private SpriteAnimationSet animationSet;

        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private CharacterAnimator characterAnimator;

        private bool _subscribed;

        /// <summary>
        /// The set actually drawing this actor. Read by callers that need a clip's real frame
        /// count — guessing a count higher than the clip has makes it wrap and visibly restart
        /// partway through an action.
        /// </summary>
        public SpriteAnimationSet AnimationSet { get { return animationSet; } }

        private void Awake()
        {
            characterAnimator = RigRefs.Find(this, characterAnimator);

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                Debug.LogError($"{nameof(SpriteAnimationView)}: no sprite renderer assigned; " +
                               "this actor will not draw.", this);
            }
        }

        private void OnEnable()
        {
            if (characterAnimator != null) characterAnimator.PoseChanged += Refresh;
            _subscribed = true;

            // Pull the current pose rather than waiting for the next change — a view enabled
            // after the animator already settled would otherwise draw nothing until it moved.
            Refresh();
        }

        private void OnDisable()
        {
            if (!_subscribed) return;

            if (characterAnimator != null) characterAnimator.PoseChanged -= Refresh;
            _subscribed = false;
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (spriteRenderer == null || animationSet == null) return;

            CharacterState state = characterAnimator != null
                ? characterAnimator.State : CharacterState.Idle;
            Facing facing = characterAnimator != null
                ? characterAnimator.Facing : Facing.Down;
            int frame = characterAnimator != null ? characterAnimator.PlaybackFrame : 0;

            spriteRenderer.sprite = Resolve(state, facing, frame);
            spriteRenderer.flipX = facing.IsMirrored();
        }

        /// <summary>
        /// Resolves the sprite, falling back to Idle when a state has no art.
        ///
        /// ART_DIRECTION §4 gives enemies one combined Idle/Move clip, so a set that binds only
        /// Idle is a legitimate authoring choice rather than a mistake — and an unbound Telegraph
        /// or Death would otherwise make the actor vanish mid-fight. A repeated sprite is a far
        /// smaller defect than a blank one, and the fallback stops mattering the moment real art
        /// is bound.
        /// </summary>
        private Sprite Resolve(CharacterState state, Facing facing, int frame)
        {
            Sprite sprite = animationSet.Resolve(state, facing, frame);
            if (sprite != null) return sprite;

            return state == CharacterState.Idle
                ? null : animationSet.Resolve(CharacterState.Idle, facing, frame);
        }
    }
}
