using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.UI
{
    /// <summary>
    /// SplashScene -> MainMenu: handled by <see cref="SplashScreenController"/> on cold start.
    /// All other transitions use <see cref="SceneLoadingController"/> (LoadingOverlay prefab).
    /// </summary>
    public static class SceneFlow
    {
        public static void LoadSplash()
        {
            SceneManager.LoadScene(GameSceneNames.Splash);
        }

        public static void LoadMainMenu()
        {
            SceneLoadingController.Load(GameSceneNames.MainMenu, GameSessionBootstrap.ResetForMenu);
        }

        public static void LoadGame()
        {
            SceneLoadingController.Load(GameSceneNames.Game);
        }
    }
}
