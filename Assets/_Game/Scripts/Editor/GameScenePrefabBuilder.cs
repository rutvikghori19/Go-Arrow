#if UNITY_EDITOR
using System.IO;
using _Game.Theme;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            return ScenePrefabEditorUtility.SavePrefab(instance, LevelManagerPrefabPath);
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

            return ScenePrefabEditorUtility.SavePrefab(root, ManagersPrefabPath);
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
            NeonHudBuilder.Apply(ui.GetComponentInChildren<HUDPanel>(true));

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

            return ScenePrefabEditorUtility.SavePrefab(instance, UiPrefabPath);
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
