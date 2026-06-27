using SerapKeremGameKit._Logging;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Game.LevelCamera
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class LevelCameraNavigator : MonoBehaviour
    {
        public static LevelCameraNavigator Instance { get; private set; }

        [SerializeField] private float _zoomFactor = 1.3f;
        [SerializeField] private float _panDragThresholdPixels = 14f;
        [SerializeField] private float _scrollZoomSpeed = 0.35f;
        [SerializeField] private float _pinchZoomSpeed = 0.004f;
        [SerializeField] private float _boundsPadding = 0.5f;

        UnityEngine.Camera _camera;
        Transform _cameraTransform;

        float _baseOrthoSize;
        Vector3 _basePosition;
        Bounds _levelBounds;
        bool _hasFitState;

        bool _trackingPointer;
        bool _isPanning;
        bool _isPinching;
        bool _isScrolling;
        bool _gestureWasPanOrZoom;
        Vector2 _pointerStartScreen;
        Vector3 _cameraPosAtPanStart;
        Vector3 _panWorldAnchor;
        float _lastPinchDistance;
        int _activePointerId = -1;

        public bool BlocksArrowSelection => _isPanning || _isPinching || _isScrolling;
        public bool RequiresDeferredTap => _hasFitState && IsZoomedIn;
        public bool CanRegisterTap => !BlocksArrowSelection && !_gestureWasPanOrZoom;

        public bool IsZoomedIn =>
            _hasFitState && _camera != null && _camera.orthographicSize < _baseOrthoSize - 0.01f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                TraceLogger.LogWarning("Duplicate LevelCameraNavigator detected. Destroying extra instance.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            ResolveCamera();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindCamera(UnityEngine.Camera camera, Transform cameraTransform)
        {
            _camera = camera;
            _cameraTransform = cameraTransform;
        }

        public void CaptureFitState(Vector3 position, float orthoSize, Bounds levelBounds)
        {
            ResolveCamera();
            if (_camera == null)
                return;

            _basePosition = position;
            _baseOrthoSize = orthoSize;
            _levelBounds = levelBounds;
            _hasFitState = true;

            ApplyCamera(position, orthoSize);
            ResetGestureState();
        }

        public bool ConsumeGameplayPointerInput()
        {
            if (!_hasFitState)
                return false;

            if (_isPanning || _isPinching || _isScrolling)
                return true;

            return Input.touchCount >= 2;
        }

        void Update()
        {
            if (!_hasFitState || _camera == null)
                return;

            _isScrolling = false;

            // Scroll wheel works in Editor and Device Simulator even when touch is simulated.
            HandleMouseScroll();

            if (Input.touchCount >= 2)
            {
                HandlePinch();
                return;
            }

            if (_isPinching)
                _isPinching = false;

            if (Input.touchCount == 1)
            {
                HandleTouchPan(Input.GetTouch(0));
                return;
            }

            HandleMouseDrag();
        }

        void LateUpdate()
        {
            _isScrolling = false;
        }

        void HandlePinch()
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            if (IsPointerOverUi(t0.fingerId) || IsPointerOverUi(t1.fingerId))
                return;
            float distance = Vector2.Distance(t0.position, t1.position);

            if (!_isPinching)
            {
                _lastPinchDistance = distance;
                _isPinching = true;
                _gestureWasPanOrZoom = true;
                return;
            }

            float delta = distance - _lastPinchDistance;
            _lastPinchDistance = distance;
            ApplyZoom(_camera.orthographicSize - delta * _pinchZoomSpeed);
            _gestureWasPanOrZoom = true;
        }

        void HandleTouchPan(Touch touch)
        {
            if (IsPointerOverUi(touch.fingerId))
            {
                EndPointerGesture();
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginPointerGesture(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    UpdatePointerGesture(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndPointerGesture();
                    break;
            }
        }

        void HandleMouseScroll()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                scroll = Input.GetAxis("Mouse ScrollWheel") * 3f;

            if (Mathf.Abs(scroll) < 0.01f)
                return;

            if (IsPointerOverUi())
                return;

            _isScrolling = true;
            _gestureWasPanOrZoom = true;
            ApplyZoom(_camera.orthographicSize - scroll * _scrollZoomSpeed);
        }

        void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUi())
                    BeginPointerGesture(Input.mousePosition, -1);
            }
            else if (Input.GetMouseButton(0) && _trackingPointer)
            {
                UpdatePointerGesture(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndPointerGesture();
            }
        }

        void BeginPointerGesture(Vector2 screenPosition, int pointerId)
        {
            _trackingPointer = true;
            _activePointerId = pointerId;
            _pointerStartScreen = screenPosition;
            _cameraPosAtPanStart = _cameraTransform.position;
            _panWorldAnchor = ScreenToWorld(screenPosition);
            _gestureWasPanOrZoom = false;
            _isPanning = false;
        }

        void UpdatePointerGesture(Vector2 screenPosition)
        {
            if (!_trackingPointer || !IsZoomedIn)
                return;

            float dragDistance = Vector2.Distance(_pointerStartScreen, screenPosition);
            if (dragDistance < _panDragThresholdPixels && !_isPanning)
                return;

            _isPanning = true;
            _gestureWasPanOrZoom = true;

            Vector3 worldNow = ScreenToWorld(screenPosition);
            Vector3 delta = _panWorldAnchor - worldNow;
            delta.z = 0f;
            _cameraTransform.position = _cameraPosAtPanStart + delta;
            ClampCameraPosition();
        }

        void EndPointerGesture()
        {
            _trackingPointer = false;
            _activePointerId = -1;
            _isPanning = false;
        }

        void ApplyZoom(float targetOrthoSize)
        {
            float minSize = _baseOrthoSize / _zoomFactor;
            float maxSize = _baseOrthoSize * _zoomFactor;
            float clamped = Mathf.Clamp(targetOrthoSize, minSize, maxSize);
            ApplyCamera(_cameraTransform.position, clamped);
            ClampCameraPosition();
        }

        void ApplyCamera(Vector3 position, float orthoSize)
        {
            _cameraTransform.position = position;
            _camera.orthographicSize = orthoSize;
        }

        void ClampCameraPosition()
        {
            float vertExtent = _camera.orthographicSize;
            float horzExtent = vertExtent * _camera.aspect;

            float halfWidth = _levelBounds.extents.x + _boundsPadding;
            float halfHeight = _levelBounds.extents.y + _boundsPadding;

            float panLimitX = Mathf.Max(0f, halfWidth - horzExtent);
            float panLimitY = Mathf.Max(0f, halfHeight - vertExtent);

            Vector3 pos = _cameraTransform.position;
            pos.x = Mathf.Clamp(pos.x, _basePosition.x - panLimitX, _basePosition.x + panLimitX);
            pos.y = Mathf.Clamp(pos.y, _basePosition.y - panLimitY, _basePosition.y + panLimitY);
            pos.z = _basePosition.z;
            _cameraTransform.position = pos;
        }

        Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 world = _camera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            return world;
        }

        void ResetGestureState()
        {
            _trackingPointer = false;
            _isPanning = false;
            _isPinching = false;
            _isScrolling = false;
            _gestureWasPanOrZoom = false;
            _activePointerId = -1;
        }

        void ResolveCamera()
        {
            if (_camera != null && _cameraTransform != null)
                return;

            _camera = GetComponentInChildren<UnityEngine.Camera>(true);
            if (_camera != null)
                _cameraTransform = _camera.transform;
        }

        static bool IsPointerOverUi(int pointerId = -1)
        {
            if (EventSystem.current == null)
                return false;

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
