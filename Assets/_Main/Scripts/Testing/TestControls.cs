using Deeper.Combat;
using Deeper.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Testing
{
    /// <summary>
    /// The test scene's cheat keys — everything that acts on the player rather than on the room
    /// (spawning lives on <see cref="TestSpawner"/>, one object per prefab).
    ///
    /// TEST-ONLY — this belongs to `TestScene` and must never end up in a real room. Its whole
    /// reason to exist is that the systems it pokes are gated on purpose: the Ultimate needs 100
    /// landed hits to charge, and there is no healing in the game at all yet, so tuning either one
    /// without these keys means replaying the charge every single time.
    ///
    /// Keys are read straight off <see cref="Keyboard"/> rather than through the project's
    /// `.inputactions` asset, on purpose: debug keys must not appear in the player's action map,
    /// which is shipped content and is where the rebinding UI will read from. Function keys were
    /// chosen because every letter, digit and arrow in that asset is already bound to a real
    /// action.
    ///
    /// **No slow-motion key**, deliberately: <see cref="HitStop"/> restores `Time.timeScale` to a
    /// fixed normal after every landed hit, so a debug slow-mo would silently snap back to 1 on the
    /// next connect and read as a broken key. Inspecting an attack frame by frame needs the
    /// editor's own pause/step, or `HitStop`'s serialized fields.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestControls : MonoBehaviour
    {
        [Header("Keys")]
        [Tooltip("Fills the Ultimate Gauge to full, so the Ultimate can be tested without landing " +
                 "100 hits first.")]
        [SerializeField] private Key fillUltimateKey = Key.F1;

        [Tooltip("Restores the player to full HP. There is no healing in the game yet and no death " +
                 "handling, so without this a test session ends parked at 0 HP.")]
        [SerializeField] private Key healPlayerKey = Key.F2;

        [Tooltip("Teleports the player back to where she started. Contact damage and lunges push " +
                 "her across the room over a long session.")]
        [SerializeField] private Key resetPlayerKey = Key.F3;

        [Header("Refs — found in the scene when empty")]
        [SerializeField] private UltimateGauge gauge;
        [SerializeField] private Damageable playerHealth;
        [SerializeField] private RunKeys playerKeys;

        [Tooltip("Where Reset Player puts her. Leave empty to use wherever she stands when Play " +
                 "starts.")]
        [SerializeField] private Transform playerStart;

        private Rigidbody2D _playerBody;
        private Vector2 _startPosition;

        /// <summary>Lines for the on-screen legend, built from the same key fields the input uses,
        /// so the panel cannot drift out of sync with what the keys actually do.</summary>
        public string[] Legend
        {
            get
            {
                return new[]
                {
                    "[" + fillUltimateKey + "] Fill Ultimate",
                    "[" + healPlayerKey + "] Heal Player",
                    "[" + resetPlayerKey + "] Reset Player",
                };
            }
        }

        private void Awake()
        {
            // The player rig is found by tag, the way CameraRig finds it — RigRefs is no help here
            // because this object is not on the rig, and FindFirstObjectByType<Damageable> would
            // happily return a training dummy.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (gauge == null) gauge = player.GetComponentInChildren<UltimateGauge>(true);
            if (playerHealth == null) playerHealth = player.GetComponentInChildren<Damageable>(true);
            if (playerKeys == null) playerKeys = player.GetComponentInChildren<RunKeys>(true);
            _playerBody = player.GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _startPosition = playerStart != null
                ? (Vector2)playerStart.position
                : (playerHealth != null ? (Vector2)playerHealth.transform.position : Vector2.zero);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (fillUltimateKey != Key.None && keyboard[fillUltimateKey].wasPressedThisFrame) FillUltimate();
            if (healPlayerKey != Key.None && keyboard[healPlayerKey].wasPressedThisFrame) HealPlayer();
            if (resetPlayerKey != Key.None && keyboard[resetPlayerKey].wasPressedThisFrame) ResetPlayer();
        }

        /// <summary>Public so a probe can call it — simulated key presses never reach play mode
        /// (Engineering/01-VERIFICATION.md §2). Same for the two below.</summary>
        [ContextMenu("Fill Ultimate")]
        public void FillUltimate()
        {
            if (gauge == null) return;

            // UltimateGauge.Add scales what it is given by PlayerStats.UltimateGaugeGain, so a
            // single 100 falls short under a gauge-gain curse. Bounded rather than a while loop:
            // a gain of 0 would otherwise hang the editor.
            for (int i = 0; i < 10 && !gauge.IsFull; i++) gauge.Add(100f);
        }

        [ContextMenu("Heal Player")]
        public void HealPlayer()
        {
            if (playerHealth != null) playerHealth.Refill();
        }

        /// <summary>
        /// Hands her a Secret Vault key without making her find a Deep Warden first.
        ///
        /// **No key binding, on purpose.** F1-F3 are the player cheats, F4-F9 the spawners, F10 the
        /// readout and F12 the room re-arm — the function row is full, which is exactly why
        /// <see cref="TestConfigHUD"/> exists. New harness features cost a button now.
        /// </summary>
        [ContextMenu("Grant Secret Key")]
        public void GrantSecretKey()
        {
            if (playerKeys != null) playerKeys.GrantSecretKey();
        }

        [ContextMenu("Reset Player")]
        public void ResetPlayer()
        {
            // Moved through the body, not the transform: writing transform.position on a rigidbody
            // leaves the physics position behind for a frame, which reads as a snap-back.
            if (_playerBody != null) _playerBody.position = _startPosition;
            else if (playerHealth != null) playerHealth.transform.position = _startPosition;
        }
    }
}
