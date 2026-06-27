using System;
using UnityEngine;
using SerapKeremGameKit._Singletons;

namespace _Game.UI
{
    public class LivesManager : MonoSingleton<LivesManager>
    {
        [Header("Lives Settings")]
        [SerializeField] private int _maxLives = 3;

        private int _currentLives;
        private int _lastLifeLossFrame = -1;
        private bool _lifeLossEnabled;

        public int CurrentLives => _currentLives;
        public int MaxLivesCount => _maxLives;
        public int LivesLostCount => Mathf.Max(0, _maxLives - _currentLives);
        public bool CanLoseLife => _lifeLossEnabled && _currentLives > 0;

        public event Action<int> OnLivesChanged;
        public event Action OnLivesDepleted;

        public void Initialize()
        {
            BeginLevel();
        }

        public void BeginLevel()
        {
            _lifeLossEnabled = false;
            ResetLives();
        }

        public void EnableLifeLoss()
        {
            _lifeLossEnabled = true;
            RefreshDisplay();
        }

        public void ResetLives()
        {
            _currentLives = _maxLives;
            _lastLifeLossFrame = -1;
            OnLivesChanged?.Invoke(_currentLives);
        }

        public void RefreshDisplay()
        {
            OnLivesChanged?.Invoke(_currentLives);
        }

        public void LoseLife()
        {
            if (!_lifeLossEnabled)
                return;

            int currentFrame = Time.frameCount;

            if (_lastLifeLossFrame == currentFrame)
                return;

            if (_currentLives <= 0)
                return;

            _lastLifeLossFrame = currentFrame;
            _currentLives--;
            OnLivesChanged?.Invoke(_currentLives);

            if (_currentLives <= 0)
                OnLivesDepleted?.Invoke();
        }

        public bool HasLivesRemaining()
        {
            return _currentLives > 0;
        }
    }
}
