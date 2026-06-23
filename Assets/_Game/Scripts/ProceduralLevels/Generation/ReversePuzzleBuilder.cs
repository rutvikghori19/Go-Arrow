using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Rule #1: build levels backwards from the intended removal order.
    /// Each new arrow must be the only removable arrow; bodies block later arrows.
    /// </summary>
    public static class ReversePuzzleBuilder
    {
        const int ChainSpacing = 3;

        static readonly Vector2Int[] Cardinals =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static List<LevelLineData> Build(PuzzlePlan plan, ShapeType shape, int gridSize, System.Random rng)
        {
            if (plan == null || plan.ArrowCount < 2)
                return null;

            var mask = ShapeMaskGenerator.CreateMask(shape, gridSize);

            for (int layoutTry = 0; layoutTry < 8; layoutTry++)
            {
                var slots = plan.Topology == PuzzleTopologyType.LinearChain
                    ? BuildChainSlots(plan, mask, rng, layoutTry)
                    : BuildDualColumnSlots(plan, mask, rng, layoutTry);

                var lines = TryPlaceFromSlots(plan, slots);
                if (lines != null)
                    return lines;
            }

            return null;
        }

        static List<LevelLineData> TryPlaceFromSlots(PuzzlePlan plan, List<ChainSlot> slots)
        {
            if (slots == null || slots.Count < plan.ArrowCount)
                return null;

            var lines = new List<LevelLineData>(plan.ArrowCount);
            var occupiedEdges = new HashSet<long>();
            var occupiedCells = new HashSet<long>();

            var reverseOrder = new List<int>(plan.RemovalOrder);
            reverseOrder.Reverse();

            for (int step = 0; step < reverseOrder.Count; step++)
            {
                int arrowId = reverseOrder[step];
                var slot = slots[arrowId];

                bool placed = false;
                for (int attempt = 0; attempt < 16 && !placed; attempt++)
                {
                    Vector2Int exitDir = attempt == 0 ? slot.ExitDir : Cardinals[(attempt + arrowId) % 4];
                    int bodyLen = plan.ArrowCount <= 6 ? 1 : 1 + (step % 2);

                    if (!TryBuildArrow(slot.Head, exitDir, bodyLen, occupiedEdges, occupiedCells, out var line))
                        continue;

                    var trial = new List<LevelLineData>(lines) { line };
                    if (!IsOnlyRemovable(trial, trial.Count - 1))
                        continue;

                    RegisterOccupancy(line, occupiedEdges, occupiedCells);
                    lines.Add(line);
                    placed = true;
                }

                if (!placed)
                    return null;
            }

            return MapToRemovalIndices(lines, plan.RemovalOrder);
        }

        struct ChainSlot
        {
            public Vector2Int Head;
            public Vector2Int ExitDir;
        }

        static List<ChainSlot> BuildChainSlots(PuzzlePlan plan, bool[,] mask, System.Random rng, int layoutTry)
        {
            var anchors = CollectShapeAnchors(mask);
            if (anchors.Count == 0)
                return new List<ChainSlot>();

            Vector2Int axisDir = Cardinals[(rng.Next(4) + layoutTry) % 4];
            Vector2Int start = anchors[(rng.Next(anchors.Count) + layoutTry) % anchors.Count];

            var slots = new List<ChainSlot>();
            for (int i = 0; i < plan.ArrowCount; i++)
            {
                Vector2Int head = start + axisDir * (i * ChainSpacing);
                if (!IsInsideMask(mask, head.x, head.y))
                    head = start - axisDir * (i * ChainSpacing);

                slots.Add(new ChainSlot { Head = head, ExitDir = axisDir });
            }

            return slots;
        }

        static List<ChainSlot> BuildDualColumnSlots(PuzzlePlan plan, bool[,] mask, System.Random rng, int layoutTry)
        {
            var anchors = CollectShapeAnchors(mask);
            if (anchors.Count == 0)
                return new List<ChainSlot>();

            Vector2Int axisDir = Vector2Int.up;
            Vector2Int center = anchors[(rng.Next(anchors.Count) + layoutTry) % anchors.Count];
            int columnGap = 4;
            int leftCount = (plan.ArrowCount + 1) / 2;
            int rightCount = plan.ArrowCount - leftCount;

            var slots = new List<ChainSlot>(plan.ArrowCount);
            for (int i = 0; i < plan.ArrowCount; i++)
                slots.Add(default);

            for (int i = 0; i < leftCount; i++)
            {
                Vector2Int head = center + Vector2Int.left * columnGap + axisDir * (i * ChainSpacing);
                slots[i] = new ChainSlot { Head = head, ExitDir = axisDir };
            }

            for (int i = 0; i < rightCount; i++)
            {
                int id = leftCount + i;
                Vector2Int head = center + Vector2Int.right * columnGap + axisDir * (i * ChainSpacing);
                slots[id] = new ChainSlot { Head = head, ExitDir = axisDir };
            }

            return slots;
        }

        static List<LevelLineData> MapToRemovalIndices(List<LevelLineData> built, List<int> removalOrder)
        {
            var reverseOrder = new List<int>(removalOrder);
            reverseOrder.Reverse();

            var result = new LevelLineData[removalOrder.Count];
            for (int step = 0; step < built.Count; step++)
            {
                int arrowId = reverseOrder[step];
                result[arrowId] = built[step];
            }

            return new List<LevelLineData>(result);
        }

        static bool IsOnlyRemovable(List<LevelLineData> lines, int newIndex)
        {
            var removable = LevelSolvabilityValidator.GetRemovableIndices(lines);
            return removable.Count == 1 && removable[0] == newIndex;
        }

        static bool TryBuildArrow(
            Vector2Int head,
            Vector2Int exitDir,
            int bodySegments,
            HashSet<long> occupiedEdges,
            HashSet<long> occupiedCells,
            out LevelLineData line)
        {
            line = new LevelLineData();
            var visited = new HashSet<Vector2Int> { head };
            line.Points.Add(new GridPoint(head.x, head.y));

            Vector2Int current = head - exitDir;
            line.Points.Insert(0, new GridPoint(current.x, current.y));
            visited.Add(current);

            for (int i = 1; i < bodySegments; i++)
            {
                Vector2Int next = current - exitDir;
                if (visited.Contains(next))
                {
                    line = null;
                    return false;
                }

                line.Points.Insert(0, new GridPoint(next.x, next.y));
                visited.Add(next);
                current = next;
            }

            if (ConflictsOccupancy(line, occupiedEdges, occupiedCells))
            {
                line = null;
                return false;
            }

            return true;
        }

        static List<Vector2Int> CollectShapeAnchors(bool[,] mask)
        {
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);
            int cx = w / 2;
            int cy = h / 2;
            var list = new List<Vector2Int>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (mask[x, y])
                        list.Add(new Vector2Int(x - cx, y - cy));
                }
            }

            return list;
        }

        static bool IsInsideMask(bool[,] mask, int x, int y)
        {
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);
            int mx = x + w / 2;
            int my = y + h / 2;
            if (mx < 0 || my < 0 || mx >= w || my >= h)
                return false;
            return mask[mx, my];
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
