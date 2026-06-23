using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Ensures dense levels stay one tight cluster with no detached peripheral arrows.
    /// </summary>
    public static class LevelClusterValidator
    {
        public static bool IsTightCluster(IReadOnlyList<LevelLineData> lines, float maxSpread = 18f, int maxIsolationGap = 2)
        {
            if (lines == null || lines.Count == 0)
                return false;

            if (FarthestPointDistance(lines) > maxSpread)
                return false;

            return !HasIsolatedLines(lines, maxIsolationGap);
        }

        static bool HasIsolatedLines(IReadOnlyList<LevelLineData> lines, int maxGap)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                bool nearOther = false;
                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (MinDistanceBetweenLines(lines[i], lines[j]) <= maxGap)
                    {
                        nearOther = true;
                        break;
                    }
                }

                if (!nearOther)
                    return true;
            }

            return false;
        }

        static int MinDistanceBetweenLines(LevelLineData a, LevelLineData b)
        {
            int min = int.MaxValue;
            foreach (var pa in a.Points)
            {
                foreach (var pb in b.Points)
                {
                    int d = Mathf.Abs(pa.X - pb.X) + Mathf.Abs(pa.Y - pb.Y);
                    if (d < min)
                        min = d;
                }
            }

            return min;
        }

        public static bool AreAllEdgesConnected(IReadOnlyList<LevelLineData> lines)
        {
            var edges = new List<(Vector2Int a, Vector2Int b)>();
            foreach (var line in lines)
            {
                for (int i = 0; i < line.PointCount - 1; i++)
                {
                    var a = line.Points[i].ToVector2Int();
                    var b = line.Points[i + 1].ToVector2Int();
                    edges.Add((a, b));
                }
            }

            if (edges.Count == 0)
                return false;

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited.Add(0);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                var edge = edges[current];
                for (int i = 0; i < edges.Count; i++)
                {
                    if (visited.Contains(i))
                        continue;

                    var other = edges[i];
                    if (EdgesTouch(edge.a, edge.b, other.a, other.b))
                    {
                        visited.Add(i);
                        queue.Enqueue(i);
                    }
                }
            }

            return visited.Count == edges.Count;
        }

        static bool EdgesTouch(Vector2Int a1, Vector2Int b1, Vector2Int a2, Vector2Int b2)
        {
            if (a1 == a2 || a1 == b2 || b1 == a2 || b1 == b2)
                return true;

            if (a1.x == b1.x && a2.x == b2.x && a1.x == a2.x)
            {
                int min1 = Mathf.Min(a1.y, b1.y);
                int max1 = Mathf.Max(a1.y, b1.y);
                int min2 = Mathf.Min(a2.y, b2.y);
                int max2 = Mathf.Max(a2.y, b2.y);
                return max1 >= min2 - 1 && max2 >= min1 - 1;
            }

            if (a1.y == b1.y && a2.y == b2.y && a1.y == a2.y)
            {
                int min1 = Mathf.Min(a1.x, b1.x);
                int max1 = Mathf.Max(a1.x, b1.x);
                int min2 = Mathf.Min(a2.x, b2.x);
                int max2 = Mathf.Max(a2.x, b2.x);
                return max1 >= min2 - 1 && max2 >= min1 - 1;
            }

            return false;
        }

        public static bool ArePointsWithinCoreBounds(IReadOnlyList<LevelLineData> lines, int margin)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            var allPoints = new List<Vector2Int>();

            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    allPoints.Add(p.ToVector2Int());
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            int coreMinX = int.MaxValue, coreMinY = int.MaxValue;
            int coreMaxX = int.MinValue, coreMaxY = int.MinValue;
            Vector2 centroid = Vector2.zero;
            foreach (var p in allPoints)
                centroid += new Vector2(p.x, p.y);
            centroid /= allPoints.Count;

            foreach (var p in allPoints)
            {
                float dist = Vector2.Distance(centroid, new Vector2(p.x, p.y));
                if (dist > 8f)
                    continue;

                if (p.x < coreMinX) coreMinX = p.x;
                if (p.y < coreMinY) coreMinY = p.y;
                if (p.x > coreMaxX) coreMaxX = p.x;
                if (p.y > coreMaxY) coreMaxY = p.y;
            }

            if (coreMinX == int.MaxValue)
                return true;

            coreMinX -= margin;
            coreMinY -= margin;
            coreMaxX += margin;
            coreMaxY += margin;

            foreach (var p in allPoints)
            {
                if (p.x < coreMinX || p.x > coreMaxX || p.y < coreMinY || p.y > coreMaxY)
                    return false;
            }

            return true;
        }

        public static float FarthestPointDistance(IReadOnlyList<LevelLineData> lines)
        {
            Vector2 centroid = Vector2.zero;
            int count = 0;
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    centroid += new Vector2(p.X, p.Y);
                    count++;
                }
            }

            if (count == 0)
                return 0f;

            centroid /= count;
            float maxDist = 0f;
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    float d = Vector2.Distance(centroid, new Vector2(p.X, p.Y));
                    if (d > maxDist)
                        maxDist = d;
                }
            }

            return maxDist;
        }
    }
}
