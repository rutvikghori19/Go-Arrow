using System;
using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class HudPanel : UIPanel
    {
        [SerializeField] TextMeshProUGUI _levelText;
        [SerializeField] TextMeshProUGUI _timeText;
        [SerializeField] Button _restartButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] HeartPanel _heartPanel;

        static readonly string[] RestartButtonNames = { "NeonRestartBtn", "RestartButton" };
        static readonly string[] SettingsButtonNames = { "NeonSettingsBtn", "SettingsButton" };

        bool _livesSubscribed;

        void Awake()
        {
            EnsureWired();
        }

        void OnEnable()
        {
            SubscribeToLivesManager();
            InitializeHeartPanel();
        }

        void OnDisable()
        {
            UnsubscribeFromLivesManager();
        }

        public override void Show(bool playSound = true)
        {
            EnsureWired();
            base.Show(playSound);
        }

        void EnsureWired()
        {
            ResolveReferences();
            WireButtons();
            HideLegacyButtonsWhenNeonPresent();
        }

        void ResolveReferences()
        {
            if (_levelText == null || !_levelText)
                _levelText = FindNamedComponent<TextMeshProUGUI>("LevelText");

            if (_timeText == null || !_timeText)
                _timeText = FindNamedComponent<TextMeshProUGUI>("Time_text");

            if (_heartPanel == null)
                _heartPanel = GetComponentInChildren<HeartPanel>(true);
        }

        void WireButtons()
        {
            WireButton(ref _restartButton, FindFirstButton(RestartButtonNames), OnRestartClicked);
            WireButton(ref _settingsButton, FindFirstButton(SettingsButtonNames), OnSettingsClicked);
        }

        void HideLegacyButtonsWhenNeonPresent()
        {
            HideLegacyButton("RestartButton", "NeonRestartBtn");
            HideLegacyButton("SettingsButton", "NeonSettingsBtn");
        }

        void HideLegacyButton(string legacyName, string neonName)
        {
            if (FindButton(neonName) == null)
                return;

            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != legacyName)
                    continue;

                t.gameObject.SetActive(false);
                break;
            }
        }

        void WireButton(ref Button cachedButton, Button button, Action handler)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.BindOnClick(this, handler);
            cachedButton = button;
            DisableChildRaycasts(button.transform);
        }

        Button FindFirstButton(string[] buttonNames)
        {
            foreach (string buttonName in buttonNames)
            {
                Button button = FindButton(buttonName);
                if (button != null)
                    return button;
            }

            return null;
        }

        static void DisableChildRaycasts(Transform buttonRoot)
        {
            foreach (var graphic in buttonRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.transform == buttonRoot)
                    continue;

                graphic.raycastTarget = false;
            }
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

        Button FindButton(string buttonName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        void SubscribeToLivesManager()
        {
            if (_livesSubscribed || !LivesManager.IsInitialized)
                return;

            LivesManager.Instance.OnLivesChanged += HandleLivesChanged;
            _livesSubscribed = true;
        }

        void UnsubscribeFromLivesManager()
        {
            if (!_livesSubscribed || !LivesManager.IsInitialized)
                return;

            LivesManager.Instance.OnLivesChanged -= HandleLivesChanged;
            _livesSubscribed = false;
        }

        void InitializeHeartPanel()
        {
            if (_heartPanel == null)
                return;

            _heartPanel.Initialize();

            if (LivesManager.IsInitialized)
                _heartPanel.UpdateHearts(LivesManager.Instance.CurrentLives);
        }

        void HandleLivesChanged(int currentLives)
        {
            if (_heartPanel != null)
                _heartPanel.UpdateHearts(currentLives);
        }

        public void RefreshLivesDisplay()
        {
            InitializeHeartPanel();
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
            GameUIManager.Instance?.OnRestartRequested();
        }

        void OnSettingsClicked()
        {
            GameUIManager.Instance?.OnOpenSettings();
        }
    }
}
