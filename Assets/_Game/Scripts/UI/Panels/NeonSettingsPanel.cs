using _Game.Theme;

using SerapKeremGameKit._Audio;

using SerapKeremGameKit._Haptics;

using SerapKeremGameKit._UI;

using SerapKeremGameKit._Utilities;

using UnityEngine;

using UnityEngine.UI;



namespace _Game.UI

{

    public sealed class NeonSettingsPanel : UIPanel

    {

        const string SoundKey = PreferencesKeys.SettingsSound;

        const string HapticKey = PreferencesKeys.SettingsHaptic;



        Toggle _soundToggle;

        Toggle _hapticToggle;

        Button _mainMenuButton;

        RectTransform _panel;



        public override void Show(bool playSound = true)

        {

            MigrateLegacyPanelIfNeeded();

            EnsureNeonHierarchy();

            if (_soundToggle != null)

                _soundToggle.isOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;

            if (_hapticToggle != null)

                _hapticToggle.isOn = PlayerPrefs.GetInt(HapticKey, 1) == 1;



            bool showMainMenu = GameUIManager.Instance != null && !GameUIManager.Instance.IsMainMenuContext;

            if (_mainMenuButton != null)

                _mainMenuButton.gameObject.SetActive(showMainMenu);



            base.Show(playSound);

        }



        void MigrateLegacyPanelIfNeeded()

        {

            var panel = transform.Find("Panel");

            if (panel == null)

                return;



            if (panel.Find("Row_SOUND/Toggle/Knob") != null)

            {

                Destroy(panel.gameObject);

                _panel = null;

                _soundToggle = null;

                _hapticToggle = null;

                _mainMenuButton = null;

            }

        }



        void EnsureNeonHierarchy()

        {

            if (_panel != null)

                return;



            var root = transform as RectTransform;

            NeonUiBuilder.Stretch(root);



            if (GetComponent<Image>() == null)

            {

                var dim = gameObject.AddComponent<Image>();

                dim.color = new Color(0f, 0f, 0f, 0.78f);

            }



            if (canvasGroup == null)

                canvasGroup = NeonUiBuilder.EnsureCanvasGroup(gameObject);



            _panel = NeonUiBuilder.CreateNeonPanel(transform, new Vector2(820f, 700f), NeonTheme.UiPanel, NeonTheme.UiCyanBorder, "Panel");



            NeonUiBuilder.CreatePositionedText(_panel, "SETTINGS", 52f, NeonTheme.UiHudText, new Vector2(0f, 250f), new Vector2(700f, 80f), TMPro.TextAlignmentOptions.Center, "Title", useTitleFont: true);



            var close = NeonUiBuilder.CreateIconButton(_panel, "X", new Vector2(72f, 72f), NeonTheme.UiMagentaBorder, () => Hide(false), "CloseButton");

            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(350f, 250f);



            _soundToggle = NeonUiBuilder.CreateNeonCheckToggle(_panel, "SOUND", new Vector2(0f, 60f), PlayerPrefs.GetInt(SoundKey, 1) == 1, OnSoundChanged);

            _hapticToggle = NeonUiBuilder.CreateNeonCheckToggle(_panel, "HAPTICS", new Vector2(0f, -50f), PlayerPrefs.GetInt(HapticKey, 1) == 1, OnHapticChanged);



            _mainMenuButton = NeonUiBuilder.CreateNeonButton(

                _panel,

                "MAIN MENU",

                new Vector2(620f, 96f),

                NeonTheme.UiMagentaBorder,

                NeonTheme.UiPanel,

                NeonTheme.UiText,

                () =>

                {

                    Hide(false);

                    GameUIManager.Instance?.OnReturnToMainMenu();

                },

                "MainMenuButton");

            _mainMenuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -220f);

            _mainMenuButton.gameObject.SetActive(false);

        }



        static void OnSoundChanged(bool value)

        {

            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);

            PlayerPrefs.Save();

            if (AudioManager.IsInitialized)

                AudioManager.Instance.SetEnabled(value);

        }



        static void OnHapticChanged(bool value)

        {

            PlayerPrefs.SetInt(HapticKey, value ? 1 : 0);

            PlayerPrefs.Save();

            if (HapticManager.IsInitialized)

                HapticManager.Instance.SetEnabled(value);

        }

    }

}


