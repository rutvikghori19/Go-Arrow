using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public static class NeonUiLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float BannerAdHeight = 150f;
        public const float TopInset = 28f;
        public const float HudHeight = 128f;

        public static void ConfigureCanvas(CanvasScaler scaler)
        {
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static void ApplyPlayArea(RectTransform root)
        {
            if (root == null)
                return;

            float scale = ReferenceHeight / Mathf.Max(1f, Screen.height);
            Rect safe = Screen.safeArea;

            float bottomInset = BannerAdHeight + Mathf.Max(0f, safe.yMin * scale);
            float topInset = TopInset + Mathf.Max(0f, (Screen.height - safe.yMax) * scale);

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(0f, bottomInset);
            root.offsetMax = new Vector2(0f, -topInset);
        }

        public static void ApplyToCanvas(RectTransform root, CanvasScaler scaler)
        {
            ConfigureCanvas(scaler);
            ApplyPlayArea(root);
        }

        public static float MainMenuLogoY => 360f;
        public static float MainMenuPlayY => 40f;
        public static float MainMenuSettingsY => -90f;
        public static float MainMenuLevelLabelY => -360f;
    }
}
