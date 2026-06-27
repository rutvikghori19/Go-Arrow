using _Game.Theme;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._Haptics;
using UnityEngine;

namespace _Game.UI
{
    public sealed class MainMenuSceneController : MonoBehaviour
    {
        [SerializeField] float _cameraDepth = -10f;

        void Awake()
        {
            NeonTheme.ApplyPostProcessing();
            NeonUiBuilder.EnsureEventSystem();
            EnsureCamera();
            EnsureManagers();
            EnsureUi();
        }

        void EnsureCamera()
        {
            if (Camera.main != null)
            {
                NeonTheme.ApplyCamera(Camera.main);
                return;
            }

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, _cameraDepth);
            NeonTheme.ApplyCamera(cam);
        }

        void EnsureManagers()
        {
            TrySpawnPrefab<AudioManager>("Managers/AudioManager");
            TrySpawnPrefab<HapticManager>("Managers/HapticManager");

            if (!CurrencyWallet.IsInitialized && FindFirstObjectByType<CurrencyWallet>() == null)
            {
                var go = new GameObject("CurrencyWallet");
                go.AddComponent<CurrencyWallet>();
            }
        }

        static void TrySpawnPrefab<T>(string resourcePath) where T : Component
        {
            if (FindFirstObjectByType<T>() != null)
                return;

            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
                Object.Instantiate(prefab);
        }

        void EnsureUi()
        {
            if (FindFirstObjectByType<GameUIManager>() != null)
                return;

            var canvas = NeonUiBuilder.CreateRootCanvas("UI");
            var ui = canvas.gameObject.AddComponent<GameUIManager>();
            ui.InitializeAsMainMenu();
        }
    }
}
