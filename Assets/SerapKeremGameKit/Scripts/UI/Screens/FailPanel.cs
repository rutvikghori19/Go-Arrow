using System;
using _Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class FailPanel : UIPanel
    {
        [SerializeField] private Image _failIcon;
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private GameUIManager _uiRoot;

        void Awake()
        {
            EnsureWired();
        }

        public void Setup(int rewardedCoins, GameUIManager uiRoot)
        {
            if (_coinText != null)
                _coinText.text = rewardedCoins.ToString();

            _uiRoot = uiRoot;
            EnsureWired();
        }

        public void SetUIRoot(GameUIManager uiRoot)
        {
            _uiRoot = uiRoot;
            EnsureWired();
        }

        public void EnsureWired()
        {
            if (_uiRoot == null)
                _uiRoot = GetComponentInParent<GameUIManager>();

            Button restart = FindButton("RestartButtonNeon");
            if (restart == null)
                restart = _restartButton != null ? _restartButton : FindButton("RestartButton");

            Button home = FindButton("HomeButton");
            if (home == null)
                home = _homeButton;

            WireButton(ref _restartButton, restart, OnRestartClicked);
            WireButton(ref _homeButton, home, OnHomeClicked);
        }

        void WireButton(ref Button cachedButton, Button button, Action handler)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.BindOnClick(this, handler);
            cachedButton = button;
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

        void OnRestartClicked()
        {
            if (_uiRoot != null)
                _uiRoot.OnRestartConfirmed();
        }

        void OnHomeClicked()
        {
            if (_uiRoot != null)
                _uiRoot.OnReturnToMainMenu();
        }
    }
}
