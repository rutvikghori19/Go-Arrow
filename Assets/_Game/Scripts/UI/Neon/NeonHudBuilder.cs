using _Game.Theme;
using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public static class NeonHudBuilder
    {
        const string AppliedKey = "NeonHudApplied";
        const float HeaderButtonSize = 88f;

        public static void Apply(HUDPanel hud)
        {
            if (hud == null)
                return;

            var root = hud.transform as RectTransform;
            if (root == null)
                return;

            var header = FindChild(root, "Header");
            if (header == null)
                return;

            RemoveLegacyBar(header);

            if (header.Find(AppliedKey) == null)
                new GameObject(AppliedKey).transform.SetParent(header, false);

            CompactHeader(header);

            float rowY = -NeonUiLayout.HudHeight * 0.5f;
            HideLegacyButton(header, "RestartButton");
            HideLegacyButton(header, "SettingsButton");

            EnsureHudIconButton(
                header,
                "NeonRestartBtn",
                NeonTheme.UiCyanBorder,
                "\u21BB",
                new Vector2(64f, rowY),
                hud.PressRestart);

            EnsureHudIconButton(
                header,
                "NeonSettingsBtn",
                NeonTheme.UiMagentaBorder,
                "\u2699",
                new Vector2(176f, rowY),
                hud.PressSettings);

            LayoutLevelAndTime(header);
            CenterHeartPanel(header, rowY);
        }

        static void EnsureHudIconButton(
            Transform header,
            string name,
            Color borderColor,
            string icon,
            Vector2 position,
            System.Action onClick)
        {
            var existing = header.Find(name);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                var rt = existing as RectTransform;
                rt.anchoredPosition = position;
                rt.sizeDelta = new Vector2(HeaderButtonSize, HeaderButtonSize);

                var btn = existing.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => onClick?.Invoke());
                }

                return;
            }

            NeonUiBuilder.CreateNeonHudIconButton(header, name, borderColor, icon, HeaderButtonSize, position, onClick);
        }

        static void HideLegacyButton(Transform header, string legacyName)
        {
            var legacy = FindChild(header, legacyName);
            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }

        static void RemoveLegacyBar(Transform header)
        {
            var oldBar = header.Find("NeonHudBar");
            if (oldBar != null)
                UnityEngine.Object.Destroy(oldBar.gameObject);
        }

        static void CompactHeader(Transform header)
        {
            var headerRt = header as RectTransform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, NeonUiLayout.HudHeight);
        }

        static void LayoutLevelAndTime(Transform header)
        {
            var timer = FindChild(header, "Timer");
            if (timer != null)
                timer.gameObject.SetActive(false);

            var levelDisplayer = FindChild(header, "LevelDisplayer");
            if (levelDisplayer != null)
            {
                levelDisplayer.gameObject.SetActive(true);

                var bg = levelDisplayer.GetComponent<Image>();
                if (bg != null)
                    bg.enabled = false;

                var rt = levelDisplayer as RectTransform;
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-24f, -20f);
                rt.sizeDelta = new Vector2(220f, 48f);

                var pill = levelDisplayer.Find("LevelPill");
                if (pill == null)
                {
                    var pillRt = NeonUiBuilder.CreateNeonPill(levelDisplayer, new Vector2(200f, 44f), NeonTheme.UiCyanBorder, "LevelPill");
                    pillRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pillRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pillRt.anchoredPosition = Vector2.zero;
                }

                var levelText = FindChild(levelDisplayer, "LevelText")?.GetComponent<TextMeshProUGUI>();
                if (levelText != null)
                {
                    levelText.transform.SetAsLastSibling();
                    NeonUiTypography.ApplyBody(levelText, NeonTheme.UiHudText, 28f, FontStyles.Bold);
                    levelText.alignment = TextAlignmentOptions.Center;
                }
            }

            if (timer == null)
                return;

            var timeText = FindChild(timer, "Time_text")?.GetComponent<TextMeshProUGUI>();
            if (timeText == null)
                return;

            timeText.transform.SetParent(header, false);
            var timeRt = timeText.rectTransform;
            timeRt.anchorMin = new Vector2(1f, 1f);
            timeRt.anchorMax = new Vector2(1f, 1f);
            timeRt.pivot = new Vector2(1f, 1f);
            timeRt.anchoredPosition = new Vector2(-24f, -64f);
            timeRt.sizeDelta = new Vector2(180f, 44f);
            timeText.gameObject.SetActive(true);
            NeonUiTypography.ApplyBody(timeText, NeonTheme.UiHudText, 36f, FontStyles.Bold);
            timeText.alignment = TextAlignmentOptions.Right;
        }

        static void CenterHeartPanel(Transform header, float rowY)
        {
            var heartPanel = UnityEngine.Object.FindFirstObjectByType<HeartPanel>();
            if (heartPanel == null)
                return;

            var hrt = heartPanel.GetComponent<RectTransform>();
            hrt.SetParent(header, false);
            hrt.anchorMin = new Vector2(0.5f, 1f);
            hrt.anchorMax = new Vector2(0.5f, 1f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0f, rowY);
            hrt.sizeDelta = new Vector2(280f, 72f);

            var bg = heartPanel.GetComponent<Image>();
            if (bg != null)
                bg.enabled = false;

            var heartRow = hrt.Find("Heart") as RectTransform;
            if (heartRow == null)
                return;

            heartRow.anchorMin = new Vector2(0.5f, 0.5f);
            heartRow.anchorMax = new Vector2(0.5f, 0.5f);
            heartRow.pivot = new Vector2(0.5f, 0.5f);
            heartRow.anchoredPosition = Vector2.zero;
            heartRow.sizeDelta = new Vector2(240f, 60f);

            var layout = heartRow.GetComponent<HorizontalLayoutGroup>() ?? heartRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }

            return null;
        }
    }
}
