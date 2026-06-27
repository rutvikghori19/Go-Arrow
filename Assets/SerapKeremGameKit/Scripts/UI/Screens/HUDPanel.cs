using _Game.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class HUDPanel : UIPanel
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameUIManager _uiRoot;
        [SerializeField] private HeartPanel _heartPanel;

        bool _isInitialized;

        void Awake()
        {
            if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
            if (_settingsButton != null) _settingsButton.BindOnClick(this, OnSettingsClicked);
            ResolveReferences();
        }

        void ResolveReferences()
        {
            if (_levelText == null || !_levelText)
                _levelText = FindNamedComponent<TextMeshProUGUI>("LevelText");

            if (_levelText == null)
            {
                foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp.name.Contains("Level"))
                    {
                        _levelText = tmp;
                        break;
                    }
                }
            }

            if (_timeText == null || !_timeText)
                _timeText = FindNamedComponent<TextMeshProUGUI>("Time_text");

            if (_timeText == null)
            {
                foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp.name.Contains("Time"))
                    {
                        _timeText = tmp;
                        break;
                    }
                }
            }

            if (_heartPanel == null)
                _heartPanel = GetComponentInChildren<HeartPanel>(true);
        }

        T FindNamedComponent<T>(string objectName) where T : Component
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != objectName)
                    continue;

                var component = t.GetComponent<T>();
                if (component != null)
                    return component;
            }

            return null;
        }

        public override void Show(bool playSound = true)
        {
            ResolveReferences();
            bool respectPrefabLayout = _uiRoot != null && _uiRoot.PrefabBuiltUi;
            NeonHudBuilder.Apply(this, respectPrefabLayout);

            base.Show(playSound);

            if (!_isInitialized)
                Initialize();

            SubscribeToLivesManager();
            InitializeHeartPanel();
        }

        void Initialize()
        {
            if (_isInitialized)
                return;

            ResolveReferences();
            _isInitialized = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromLivesManager();
        }

        void SubscribeToLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                _uiRoot.LivesManagerInstance.OnLivesChanged += HandleLivesChanged;
        }

        void UnsubscribeFromLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                _uiRoot.LivesManagerInstance.OnLivesChanged -= HandleLivesChanged;
        }

        void InitializeHeartPanel()
        {
            if (_heartPanel == null)
                return;

            _heartPanel.Initialize();
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                _heartPanel.UpdateHearts(_uiRoot.LivesManagerInstance.CurrentLives);
        }

        void HandleLivesChanged(int currentLives)
        {
            if (_heartPanel != null)
                _heartPanel.UpdateHearts(currentLives);
        }

        public void RefreshLivesDisplay()
        {
            if (_heartPanel == null)
                return;

            _heartPanel.Initialize();

            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                _heartPanel.UpdateHearts(_uiRoot.LivesManagerInstance.CurrentLives);
        }

        public void SetLevelIndex(int levelIndex)
        {
            if (_levelText != null)
                _levelText.text = $"LEVEL {levelIndex + 1}";
        }

        public void UpdateTimeDisplay(float remainingTime)
        {
            if (_timeText != null)
                _timeText.text = FormatTime(remainingTime);
        }

        static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:D2}:{remainingSeconds:D2}";
        }

        void OnRestartClicked()
        {
            if (_uiRoot != null)
                _uiRoot.OnRestartRequested();
        }

        void OnSettingsClicked()
        {
            if (_uiRoot != null)
                _uiRoot.OnOpenSettings();
        }

        public void PressRestart() => OnRestartClicked();

        public void PressSettings() => OnSettingsClicked();

        public void SetUIRoot(GameUIManager uiRoot)
        {
            _uiRoot = uiRoot;
        }
    }
}
