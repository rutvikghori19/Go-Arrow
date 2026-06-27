using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    /// <summary>
    /// Cold start only: SplashScene logo + bar. Does not use LoadingOverlay prefab.
    /// </summary>
    public sealed class SplashScreenController : MonoBehaviour
    {
        [SerializeField] Image _fillImg;
        [SerializeField] float _extraFillDuration = 2f;

        IEnumerator Start()
        {
            yield return null;

            PrepareFillBar();
            SetProgress(0f);

            yield return SceneAsyncLoadProgress.Run(
                GameSceneNames.MainMenu,
                _extraFillDuration,
                SetProgress,
                GameSessionBootstrap.ResetForMenu);
        }

        void PrepareFillBar()
        {
            if (_fillImg == null)
                return;

            if (_fillImg.type != Image.Type.Filled)
            {
                _fillImg.type = Image.Type.Filled;
                _fillImg.fillMethod = Image.FillMethod.Horizontal;
                _fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            _fillImg.fillAmount = 0f;
        }

        void SetProgress(float normalized)
        {
            if (_fillImg == null)
                return;

            _fillImg.fillAmount = Mathf.Clamp01(normalized);
        }
    }
}
