using Deeper.Animation;
using UnityEngine;

namespace Deeper.Equipment
{
    /// <summary>
    /// Shared plumbing for anything that draws the layered character: the in-world player rig
    /// (<see cref="SpriteRenderer"/> layers) and the inventory preview (UI <c>Image</c> layers)
    /// need identical resolve/refresh behaviour and differ only in the renderer type.
    ///
    /// Every layer is resolved from the same <see cref="CharacterAnimator"/> pose, so the body and
    /// each piece of gear are frame-locked by construction rather than by keeping several
    /// independent animators in sync.
    /// </summary>
    public abstract class EquipmentLayerView : MonoBehaviour
    {
        [Tooltip("Left empty: falls back to this GameObject, then to the first one in the scene.")]
        [SerializeField] private EquipmentInventory inventory;

        [Tooltip("Left empty: falls back to this GameObject, then to the first one in the scene.")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Tooltip("Art for the bare body drawn underneath all gear.")]
        [SerializeField] private SpriteAnimationSet baseBodyAnimation;

        private EquipmentInventory _inventory;
        private CharacterAnimator _animator;
        private bool _subscribed;

        protected EquipmentInventory Inventory
        {
            get
            {
                if (_inventory != null) return _inventory;

                _inventory = inventory != null ? inventory : GetComponent<EquipmentInventory>();
                if (_inventory == null) _inventory = FindFirstObjectByType<EquipmentInventory>();
                return _inventory;
            }
        }

        protected CharacterAnimator Animator
        {
            get
            {
                if (_animator != null) return _animator;

                _animator = characterAnimator != null ? characterAnimator : GetComponent<CharacterAnimator>();
                if (_animator == null) _animator = FindFirstObjectByType<CharacterAnimator>();
                return _animator;
            }
        }

        /// <summary>Overridable so a view can pin itself to a fixed pose (the inventory preview does).</summary>
        protected virtual CharacterState PoseState => Animator != null ? Animator.State : CharacterState.Idle;

        protected virtual Facing PoseFacing => Animator != null ? Animator.Facing : Facing.Down;

        protected virtual int PoseFrame => Animator != null ? Animator.Frame : 0;

        protected abstract int LayerCount { get; }

        protected abstract EquipmentSlot GetSlot(int index);

        protected abstract void ApplyLayer(int index, Sprite sprite, bool flipX);

        protected abstract void ApplyBaseBody(Sprite sprite, bool flipX);

        protected virtual void OnEnable()
        {
            EquipmentInventory source = Inventory;
            if (source != null) source.EquipmentChanged += HandleEquipmentChanged;

            CharacterAnimator pose = Animator;
            if (pose != null) pose.PoseChanged += RefreshAll;

            _subscribed = true;

            // Starting equipment is applied in EquipmentInventory.Awake, and a view inside a
            // hidden tab enables long after that, so pull current state rather than waiting.
            RefreshAll();
        }

        protected virtual void OnDisable()
        {
            if (!_subscribed) return;

            EquipmentInventory source = Inventory;
            if (source != null) source.EquipmentChanged -= HandleEquipmentChanged;

            CharacterAnimator pose = Animator;
            if (pose != null) pose.PoseChanged -= RefreshAll;

            _subscribed = false;
        }

        private void HandleEquipmentChanged(EquipmentSlot slot, EquipmentDefinition previous, EquipmentDefinition current)
        {
            // Six layers — re-resolving all of them is cheaper than tracking which need it.
            RefreshAll();
        }

        [ContextMenu("Refresh All Layers")]
        public void RefreshAll()
        {
            EquipmentInventory source = Inventory;
            CharacterState state = PoseState;
            Facing facing = PoseFacing;
            int frame = PoseFrame;
            bool flip = facing.IsMirrored();

            ApplyBaseBody(baseBodyAnimation != null ? baseBodyAnimation.Resolve(state, facing, frame) : null, flip);

            for (int i = 0; i < LayerCount; i++)
            {
                EquipmentDefinition item = source != null ? source.GetEquipped(GetSlot(i)) : null;
                ApplyLayer(i, Resolve(item, state, facing, frame), flip);
            }
        }

        private static Sprite Resolve(EquipmentDefinition item, CharacterState state, Facing facing, int frame)
        {
            if (item == null) return null;

            // Falls back to the single static sprite so a piece without animation art still shows.
            return item.AnimationSet != null ? item.AnimationSet.Resolve(state, facing, frame) : item.BodyLayer;
        }
    }
}
