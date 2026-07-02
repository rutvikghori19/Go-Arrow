using _Game.ProceduralLevels;
using _Game.UI;
using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Singletons;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Utilities;

namespace SerapKeremGameKit._Managers
{
    [DefaultExecutionOrder(-2)]
    public class LevelManager : MonoSingleton<LevelManager>
    {
        #region Properties & Data Access

        private const string ProgressKey = PreferencesKeys.ProgressData;
        public int ActiveLevelNumber
        {
            get => PlayerPrefs.GetInt(ProgressKey, 1);
            set { PlayerPrefs.SetInt(ProgressKey, value); SaveUtility.SaveImmediate(); }
        }

        [Tooltip("Use random selection after tutorials are completed.")]
        [SerializeField] private bool _useRandomSelection = true;

        [Title("Level Collections")]
        [ListDrawerSettings(Draggable = true, AlwaysExpanded = false)]
        [FormerlySerializedAs("_gameplayLevels")]
        [SerializeField, Required] private Level[] _levels;

        public Level ActiveLevelInstance { get; private set; }
        public int ProcessedLevelIndex { get; private set; }

        // Public accessors for external systems
        public Level[] GameplayLevels => _levels;
        public int HandcraftedLevelCount => _levels != null ? _levels.Length : 0;
        public int GameplayLevelCount => ProceduralLevelConstants.TotalLevelCount;
        public int TotalLevelCount => ProceduralLevelConstants.TotalLevelCount;

        #endregion

        // Events removed to keep template simple and robust

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            PerformInitialValidation();
        }

        void Start()
        {
            if (!GameSessionBootstrap.ShouldStartLevelOnLoad)
                return;

            StartCurrentLevelInstance();
        }

        public void StartCurrentLevelInstance()
        {
            ConfigureEnvironment();

            if (GameSessionBootstrap.PendingLevelNumber > 0)
            {
                int level = GameSessionBootstrap.PendingLevelNumber;
                GameSessionBootstrap.ClearAfterLoad();
                LoadLevel(level);
                return;
            }

            GameSessionBootstrap.ClearAfterLoad();
            LoadCurrentLevel();
        }

        #endregion

        #region Core Level Management

        public void LoadCurrentLevel()
        {
            int levelNumber = ClampLevel(ActiveLevelNumber);
            ActiveLevelNumber = levelNumber;
            ProcessedLevelIndex = levelNumber;
            InstantiateAndBegin(ResolveHandcraftedLevel(levelNumber));
        }

        static int ClampLevel(int levelNumber)
        {
            return Mathf.Clamp(levelNumber, 1, ProceduralLevelConstants.TotalLevelCount);
        }

        bool HasConfiguredHandcraftedLevel(int levelNumber)
        {
            if (_levels == null || levelNumber < 1 || levelNumber > _levels.Length)
                return false;

            return _levels[levelNumber - 1] != null;
        }

        Level ResolveHandcraftedLevel(int levelNumber)
        {
            if (HasConfiguredHandcraftedLevel(levelNumber))
                return _levels[levelNumber - 1];

            var resourceLevel = Resources.Load<Level>($"Levels/Level {levelNumber}");
            if (resourceLevel != null)
                return resourceLevel;

            TraceLogger.LogWarning($"{name}: Missing prefab for level {levelNumber}.", this);
            return null;
        }

        private void InstantiateAndBegin(Level targetLevel)
        {
            if (targetLevel == null)
            {
                TraceLogger.LogWarning($"{name}: No level prefab to load.", this);
                return;
            }

            ActiveLevelInstance = Instantiate(targetLevel);
            BeginLoadedLevel();
        }

        void BeginLoadedLevel()
        {
            ActiveLevelInstance.Load();
            Time.timeScale = 1f;
            if (SerapKeremGameKit._InputSystem.InputHandler.Instance != null)
            {
                SerapKeremGameKit._InputSystem.InputHandler.Instance.UnlockInput();
            }
            StateManager.Instance.SetLoading();
            // UI: update level text on load
            //UIManager.Instance?.RefreshLevelNumber();
            StartLevel();
        }

