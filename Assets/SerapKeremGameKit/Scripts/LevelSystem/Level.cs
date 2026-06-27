using System.Collections;
using _Game.Line;
using _Game.Theme;
using _Game.UI;
using Array2DEditor;
using SerapKeremGameKit._Camera;
using SerapKeremGameKit._InputSystem;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Managers;
using TriInspector;
using UnityEngine;

namespace SerapKeremGameKit._LevelSystem
{
    public class Level : MonoBehaviour
    {

        [Title("Grid Settings"), PropertyOrder(2)]
        [SerializeField] private Array2DInt _tileSizeArray;

        [Title("Time Settings")]
        [SerializeField, Min(0f)] private float _levelTime = 120f;
        public float LevelTime => _levelTime;

        [ReadOnly]
        [SerializeField] private bool _isLevelWon;

        [Title("Money Settings")]
        [SerializeField] private long _money = 10;
        public long Money => _money;


        private Coroutine _winCoroutine;
        private Coroutine _loseCoroutine;
        private Coroutine _cameraFitCoroutine;
        private Coroutine _lifeLossCoroutine;

        [SerializeField] private LineManager _lineManager;
        public LineManager LineManager { get => _lineManager; set => _lineManager = value; }

        [SerializeField] private Transform _linesParent;
        public virtual void Load()
        {
            gameObject.SetActive(true);
            _isLevelWon = false;
            if (_winCoroutine != null) { StopCoroutine(_winCoroutine); _winCoroutine = null; }
            if (_loseCoroutine != null) { StopCoroutine(_loseCoroutine); _loseCoroutine = null; }
            
            UnsubscribeFromEvents();
            Initialize();
        }

        private void Initialize()
        {
            NeonTheme.ApplyLevelBackground(transform);
            InitializeLines();
            InitializeCamera();
        }

        private void InitializeLines()
        {
            if (_lineManager != null)
            {
                _lineManager.InitializeLines(transform);
            }
            else
            {
                TraceLogger.LogWarning("LineManager is not initialized. Lines will not be initialized.", this);
            }
        }

        private void InitializeCamera()
        {
            if (CameraManager.Instance == null)
            {
                TraceLogger.LogError("CameraManager.Instance is null! Cannot initialize camera position.", this);
                return;
            }

            FitCameraToLevel();

            var cam = CameraManager.Instance.GetComponentInChildren<UnityEngine.Camera>(true);
            if (cam != null)
                NeonTheme.ApplyCamera(cam);
            NeonTheme.ApplyPostProcessing();

            if (_cameraFitCoroutine != null)
                StopCoroutine(_cameraFitCoroutine);
            _cameraFitCoroutine = StartCoroutine(RefitCameraAfterLayout());
        }

        private void FitCameraToLevel()
        {
            CameraManager.Instance.FitCameraToLevel(transform);
        }

        private IEnumerator RefitCameraAfterLayout()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            FitCameraToLevel();
            _cameraFitCoroutine = null;
        }

        public virtual void Play()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UnlockInput();
            }

            _isLevelWon = false;
            
            if (_winCoroutine != null) 
            { 
                StopCoroutine(_winCoroutine); 
                _winCoroutine = null; 
            }
            
            if (_loseCoroutine != null) 
            { 
                StopCoroutine(_loseCoroutine); 
                _loseCoroutine = null; 
            }

            UnsubscribeFromEvents();

            if (LivesManager.IsInitialized)
                LivesManager.Instance.BeginLevel();

            SubscribeToLivesManager();

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved += HandleAllLinesRemoved;
            }

            InitializeHUD();
            StartLifeLossWhenReady();
        }

        void StartLifeLossWhenReady()
        {
            if (_lifeLossCoroutine != null)
                StopCoroutine(_lifeLossCoroutine);

            _lifeLossCoroutine = StartCoroutine(EnableLifeLossWhenReady());
        }

        IEnumerator EnableLifeLossWhenReady()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            if (LivesManager.IsInitialized)
                LivesManager.Instance.EnableLifeLoss();

            RefreshLivesHud();
            _lifeLossCoroutine = null;
        }

        void RefreshLivesHud()
        {
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.RefreshLivesDisplay();
        }

        private void InitializeHUD()
        {
            GameUIManager uiRoot = FindFirstObjectByType<GameUIManager>();
            if (uiRoot != null)
            {
                uiRoot.InitializeHUD();
            }
        }

        private void SubscribeToLivesManager()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.OnLivesDepleted -= HandleLivesDepleted;
                LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;

                if (LivesManager.Instance.CurrentLives <= 0)
                    HandleLivesDepleted();
            }
            else
            {
                StartCoroutine(SubscribeToLivesManagerCoroutine());
            }
        }

        private IEnumerator SubscribeToLivesManagerCoroutine()
        {
            int maxAttempts = 10;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                if (LivesManager.IsInitialized && LivesManager.Instance != null)
                {
                    LivesManager.Instance.OnLivesDepleted -= HandleLivesDepleted;
                    LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;

                    if (LivesManager.Instance.CurrentLives <= 0)
                        HandleLivesDepleted();

                    yield break;
                }
                
                yield return null;
                attempts++;
            }

            TraceLogger.LogWarning("LivesManager is not initialized after multiple attempts. Fail condition may not work.", this);
        }


        private void HandleLivesDepleted()
        {
            if (_loseCoroutine != null) return;
            CheckLoseCondition();
        }

        private void HandleAllLinesRemoved()
        {
            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            if (_isLevelWon) return;

            _isLevelWon = true;
            _winCoroutine = StartCoroutine(WinCoroutine());
        }

        private IEnumerator WinCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);
            LevelManager.Instance.Win();
        }

        public void CheckLoseCondition()
        {
            if (_loseCoroutine != null) return;

            _loseCoroutine = StartCoroutine(LoseCoroutine());
        }

        private IEnumerator LoseCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);

            LevelManager.Instance.Lose();
        }

        private void UnsubscribeFromEvents()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.OnLivesDepleted -= HandleLivesDepleted;
            }

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved -= HandleAllLinesRemoved;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}