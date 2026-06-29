using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public sealed class MainMenuPanel : UIPanel
    {
        [SerializeField] Button _playButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] TextMeshProUGUI _levelText;

        bool _wired;

        void Awake()
        {
            WireIfNeeded();
        }

        void OnEnable()
        {
            RefreshLevelLabel();
        }

        void WireIfNeeded()
        {
            if (_wired)
                return;

            if (_playButton == null)
                _playButton = transform.Find("PlayButton")?.GetComponent<Button>();

            if (_settingsButton == null)
                _settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();

            if (_levelText == null)
                _levelText = transform.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();

            var levelsButton = transform.Find("LevelsButton");
            if (levelsButton != null)
                levelsButton.gameObject.SetActive(false);

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => GameUIManager.Instance?.OnPlayRequested());
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => GameUIManager.Instance?.OnOpenSettings());
            }

            _wired = true;
        }

        public override void Show(bool playSound = true)
        {
            RefreshLevelLabel();
            base.Show(playSound);
        }

        public void RefreshDisplay() => RefreshLevelLabel();

        void RefreshLevelLabel()
        {
            if (_levelText == null)
                return;

            _levelText.text = $"Level {LevelProgress.ActiveLevelNumber}";
        }
    }
}
