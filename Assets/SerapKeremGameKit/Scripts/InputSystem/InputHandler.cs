using _Game.LevelCamera;
using SerapKeremGameKit._InputSystem.Data;
using SerapKeremGameKit._Singletons;
using UnityEngine;

namespace SerapKeremGameKit._InputSystem
{
    public class InputHandler : MonoSingleton<InputHandler>
    {
        [Header("Input Settings")]
        [SerializeField, Tooltip("Scriptable object for managing player input.")]
        private PlayerInputSO _playerInput;

        private bool _isInputLocked = false; // Indicates whether input is currently locked

        public bool IsInputLocked { get => _isInputLocked; }

        protected override void Awake()
        {
            base.Awake();

            //if (LoadingPanelController.Instance)
            //{
            //    LockInput();
            //    LoadingPanelController.Instance.OnLoadingFinished += UnlockInput;
            //}
        }

        private void Update()
        {
            if (_isInputLocked) return;

            if (LevelCameraNavigator.Instance != null &&
                LevelCameraNavigator.Instance.ConsumeGameplayPointerInput())
            {
                _playerInput.ResetFrame();
                return;
            }

            _playerInput.ResetFrame();
            ProcessMouseInput();
        }

        private void ProcessMouseInput()
        {
            Vector3 mousePosition = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown(mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                HandleMouseHeld(mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleMouseUp(mousePosition);
            }
        }

        private void HandleMouseDown(Vector3 position)
        {
            _playerInput.SetMouseDown(position);
        }

        private void HandleMouseHeld(Vector3 position)
        {
            _playerInput.SetMouseHeld(position);
        }

        private void HandleMouseUp(Vector3 position)
        {
            _playerInput.SetMouseUp(position);
        }

        public void UnlockInput()
        {
            _isInputLocked = false;
        }

        public void LockInput()
        {
            _isInputLocked = true;
        }
    }
}