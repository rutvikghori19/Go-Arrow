using _Game.Theme;
using UnityEngine;

namespace _Game.UI
{
    /// <summary>
    /// Game scene bootstrap. Scene content comes from prefabs, not this script.
    /// </summary>
    public sealed class GameSceneController : MonoBehaviour
    {
        void Awake()
        {
            if (!GameSessionBootstrap.ShouldStartLevelOnLoad)
                GameSessionBootstrap.PrepareEditorPlayInGameScene();

            EnsureLivesManager();
            NeonTheme.ApplyPostProcessing();

            if (Camera.main != null)
                NeonTheme.ApplyCamera(Camera.main);
        }

        static void EnsureLivesManager()
        {
            if (LivesManager.IsInitialized)
                return;

            Transform parent = null;
            var managers = GameObject.Find("GameManagers");
            if (managers != null)
                parent = managers.transform;

            var livesGo = new GameObject("LivesManager");
            if (parent != null)
                livesGo.transform.SetParent(parent, false);

            livesGo.AddComponent<LivesManager>();
        }
    }
}
