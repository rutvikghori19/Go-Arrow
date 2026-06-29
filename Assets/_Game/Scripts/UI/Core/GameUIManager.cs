using _Game.Theme;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._Haptics;
using SerapKeremGameKit._InputSystem;
using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Levels;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._Time;
using SerapKeremGameKit._UI;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public enum GameUiContext
    {
        Gameplay,
        MainMenu
    }

    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("Context")]
        [SerializeField] GameUiContext _context = GameUiContext.Gameplay;

        [Header("Main Menu References")]
        [SerializeField] MainMenuPanel _mainMenuPanel;

        [Header("Gameplay References")]
        [SerializeField] HudPanel _hud;
        [SerializeField] WinPanel _win;
        [SerializeField] FailPanel _fail;
        [SerializeField] SettingPanel _settings;
        [SerializeField] RetryPanel _retry;

        [Header("Data")]
        [SerializeField] LevelConfig _fallbackConfig;

        [Header("Audio Keys")]
        [SerializeField] string _keyOnWin = "game_win";
        [SerializeField] string _keyOnLose = "game_lose";
        [SerializeField] string _keyOnOpenSettings = "ui_open";
        [SerializeField] string _keyOnRestartDialog = "ui_open";
        [SerializeField] string _keyOnRestartConfirm = "ui_confirm";
        [SerializeField] string _keyOnNext = "ui_next";

        GameState _lastState = GameState.None;

        public bool IsMainMenuContext => _context == GameUiContext.MainMenu;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolvePanelReferences();
            CleanupLegacyUi();

            var scaler = GetComponent<CanvasScaler>() ?? GetComponentInParent<CanvasScaler>();
            NeonUiLayout.ApplyToCanvas(GetComponent<RectTransform>(), scaler);
        }

        protected virtual void Start()
        {
            if (_context == GameUiContext.Gameplay)
                ApplyGameplayInitialState();
            else
                ShowMainMenu();
        }

        void Update()
        {
            if (_context == GameUiContext.Gameplay)
                SyncWithGameState();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void InitializeAsMainMenu()
        {
            _context = GameUiContext.MainMenu;
            ResolvePanelReferences();
            CleanupLegacyUi();
        }

        void ResolvePanelReferences()
        {
            if (_mainMenuPanel == null)
                _mainMenuPanel = GetComponentInChildren<MainMenuPanel>(true);

            if (_context != GameUiContext.Gameplay)
                return;

            if (_hud == null) _hud = GetComponentInChildren<HudPanel>(true);
            if (_win == null) _win = GetComponentInChildren<WinPanel>(true);
            if (_fail == null) _fail = GetComponentInChildren<FailPanel>(true);
            if (_retry == null) _retry = GetComponentInChildren<RetryPanel>(true);
            if (_settings == null) _settings = GetComponentInChildren<SettingPanel>(true);

            HideAllGameplayPanelsImmediate();
        }

        void CleanupLegacyUi()
        {
            foreach (var panel in GetComponentsInChildren<LevelSelectPanel>(true))
                Destroy(panel.gameObject);

            foreach (var legacy in GetComponentsInChildren<SettingsPanel>(true))
                legacy.gameObject.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAll();
            _mainMenuPanel?.Show(false);
        }

        void ApplyGameplayInitialState()
        {
            HideAll();
            InitializeHUD();
        }

        public void InitializeHUD()
        {
            if (_hud == null || !LevelManager.IsInitialized)
                return;

            _hud.Show(false);
            _hud.SetLevelIndex(LevelManager.Instance.ActiveLevelNumber - 1);
            RefreshLivesDisplay();
        }

        public void RefreshLivesDisplay()
        {
            _hud?.RefreshLivesDisplay();
        }

        void SyncWithGameState()
        {
            if (!LevelManager.IsInitialized)
                return;

            GameState current = StateManager.Instance.CurrentState;
            if (current == _lastState)
                return;

            _lastState = current;

            if (current == GameState.OnStart)
            {
                HideAll();
                InitializeHUD();
            }
            else if (current == GameState.OnWin)
            {
                ShowWin();
                PlayFeedback(_keyOnWin, HapticType.Success);
            }
            else if (current == GameState.OnLose)
            {
                ShowFail();
                PlayFeedback(_keyOnLose, HapticType.Failure);
            }
        }

        void PlayFeedback(string audioKey, HapticType haptic)
        {
            if (AudioManager.IsInitialized && !string.IsNullOrEmpty(audioKey))
                AudioManager.Instance.Play(audioKey);
            if (HapticManager.IsInitialized)
                HapticManager.Instance.Play(haptic);
        }

        public void HideAll()
        {
            _mainMenuPanel?.Hide(false);
            _hud?.Hide(false);
            _win?.Hide(false);
            _fail?.Hide(false);
            _settings?.Hide(false);
            _retry?.Hide(false);
        }

        void HideAllGameplayPanelsImmediate()
        {
            _win?.HideImmediate();
            _fail?.HideImmediate();
            _settings?.HideImmediate();
            _retry?.HideImmediate();
            _hud?.HideImmediate();
        }

        public void OnPlayRequested()
        {
            GameSessionBootstrap.RequestContinue();
            SceneFlow.LoadGame();
        }

        public void OnLevelSelected(int levelNumber)
        {
            if (IsMainMenuContext)
            {
                LevelProgress.ActiveLevelNumber = levelNumber;
                GameSessionBootstrap.RequestPlay(levelNumber);
                SceneFlow.LoadGame();
                return;
            }

            if (LevelManager.IsInitialized)
                LevelManager.Instance.LoadLevel(levelNumber);

            HideAll();
            InitializeHUD();
        }

        public void OnReturnToMainMenu()
        {
            if (InputHandler.IsInitialized && InputHandler.Instance != null)
                InputHandler.Instance.UnlockInput();

            Time.timeScale = 1f;
            SceneFlow.LoadMainMenu();
        }

        void ShowWin()
        {
            HideExcept(_win);
            if (_win == null || !LevelManager.IsInitialized)
                return;

            if (InputHandler.IsInitialized && InputHandler.Instance != null)
                InputHandler.Instance.UnlockInput();

            Level active = LevelManager.Instance.ActiveLevelInstance;
            LevelConfig config = ResolveConfig(active);
            int stars = StarEvaluator.EvaluateStarsByLives();
            int reward = Mathf.Max(0, config != null ? config.WinCoins : 10);
            int totalBefore = CurrencyWallet.IsInitialized ? CurrencyWallet.Instance.Coins : 0;
            _win.Setup(stars, reward, totalBefore);
            _win.Show();
        }

        void ShowFail()
        {
            HideExcept(_fail);
            if (_fail == null)
                return;

            if (InputHandler.IsInitialized && InputHandler.Instance != null)
                InputHandler.Instance.UnlockInput();

            Level active = LevelManager.IsInitialized ? LevelManager.Instance.ActiveLevelInstance : null;
            LevelConfig config = ResolveConfig(active);
            int reward = Mathf.Max(0, config != null ? config.FailCoins : 0);
            if (reward > 0 && CurrencyWallet.IsInitialized)
                CurrencyWallet.Instance.Add(reward);

            _fail.Setup(reward);
            _fail.Show();
        }

        LevelConfig ResolveConfig(Level level)
        {
            if (level != null)
            {
                LevelConfig attached = level.GetComponent<LevelConfig>();
                if (attached == null)
                    attached = level.GetComponentInChildren<LevelConfig>(true);
                if (attached != null)
                    return attached;
            }

            return _fallbackConfig;
        }

        void HideExcept(UIPanel screen)
        {
            if (_mainMenuPanel != null && _mainMenuPanel != screen) _mainMenuPanel.Hide(true);
            if (_hud != null && _hud != screen) _hud.Hide(true);
            if (_win != null && _win != screen) _win.Hide(true);
            if (_fail != null && _fail != screen) _fail.Hide(true);
            if (_settings != null && _settings != screen) _settings.Hide(true);
            if (_retry != null && _retry != screen) _retry.Hide(true);
        }

        public void OnRestartRequested()
        {
            _retry?.Show();
            PlayFeedback(_keyOnRestartDialog, HapticType.Medium);
        }

        public void OnWinReplayRequested()
        {
            if (InputHandler.IsInitialized && InputHandler.Instance != null)
                InputHandler.Instance.UnlockInput();

            HideAll();

            if (LevelManager.IsInitialized)
                LevelManager.Instance.RestartLevel();

            if (_lastState != GameState.OnStart)
                _lastState = GameState.None;

            InitializeHUD();
            PlayFeedback(_keyOnRestartConfirm, HapticType.Medium);
        }

        public void OnRestartConfirmed()
        {
            OnWinReplayRequested();
        }

        public void OnNextLevelRequested()
        {
            HideAll();
            if (!LevelManager.IsInitialized)
                return;

            LevelManager.Instance.IncreaseLevelNumber();
            LevelManager.Instance.LoadCurrentLevel();
            InitializeHUD();
            PlayFeedback(_keyOnNext, HapticType.Light);
        }

        public void ProceedNextLevelAfterReward(int reward)
        {
            if (CurrencyWallet.IsInitialized && reward > 0)
                CurrencyWallet.Instance.Add(reward);
            OnNextLevelRequested();
        }

        public void OnOpenSettings()
        {
            _settings?.Show();
            PlayFeedback(_keyOnOpenSettings, HapticType.Selection);
        }

        public void UpdateTimeDisplay(float remainingTime)
        {
            _hud?.UpdateTimeDisplay(remainingTime);
        }
    }
}
