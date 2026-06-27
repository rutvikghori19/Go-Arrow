using _Game.LevelCamera;
using _Game.UI;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Singletons;
using UnityEngine;

namespace SerapKeremGameKit._Camera
{
    public sealed class CameraManager : MonoSingleton<CameraManager>
    {
        [SerializeField] private Transform _gameCamera;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Vector3 _followOffset;
        [SerializeField] private float _followLerp = 10f;
        [SerializeField] private bool _snapOnStart = true;

        [Header("Camera Fitting Settings")]
        [SerializeField] private float _padding = 1.5f;
        [SerializeField, Range(0f, 0.25f)] private float _paddingPercent = 0.08f;
        [SerializeField] private float _minOrthographicSize = 3f;
        [SerializeField] private float _maxOrthographicSize = 200f;
        [SerializeField] private bool _accountForUiInsets = true;
        [SerializeField] private bool _forceOrthographicOnFit = true;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private LevelCameraNavigator _navigator;

        public void SnapFollow()
        {
            if (_gameCamera == null || _followTarget == null) return;
            _gameCamera.position = _followTarget.position + _followOffset;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            if (_gameCamera == null) { TraceLogger.LogError("Camera is null!", this); return; }

            _initialPosition = _gameCamera.position;
            _initialRotation = _gameCamera.rotation;

            if (_snapOnStart && _followTarget != null)
                SnapFollow();

            EnsureNavigator();
        }

        void EnsureNavigator()
        {
            _navigator = GetComponent<LevelCameraNavigator>();
            if (_navigator == null)
                _navigator = gameObject.AddComponent<LevelCameraNavigator>();

            UnityEngine.Camera cam = _gameCamera != null ? _gameCamera.GetComponent<UnityEngine.Camera>() : GetComponentInChildren<UnityEngine.Camera>(true);
            if (cam != null)
                _navigator.BindCamera(cam, cam.transform);
        }

        public void InitializeCameraPosition(Transform point)
        {
            if (_gameCamera == null || point == null) return;
            _gameCamera.position = point.position;
            _gameCamera.rotation = point.rotation;
        }

        public void SetFollowTarget(Transform target, Vector3 offset)
        {
            _followTarget = target;
            _followOffset = offset;
        }

        [System.Obsolete("Continuous follow is deprecated. Use SnapFollow/InitializeCameraPosition for one-shot.")]
        public void StepFollow(float deltaTime)
        {
            if (_gameCamera == null || _followTarget == null) return;
            Vector3 targetPos = _followTarget.position + _followOffset;
            _gameCamera.position = Vector3.Lerp(_gameCamera.position, targetPos, deltaTime * _followLerp);
        }

        public void ResetCamera()
        {
            if (_gameCamera == null) return;
            _gameCamera.position = _initialPosition;
            _gameCamera.rotation = _initialRotation;
        }

        public void FitCameraToLevel(Transform levelRoot)
        {
            if (levelRoot == null)
            {
                TraceLogger.LogWarning("Level root is null. Cannot fit camera.", this);
                return;
            }

            Transform contentRoot = levelRoot.Find("LINES");
            if (contentRoot == null)
                contentRoot = levelRoot;

            FitCameraToContent(contentRoot);
        }

        public void FitCameraToLines(Transform linesParent)
        {
            FitCameraToContent(linesParent);
        }

        void FitCameraToContent(Transform contentRoot)
        {
            if (_gameCamera == null)
            {
                TraceLogger.LogError("Game camera is null. Cannot fit camera to level.", this);
                return;
            }

            UnityEngine.Camera cam = _gameCamera.GetComponent<UnityEngine.Camera>();
            if (cam == null)
            {
                TraceLogger.LogError("Camera component not found on game camera transform.", this);
                return;
            }

            if (contentRoot == null)
            {
                TraceLogger.LogWarning("Level content root is null. Cannot fit camera.", this);
                return;
            }

            if (_forceOrthographicOnFit && !cam.orthographic)
                cam.orthographic = true;

            Bounds bounds = CalculateContentBounds(contentRoot);
            if (bounds.size.sqrMagnitude < 0.001f)
            {
                TraceLogger.LogWarning("Level bounds are invalid. Cannot fit camera.", this);
                return;
            }

            float paddedWidth = bounds.size.x + GetAxisPadding(bounds.size.x);
            float paddedHeight = bounds.size.y + GetAxisPadding(bounds.size.y);

            float usableHeightFraction = _accountForUiInsets ? GetUsableHeightFraction() : 1f;
            float sizeByWidth = paddedWidth / (2f * Mathf.Max(0.01f, cam.aspect));
            float sizeByHeight = (paddedHeight * 0.5f) / Mathf.Max(0.35f, usableHeightFraction);
            float orthographicSize = Mathf.Max(sizeByWidth, sizeByHeight);
            orthographicSize = Mathf.Clamp(orthographicSize, _minOrthographicSize, _maxOrthographicSize);

            Vector3 center = bounds.center;
            center.z = _gameCamera.position.z;

            if (_accountForUiInsets)
                center.y += GetPlayAreaCenterYOffset(orthographicSize);

            _gameCamera.position = center;

            if (cam.orthographic)
                cam.orthographicSize = orthographicSize;

            EnsureNavigator();
            if (_navigator != null)
                _navigator.CaptureFitState(center, cam.orthographicSize, bounds);
        }

