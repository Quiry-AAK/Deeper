using System.Collections;
using Deeper.Run;
using Deeper.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deeper.Hub
{
    /// <summary>
    /// The mine shaft — the way out of the camp and into a run (GDD §Game Loop 2: *"Hub: view/spend
    /// Shards, select Weapon, start run"*).
    ///
    /// It refuses to descend with no weapon chosen, and says so on the prompt line rather than
    /// loading a run she cannot fight. That is not a hypothetical: <see cref="RunConfig"/> ships
    /// with a default, but an empty one is one Inspector click away and the failure would otherwise
    /// surface as a player with no attacks, three scenes later.
    ///
    /// **The descent is played, not cut to.** A hard scene load on the keypress reads as the game
    /// glitching rather than as going underground — the one moment the whole Hub exists to set up.
    /// She walks into the shaft's mouth, sinks into it behind the brick lip, and the screen goes
    /// dark; only then does the run load. The shaft's art is split into two sprites for exactly
    /// this (see <c>BuildHubArt.SplitShaft</c>) — with one sprite she would sink in front of the
    /// structure rather than into it.
    ///
    /// **The destination is a scene name, not a build index.** Indices renumber whenever anything is
    /// reordered in Build Settings, and the failure is silent — you descend into the wrong scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubDescent : MonoBehaviour
    {
        [Header("Opened by")]
        [Tooltip("The shaft's station. This subscribes to it rather than the fixture knowing what " +
                 "descending means.")]
        [SerializeField] private HubStation station;

        [Header("Destination")]
        [Tooltip("The scene a run is played in. Built by Deeper/Build Run Scene, which mounts the " +
                 "FloorLoader. It pointed at TestScene while no run scene existed — and this was " +
                 "always going to be a field change rather than a code change, which is why the " +
                 "destination is serialized at all. Must be in Build Settings or the load throws.")]
        [SerializeField] private string runScene = "RunScene";

        [Header("Sources")]
        [Tooltip("Checked for a weapon before the descent is allowed.")]
        [SerializeField] private RunConfig config;

        [Tooltip("Where the refusal is said. Found in the scene when empty.")]
        [SerializeField] private HubPromptHUD prompt;

        [SerializeField] private string noWeaponMessage = "PICK A WEAPON AT THE RACK FIRST.";

        [Header("Descent")]
        [Tooltip("The mouth of the shaft she climbs down into. The station's own transform, which " +
                 "already sits on the pit.")]
        [SerializeField] private Transform shaftMouth;

        [Tooltip("Full-screen black. Switched on when the descent starts and never off — the scene " +
                 "load is what clears it.")]
        [SerializeField] private Image fade;

        [Tooltip("The shared action asset. Its Player map is disabled for the descent so she " +
                 "cannot walk out of her own animation.")]
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private string actionMapName = "Player";

        [Tooltip("Seconds spent walking to the shaft's mouth.")]
        [SerializeField] private float approachSeconds = 0.45f;

        [Tooltip("Seconds spent sinking out of sight.")]
        [SerializeField] private float sinkSeconds = 0.7f;

        [Tooltip("How far she sinks, in world units. Enough to be well under the brick lip.")]
        [SerializeField] private float sinkDepth = 1.6f;

        [Tooltip("Seconds the screen takes to reach full black, overlapping the sink.")]
        [SerializeField] private float fadeSeconds = 0.55f;

        [SerializeField] private string playerTag = "Player";

        private bool _descending;

        private void Awake()
        {
            if (prompt == null) prompt = FindFirstObjectByType<HubPromptHUD>();
        }

        private void OnEnable()
        {
            if (station != null) station.Used += Descend;
        }

        private void OnDisable()
        {
            if (station != null) station.Used -= Descend;
        }

        /// <summary>
        /// Starts the run. Public so the sandbox and a probe can descend without walking to the
        /// shaft — simulated key input never reaches play mode (Engineering/01-VERIFICATION.md §2).
        /// </summary>
        [ContextMenu("Descend")]
        public void Descend()
        {
            // A scene load takes until the end of the frame, and the station is perfectly capable of
            // raising Used twice before it completes if the key repeats. Loading the same scene
            // twice tears down the one that is mid-load.
            if (_descending) return;

            if (config == null || config.Weapon == null)
            {
                if (prompt != null) prompt.Say(noWeaponMessage);
                return;
            }

            if (string.IsNullOrEmpty(runScene))
            {
                Debug.LogError(name + ": no run scene named, so the shaft goes nowhere.", this);
                return;
            }

            _descending = true;
            StartCoroutine(Descending());
        }

        private IEnumerator Descending()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            // Hand control over before moving her, or she walks out of the animation. Disabling the
            // whole map also stops a second Interact reaching the station behind her.
            InputActionMap map = inputActions != null
                ? inputActions.FindActionMap(actionMapName, false)
                : null;
            if (map != null) map.Disable();

            Rigidbody2D body = player != null ? player.GetComponent<Rigidbody2D>() : null;

            // Physics off rather than moved through: she is about to be put somewhere her collider
            // would refuse to go — inside the shaft's blocker, then below the floor.
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            Vector3 mouth = shaftMouth != null ? shaftMouth.position : start;

            // Keep her own depth on the approach; only the walk across matters here.
            mouth.z = start.z;

            if (player != null) yield return Move(player.transform, start, mouth, approachSeconds);

            if (fade != null) fade.enabled = true;

            Vector3 bottom = mouth + Vector3.down * sinkDepth;
            float elapsed = 0f;
            float duration = Mathf.Max(sinkSeconds, 0.01f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Eased in: she steps off the edge slowly and drops away, rather than moving at a
                // constant speed like a lift.
                if (player != null) player.transform.position = Vector3.Lerp(mouth, bottom, t * t);

                if (fade != null)
                {
                    Color colour = fade.color;
                    colour.a = Mathf.Clamp01(elapsed / Mathf.Max(fadeSeconds, 0.01f));
                    fade.color = colour;
                }

                yield return null;
            }

            // Hold on full black for a beat so the cut does not land on a half-faded frame.
            if (fade != null)
            {
                Color colour = fade.color;
                colour.a = 1f;
                fade.color = colour;
            }

            // Hand time and input back before leaving. Nothing here pauses, but a panel closing in
            // the same frame could still hold RunPause, and timeScale 0 survives a scene load —
            // arriving in the run frozen looks exactly like a hang. The map has to be re-enabled for
            // the same reason: it is the shared asset, and it would arrive disabled.
            Time.timeScale = 1f;
            if (map != null) map.Enable();

            SceneManager.LoadScene(runScene);
        }

        private IEnumerator Move(Transform subject, Vector3 from, Vector3 to, float seconds)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(seconds, 0.01f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                subject.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            subject.position = to;
        }
    }
}
