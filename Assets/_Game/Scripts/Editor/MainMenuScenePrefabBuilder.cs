#if UNITY_EDITOR
using System.IO;
using _Game.Theme;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._Haptics;
using SerapKeremGameKit._UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI.Editor
{
    public static class MainMenuScenePrefabBuilder
    {
        const string PrefabFolder = "Assets/_Game/Prefabs/MainMenu";
        const string UiPrefabPath = PrefabFolder + "/MainMenuUI.prefab";
        const string CameraPrefabPath = PrefabFolder + "/MainMenuCamera.prefab";
        const string ManagersPrefabPath = PrefabFolder + "/MainMenuManagers.prefab";
        const string ScenePath = "Assets/_Game/Scenes/MainMenuScene.unity";

        [MenuItem("Go-Arrow/Build Main Menu Scene Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabFolder);

            var cameraPrefab = BuildCameraPrefab();
            var managersPrefab = BuildManagersPrefab();
            var uiPrefab = BuildUiPrefab();

            SetupScene(cameraPrefab, managersPrefab, uiPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Main menu prefabs built and MainMenuScene updated.");
        }

        static GameObject BuildCameraPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(CameraPrefabPath);

            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = go.GetComponent<Camera>();
            cam.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            NeonTheme.ApplyCamera(cam);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CameraPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildManagersPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefabPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(ManagersPrefabPath);

            var root = new GameObject("MainMenuManagers");

            SpawnManagerPrefab<AudioManager>(root.transform, "Managers/AudioManager");
            SpawnManagerPrefab<HapticManager>(root.transform, "Managers/HapticManager");

            if (root.GetComponentInChildren<CurrencyWallet>(true) == null)
            {
                var walletGo = new GameObject("CurrencyWallet");
                walletGo.transform.SetParent(root.transform, false);
                walletGo.AddComponent<CurrencyWallet>();
            }

            return ScenePrefabEditorUtility.SavePrefab(root, ManagersPrefabPath);
        }

        static void SpawnManagerPrefab<T>(Transform parent, string resourcePath) where T : Component
        {
            ScenePrefabEditorUtility.SpawnManagerPrefab<T>(parent, resourcePath);
        }

        static GameObject BuildUiPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(UiPrefabPath);

            var canvasGo = new GameObject(
                "MainMenuUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameUIManager));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            NeonUiLayout.ConfigureCanvas(scaler);

            var ui = canvasGo.GetComponent<GameUIManager>();
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_context").enumValueIndex = (int)GameUiContext.MainMenu;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            var menuPanelGo = new GameObject("MainMenuPanel", typeof(RectTransform));
            menuPanelGo.transform.SetParent(canvasGo.transform, false);
            NeonUiBuilder.Stretch(menuPanelGo.GetComponent<RectTransform>());

            var menuBg = menuPanelGo.AddComponent<Image>();
            menuBg.color = NeonTheme.CameraClear;
            menuBg.raycastTarget = true;

            var menuPanel = menuPanelGo.AddComponent<MainMenuPanel>();
            NeonUiBuilder.EnsureCanvasGroup(menuPanelGo);

            GoArrowBranding.CreateLogoImage(menuPanelGo.transform, new Vector2(0f, NeonUiLayout.MainMenuLogoY), new Vector2(920f, 340f));

            var play = NeonUiBuilder.CreateNeonButton(
                menuPanelGo.transform,
                "PLAY",
                new Vector2(620f, 110f),
                NeonTheme.UiCyanBorder,
                NeonTheme.UiPanel,
                Color.white,
                null,
                "PlayButton");
            play.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, NeonUiLayout.MainMenuPlayY);

            var settings = NeonUiBuilder.CreateNeonButton(
                menuPanelGo.transform,
                "SETTINGS",
                new Vector2(620f, 110f),
                NeonTheme.UiMagentaBorder,
                NeonTheme.UiPanel,
                NeonTheme.UiText,
                null,
                "SettingsButton");
            settings.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, NeonUiLayout.MainMenuSettingsY);

            var levelLabel = NeonUiBuilder.CreatePositionedText(
                menuPanelGo.transform,
                "Playing Level 1",
                34f,
                NeonTheme.UiHudText,
                new Vector2(0f, NeonUiLayout.MainMenuLevelLabelY),
                new Vector2(700f, 60f),
                TMPro.TextAlignmentOptions.Center,
                "LevelLabel");

            var menuSo = new SerializedObject(menuPanel);
            menuSo.FindProperty("_playButton").objectReferenceValue = play;
            menuSo.FindProperty("_settingsButton").objectReferenceValue = settings;
            menuSo.FindProperty("_levelText").objectReferenceValue = levelLabel;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            var settingsGo = new GameObject("NeonSettingsPanel", typeof(RectTransform));
            settingsGo.transform.SetParent(canvasGo.transform, false);
            NeonUiBuilder.Stretch(settingsGo.GetComponent<RectTransform>());
            var settingsPanel = settingsGo.AddComponent<NeonSettingsPanel>();
            settingsPanel.Show(false);
            settingsPanel.HideImmediate();

            uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_mainMenuPanel").objectReferenceValue = menuPanel;
            uiSo.FindProperty("_settings").objectReferenceValue = settingsPanel;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, UiPrefabPath);
            Object.DestroyImmediate(canvasGo);
            return prefab;
        }

        static void SetupScene(GameObject cameraPrefab, GameObject managersPrefab, GameObject uiPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ClearSceneRootsExceptEventSystem();

            var bootstrap = Object.FindFirstObjectByType<MainMenuSceneController>();
            if (bootstrap == null)
            {
                var bootstrapGo = new GameObject("MainMenu");
                bootstrapGo.AddComponent<MainMenuSceneController>();
            }

            if (cameraPrefab != null)
                PrefabUtility.InstantiatePrefab(cameraPrefab, scene);

            if (managersPrefab != null)
                PrefabUtility.InstantiatePrefab(managersPrefab, scene);

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

        static void ClearSceneRootsExceptEventSystem()
        {
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;

                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
