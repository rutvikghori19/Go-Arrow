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

        [Title("Procedural Levels")]
        [SerializeField] private Level _proceduralLevelTemplate;
        [SerializeField] private bool _useProceduralLevels = true;

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
            int levelNumber = ProceduralLevelUtility.ClampLevelNumber(ActiveLevelNumber);
            ActiveLevelNumber = levelNumber;
            ProcessedLevelIndex = levelNumber;

            if (ShouldUseProceduralLevel(levelNumber))
                InstantiateProceduralAndBegin(levelNumber);
            else
                InstantiateAndBegin(ResolveHandcraftedLevel(levelNumber));
        }

        bool ShouldUseProceduralLevel(int levelNumber)
        {
            if (!_useProceduralLevels || ResolveProceduralTemplate() == null)
                return false;

            if (ProceduralLevelUtility.IsHandcraftedLevel(levelNumber) && HasConfiguredHandcraftedLevel(levelNumber))
                return false;

            return ProceduralLevelUtility.IsProceduralLevel(levelNumber);
        }

        bool HasConfiguredHandcraftedLevel(int levelNumber)
        {
            if (_levels == null || levelNumber < 1 || levelNumber > _levels.Length)
                return false;

            var level = _levels[levelNumber - 1];
            return level != null && !IsProceduralTemplate(level);
        }

        Level ResolveHandcraftedLevel(int levelNumber)
        {
            if (HasConfiguredHandcraftedLevel(levelNumber))
                return _levels[levelNumber - 1];

            var resourceLevel = Resources.Load<Level>($"Levels/Level {levelNumber}");
            if (resourceLevel != null && !IsProceduralTemplate(resourceLevel))
                return resourceLevel;

            TraceLogger.LogWarning(
                $"{name}: Missing handcrafted prefab for level {levelNumber}. Falling back to procedural generation.",
                this);
            return null;
        }

        static bool IsProceduralTemplate(Level level)
        {
            return level != null && level.name == "Level_Base";
        }

        Level ResolveProceduralTemplate()
        {
            if (_proceduralLevelTemplate != null)
                return _proceduralLevelTemplate;

            return Resources.Load<Level>("Levels/Level_Base");
        }

        private void InstantiateProceduralAndBegin(int levelNumber)
        {
            var template = ResolveProceduralTemplate();
            ActiveLevelInstance = Instantiate(template);
            var host = ActiveLevelInstance.GetComponent<ProceduralLevelHost>();
            if (host == null)
                host = ActiveLevelInstance.gameObject.AddComponent<ProceduralLevelHost>();

            host.Build(levelNumber);
            BeginLoadedLevel();
        }

        private void InstantiateAndBegin(Level targetLevel)
        {
            if (targetLevel == null)
            {
                InstantiateProceduralAndBegin(ProcessedLevelIndex);
                return;
            }

            if (IsProceduralTemplate(targetLevel))
            {
                InstantiateProceduralAndBegin(ProcessedLevelIndex);
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
            if (ShouldUseProceduralLevel(ProcessedLevelIndex))
                InstantiateProceduralAndBegin(ProcessedLevelIndex);
            else
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
            levelNumber = ProceduralLevelUtility.ClampLevelNumber(levelNumber);
            ActiveLevelNumber = levelNumber;
            ProcessedLevelIndex = levelNumber;

            if (ShouldUseProceduralLevel(levelNumber))
                InstantiateProceduralAndBegin(levelNumber);
            else
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
            levelNumber = ProceduralLevelUtility.ClampLevelNumber(levelNumber);

            if (ProceduralLevelUtility.IsHandcraftedLevel(levelNumber))
            {
                var handcrafted = ResolveHandcraftedLevel(levelNumber);
                if (handcrafted != null)
                    return handcrafted;
            }

            return ResolveProceduralTemplate();
        }

        public bool IsCurrentLevelProcedural()
        {
            return ShouldUseProceduralLevel(ProcessedLevelIndex);
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