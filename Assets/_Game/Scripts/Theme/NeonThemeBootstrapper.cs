using UnityEngine;

namespace _Game.Theme
{
    public sealed class NeonThemeBootstrapper : MonoBehaviour
    {
        void Awake()
        {
            NeonTheme.ApplyPostProcessing();

            if (Camera.main != null)
                NeonTheme.ApplyCamera(Camera.main);
        }
    }
}
