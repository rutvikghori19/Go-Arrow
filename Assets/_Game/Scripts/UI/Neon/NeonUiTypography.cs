using TMPro;
using UnityEngine;

namespace _Game.UI
{
    public static class NeonUiTypography
    {
        const string TitleFontPath = "Fonts & Materials/Bangers SDF";
        const string BodyFontPath = "Fonts & Materials/Oswald Bold SDF";

        static TMP_FontAsset _titleFont;
        static TMP_FontAsset _bodyFont;

        public static TMP_FontAsset TitleFont => _titleFont ??= LoadFont(TitleFontPath);
        public static TMP_FontAsset BodyFont => _bodyFont ??= LoadFont(BodyFontPath);

        static TMP_FontAsset LoadFont(string path)
        {
            var font = Resources.Load<TMP_FontAsset>(path);
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        public static void ApplyTitle(TextMeshProUGUI tmp, Color color, float size)
        {
            if (tmp == null)
                return;

            tmp.font = BodyFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = size;
            tmp.color = color;
        }

        public static void ApplyButton(TextMeshProUGUI tmp, Color color, float size)
        {
            if (tmp == null)
                return;

            tmp.font = BodyFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = size;
            tmp.color = color;
        }

        public static void ApplyBody(TextMeshProUGUI tmp, Color color, float size, FontStyles extra = FontStyles.Normal)
        {
            if (tmp == null)
                return;

            tmp.font = BodyFont;
            tmp.fontStyle = extra;
            tmp.fontSize = size;
            tmp.color = color;
        }

        public static void ApplyHud(TextMeshProUGUI tmp, Color color, float size)
        {
            if (tmp == null)
                return;

            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
        }
    }
}
