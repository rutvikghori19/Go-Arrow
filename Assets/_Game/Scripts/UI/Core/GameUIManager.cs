using _Game.Theme;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._Haptics;
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

        [Header("Gameplay References")]
        [SerializeField] HUDPanel _hud;
        [SerializeField] WinPanel _win;
        [SerializeField] FailPanel _fail;
        [SerializeField] UIPanel _settings;
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
        MainMenuPanel _mainMenu;

        public bool IsMainMenuContext => _context == GameUiContext.MainMenu;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_context == GameUiContext.Gameplay)
                WireGameplayPanels();

            var scaler = GetComponent<CanvasScaler>() ?? GetComponentInParent<CanvasScaler>();
            NeonUiLayout.ApplyToCanvas(GetComponent<RectTransform>(), scaler);
        }

        void WireGameplayPanels()
        {
            if (_hud == null) _hud = GetComponentInChildren<HUDPanel>(true);
            if (_win == null) _win = GetComponentInChildren<WinPanel>(true);
            if (_fail == null) _fail = GetComponentInChildren<FailPanel>(true);
            if (_retry == null) _retry = GetComponentInChildren<RetryPanel>(true);

            CleanupLegacySettings();
            CleanupLevelSelect();
            EnsureSettingsPanel();

            if (_hud != null) _hud.SetUIRoot(this);
            if (_win != null) _win.SetUIRoot(this);
            if (_fail != null) _fail.SetUIRoot(this);
            if (_retry != null) _retry.SetUIRoot(this);

            HideAllGameplayPanelsImmediate();
            NeonGameplayUiStyler.Apply(this);
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
            CleanupLevelSelect();
            EnsureSettingsPanel();
            BuildMainMenuIfNeeded();
            HideAllGameplayPanelsImmediate();
            if (_settings != null) _settings.HideImmediate();
        }

        void BuildMainMenuIfNeeded()
        {
            if (_mainMenu != null)
                return;

            _mainMenu = GetComponentInChildren<MainMenuPanel>(true);
            if (_mainMenu == null)
            {
                var go = new GameObject("MainMenuPanel", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                NeonUiBuilder.Stretch(go.GetComponent<RectTransform>());
                _mainMenu = go.AddComponent<MainMenuPanel>();
            }

            _mainMenu.Initialize(this);
        }

        public void ShowMainMenu()
        {
            HideAll();
            if (_mainMenu != null)
                _mainMenu.Show(false);
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
                if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnWin))
                    AudioManager.Instance.Play(_keyOnWin);
                if (HapticManager.IsInitialized)
                    HapticManager.Instance.Play(HapticType.Success);
            }
            else if (current == GameState.OnLose)
            {
                ShowFail();
                if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnLose))
                    AudioManager.Instance.Play(_keyOnLose);
                if (HapticManager.IsInitialized)
                    HapticManager.Instance.Play(HapticType.Failure);
            }
        }

        public void HideAll()
        {
            if (_mainMenu != null) _mainMenu.Hide(false);
            if (_hud != null) _hud.Hide(false);
            if (_win != null) _win.Hide(false);
            if (_fail != null) _fail.Hide(false);
            if (_settings != null) _settings.Hide(false);
            if (_retry != null) _retry.Hide(false);
        }

        void HideAllGameplayPanelsImmediate()
        {
            if (_win != null) _win.HideImmediate();
            if (_fail != null) _fail.HideImmediate();
            if (_settings != null) _settings.HideImmediate();
            if (_retry != null) _retry.HideImmediate();
            if (_hud != null) _hud.HideImmediate();
        }

        void CleanupLevelSelect()
        {
            foreach (var panel in GetComponentsInChildren<LevelSelectPanel>(true))
                Destroy(panel.gameObject);
        }

        void CleanupLegacySettings()
        {
            foreach (var legacy in GetComponentsInChildren<SettingsPanel>(true))
                legacy.gameObject.SetActive(false);
        }

        void EnsureSettingsPanel()
        {
            CleanupLegacySettings();

            if (_settings != null && _settings is not NeonSettingsPanel)
                _settings = null;

            if (_settings is NeonSettingsPanel neonPanel)
            {
                neonPanel.HideImmediate();
                return;
            }

            var existing = GetComponentInChildren<NeonSettingsPanel>(true);
            if (existing != null)
            {
                _settings = existing;
                _settings.HideImmediate();
                return;
            }

            var go = new GameObject("NeonSettingsPanel", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            NeonUiBuilder.Stretch(go.GetComponent<RectTransform>());
            _settings = go.AddComponent<NeonSettingsPanel>();
            _settings.HideImmediate();
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
            Time.timeScale = 1f;
            SceneFlow.LoadMainMenu();
        }

        void ShowWin()
        {
            HideExcept(_win);
            if (_win == null || !LevelManager.IsInitialized)
                return;

            Level active = LevelManager.Instance.ActiveLevelInstance;
            LevelConfig config = ResolveConfig(active);
            int stars = StarEvaluator.EvaluateStarsByLives();
            int reward = Mathf.Max(0, config != null ? config.WinCoins : 10);
            int totalBefore = CurrencyWallet.IsInitialized ? CurrencyWallet.Instance.Coins : 0;
            _win.Setup(stars, reward, totalBefore, this);
            _win.Show();
        }

        void ShowFail()
        {
            HideExcept(_fail);
            if (_fail == null)
                return;

            Level active = LevelManager.IsInitialized ? LevelManager.Instance.ActiveLevelInstance : null;
            LevelConfig config = ResolveConfig(active);
            int reward = Mathf.Max(0, config != null ? config.FailCoins : 0);
            if (reward > 0 && CurrencyWallet.IsInitialized)
                CurrencyWallet.Instance.Add(reward);
            _fail.Setup(reward, this);
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
            if (_mainMenu != null && _mainMenu != screen) _mainMenu.Hide(true);
            if (_hud != null && _hud != screen) _hud.Hide(true);
            if (_win != null && _win != screen) _win.Hide(true);
            if (_fail != null && _fail != screen) _fail.Hide(true);
            if (_settings != null && _settings != screen) _settings.Hide(true);
            if (_retry != null && _retry != screen) _retry.Hide(true);
        }

        public void OnRestartRequested()
        {
            if (_retry != null) _retry.Show();
            if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnRestartDialog))
                AudioManager.Instance.Play(_keyOnRestartDialog);
            if (HapticManager.IsInitialized)
                HapticManager.Instance.Play(HapticType.Medium);
        }

        public void OnRestartConfirmed()
        {
            HideAll();
            if (LevelManager.IsInitialized)
                LevelManager.Instance.RestartLevel();
            InitializeHUD();
            if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnRestartConfirm))
                AudioManager.Instance.Play(_keyOnRestartConfirm);
            if (HapticManager.IsInitialized)
                HapticManager.Instance.Play(HapticType.Medium);
        }

        public void OnNextLevelRequested()
        {
            HideAll();
            if (!LevelManager.IsInitialized)
                return;

            LevelManager.Instance.IncreaseLevelNumber();
            LevelManager.Instance.LoadCurrentLevel();
            InitializeHUD();
            if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnNext))
                AudioManager.Instance.Play(_keyOnNext);
            if (HapticManager.IsInitialized)
                HapticManager.Instance.Play(HapticType.Light);
        }

        public void ProceedNextLevelAfterReward(int reward)
        {
            if (CurrencyWallet.IsInitialized && reward > 0)
                CurrencyWallet.Instance.Add(reward);
            OnNextLevelRequested();
        }

        public void OnOpenSettings()
        {
            EnsureSettingsPanel();
            if (_settings != null) _settings.Show();
            if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_keyOnOpenSettings))
                AudioManager.Instance.Play(_keyOnOpenSettings);
            if (HapticManager.IsInitialized)
                HapticManager.Instance.Play(HapticType.Selection);
        }

        public void UpdateTimeDisplay(float remainingTime)
        {
            if (_hud != null)
                _hud.UpdateTimeDisplay(remainingTime);
        }

        public LivesManager LivesManagerInstance =>
            LivesManager.IsInitialized ? LivesManager.Instance : null;
    }
}
