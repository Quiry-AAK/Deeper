using System.Collections.Generic;
using System.Text;
using Deeper.Combat;
using Deeper.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.Testing
{
    /// <summary>
    /// The test scene's on-screen panel: which key does what, and what the systems currently read.
    ///
    /// TEST-ONLY — this belongs to `TestScene` and must never end up in a real room. It exists
    /// because the game has no HP bar and no damage numbers yet, so "did that hit land, and for how
    /// much" is otherwise invisible; and because a cheat key nobody can remember is a cheat key
    /// nobody uses. The legend is built from <see cref="TestControls"/> and each
    /// <see cref="TestSpawner"/>'s own key fields, so it cannot drift away from what the keys
    /// actually do.
    ///
    /// This is not the real HUD. `UltimateGaugeHUD` is the shipped one; everything here is
    /// developer-facing text that the UI art pass (ART_DIRECTION §5) does not touch.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestOverlay : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("Switched off wholesale by the toggle key, so the view can be cleared for a " +
                 "screenshot without unwiring anything.")]
        [SerializeField] private GameObject panel;

        [SerializeField] private Text legendLabel;
        [SerializeField] private Text statusLabel;

        [Header("Sources — legend")]
        [SerializeField] private TestControls controls;
        [SerializeField] private TestSpawner[] spawners;

        [Header("Sources — status")]
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private UltimateGauge gauge;
        [SerializeField] private ComboCounter combo;

        [Header("Keys")]
        [SerializeField] private Key toggleKey = Key.F10;

        [Tooltip("Seconds between status refreshes. Rebuilding the string every frame allocates on " +
                 "every frame, and this scene exists to judge frame feel — the readout must not be " +
                 "the thing stuttering it.")]
        [SerializeField] private float refreshInterval = 0.1f;

        private readonly StringBuilder _status = new StringBuilder(96);
        private float _nextRefresh;

        private void Awake()
        {
            if (controls == null) controls = FindFirstObjectByType<TestControls>();
            if (spawners == null || spawners.Length == 0) spawners = FindObjectsByType<TestSpawner>(FindObjectsSortMode.None);

            // Found by tag like TestControls does, for the same reason: FindFirstObjectByType
            // <Damageable> would happily return a training dummy.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerHealth == null) playerHealth = player.GetComponentInChildren<Damageable>(true);
                if (gauge == null) gauge = player.GetComponentInChildren<UltimateGauge>(true);
                if (combo == null) combo = player.GetComponentInChildren<ComboCounter>(true);
            }

            LegacyUIFont.EnsureFont(legendLabel);
            LegacyUIFont.EnsureFont(statusLabel);
        }

        private void Start()
        {
            BuildLegend();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && toggleKey != Key.None && keyboard[toggleKey].wasPressedThisFrame && panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }

            // Unscaled: hitstop freezes scaled time to 2%, and a readout that stops updating during
            // every hit is useless for reading what the hit did.
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshInterval;

            RefreshStatus();
        }

        private void BuildLegend()
        {
            if (legendLabel == null) return;

            List<string> lines = new List<string> { "— TEST SCENE —" };
            if (controls != null) lines.AddRange(controls.Legend);

            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] != null) lines.AddRange(spawners[i].Legend);
            }

            lines.Add("[" + toggleKey + "] Hide panel");
            legendLabel.text = string.Join("\n", lines.ToArray());
        }

        private void RefreshStatus()
        {
            if (statusLabel == null) return;

            _status.Length = 0;

            if (playerHealth != null)
            {
                _status.Append("HP ").Append(Mathf.CeilToInt(playerHealth.Health))
                       .Append('/').Append(Mathf.CeilToInt(playerHealth.MaxHealth));
            }

            if (gauge != null)
            {
                if (_status.Length > 0) _status.Append("   ");
                _status.Append("ULT ").Append(Mathf.RoundToInt(gauge.Normalized * 100f)).Append('%');
            }

            if (combo != null)
            {
                if (_status.Length > 0) _status.Append("   ");
                _status.Append("COMBO x").Append(combo.Stacks);
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] == null) continue;
                if (_status.Length > 0) _status.Append("   ");
                _status.Append(spawners[i].DisplayName).Append(' ').Append(spawners[i].Alive);
            }

            statusLabel.text = _status.ToString();
        }
    }
}
