using Deeper.Animation;
using Deeper.Core;
using UnityEngine;

namespace Deeper.Character
{
    /// <summary>
    /// Shared plumbing for anything that draws the layered character. Two layers exist today —
    /// the protagonist's body, which is fixed art rather than a choice, and the weapon from
    /// <see cref="RunLoadout.Weapon"/> — and cosmetic layers are planned post-launch, which is
    /// why this stays a layered rig rather than collapsing into a single sprite.
    ///
    /// Every layer resolves from the same <see cref="CharacterAnimator"/> pose, so body and
    /// weapon are frame-locked by construction rather than by keeping separate animators in sync.
    /// </summary>
    public abstract class CharacterLayerView : MonoBehaviour
    {
        [Tooltip("Left empty: falls back to this rig, then to the first one in the scene.")]
        [SerializeField] private RunLoadout loadout;

        [Tooltip("Left empty: falls back to this rig, then to the first one in the scene.")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("The protagonist's body art. Fixed — there is no body selection.")]
        [SerializeField] private SpriteAnimationSet bodyAnimation;

        /// <summary>
        /// The set actually drawing the character. Exposed so the Attack State Machine can read a
        /// clip's real frame count instead of guessing — guessing a count higher than the clip has
        /// makes the animation wrap and visibly restart mid-action.
        /// </summary>
        public SpriteAnimationSet BodyAnimation { get { return bodyAnimation; } }

        private RunLoadout _loadout;
        private CharacterAnimator _animator;
        private bool _subscribed;

        protected RunLoadout Loadout
        {
            get
            {
                if (_loadout != null) return _loadout;

                // The scene-wide fallback is for views that are not on the rig at all — a Hub
                // character preview drawing the same layers from a UI canvas.
                _loadout = RigRefs.Find(this, loadout);
                if (_loadout == null) _loadout = FindFirstObjectByType<RunLoadout>();
                return _loadout;
            }
        }

        protected CharacterAnimator Animator
        {
            get
            {
                if (_animator != null) return _animator;

                _animator = RigRefs.Find(this, characterAnimator);
                if (_animator == null) _animator = FindFirstObjectByType<CharacterAnimator>();
                return _animator;
            }
        }

        /// <summary>Overridable so a view can pin itself to a fixed pose (a Hub preview would).</summary>
        protected virtual CharacterState PoseState => Animator != null ? Animator.State : CharacterState.Idle;

        protected virtual Facing PoseFacing => Animator != null ? Animator.Facing : Facing.Down;

        protected virtual int PoseFrame => Animator != null ? Animator.PlaybackFrame : 0;

        protected abstract void ApplyBody(Sprite sprite, bool flipX);

        protected abstract void ApplyWeapon(Sprite sprite, bool flipX);

        protected virtual void OnEnable()
        {
            RunLoadout source = Loadout;
            if (source != null) source.LoadoutChanged += RefreshAll;

            CharacterAnimator pose = Animator;
            if (pose != null) pose.PoseChanged += RefreshAll;

            _subscribed = true;

            // A view enabled after the Hub applied the loadout must pull current state rather
            // than wait for the next change.
            RefreshAll();
        }

        protected virtual void OnDisable()
        {
            if (!_subscribed) return;

            RunLoadout source = Loadout;
            if (source != null) source.LoadoutChanged -= RefreshAll;

            CharacterAnimator pose = Animator;
            if (pose != null) pose.PoseChanged -= RefreshAll;

            _subscribed = false;
        }

        [ContextMenu("Refresh All Layers")]
        public void RefreshAll()
        {
            RunLoadout source = Loadout;
            CharacterState state = PoseState;
            Facing facing = PoseFacing;
            int frame = PoseFrame;
            bool flip = facing.IsMirrored();

            ApplyBody(ResolveBody(state, facing, frame), flip);

            WeaponDefinition weapon = source != null ? source.Weapon : null;
            ApplyWeapon(weapon != null ? weapon.Resolve(state, facing, frame) : null, flip);
        }

        /// <summary>
        /// Resolves the body sprite, standing an older sibling clip in when this state has no art
        /// yet (<see cref="CharacterStateExtensions.FallbackArt"/>).
        ///
        /// States arrive before their animations do — the Basic chain's later hits, the Dash
        /// Attack and the Heavy Strike charge all shipped as working code first. Without this the
        /// character simply vanishes for those frames, and a blank sprite is far worse than a
        /// repeated one. Each fallback disappears on its own the moment real art is bound.
        ///
        /// The map itself lives on <see cref="CharacterState"/> because the Attack State Machine
        /// reads the same answer to size the clip — see the note there.
        /// </summary>
        private Sprite ResolveBody(CharacterState state, Facing facing, int frame)
        {
            if (bodyAnimation == null) return null;

            Sprite sprite = bodyAnimation.Resolve(state, facing, frame);
            if (sprite != null) return sprite;

            CharacterState fallback = state.FallbackArt();
            return fallback != state ? bodyAnimation.Resolve(fallback, facing, frame) : null;
        }
    }
}
