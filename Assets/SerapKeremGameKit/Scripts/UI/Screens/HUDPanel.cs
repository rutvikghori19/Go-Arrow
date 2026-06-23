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
        [SerializeField] private Button _levelSelectButton;
        [SerializeField] private UIRootController _uiRoot;
        [SerializeField] private HeartPanel _heartPanel;

        private bool _isInitialized = false;

        private void Awake()
        {
            if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
            if (_settingsButton != null) _settingsButton.BindOnClick(this, OnSettingsClicked);
            EnsureLevelSelectButton();
            if (_levelSelectButton != null) _levelSelectButton.BindOnClick(this, OnLevelSelectClicked);
        }

        void EnsureLevelSelectButton()
        {
            if (_levelSelectButton != null)
                return;

            Transform displayer = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "LevelDisplayer")
                {
                    displayer = t;
                    break;
                }
            }

            if (displayer == null)
                return;

            _levelSelectButton = displayer.GetComponent<Button>();
            if (_levelSelectButton == null)
                _levelSelectButton = displayer.gameObject.AddComponent<Button>();
        }

        public override void Show(bool playSound = true)
        {
            base.Show(playSound);
            
            if (!_isInitialized)
            {
                Initialize();
            }
            
            SubscribeToLivesManager();
            InitializeHeartPanel();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            ApplyNeonHudStyle();
            _isInitialized = true;
        }

        void ApplyNeonHudStyle()
        {
            if (_levelText != null)
                _levelText.color = NeonTheme.UiHudText;
            if (_timeText != null)
                _timeText.color = NeonTheme.UiHudText;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromLivesManager();
            // Auto-unsubscribe handled by ButtonExtensions
        }

        private void SubscribeToLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
            {
                _uiRoot.LivesManagerInstance.OnLivesChanged += HandleLivesChanged;
            }
        }

        private void UnsubscribeFromLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
            {
                _uiRoot.LivesManagerInstance.OnLivesChanged -= HandleLivesChanged;
            }
        }

        private void InitializeHeartPanel()
        {
            if (_heartPanel != null)
            {
                _heartPanel.Initialize();
                if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                {
                    _heartPanel.UpdateHearts(_uiRoot.LivesManagerInstance.CurrentLives);
                }
            }
        }

        private void HandleLivesChanged(int currentLives)
        {
            if (_heartPanel != null)
            {
                _heartPanel.UpdateHearts(currentLives);
            }
        }

        public void SetLevelIndex(int levelIndex)
        {
            if (_levelText != null)
                _levelText.text = $"Level {levelIndex + 1}";
        }

        public void UpdateTimeDisplay(float remainingTime)
        {
            if (_timeText != null)
            {
                _timeText.text = FormatTime(remainingTime);
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;

            return $"{minutes:D2}:{remainingSeconds:D2}";
        }

        private void OnRestartClicked()
        {
            if (_uiRoot != null) _uiRoot.OnRestartRequested();
        }

        private void OnSettingsClicked()
        {
            if (_uiRoot != null) _uiRoot.OnOpenSettings();
        }

        private void OnLevelSelectClicked()
        {
            if (_uiRoot != null) _uiRoot.OnOpenLevelSelect();
        }

        public void SetUIRoot(UIRootController uiRoot)
        {
            _uiRoot = uiRoot;
        }
    }
}