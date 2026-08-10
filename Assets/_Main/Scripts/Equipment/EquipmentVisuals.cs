using System;
using UnityEngine;

namespace Deeper.Equipment
{
    /// <summary>
    /// Draws the layered character in the world: one <see cref="SpriteRenderer"/> per gear slot
    /// plus the bare-body layer underneath, with sorting order deciding what covers what. A slot
    /// with no gear (or gear with no art) disables its renderer.
    ///
    /// This is the seam real pixel art plugs into — nothing here assumes a sprite size.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentInventory))]
    public sealed class EquipmentVisuals : EquipmentLayerView
    {
        [Serializable]
        private struct SlotLayer
        {
            public EquipmentSlot Slot;
            public SpriteRenderer Renderer;
        }

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SlotLayer[] layers = new SlotLayer[0];

        protected override int LayerCount => layers.Length;

        protected override EquipmentSlot GetSlot(int index) => layers[index].Slot;

        protected override void ApplyLayer(int index, Sprite sprite, bool flipX)
        {
            Apply(layers[index].Renderer, sprite, flipX);
        }

        protected override void ApplyBaseBody(Sprite sprite, bool flipX)
        {
            Apply(bodyRenderer, sprite, flipX);
        }

        private static void Apply(SpriteRenderer target, Sprite sprite, bool flipX)
        {
            if (target == null) return;

            target.sprite = sprite;
            target.flipX = flipX;
            target.enabled = sprite != null;
        }
    }
}
