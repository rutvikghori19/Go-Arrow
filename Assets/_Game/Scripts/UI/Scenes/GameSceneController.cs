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

            NeonTheme.ApplyPostProcessing();

            if (Camera.main != null)
                NeonTheme.ApplyCamera(Camera.main);
        }
    }
}
