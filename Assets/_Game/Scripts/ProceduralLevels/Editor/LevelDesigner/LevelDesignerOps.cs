#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using GameLine = _Game.Line.Line;
using _Game.Line;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor.LevelDesigner
{
    /// <summary>
    /// Stateless logic for the Level Designer: analysis (solver + difficulty + stuck detection),
    /// auto-fill, auto-balance, bake-to-prefab, load-from-prefab, and image silhouette matching.
    /// Everything here is bounded so it can never freeze the editor.
    /// </summary>
    public static class LevelDesignerOps
    {
        const string TemplateResource = "Levels/Level 10";
        const string LinePrefabResource = "Line/Line (1)";
        const float LineThickness = 0.3f;
        const float LineScale = 0.9f;
        static readonly Vector3 BackgroundScale = new Vector3(6f, 6f, 6f);
        static readonly Vector3 BackgroundPosition = new Vector3(2.71f, -2f, 0f);

        // ---------------------------------------------------------------- Analysis

        // Above this, the O(N²·rays) solver would freeze the editor, so we skip full validation.
        const int FullValidateCap = 140;

        public static DesignAnalysis Analyze(LevelDesignBoard board)
        {
            var result = new DesignAnalysis();
            var lines = board.ToLineData();
            result.ArrowCount = lines.Count;
            if (lines.Count == 0)
                return result;

            if (lines.Count > FullValidateCap)
            {
                // Too many arrows to validate cheaply. Report but don't run the solver.
                result.NotChecked = true;
                result.Solvable = true;
                result.Difficulty = DesignDifficulty.Expert;
                result.DifficultyScore = lines.Count;
                return result;
            }

            result.Order = LevelSolvabilityValidator.FindRemovalOrder(lines);
            result.Solvable = result.Order != null;

            var all = new List<int>();
            for (int i = 0; i < lines.Count; i++) all.Add(i);
            result.StartMoves = LevelSolvabilityValidator.GetRemovableIndices(lines, all).Count;
            result.Decisions = LevelDifficultyAnalyzer.CountDecisionPoints(lines);

            if (!result.Solvable)
                result.StuckArrows = FindStuck(lines);

            ScoreDifficulty(result);
            return result;
        }

        /// <summary>Greedily peel; whatever remains when no arrow can move is the stuck set.</summary>
        static List<int> FindStuck(IReadOnlyList<LevelLineData> lines)
        {
            var remaining = new List<int>();
            for (int i = 0; i < lines.Count; i++) remaining.Add(i);

            while (remaining.Count > 0)
            {
                int removable = -1;
                for (int i = remaining.Count - 1; i >= 0; i--)
                    if (LevelSolvabilityValidator.CanExit(lines, remaining[i], remaining))
                    { removable = remaining[i]; break; }

                if (removable < 0) break;
                remaining.Remove(removable);
            }
            return remaining;
        }

        static void ScoreDifficulty(DesignAnalysis r)
        {
            if (!r.Solvable) { r.Difficulty = DesignDifficulty.Expert; r.DifficultyScore = 0; return; }

            // More arrows and more forced branch points = harder; many opening moves = easier.
            float score = r.ArrowCount + r.Decisions * 4f - Mathf.Max(0, r.StartMoves - 1) * 2f;
            r.DifficultyScore = Mathf.RoundToInt(score);

            if (r.ArrowCount <= 5 || score < 8f) r.Difficulty = DesignDifficulty.Easy;
            else if (score < 16f) r.Difficulty = DesignDifficulty.Medium;
            else if (score < 26f) r.Difficulty = DesignDifficulty.Hard;
            else r.Difficulty = DesignDifficulty.Expert;
        }

        // ---------------------------------------------------------------- Auto-fill

        /// <summary>Generates a solvable, multi-bend layout of the given shape/count and drops it
        /// onto the board centred. Returns null on failure (board left untouched).</summary>
        // Up to ~16 arrows uses the pretty validated dense builder; beyond that the dense builder
        // and the validator both get too slow, so we switch to a fast solvable-by-construction fill.
        const int DenseMaxCount = 16;

        public static LevelDesignBoard AutoFill(LevelDesignBoard board, ShapeType shape, int count,
            DesignDifficulty mode, int seedBase)
        {
            count = Mathf.Clamp(count, 2, 500);
            if (count > DenseMaxCount)
                return DensePack(board, count, null, seedBase);

            // Comfortable grid keeps generation on the builder's FAST path (a tight grid forces a
            // slow raycast fallback that would freeze the editor).
            int grid = Mathf.Clamp(MakeOdd(count + 9), 11, 21);

            // Auto-fill only uses ROOMY shapes — thin silhouettes (Star, Cross, Plus, Arrow, …)
            // can't fit an exact count on the fast path and would freeze the editor for seconds.
            // (Thin shapes are still fully usable as a hand-drawing guide overlay.)
            ShapeType chosen = ToFastShape(shape);
            ShapeType[] ladder = { chosen, ShapeType.Circle, ShapeType.Square, ShapeType.Diamond };
            int wantDecisions = mode == DesignDifficulty.Easy ? 0 : mode == DesignDifficulty.Medium ? 1 : 2;
            LevelDesignBoard fallback = null;

            foreach (var s in ladder)
                for (int seed = 0; seed < 3; seed++)
                {
                    var lines = DenseShapeLevelBuilder.Build(s, grid, count, seedBase + seed * 13 + 1);
                    if (lines == null || lines.Count != count) continue;
                    if (!LevelSolvabilityValidator.IsSolvable(lines)) continue;

                    var built = BoardFromLines(board.Width, board.Height, lines);
                    if (fallback == null) fallback = built;
                    if (LevelDifficultyAnalyzer.CountDecisionPoints(lines) >= wantDecisions)
                        return built;
                }
            return fallback;
        }

        // ---------------------------------------------------------------- Auto-balance

        /// <summary>Bounded local search: shift whole arrows by ±1 until the level becomes solvable
        /// or the budget runs out. Returns (success, changes).</summary>
        public static bool AutoBalance(LevelDesignBoard board, out int changes)
        {
            changes = 0;
            var analysis = Analyze(board);
            if (analysis.Solvable) return true;

            var offsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            const int budget = 120;
            for (int iter = 0; iter < budget; iter++)
            {
                var a = Analyze(board);
                if (a.Solvable) return true;
                if (a.StuckArrows.Count == 0) return false;

                int best = ArrowPeelDepth(board);
                bool improved = false;

                foreach (int idx in a.StuckArrows)
                {
                    foreach (var off in offsets)
                    {
                        var candidate = board.Clone();
                        if (!ShiftArrow(candidate, idx, off)) continue;
                        int depth = ArrowPeelDepth(candidate);
                        if (depth > best)
                        {
                            best = depth;
                            board.Arrows[idx] = candidate.Arrows[idx];
                            changes++;
                            improved = true;
                            break;
                        }
                    }
                    if (improved) break;
                }

                if (!improved)
                {
                    // Last resort: extend the most-stuck arrow's head outward by one free cell.
                    if (!ExtendHeadOutward(board, a.StuckArrows[0])) return false;
                    changes++;
                }
            }
            return Analyze(board).Solvable;
        }

        static int ArrowPeelDepth(LevelDesignBoard board)
        {
            var lines = board.ToLineData();
            var remaining = new List<int>();
            for (int i = 0; i < lines.Count; i++) remaining.Add(i);
            int peeled = 0;
            while (remaining.Count > 0)
            {
                int removable = -1;
                for (int i = remaining.Count - 1; i >= 0; i--)
                    if (LevelSolvabilityValidator.CanExit(lines, remaining[i], remaining))
                    { removable = remaining[i]; break; }
                if (removable < 0) break;
                remaining.Remove(removable);
                peeled++;
            }
            return peeled;
        }

        static bool ShiftArrow(LevelDesignBoard board, int idx, Vector2Int off)
        {
            var arrow = board.Arrows[idx];
            var moved = new List<Vector2Int>(arrow.PointCount);
            foreach (var p in arrow.Points)
            {
                var np = p + off;
                if (!board.InBounds(np)) return false;
                moved.Add(np);
            }
            arrow.Points = moved;
            return true;
        }

        static bool ExtendHeadOutward(LevelDesignBoard board, int idx)
        {
            var arrow = board.Arrows[idx];
            if (arrow.PointCount < 2) return false;
            var dir = arrow.HeadDir();
            var step = LevelLineData.SnapToAxis(new Vector2Int(dir.x, dir.y));
            var np = arrow.Head + step;
            if (!board.InBounds(np)) return false;
            arrow.Points.Add(np);
            return true;
        }

        // ---------------------------------------------------------------- Bake to prefab

        public static string BakeToPrefab(LevelDesignBoard board, string levelName, string folder)
        {
            var lines = board.ToCenteredLineData();
            if (lines.Count == 0)
            {
                Debug.LogError("[LevelDesigner] Nothing to bake — board has no arrows.");
                return null;
            }

            var template = Resources.Load<Level>(TemplateResource);
            var linePrefab = Resources.Load<GameLine>(LinePrefabResource);
            if (template == null || linePrefab == null)
            {
                Debug.LogError("[LevelDesigner] Missing Level 10 template or Line prefab in Resources.");
                return null;
            }

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var instance = PrefabUtility.InstantiatePrefab(template) as Level;
            if (instance == null)
            {
                Debug.LogError("[LevelDesigner] Failed to instantiate template.");
                return null;
            }

            string path = $"{folder}/{levelName}.prefab";
            try
            {
                instance.gameObject.name = levelName;
                var linesParent = instance.transform.Find("LINES");
                if (linesParent == null)
                {
                    Debug.LogError("[LevelDesigner] Template missing LINES child.");
                    return null;
                }

                for (int i = linesParent.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(linesParent.GetChild(i).gameObject);

                for (int i = 0; i < lines.Count; i++)
                {
                    var data = lines[i];
                    var line = PrefabUtility.InstantiatePrefab(linePrefab, linesParent) as GameLine;
                    if (line == null) continue;
                    line.name = $"Line ({i + 1})";
                    var renderer = line.LineRenderer;
                    if (renderer == null) continue;
                    line.transform.localPosition = Vector3.zero;
                    line.transform.localRotation = Quaternion.identity;
                    renderer.positionCount = data.PointCount;
                    for (int p = 0; p < data.PointCount; p++)
                        renderer.SetPosition(p, new Vector3(data.Points[p].X, data.Points[p].Y, 0f));
                    AlignHead(line, data);
                }

                ApplyLineOverrides(linesParent);
                ConfigureBackground(instance.transform.Find("Background"));

                PrefabUtility.SaveAsPrefabAsset(instance.gameObject, path);
                AssetDatabase.Refresh();
                Debug.Log($"[LevelDesigner] Saved {path} | arrows={lines.Count}");
                return path;
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        static void AlignHead(GameLine line, LevelLineData data)
        {
            if (data.PointCount < 2) return;
            var head = line.transform.Find("Head (1)");
            if (head == null) return;
            var last = data.Points[data.PointCount - 1];
            var prev = data.Points[data.PointCount - 2];
            head.localPosition = new Vector3(last.X, last.Y, 0f);
            float angle = Mathf.Atan2(last.Y - prev.Y, last.X - prev.X) * Mathf.Rad2Deg - 90f;
            head.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        static void ApplyLineOverrides(Transform linesParent)
        {
            foreach (var line in linesParent.GetComponentsInChildren<GameLine>(true))
            {
                line.transform.localScale = Vector3.one * LineScale;
                var snap = line.GetComponent<LineRendererSnapFixer>();
                if (snap != null) snap.enabled = false;
                var spawner = line.GetComponent<LineSegmentColliderSpawner2D>();
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    var thickness = so.FindProperty("thickness");
                    if (thickness != null) { thickness.floatValue = LineThickness; so.ApplyModifiedPropertiesWithoutUndo(); }
                }
            }
        }

        static void ConfigureBackground(Transform bg)
        {
            if (bg == null) return;
            bg.localScale = BackgroundScale;
            bg.localPosition = BackgroundPosition;
        }

        // ---------------------------------------------------------------- Load from prefab

        public static LevelDesignBoard LoadFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            var root = prefab.transform.Find("LINES");
            if (root == null)
            {
                var level = prefab.GetComponentInChildren<Level>(true);
                if (level != null) root = level.transform.Find("LINES");
            }
            if (root == null)
            {
                Debug.LogError("[LevelDesigner] Prefab has no LINES child — not a Go-Arrow level.");
                return null;
            }

            var raw = new List<List<Vector2Int>>();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
            {
                int n = lr.positionCount;
                if (n < 2) continue;
                var pts = new List<Vector2Int>(n);
                for (int i = 0; i < n; i++)
                {
                    var w = lr.GetPosition(i);
                    var g = new Vector2Int(Mathf.RoundToInt(w.x), Mathf.RoundToInt(w.y));
                    pts.Add(g);
                    minX = Mathf.Min(minX, g.x); minY = Mathf.Min(minY, g.y);
                    maxX = Mathf.Max(maxX, g.x); maxY = Mathf.Max(maxY, g.y);
                }
                raw.Add(pts);
            }

            if (raw.Count == 0) return null;

            int pad = 3;
            int w2 = (maxX - minX) + pad * 2 + 1;
            int h2 = (maxY - minY) + pad * 2 + 1;
            var board = new LevelDesignBoard { Width = Mathf.Max(w2, 7), Height = Mathf.Max(h2, 7) };
            foreach (var pts in raw)
            {
                var arrow = new DesignArrow();
                foreach (var g in pts)
                    arrow.Points.Add(new Vector2Int(g.x - minX + pad, g.y - minY + pad));
                board.Arrows.Add(arrow);
            }
            return board;
        }

        // ---------------------------------------------------------------- Image silhouette

        /// <summary>Threshold an image into a board-sized mask (dark or opaque pixels = inside).
        /// Supersamples SS×SS sub-samples per cell and keeps the cell when coverage ≥ 40%, so
        /// curved silhouette edges land cleanly on the grid instead of stair-stepping.</summary>
        public static bool[,] ImageToMask(Texture2D tex, int width, int height, float darkThreshold)
        {
            var mask = new bool[width, height];
            if (tex == null || !tex.isReadable) return mask;

            const int SS = 4;                      // 4×4 = 16 sub-samples per cell
            const float coverage = 0.40f;          // fraction of sub-samples that must be "inside"
            int need = Mathf.CeilToInt(SS * SS * coverage);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (x + (sx + 0.5f) / SS) / width;
                    float v = (y + (sy + 0.5f) / SS) / height;
                    var c = tex.GetPixelBilinear(u, v);
                    float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    if (c.a > 0.5f && lum <= darkThreshold) hits++;
                }
                mask[x, y] = hits >= need;
            }
            return mask;
        }

        /// <summary>Fill an image silhouette with a solvable layout of the requested size.</summary>
        public static LevelDesignBoard ImageAutoFill(LevelDesignBoard board, Texture2D tex, int count,
            float threshold, int seedBase)
        {
            count = Mathf.Clamp(count, 2, 16);
            int grid = Mathf.Clamp(MakeOdd(count + 9), 11, 21);
            var mask = ImageToMask(tex, grid, grid, threshold);

            var template = TemplateLevelGenerator.LoadTemplateLines(10);
            if (template != null && template.Count > 0)
            {
                for (int seed = 0; seed < 6; seed++)
                {
                    var fitted = DenseShapeConformer.FitToTarget(template, mask, count, seedBase + seed * 31);
                    if (fitted != null && fitted.Count == count && LevelSolvabilityValidator.IsSolvable(fitted))
                        return BoardFromLines(board.Width, board.Height, fitted);
                }
            }

            // Fallback: match the image to the nearest built-in shape and fill that.
            var shape = NearestShape(mask, grid);
            return AutoFill(board, shape, count, DesignDifficulty.Medium, seedBase);
        }

        /// <summary>Pick the built-in ShapeType whose silhouette best overlaps the image mask.</summary>
        public static ShapeType NearestShape(bool[,] imageMask, int grid)
        {
            ShapeType best = ShapeType.Circle;
            float bestIoU = -1f;
            foreach (ShapeType s in System.Enum.GetValues(typeof(ShapeType)))
            {
                var sm = ShapeMaskGenerator.CreateMask(s, grid);
                int inter = 0, union = 0;
                int w = Mathf.Min(imageMask.GetLength(0), sm.GetLength(0));
                int h = Mathf.Min(imageMask.GetLength(1), sm.GetLength(1));
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool a = imageMask[x, y], b = sm[x, y];
                    if (a && b) inter++;
                    if (a || b) union++;
                }
                float iou = union == 0 ? 0f : inter / (float)union;
                if (iou > bestIoU) { bestIoU = iou; best = s; }
            }
            return best;
        }

        // ---------------------------------------------------------------- Helpers

        static LevelDesignBoard BoardFromLines(int width, int height, IReadOnlyList<LevelLineData> lines)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var l in lines)
                foreach (var p in l.Points)
                {
                    minX = Mathf.Min(minX, p.X); minY = Mathf.Min(minY, p.Y);
                    maxX = Mathf.Max(maxX, p.X); maxY = Mathf.Max(maxY, p.Y);
                }

            int spanX = maxX - minX, spanY = maxY - minY;
            int w = Mathf.Max(width, spanX + 7);
            int h = Mathf.Max(height, spanY + 7);
            int offX = (w - spanX) / 2 - minX;
            int offY = (h - spanY) / 2 - minY;

            var board = new LevelDesignBoard { Width = w, Height = h };
            foreach (var l in lines)
            {
                if (l.PointCount < 2) continue;
                var arrow = new DesignArrow();
                foreach (var p in l.Points)
                    arrow.Points.Add(new Vector2Int(p.X + offX, p.Y + offY));
                board.Arrows.Add(arrow);
            }
            return board;
        }

        static int MakeOdd(int v) => (v & 1) == 0 ? v + 1 : v;

        /// <summary>
        /// Fast, dense, NON-OVERLAPPING, solvable-by-construction fill. Each arrow is a vertical
        /// 2-cell arrow; columns are packed independently and alternate up/down. No two arrows share
        /// a cell (no overlap). Within a column all arrows face the same way, so a peel order always
        /// exists (top-down for up columns, bottom-up for down columns); adjacent columns are a full
        /// unit apart so they never block each other. With a mask, only cells inside the silhouette
        /// are used (conform to shape/image); without one, a tight square block is packed (max density).
        /// </summary>
        public static LevelDesignBoard DensePack(LevelDesignBoard board, int count, HashSet<Vector2Int> maskCells, int seed)
        {
            count = Mathf.Clamp(count, 2, 500);

            var byCol = new Dictionary<int, List<int>>();
            if (maskCells != null && maskCells.Count > 0)
            {
                foreach (var c in maskCells)
                {
                    if (!byCol.TryGetValue(c.x, out var l)) { l = new List<int>(); byCol[c.x] = l; }
                    l.Add(c.y);
                }
            }
            else
            {
                int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count)));
                int rowsArrows = Mathf.CeilToInt(count / (float)cols);
                for (int x = 0; x < cols; x++)
                {
                    var l = new List<int>(rowsArrows * 2);
                    for (int y = 0; y < rowsArrows * 2; y++) l.Add(y);
                    byCol[x] = l;
                }
            }

            var lines = new List<LevelLineData>(count);
            var cols2 = new List<int>(byCol.Keys);
            cols2.Sort();
            foreach (int col in cols2)
            {
                if (lines.Count >= count) break;
                var ys = byCol[col];
                ys.Sort();
                var yset = new HashSet<int>(ys);
                var usedY = new HashSet<int>();
                bool down = (col & 1) == 1; // alternate orientation per column for variety
                foreach (int y in ys)
                {
                    if (lines.Count >= count) break;
                    if (usedY.Contains(y) || !yset.Contains(y + 1) || usedY.Contains(y + 1)) continue;
                    usedY.Add(y); usedY.Add(y + 1);
                    var line = new LevelLineData();
                    if (down) { line.Points.Add(new GridPoint(col, y + 1)); line.Points.Add(new GridPoint(col, y)); }
                    else { line.Points.Add(new GridPoint(col, y)); line.Points.Add(new GridPoint(col, y + 1)); }
                    lines.Add(line);
                }
            }

            return BoardFromLines(board.Width, board.Height, lines);
        }

        /// <summary>Shapes whose silhouette has enough area to fit an exact arrow count on the
        /// builder's fast path. Thin shapes are mapped to the closest roomy one.</summary>
        static ShapeType ToFastShape(ShapeType s)
        {
            switch (s)
            {
                case ShapeType.Square:
                case ShapeType.Circle:
                case ShapeType.Diamond:
                case ShapeType.Hexagon:
                case ShapeType.Triangle:
                    return s;
                case ShapeType.Heart:
                case ShapeType.Star:
                case ShapeType.Flower:
                case ShapeType.Cake:
                case ShapeType.Cat:
                case ShapeType.Paw:
                    return ShapeType.Circle;
                case ShapeType.Cross:
                case ShapeType.Plus:
                case ShapeType.Arrow:
                case ShapeType.Rocket:
                case ShapeType.Tree:
                    return ShapeType.Diamond;
                default:
                    return ShapeType.Square;
            }
        }
    }
}
#endif
