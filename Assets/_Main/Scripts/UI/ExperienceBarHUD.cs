using Deeper.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The XP bar and level readout — ART_DIRECTION §5 puts both top-right.
    ///
    /// The bar shows progress *into the current level*, not the run total, because the only thing
    /// the player can act on is how close the next upgrade offer is (CORE_SYSTEMS §12).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExperienceBarHUD : MonoBehaviour
    {
        [Header("Source — found by tag when empty")]
        [SerializeField] private PlayerXP experience;

        [Header("Widgets")]
        [SerializeField] private StatBar bar;
        [SerializeField] private Text levelLabel;

        [Header("Feel")]
        [Tooltip("Seconds the level badge stays highlighted after a level-up. The upgrade offer " +
                 "that should interrupt here is Milestone 4 and does not exist, so this flash is " +
                 "currently the only feedback that a level happened at all.")]
        [SerializeField] private float levelFlashTime = 1.2f;

        [SerializeField] private Color levelNormal = new Color(0.85f, 0.83f, 0.78f, 1f);
        [SerializeField] private Color levelFlash = new Color(1f, 0.94f, 0.72f, 1f);

        private float _flashRemaining;

        private void Awake()
        {
            if (experience == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) experience = player.GetComponentInChildren<PlayerXP>(true);
            }

            LegacyUIFont.EnsureFont(levelLabel);
        }

        private void OnEnable()
        {
            if (experience == null) return;

            experience.Changed += Refresh;
            experience.LeveledUp += HandleLeveledUp;

            Refresh();
            if (bar != null) bar.SnapToTarget();
        }

        private void OnDisable()
        {
            if (experience == null) return;

            experience.Changed -= Refresh;
            experience.LeveledUp -= HandleLeveledUp;
        }

        private void Update()
        {
            if (_flashRemaining <= 0f) return;

            _flashRemaining -= Time.unscaledDeltaTime;
            if (_flashRemaining <= 0f && levelLabel != null) levelLabel.color = levelNormal;
        }

        private void HandleLeveledUp(int level)
        {
            _flashRemaining = levelFlashTime;
            if (levelLabel != null) levelLabel.color = levelFlash;
        }

        private void Refresh()
        {
            if (experience == null) return;

            if (bar != null)
            {
                bar.SetNormalized(experience.Normalized);
                bar.SetLabel(Mathf.FloorToInt(experience.XP) + " / " +
                             Mathf.CeilToInt(experience.XPToNextLevel));
            }

            // The number alone: the hexagon is what says "level" — it is the one shape in the HUD
            // that is neither a bar nor a slot, which is what makes it findable — and at the pixel
            // font's fixed advance a "LV 12" runs straight through the badge's walls.
            if (levelLabel != null) levelLabel.text = experience.Level.ToString();
        }
    }
}
