using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public static class GoArrowBranding
    {
        public const string GameTitle = "GO ARROW";
        public const string LogoResourcePath = "UI/GoArrowLogo";

        static Sprite _cachedLogo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PreloadLogo() => InvalidateCache();

        public static void InvalidateCache() => _cachedLogo = null;

        public static Sprite LoadLogoSprite()
        {
            if (_cachedLogo != null)
                return _cachedLogo;

            var sprite = Resources.Load<Sprite>(LogoResourcePath);
            if (sprite != null)
            {
                _cachedLogo = sprite;
                return _cachedLogo;
            }

            foreach (var obj in Resources.LoadAll(LogoResourcePath))
            {
                if (obj is Sprite loadedSprite)
                {
                    _cachedLogo = loadedSprite;
                    return _cachedLogo;
                }

                if (obj is Texture2D texture)
                {
                    _cachedLogo = CreateSpriteFromTexture(texture);
                    if (_cachedLogo != null)
                        return _cachedLogo;
                }
            }

            var textureOnly = Resources.Load<Texture2D>(LogoResourcePath);
            if (textureOnly != null)
            {
                _cachedLogo = CreateSpriteFromTexture(textureOnly);
                return _cachedLogo;
            }

            return TryLoadFromStreamingAssets();
        }

        static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null)
                return null;

            try
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            catch
            {
                return null;
            }
        }

        static Sprite TryLoadFromStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "UI/GoArrowLogo.png");
            if (!File.Exists(path))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                    return null;

                _cachedLogo = CreateSpriteFromTexture(texture);
                return _cachedLogo;
            }
            catch
            {
                return null;
            }
        }

        public static Image CreateLogoImage(Transform parent, Vector2 anchoredPosition, Vector2 size, string name = "Logo")
        {
            var rt = NeonUiBuilder.CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size);
            rt.anchoredPosition = anchoredPosition;

            var image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            ApplyLogo(image);
            return image;
        }

        public static void ApplyLogo(Image target)
        {
            if (target == null)
                return;

            var sprite = LoadLogoSprite();
            if (sprite == null)
            {
                target.enabled = false;
                Debug.LogWarning($"[GoArrow] Logo not found at Resources/{LogoResourcePath}.png");
                return;
            }

            NeonUiBuilder.ApplyLogo(target, sprite);
        }
    }
}
