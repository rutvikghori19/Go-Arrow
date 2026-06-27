using System.Collections.Generic;
using System.Linq;
using _Game.Theme;
using UnityEngine;
using SerapKeremGameKit._UI;

namespace _Game.UI
{
    public class HeartPanel : MonoBehaviour
    {
        [Header("Heart References")]
        [SerializeField] private List<HeartUI> _hearts = new List<HeartUI>();

        bool _isInitialized;

        int MaxHearts
        {
            get
            {
                if (LivesManager.IsInitialized && LivesManager.Instance != null)
                    return LivesManager.Instance.MaxLivesCount;
                return 3;
            }
        }

        void Awake()
        {
            if (_hearts == null || _hearts.Count == 0)
                _hearts = GetComponentsInChildren<HeartUI>(true).ToList();

            TrimHeartSlots();
            ApplyNeonLayout();
        }

        void TrimHeartSlots()
        {
            int max = MaxHearts;
            for (int i = 0; i < _hearts.Count; i++)
            {
                if (_hearts[i] == null)
                    continue;

                bool visible = i < max;
                _hearts[i].gameObject.SetActive(visible);
            }

            _hearts = _hearts.Take(max).Where(h => h != null).ToList();
        }

        void ApplyNeonLayout()
        {
            var bg = GetComponent<UnityEngine.UI.Image>();
            if (bg != null)
                bg.enabled = false;
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            TrimHeartSlots();

            foreach (var heart in _hearts)
            {
                if (heart != null)
                    heart.Initialize();
            }

            _isInitialized = true;
        }

        public void UpdateHearts(int activeLives)
        {
            for (int i = 0; i < _hearts.Count; i++)
            {
                if (_hearts[i] != null)
                    _hearts[i].SetActive(i < activeLives);
            }
        }

        public void ResetHearts()
        {
            UpdateHearts(MaxHearts);
        }
    }
}
