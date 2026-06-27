using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.UI
{
    /// <summary>
    /// In-game scene changes only (MainMenu &lt;-&gt; Game). Shows LoadingOverlay prefab.
    /// SplashScene uses <see cref="SplashScreenController"/> instead.
    /// </summary>
    public static class SceneLoadingController
    {
        const string OverlayPrefabResourcePath = "Prefabs/LoadingOverlay";

        public static float DefaultExtraFillDuration { get; set; } = 1f;
        public static bool IsLoading { get; private set; }

        public static void Load(string sceneName, Action prepare = null, float? extraFillDuration = null)
        {
            if (IsLoading)
                return;

            var runnerGo = new GameObject(nameof(SceneLoadingController));
            var runner = runnerGo.AddComponent<SceneLoadingRunner>();
            runner.Begin(sceneName, prepare, extraFillDuration ?? DefaultExtraFillDuration);
        }

        sealed class SceneLoadingRunner : MonoBehaviour
        {
            LoadingOverlayPanel _panel;

            public void Begin(string sceneName, Action prepare, float extraFillDuration)
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(LoadRoutine(sceneName, prepare, extraFillDuration));
            }

            IEnumerator LoadRoutine(string sceneName, Action prepare, float extraFillDuration)
            {
                IsLoading = true;

                _panel = CreateOverlay();
                if (_panel == null)
                {
                    IsLoading = false;
                    yield return SceneAsyncLoadProgress.Run(sceneName, extraFillDuration, null, prepare);
                    Destroy(gameObject);
                    yield break;
                }

                _panel.Show();
                _panel.SetProgress(0f);

                yield return SceneAsyncLoadProgress.Run(
                    sceneName,
                    extraFillDuration,
                    _panel.SetProgress,
                    prepare);

                IsLoading = false;
                _panel.DestroyOverlay();
                Destroy(gameObject);
            }

            static LoadingOverlayPanel CreateOverlay()
            {
                var prefabAsset = Resources.Load(OverlayPrefabResourcePath);
                if (prefabAsset is not GameObject prefab)
                {
                    Debug.LogError(
                        $"Loading overlay prefab not found at Resources/{OverlayPrefabResourcePath}.");
                    return null;
                }

                var instance = Instantiate(prefab);
                instance.name = prefab.name;
                DontDestroyOnLoad(instance);

                var panel = instance.GetComponent<LoadingOverlayPanel>();
                if (panel == null)
                    panel = instance.AddComponent<LoadingOverlayPanel>();

                return panel;
            }
        }
    }
}
