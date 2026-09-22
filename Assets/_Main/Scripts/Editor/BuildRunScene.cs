using Deeper.Meta;
using Deeper.Run;
using Deeper.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Deeper.EditorTools
{
    /// <summary>
    /// Builds `RunScene` — the descent itself. The Hub's shaft leads here.
    ///
    /// **This scene had to come back.** `FloorLoader` was written, verified in play mode and then
    /// mounted in nothing: the owner asked for a room-visual sandbox instead (2026-08-24) and the
    /// engineering plan has carried "`FloorLoader` has no scene… re-add a run scene when the run
    /// flow is the objective" ever since. It is the objective now.
    ///
    /// **Generated, not hand-authored**, the argument <see cref="BuildRoomPrefab"/>,
    /// <see cref="BuildRunHUD"/> and <see cref="BuildHubScene"/> all already make: a scene assembled
    /// by dragging is one nobody can reproduce, review or diff. It also answers
    /// `02-TEST_SCENE.md` §1's objection to a second scene — the camera, the light and the player
    /// come from the same prefabs `TestScene` uses, so the two cannot drift apart.
    ///
    /// `TestScene` is untouched and stays the sandbox. A run is a game loop; a sandbox is a place to
    /// park in one room and tune a weapon, and one scene cannot be both.
    /// </summary>
    public static class BuildRunScene
    {
        private const string ScenePath = "Assets/_Main/Scenes/RunScene.unity";
        private const string CameraPrefab = "Assets/_Main/Prefabs/Rig/Main Camera.prefab";
        private const string LightPrefab = "Assets/_Main/Prefabs/Rig/Global Light 2D.prefab";
        private const string PlayerPrefab = "Assets/_Main/Prefabs/Player.prefab";
        private const string RunPlanAsset = "Assets/_Main/Data/Run/RunPlan.asset";
        private const string ShardBankAsset = "Assets/_Main/Data/Meta/ShardBank.asset";

        /// <summary>
        /// A cave has no sky. Matched to the darkest step of the Upper Caves ramp rather than to
        /// black, so the edges of a room read as unlit stone instead of as a hole in the world.
        /// </summary>
        private static readonly Color Void = new Color(0.043f, 0.043f, 0.055f, 1f);

        [MenuItem("Deeper/Build Run Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light2D light = BuildLight();
            GameObject camera = BuildCamera();
            GameObject player = BuildPlayer();
            BuildEventSystem();

            var rig = camera != null ? camera.GetComponent<Deeper.CameraControl.CameraRig>() : null;
            if (rig != null && player != null) HUDLayout.Wire(rig, "target", player.transform);

            // Live is where mounted rooms end up. Staging MUST stay inactive: Unity defers Awake for
            // anything instantiated under an inactive parent, and that deferral is the only window
            // in which a room's encounter can be swapped — WaveSpawner builds its pools in Awake
            // exactly once and ActorPool has no dispose. FloorLoader.Start says so out loud if this
            // is ever switched on.
            var live = new GameObject("Live");
            var staging = new GameObject("Staging");
            staging.SetActive(false);

            var runObject = new GameObject("Run");
            RunSeed seed = runObject.AddComponent<RunSeed>();
            FloorLoader loader = runObject.AddComponent<FloorLoader>();
            RunEnd end = runObject.AddComponent<RunEnd>();

            // The HUD first: FloorLoader wants the depth readout, and BuildRunHUD creates the canvas
            // the other two panels then attach to.
            BuildRunHUD.Build();
            BuildUpgradePanel.Build();

            // Between the offer and the summary, and the order is the draw order: the pause menu
            // covers the run HUD, and the summary covers the pause menu — once a run is over there
            // is nothing left to pause.
            BuildPauseMenu.Build();
            BuildRunSummaryPanel.Build();

            HUDLayout.Wire(loader, "runPlan", AssetDatabase.LoadAssetAtPath<RunPlan>(RunPlanAsset));
            HUDLayout.Wire(loader, "liveRoot", live.transform);
            HUDLayout.Wire(loader, "stagingRoot", staging.transform);
            HUDLayout.Wire(loader, "seed", seed);
            HUDLayout.Wire(loader, "ambient", light);
            HUDLayout.Wire(loader, "depth", Object.FindFirstObjectByType<DepthIndicatorHUD>());
            HUDLayout.Wire(loader, "cameraRig", rig);

            HUDLayout.Wire(end, "floor", loader);
            HUDLayout.Wire(end, "bank", AssetDatabase.LoadAssetAtPath<ShardBank>(ShardBankAsset));
            HUDLayout.Wire(end, "pause", Object.FindFirstObjectByType<Deeper.Core.RunPause>());

            if (player != null)
            {
                HUDLayout.Wire(end, "health", player.GetComponent<Deeper.Combat.Damageable>());
                HUDLayout.Wire(end, "experience", player.GetComponentInChildren<Deeper.Player.PlayerXP>(true));
                HUDLayout.Wire(end, "loadout", player.GetComponent<Deeper.Character.RunLoadout>());
            }

            RunSummaryPanel summary = Object.FindFirstObjectByType<RunSummaryPanel>(FindObjectsInactive.Include);
            if (summary != null) HUDLayout.Wire(summary, "run", end);

            // Wired here rather than inside BuildPauseMenu, because the pause menu is built FIRST
            // so that the summary screen ends up drawing over it — at that point there is no
            // summary panel in the scene to find. PauseMenu falls back to a runtime search, but the
            // house rule is that the connection is visible in the Inspector.
            var menu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            if (menu != null) HUDLayout.Wire(menu, "summary", summary);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();

            Debug.Log("Built " + ScenePath + " — press Play, or descend from the Hub.",
                      AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static Light2D BuildLight()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefab);
            if (prefab == null) { Debug.LogError("No light prefab at " + LightPrefab); return null; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance.GetComponent<Light2D>();
        }

        private static GameObject BuildCamera()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab);
            if (prefab == null) { Debug.LogError("No camera prefab at " + CameraPrefab); return null; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // Set on the INSTANCE, not the shared prefab, exactly as BuildHubScene sets its night
            // sky: the same camera serves TestScene and the room lab, and this colour is the run's.
            var camera = instance.GetComponent<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Void;
            }

            return instance;
        }

        /// <summary>
        /// Dropped at the origin and left there. `FloorLoader` moves her onto the first room's
        /// arrival marker in its own Start and snaps the camera after — placing her here as well
        /// would be a second answer to the same question that could disagree with it.
        /// </summary>
        private static GameObject BuildPlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (prefab == null) { Debug.LogError("No player prefab at " + PlayerPrefab); return null; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;

            // Her rig carries this on the prefab, but FloorLoader and RunEnd both find her by tag and
            // an untagged player fails as a null reference three systems away from the cause.
            if (instance.tag != "Player")
            {
                Debug.LogError("Player.prefab is not tagged Player, so FloorLoader cannot place " +
                               "her and RunEnd cannot hear her die.", instance);
            }

            return instance;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// Added to Build Settings, because `HubDescent` loads it **by name** and
        /// <c>SceneManager.LoadScene</c> throws for a scene that is not listed.
        /// </summary>
        private static void AddToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene entry in scenes)
            {
                if (entry.path == ScenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            // Flushed, not left dirty. The assignment above only marks EditorBuildSettings.asset
            // dirty in memory; without this it reaches disk on the next File > Save Project, so a
            // fresh clone that ran this tool and then played the Hub would descend into a scene
            // Unity insists is not in the build.
            AssetDatabase.SaveAssets();
        }
    }
}
