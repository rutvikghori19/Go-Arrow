using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _Game.Theme
{
    public static class NeonTheme
    {
        public const float LineEmissionBoost = 1.45f;
        public const float SpriteEmissionBoost = 1f;
        public const float LineVertexBoost = 1f;

        public static readonly Color Background = new Color(0.02f, 0.02f, 0.06f, 1f);
        public static readonly Color CameraClear = new Color(0.01f, 0.01f, 0.03f, 1f);
        public static readonly Color FailureFlash = new Color(1f, 0.15f, 0.35f, 1f);

        static readonly Color[] ArrowPalette =
        {
            new Color(1f, 0.08f, 0.55f),
            new Color(0f, 1f, 1f),
            new Color(0.7f, 0.15f, 1f),
            new Color(0.15f, 1f, 0.35f),
            new Color(1f, 0.92f, 0.05f),
            new Color(1f, 0.3f, 0.05f),
            new Color(0.35f, 0.55f, 1f),
            new Color(1f, 0.15f, 0.8f),
            new Color(0.1f, 1f, 0.85f),
            new Color(1f, 0.2f, 1f),
            new Color(1f, 0.45f, 0.65f),
            new Color(0.55f, 1f, 0.2f),
            new Color(0.2f, 0.65f, 1f),
            new Color(1f, 0.55f, 0.15f),
            new Color(0.85f, 0.25f, 1f),
            new Color(0.15f, 0.95f, 0.55f),
            new Color(1f, 0.75f, 0.35f),
            new Color(0.45f, 0.35f, 1f),
            new Color(1f, 0.15f, 0.25f),
            new Color(0.35f, 1f, 0.95f),
            new Color(0.95f, 0.35f, 0.75f),
            new Color(0.75f, 1f, 0.15f),
            new Color(0.25f, 0.85f, 1f),
            new Color(1f, 0.6f, 0.9f),
        };

        public static Color PickArrowColor(int seed)
        {
            if (ArrowPalette.Length == 0)
                return Color.cyan;

            int index = Mathf.Abs(seed) % ArrowPalette.Length;
            Color color = ArrowPalette[index];

            Color.RGBToHSV(color, out float h, out float s, out float v);
            float hueJitter = ((Mathf.Abs(seed) >> 6) % 17 - 8) / 120f;
            h = (h + hueJitter + 1f) % 1f;
            s = Mathf.Clamp01(s + 0.05f);
            v = Mathf.Clamp01(v);
            return Color.HSVToRGB(h, s, v);
        }

        public static Color BoostForGlow(Color color, float multiplier)
        {
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a);
        }

        public static void ApplyNeonColor(Material material, Color color, float emissionBoost = LineEmissionBoost)
        {
            if (material == null)
                return;

            material.color = color;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", BoostForGlow(color, emissionBoost));
                material.EnableKeyword("_EMISSION");
            }

            if (material.HasProperty("_UseEmission"))
                material.SetFloat("_UseEmission", 1f);

            if (material.HasProperty("_OutlineColor"))
                material.SetColor("_OutlineColor", BoostForGlow(color, 0.45f));

            if (material.HasProperty("_UseOutline"))
                material.SetFloat("_UseOutline", 1f);

            if (material.HasProperty("_OutlineWidth"))
                material.SetFloat("_OutlineWidth", 1f);

            if (material.HasProperty("_RimColor"))
                material.SetColor("_RimColor", BoostForGlow(color, 1.15f));

            if (material.HasProperty("_UseRim"))
                material.SetFloat("_UseRim", 1f);

            if (material.HasProperty("_RimStrength"))
                material.SetFloat("_RimStrength", 0.45f);

            if (material.HasProperty("_RimMin"))
                material.SetFloat("_RimMin", 0.55f);

            if (material.HasProperty("_RimMax"))
                material.SetFloat("_RimMax", 0.9f);

            if (material.HasProperty("_HColor"))
                material.SetColor("_HColor", BoostForGlow(color, 1.1f));
        }

        public static void ApplyNeonLineRenderer(LineRenderer lineRenderer, Color color, float emissionBoost = LineEmissionBoost)
        {
            if (lineRenderer == null)
                return;

            var vertexColor = BoostForGlow(color, LineVertexBoost);
            lineRenderer.startColor = vertexColor;
            lineRenderer.endColor = vertexColor;

            if (lineRenderer.sharedMaterial != null && lineRenderer.material == lineRenderer.sharedMaterial)
                lineRenderer.material = new Material(lineRenderer.sharedMaterial);

            ApplyNeonColor(lineRenderer.material, color, emissionBoost);
        }

        public static void ApplyNeonSprite(SpriteRenderer spriteRenderer, Color color, float emissionBoost = SpriteEmissionBoost)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.color = emissionBoost <= 1f ? color : BoostForGlow(color, emissionBoost);
        }

        public static void ApplyLevelBackground(Transform levelRoot)
        {
            if (levelRoot == null)
                return;

            var background = levelRoot.Find("Background");
            if (background == null)
                return;

            var meshRenderer = background.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;

            var material = meshRenderer.material;
            ApplyNeonColor(material, Background, 0.15f);
        }

        public static void ApplyCamera(Camera camera)
        {
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraClear;
            camera.allowHDR = true;

            var urp = camera.GetComponent<UniversalAdditionalCameraData>();
            if (urp != null)
                urp.renderPostProcessing = true;
        }

        public static void ApplyPostProcessing()
        {
            var volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null || volume.profile == null)
                return;

            if (volume.profile.TryGet(out Bloom bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(1.05f);
                bloom.intensity.Override(0.42f);
                bloom.scatter.Override(0.4f);
            }

            if (volume.profile.TryGet(out Vignette vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.18f);
            }
        }

        public static readonly Color UiPanel = new Color(0.06f, 0.04f, 0.12f, 1f);
        public static readonly Color UiCell = new Color(0.1f, 0.08f, 0.16f, 1f);
        public static readonly Color UiCellActive = new Color(0.05f, 0.75f, 0.95f, 1f);
        public static readonly Color UiText = new Color(0.75f, 0.85f, 1f, 1f);
        public static readonly Color UiAccent = new Color(1f, 0.2f, 0.75f, 1f);
        public static readonly Color UiHudText = new Color(0.2f, 0.95f, 1f, 1f);
        public static readonly Color UiSuccess = new Color(0.35f, 1f, 0.35f, 1f);
        public static readonly Color UiFail = new Color(1f, 0.25f, 0.45f, 1f);
        public static readonly Color UiCyanBorder = new Color(0.1f, 0.9f, 1f, 1f);
        public static readonly Color UiMagentaBorder = new Color(1f, 0.2f, 0.75f, 1f);
    }
}
