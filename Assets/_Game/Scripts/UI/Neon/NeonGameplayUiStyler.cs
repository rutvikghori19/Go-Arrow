using System;
using _Game.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    /// <summary>Editor-time prefab builder only. Runtime UI comes from prefabs.</summary>
    public static class NeonGameplayUiStyler
    {
        public static void Apply(GameUIManager manager)
        {
            if (manager == null)
                return;

            NeonHudBuilder.Apply(manager.GetComponentInChildren<HudPanel>(true), respectPrefabLayout: true);
            StyleWin(manager.GetComponentInChildren<WinPanel>(true));
            StyleFail(manager.GetComponentInChildren<FailPanel>(true));
            StyleRetry(manager.GetComponentInChildren<RetryPanel>(true));
        }

        public static void StyleWin(WinPanel win)
        {
            if (win == null)
                return;

            EnsureOverlay(win.transform);
            var panel = EnsureNeonDialog(win.transform, new Vector2(860f, 900f), NeonTheme.UiSuccess, "WinPanelNeon");
            BuildNeonDialogContent(win.transform, panel);
            ReparentDecorations(win.transform, panel);
            RemoveDialogButton(panel, "LevelSelectButton");

            EnsureText(panel, "Title", "LEVEL CLEAR!", 72f, NeonTheme.UiSuccess, new Vector2(0f, 320f), new Vector2(760f, 90f), true);
            StyleCoinLabels(win.transform);

            CreateDialogButton(panel, "RestartButtonNeon", "LEVEL RESTART", new Vector2(0f, -40f), new Vector2(620f, 96f),
                NeonTheme.UiMagentaBorder, Color.white, null);

            CreateDialogButton(panel, "NextButtonNeon", "NEXT LEVEL", new Vector2(0f, -200f), new Vector2(620f, 96f),
                NeonTheme.UiCyanBorder, Color.white, null);
        }

        public static void StyleFail(FailPanel fail)
        {
            if (fail == null)
                return;

            EnsureOverlay(fail.transform);
            var panel = EnsureNeonDialog(fail.transform, new Vector2(860f, 900f), NeonTheme.UiFail, "FailPanelNeon");
            BuildNeonDialogContent(fail.transform, panel);
            RemoveDialogButton(panel, "LevelSelectButton");

            EnsureText(panel, "HeartIcon", "\u2665", 72f, NeonTheme.UiFail, new Vector2(0f, 300f), new Vector2(100f, 80f));
            EnsureText(panel, "Title", "OUT OF LIVES", 64f, NeonTheme.UiFail, new Vector2(0f, 210f), new Vector2(760f, 80f), true);
            EnsureText(panel, "Subtitle", "No more lives left. Try again!", 34f, NeonTheme.UiText, new Vector2(0f, 130f), new Vector2(760f, 60f));

            CreateDialogButton(panel, "RestartButtonNeon", "RETRY", new Vector2(0f, -40f), new Vector2(620f, 96f),
                NeonTheme.UiFail, Color.white, null);

            CreateDialogButton(panel, "HomeButton", "MAIN MENU", new Vector2(0f, -160f), new Vector2(620f, 96f),
                NeonTheme.UiMagentaBorder, NeonTheme.UiText, null);
        }

        public static void StyleRetry(RetryPanel retry)
        {
            if (retry == null)
                return;

            EnsureOverlay(retry.transform);
            var panel = EnsureNeonDialog(retry.transform, new Vector2(760f, 560f), NeonTheme.UiCyanBorder, "RetryPanelNeon");
            BuildNeonDialogContent(retry.transform, panel);

            EnsureText(panel, "WarningIcon", "!", 64f, NeonTheme.UiFail, new Vector2(0f, 150f), new Vector2(80f, 80f));
            EnsureText(panel, "Title", "Restart level?", 48f, NeonTheme.UiHudText, new Vector2(0f, 60f), new Vector2(700f, 70f), true);

            CreateDialogButton(panel, "YesButtonNeon", "YES", new Vector2(0f, -70f), new Vector2(620f, 96f),
                NeonTheme.UiMagentaBorder, Color.white, null);

            CreateDialogButton(panel, "NoButtonNeon", "NO", new Vector2(0f, -190f), new Vector2(620f, 96f),
                NeonTheme.UiCyanBorder, NeonTheme.UiHudText, null);
        }

        static void BuildNeonDialogContent(Transform root, RectTransform panel, RectTransform keepPanel = null)
        {
            HideLegacyContent(root, keepPanel ?? panel);
        }

        static void EnsureOverlay(Transform root)
        {
            var bg = root.GetComponent<Image>();
            if (bg == null)
                bg = root.gameObject.AddComponent<Image>();
            NeonUiBuilder.StyleScreenOverlay(bg);
        }

        static RectTransform EnsureNeonDialog(Transform root, Vector2 size, Color border, string name)
        {
            var existing = root.Find(name) as RectTransform;
            if (existing != null)
                return existing;

            return NeonUiBuilder.CreateNeonPanel(root, size, NeonTheme.UiPanel, border, name);
        }

        static void EnsureText(
            Transform panel,
            string name,
            string text,
            float fontSize,
            Color color,
            Vector2 position,
            Vector2 size,
            bool title = false)
        {
            if (panel.Find(name) != null)
                return;

            NeonUiBuilder.CreatePositionedText(panel, text, fontSize, color, position, size, TextAlignmentOptions.Center, name, title);
        }

        static void RemoveDialogButton(Transform panel, string name)
        {
            var button = panel.Find(name);
            if (button != null)
                UnityEngine.Object.Destroy(button.gameObject);
        }

        static void HideLegacyContent(Transform root, Transform keepPanel)
        {
            foreach (Transform child in root)
            {
                if (keepPanel != null && child == keepPanel)
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        static void ReparentDecorations(Transform root, Transform panel)
        {
            foreach (var star in root.GetComponentsInChildren<Transform>(true))
            {
                if (!star.name.Contains("Star") && !star.name.Contains("star"))
                    continue;

                if (star == panel || star.IsChildOf(panel))
                    continue;

                star.SetParent(panel, false);
                star.gameObject.SetActive(true);
            }

            foreach (var coin in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!coin.name.Contains("Coin") && !coin.name.Contains("coin"))
                    continue;

                if (coin.transform.IsChildOf(panel))
                    continue;

                coin.transform.SetParent(panel, false);
                coin.gameObject.SetActive(true);
            }
        }

        static void StyleCoinLabels(Transform root)
        {
            foreach (var coin in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!coin.name.Contains("Coin") && !coin.name.Contains("coin"))
                    continue;

                if (int.TryParse(coin.text, out int value) && value > 0)
                    coin.text = $"+{value} coins";

                coin.color = NeonTheme.UiHudText;
                coin.fontStyle = FontStyles.Bold;
            }
        }

        static void CreateDialogButton(
            Transform panel,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            Color border,
            Color textColor,
            Action onClick)
        {
            if (panel.Find(name) != null)
                return;

            var btn = NeonUiBuilder.CreateNeonButton(panel, label, size, border, NeonTheme.UiPanel, textColor, onClick, name);
            btn.GetComponent<RectTransform>().anchoredPosition = position;
        }
    }
}
