#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor.LevelDesigner
{
    /// <summary>Zoom/pan transform for an infinite dot grid. Returns CANVAS-LOCAL points
    /// (origin = canvas top-left). +Y is up. Window-absolute = local + canvasRect.position.</summary>
    [System.Serializable]
    public class CanvasView
    {
        public Vector2 Pan = new Vector2(400f, 350f);
        public float Zoom = 26f;

        public Vector2 GridToLocal(Vector2 g) => new Vector2(Pan.x + g.x * Zoom, Pan.y - g.y * Zoom);
        public Vector2Int LocalToGrid(Vector2 l) => new Vector2Int(
            Mathf.RoundToInt((l.x - Pan.x) / Zoom), Mathf.RoundToInt((Pan.y - l.y) / Zoom));
        public Vector2 LocalToGridF(Vector2 l) => new Vector2((l.x - Pan.x) / Zoom, (Pan.y - l.y) / Zoom);
    }

    public class RenderContext
    {
        public Rect Rect;
        public Vector2 ContentSize;   // window content size in POINTS (for the DPI-correct pixel matrix)
        public LevelDesignBoard Board;
        public CanvasView View;
        public HashSet<Vector2Int> ShapeDots;
        public HashSet<Vector2Int> ImageDots;
        public DesignAnalysis Analysis;
        public DesignArrow Drawing;
        public HashSet<int> Removed;
        public int HoverArrow = -1;
        public Vector2Int? HoverDot;
    }

    public static class LevelDesignerRenderer
    {
        static Material _mat;

        static readonly Color ColBg = new Color(0.12f, 0.13f, 0.16f);
        static readonly Color ColDot = new Color(0.30f, 0.32f, 0.38f);
        static readonly Color ColAxis = new Color(0.42f, 0.62f, 0.85f, 0.9f);
        static readonly Color ColShape = new Color(0.25f, 0.65f, 1f);
        static readonly Color ColImage = new Color(1f, 0.6f, 0.25f);
        static readonly Color ColArrow = new Color(0.30f, 0.95f, 0.85f);
        static readonly Color ColArrowHover = new Color(1f, 0.95f, 0.4f);
        static readonly Color ColStuck = new Color(1f, 0.32f, 0.32f);
        static readonly Color ColDraw = Color.white;
        static readonly Color ColRemoved = new Color(0.30f, 0.32f, 0.38f, 0.55f);

        static void EnsureMaterial()
        {
            if (_mat != null) return;
            _mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _mat.SetInt("_ZWrite", 0);
        }

        public static void Draw(RenderContext ctx)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureMaterial();

            var rect = ctx.Rect;
            var view = ctx.View;
            Vector2 o = rect.position; // canvas-local -> window-absolute offset

            // Pixel matrix spanning the WHOLE window content in points. Because the underlying
            // viewport is the full window in physical pixels, a point (px,py) lands at exactly
            // (px,py) logical points on screen at any DPI — so render space == mouse space exactly.
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Mathf.Max(1f, ctx.ContentSize.x), Mathf.Max(1f, ctx.ContentSize.y), 0);
            _mat.SetPass(0);

            FillRect(rect.x, rect.y, rect.width, rect.height, ColBg);

            // Visible grid range (infinite grid, culled to the canvas viewport).
            var c0 = view.LocalToGrid(new Vector2(0, rect.height));
            var c1 = view.LocalToGrid(new Vector2(rect.width, 0));
            int minX = Mathf.Min(c0.x, c1.x) - 1, maxX = Mathf.Max(c0.x, c1.x) + 1;
            int minY = Mathf.Min(c0.y, c1.y) - 1, maxY = Mathf.Max(c0.y, c1.y) + 1;
            if (maxX - minX > 400) { int m = (minX + maxX) / 2; minX = m - 200; maxX = m + 200; }
            if (maxY - minY > 400) { int m = (minY + maxY) / 2; minY = m - 200; maxY = m + 200; }

            float dotSize = Mathf.Max(1.5f, view.Zoom * 0.09f);
            GL.Begin(GL.QUADS);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var key = new Vector2Int(x, y);
                var p = o + view.GridToLocal(new Vector2(x, y));
                Color c = ColDot; float s = dotSize;
                if (ctx.ShapeDots != null && ctx.ShapeDots.Contains(key)) { c = ColShape; s = dotSize * 2f; }
                else if (ctx.ImageDots != null && ctx.ImageDots.Contains(key)) { c = ColImage; s = dotSize * 2f; }
                else if (x == 0 || y == 0) c = ColAxis;
                else if (x % 5 == 0 && y % 5 == 0) { c = ColDot; s = dotSize * 1.5f; }
                QuadAt(p, s, c);
            }
            if (ctx.HoverDot.HasValue) QuadAt(o + view.GridToLocal(ctx.HoverDot.Value), dotSize * 2.6f, ColDraw);
            GL.End();

            for (int i = 0; i < ctx.Board.Arrows.Count; i++)
            {
                if (ctx.Removed != null && ctx.Removed.Contains(i)) { DrawArrow(ctx.Board.Arrows[i], view, o, ColRemoved, false); continue; }
                Color c = ColArrow;
                if (ctx.Analysis != null && !ctx.Analysis.Solvable && ctx.Analysis.StuckArrows != null && ctx.Analysis.StuckArrows.Contains(i)) c = ColStuck;
                if (i == ctx.HoverArrow) c = ColArrowHover;
                DrawArrow(ctx.Board.Arrows[i], view, o, c, true);
            }

            if (ctx.Drawing != null && ctx.Drawing.PointCount >= 1)
                DrawArrow(ctx.Drawing, view, o, ColDraw, ctx.Drawing.PointCount >= 2);

            GL.PopMatrix();
        }

        static void DrawArrow(DesignArrow arrow, CanvasView view, Vector2 o, Color color, bool drawHead)
        {
            // Clip at the canvas's left edge (local.x < 0 == under the side panel) so arrows never
            // render over the menu UI. Pure coordinate check — no GL.Viewport/BeginClip, so this
            // does NOT reintroduce the DPI/pointer-offset bug.
            float w = Mathf.Max(2.5f, view.Zoom * 0.13f);
            GL.Begin(GL.QUADS);
            for (int i = 0; i < arrow.PointCount - 1; i++)
            {
                var la = view.GridToLocal(arrow.Points[i]);
                var lb = view.GridToLocal(arrow.Points[i + 1]);
                if (la.x < 0f || lb.x < 0f) continue; // segment enters the panel zone — skip
                ThickSegment(o + la, o + lb, w, color);
            }
            if (arrow.PointCount >= 1)
            {
                var l0 = view.GridToLocal(arrow.Points[0]);
                if (l0.x >= 0f) QuadAt(o + l0, w * 1.7f, color); // tail dot
            }
            GL.End();

            if (drawHead && arrow.PointCount >= 2)
            {
                var lh = view.GridToLocal(arrow.Head);
                if (lh.x >= 0f)
                {
                    var head = o + lh;
                    var prev = o + view.GridToLocal(arrow.Points[arrow.PointCount - 2]);
                    DrawHead(head, (head - prev).normalized, Mathf.Max(8f, view.Zoom * 0.45f), color);
                }
            }
        }

        static void DrawHead(Vector2 tip, Vector2 dir, float size, Color color)
        {
            if (dir == Vector2.zero) dir = Vector2.up;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 b1 = tip - dir * size + perp * (size * 0.55f);
            Vector2 b2 = tip - dir * size - perp * (size * 0.55f);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(tip.x, tip.y, 0); GL.Vertex3(b1.x, b1.y, 0); GL.Vertex3(b2.x, b2.y, 0);
            GL.End();
        }

        static void ThickSegment(Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            Vector2 n = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            GL.Color(color);
            GL.Vertex3(a.x - n.x, a.y - n.y, 0);
            GL.Vertex3(a.x + n.x, a.y + n.y, 0);
            GL.Vertex3(b.x + n.x, b.y + n.y, 0);
            GL.Vertex3(b.x - n.x, b.y - n.y, 0);
        }

        static void QuadAt(Vector2 p, float size, Color color)
        {
            float h = size * 0.5f;
            GL.Color(color);
            GL.Vertex3(p.x - h, p.y - h, 0);
            GL.Vertex3(p.x + h, p.y - h, 0);
            GL.Vertex3(p.x + h, p.y + h, 0);
            GL.Vertex3(p.x - h, p.y + h, 0);
        }

        static void FillRect(float x, float y, float w, float h, Color color)
        {
            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex3(x, y, 0); GL.Vertex3(x + w, y, 0); GL.Vertex3(x + w, y + h, 0); GL.Vertex3(x, y + h, 0);
            GL.End();
        }
    }
}
#endif
