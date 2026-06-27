using System;
using _Game.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Game.UI
{
    public static class NeonUiBuilder
    {
        public static EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return EventSystem.current;

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return esGo.GetComponent<EventSystem>();
        }

        public static Canvas CreateRootCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            NeonUiLayout.ConfigureCanvas(scaler);
            NeonUiLayout.ApplyPlayArea(go.GetComponent<RectTransform>());

            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static TextMeshProUGUI CreatePositionedText(
            Transform parent,
            string text,
            float fontSize,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            string name = "Text",
            bool useTitleFont = false)
        {
            var rt = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size);
            rt.anchoredPosition = anchoredPosition;

            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            if (useTitleFont)
                NeonUiTypography.ApplyTitle(tmp, color, fontSize);
            else
                NeonUiTypography.ApplyBody(tmp, color, fontSize, FontStyles.Bold);

            return tmp;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            string name = "Text")
        {
            return CreatePositionedText(parent, text, fontSize, color, Vector2.zero, new Vector2(400f, 80f), alignment, name);
        }

        public static RectTransform CreateNeonPanel(Transform parent, Vector2 size, Color fill, Color border, string name = "Panel")
        {
            var root = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size);
            var borderImg = root.gameObject.AddComponent<Image>();
            borderImg.color = border;

            var inner = CreateRect("Inner", root, Vector2.zero, Vector2.one, Vector2.zero);
            Stretch(inner);
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
            inner.gameObject.AddComponent<Image>().color = fill;
            return root;
        }

        public static Button CreateNeonButton(
            Transform parent,
            string label,
            Vector2 size,
            Color borderColor,
            Color fillColor,
            Color textColor,
            Action onClick,
            string name = "Button")
        {
            var rt = CreateNeonPanel(parent, size, fillColor, borderColor, name);
            var button = rt.gameObject.AddComponent<Button>();
            var inner = rt.Find("Inner");
            if (inner != null)
                button.targetGraphic = inner.GetComponent<Image>();

            var labelTmp = CreatePositionedText(rt, label, 36f, textColor, Vector2.zero, size, TextAlignmentOptions.Center, "Label");
            NeonUiTypography.ApplyButton(labelTmp, textColor, 36f);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        public static Button CreateIconButton(Transform parent, string label, Vector2 size, Color borderColor, Action onClick, string name = null)
        {
            return CreateNeonButton(parent, label, size, borderColor, NeonTheme.UiPanel, Color.white, onClick, name ?? $"Btn_{label}");
        }

        public static void StyleCircleIconButton(RectTransform button, Color borderColor, string iconGlyph, float diameter)
        {
            if (button == null)
                return;

            button.sizeDelta = new Vector2(diameter, diameter);

            foreach (var childImg in button.GetComponentsInChildren<Image>(true))
            {
                if (childImg.transform != button)
                    childImg.enabled = false;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();

            image.color = new Color(NeonTheme.UiPanel.r, NeonTheme.UiPanel.g, NeonTheme.UiPanel.b, 0.94f);
            image.raycastTarget = true;

            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var iconRoot = button.Find("NeonIcon") as RectTransform;
            if (iconRoot == null)
            {
                var iconGo = new GameObject("NeonIcon", typeof(RectTransform));
                iconGo.transform.SetParent(button, false);
                iconRoot = iconGo.GetComponent<RectTransform>();
                Stretch(iconRoot);
            }

            var label = iconRoot.GetComponent<TextMeshProUGUI>();
            if (label == null)
                label = iconRoot.gameObject.AddComponent<TextMeshProUGUI>();

            label.text = iconGlyph;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            NeonUiTypography.ApplyBody(label, borderColor, diameter * 0.38f, FontStyles.Bold);
        }

        public static Button CreateNeonHudIconButton(
            Transform parent,
            string name,
            Color borderColor,
            string iconGlyph,
            float size,
            Vector2 anchoredPosition,
            Action onClick)
        {
            var rt = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(size, size));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(NeonTheme.UiPanel.r, NeonTheme.UiPanel.g, NeonTheme.UiPanel.b, 0.96f);
            image.raycastTarget = true;

            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rt, false);
            Stretch(iconGo.GetComponent<RectTransform>());
            var icon = iconGo.AddComponent<TextMeshProUGUI>();
            icon.text = iconGlyph;
            icon.alignment = TextAlignmentOptions.Center;
            icon.raycastTarget = false;
            NeonUiTypography.ApplyBody(icon, borderColor, size * 0.36f, FontStyles.Bold);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        public static RectTransform CreateNeonPill(Transform parent, Vector2 size, Color borderColor, string name = "Pill")
        {
            return CreateNeonPanel(parent, size, NeonTheme.UiPanel, borderColor, name);
        }

        public static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = go.AddComponent<CanvasGroup>();
            return group;
        }

        public static void StyleScreenOverlay(Image background, Color? dim = null)
        {
            if (background == null)
                return;

            background.color = dim ?? new Color(0f, 0f, 0f, 0.78f);
        }

        public static Toggle CreateNeonCheckToggle(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            bool initialValue,
            Action<bool> onChanged)
        {
            const float rowWidth = 560f;
            const float rowHeight = 72f;

            var row = CreateRect($"Row_{label}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rowWidth, rowHeight));
            row.anchoredPosition = anchoredPosition;

            var labelTmp = CreatePositionedText(
                row,
                label,
                34f,
                NeonTheme.UiHudText,
                new Vector2(-48f, 0f),
                new Vector2(360f, 60f),
                TextAlignmentOptions.MidlineRight,
                "Label");
            NeonUiTypography.ApplyBody(labelTmp, NeonTheme.UiHudText, 34f, FontStyles.Bold);

            var boxRt = CreateRect("Box", row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f));
            boxRt.anchoredPosition = new Vector2(196f, 0f);
            var boxImg = boxRt.gameObject.AddComponent<Image>();
            boxImg.color = NeonTheme.UiCell;

            var checkRt = CreateRect("Check", boxRt, Vector2.zero, Vector2.one, Vector2.zero);
            Stretch(checkRt);
            var checkTmp = checkRt.gameObject.AddComponent<TextMeshProUGUI>();
            checkTmp.text = "\u2713";
            checkTmp.alignment = TextAlignmentOptions.Center;
            checkTmp.raycastTarget = false;
            NeonUiTypography.ApplyBody(checkTmp, NeonTheme.UiSuccess, 42f, FontStyles.Bold);

            var crossRt = CreateRect("Cross", boxRt, Vector2.zero, Vector2.one, Vector2.zero);
            Stretch(crossRt);
            var crossTmp = crossRt.gameObject.AddComponent<TextMeshProUGUI>();
            crossTmp.text = "\u2717";
            crossTmp.alignment = TextAlignmentOptions.Center;
            crossTmp.raycastTarget = false;
            NeonUiTypography.ApplyBody(crossTmp, NeonTheme.UiFail, 42f, FontStyles.Bold);

            var toggle = boxRt.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = boxImg;
            toggle.isOn = initialValue;
            ApplyCheckToggleVisual(checkRt.gameObject, crossRt.gameObject, initialValue);

            toggle.onValueChanged.AddListener(value =>
            {
                ApplyCheckToggleVisual(checkRt.gameObject, crossRt.gameObject, value);
                onChanged?.Invoke(value);
            });

            return toggle;
        }

        static void ApplyCheckToggleVisual(GameObject check, GameObject cross, bool isOn)
        {
            if (check != null)
                check.SetActive(isOn);
            if (cross != null)
                cross.SetActive(!isOn);
        }

        public static Toggle CreateNeonToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged)
        {
            return CreateNeonCheckToggle(parent, label, Vector2.zero, initialValue, onChanged);
        }

        public static void ApplyLogo(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            if (sprite == null)
            {
                target.enabled = false;
                return;
            }

            target.enabled = true;
            target.sprite = sprite;
            target.preserveAspect = true;
            target.color = Color.white;
        }
    }
}
