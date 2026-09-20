using Deeper.Character;
using Deeper.Core;
using Deeper.Hub;
using Deeper.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Deeper.UI
{
    /// <summary>
    /// The weapon rack's screen — GDD §Player: *"Player chooses 1 of 3 weapons in the Hub before
    /// descending. Locked for the full run. All 3 are unlocked from the start — no gating."*
    ///
    /// It writes the pick to two places on purpose, and they are not redundant:
    /// <see cref="RunConfig"/> is what survives the load into the run scene, and
    /// <see cref="RunLoadout"/> is what the player standing in the camp is holding *now* — so her
    /// drawn weapon layer changes the moment she picks, instead of only once she has descended.
    ///
    /// There is no confirm step and no cancel. A pick is reversible right up until she walks into
    /// the shaft, so a dialog asking her to be sure would be guarding nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSelectPanel : MonoBehaviour
    {
        [Header("Opened by")]
        [Tooltip("The weapon rack. This panel subscribes to it rather than the rack knowing about " +
                 "a screen — a fixture is only ever 'she pressed E here'.")]
        [SerializeField] private HubStation station;

        [Header("Widgets")]
        [Tooltip("The whole screen. Starts inactive: it pauses the game while open, so a panel " +
                 "that opened itself on Play would freeze the camp.")]
        [SerializeField] private GameObject panel;

        [SerializeField] private WeaponCard[] cards = new WeaponCard[0];
        [SerializeField] private Text headerLabel;
        [SerializeField] private Button closeButton;

        [Header("Sources")]
        [Tooltip("The run's choice, and the list of weapons to show. Survives the load into the run.")]
        [SerializeField] private RunConfig config;

        [Tooltip("The player in the camp, so the pick is visible on her before she descends. " +
                 "Found by tag when empty.")]
        [SerializeField] private RunLoadout loadout;

        [Header("Pause")]
        [SerializeField] private RunPause pause;

        private bool _holdingPause;

        /// <summary>Whether the rack's screen is on show.</summary>
        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Awake()
        {
            if (loadout == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) loadout = player.GetComponentInChildren<RunLoadout>(true);
            }

            if (pause == null) pause = FindFirstObjectByType<RunPause>();

            foreach (WeaponCard card in cards)
            {
                if (card != null) card.Picked += HandlePicked;
            }

            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            foreach (WeaponCard card in cards)
            {
                if (card != null) card.Picked -= HandlePicked;
            }

            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        private void OnEnable()
        {
            if (station != null) station.Used += Open;

            Close();
        }

        private void OnDisable()
        {
            if (station != null) station.Used -= Open;

            // Never leave the camp paused because this object went away with the panel open.
            Release();
        }

        /// <summary>Shows the rack. Public so the sandbox and a probe can open it without walking there.</summary>
        [ContextMenu("Open")]
        public void Open()
        {
            if (panel == null || config == null) return;

            WeaponDefinition[] offered = config.Available;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                cards[i].Bind(i < offered.Length ? offered[i] : null);
            }

            PaintSelection();

            if (headerLabel != null)
            {
                LegacyUIFont.EnsureFont(headerLabel);
                headerLabel.text = "CHOOSE YOUR WEAPON";
            }

            panel.SetActive(true);

            if (_holdingPause || pause == null) return;

            pause.Push();
            _holdingPause = true;
        }

        /// <summary>Hides the rack and hands the camp back.</summary>
        [ContextMenu("Close")]
        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            Release();
        }

        private void HandlePicked(WeaponCard card)
        {
            if (!IsOpen || card == null || card.Weapon == null) return;

            // RunConfig first: it is the half that survives the scene load, and the half that would
            // be missed if the player quit the camp straight after picking.
            if (config != null) config.Choose(card.Weapon);
            if (loadout != null) loadout.SetWeapon(card.Weapon);

            PaintSelection();
        }

        private void PaintSelection()
        {
            WeaponDefinition chosen = config != null ? config.Weapon : null;

            foreach (WeaponCard card in cards)
            {
                if (card != null) card.SetSelected(card.Weapon != null && card.Weapon == chosen);
            }
        }

        private void Release()
        {
            if (!_holdingPause) return;

            _holdingPause = false;
            if (pause != null) pause.Pop();
        }
    }
}
