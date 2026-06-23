using System.Collections.Generic;
using _Game.Theme;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public sealed class LevelSelectPanel : UIPanel
    {
        [SerializeField] RectTransform _contentRoot;
        [SerializeField] ScrollRect _scrollRect;
        [SerializeField] Button _closeButton;
        [SerializeField] int _columns = 5;
        [SerializeField] float _cellSize = 140f;
        [SerializeField] float _spacing = 16f;

        UIRootController _uiRoot;
        readonly List<Button> _buttons = new List<Button>();
        bool _built;

        public void SetUIRoot(UIRootController uiRoot) => _uiRoot = uiRoot;

        void Awake()
        {
            EnsureHierarchy();
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        void OnCloseClicked() => Hide();

        public override void Show(bool playSound = true)
        {
            EnsureHierarchy();
            if (!_built)
                BuildLevelButtons();

            HighlightCurrentLevel();
            base.Show(playSound);
        }

        void EnsureHierarchy()
        {
            if (_contentRoot != null && _scrollRect != null)
                return;

            var overlay = transform as RectTransform;
            if (overlay == null)
                return;

            Stretch(overlay);

            if (GetComponent<Image>() == null)
            {
                var bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.72f);
            }

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var panel = CreateRect("Panel", overlay, new Vector2(0.5f, 0.5f), new Vector2(900f, 1200f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = NeonTheme.UiPanel;

            var title = CreateText("Title", panel, "Select Level", 56, new Vector2(0f, 520f), new Vector2(800f, 80f), NeonTheme.UiHudText);

            var closeGo = CreateRect("CloseButton", panel, new Vector2(0.5f, 0.5f), new Vector2(72f, 72f));
            closeGo.anchoredPosition = new Vector2(380f, 520f);
            _closeButton = closeGo.gameObject.AddComponent<Button>();
            var closeImg = closeGo.gameObject.AddComponent<Image>();
            closeImg.color = NeonTheme.UiAccent;
            CreateText("X", closeGo, "X", 40, Vector2.zero, closeGo.sizeDelta);

            var scrollGo = CreateRect("ScrollView", panel, new Vector2(0.5f, 0.5f), new Vector2(820f, 980f));
            scrollGo.anchoredPosition = new Vector2(0f, -40f);
            _scrollRect = scrollGo.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", scrollGo, new Vector2(0.5f, 0.5f), scrollGo.sizeDelta);
            viewport.gameObject.AddComponent<RectMask2D>();
            _scrollRect.viewport = viewport;

            _contentRoot = CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(viewport.sizeDelta.x, 400f));
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            _contentRoot.anchoredPosition = Vector2.zero;
            var layout = _contentRoot.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(_cellSize, _cellSize);
            layout.spacing = new Vector2(_spacing, _spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = _columns;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset(12, 12, 12, 12);

            var fitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _contentRoot;
        }

        void BuildLevelButtons()
        {
            if (_contentRoot == null || LevelManager.Instance == null)
                return;

            int total = LevelManager.Instance.TotalLevelCount;
            int current = LevelManager.Instance.ActiveLevelNumber;

            for (int level = 1; level <= total; level++)
            {
                int captured = level;
                var cell = CreateRect($"Level_{level}", _contentRoot, new Vector2(0.5f, 0.5f), new Vector2(_cellSize, _cellSize));
                var image = cell.gameObject.AddComponent<Image>();
                image.color = level == current
                    ? NeonTheme.UiCellActive
                    : NeonTheme.UiCell;

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => OnLevelClicked(captured));
                _buttons.Add(button);

                var textColor = level == current ? Color.white : NeonTheme.UiText;
                CreateText("Label", cell, level.ToString(), 42, Vector2.zero, cell.sizeDelta, textColor);
            }

            _built = true;
        }

        void HighlightCurrentLevel()
        {
            if (!_built || LevelManager.Instance == null)
                return;

            int current = LevelManager.Instance.ActiveLevelNumber;
            for (int i = 0; i < _buttons.Count; i++)
            {
                var image = _buttons[i].GetComponent<Image>();
                if (image == null)
                    continue;

                bool isCurrent = i + 1 == current;
                image.color = isCurrent
                    ? NeonTheme.UiCellActive
                    : NeonTheme.UiCell;

                var label = _buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = isCurrent ? Color.white : NeonTheme.UiText;
            }
        }

        void OnLevelClicked(int levelNumber)
        {
            Hide(false);
            if (_uiRoot != null)
                _uiRoot.OnLevelSelected(levelNumber);
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? Color.white;
            return tmp;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
