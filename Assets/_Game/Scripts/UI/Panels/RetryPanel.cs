using System;
using SerapKeremGameKit._UI;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class RetryPanel : UIPanel
    {
        [SerializeField] Button _yesButton;
        [SerializeField] Button _noButton;

        bool _wired;

        void Awake()
        {
            WireIfNeeded();
        }

        void WireIfNeeded()
        {
            if (_wired)
                return;

            Button yes = FindButton("YesButtonNeon") ?? _yesButton ?? FindButton("YesButton");
            Button no = FindButton("NoButtonNeon") ?? _noButton ?? FindButton("NoButton");

            WireButton(ref _yesButton, yes, OnYes);
            WireButton(ref _noButton, no, OnNo);
            _wired = true;
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
            GameUIManager.Instance?.OnRestartConfirmed();
        }

        void OnNo()
        {
            Hide(false);
        }
    }
}
