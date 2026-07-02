#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor.LevelDesigner
{
    public class LevelDesignerWindow : EditorWindow
    {
        enum Tool { Pencil, Eraser, Pan, Playtest }

        const string DefaultFolder = "Assets/_Game/Resources/CustomLevels";
        const float PanelWidth = 280f;

        // [SerializeField] state survives domain reloads (script recompiles) so you never lose work
        // or need to restart the editor after a code change.
        [SerializeField] LevelDesignBoard _board = new LevelDesignBoard();
        [SerializeField] CanvasView _view = new CanvasView();
        [SerializeField] Tool _tool = Tool.Pencil;

        [SerializeField] ShapeType _shape = ShapeType.Star;
        [SerializeField] int _shapeSize = 21;
        [SerializeField] bool _showShape;
        [SerializeField] bool _snapToShape;
        [SerializeField] SymmetryMode _symmetry = SymmetryMode.None;
        readonly HashSet<Vector2Int> _shapeDots = new HashSet<Vector2Int>();
        int _shapeDotsBuiltFor = -1;

        [SerializeField] Texture2D _image;
        [SerializeField] float _imageThreshold = 0.5f;
        bool _showImage;
        readonly HashSet<Vector2Int> _imageDots = new HashSet<Vector2Int>();

        [SerializeField] string _shapeLibFolder = "Assets/_Game/ShapeSilhouettes";
        [SerializeField] int _libIndex;
        string[] _libFiles = new string[0];
        string[] _libNames = new string[0];

        [SerializeField] int _fillCount = 10;
        [SerializeField] DesignDifficulty _fillDifficulty = DesignDifficulty.Medium;

        [SerializeField] string _levelName = "Custom Level 1";
        [SerializeField] string _saveFolder = DefaultFolder;
        GameObject _loadPrefab;

        DesignAnalysis _analysis;

        readonly Stack<LevelDesignBoard> _undo = new Stack<LevelDesignBoard>();
        readonly Stack<LevelDesignBoard> _redo = new Stack<LevelDesignBoard>();

        DesignArrow _drawing;
        Vector2Int _lastDot;
        bool _panning;
        Vector2 _panStartMouse, _panStartPan;
        int _hoverArrow = -1;
        Vector2Int? _hoverDot;
        Vector2 _scroll;
        Rect _canvasRect;
        bool _viewInit;

        readonly HashSet<int> _removed = new HashSet<int>();
        bool _solving;
        List<int> _solveOrder;
        int _solveIndex;
        double _solveNextTime;

        [MenuItem("Go-Arrow/Level Designer")]
        public static void Open()
        {
            var w = GetWindow<LevelDesignerWindow>("Level Designer");
            w.minSize = new Vector2(940f, 560f);
            w.Show();
        }

        // Turn on Unity's "Auto Refresh" once so script edits recompile automatically (no need to
        // close/reopen the editor). The window itself survives recompiles via its [SerializeField] state.
        [InitializeOnLoadMethod]
        static void EnsureAutoRefresh()
        {
            if (EditorPrefs.GetInt("kAutoRefresh", 1) == 0) EditorPrefs.SetInt("kAutoRefresh", 1);
        }

        void OnEnable() { wantsMouseMove = true; EditorApplication.update += OnEditorUpdate; }
        void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        void OnEditorUpdate()
        {
            if (!_solving) return;
            if (EditorApplication.timeSinceStartup < _solveNextTime) return;
            if (_solveOrder == null || _solveIndex >= _solveOrder.Count) { _solving = false; Repaint(); return; }
            _removed.Add(_solveOrder[_solveIndex]);
            _solveIndex++;
            _solveNextTime = EditorApplication.timeSinceStartup + 0.35;
            Repaint();
        }

        // ---------------------------------------------------------------- GUI

        Vector2 _contentSize;

        void OnGUI()
        {
            var content = rootVisualElement != null && rootVisualElement.contentRect.height > 1f
                ? rootVisualElement.contentRect
                : new Rect(0, 0, position.width, position.height);
            _contentSize = new Vector2(content.width, content.height);

            _canvasRect = new Rect(PanelWidth, 0f, _contentSize.x - PanelWidth, _contentSize.y);
            if (!_viewInit && _canvasRect.width > 1f)
            { _view.Pan = new Vector2(_canvasRect.width * 0.5f, _canvasRect.height * 0.5f); _viewInit = true; }

            HandleInput(_canvasRect);
            LevelDesignerRenderer.Draw(BuildContext());
            DrawCanvasHud();
            DrawSidePanel(new Rect(0f, 0f, PanelWidth, _contentSize.y));
        }

        RenderContext BuildContext()
        {
            EnsureShapeDots();
            return new RenderContext
            {
                Rect = _canvasRect,
                ContentSize = _contentSize,
                Board = _board,
                View = _view,
                ShapeDots = _showShape ? _shapeDots : null,
                ImageDots = _showImage ? _imageDots : null,
                Analysis = _analysis,
                Drawing = _drawing,
                Removed = (_tool == Tool.Playtest || _solving) ? _removed : null,
                HoverArrow = _hoverArrow,
                HoverDot = _tool == Tool.Pencil ? _hoverDot : null
            };
        }

        void DrawCanvasHud()
        {
            var r = _canvasRect;
            GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, 18),
                $"{_tool}{(_symmetry == SymmetryMode.None ? "" : "  ·  Mirror " + _symmetry)}   ·   {_board.Arrows.Count} arrows   ·   zoom {_view.Zoom:0}",
                EditorStyles.whiteMiniLabel);

            if (_analysis != null)
            {
                var prev = GUI.color;
                string msg;
                if (_analysis.NotChecked)
                { GUI.color = new Color(0.85f, 0.85f, 0.55f); msg = $"⚠ Large level ({_analysis.ArrowCount} arrows) — solvability not auto-checked"; }
                else if (_analysis.Solvable)
                { GUI.color = new Color(0.45f, 1f, 0.55f); msg = $"✔ Solvable · {_analysis.DifficultyLabel} (score {_analysis.DifficultyScore}) · {_analysis.StartMoves} opening moves · {_analysis.Decisions} decisions"; }
                else
                { GUI.color = new Color(1f, 0.5f, 0.5f); msg = $"✘ STUCK · {_analysis.StuckArrows.Count} arrow(s) can never exit (shown red)"; }
                GUI.Label(new Rect(r.x + 10, r.y + 26, r.width - 20, 18), msg, EditorStyles.whiteMiniLabel);
                GUI.color = prev;
            }
        }

        void DrawSidePanel(Rect panelRect)
        {
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new RectOffset(6, 6, 6, 6).Remove(panelRect));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("LEVEL DESIGNER", EditorStyles.boldLabel);

            Header("Tool");
            _tool = (Tool)GUILayout.Toolbar((int)_tool, new[] { "✏ Pencil", "⌫ Eraser", "✋ Pan", "▶ Test" });
            EditorGUILayout.HelpBox(ToolHelp(), MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _undo.Count > 0; if (GUILayout.Button("↶ Undo")) DoUndo();
                GUI.enabled = _redo.Count > 0; if (GUILayout.Button("↷ Redo")) DoRedo();
                GUI.enabled = true;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit view")) FitView();
                if (GUILayout.Button("Recenter")) { _view.Pan = new Vector2(_canvasRect.width * 0.5f, _canvasRect.height * 0.5f); _view.Zoom = 26f; Repaint(); }
            }
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕  Clear All Arrows") && _board.Arrows.Count > 0
                && EditorUtility.DisplayDialog("Clear all", $"Delete all {_board.Arrows.Count} arrows?", "Clear all", "Cancel"))
            { PushUndo(); _board.Clear(); Invalidate(); }
            GUI.backgroundColor = Color.white;

            Header("Shape guide");
            EditorGUI.BeginChangeCheck();
            _shape = (ShapeType)EditorGUILayout.EnumPopup("Shape", _shape);
            _shapeSize = EditorGUILayout.IntSlider("Size (area)", _shapeSize, 7, 101);
            if (EditorGUI.EndChangeCheck()) _shapeDotsBuiltFor = -1;
            _showShape = EditorGUILayout.ToggleLeft("Highlight shape on grid", _showShape);
            _snapToShape = EditorGUILayout.ToggleLeft("Snap pencil to shape only", _snapToShape);
            GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            if (GUILayout.Button($"★ Generate level in this {_shape} shape", GUILayout.Height(24)))
            {
                _showShape = true;   // activate the shape so Auto-fill builds inside it
                EnsureShapeDots();
                AutoFill();          // fills the mask with a solvable bent puzzle (uses Arrows/Difficulty above)
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Fills ONLY inside the selected shape with a solvable, bent puzzle.\n• 'Size (area)' above controls BOTH precision (finer curves) and how many dots there are to fill — turn it up for sharper, denser shapes.\n• For big shapes the arrow count auto-scales to fill the whole shape (~95%), so the Arrows field below doesn't matter; only Difficulty does.\n• Large sizes (60+) take a few seconds to pack — the progress bar will show.", MessageType.None);

            Header("Symmetry");
            _symmetry = (SymmetryMode)EditorGUILayout.EnumPopup("Mirror (around 0,0)", _symmetry);

            Header("Validate & AI");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate")) Validate();
                if (GUILayout.Button(_solving ? "■ Stop" : "▶ AI Solve")) ToggleSolve();
            }
            if (GUILayout.Button("Auto-balance to solvable")) AutoBalance();
            if (_analysis != null)
                EditorGUILayout.HelpBox(
                    _analysis.Solvable
                        ? $"Solvable · {_analysis.DifficultyLabel} (score {_analysis.DifficultyScore})\nopening moves {_analysis.StartMoves} · decision points {_analysis.Decisions}"
                        : $"NOT solvable · {_analysis.StuckArrows.Count} stuck arrow(s) in red.",
                    _analysis.Solvable ? MessageType.Info : MessageType.Warning);

            Header("Auto-fill");
            _fillCount = Mathf.Clamp(EditorGUILayout.IntField("Arrows", _fillCount), 2, 500);
            _fillDifficulty = (DesignDifficulty)EditorGUILayout.EnumPopup("Difficulty", _fillDifficulty);
            EditorGUILayout.HelpBox(
                _fillCount <= 60
                    ? ((_showShape || _showImage)
                        ? "Builds a hard, solvable puzzle INSIDE the shape/image: varied arrow lengths, balanced directions, crossing paths & dependencies."
                        : "Builds a hard, solvable puzzle: varied lengths (1–5), 4 balanced directions, ≤3 per row/col, crossing paths & real dependencies.")
                    : "Over 60 arrows: dense packed fill (not difficulty-tuned).",
                MessageType.None);
            if (GUILayout.Button("Generate level")) AutoFill();

            Header("Screenshot → level");
            _image = (Texture2D)EditorGUILayout.ObjectField("Image", _image, typeof(Texture2D), false);
            _imageThreshold = EditorGUILayout.Slider("Dark threshold", _imageThreshold, 0.05f, 0.95f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show silhouette")) ShowSilhouette();
                if (GUILayout.Button("Hide")) { _showImage = false; Repaint(); }
            }
            if (GUILayout.Button("Fill from image")) ImageFill();

            Header("Shape library  (icons → shapes)");
            _shapeLibFolder = EditorGUILayout.TextField("Folder", _shapeLibFolder);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rescan folder")) ScanShapeLibrary();
                GUILayout.Label($"{_libNames.Length} icons", GUILayout.Width(70));
            }
            if (_libNames.Length > 0)
            {
                _libIndex = Mathf.Clamp(_libIndex, 0, _libNames.Length - 1);
                _libIndex = EditorGUILayout.Popup("Icon", _libIndex, _libNames);
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
                if (GUILayout.Button("★ Generate level in this icon", GUILayout.Height(24))) GenerateFromLibrary();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.HelpBox("Uses the 'Size (area)' slider above for resolution/density — turn it up for a bigger, sharper, denser icon. Arrow count auto-scales to fill it.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Drop silhouette PNGs (bold solid shapes on transparent/white) into the folder above, then Rescan. Each becomes a pickable shape — this is how you scale to 200+.", MessageType.Info);
            }

            Header("Load existing level");
            _loadPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _loadPrefab, typeof(GameObject), false);
            if (GUILayout.Button("Load into editor")) LoadPrefab();

            Header("Save");
            _levelName = EditorGUILayout.TextField("Name", _levelName);
            _saveFolder = EditorGUILayout.TextField("Folder", _saveFolder);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Browse", GUILayout.Width(70))) BrowseFolder();
                GUI.backgroundColor = new Color(0.45f, 0.95f, 0.55f);
                if (GUILayout.Button("SAVE PREFAB", GUILayout.Height(26))) SavePrefab();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static void Header(string title) { GUILayout.Space(7); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }

        string ToolHelp()
        {
            switch (_tool)
            {
                case Tool.Pencil: return "Drag from a start dot through dots; release on the head. Wheel = zoom, middle-drag = pan.";
                case Tool.Eraser: return "Click an arrow to delete it.";
                case Tool.Pan: return "Drag to pan. Wheel = zoom.";
                default: return "Click an arrow to launch it; clears only if its forward path is free. Validate to reset.";
            }
        }

        // ---------------------------------------------------------------- Input

        void HandleInput(Rect rect)
        {
            var e = Event.current;
            bool inside = rect.Contains(e.mousePosition);
            Vector2 local = e.mousePosition - rect.position;
            var dot = _view.LocalToGrid(local);

            if (inside)
            {
                _hoverDot = dot;
                _hoverArrow = (_tool == Tool.Eraser || _tool == Tool.Playtest) ? FindArrowAt(local) : -1;
            }

            if (e.type == EventType.ScrollWheel && inside)
            {
                var gridF = _view.LocalToGridF(local);
                _view.Zoom = Mathf.Clamp(_view.Zoom * (e.delta.y > 0 ? 0.9f : 1.1f), 6f, 90f);
                _view.Pan = new Vector2(local.x - gridF.x * _view.Zoom, local.y + gridF.y * _view.Zoom);
                e.Use(); Repaint(); return;
            }

            if (e.type == EventType.MouseDown && e.button == 2 && inside)
            { _panning = true; _panStartMouse = e.mousePosition; _panStartPan = _view.Pan; e.Use(); return; }
            if (_panning && e.type == EventType.MouseDrag)
            { _view.Pan = _panStartPan + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); return; }
            if (_panning && e.type == EventType.MouseUp) { _panning = false; e.Use(); return; }

            if (e.button == 0 && (inside || _drawing != null || _tool == Tool.Pan))
            {
                switch (_tool)
                {
                    case Tool.Pencil: HandlePencil(e, dot, inside); break;
                    case Tool.Eraser: HandleEraser(e, local); break;
                    case Tool.Pan: HandlePan(e); break;
                    case Tool.Playtest: HandlePlaytest(e, local); break;
                }
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) Repaint();
        }

        void HandlePencil(Event e, Vector2Int dot, bool inside)
        {
            if (_snapToShape && _showShape && !_shapeDots.Contains(dot)) return;

            if (e.type == EventType.MouseDown)
            {
                if (!inside) return; // never start a stroke under the side panel
                _drawing = new DesignArrow(); _drawing.Points.Add(dot); _lastDot = dot; e.Use();
            }
            else if (e.type == EventType.MouseDrag && _drawing != null)
            {
                if (inside && dot != _lastDot)
                {
                    // If you retrace back onto a dot already in the stroke, snap the path back to it
                    // (the overshoot you'd drawn past that dot is erased). Otherwise extend the stroke.
                    int existing = _drawing.Points.IndexOf(dot);
                    if (existing >= 0 && existing < _drawing.Points.Count - 1)
                        _drawing.Points.RemoveRange(existing + 1, _drawing.Points.Count - existing - 1);
                    else
                        _drawing.Points.Add(dot);
                    _lastDot = dot;
                    Repaint();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _drawing != null)
            { CommitDrawing(); e.Use(); }
        }

        void CommitDrawing()
        {
            var arrow = _drawing; _drawing = null;
            if (arrow == null || arrow.PointCount < 2) return;
            PushUndo();
            _board.Arrows.Add(arrow);
            foreach (var m in Mirror(arrow)) _board.Arrows.Add(m);
            Invalidate();
        }

        IEnumerable<DesignArrow> Mirror(DesignArrow arrow)
        {
            if (_symmetry == SymmetryMode.None) yield break;
            bool h = _symmetry == SymmetryMode.Horizontal || _symmetry == SymmetryMode.Both;
            bool v = _symmetry == SymmetryMode.Vertical || _symmetry == SymmetryMode.Both;
            if (h) yield return MirrorArrow(arrow, true, false);
            if (v) yield return MirrorArrow(arrow, false, true);
            if (_symmetry == SymmetryMode.Both) yield return MirrorArrow(arrow, true, true);
        }

        static DesignArrow MirrorArrow(DesignArrow a, bool fx, bool fy)
        {
            var r = new DesignArrow();
            foreach (var p in a.Points) r.Points.Add(new Vector2Int(fx ? -p.x : p.x, fy ? -p.y : p.y));
            return r;
        }

        void HandleEraser(Event e, Vector2 local)
        {
            if (e.type != EventType.MouseDown) return;
            int idx = FindArrowAt(local);
            if (idx >= 0) { PushUndo(); _board.Arrows.RemoveAt(idx); Invalidate(); }
            e.Use();
        }

        void HandlePan(Event e)
        {
            if (e.type == EventType.MouseDown) { _panning = true; _panStartMouse = e.mousePosition; _panStartPan = _view.Pan; e.Use(); }
        }

        void HandlePlaytest(Event e, Vector2 local)
        {
            if (e.type != EventType.MouseDown) return;
            int idx = FindArrowAt(local);
            if (idx < 0 || _removed.Contains(idx)) { e.Use(); return; }
            var lines = _board.ToLineData();
            var active = new List<int>();
            for (int i = 0; i < lines.Count; i++) if (!_removed.Contains(i)) active.Add(i);
            if (LevelSolvabilityValidator.CanExit(lines, idx, active)) _removed.Add(idx);
            else ShowNotification(new GUIContent("Blocked! Path not clear."));
            e.Use(); Repaint();
        }

        int FindArrowAt(Vector2 local)
        {
            // Hit anywhere along the arrow: thin line segments need a tight tolerance, but the
            // prominent head triangle / tail dot / bend vertices get a larger one so they're easy
            // to click (clicking the visible arrowhead must erase it).
            float segHit = Mathf.Max(10f, _view.Zoom * 0.45f);
            float ptHit = Mathf.Max(14f, _view.Zoom * 0.65f);
            float best = float.MaxValue;
            int found = -1;
            for (int i = 0; i < _board.Arrows.Count; i++)
            {
                var a = _board.Arrows[i];
                for (int s = 0; s < a.PointCount - 1; s++)
                {
                    float d = DistToSegment(local, _view.GridToLocal(a.Points[s]), _view.GridToLocal(a.Points[s + 1]));
                    if (d <= segHit && d < best) { best = d; found = i; }
                }
                for (int p = 0; p < a.PointCount; p++)
                {
                    float d = Vector2.Distance(local, _view.GridToLocal(a.Points[p]));
                    if (d <= ptHit && d < best) { best = d; found = i; }
                }
            }
            return found;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ---------------------------------------------------------------- Actions

        void EnsureShapeDots()
        {
            if (!_showShape) return;
            int key = (int)_shape * 1000 + _shapeSize;
            if (_shapeDotsBuiltFor == key) return;
            _shapeDots.Clear();
            int g = Mathf.Max(7, _shapeSize);
            var m = ShapeMaskGenerator.CreateMask(_shape, g);
            int o = m.GetLength(0) / 2;
            for (int y = 0; y < m.GetLength(1); y++)
            for (int x = 0; x < m.GetLength(0); x++)
                if (m[x, y]) _shapeDots.Add(new Vector2Int(x - o, y - o));
            _shapeDotsBuiltFor = key;
        }

        void Validate() { _analysis = LevelDesignerOps.Analyze(_board); Repaint(); }

        void ToggleSolve()
        {
            if (_solving) { _solving = false; return; }
            Validate();
            if (_analysis == null || !_analysis.Solvable) { ShowNotification(new GUIContent("Not solvable — nothing to play.")); return; }
            _removed.Clear(); _solveOrder = _analysis.Order; _solveIndex = 0; _solving = true;
            _solveNextTime = EditorApplication.timeSinceStartup + 0.3;
        }

        void AutoBalance()
        {
            PushUndo();
            bool ok = LevelDesignerOps.AutoBalance(_board, out int changes);
            Validate();
            ShowNotification(new GUIContent(ok ? $"Balanced in {changes} change(s)." : "Couldn't fully balance."));
        }

        void AutoFill()
        {
            EnsureShapeDots();
            bool shapeOn = _showShape && _shapeDots.Count > 0;
            bool imageOn = !shapeOn && _showImage && _imageDots.Count > 0;
            var mask = shapeOn ? new HashSet<Vector2Int>(_shapeDots)
                     : imageOn ? new HashSet<Vector2Int>(_imageDots) : null;

            LevelDesignBoard filled;
            try
            {
                EditorUtility.DisplayProgressBar("Generating level", "Building a solvable puzzle…", 0.5f);
                // Big shapes -> the fast grid-based packer regardless of the Arrows count: it fills
                // the whole mask densely with an O(1)-per-cell solvability check (no freeze), and the
                // count auto-scales to the shape's area so "how many arrows" no longer matters. Small
                // shapes/counts keep the slower difficulty-aware Generate path.
                int maskCells = mask?.Count ?? 0;
                bool big = maskCells > 200;
                int packCount = big ? Mathf.Max(_fillCount, maskCells / 5) : _fillCount;
                filled = (!big && _fillCount <= 60)
                    ? LevelDesignerGenerator.Generate(_board, _fillCount, mask, NextSeed(), _fillDifficulty)          // ≤60 & small: difficulty-aware, bent + gap-fill
                    : LevelDesignerGenerator.FastBentPack(_board, packCount, mask, NextSeed(), _fillDifficulty);      // big or >60: fast bent solvable pack (fills the mask)
            }
            finally { EditorUtility.ClearProgressBar(); }

            if (filled == null || filled.Arrows.Count == 0) { ShowNotification(new GUIContent("Fill failed — try another count.")); return; }
            PushUndo(); _board = filled; FitView(); Invalidate();
            Validate(); // show the resulting difficulty immediately
            // Count is a target: the generator fills a compact board to ~65% (dense, solvable),
            // so the actual count often lands a little under the request — that's expected, not an
            // error. Only flag a *severe* shortfall (the board couldn't take anywhere near it).
            if (filled.Arrows.Count < _fillCount / 2)
                ShowNotification(new GUIContent($"Board packed to {filled.Arrows.Count} arrows (a denser fill wouldn't stay solvable)."));
        }

        void ShowSilhouette()
        {
            if (_image == null) { ShowNotification(new GUIContent("Assign an image first.")); return; }
            var tex = MakeReadable(_image);
            int g = Mathf.Max(15, _shapeSize);
            var mask = LevelDesignerOps.ImageToMask(tex, g, g, _imageThreshold);
            _imageDots.Clear();
            int o = g / 2;
            for (int y = 0; y < g; y++) for (int x = 0; x < g; x++) if (mask[x, y]) _imageDots.Add(new Vector2Int(x - o, y - o));
            _showImage = true; Repaint();
        }

        void ImageFill()
        {
            if (_image == null) { ShowNotification(new GUIContent("Assign an image first.")); return; }
            var tex = MakeReadable(_image);
            var filled = LevelDesignerOps.ImageAutoFill(_board, tex, _fillCount, _imageThreshold, NextSeed());
            if (filled == null) { ShowNotification(new GUIContent("Couldn't fit the silhouette.")); return; }
            PushUndo(); _board = filled; FitView(); Invalidate();
        }

        // -------- Shape library: a whole folder of silhouette PNGs -> pickable shapes (scale to 200+)

        void ScanShapeLibrary()
        {
            var files = new System.Collections.Generic.List<string>();
            if (AssetDatabase.IsValidFolder(_shapeLibFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { _shapeLibFolder }))
                    files.Add(AssetDatabase.GUIDToAssetPath(guid));
            files.Sort();
            _libFiles = files.ToArray();
            _libNames = new string[_libFiles.Length];
            for (int i = 0; i < _libFiles.Length; i++)
                _libNames[i] = System.IO.Path.GetFileNameWithoutExtension(_libFiles[i]);
            if (_libFiles.Length == 0)
                ShowNotification(new GUIContent($"No textures in {_shapeLibFolder} — create it and drop PNGs in."));
            Repaint();
        }

        void GenerateFromLibrary()
        {
            if (_libFiles.Length == 0) { ScanShapeLibrary(); if (_libFiles.Length == 0) return; }
            _libIndex = Mathf.Clamp(_libIndex, 0, _libFiles.Length - 1);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(_libFiles[_libIndex]);
            if (tex == null) { ShowNotification(new GUIContent("Couldn't load icon.")); return; }
            _image = tex;         // reuse the existing image -> mask pipeline
            _showShape = false;   // make sure the image mask wins over any shape guide
            ShowSilhouette();     // builds _imageDots + sets _showImage
            AutoFill();           // fills the silhouette using the Arrows/Difficulty above
        }

        void LoadPrefab()
        {
            if (_loadPrefab == null) { ShowNotification(new GUIContent("Assign a Level prefab first.")); return; }
            var loaded = LevelDesignerOps.LoadFromPrefab(_loadPrefab);
            if (loaded == null) { ShowNotification(new GUIContent("Not a Go-Arrow level prefab.")); return; }
            PushUndo(); _board = loaded; _levelName = _loadPrefab.name; FitView(); Invalidate();
        }

        void SavePrefab()
        {
            if (_board.Arrows.Count == 0) { ShowNotification(new GUIContent("Draw some arrows first.")); return; }
            Validate();
            if (_analysis != null && !_analysis.Solvable &&
                !EditorUtility.DisplayDialog("Level not solvable",
                    "This level is UNSOLVABLE (red arrows can't exit). Save anyway?", "Save anyway", "Cancel"))
                return;
            string path = LevelDesignerOps.BakeToPrefab(_board, SanitizeName(_levelName), _saveFolder);
            if (path != null)
            {
                ShowNotification(new GUIContent($"Saved {System.IO.Path.GetFileName(path)}"));
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null) EditorGUIUtility.PingObject(asset);
            }
        }

        void BrowseFolder()
        {
            string abs = EditorUtility.OpenFolderPanel("Save folder (inside Assets)", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;
            if (abs.StartsWith(Application.dataPath)) _saveFolder = "Assets" + abs.Substring(Application.dataPath.Length);
            else ShowNotification(new GUIContent("Folder must be inside Assets."));
        }

        // ---------------------------------------------------------------- Helpers

        void Invalidate() { _analysis = null; _removed.Clear(); _solving = false; Repaint(); }

        void PushUndo()
        {
            _undo.Push(_board.Clone());
            if (_undo.Count > 60)
            { var keep = new List<LevelDesignBoard>(_undo); keep.RemoveAt(keep.Count - 1); _undo.Clear(); for (int i = keep.Count - 1; i >= 0; i--) _undo.Push(keep[i]); }
            _redo.Clear();
        }

        void DoUndo() { if (_undo.Count == 0) return; _redo.Push(_board.Clone()); _board = _undo.Pop(); Invalidate(); }
        void DoRedo() { if (_redo.Count == 0) return; _undo.Push(_board.Clone()); _board = _redo.Pop(); Invalidate(); }

        void FitView()
        {
            if (_canvasRect.width < 2f) return;
            if (_board.Arrows.Count == 0)
            { _view.Pan = new Vector2(_canvasRect.width * 0.5f, _canvasRect.height * 0.5f); _view.Zoom = 26f; Repaint(); return; }

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var a in _board.Arrows) foreach (var p in a.Points)
            { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }

            float spanX = Mathf.Max(1, maxX - minX), spanY = Mathf.Max(1, maxY - minY);
            float zx = (_canvasRect.width - 120f) / spanX, zy = (_canvasRect.height - 120f) / spanY;
            _view.Zoom = Mathf.Clamp(Mathf.Min(zx, zy), 6f, 90f);
            Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            _view.Pan = new Vector2(_canvasRect.width * 0.5f - center.x * _view.Zoom,
                                    _canvasRect.height * 0.5f + center.y * _view.Zoom);
            Repaint();
        }

        int _seed = 12345;
        int NextSeed() { unchecked { _seed = _seed * 1103515245 + 12345; } return _seed & 0x7fffffff; }

        static string SanitizeName(string n) => string.IsNullOrWhiteSpace(n) ? "Custom Level" : n.Trim();

        static Texture2D MakeReadable(Texture2D src)
        {
            if (src == null) return null;
            if (src.isReadable) return src;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); tex.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return tex;
        }
    }
}
#endif
