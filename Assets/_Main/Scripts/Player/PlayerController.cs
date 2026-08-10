using Deeper.Animation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deeper.Player
{
    /// <summary>
    /// 8-directional top-down movement at a fixed speed with no acceleration curve, per GDD
    /// §Player. Speed is read from <see cref="PlayerStats"/> every frame, so equipment and (later)
    /// upgrades and Hub stats change it without touching this class.
    ///
    /// Input comes from the <c>.inputactions</c> asset rather than hardcoded keys, so rebinding
    /// stays a data change (see the Milestone 1 input risk note in the engineering plan).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";

        [Header("Refs — resolved from this GameObject when empty")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private CharacterAnimator characterAnimator;

        private Rigidbody2D _body;
        private InputAction _moveAction;
        private Vector2 _input;

        /// <summary>Current movement input, already clamped to length 1.</summary>
        public Vector2 MoveInput => _input;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerController)}: no input asset assigned; the player will not move.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            _moveAction = map != null ? map.FindAction(moveActionName, false) : null;

            if (_moveAction == null)
            {
                Debug.LogError($"{nameof(PlayerController)}: action '{actionMapName}/{moveActionName}' not found in {inputActions.name}.", this);
            }
        }

        private void OnEnable()
        {
            if (_moveAction != null) _moveAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null) _moveAction.Disable();

            // Don't leave the body drifting if this is disabled mid-stride.
            if (_body != null) _body.linearVelocity = Vector2.zero;
            _input = Vector2.zero;
        }

        private void Update()
        {
            Vector2 raw = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            // Clamp rather than normalize: keeps analog sticks analog while making sure a
            // diagonal on the keyboard isn't faster than a cardinal.
            _input = Vector2.ClampMagnitude(raw, 1f);

            if (characterAnimator != null) characterAnimator.SetMotion(_input);
        }

        private void FixedUpdate()
        {
            float speed = stats != null ? stats.MoveSpeed : 0f;
            _body.linearVelocity = _input * speed;
        }
    }
}
