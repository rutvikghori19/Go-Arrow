#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using _Game.ProceduralLevels;
using _Game.Theme;
using _Game.UI;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI.Editor
{
    public static class GameScenePrefabBuilder
    {
        const string PrefabFolder = "Assets/_Game/Prefabs/Game";
        const string UiPrefabPath = PrefabFolder + "/GameUI.prefab";
        const string CameraPrefabPath = PrefabFolder + "/GameCamera.prefab";
        const string ManagersPrefabPath = PrefabFolder + "/GameManagers.prefab";
        const string LevelManagerPrefabPath = PrefabFolder + "/GameLevelManager.prefab";
        const string ScenePath = "Assets/_Game/Scenes/GameScene.unity";
        const string UiSourcePath = "Assets/SerapKeremGameKit/Resources/UI/UI.prefab";
        const string CameraSourcePath = "Assets/SerapKeremGameKit/Resources/Camera/CameraManager.prefab";
        const string LevelManagerSourcePath = "Assets/SerapKeremGameKit/Resources/Managers/LevelManager.prefab";
        const string LevelsFolder = "Assets/_Game/Resources/Levels";
        const string LevelBaseResourcePath = "Assets/_Game/Resources/Levels/Level_Base.prefab";

        static readonly string[] ManagerResourcePaths =
        {
            "Managers/GameManager",
            "Managers/StateManager",
            "Managers/ParticleManager",
            "Tile/TileManager",
            "Managers/AudioManager",
            "Managers/HapticManager",
            "Time/TimeManager",
            "Game/InputHandler",
            "Tile/TileSpawner",
            "Particle/ParticlePool",
            "Audio/AudioPool",
            "Game/Selector",
        };

        static readonly string[] PreservedSceneRoots =
        {
            "EventSystem",
            "Directional Light",
            "Global Volume",
            "Spawner",
        };

        [MenuItem("Go-Arrow/Build Game Scene Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabFolder);

            var cameraPrefab = BuildCameraPrefab();
            var managersPrefab = BuildManagersPrefab();
            var levelManagerPrefab = BuildLevelManagerPrefab();
            var uiPrefab = BuildUiPrefab();

            SetupScene(cameraPrefab, managersPrefab, levelManagerPrefab, uiPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Game scene prefabs built and GameScene updated.");
        }

        static GameObject BuildCameraPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(CameraSourcePath);
            if (source == null)
            {
                Debug.LogError($"Missing camera prefab at {CameraSourcePath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "GameCamera";

            var cam = instance.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.tag = "MainCamera";
                NeonTheme.ApplyCamera(cam);
            }

            return ScenePrefabEditorUtility.SavePrefab(instance, CameraPrefabPath);
        }

        static GameObject BuildLevelManagerPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(LevelManagerSourcePath);
            if (source == null)
            {
                Debug.LogError($"Missing level manager prefab at {LevelManagerSourcePath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "LevelManager";
            WireLevelManagerLevels(instance.GetComponent<LevelManager>());
            return ScenePrefabEditorUtility.SavePrefab(instance, LevelManagerPrefabPath);
        }

        static void WireLevelManagerLevels(LevelManager levelManager)
        {
            if (levelManager == null)
                return;

            var levels = new List<Level>();
            for (int i = 1; i <= ProceduralLevelConstants.HandcraftedLevelCount; i++)
            {
                string path = $"{LevelsFolder}/Level {i}.prefab";
                var level = AssetDatabase.LoadAssetAtPath<Level>(path);
                if (level == null)
                {
                    Debug.LogWarning($"Missing level prefab at {path}");
                    continue;
                }

                levels.Add(level);
            }

            var template = AssetDatabase.LoadAssetAtPath<Level>(LevelBaseResourcePath);
            var so = new SerializedObject(levelManager);
            so.FindProperty("_levels").arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
                so.FindProperty("_levels").GetArrayElementAtIndex(i).objectReferenceValue = levels[i];

            so.FindProperty("_proceduralLevelTemplate").objectReferenceValue = template;
            so.FindProperty("_useProceduralLevels").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildManagersPrefab()
        {
            var root = new GameObject("GameManagers");

            foreach (var path in ManagerResourcePaths)
                ScenePrefabEditorUtility.SpawnManagerPrefab<Component>(root.transform, path);

            if (root.GetComponentInChildren<CurrencyWallet>(true) == null)
            {
                var walletGo = new GameObject("CurrencyWallet");
                walletGo.transform.SetParent(root.transform, false);
                walletGo.AddComponent<CurrencyWallet>();
            }

            EnsureLivesManager(root.transform);

            return ScenePrefabEditorUtility.SavePrefab(root, ManagersPrefabPath);
        }

        static void EnsureLivesManager(Transform managersRoot)
        {
            if (managersRoot.GetComponentInChildren<LivesManager>(true) != null)
                return;

            var livesGo = new GameObject("LivesManager");
            livesGo.transform.SetParent(managersRoot, false);
            livesGo.AddComponent<LivesManager>();
        }

        static GameObject BuildUiPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UiSourcePath);
            if (source == null)
            {
                Debug.LogError($"Missing UI source prefab at {UiSourcePath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "GameUI";

            var ui = instance.GetComponent<GameUIManager>();
            if (ui == null)
            {
                Debug.LogError("UI source prefab must include GameUIManager or UIRootController.");
                Object.DestroyImmediate(instance);
                return null;
            }

            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_context").enumValueIndex = (int)GameUiContext.Gameplay;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            NeonGameplayUiStyler.Apply(ui);
            NeonHudBuilder.Apply(ui.GetComponentInChildren<HUDPanel>(true), respectPrefabLayout: true);

            EnsureSettingsPanel(instance.transform, ui);

            uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_prefabBuiltUi").boolValue = true;
            uiSo.FindProperty("_hud").objectReferenceValue = ui.GetComponentInChildren<HUDPanel>(true);
            uiSo.FindProperty("_win").objectReferenceValue = ui.GetComponentInChildren<WinPanel>(true);
            uiSo.FindProperty("_fail").objectReferenceValue = ui.GetComponentInChildren<FailPanel>(true);
            uiSo.FindProperty("_retry").objectReferenceValue = ui.GetComponentInChildren<RetryPanel>(true);
            uiSo.FindProperty("_settings").objectReferenceValue = ui.GetComponentInChildren<NeonSettingsPanel>(true);
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            WireHud(ui.GetComponentInChildren<HUDPanel>(true), ui);

            var retry = ui.GetComponentInChildren<RetryPanel>(true);
            if (retry != null)
                retry.EnsureWired();

            var win = ui.GetComponentInChildren<WinPanel>(true);
            if (win != null)
            {
                win.EnsureWired();
                WireWinPanel(win);
            }

            return ScenePrefabEditorUtility.SavePrefab(instance, UiPrefabPath);
        }

        static void WireWinPanel(WinPanel win)
        {
            if (win == null)
                return;

            var winPanelNeon = win.transform.Find("WinPanelNeon");
            if (winPanelNeon != null)
            {
                if (winPanelNeon.Find("RestartButtonNeon") == null)
                {
                    var restartButton = NeonUiBuilder.CreateNeonButton(
                        winPanelNeon,
                        "LEVEL RESTART",
                        new Vector2(620f, 96f),
                        NeonTheme.UiMagentaBorder,
                        NeonTheme.UiPanel,
                        Color.white,
                        null,
                        "RestartButtonNeon");
                    restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);
                }

                var neonImage = winPanelNeon.GetComponent<Image>();
                if (neonImage != null)
                    neonImage.raycastTarget = false;
            }

            var winSo = new SerializedObject(win);
            var heartPanel = win.GetComponentInChildren<HeartPanel>(true);
            if (heartPanel != null)
                winSo.FindProperty("_heartPanel").objectReferenceValue = heartPanel;

            if (winPanelNeon != null)
            {
                var nextButton = winPanelNeon.Find("NextButtonNeon")?.GetComponent<Button>();
                if (nextButton != null)
                    winSo.FindProperty("_nextButton").objectReferenceValue = nextButton;

                var restart = winPanelNeon.Find("RestartButtonNeon")?.GetComponent<Button>();
                if (restart != null)
                    winSo.FindProperty("_restartButton").objectReferenceValue = restart;
            }

            winSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureSettingsPanel(Transform uiRoot, GameUIManager ui)
        {
            var settings = uiRoot.GetComponentInChildren<NeonSettingsPanel>(true);
            if (settings == null)
            {
                var settingsGo = new GameObject("NeonSettingsPanel", typeof(RectTransform));
                settingsGo.transform.SetParent(uiRoot, false);
                NeonUiBuilder.Stretch(settingsGo.GetComponent<RectTransform>());
                settings = settingsGo.AddComponent<NeonSettingsPanel>();
            }

            settings.Show(false);
            settings.HideImmediate();
        }

        static void WireHud(HUDPanel hud, GameUIManager ui)
        {
            if (hud == null)
                return;

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_uiRoot").objectReferenceValue = ui;
            hudSo.FindProperty("_heartPanel").objectReferenceValue = hud.GetComponentInChildren<HeartPanel>(true);
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetupScene(
            GameObject cameraPrefab,
            GameObject managersPrefab,
            GameObject levelManagerPrefab,
            GameObject uiPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearSceneRoots();

            if (Object.FindFirstObjectByType<GameSceneController>() == null)
            {
                var bootstrapGo = new GameObject("Game");
                bootstrapGo.AddComponent<GameSceneController>();
            }

            if (cameraPrefab != null)
                PrefabUtility.InstantiatePrefab(cameraPrefab, scene);

            if (managersPrefab != null)
                PrefabUtility.InstantiatePrefab(managersPrefab, scene);

            if (levelManagerPrefab != null)
                PrefabUtility.InstantiatePrefab(levelManagerPrefab, scene);

            if (uiPrefab != null)
                PrefabUtility.InstantiatePrefab(uiPrefab, scene);

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                EditorSceneManager.MoveGameObjectToScene(es, scene);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ClearSceneRoots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (IsPreservedRoot(root.name))
                    continue;

                Object.DestroyImmediate(root);
            }
        }

        static bool IsPreservedRoot(string rootName)
        {
            foreach (var preserved in PreservedSceneRoots)
            {
                if (rootName == preserved)
                    return true;
            }

            return false;
        }
    }
}
#endif
