using UnityEngine;

namespace _Game.UI
{
    /// <summary>
    /// Ensures direct play-in-editor from GameScene still starts a level.
    /// </summary>
    public sealed class GameSceneEntry : MonoBehaviour
    {
        void Awake()
        {
            if (!GameSessionBootstrap.ShouldStartLevelOnLoad)
                GameSessionBootstrap.PrepareEditorPlayInGameScene();
        }
    }
}
