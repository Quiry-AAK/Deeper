using Deeper.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds the test scene's debug menu and the room selector behind it, then wires both.
    ///
    /// Committed for the reason <see cref="BuildRunHUD"/> is: a panel assembled by dragging is one
    /// nobody can reproduce, review or diff — and this one is generated from the harness that
    /// happens to be in the scene, so re-running it after adding a spawner or a room layout is how
    /// the menu picks them up.
    ///
    /// Deliberately plain: default UGUI sprites, one font, no chrome. `TestOverlay` already records
    /// why — this is developer-facing text that the ART_DIRECTION §5 art pass does not touch, and
    /// dressing it would only make it look like shipped UI.
    /// </summary>
    public static class BuildTestConfigHUD
    {
        private const string InputAssetPath = "Assets/_Main/Input/InputSystem_Actions.inputactions";

        private static readonly string[] RoomPrefabPaths =
        {
            "Assets/_Main/Prefabs/Rooms/CombatRoom_UpperCaves_01.prefab",
            "Assets/_Main/Prefabs/Rooms/WaveRoom_UpperCaves_02.prefab",
        };

        // Drawn above both the run HUD and the overlay, because a debug menu that opens behind the
        // thing you are debugging is not a menu.
        private const int CanvasSortingOrder = 100;

        [MenuItem("Deeper/Build Test Config HUD")]
        private static void Build()
        {
            GameObject harness = GameObject.Find("TestHarness");
            if (harness == null)
            {
                Debug.LogError("No TestHarness in the open scene. Open TestScene first.");
                return;
            }

            TestRoomControls roomControls = Object.FindFirstObjectByType<TestRoomControls>();
            TestControls controls = Object.FindFirstObjectByType<TestControls>();
            TestSpawner[] spawners = Object.FindObjectsByType<TestSpawner>(FindObjectsSortMode.None);

            TestRoomSelector selector = BuildSelector(harness.transform, roomControls);

            Replace(harness.transform, "TestConfig");
            var root = new GameObject("TestConfig", typeof(RectTransform));
            root.transform.SetParent(harness.transform, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // The panel is clicked, so it needs its own raycaster — the Overlay canvas next door has
            // none, because nothing on it was ever meant to be interactive.
            root.AddComponent<GraphicRaycaster>();

            GameObject panel = Panel(root.transform);
            Text roomLabel = Label(panel.transform, "RoomLabel", "no room loaded");
            Transform roomRow = Row(panel.transform, "RoomRow");
            Transform actionRow = Grid(panel.transform, "ActionRow");
            Button template = ButtonTemplate(panel.transform);

            TestConfigHUD hud = root.AddComponent<TestConfigHUD>();
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("roomButtonRow").objectReferenceValue = roomRow;
            so.FindProperty("actionButtonRow").objectReferenceValue = actionRow;
            so.FindProperty("buttonTemplate").objectReferenceValue = template;
            so.FindProperty("roomLabel").objectReferenceValue = roomLabel;
            so.FindProperty("rooms").objectReferenceValue = selector;
            so.FindProperty("roomControls").objectReferenceValue = roomControls;
            so.FindProperty("controls").objectReferenceValue = controls;
            so.FindProperty("overlay").objectReferenceValue = Object.FindFirstObjectByType<TestOverlay>();
            so.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

            SerializedProperty spawnerList = so.FindProperty("spawners");
            spawnerList.arraySize = spawners.Length;
            for (int i = 0; i < spawners.Length; i++)
            {
                spawnerList.GetArrayElementAtIndex(i).objectReferenceValue = spawners[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("Built TestConfig: " + RoomPrefabPaths.Length + " rooms, " + spawners.Length +
                      " spawners.", root);
        }

        /// <summary>
        /// Creates the room selector and points it at `Level`, where the hand-mounted room used to
        /// sit. Also removes that mounted room: with a selector, a room in the scene is a second
        /// one that nothing manages and that overlaps whatever gets loaded.
        /// </summary>
        private static TestRoomSelector BuildSelector(Transform harness, TestRoomControls roomControls)
        {
            GameObject level = GameObject.Find("Level");

            if (level != null)
            {
                for (int i = level.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = level.transform.GetChild(i);
                    if (child.GetComponent<Deeper.Rooms.CombatRoom>() == null) continue;

                    Debug.Log("Removed hand-mounted room '" + child.name + "'; the selector loads it now.");
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            Replace(harness, "RoomSelector");
            var go = new GameObject("RoomSelector");
            go.transform.SetParent(harness, false);

            TestRoomSelector selector = go.AddComponent<TestRoomSelector>();
            SerializedObject so = new SerializedObject(selector);

            SerializedProperty prefabs = so.FindProperty("roomPrefabs");
            prefabs.arraySize = RoomPrefabPaths.Length;
            for (int i = 0; i < RoomPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPaths[i]);
                if (prefab == null) Debug.LogError("Missing room prefab: " + RoomPrefabPaths[i]);

                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }

            so.FindProperty("mountPoint").objectReferenceValue = level != null ? level.transform : null;
            so.FindProperty("roomControls").objectReferenceValue = roomControls;
            so.FindProperty("startIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            return selector;
        }

        private static void Replace(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        private static GameObject Panel(Transform parent)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            // Pushed down clear of TestOverlay's status line, which runs across the very top of the
            // screen on its own canvas — at -12 the two overlap and both become unreadable.
            rect.anchoredPosition = new Vector2(12f, -34f);
            rect.sizeDelta = new Vector2(660f, 0f);

            Image background = go.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.85f);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Label(go.transform, "Title", "— TEST CONFIG —   [`] close");
            return go;
        }

        private static Text Label(Transform parent, string name, string content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.text = content;
            text.color = Color.white;
            text.fontSize = 14;

            // No font assigned here on purpose: TestConfigHUD.Awake fills every label in, because
            // the font helper is internal to the runtime assembly and out of this tool's reach.

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 18f;

            return text;
        }

        private static Transform Row(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Both default to true, which stretches two buttons across the whole panel width and
            // leaves a gap you cannot tell from a missing button.
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 26f;

            return go.transform;
        }

        private static Transform Grid(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(160f, 24f);
            layout.spacing = new Vector2(4f, 4f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;

            // A GridLayoutGroup reports no preferred height to the parent's vertical layout, so
            // without this the panel closes up over it and the buttons draw outside the background.
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go.transform;
        }

        private static Button ButtonTemplate(Transform parent)
        {
            var go = new GameObject("ButtonTemplate", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(160f, 24f);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.28f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = Label(go.transform, "Label", "button");
            label.alignment = TextAnchor.MiddleCenter;

            // A room prefab's name is longer than any button worth putting on screen, and a clipped
            // "CombatRoom_Upp" does not say which layout it loads. Shrink to fit instead.
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 14;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            go.SetActive(false);
            return button;
        }
    }
}
