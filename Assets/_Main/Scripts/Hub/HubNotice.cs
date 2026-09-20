using Deeper.UI;
using UnityEngine;

namespace Deeper.Hub
{
    /// <summary>
    /// A camp fixture that is built but has nothing behind it yet — the stat shrine, and the Relic
    /// Vault when it is placed. Using it says so on the prompt line.
    ///
    /// **A station that answers is better than one that ignores you.** The alternative was to leave
    /// the shrine with no listener at all, which reads in play as a broken fixture: she stands
    /// there, the prompt says the key works, and pressing it does nothing. Saying "not yet" is
    /// honest and costs one line of text.
    ///
    /// This is scaffolding with a known end date. The shrine is Milestone 6's Hub Stat System
    /// (CONTENT_DESIGN §7, BALANCE §15) and the vault is the Relic Vault; when either is built, its
    /// panel takes the station's <c>Used</c> event and this component comes off the fixture.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubNotice : MonoBehaviour
    {
        [Header("Opened by")]
        [SerializeField] private HubStation station;

        [Header("Message")]
        [Tooltip("Said on the prompt line when the fixture is used. Name what will eventually be " +
                 "here, so the camp reads as unfinished rather than as broken.")]
        [SerializeField, TextArea(1, 3)] private string message = "NOT BUILT YET.";

        [Tooltip("Where it is said. Found in the scene when empty.")]
        [SerializeField] private HubPromptHUD prompt;

        private void Awake()
        {
            if (prompt == null) prompt = FindFirstObjectByType<HubPromptHUD>();
        }

        private void OnEnable()
        {
            if (station != null) station.Used += Announce;
        }

        private void OnDisable()
        {
            if (station != null) station.Used -= Announce;
        }

        [ContextMenu("Announce")]
        public void Announce()
        {
            if (prompt != null) prompt.Say(message);
        }
    }
}
