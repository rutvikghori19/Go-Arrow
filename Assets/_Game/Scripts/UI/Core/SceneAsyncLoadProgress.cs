using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.UI
{
    /// <summary>
    /// Shared async scene load timing: real load progress spread over load time + extra hold, then activate.
    /// </summary>
    public static class SceneAsyncLoadProgress
    {
        public const float ActivationProgress = 0.9f;

        public static IEnumerator Run(
            string sceneName,
            float extraFillDuration,
            Action<float> onProgress,
            Action prepare = null)
        {
            prepare?.Invoke();

            var loadOp = SceneManager.LoadSceneAsync(sceneName);
            if (loadOp == null)
            {
                onProgress?.Invoke(1f);
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            loadOp.allowSceneActivation = false;

            float elapsed = 0f;

            while (loadOp.progress < ActivationProgress)
            {
                elapsed += Time.unscaledDeltaTime;
                onProgress?.Invoke(elapsed / (elapsed + extraFillDuration));
                yield return null;
            }

            float totalDuration = elapsed + extraFillDuration;

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                onProgress?.Invoke(elapsed / totalDuration);
                yield return null;
            }

            onProgress?.Invoke(1f);
            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
                yield return null;
        }
    }
}
