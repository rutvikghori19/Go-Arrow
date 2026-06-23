using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Dense grid polylines like handcrafted Level 10: short orthogonal snakes on a tight board.
    /// </summary>
    public static class DensePolylineGenerator
    {
        static readonly Vector2Int[] Cardinals =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static LevelDefinition Generate(int levelNumber)
        {
            return Generate(levelNumber, 0);
        }

        public static LevelDefinition Generate(int levelNumber, int seedOffset)
        {
            var profile = ResolveProfile(levelNumber);
            bool dense = DenseHandcraftedProfile.IsDenseHandcraftedLevel(levelNumber);
            int maxAttempts = dense ? 50 : 32;
            int seed = levelNumber * 1543 + 27037 + seedOffset;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var rng = new System.Random(seed + attempt * 911);
                var lines = BuildLines(profile, rng);
                if (lines == null || lines.Count < 2)
                    continue;

                if (!LevelSolvabilityValidator.IsSolvable(lines))
                    continue;

                CenterLines(lines);

                return new LevelDefinition
                {
                    LevelNumber = profile.LevelNumber,
                    Shape = profile.Shape,
                    Tier = profile.Tier,
                    GridSize = profile.GridSize,
                    CellSize = ProceduralLevelConstants.DefaultCellSize,
                    DifficultyScore = profile.ComputeDifficultyScore(),
                    TargetLineCount = profile.LineCount,
                    Lines = lines
                };
            }

            return BuildFallback(profile);
        }

        public static bool TryCreateLinePublic(
            System.Random rng,
            int radius,
            int segments,
            HashSet<long> occupiedEdges,
            out LevelLineData line)
        {
            return TryCreateLine(rng, radius, segments, occupiedEdges, out line);
        }

        public static void RegisterEdgesPublic(LevelLineData line, HashSet<long> occupiedEdges)
        {
            RegisterEdges(line, occupiedEdges);
        }

        public static bool TryCreateLineInMask(
            System.Random rng,
            bool[,] mask,
            int segments,
            HashSet<long> occupiedEdges,
            out LevelLineData line)
        {
            line = null;
            if (mask == null)
                return false;

            int gridSize = mask.GetLength(0);
            int center = (gridSize - 1) / 2;
            var anchors = CollectMaskAnchors(mask, center);
            if (anchors.Count == 0)
                return false;

            for (int attempt = 0; attempt < 48; attempt++)
            {
                Vector2Int head = anchors[rng.Next(anchors.Count)];
                Vector2Int exitDir = Cardinals[rng.Next(Cardinals.Length)];

                var points = new List<GridPoint> { new GridPoint(head.x, head.y) };
                var visited = new HashSet<Vector2Int> { head };

                Vector2Int current = head - exitDir;
                if (!IsInsideMask(mask, center, current))
                    continue;

                points.Insert(0, new GridPoint(current.x, current.y));
                visited.Add(current);

                bool failed = false;
                for (int s = 1; s < segments; s++)
                {
                    var options = new List<Vector2Int>();
                    foreach (var dir in Cardinals)
                    {
                        Vector2Int next = points[0].ToVector2Int() + dir;
                        if (!IsInsideMask(mask, center, next))
                            continue;
                        if (visited.Contains(next))
                            continue;
                        options.Add(next);
                    }

                    if (options.Count == 0)
                    {
                        failed = true;
                        break;
                    }

                    Vector2Int chosen = options[rng.Next(options.Count)];
                    points.Insert(0, new GridPoint(chosen.x, chosen.y));
                    visited.Add(chosen);
                }

                if (failed)
                    continue;

                line = new LevelLineData { Points = points };
                if (!ConflictsWithOccupied(line, occupiedEdges))
                    return true;
            }

            line = null;
            return false;
        }

        static List<Vector2Int> CollectMaskAnchors(bool[,] mask, int center)
        {
            var anchors = new List<Vector2Int>();
            int size = mask.GetLength(0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!mask[x, y])
                        continue;

                    anchors.Add(new Vector2Int(x - center, y - center));
                }
            }

            return anchors;
        }

        static bool IsInsideMask(bool[,] mask, int center, Vector2Int centered)
        {
            return ShapeMaskGenerator.IsInsideMask(mask, centered.x + center, centered.y + center);
        }

        static DifficultyProfile ResolveProfile(int levelNumber)
        {
            if (DenseHandcraftedProfile.IsDenseHandcraftedLevel(levelNumber))
                return DenseHandcraftedProfile.ForLevel(levelNumber);

            return DifficultyProfile.ForLevel(levelNumber);
        }

        static List<LevelLineData> BuildLines(DifficultyProfile profile, System.Random rng)
        {
            var lines = new List<LevelLineData>();
            var occupiedEdges = new HashSet<long>();
            int radius = Mathf.Clamp(6 + profile.LineCount / 8, 8, 12);
            int target = profile.LineCount;
            int minSeg = profile.MinPathLength - 1;
            int maxSeg = profile.MaxPathLength - 1;
            int tryLimit = profile.LineCount > 20 ? 240 : 120;

            for (int i = 0; i < target; i++)
            {
                bool placed = false;
                for (int tryPlace = 0; tryPlace < tryLimit && !placed; tryPlace++)
                {
                    int segments = rng.Next(minSeg, maxSeg + 1);
                    if (!TryCreateLine(rng, radius, segments, occupiedEdges, out var line))
                        continue;

                    if (!LevelSolvabilityValidator.CanNewLineExit(lines, line))
                        continue;

                    RegisterEdges(line, occupiedEdges);
                    lines.Add(line);
                    placed = true;
                }

                if (!placed)
                    return null;
            }

            return lines;
        }

        static bool TryCreateLine(
            System.Random rng,
            int radius,
            int segments,
            HashSet<long> occupiedEdges,
            out LevelLineData line)
        {
            line = null;
            Vector2Int exitDir = Cardinals[rng.Next(Cardinals.Length)];
            Vector2Int head = new Vector2Int(
                rng.Next(-radius, radius + 1),
                rng.Next(-radius, radius + 1));

            var points = new List<GridPoint> { new GridPoint(head.x, head.y) };
            var visited = new HashSet<Vector2Int> { head };

            Vector2Int current = head - exitDir;
            if (Mathf.Abs(current.x) > radius || Mathf.Abs(current.y) > radius)
                return false;

            points.Insert(0, new GridPoint(current.x, current.y));
            visited.Add(current);

            for (int s = 1; s < segments; s++)
            {
                var options = new List<Vector2Int>();
                foreach (var dir in Cardinals)
                {
                    Vector2Int next = points[0].ToVector2Int() + dir;
                    if (Mathf.Abs(next.x) > radius || Mathf.Abs(next.y) > radius)
                        continue;
                    if (visited.Contains(next))
                        continue;
                    options.Add(next);
                }

                if (options.Count == 0)
                    return false;

                Vector2Int chosen = options[rng.Next(options.Count)];
                points.Insert(0, new GridPoint(chosen.x, chosen.y));
                visited.Add(chosen);
            }

            line = new LevelLineData { Points = points };
            return !ConflictsWithOccupied(line, occupiedEdges);
        }

        static bool ConflictsWithOccupied(LevelLineData line, HashSet<long> occupiedEdges)
        {
            for (int i = 0; i < line.PointCount - 1; i++)
            {
                var a = line.Points[i].ToVector2Int();
                var b = line.Points[i + 1].ToVector2Int();
                if (occupiedEdges.Contains(PackEdge(a, b)))
                    return true;
            }

            return false;
        }

        static void RegisterEdges(LevelLineData line, HashSet<long> occupiedEdges)
        {
            for (int i = 0; i < line.PointCount - 1; i++)
            {
                var a = line.Points[i].ToVector2Int();
                var b = line.Points[i + 1].ToVector2Int();
                occupiedEdges.Add(PackEdge(a, b));
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

        static void CenterLines(List<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            int offX = (minX + maxX) / 2;
            int offY = (minY + maxY) / 2;

            foreach (var line in lines)
            {
                for (int i = 0; i < line.Points.Count; i++)
                {
                    var p = line.Points[i];
                    line.Points[i] = new GridPoint(p.X - offX, p.Y - offY);
                }
            }
        }

        static LevelDefinition BuildFallback(DifficultyProfile profile)
        {
            var rng = new System.Random(profile.LevelNumber * 31);
            var lines = BuildLines(profile, rng) ?? new List<LevelLineData>();
            if (lines.Count == 0)
            {
                var fallback = new LevelLineData();
                fallback.Points.Add(new GridPoint(0, 0));
                fallback.Points.Add(new GridPoint(0, 1));
                lines.Add(fallback);
            }

            CenterLines(lines);
            return new LevelDefinition
            {
                LevelNumber = profile.LevelNumber,
                Shape = ShapeType.Square,
                Tier = profile.Tier,
                GridSize = profile.GridSize,
                CellSize = ProceduralLevelConstants.DefaultCellSize,
                DifficultyScore = profile.ComputeDifficultyScore(),
                TargetLineCount = profile.LineCount,
                Lines = lines
            };
        }
    }
}
