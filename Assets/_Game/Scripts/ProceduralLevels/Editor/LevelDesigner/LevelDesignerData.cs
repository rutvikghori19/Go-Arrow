#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor.LevelDesigner
{
    /// <summary>A single authored arrow: an ordered polyline of grid dots. Tail = first dot
    /// (where you start drawing), Head = last dot (the pointy end / launch direction).</summary>
    [Serializable]
    public class DesignArrow
    {
        public List<Vector2Int> Points = new List<Vector2Int>();

        public int PointCount => Points?.Count ?? 0;
        public Vector2Int Tail => Points[0];
        public Vector2Int Head => Points[Points.Count - 1];

        public Vector2Int HeadDir()
        {
            if (PointCount < 2) return Vector2Int.up;
            var a = Points[Points.Count - 2];
            var b = Points[Points.Count - 1];
            return new Vector2Int(b.x - a.x, b.y - a.y);
        }

        public DesignArrow Clone()
        {
            var c = new DesignArrow();
            c.Points = new List<Vector2Int>(Points);
            return c;
        }
    }

    public enum SymmetryMode { None, Horizontal, Vertical, Both }

    public enum DesignDifficulty { Easy, Medium, Hard, Expert }

    /// <summary>The full editable level: a W×H dot grid plus the authored arrows.</summary>
    [Serializable]
    public class LevelDesignBoard
    {
        // The grid is conceptually infinite; Width/Height are kept only so generation helpers can
        // centre their output. Authoring is unbounded — InBounds is always true.
        public int Width = 41;
        public int Height = 41;
        public List<DesignArrow> Arrows = new List<DesignArrow>();

        public bool InBounds(Vector2Int p) => true;

        public LevelDesignBoard Clone()
        {
            var b = new LevelDesignBoard { Width = Width, Height = Height };
            b.Arrows = new List<DesignArrow>(Arrows.Count);
            foreach (var a in Arrows)
                b.Arrows.Add(a.Clone());
            return b;
        }

        /// <summary>Solver-space lines (raw grid coords; spacing = 1 unit, matching the
        /// validator's 0.42 body / 0.35 ray-step constants).</summary>
        public List<LevelLineData> ToLineData()
        {
            var list = new List<LevelLineData>(Arrows.Count);
            foreach (var arrow in Arrows)
            {
                if (arrow.PointCount < 2) continue;
                var line = new LevelLineData();
                foreach (var p in arrow.Points)
                    line.Points.Add(new GridPoint(p.x, p.y));
                list.Add(line);
            }
            return list;
        }

        /// <summary>Bake-space lines, centred on origin by the arrows' bounding box so the
        /// saved prefab frames nicely regardless of grid size.</summary>
        public List<LevelLineData> ToCenteredLineData()
        {
            var lines = ToLineData();
            if (lines.Count == 0) return lines;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var l in lines)
                foreach (var p in l.Points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }

            int offX = (minX + maxX) / 2;
            int offY = (minY + maxY) / 2;
            foreach (var l in lines)
                for (int i = 0; i < l.Points.Count; i++)
                {
                    var p = l.Points[i];
                    l.Points[i] = new GridPoint(p.X - offX, p.Y - offY);
                }
            return lines;
        }

        public void Clear() => Arrows.Clear();
    }

    /// <summary>Result of analysing a board with the existing solver + difficulty systems.</summary>
    public class DesignAnalysis
    {
        public bool Solvable;
        public int ArrowCount;
        public int StartMoves;       // arrows removable on the opening
        public int Decisions;        // branch points along a greedy solve
        public List<int> Order;      // full peel order (the "AI solution"), or null
        public List<int> StuckArrows = new List<int>(); // arrows that can never exit
        public DesignDifficulty Difficulty;
        public int DifficultyScore;
        public bool NotChecked; // true when the level is too large to fully validate cheaply

        public string DifficultyLabel => NotChecked ? "Not auto-checked (large)" : Solvable ? Difficulty.ToString() : "UNSOLVABLE";
    }
}
#endif
