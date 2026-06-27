using _Game.Theme;

using SerapKeremGameKit._UI;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace _Game.UI

{

    public sealed class MainMenuPanel : UIPanel

    {

        TextMeshProUGUI _levelText;

        GameUIManager _ui;



        public void Initialize(GameUIManager ui)

        {

            _ui = ui;

            EnsureHierarchy();

        }



        void EnsureHierarchy()

        {

            var root = transform as RectTransform;

            if (root == null)

                return;



            NeonUiBuilder.Stretch(root);

            if (GetComponent<Image>() == null)

            {

                var bg = gameObject.AddComponent<Image>();

                bg.color = NeonTheme.CameraClear;

                bg.raycastTarget = true;

            }



            if (canvasGroup == null)

                canvasGroup = NeonUiBuilder.EnsureCanvasGroup(gameObject);



            if (transform.Find("PlayButton") != null)

            {

                RefreshExistingLayout();

                return;

            }



            GoArrowBranding.CreateLogoImage(transform, new Vector2(0f, NeonUiLayout.MainMenuLogoY), new Vector2(920f, 340f));



            PlaceMenuButton("PLAY", NeonUiLayout.MainMenuPlayY, NeonTheme.UiCyanBorder, Color.white, () => _ui?.OnPlayRequested(), "PlayButton");

            PlaceMenuButton("SETTINGS", NeonUiLayout.MainMenuSettingsY, NeonTheme.UiMagentaBorder, NeonTheme.UiText, () => _ui?.OnOpenSettings(), "SettingsButton");



            _levelText = NeonUiBuilder.CreatePositionedText(

                transform,

                string.Empty,

                34f,

                NeonTheme.UiHudText,

                new Vector2(0f, NeonUiLayout.MainMenuLevelLabelY),

                new Vector2(700f, 60f),

                TextAlignmentOptions.Center,

                "LevelLabel");

            RefreshLevelLabel();

        }



        void PlaceMenuButton(string label, float y, Color border, Color textColor, System.Action onClick, string name)

        {

            var btn = NeonUiBuilder.CreateNeonButton(transform, label, new Vector2(620f, 110f), border, NeonTheme.UiPanel, textColor, onClick, name);

            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);

        }



        void RefreshExistingLayout()

        {

            _levelText = transform.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();



            var levelsButton = transform.Find("LevelsButton");

            if (levelsButton != null)

                levelsButton.gameObject.SetActive(false);



            var logo = transform.Find("Logo");

            if (logo != null)

            {

                var logoRt = logo as RectTransform;

                logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 0.5f);

                logoRt.anchoredPosition = new Vector2(0f, NeonUiLayout.MainMenuLogoY);

                logoRt.sizeDelta = new Vector2(920f, 340f);

                GoArrowBranding.ApplyLogo(logo.GetComponent<Image>());

            }

            else

            {

                GoArrowBranding.CreateLogoImage(transform, new Vector2(0f, NeonUiLayout.MainMenuLogoY), new Vector2(920f, 340f));

            }



            SetButtonPosition(transform, "PlayButton", NeonUiLayout.MainMenuPlayY);

            SetButtonPosition(transform, "SettingsButton", NeonUiLayout.MainMenuSettingsY);



            if (_levelText != null)

            {

                var labelRt = _levelText.rectTransform;

                labelRt.anchoredPosition = new Vector2(0f, NeonUiLayout.MainMenuLevelLabelY);

                NeonUiTypography.ApplyBody(_levelText, NeonTheme.UiHudText, 34f, FontStyles.Bold);

            }



            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))

                NeonUiTypography.ApplyButton(tmp, tmp.color, tmp.fontSize > 0f ? tmp.fontSize : 36f);

        }



        static void SetButtonPosition(Transform root, string name, float y)

        {

            var btn = root.Find(name);

            if (btn == null)

                return;



            var rt = btn as RectTransform;

            if (rt != null)

                rt.anchoredPosition = new Vector2(0f, y);

        }



        public override void Show(bool playSound = true)

        {

            EnsureHierarchy();

            RefreshLevelLabel();

            base.Show(playSound);

        }



        void RefreshLevelLabel()

        {

            if (_levelText == null)

                return;



            int level = LevelProgress.ActiveLevelNumber;

            _levelText.text = $"Playing Level {level}";

        }

    }

}


