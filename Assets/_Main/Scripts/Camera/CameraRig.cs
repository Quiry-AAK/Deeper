using UnityEngine;

namespace Deeper.CameraControl
{
    /// <summary>
    /// Smooth-follow camera with impact shake.
    ///
    /// A static camera is one of the biggest reasons a top-down game feels stiff: the world never
    /// reacts to the player. Following with a small lag makes movement feel weighty, and a short
    /// positional shake on impact sells hits far more cheaply than any amount of extra sprite work.
    ///
    /// Runs in LateUpdate so it reads the player's final position for the frame, and shakes on
    /// **unscaled** time so a hitstop freeze doesn't also freeze the shake — the two need to
    /// overlap or the impact reads as a stall instead of a hit.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("Target to follow. Falls back to the object tagged Player.")]
        [SerializeField] private Transform target;

        [Tooltip("Seconds to close most of the gap. Higher = laggier, floatier. 0.12-0.2 reads well.")]
        [SerializeField] private float followSmoothing = 0.15f;

        [Tooltip("Camera offset from the target. Z must stay negative for a 2D camera.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Look-ahead")]
        [Tooltip("How far the camera leads the player's movement, in world units. Gives the view " +
                 "a sense of intent instead of trailing passively.")]
        [SerializeField] private float lookAhead = 1.1f;

        [SerializeField] private float lookAheadSmoothing = 0.35f;

        [Header("Shake")]
        [Tooltip("Global multiplier. Set 0 to disable shake without unwiring anything.")]
        [SerializeField] private float shakeScale = 1f;

        [Tooltip("How fast a shake decays. Higher = snappier, less lingering rumble.")]
        [SerializeField] private float shakeDecay = 9f;

        private Vector3 _velocity;
        private Vector2 _lookAheadCurrent;
        private Vector2 _lookAheadVelocity;
        private float _shakeAmount;
        private float _seed;

        private Deeper.Player.PlayerController _player;

        private void Awake()
        {
            _seed = Random.value * 100f;

            if (target == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) target = go.transform;
            }

            // In children too: the player's scripts are split across subsystem child objects, and
            // only the movement controller is pinned to the root by its rigidbody.
            if (target != null) _player = target.GetComponentInChildren<Deeper.Player.PlayerController>(true);
            if (target != null) transform.position = target.position + offset;
        }

        /// <summary>
        /// The offset shake is currently adding to the camera. Anything unprojecting screen
        /// coordinates (the aim reticle) must subtract this, or it wobbles with the shake even
        /// though the pointer never moved.
        /// </summary>
        public Vector3 ShakeOffset { get; private set; }

        /// <summary>Kicks the camera. Callers pass intensity in world units, typically 0.05-0.3.</summary>
        public void Shake(float amount)
        {
            if (amount <= 0f) return;

            // Take the strongest request rather than summing, so a chain of hits cannot escalate
            // into an unreadable screen.
            _shakeAmount = Mathf.Max(_shakeAmount, amount * shakeScale);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector2 intent = _player != null ? _player.MoveInput : Vector2.zero;
            _lookAheadCurrent = Vector2.SmoothDamp(
                _lookAheadCurrent, intent * lookAhead, ref _lookAheadVelocity, lookAheadSmoothing);

            Vector3 desired = target.position + offset
                + new Vector3(_lookAheadCurrent.x, _lookAheadCurrent.y, 0f);

            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _velocity, followSmoothing);

            if (_shakeAmount <= 0.0001f)
            {
                ShakeOffset = Vector3.zero;
                return;
            }

            // Unscaled so shake survives hitstop. Perlin rather than Random keeps it a smooth
            // wobble instead of per-frame jitter, which reads as noise.
            float t = Time.unscaledTime * 28f;
            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 17f, t) - 0.5f) * 2f;
            ShakeOffset = new Vector3(x, y, 0f) * _shakeAmount;
            transform.position += ShakeOffset;

            _shakeAmount = Mathf.MoveTowards(
                _shakeAmount, 0f, shakeDecay * _shakeAmount * Time.unscaledDeltaTime + 0.0005f);
        }
    }
}
