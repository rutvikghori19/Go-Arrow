using System;
using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class FailPanel : UIPanel
    {
        [SerializeField] Image _failIcon;
        [SerializeField] TextMeshProUGUI _coinText;
        [SerializeField] Button _restartButton;
        [SerializeField] Button _homeButton;

        bool _wired;

        void Awake()
        {
            HideLegacyContent();
            WireIfNeeded();
        }

        public override void Show(bool playSound = true)
        {
            WireIfNeeded();
            base.Show(playSound);
        }

        public void Setup(int rewardedCoins)
        {
            if (_coinText != null)
                _coinText.text = rewardedCoins.ToString();
        }

        void WireIfNeeded()
        {
            if (_wired)
                return;

            Button restart = FindButton("RestartButtonNeon") ?? _restartButton ?? FindButton("RestartButton");
            Button home = FindButton("HomeButton") ?? _homeButton;

            WireButton(ref _restartButton, restart, OnRestartClicked);
            WireButton(ref _homeButton, home, OnHomeClicked);
            _wired = true;
        }

        void HideLegacyContent()
        {
            var neonRoot = transform.Find("FailPanelNeon");
            foreach (Transform child in transform)
            {
                if (neonRoot != null && child == neonRoot)
                    continue;

                child.gameObject.SetActive(false);
            }
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
            GameUIManager.Instance?.OnRestartConfirmed();
        }

        void OnHomeClicked()
        {
            GameUIManager.Instance?.OnReturnToMainMenu();
        }
    }
}