        float GetAxisPadding(float axisSize)
        {
            return (_padding * 2f) + (axisSize * _paddingPercent * 2f);
        }

        static Bounds CalculateContentBounds(Transform contentRoot)
        {
            bool hasBounds = false;
            Bounds bounds = default;

            Renderer[] renderers = contentRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                Bounds rendererBounds = renderer.bounds;
                if (rendererBounds.size.sqrMagnitude < 1e-8f)
                    continue;

                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            if (hasBounds)
                return bounds;

            Collider2D[] colliders = contentRoot.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBounds)
                return bounds;

            return CalculateLinePointBounds(contentRoot);
        }

        static Bounds CalculateLinePointBounds(Transform contentRoot)
        {
            LineRenderer[] lineRenderers = contentRoot.GetComponentsInChildren<LineRenderer>(true);
            if (lineRenderers == null || lineRenderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            bool hasValidPoint = false;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                if (lineRenderer == null || lineRenderer.positionCount < 2)
                    continue;

                float halfWidth = GetLineHalfWidth(lineRenderer);

                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    Vector3 worldPos = GetLineWorldPosition(lineRenderer, i);

                    minX = Mathf.Min(minX, worldPos.x - halfWidth);
                    maxX = Mathf.Max(maxX, worldPos.x + halfWidth);
                    minY = Mathf.Min(minY, worldPos.y - halfWidth);
                    maxY = Mathf.Max(maxY, worldPos.y + halfWidth);
                    hasValidPoint = true;
                }
            }

            if (!hasValidPoint)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            return new Bounds(center, size);
        }

        static Vector3 GetLineWorldPosition(LineRenderer lineRenderer, int index)
        {
            Vector3 localPos = lineRenderer.GetPosition(index);
            return lineRenderer.useWorldSpace
                ? localPos
                : lineRenderer.transform.TransformPoint(localPos);
        }

        static float GetLineHalfWidth(LineRenderer lineRenderer)
        {
            float width = lineRenderer.widthMultiplier;
            if (lineRenderer.widthCurve != null && lineRenderer.widthCurve.length > 0)
                width *= lineRenderer.widthCurve.Evaluate(0.5f);

            return Mathf.Max(0.05f, width * 0.5f);
        }

        static void GetUiInsets(out float topInset, out float bottomInset)
        {
            float scale = NeonUiLayout.ReferenceHeight / Mathf.Max(1f, Screen.height);
            Rect safe = Screen.safeArea;

            bottomInset = NeonUiLayout.BannerAdHeight + Mathf.Max(0f, safe.yMin * scale);
            topInset = NeonUiLayout.TopInset + NeonUiLayout.HudHeight +
                       Mathf.Max(0f, (Screen.height - safe.yMax) * scale);
        }

        static float GetUsableHeightFraction()
        {
            GetUiInsets(out float topInset, out float bottomInset);
            float usableHeight = NeonUiLayout.ReferenceHeight - topInset - bottomInset;
            return Mathf.Clamp(usableHeight / NeonUiLayout.ReferenceHeight, 0.35f, 1f);
        }

        static float GetPlayAreaCenterYOffset(float orthographicSize)
        {
            GetUiInsets(out float topInset, out float bottomInset);
            float playTop = NeonUiLayout.ReferenceHeight - topInset;
            float playBottom = bottomInset;
            float playCenterNormalized = ((playTop + playBottom) * 0.5f) / NeonUiLayout.ReferenceHeight;
            float screenCenterNormalized = 0.5f;
            return (playCenterNormalized - screenCenterNormalized) * (orthographicSize * 2f);
        }
    }
}
