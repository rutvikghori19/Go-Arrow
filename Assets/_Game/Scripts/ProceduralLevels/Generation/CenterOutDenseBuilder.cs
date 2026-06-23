using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Builds dense handcrafted puzzles from the center outward inside a shape mask.
    /// Uses cell + edge occupancy so arrows never visually overlap.
    /// </summary>
    public static class CenterOutDenseBuilder
    {
        static readonly Vector2Int[] Cardinals =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static List<LevelLineData> Build(ShapeType shape, int gridSize, int targetCount, int seed)
        {
            var mask = DenseShapeConformer.CreateMask(shape, gridSize);
            int center = (gridSize - 1) / 2;
            var rings = BuildRings(mask, center);
            if (rings.Inner.Count == 0)
                return null;

            int maxAttempts = targetCount > 50 ? 320 : 200;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var rng = new System.Random(seed + attempt * 173);
                var lines = TryBuild(mask, center, rings, targetCount, rng);
                if (lines == null)
                    continue;

                if (!LevelSolvabilityValidator.IsSolvable(lines))
                    continue;

                if (!LevelClusterValidator.IsTightCluster(lines))
                    continue;

                if (DenseShapeConformer.HasOverlappingEdges(lines))
                    continue;

                if (HasOverlappingCells(lines))
                    continue;

                if (!AllInside(mask, lines))
                    continue;

                if (!HasDenseCore(lines, targetCount))
                    continue;

                return lines;
            }

            return null;
        }

        static List<LevelLineData> TryBuild(
            bool[,] mask,
            int center,
            RingAnchors rings,
            int targetCount,
            System.Random rng)
        {
            var allAnchors = new List<Anchor>();
            allAnchors.AddRange(rings.Inner);
            allAnchors.AddRange(rings.Mid);
            allAnchors.AddRange(rings.Outer);
            if (allAnchors.Count == 0)
                return null;

            var lines = new List<LevelLineData>();
            var occupiedEdges = new HashSet<long>();
            var occupiedCells = new HashSet<long>();

            for (int i = 0; i < targetCount; i++)
            {
                bool success = false;
                for (int tryPlace = 0; tryPlace < 2000 && !success; tryPlace++)
                {
                    Anchor? anchor = PickAnchor(allAnchors, occupiedCells, rng);
                    if (!anchor.HasValue)
                        continue;

                    float dist = anchor.Value.Distance;
                    int ringIndex = dist <= rings.InnerMaxDist ? 0 : dist <= rings.MidMaxDist ? 1 : 2;
                    int segments = PickSegmentCount(rng, ringIndex);
                    if (!TryGrowLine(mask, center, anchor.Value.Position, segments, occupiedEdges, occupiedCells, rng, out var line))
                        continue;

                    var trial = new List<LevelLineData>(lines) { line };
                    if (!IsOnlyRemovable(trial, trial.Count - 1))
                        continue;

                    RegisterOccupancy(line, occupiedEdges, occupiedCells);
                    lines.Add(line);
                    success = true;
                }

                if (!success)
                    return null;
            }

            return lines;
        }

        static Anchor? PickAnchor(
            List<Anchor> anchors,
            HashSet<long> occupiedCells,
            System.Random rng)
        {
            var candidates = new List<Anchor>();
            var weights = new List<float>();

            foreach (var a in anchors)
            {
                if (occupiedCells.Contains(PackCell(a.Position)))
                    continue;

                candidates.Add(a);
                float centerWeight = 4f / (0.35f + a.Distance * a.Distance);
                int adjacent = CountAdjacentOccupied(a.Position, occupiedCells);
                float packWeight = occupiedCells.Count == 0
                    ? centerWeight
                    : (1f + adjacent * 1.8f) * centerWeight;
                weights.Add(packWeight);
            }

            if (candidates.Count == 0)
                return null;

            float total = 0f;
            foreach (float w in weights)
                total += w;

            float roll = (float)rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        static bool IsOnlyRemovable(List<LevelLineData> lines, int newIndex)
        {
            var removable = LevelSolvabilityValidator.GetRemovableIndices(lines);
            return removable.Count == 1 && removable[0] == newIndex;
        }

        static int CountAdjacentOccupied(Vector2Int pos, HashSet<long> occupiedCells)
        {
            int count = 0;
            foreach (var dir in Cardinals)
            {
                if (occupiedCells.Contains(PackCell(pos + dir)))
                    count++;
            }

            return count;
        }

        static int PickSegmentCount(System.Random rng, int ringIndex)
        {
            double roll = rng.NextDouble();
            if (ringIndex == 0)
            {
                if (roll < 0.45) return 1;
                if (roll < 0.85) return rng.Next(2, 3);
                return rng.Next(3, 5);
            }

            if (roll < 0.25) return 1;
            if (roll < 0.60) return rng.Next(2, 4);
            if (roll < 0.85) return rng.Next(4, 6);
            return rng.Next(6, 8);
        }

        static bool TryGrowLine(
            bool[,] mask,
            int center,
            Vector2Int head,
            int segments,
            HashSet<long> occupiedEdges,
            HashSet<long> occupiedCells,
            System.Random rng,
            out LevelLineData line)
        {
            line = null;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                Vector2Int exitDir = Cardinals[rng.Next(Cardinals.Length)];
                var points = new List<GridPoint> { new GridPoint(head.x, head.y) };
                var visited = new HashSet<Vector2Int> { head };

                Vector2Int current = head - exitDir;
                if (!IsInsideMask(mask, center, current) || visited.Contains(current))
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
                        if (occupiedCells.Contains(PackCell(next)))
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
                if (!ConflictsOccupancy(line, occupiedEdges, occupiedCells))
                    return true;
            }

            line = null;
            return false;
        }

        struct Anchor
        {
            public Vector2Int Position;
            public float Distance;
        }

        sealed class RingAnchors
        {
            public List<Anchor> Inner = new List<Anchor>();
            public List<Anchor> Mid = new List<Anchor>();
            public List<Anchor> Outer = new List<Anchor>();
            public float InnerMaxDist;
            public float MidMaxDist;
        }

        static RingAnchors BuildRings(bool[,] mask, int center)
        {
            var rings = new RingAnchors();
            float maxDist = 0f;
            var all = new List<Anchor>();

            int size = mask.GetLength(0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!mask[x, y])
                        continue;

                    var pos = new Vector2Int(x - center, y - center);
                    float dist = pos.magnitude;
                    if (dist > maxDist)
                        maxDist = dist;

                    all.Add(new Anchor { Position = pos, Distance = dist });
                }
            }

            if (maxDist < 0.001f)
                return rings;

            foreach (var a in all)
            {
                float t = a.Distance / maxDist;
                if (t <= 0.38f)
                    rings.Inner.Add(a);
                else if (t <= 0.72f)
                    rings.Mid.Add(a);
                else
                    rings.Outer.Add(a);
            }

            rings.InnerMaxDist = maxDist * 0.38f;
            rings.MidMaxDist = maxDist * 0.72f;

            return rings;
        }

        static bool HasDenseCore(IReadOnlyList<LevelLineData> lines, int targetCount)
        {
            int corePoints = 0;
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    if (Mathf.Abs(p.X) <= 3 && Mathf.Abs(p.Y) <= 3)
                        corePoints++;
                }
            }

            int minCore = Mathf.Max(8, Mathf.RoundToInt(targetCount * 0.22f));
            return corePoints >= minCore;
        }

        public static bool HasOverlappingCellsPublic(IReadOnlyList<LevelLineData> lines) =>
            HasOverlappingCells(lines);

        static bool HasOverlappingCells(IReadOnlyList<LevelLineData> lines)
        {
            var occupied = new HashSet<long>();
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    long cell = PackCell(p.ToVector2Int());
                    if (!occupied.Add(cell))
                        return true;
                }
            }

            return false;
        }

        static bool AllInside(bool[,] mask, List<LevelLineData> lines)
        {
            foreach (var line in lines)
            {
                if (!DenseShapeConformer.IsLineInside(mask, line))
                    return false;
            }

            return true;
        }

        static bool ConflictsOccupancy(
            LevelLineData line,
            HashSet<long> occupiedEdges,
            HashSet<long> occupiedCells)
        {
            for (int i = 0; i < line.PointCount; i++)
            {
                if (occupiedCells.Contains(PackCell(line.Points[i].ToVector2Int())))
                    return true;
            }

            for (int i = 0; i < line.PointCount - 1; i++)
            {
                var a = line.Points[i].ToVector2Int();
                var b = line.Points[i + 1].ToVector2Int();
                if (occupiedEdges.Contains(PackEdge(a, b)))
                    return true;
            }

            return false;
        }

        static void RegisterOccupancy(
            LevelLineData line,
            HashSet<long> occupiedEdges,
            HashSet<long> occupiedCells)
        {
            for (int i = 0; i < line.PointCount; i++)
                occupiedCells.Add(PackCell(line.Points[i].ToVector2Int()));

            for (int i = 0; i < line.PointCount - 1; i++)
            {
                var a = line.Points[i].ToVector2Int();
                var b = line.Points[i + 1].ToVector2Int();
                occupiedEdges.Add(PackEdge(a, b));
            }
        }

        static bool IsInsideMask(bool[,] mask, int center, Vector2Int pos)
        {
            int mx = pos.x + center;
            int my = pos.y + center;
            return ShapeMaskGenerator.IsInsideMask(mask, mx, my);
        }

        static long PackCell(Vector2Int c) =>
            ((long)(c.x + 512) << 16) | (long)(c.y + 512);

        static long PackEdge(Vector2Int a, Vector2Int b)
        {
            if (a.x > b.x || (a.x == b.x && a.y > b.y))
                (a, b) = (b, a);

            return ((long)(a.x + 512) << 40) | ((long)(a.y + 512) << 20) |
                   ((long)(b.x + 512) << 10) | (long)(b.y + 512);
        }
    }
}
