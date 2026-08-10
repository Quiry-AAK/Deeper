using System;
using Deeper.Animation;
using Deeper.Equipment;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The paper-doll preview: the same gear layering as the in-world rig, drawn with UI
    /// <c>Image</c>s so it can be scaled up inside the inventory screen without a second camera
    /// or a render texture. Layer order is the sibling order of the Images.
    ///
    /// By default it pins itself to a front-facing idle pose — a portrait shouldn't turn its back
    /// on you because the player happened to be walking north when they opened the screen — while
    /// still cycling the idle frames so it reads as alive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentPreview : EquipmentLayerView
    {
        [Serializable]
        private struct SlotLayer
        {
            public EquipmentSlot Slot;
            public Image Image;
        }

        [Tooltip("On: mirrors the player's live state and facing. Off: front-facing idle.")]
        [SerializeField] private bool followLivePose;

        [SerializeField] private Image bodyImage;
        [SerializeField] private SlotLayer[] layers = new SlotLayer[0];

        protected override CharacterState PoseState =>
            followLivePose ? base.PoseState : CharacterState.Idle;

        protected override Facing PoseFacing =>
            followLivePose ? base.PoseFacing : Facing.Down;

        protected override int LayerCount => layers.Length;

        protected override EquipmentSlot GetSlot(int index) => layers[index].Slot;

        protected override void ApplyLayer(int index, Sprite sprite, bool flipX)
        {
            Apply(layers[index].Image, sprite, flipX);
        }

        protected override void ApplyBaseBody(Sprite sprite, bool flipX)
        {
            Apply(bodyImage, sprite, flipX);
        }

        private static void Apply(Image target, Sprite sprite, bool flipX)
        {
            if (target == null) return;

            target.sprite = sprite;
            target.enabled = sprite != null;
            target.preserveAspect = true;

            // Images have no flipX; mirror through scale instead.
            Vector3 scale = target.rectTransform.localScale;
            scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            target.rectTransform.localScale = scale;
        }
    }
}
