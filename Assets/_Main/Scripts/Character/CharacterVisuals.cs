using UnityEngine;

namespace Deeper.Character
{
    /// <summary>
    /// Draws the layered character in the world: the protagonist's body plus the run's weapon,
    /// with sorting order deciding what covers what. A layer with no art disables its renderer.
    ///
    /// <see cref="cosmeticRenderers"/> holds the renderers left over from the removed armor
    /// system. They are force-disabled so no stale gear art survives on the rig, and they stay
    /// wired so post-launch cosmetics have somewhere to draw without rebuilding the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisuals : CharacterLayerView
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;

        [Tooltip("Reserved for post-launch cosmetic layers. Disabled while no cosmetic system exists.")]
        [SerializeField] private SpriteRenderer[] cosmeticRenderers = new SpriteRenderer[0];

        protected override void OnEnable()
        {
            for (int i = 0; i < cosmeticRenderers.Length; i++)
            {
                if (cosmeticRenderers[i] == null) continue;

                cosmeticRenderers[i].sprite = null;
                cosmeticRenderers[i].enabled = false;
            }

            base.OnEnable();
        }

        protected override void ApplyBody(Sprite sprite, bool flipX) => Apply(bodyRenderer, sprite, flipX);

        protected override void ApplyWeapon(Sprite sprite, bool flipX) => Apply(weaponRenderer, sprite, flipX);

        private static void Apply(SpriteRenderer target, Sprite sprite, bool flipX)
        {
            if (target == null) return;

            target.sprite = sprite;
            target.flipX = flipX;
            target.enabled = sprite != null;
        }
    }
}
