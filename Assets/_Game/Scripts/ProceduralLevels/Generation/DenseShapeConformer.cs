using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Fits dense maze layouts into recognizable silhouettes (square, circle, heart, etc.).
    /// </summary>
    public static class DenseShapeConformer
    {
        public static bool[,] CreateMask(ShapeType shape, int gridSize)
        {
            return ShapeMaskGenerator.CreateMask(shape, gridSize);
        }

        public static int MaskCenter(int gridSize)
        {
            return (gridSize - 1) / 2;
        }

        public static bool IsPointInside(bool[,] mask, int centeredX, int centeredY)
        {
            if (mask == null)
                return false;

            int center = MaskCenter(mask.GetLength(0));
            int mx = centeredX + center;
            int my = centeredY + center;
            return ShapeMaskGenerator.IsInsideMask(mask, mx, my);
        }

        public static bool IsLineInside(bool[,] mask, LevelLineData line)
        {
            if (line == null || line.PointCount < 2)
                return false;

            foreach (var p in line.Points)
            {
                if (!IsPointInside(mask, p.X, p.Y))
                    return false;
            }

            return true;
        }

        public static void ScaleToFit(List<LevelLineData> lines, bool[,] mask, float fill = 0.88f)
        {
            if (lines == null || lines.Count == 0 || mask == null)
                return;

            int gridSize = mask.GetLength(0);
            float allowed = gridSize * fill * 0.5f;
            float maxExtent = 0f;

            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    maxExtent = Mathf.Max(maxExtent, Mathf.Abs(p.X), Mathf.Abs(p.Y));
                }
            }

            if (maxExtent <= allowed || maxExtent < 0.001f)
                return;

            float scale = allowed / maxExtent;
            if (Mathf.Abs(scale - 1f) < 0.08f)
                return;
            foreach (var line in lines)
            {
                for (int i = 0; i < line.Points.Count; i++)
                {
                    var p = line.Points[i];
                    line.Points[i] = new GridPoint(
                        Mathf.RoundToInt(p.X * scale),
                        Mathf.RoundToInt(p.Y * scale));
                }
            }
        }

        public static List<LevelLineData> ClipOutside(List<LevelLineData> lines, bool[,] mask)
        {
            var kept = new List<LevelLineData>();
            foreach (var line in lines)
            {
                if (IsLineInside(mask, line))
                    kept.Add(line);
            }

            return kept;
        }

        public static bool HasOverlappingEdges(IReadOnlyList<LevelLineData> lines)
        {
            var occupied = new HashSet<long>();
            foreach (var line in lines)
            {
                for (int i = 0; i < line.PointCount - 1; i++)
                {
                    long edge = PackEdge(line.Points[i].ToVector2Int(), line.Points[i + 1].ToVector2Int());
                    if (!occupied.Add(edge))
                        return true;
                }
            }

            return false;
        }

        public static List<LevelLineData> RemoveOverlapping(List<LevelLineData> lines)
        {
            var kept = new List<LevelLineData>();
            var occupied = new HashSet<long>();

            foreach (var line in lines)
            {
                bool conflicts = false;
                for (int i = 0; i < line.PointCount - 1; i++)
                {
                    long edge = PackEdge(line.Points[i].ToVector2Int(), line.Points[i + 1].ToVector2Int());
                    if (occupied.Contains(edge))
                    {
                        conflicts = true;
                        break;
                    }
                }

                if (conflicts)
                    continue;

                for (int i = 0; i < line.PointCount - 1; i++)
                {
                    long edge = PackEdge(line.Points[i].ToVector2Int(), line.Points[i + 1].ToVector2Int());
                    occupied.Add(edge);
                }

                kept.Add(line);
            }

            return kept;
        }

        public static List<LevelLineData> FitToTarget(
            List<LevelLineData> lines,
            bool[,] mask,
            int target,
            int seed)
        {
            var working = new List<LevelLineData>(lines);
            var rng = new System.Random(seed * 41 + 17);

            working = ClipOutside(working, mask);
            working = RemoveOverlapping(working);

            working = TrimToTarget(working, target, rng);
            if (working == null)
                return null;

            working = ExtendToTarget(working, mask, target, seed);
            if (working == null || working.Count != target)
                return null;

            if (!IsTightClusterForMask(working, mask))
                return null;

            if (HasOverlappingEdges(working))
                return null;

            if (CenterOutDenseBuilder.HasOverlappingCellsPublic(working))
                return null;

            if (!LevelSolvabilityValidator.IsSolvable(working))
                return null;

            return working;
        }

        static bool IsTightClusterForMask(List<LevelLineData> lines, bool[,] mask)
        {
            if (!LevelClusterValidator.IsTightCluster(lines))
                return false;

            foreach (var line in lines)
            {
                if (!IsLineInside(mask, line))
                    return false;
            }

            return true;
        }

        static List<LevelLineData> TrimToTarget(List<LevelLineData> lines, int target, System.Random rng)
        {
            var working = new List<LevelLineData>(lines);
            while (working.Count > target)
            {
                var removable = LevelSolvabilityValidator.GetRemovableIndices(working);
                if (removable.Count == 0)
                    return null;

                int pick = PickMostPeripheral(working, removable, rng);
                working.RemoveAt(pick);
            }

            return working;
        }

        static int PickMostPeripheral(List<LevelLineData> lines, List<int> removable, System.Random rng)
        {
            Vector2 centroid = ComputeCentroid(lines);
            int best = removable[0];
            float bestScore = float.MinValue;

            foreach (int index in removable)
            {
                float score = LineDistanceFromCentroid(lines[index], centroid);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            if (rng.NextDouble() < 0.12)
                return removable[rng.Next(removable.Count)];

            return best;
        }

        static List<LevelLineData> ExtendToTarget(List<LevelLineData> lines, bool[,] mask, int target, int seed)
        {
            var working = new List<LevelLineData>(lines);
            var occupied = new HashSet<long>();
            foreach (var line in working)
                RegisterEdges(line, occupied);

            var rng = new System.Random(seed * 23 + 97);

            while (working.Count < target)
            {
                bool placed = false;
                for (int tryPlace = 0; tryPlace < 500 && !placed; tryPlace++)
                {
                    int segments = rng.Next(1, 6);
                    if (!DensePolylineGenerator.TryCreateLineInMask(rng, mask, segments, occupied, out var line))
                        continue;

                    if (!LevelSolvabilityValidator.CanNewLineExit(working, line))
                        continue;

                    RegisterEdges(line, occupied);
                    working.Add(line);
                    placed = true;
                }

                if (!placed)
                    return null;
            }

            return working;
        }

        static void RegisterEdges(LevelLineData line, HashSet<long> occupied)
        {
            for (int i = 0; i < line.PointCount - 1; i++)
            {
                long edge = PackEdge(line.Points[i].ToVector2Int(), line.Points[i + 1].ToVector2Int());
                occupied.Add(edge);
            }
        }

        static long PackEdge(Vector2Int a, Vector2Int b)
        {
            if (a.x > b.x || (a.x == b.x && a.y > b.y))
            {
                var t = a;
                a = b;
                b = t;
            }

            return ((long)(a.x + 512) << 40) | ((long)(a.y + 512) << 20) | ((long)(b.x + 512) << 10) | (long)(b.y + 512);
        }

        static Vector2 ComputeCentroid(List<LevelLineData> lines)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    sum += new Vector2(p.X, p.Y);
                    count++;
                }
            }

            return count == 0 ? Vector2.zero : sum / count;
        }

        static float LineDistanceFromCentroid(LevelLineData line, Vector2 centroid)
        {
            float max = 0f;
            foreach (var p in line.Points)
            {
                float d = Vector2.Distance(centroid, new Vector2(p.X, p.Y));
                if (d > max)
                    max = d;
            }

            return max;
        }
    }
}
