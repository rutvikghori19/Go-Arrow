#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.UI.Editor
{
    public static class NeonUiSceneSetup
    {
        const string SplashPath = "Assets/_Game/Scenes/SplashScene.unity";
        const string MainMenuPath = "Assets/_Game/Scenes/MainMenuScene.unity";
        const string GamePath = "Assets/_Game/Scenes/GameScene.unity";

        [MenuItem("Go-Arrow/Setup Neon UI Scenes")]
        public static void SetupScenes()
        {
            CreateSplashScene();
            CreateMainMenuScene();
            EnsureGameSceneEntry();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Neon UI scenes ready: SplashScene -> MainMenuScene -> GameScene");
        }

        static void CreateSplashScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Splash");
            root.AddComponent<SplashScreenController>();
            EditorSceneManager.SaveScene(scene, SplashPath);
        }

        static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MainMenu");
            root.AddComponent<MainMenuSceneController>();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        static void EnsureGameSceneEntry()
        {
            if (!File.Exists(GamePath))
                return;

            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<GameSceneEntry>() == null)
            {
                var go = new GameObject("GameSceneEntry");
                go.AddComponent<GameSceneEntry>();
            }

            EditorSceneManager.SaveScene(scene);
        }

        static void UpdateBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(SplashPath, true),
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GamePath, true),
            };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
#endif
