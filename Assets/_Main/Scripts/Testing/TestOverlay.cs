using System.Collections.Generic;
using System.Text;
using Deeper.Combat;
using Deeper.Player;
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

        [Tooltip("Supplies both the room's legend line and its status line. Its own field rather " +
                 "than a shared interface: one extra source does not earn an abstraction, and " +
                 "replacing the two typed slots above with an untyped list would make the wiring " +
                 "harder to read in exchange for nothing.")]
        [SerializeField] private TestRoomControls roomControls;

        [Header("Sources — status")]
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private UltimateGauge gauge;
        [SerializeField] private ComboCounter combo;

        [Tooltip("Whether the dash is off cooldown, and whether her i-frames are up, are both " +
                 "completely invisible otherwise — which makes 'did the dodge work' unanswerable " +
                 "at the exact moment it matters most.")]
        [SerializeField] private DigDash dash;

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
            if (roomControls == null) roomControls = FindFirstObjectByType<TestRoomControls>();

            // Found by tag like TestControls does, for the same reason: FindFirstObjectByType
            // <Damageable> would happily return a training dummy.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerHealth == null) playerHealth = player.GetComponentInChildren<Damageable>(true);
                if (gauge == null) gauge = player.GetComponentInChildren<UltimateGauge>(true);
                if (combo == null) combo = player.GetComponentInChildren<ComboCounter>(true);
                if (dash == null) dash = player.GetComponentInChildren<DigDash>(true);
            }

            LegacyUIFont.EnsureFont(legendLabel);
            LegacyUIFont.EnsureFont(statusLabel);
        }

        private void Start()
        {
            BuildLegend();
        }

        /// <summary>
        /// Shows or hides just the key legend, leaving the status line alone.
        ///
        /// <see cref="TestConfigHUD"/> hides it while its menu is open, because that menu lists the
        /// same cheats as clickable buttons — two copies of the same list, one drawn over the other,
        /// is worse than either alone. The status line stays: it is the thing you are reading while
        /// you click.
        /// </summary>
        public void SetLegendVisible(bool visible)
        {
            if (legendLabel != null) legendLabel.gameObject.SetActive(visible);
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

            // Between the player cheats and the spawners, so the legend reads in the order a
            // session uses it: fix yourself up, restart the room, then add loose enemies to it.
            if (roomControls != null) lines.AddRange(roomControls.Legend);

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

            if (dash != null)
            {
                if (_status.Length > 0) _status.Append("   ");
                _status.Append("DASH ");

                // Three states, because they answer three different questions: is she dodging
                // right now, is she still untouchable, and can she go again.
                if (dash.IsDashing) _status.Append("GO");
                else if (playerHealth != null && playerHealth.IsInvulnerable) _status.Append("i-FRAME");
                else if (dash.IsReady) _status.Append("ready");
                else _status.Append(Mathf.RoundToInt(dash.CooldownNormalized * 100f)).Append('%');
            }

            // The room's own count, kept separate from the spawner counts below on purpose: the
            // room only ever counts what IT spawned, so an F6 crawler walking around inside it
            // shows up in "Crawlers" and never in "LEFT". That is correct — those kills do not
            // unlock the doors — and reading both lines is how you see it.
            if (roomControls != null)
            {
                if (_status.Length > 0) _status.Append("   ");
                _status.Append(roomControls.Status);
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
