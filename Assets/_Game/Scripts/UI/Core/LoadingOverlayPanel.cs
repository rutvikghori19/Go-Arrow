using System.Collections;
using TMPro;
using UnityEngine;

namespace _Game.UI
{
    public sealed class LoadingOverlayPanel : MonoBehaviour
    {
        const string LoadingBaseText = "LOADING";

        [SerializeField] RectTransform _fillBar;
        [SerializeField] TextMeshProUGUI _loadingText;
        [SerializeField] float _maxFillWidth = 900f;
        [SerializeField] float _dotInterval = 0.4f;

        Canvas _canvas;
        Coroutine _dotRoutine;
        int _dotCount;

        void Awake()
        {
            ResolveReferences();
        }

        void ResolveReferences()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = GetComponentInChildren<Canvas>(true);

            if (_fillBar == null)
            {
                var fill = transform.Find("LoadingOverlay/Loader/fillbar");
                if (fill == null)
                    fill = FindChildByName(transform, "fillbar");

                if (fill != null)
                    _fillBar = fill as RectTransform;
            }

            if (_loadingText == null)
            {
                foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text.name.IndexOf("Loading", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _loadingText = text;
                        break;
                    }
                }
            }
        }

        public void Show(int sortOrder = 5000)
        {
            ResolveReferences();
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);

            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = sortOrder;
            }

            SetProgress(0f);
            StartDotAnimation();
        }

        public void SetProgress(float normalized)
        {
            if (_fillBar == null)
                return;

            var size = _fillBar.sizeDelta;
            size.x = Mathf.Lerp(0f, _maxFillWidth, Mathf.Clamp01(normalized));
            _fillBar.sizeDelta = size;
        }

        public void DestroyOverlay()
        {
            StopDotAnimation();
            Destroy(gameObject);
        }

        void StartDotAnimation()
        {
            StopDotAnimation();
            _dotCount = 0;
            UpdateLoadingText();
            _dotRoutine = StartCoroutine(AnimateDots());
        }

        void StopDotAnimation()
        {
            if (_dotRoutine == null)
                return;

            StopCoroutine(_dotRoutine);
            _dotRoutine = null;
        }

        IEnumerator AnimateDots()
        {
            var wait = new WaitForSecondsRealtime(_dotInterval);

            while (true)
            {
                yield return wait;
                _dotCount = (_dotCount + 1) % 4;
                UpdateLoadingText();
            }
        }

        void UpdateLoadingText()
        {
            if (_loadingText == null)
                return;

            _loadingText.text = LoadingBaseText + new string('.', _dotCount);
        }

        static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }
    }
}
