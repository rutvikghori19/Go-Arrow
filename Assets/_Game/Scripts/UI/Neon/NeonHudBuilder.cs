using _Game.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public static class NeonHudBuilder
    {
        const string AppliedKey = "NeonHudApplied";
        const float HeaderButtonSize = 88f;

        public static void Apply(HudPanel hud, bool respectPrefabLayout = false)
        {
            if (hud == null)
                return;

            var root = hud.transform as RectTransform;
            if (root == null)
                return;

            var header = FindChild(root, "Header");
            var layoutRoot = header != null ? header : root;

            RemoveLegacyBar(layoutRoot);

            if (layoutRoot.Find(AppliedKey) == null)
                new GameObject(AppliedKey).transform.SetParent(layoutRoot, false);

            if (header != null && !respectPrefabLayout)
                CompactHeader(header);

            float rowY = -NeonUiLayout.HudHeight * 0.5f;
            HideLegacyButton(layoutRoot, "RestartButton");
            HideLegacyButton(layoutRoot, "SettingsButton");

            EnsureHudIconButton(
                layoutRoot,
                "NeonRestartBtn",
                NeonTheme.UiCyanBorder,
                "\u21BB",
                new Vector2(64f, rowY),
                () => GameUIManager.Instance?.OnRestartRequested(),
                respectPrefabLayout);

            EnsureHudIconButton(
                layoutRoot,
                "NeonSettingsBtn",
                NeonTheme.UiMagentaBorder,
                "\u2699",
                new Vector2(176f, rowY),
                () => GameUIManager.Instance?.OnOpenSettings(),
                respectPrefabLayout);

            LayoutLevelAndTime(root, layoutRoot, respectPrefabLayout);
            LayoutHeartPanel(root, layoutRoot, rowY, respectPrefabLayout);
            EnsureHudElementsVisible(root);
        }

        static void EnsureHudElementsVisible(Transform hudRoot)
        {
            foreach (var name in new[] { "Header", "LevelDisplayer", "LevelText", "HeartPanel" })
            {
                var child = FindChild(hudRoot, name);
                if (child != null)
                    child.gameObject.SetActive(true);
            }
        }

        static void LayoutHeartPanel(Transform searchRoot, Transform layoutRoot, float rowY, bool respectPrefabLayout)
        {
            var heartPanel = searchRoot.GetComponentInChildren<HeartPanel>(true);
            if (heartPanel == null)
                return;

            if (!respectPrefabLayout)
            {
                var panelRt = heartPanel.GetComponent<RectTransform>();
                if (panelRt.parent != layoutRoot)
                    panelRt.SetParent(layoutRoot, false);

                panelRt.anchorMin = new Vector2(0.5f, 1f);
                panelRt.anchorMax = new Vector2(0.5f, 1f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.anchoredPosition = new Vector2(0f, rowY);
                panelRt.sizeDelta = new Vector2(280f, 72f);
            }

            var bg = heartPanel.GetComponent<Image>();
            if (bg != null)
                bg.enabled = false;
        }

        static void EnsureHudIconButton(
            Transform header,
            string name,
            Color borderColor,
            string icon,
            Vector2 position,
            System.Action onClick,
            bool respectPrefabLayout)
        {
            var existing = header.Find(name);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);

                if (!respectPrefabLayout)
                {
                    var rt = existing as RectTransform;
                    rt.anchoredPosition = position;
                    rt.sizeDelta = new Vector2(HeaderButtonSize, HeaderButtonSize);
                }

                var btn = existing.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => onClick?.Invoke());
                }

                return;
            }

            if (respectPrefabLayout)
                return;

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

        static void LayoutLevelAndTime(Transform searchRoot, Transform layoutRoot, bool respectPrefabLayout)
        {
            var timer = FindChild(searchRoot, "Timer");
            if (timer != null)
                timer.gameObject.SetActive(false);

            var levelDisplayer = FindChild(searchRoot, "LevelDisplayer");
            if (levelDisplayer != null)
            {
                levelDisplayer.gameObject.SetActive(true);

                if (!respectPrefabLayout)
                {
                    if (levelDisplayer.parent != layoutRoot && levelDisplayer.IsChildOf(searchRoot))
                        levelDisplayer.SetParent(layoutRoot, false);

                    var rt = levelDisplayer as RectTransform;
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-24f, -20f);
                    rt.sizeDelta = new Vector2(220f, 48f);
                }

                var bg = levelDisplayer.GetComponent<Image>();
                if (bg != null)
                    bg.enabled = false;

                var pill = levelDisplayer.Find("LevelPill");
                if (!respectPrefabLayout && pill == null)
                {
                    var pillRt = NeonUiBuilder.CreateNeonPill(levelDisplayer, new Vector2(200f, 44f), NeonTheme.UiCyanBorder, "LevelPill");
                    pillRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pillRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pillRt.anchoredPosition = Vector2.zero;
                }

                var levelText = FindChild(levelDisplayer, "LevelText")?.GetComponent<TextMeshProUGUI>();
                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    if (!respectPrefabLayout)
                    {
                        levelText.transform.SetAsLastSibling();
                        NeonUiTypography.ApplyBody(levelText, NeonTheme.UiHudText, 28f, FontStyles.Bold);
                        levelText.alignment = TextAlignmentOptions.Center;
                    }
                }
            }

            if (timer == null || respectPrefabLayout)
                return;

            var timeText = FindChild(timer, "Time_text")?.GetComponent<TextMeshProUGUI>();
            if (timeText == null)
                return;

            timeText.transform.SetParent(layoutRoot, false);
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
