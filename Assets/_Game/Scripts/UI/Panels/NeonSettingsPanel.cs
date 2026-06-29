using _Game.Theme;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Economy;
using SerapKeremGameKit._Haptics;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._UI;
using SerapKeremGameKit._Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public sealed class NeonSettingsPanel : UIPanel
    {
        static readonly string[] OnMarkNames = { "Check", "RightMark", "Right", "RightMarkIcon" };
        static readonly string[] CrossNames = { "Cross", "WrongMark", "X" };
        static readonly string[] ClearDataButtonNames = { "Clear Data", "ClearDataButton", "ClearData" };

        const string SoundKey = PreferencesKeys.SettingsSound;
        const string HapticKey = PreferencesKeys.SettingsHaptic;

        Toggle _soundToggle;
        Toggle _hapticToggle;
        GameObject _soundOnMark;
        GameObject _soundCross;
        GameObject _hapticOnMark;
        GameObject _hapticCross;
        Button _closeButton;
        Button _mainMenuButton;
        Button _clearDataButton;
        RectTransform _panel;

        public override void Show(bool playSound = true)
        {
            MigrateLegacyPanelIfNeeded();
            EnsurePanel();
            EnsureWired();
            RefreshFromPrefs();
            RefreshContextButtons();
            base.Show(playSound);
        }

        public void EnsureWired()
        {
            ClearResolvedReferences();
            EnsurePanel();
            if (_panel == null)
                return;

            ResolveReferences();
            PreparePrefabPanelRaycasts();
            WireButtons();
            WireToggles();
        }

        void EnsurePanel()
        {
            _panel = transform.Find("Panel") as RectTransform;
            if (_panel != null)
                return;

            EnsureNeonHierarchy();
        }

        void ClearResolvedReferences()
        {
            _soundToggle = null;
            _hapticToggle = null;
            _soundOnMark = null;
            _soundCross = null;
            _hapticOnMark = null;
            _hapticCross = null;
            _closeButton = null;
            _mainMenuButton = null;
            _clearDataButton = null;
        }

        void ResolveReferences()
        {
            if (_panel == null)
                return;

            _soundToggle = FindRowToggle(_panel, "Row_SOUND");
            _hapticToggle = FindRowToggle(_panel, "Row_HAPTICS");

            _soundOnMark = FindOnMark(_panel, "Row_SOUND");
            _soundCross = FindCross(_panel, "Row_SOUND");
            _hapticOnMark = FindOnMark(_panel, "Row_HAPTICS");
            _hapticCross = FindCross(_panel, "Row_HAPTICS");

            _closeButton = _panel.Find("CloseButton")?.GetComponent<Button>();
            _mainMenuButton = _panel.Find("MainMenuButton")?.GetComponent<Button>();
            _clearDataButton = FindButtonByNames(_panel, ClearDataButtonNames);
        }

        static Toggle FindRowToggle(Transform panel, string rowName)
        {
            var row = panel.Find(rowName);
            if (row == null)
                return null;

            var box = row.Find("Box");
            if (box != null)
            {
                var boxToggle = box.GetComponent<Toggle>();
                if (boxToggle != null)
                    return boxToggle;
            }

            return row.GetComponent<Toggle>();
        }

        static GameObject FindOnMark(Transform panel, string rowName)
        {
            var box = panel.Find($"{rowName}/Box");
            if (box == null)
                return null;

            foreach (string name in OnMarkNames)
            {
                var mark = box.Find(name);
                if (mark != null)
                    return mark.gameObject;
            }

            return null;
        }

        static GameObject FindCross(Transform panel, string rowName)
        {
            var box = panel.Find($"{rowName}/Box");
            if (box == null)
                return null;

            foreach (string name in CrossNames)
            {
                var cross = box.Find(name);
                if (cross != null)
                    return cross.gameObject;
            }

            return null;
        }

        static Button FindButtonByNames(Transform panel, string[] names)
        {
            foreach (string name in names)
            {
                var button = panel.Find(name)?.GetComponent<Button>();
                if (button != null)
                    return button;
            }

            return null;
        }

        void PreparePrefabPanelRaycasts()
        {
            if (_panel == null)
                return;

            DisableRaycast(_panel.GetComponent<Image>());

            var inner = _panel.Find("Inner");
            if (inner != null)
                DisableRaycast(inner.GetComponent<Image>());

            DisableRowDecorRaycasts(_panel, "Row_SOUND");
            DisableRowDecorRaycasts(_panel, "Row_HAPTICS");
        }

        static void DisableRowDecorRaycasts(Transform panel, string rowName)
        {
            var row = panel.Find(rowName);
            if (row == null)
                return;

            var icon = row.Find("Icon");
            if (icon != null)
                DisableRaycast(icon.GetComponent<Image>());

            var label = row.Find("Label");
            if (label != null)
                DisableRaycast(label.GetComponent<Graphic>());

            var box = row.Find("Box");
            if (box == null)
                return;

            foreach (string name in OnMarkNames)
            {
                var mark = box.Find(name);
                if (mark != null)
                    DisableRaycast(mark.GetComponent<Graphic>());
            }

            foreach (string name in CrossNames)
            {
                var cross = box.Find(name);
                if (cross != null)
                    DisableRaycast(cross.GetComponent<Graphic>());
            }
        }

        static void DisableRaycast(Graphic graphic)
        {
            if (graphic != null)
                graphic.raycastTarget = false;
        }

        void WireButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.BindOnClick(this, () => Hide(false));
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveAllListeners();
                _mainMenuButton.BindOnClick(this, () =>
                {
                    Hide(false);
                    GameUIManager.Instance?.OnReturnToMainMenu();
                });
            }

            if (_clearDataButton != null)
            {
                _clearDataButton.onClick.RemoveAllListeners();
                _clearDataButton.BindOnClick(this, OnClearDataClicked);
            }
        }

        void WireToggles()
        {
            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.RemoveAllListeners();
                _soundToggle.onValueChanged.AddListener(value =>
                {
                    ApplyToggleVisual(_soundOnMark, _soundCross, value);
                    OnSoundChanged(value);
                });
            }

            if (_hapticToggle != null)
            {
                _hapticToggle.onValueChanged.RemoveAllListeners();
                _hapticToggle.onValueChanged.AddListener(value =>
                {
                    ApplyToggleVisual(_hapticOnMark, _hapticCross, value);
                    OnHapticChanged(value);
                });
            }
        }

        void RefreshFromPrefs()
        {
            bool soundOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            bool hapticOn = PlayerPrefs.GetInt(HapticKey, 1) == 1;

            if (_soundToggle != null)
            {
                _soundToggle.SetIsOnWithoutNotify(soundOn);
                ApplyToggleVisual(_soundOnMark, _soundCross, soundOn);
            }

            if (_hapticToggle != null)
            {
                _hapticToggle.SetIsOnWithoutNotify(hapticOn);
                ApplyToggleVisual(_hapticOnMark, _hapticCross, hapticOn);
            }
        }

        void RefreshContextButtons()
        {
            bool isMainMenu = GameUIManager.Instance != null && GameUIManager.Instance.IsMainMenuContext;

            if (_mainMenuButton != null)
                _mainMenuButton.gameObject.SetActive(!isMainMenu);

            if (_clearDataButton != null)
                _clearDataButton.gameObject.SetActive(isMainMenu);
        }

        void OnClearDataClicked()
        {
            ClearAllGameData();
            RefreshFromPrefs();
            RefreshMainMenuAfterClear();
        }

        static void ClearAllGameData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            GameSessionBootstrap.ResetForMenu();

            if (CurrencyWallet.IsInitialized)
                CurrencyWallet.Instance.Load();

            if (AudioManager.IsInitialized)
                AudioManager.Instance.SetEnabled(true);

            if (HapticManager.IsInitialized)
                HapticManager.Instance.SetEnabled(true);

            if (LivesManager.IsInitialized)
                LivesManager.Instance.ResetLives();

            if (LevelManager.IsInitialized)
                LevelManager.Instance.ActiveLevelNumber = 1;
        }

        void RefreshMainMenuAfterClear()
        {
            if (GameUIManager.Instance == null || !GameUIManager.Instance.IsMainMenuContext)
                return;

            var menuPanel = GameUIManager.Instance.GetComponentInChildren<MainMenuPanel>(true);
            menuPanel?.RefreshDisplay();
        }

        static void ApplyToggleVisual(GameObject onMark, GameObject cross, bool isOn)
        {
            if (onMark != null)
                onMark.SetActive(isOn);
            if (cross != null)
                cross.SetActive(!isOn);
        }

        void MigrateLegacyPanelIfNeeded()
        {
            var panel = transform.Find("Panel");
            if (panel == null)
                return;

            if (panel.Find("Row_SOUND/Toggle/Knob") == null)
                return;

            Destroy(panel.gameObject);
            _panel = null;
            ClearResolvedReferences();
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

            _soundOnMark = _panel.Find("Row_SOUND/Box/Check")?.gameObject;
            _soundCross = _panel.Find("Row_SOUND/Box/Cross")?.gameObject;
            _hapticOnMark = _panel.Find("Row_HAPTICS/Box/Check")?.gameObject;
            _hapticCross = _panel.Find("Row_HAPTICS/Box/Cross")?.gameObject;

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