        #endregion

        #region Level Control Methods

        public void StartLevel()
        {
            ActiveLevelInstance.Play();
            StateManager.Instance.SetOnStart();
            // UI: show in-game UI and refresh text
            //UIManager.Instance?.ShowInGameUI();
            //UIManager.Instance?.RefreshLevelNumber();
        }

        public void RetryLevel()
        {
            TerminateCurrentLevel();
            InstantiateAndBegin(ResolveHandcraftedLevel(ProcessedLevelIndex));
        }

        public void RestartLevel()
        {
            StateManager.Instance.SetOnRestart();
            RetryLevel();
        }

        public void LoadLevel(int levelNumber)
        {
            TerminateCurrentLevel();
            levelNumber = ClampLevel(levelNumber);
            ActiveLevelNumber = levelNumber;
            ProcessedLevelIndex = levelNumber;
            InstantiateAndBegin(ResolveHandcraftedLevel(levelNumber));
        }

        public void CleanCurrentLevel()
        {
            TerminateCurrentLevel();
            // UI: hide gameplay UI if needed
            //UIManager.Instance?.HideInGameUI();
        }

        public void IncreaseLevelNumber()
        {
            TerminateCurrentLevel();
            if (ActiveLevelNumber < TotalLevelCount)
                ActiveLevelNumber++;
        }

        private void TerminateCurrentLevel()
        {
            // notify state if needed
            if (ActiveLevelInstance != null)
                Destroy(ActiveLevelInstance.gameObject);
        }

        #endregion

        #region Game Result Handlers
        [Button("Test LevelWin")]
        public void Win()
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnWin();
            // Example: level-based coin reward
            // Currency.Currency.Add(ActiveLevelNumber * 5);
            // UI: show win panel
            //UIManager.Instance?.ShowWinScreen();
        }

        [Button("Test LevelWin")]
        public void Win(int moveCount)
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnWin();
            // Example: move-based bonus
            // int bonus = Mathf.Max(0, 10 - moveCount);
            // Currency.Currency.Add(bonus);
        }

        [Button("Test LevelLose")]
        public void Lose()
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnLose();
            // UI: show fail panel
            //UIManager.Instance?.ShowFailScreen();
        }

        private bool ValidateGameStateForEvents()
        {
            return StateManager.Instance.CurrentState == GameState.OnStart;
        }

        #endregion

        #region Utility & Validation Methods

        private void PerformInitialValidation()
        {
            if (_levels == null || _levels.Length == 0)
                TraceLogger.LogWarning($"{name}: Levels array is not configured.", this);
        }

        private void ConfigureEnvironment()
        {
#if UNITY_EDITOR
            CleanupExistingLevelsInEditor();
#endif
        }

#if UNITY_EDITOR
        private void CleanupExistingLevelsInEditor()
        {
            var existingLevelInstances = FindObjectsByType<Level>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var levelInstance in existingLevelInstances)
                levelInstance.gameObject.SetActive(false);
        }
#endif

        #endregion

        public Level GetLevelByNumber(int levelNumber)
        {
            levelNumber = ClampLevel(levelNumber);
            return ResolveHandcraftedLevel(levelNumber);
        }

        public bool IsCurrentLevelProcedural()
        {
            return false;
        }

        #region Utility & Validation Methods
        public Level GetCurrentLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber);
        }

        public Level GetNextLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 1) ?? GetLevelByNumber(1);
        }

        public Level GetNextestLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 2) ?? GetLevelByNumber(1);
        }

        public Level GetFinalLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 3) ?? GetLevelByNumber(1);
        }

        public Level GetFinalNextLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 4) ?? GetLevelByNumber(1);
        }
        #endregion
    }
}