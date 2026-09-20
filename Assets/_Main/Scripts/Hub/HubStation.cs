using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Hub
{
    /// <summary>
    /// A fixture in the camp the player can use — the weapon rack, the mine shaft, the shrine.
    /// Stand in its volume, press Interact (E).
    ///
    /// **One class for every station, not one per fixture.** A station is only ever "she is
    /// standing here and pressed the key"; what that then *does* belongs to whatever listens. A
    /// `WeaponRack` class whose entire body was a subscription to this event would be a file that
    /// exists to be opened and found empty.
    ///
    /// It lives on its own child of the fixture, the same shape <see cref="Deeper.Rooms.RoomEntry"/>
    /// and <c>AttackHitbox</c> use: the reach you can *use* a fixture from is much wider than the
    /// box you cannot walk through, so the trigger and the fixture's blocker have to be two
    /// colliders — and Unity delivers trigger callbacks to the GameObject carrying the collider.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class HubStation : MonoBehaviour
    {
        [Header("Prompt")]
        [Tooltip("Shown by HubPromptHUD while she stands here. Phrased as the verb she is about to " +
                 "perform, because the HUD draws it straight after the key: 'E  Choose Weapon'.")]
        [SerializeField] private string prompt = "Use";

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string interactActionName = "Interact";

        [Tooltip("Who can use this. Layer 6 (Player) — the same filter RoomEntry and ContactDamage " +
                 "use. The whole rig is on 6, so her hitbox and reticle trip this too; harmless, " +
                 "because presence is a yes/no and not a count.")]
        [SerializeField] private LayerMask playerLayers = 1 << 6;

        [Tooltip("How long after the last physics touch she still counts as standing here. Must " +
                 "outlast one fixed step (0.02s) with room to spare — see the note on _lastTouched.")]
        [SerializeField] private float presenceGrace = 0.15f;

        private InputAction _interact;
        private float _lastTouched = float.NegativeInfinity;

        /// <summary>Raised when she presses Interact inside the volume.</summary>
        public event Action Used;

        /// <summary>What the prompt HUD draws for this station.</summary>
        public string Prompt { get { return prompt; } }

        /// <summary>
        /// Whether she is standing in the volume right now.
        ///
        /// Measured as "touched recently" rather than by counting Enter/Exit pairs, because the
        /// player rig's colliders come and go: <c>AttackHitbox</c> enables and disables itself
        /// every swing, and a collider that is switched off inside a trigger does not reliably
        /// balance its Enter with an Exit. A leaked count leaves a station permanently occupied.
        /// Her Rigidbody2D is Never Sleep, so Stay keeps arriving while she stands perfectly still.
        ///
        /// Unscaled, because a modal panel sets timeScale to 0: scaled time would freeze the clock
        /// and hold the last touch "fresh" forever, so the prompt would still be up behind the panel.
        /// </summary>
        public bool PlayerInRange
        {
            get { return Time.unscaledTime - _lastTouched <= presenceGrace; }
        }

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError(name + ": no input asset assigned; this station cannot be used.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            _interact = map != null ? map.FindAction(interactActionName, false) : null;

            if (_interact == null)
            {
                Debug.LogError(name + ": action '" + actionMapName + "/" + interactActionName +
                               "' not found in " + inputActions.name + ".", this);
            }
        }

        private void OnEnable()
        {
            // Enabled through the action rather than the map, so RunPause still owns the map: it
            // disables the whole Player map for a modal panel, which switches this off with it.
            // That is what stops E re-firing at the rack while the weapon panel is open.
            if (_interact != null) _interact.Enable();

            // Pooling rule, even though nothing pools the camp today: a recycled instance never runs
            // Awake again, so presence resets here.
            _lastTouched = float.NegativeInfinity;
        }

        private void OnDisable()
        {
            if (_interact != null) _interact.Disable();
        }

        private void Update()
        {
            // Update, not FixedUpdate: WasPressedThisFrame is a render-frame edge and is missed
            // entirely when polled on the physics step.
            if (_interact == null || !PlayerInRange) return;
            if (!_interact.WasPressedThisFrame()) return;

            if (Used != null) Used();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null) return;
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            _lastTouched = Time.unscaledTime;
        }
    }
}
