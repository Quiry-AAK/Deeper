using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Deeper.Rooms;
using Deeper.Testing;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds `RoomVisualTestScene` — the isometric sandbox for looking at rooms. One button; press
    /// it and a different room appears.
    ///
    /// **Generated, not hand-authored**, the same argument `BuildRoomPrefab` and `BuildRunHUD`
    /// already make: a scene assembled by dragging is one nobody can reproduce, review or diff. It
    /// is also the answer to `02-TEST_SCENE.md` §1's objection to a second scene — the camera and
    /// light come from the same prefabs `TestScene` uses, so the two cannot drift apart.
    ///
    /// The isometric `Grid` is the whole projection. A 2:1 diamond is the classic pixel-art ratio:
    /// every edge is a clean two-across-one-down staircase, where a true 30° isometric would need
    /// anti-aliased diagonals that do not exist at 32px.
    /// </summary>
    public static class BuildRoomVisualTestScene
    {
        private const string ScenePath = "Assets/_Main/Scenes/RoomVisualTestScene.unity";
        private const string CameraPrefab = "Assets/_Main/Prefabs/Rig/Main Camera.prefab";
        private const string LightPrefab = "Assets/_Main/Prefabs/Rig/Global Light 2D.prefab";
        private const string Layouts = "Assets/_Main/Data/Layouts";
        private const string IsoArt = "Assets/_Main/Art/Environment/UpperCaves/Iso";
        private const string Props = "Assets/_Main/Art/Environment/UpperCaves";

        /// <summary>Diamond footprint in world units: 64x32 px at 32 PPU.</summary>
        private static readonly Vector3 CellSize = new Vector3(2f, 1f, 1f);

        [MenuItem("Deeper/Build Room Visual Test Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefab));
            var cameraObject = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab));

            // Nothing to follow: there is no player in this scene. Disabled rather than removed so
            // the shared prefab stays untouched and the reason is visible in the Inspector.
            var rig = cameraObject.GetComponent<Deeper.CameraControl.CameraRig>();
            if (rig != null) rig.enabled = false;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            Grid grid = BuildGrid();
            Tilemap floor = BuildFloor(grid);
            Transform scenery = new GameObject("Scenery").transform;

            var labObject = new GameObject("RoomVisualTest");
            RoomVisualTest lab = labObject.AddComponent<RoomVisualTest>();
            Wire(lab, grid, floor, scenery, cameraObject.GetComponent<Camera>());

            BuildHud(lab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();

            Debug.Log("Built " + ScenePath + " — Play it and press the button to re-roll the room.",
                      AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static Grid BuildGrid()
        {
            var go = new GameObject("Level", typeof(Grid));
            Grid grid = go.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = CellSize;
            return grid;
        }

        private static Tilemap BuildFloor(Grid grid)
        {
            var go = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform, false);

            TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -20;

            // Per-tile sorting, not chunked. An isometric floor tile is a diamond with a slab under
            // it, so a near tile has to be drawn OVER the one behind to hide that slab's edge.
            // Chunked mode draws the whole map as one mesh with no such ordering, which turned a
            // flat floor into a field of raised blocks — every slab edge visible at once.
            renderer.mode = TilemapRenderer.Mode.Individual;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;

            return go.GetComponent<Tilemap>();
        }

        private static void Wire(RoomVisualTest lab, Grid grid, Tilemap floor, Transform scenery,
                                 Camera view)
        {
            var so = new SerializedObject(lab);

            SetArray(so.FindProperty("layouts"), Load<RoomLayoutAsset>(Layouts, "t:RoomLayoutAsset"));
            SetArray(so.FindProperty("floorTiles"), IsoTiles("Floor_"));
            SetArray(so.FindProperty("wallBlocks"), IsoSprites("Wall_"));
            SetArray(so.FindProperty("props"), PropSprites());

            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("floor").objectReferenceValue = floor;
            so.FindProperty("scenery").objectReferenceValue = scenery;
            so.FindProperty("view").objectReferenceValue = view;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object[] Load<T>(string folder, string filter) where T : Object
        {
            var found = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) found.Add(asset);
            }
            return found.ToArray();
        }

        private static Object[] IsoTiles(string prefix)
        {
            var found = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { "Assets/_Main/Data/Tiles" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Iso/")) continue;
                if (!Path.GetFileName(path).StartsWith("Tile_Iso_" + prefix)) continue;
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null) found.Add(tile);
            }
            return found.ToArray();
        }

        private static Object[] IsoSprites(string prefix)
        {
            var found = new List<Object>();
            if (!Directory.Exists(IsoArt)) return found.ToArray();

            foreach (string file in Directory.GetFiles(IsoArt, prefix + "*.png"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToAssetPath(file));
                if (sprite != null) found.Add(sprite);
            }
            return found.ToArray();
        }

        private static Object[] PropSprites()
        {
            var found = new List<Object>();
            if (!Directory.Exists(Props)) return found.ToArray();

            foreach (string file in Directory.GetFiles(Props, "Prop_*.png"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToAssetPath(file));
                if (sprite != null) found.Add(sprite);
            }
            return found.ToArray();
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void BuildHud(RoomVisualTest lab)
        {
            var canvasObject = new GameObject("LabCanvas", typeof(Canvas), typeof(CanvasScaler),
                                              typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasObject.AddComponent<Deeper.UI.PixelPerfectHUDScale>();

            Button button = NewButton(canvas.transform, "Re-roll Room  (F1)");
            Text readout = NewReadout(canvas.transform);

            var hud = canvasObject.AddComponent<RoomVisualTestHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("lab").objectReferenceValue = lab;
            so.FindProperty("reRollButton").objectReferenceValue = button;
            so.FindProperty("readout").objectReferenceValue = readout;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button NewButton(Transform parent, string label)
        {
            var go = new GameObject("ReRoll", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(8f, -8f);
            rect.sizeDelta = new Vector2(136f, 22f);
            go.GetComponent<Image>().color = new Color(0.34f, 0.32f, 0.36f, 0.95f);

            Text text = NewText(rect, label, TextAnchor.MiddleCenter);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static Text NewReadout(Transform parent)
        {
            Text text = NewText(parent, "", TextAnchor.UpperLeft);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(8f, -34f);
            rect.sizeDelta = new Vector2(460f, 40f);
            return text;
        }

        private static Text NewText(Transform parent, string value, TextAnchor anchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.text = value;
            text.alignment = anchor;
            text.fontSize = 11;
            text.color = Color.white;

            // The project's generated pixel face when it exists, the built-in otherwise. Not
            // LegacyUIFont: that is `internal` to Assembly-CSharp and this is an editor assembly.
            var pixel = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Main/Art/UI/HUD_Font.fontsettings");
            text.font = pixel != null ? pixel : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static string ToAssetPath(string file)
        {
            string path = file.Replace('\\', '/');
            int index = path.IndexOf("Assets/");
            return index > 0 ? path.Substring(index) : path;
        }

        private static void AddToBuildSettings()
        {
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.path == ScenePath) return;
            }

            var scenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length + 1];
            EditorBuildSettings.scenes.CopyTo(scenes, 0);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }
    }
}
