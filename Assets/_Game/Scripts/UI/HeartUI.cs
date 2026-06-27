using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class HeartUI : MonoBehaviour
    {
        [Header("Heart Sprites")]
        [SerializeField] private Sprite _redHeartSprite;
        [SerializeField] private Sprite _grayHeartSprite;

        [Header("References")]
        [SerializeField] private Image _heartImage;

        bool _isInitialized;

        public void SetActive(bool active)
        {
            if (_heartImage == null)
                return;

            if (active && _redHeartSprite != null)
                _heartImage.sprite = _redHeartSprite;
            else if (!active && _grayHeartSprite != null)
                _heartImage.sprite = _grayHeartSprite;
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (_heartImage == null)
                _heartImage = GetComponent<Image>();

            if (_heartImage == null)
            {
                Debug.LogWarning($"{name}: Image reference is missing for heart display.", this);
                return;
            }

            _heartImage.raycastTarget = false;
            SetActive(true);
            _isInitialized = true;
        }
    }
}
