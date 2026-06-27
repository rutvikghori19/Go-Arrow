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
        [SerializeField] private GameUIManager _uiRoot;

		private void Awake()
		{
			if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			// Auto-unsubscribe handled by ButtonExtensions
		}

        public void Setup(int rewardedCoins, GameUIManager uiRoot)
        {
            if (_coinText != null) _coinText.text = rewardedCoins.ToString();
            _uiRoot = uiRoot;
        }

        private void OnRestartClicked()
        {
			if (_uiRoot != null) _uiRoot.OnRestartConfirmed();
        }

		public void SetUIRoot(GameUIManager uiRoot)
		{
			_uiRoot = uiRoot;
		}
    }
}



