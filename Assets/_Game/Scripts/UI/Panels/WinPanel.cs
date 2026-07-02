using System;
using DG.Tweening;
using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class WinPanel : UIPanel
    {
        static readonly string[] RestartButtonNames =
        {
            "RestartButtonNeon",
            "LevelRestartButtonNeon",
            "RestartButton",
            "LevelRestartButton",
        };

        [Header("Stars")]
        [SerializeField] GameObject _star1;
        [SerializeField] GameObject _star2;
        [SerializeField] GameObject _star3;
        [SerializeField] GameObject _starZeroIcon;

        [Header("Coin")]
        [SerializeField] TextMeshProUGUI _coinText;
        [SerializeField] TextMeshProUGUI _totalCoinText;
        [SerializeField] Button _nextButton;
        [SerializeField] Button _restartButton;
        [SerializeField] CoinFlyAnimator _coinFly;
        [SerializeField] RectTransform _totalCoinTarget;

        [Header("Lives Summary")]
        [SerializeField] HeartPanel _heartPanel;
        [SerializeField] TextMeshProUGUI _livesLostText;

        Transform _neonPanelRoot;
        int _pendingReward;
        Sequence _pendingSequence;
        bool _wired;

        Transform NeonPanelRoot
        {
            get
            {
                if (_neonPanelRoot == null)
                    _neonPanelRoot = transform.Find("WinPanelNeon");
                return _neonPanelRoot;
            }
        }

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

        public void Setup(int stars, int rewardedCoins, int totalCoins)
        {
            SetStars(stars);
            if (_coinText != null) _coinText.text = rewardedCoins.ToString();
            if (_totalCoinText != null) _totalCoinText.text = Mathf.Max(0, totalCoins).ToString();
            _pendingReward = rewardedCoins;
            UpdateLivesSummary();
        }

        void WireIfNeeded()
        {
            if (_wired)
                return;

            Transform neonRoot = NeonPanelRoot;
            DisableNeonPanelRaycast(neonRoot);

            Button next = FindButtonInNeon("NextButtonNeon", neonRoot);
            if (next == null && IsChildOfNeonPanel(_nextButton, neonRoot))
                next = _nextButton;

            Button restart = FindRestartButton(neonRoot);
            if (restart == null && IsChildOfNeonPanel(_restartButton, neonRoot))
                restart = _restartButton;

            WireButton(ref _nextButton, next, OnNextClicked);
            WireButton(ref _restartButton, restart, OnRestartClicked);
            ResolveHeartPanel();
            ResolveLivesLostText();
            _wired = true;
        }

        void DisableNeonPanelRaycast(Transform neonRoot)
        {
            if (neonRoot == null)
                return;

            var image = neonRoot.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }

        void HideLegacyContent()
        {
            Transform neonRoot = NeonPanelRoot;
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

        Button FindRestartButton(Transform neonRoot)
        {
            if (neonRoot == null)
                return null;

            foreach (string buttonName in RestartButtonNames)
            {
                Button named = FindButtonInNeon(buttonName, neonRoot);
                if (named != null)
                    return named;
            }

            foreach (var button in neonRoot.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null)
                    continue;

                string text = label.text.ToUpperInvariant();
                if (text.Contains("NEXT"))
                    continue;

                if (text.Contains("RESTART") || text.Contains("REPLAY") || text.Contains("AGAIN"))
                    return button;
            }

            return null;
        }

        Button FindButtonInNeon(string buttonName, Transform neonRoot)
        {
            if (neonRoot == null || string.IsNullOrEmpty(buttonName))
                return null;

            var direct = neonRoot.Find(buttonName);
            if (direct != null && direct.TryGetComponent(out Button directButton))
                return directButton;

            foreach (var button in neonRoot.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        static bool IsChildOfNeonPanel(Button button, Transform neonRoot)
        {
            return button != null && neonRoot != null && button.transform.IsChildOf(neonRoot);
        }

        void ResolveHeartPanel()
        {
            if (_heartPanel != null)
                return;

            foreach (var panel in GetComponentsInChildren<HeartPanel>(true))
            {
                if (panel.GetComponentInParent<WinPanel>(true) == this)
                {
                    _heartPanel = panel;
                    return;
                }
            }

            var heartRoot = FindChildTransform("HeartPanel") ?? FindChildTransform("Heart");
            if (heartRoot == null)
                return;

            _heartPanel = heartRoot.GetComponent<HeartPanel>();
            if (_heartPanel == null)
                _heartPanel = heartRoot.gameObject.AddComponent<HeartPanel>();
        }

        void ResolveLivesLostText()
        {
            if (_livesLostText != null)
                return;

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name == "LivesLostText" || tmp.name == "LivesText")
                {
                    _livesLostText = tmp;
                    return;
                }
            }
        }

        Transform FindChildTransform(string objectName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }

            return null;
        }

        void UpdateLivesSummary()
        {
            ResolveHeartPanel();
            ResolveLivesLostText();

            if (!LivesManager.IsInitialized)
            {
                if (_heartPanel != null)
                    _heartPanel.gameObject.SetActive(false);
                if (_livesLostText != null)
                    _livesLostText.gameObject.SetActive(false);
                return;
            }

            int currentLives = LivesManager.Instance.CurrentLives;
            int livesLost = LivesManager.Instance.LivesLostCount;

            if (_heartPanel != null)
            {
                _heartPanel.gameObject.SetActive(true);
                _heartPanel.Initialize();
                _heartPanel.UpdateHearts(currentLives);
            }

            if (_livesLostText != null)
            {
                _livesLostText.gameObject.SetActive(true);
                _livesLostText.text = FormatLivesLostMessage(livesLost);
            }
        }

        static string FormatLivesLostMessage(int livesLost)
        {
            if (livesLost <= 0)
                return "No lives lost";
            if (livesLost == 1)
                return "1 life lost";
            return $"{livesLost} lives lost";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_pendingSequence != null && _pendingSequence.IsActive())
            {
                _pendingSequence.Kill();
                _pendingSequence = null;
            }
        }

        void SetStars(int count)
        {
            if (_star1 != null) _star1.SetActive(count >= 1);
            if (_star2 != null) _star2.SetActive(count >= 2);
            if (_star3 != null) _star3.SetActive(count >= 3);
            if (_starZeroIcon != null) _starZeroIcon.SetActive(count == 0);
        }

        void OnRestartClicked()
        {
            if (_pendingSequence != null && _pendingSequence.IsActive())
            {
                _pendingSequence.Kill();
                _pendingSequence = null;
            }

            GameUIManager.Instance?.OnWinReplayRequested();
        }

        void OnNextClicked()
        {
            if (_pendingSequence != null && _pendingSequence.IsActive())
                return;

            // Coin fly animation disabled (no CoinPool in the scene → it logged an error). Proceed directly.
            // if (_coinFly != null && _totalCoinTarget != null && _coinText != null && _totalCoinText != null)
            // {
            //     long startAmount = 0;
            //     long.TryParse(_totalCoinText.text, out startAmount);
            //     _pendingSequence = _coinFly.AnimateAdd(_totalCoinText, _coinText.rectTransform, _totalCoinTarget, startAmount, _pendingReward);
            //     if (_pendingSequence != null)
            //     {
            //         _pendingSequence.SetAutoKill(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy).OnComplete(() =>
            //         {
            //             GameUIManager.Instance?.ProceedNextLevelAfterReward(_pendingReward);
            //         });
            //         return;
            //     }
            // }

            GameUIManager.Instance?.ProceedNextLevelAfterReward(_pendingReward);
        }
    }
}
