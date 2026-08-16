using Deeper.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.Testing
{
    /// <summary>
    /// The sandbox's debug menu: every cheat as a labelled button, plus the room selector.
    ///
    /// TEST-ONLY — this belongs to `TestScene`. It exists because the function row ran out. F1-F3
    /// are the player cheats, F4-F9 the spawners, F10 the readout and F12 the room re-arm, and the
    /// note left on `TestRoomControls.rearmKey` — "the next addition needs a modifier chord or a
    /// real debug menu" — came due the moment a second room layout existed. This is that menu, so
    /// new harness features cost a button instead of a key.
    ///
    /// **The keys still work.** Every button calls the same public method its key calls, so there
    /// is one implementation per cheat and no muscle memory is broken. This panel is another way in,
    /// not a replacement, which is also why nothing here duplicates a cheat's logic.
    ///
    /// It opens on the backquote/tilde key rather than a function key — the conventional debug
    /// console key, and the obvious one left that the player action map does not use.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestConfigHUD : MonoBehaviour
    {
        [Header("Widgets")]
        [Tooltip("The whole menu. Starts inactive: it gates player input while open, so a panel " +
                 "that opened itself on Play would leave her unable to move until it was closed.")]
        [SerializeField] private GameObject panel;

        [Tooltip("Where the room buttons are generated, one per prefab in the selector.")]
        [SerializeField] private Transform roomButtonRow;

        [Tooltip("Where the cheat buttons are generated, one per public method the keys drive.")]
        [SerializeField] private Transform actionButtonRow;

        [Tooltip("Cloned for every generated button. Lives inactive in the panel.")]
        [SerializeField] private Button buttonTemplate;

        [Tooltip("Reads out which room is loaded and what it is doing.")]
        [SerializeField] private Text roomLabel;

        [Header("Sources")]
        [SerializeField] private TestRoomSelector rooms;
        [SerializeField] private TestRoomControls roomControls;
        [SerializeField] private TestControls controls;
        [SerializeField] private TestSpawner[] spawners;

        [Tooltip("Its key legend is hidden while this menu is open — the menu lists the same cheats " +
                 "as buttons, and the two drawn on top of each other are unreadable.")]
        [SerializeField] private TestOverlay overlay;

        [Header("Input")]
        [Tooltip("Opens and closes the menu.")]
        [SerializeField] private Key toggleKey = Key.Backquote;

        [Tooltip("Disabled while the menu is open, and re-enabled when it closes. Without this a " +
                 "click on a button ALSO swings the katana: every player system reads its " +
                 "InputAction straight off this asset, and UGUI's EventSystem is nowhere in that " +
                 "path, so it cannot swallow the click for them.")]
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private string actionMapName = "Player";

        private InputActionMap _playerMap;
        private bool _cursorWasVisible;

        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Awake()
        {
            if (rooms == null) rooms = FindFirstObjectByType<TestRoomSelector>();
            if (roomControls == null) roomControls = FindFirstObjectByType<TestRoomControls>();
            if (controls == null) controls = FindFirstObjectByType<TestControls>();
            if (overlay == null) overlay = FindFirstObjectByType<TestOverlay>();
            if (spawners == null || spawners.Length == 0) spawners = FindObjectsByType<TestSpawner>(FindObjectsSortMode.None);

            if (inputActions != null) _playerMap = inputActions.FindActionMap(actionMapName, false);

            // Every label at once, including the ones the editor builder made: LegacyUIFont is
            // internal to the runtime assembly, so the builder cannot reach it, and a Text with no
            // font draws nothing at all rather than drawing badly.
            foreach (Text text in GetComponentsInChildren<Text>(true)) LegacyUIFont.EnsureFont(text);
        }

        private void Start()
        {
            BuildButtons();
            SetOpen(false);
        }

        private void OnDisable()
        {
            // Both restored unconditionally on the way out. A leaked disable leaves the player
            // unable to move or attack for the rest of the session, which looks exactly like a bug
            // in the input system rather than a panel that forgot to clean up; a leaked hidden
            // legend is the same mistake in a cheaper costume.
            if (_playerMap != null) _playerMap.Enable();
            if (overlay != null) overlay.SetLegendVisible(true);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || toggleKey == Key.None) return;

            if (keyboard[toggleKey].wasPressedThisFrame) SetOpen(!IsOpen);

            if (IsOpen) RefreshRoomLabel();
        }

        /// <summary>Public so a probe can drive it — simulated key presses never reach play mode.</summary>
        [ContextMenu("Toggle")]
        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (panel == null) return;

            panel.SetActive(open);

            if (overlay != null) overlay.SetLegendVisible(!open);

            if (_playerMap != null)
            {
                if (open) _playerMap.Disable();
                else _playerMap.Enable();
            }

            // PlayerAim hides the hardware cursor while a reticle is drawn, which would leave this
            // panel unclickable — the buttons are there but there is nothing to click them with.
            if (open)
            {
                _cursorWasVisible = Cursor.visible;
                Cursor.visible = true;
            }
            else
            {
                Cursor.visible = _cursorWasVisible;
            }

            if (open) RefreshRoomLabel();
        }

        /// <summary>Loads a room by index. Wired onto the generated room buttons.</summary>
        public void LoadRoom(int index)
        {
            if (rooms != null) rooms.Load(index);
        }

        private void BuildButtons()
        {
            if (buttonTemplate == null) return;

            buttonTemplate.gameObject.SetActive(false);

            if (rooms != null && roomButtonRow != null)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    // Captured into a local: the loop variable is shared by every closure
                    // otherwise, so all the buttons would load whichever room the loop ended on.
                    int index = i;
                    Add(roomButtonRow, rooms.NameAt(i), delegate { LoadRoom(index); });
                }
            }

            if (actionButtonRow == null) return;

            // Each of these is the same public method the key calls — never a copy of its logic.
            if (roomControls != null) Add(actionButtonRow, "Re-arm Room", roomControls.Rearm);

            if (controls != null)
            {
                Add(actionButtonRow, "Fill Ultimate", controls.FillUltimate);
                Add(actionButtonRow, "Heal", controls.HealPlayer);
                Add(actionButtonRow, "Reset", controls.ResetPlayer);
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                TestSpawner spawner = spawners[i];
                if (spawner == null) continue;

                Add(actionButtonRow, "+ " + spawner.name, spawner.Spawn);
                Add(actionButtonRow, "x " + spawner.name, spawner.Clear);
            }
        }

        private void Add(Transform row, string label, UnityEngine.Events.UnityAction onClick)
        {
            Button button = Instantiate(buttonTemplate, row);
            button.gameObject.SetActive(true);
            button.gameObject.name = label;

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                LegacyUIFont.EnsureFont(text);
                text.text = label;
            }

            button.onClick.AddListener(onClick);
        }

        private void RefreshRoomLabel()
        {
            if (roomLabel == null) return;

            if (rooms == null || rooms.Current == null)
            {
                roomLabel.text = "no room loaded";
                return;
            }

            string status = roomControls != null ? roomControls.Status : string.Empty;

            roomLabel.text = rooms.NameAt(rooms.CurrentIndex) +
                             (rooms.Current.IsWaveRoom ? "   WAVE ROOM" : "   standard") +
                             "   " + status;
        }
    }
}
