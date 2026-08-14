using Deeper.Core;
using UnityEngine;

namespace Deeper.Combat
{
    /// <summary>
    /// Flashes the sprites of whatever just got hit. GDD §Feedback lists hit-flash as a must-have,
    /// and it earns that: a hit that only moves a number is invisible, so most of "did that connect?"
    /// is carried by this and by hitstop rather than by the damage itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFlash : MonoBehaviour
    {
        [Header("Refs — found anywhere on this rig when empty")]
        [SerializeField] private Damageable source;

        [Tooltip("Sprites to tint. Wired rather than scraped from the hierarchy, so this only " +
                 "touches the layers it should — the character rig has six renderers plus an aura " +
                 "that owns its own colour.")]
        [SerializeField] private SpriteRenderer[] renderers;

        [SerializeField] private Color flashColor = Color.white;

        [Tooltip("Seconds the tint holds. Short: a flash should read as an impact, not as a state.")]
        [SerializeField] private float duration = 0.09f;

        private Color[] _original;
        private float _remaining;

        private void Awake()
        {
            source = RigRefs.Find(this, source);

            // Captured once, so a second hit arriving mid-flash still restores the authored colour
            // instead of saving the flash colour as the new normal.
            _original = new Color[renderers != null ? renderers.Length : 0];
            for (int i = 0; i < _original.Length; i++)
            {
                if (renderers[i] != null) _original[i] = renderers[i].color;
            }
        }

        private void OnEnable()
        {
            if (source != null) source.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (source != null) source.Damaged -= HandleDamaged;

            // Never leave a rig tinted because this component went away mid-flash — the training
            // dummy disables its whole visual on death and re-enables it on respawn.
            Restore();
        }

        private void HandleDamaged(float amount)
        {
            _remaining = duration;

            for (int i = 0; i < _original.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = flashColor;
            }
        }

        private void Update()
        {
            if (_remaining <= 0f) return;

            // Unscaled, so the flash survives the hitstop that fires with it. On scaled time the
            // whole freeze would burn through the tint in a fraction of a frame's worth of game
            // time and the flash would be gone before the freeze ended.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f) Restore();
        }

        private void Restore()
        {
            _remaining = 0f;
            if (_original == null) return;

            for (int i = 0; i < _original.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = _original[i];
            }
        }
    }
}
