using System;
using _Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class RetryPanel : UIPanel
    {
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;
        [SerializeField] private GameUIManager _uiRoot;

        void Awake()
        {
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

            Button yes = FindButton("YesButtonNeon");
            if (yes == null)
                yes = _yesButton != null ? _yesButton : FindButton("YesButton");

            Button no = FindButton("NoButtonNeon");
            if (no == null)
                no = _noButton != null ? _noButton : FindButton("NoButton");

            WireButton(ref _yesButton, yes, OnYes);
            WireButton(ref _noButton, no, OnNo);
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

        void OnYes()
        {
            if (_uiRoot != null)
                _uiRoot.OnRestartConfirmed();
        }

        void OnNo()
        {
            Hide(false);
        }
    }
}
