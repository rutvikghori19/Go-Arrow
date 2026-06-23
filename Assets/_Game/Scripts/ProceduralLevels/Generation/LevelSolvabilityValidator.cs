using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class LevelSolvabilityValidator
    {
        const float RayStep = 0.35f;
        const float RayDistance = 64f;

        public static bool IsSolvable(LevelDefinition definition)
        {
            if (definition?.Lines == null || definition.Lines.Count == 0)
                return false;

            return FindRemovalOrder(definition.Lines) != null;
        }

        public static bool IsSolvable(IReadOnlyList<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return false;

            return FindRemovalOrder(lines) != null;
        }

        /// <summary>
        /// Fast check used during generation: new line must exit without hitting already-placed lines.
        /// </summary>
        public static bool CanNewLineExit(IReadOnlyList<LevelLineData> placedLines, LevelLineData candidate)
        {
            if (candidate == null || candidate.PointCount < 2)
                return false;

            if (placedLines == null || placedLines.Count == 0)
                return true;

            Vector2 head = ToVector(candidate.GetHead());
            Vector2 dir = ToVector(candidate.GetDirection());
            if (dir == Vector2.zero)
                return false;

            dir.Normalize();

            for (float dist = RayStep; dist <= RayDistance; dist += RayStep)
            {
                Vector2 sample = head + dir * dist;
                for (int i = 0; i < placedLines.Count; i++)
                {
                    if (HitsBody(placedLines[i], sample, candidate))
                        return false;
                }
            }

            return true;
        }

        public static List<int> GetRemovableIndices(IReadOnlyList<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return new List<int>();

            var all = new List<int>(lines.Count);
            for (int i = 0; i < lines.Count; i++)
                all.Add(i);

            return GetRemovableIndices(lines, all);
        }

        public static List<int> GetRemovableIndices(IReadOnlyList<LevelLineData> lines, IReadOnlyList<int> activeIndices)
        {
            var removable = new List<int>();
            if (lines == null || activeIndices == null)
                return removable;

            foreach (int index in activeIndices)
            {
                if (CanExit(lines, index, activeIndices))
                    removable.Add(index);
            }

            return removable;
        }

        public static bool IsSolvableSubset(IReadOnlyList<LevelLineData> lines, IReadOnlyList<int> activeIndices)
        {
            if (lines == null || activeIndices == null || activeIndices.Count == 0)
                return true;

            return FindRemovalOrderSubset(lines, activeIndices) != null;
        }

        static List<int> FindRemovalOrderSubset(IReadOnlyList<LevelLineData> lines, IReadOnlyList<int> activeIndices)
        {
            var remaining = new List<int>(activeIndices);
            var order = new List<int>(remaining.Count);

            while (remaining.Count > 0)
            {
                int removable = -1;
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    int lineIndex = remaining[i];
                    if (CanExit(lines, lineIndex, remaining))
                    {
                        removable = lineIndex;
                        break;
                    }
                }

                if (removable < 0)
                    return null;

                order.Add(removable);
                remaining.Remove(removable);
            }

            return order;
        }

        public static List<int> FindRemovalOrder(IReadOnlyList<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return null;

            var remaining = new List<int>();
            for (int i = 0; i < lines.Count; i++)
                remaining.Add(i);

            var order = new List<int>(lines.Count);

            while (remaining.Count > 0)
            {
                int removable = -1;
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    int lineIndex = remaining[i];
                    if (CanExit(lines, lineIndex, remaining))
                    {
                        removable = lineIndex;
                        break;
                    }
                }

                if (removable < 0)
                    return null;

                order.Add(removable);
                remaining.Remove(removable);
            }

            return order;
        }

        public static bool CanExit(IReadOnlyList<LevelLineData> lines, int lineIndex, IReadOnlyList<int> activeIndices)
        {
            if (lines == null || lineIndex < 0 || lineIndex >= lines.Count)
                return false;

            var line = lines[lineIndex];
            if (line.PointCount < 2)
                return false;

            Vector2 head = ToVector(line.GetHead());
            Vector2 dir = ToVector(line.GetDirection());
            if (dir == Vector2.zero)
                return false;

            dir.Normalize();

            for (float dist = RayStep; dist <= RayDistance; dist += RayStep)
            {
                Vector2 sample = head + dir * dist;
                foreach (int otherIndex in activeIndices)
                {
                    if (otherIndex == lineIndex)
                        continue;

                    if (HitsBody(lines[otherIndex], sample, line))
                        return false;
                }
            }

            return true;
        }

        static bool HitsBody(LevelLineData bodyLine, Vector2 point, LevelLineData movingLine)
        {
            if (bodyLine?.Points == null || bodyLine.PointCount < 2)
                return false;

            for (int i = 0; i < bodyLine.PointCount - 1; i++)
            {
                Vector2 a = ToVector(bodyLine.Points[i]);
                Vector2 b = ToVector(bodyLine.Points[i + 1]);
                if (DistancePointToSegment(point, a, b) <= 0.42f)
                    return true;
            }

            return false;
        }

        static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        static Vector2 ToVector(Vector2Int grid) => new Vector2(grid.x, grid.y);

        static Vector2 ToVector(GridPoint grid) => new Vector2(grid.X, grid.Y);
    }
}
