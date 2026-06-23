using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Builds dense solvable mazes inside shape masks from the Level 10 template.
    /// </summary>
    public static class DenseShapeLevelBuilder
    {
        public static List<LevelLineData> Build(ShapeType shape, int gridSize, int targetCount, int seed)
        {
            var template = TemplateLevelGenerator.LoadTemplateLines(10);
            if (template == null || template.Count == 0)
                return CenterOutDenseBuilder.Build(shape, gridSize, targetCount, seed);

            var mask = DenseShapeConformer.CreateMask(shape, gridSize);

            for (int variant = 0; variant < 24; variant++)
            {
                var candidate = TransformLines(template, variant);
                CenterLines(candidate);
                var fitted = DenseShapeConformer.FitToTarget(candidate, mask, targetCount, seed + variant * 31);
                if (fitted == null || fitted.Count != targetCount)
                    continue;

                if (!LevelSolvabilityValidator.IsSolvable(fitted))
                    continue;

                if (!LevelClusterValidator.IsTightCluster(fitted))
                    continue;

                if (DenseShapeConformer.HasOverlappingEdges(fitted))
                    continue;

                if (CenterOutDenseBuilder.HasOverlappingCellsPublic(fitted))
                    continue;

                if (!HasDenseCore(fitted, targetCount))
                    continue;

                return fitted;
            }

            return CenterOutDenseBuilder.Build(shape, gridSize, targetCount, seed);
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

        static void CenterLines(List<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    minX = Mathf.Min(minX, p.X);
                    maxX = Mathf.Max(maxX, p.X);
                    minY = Mathf.Min(minY, p.Y);
                    maxY = Mathf.Max(maxY, p.Y);
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

        static List<LevelLineData> TransformLines(List<LevelLineData> source, int variant)
        {
            bool mirrorX = (variant & 1) == 1;
            bool mirrorY = (variant & 2) == 2;
            bool swapAxes = (variant & 4) == 4;
            var result = new List<LevelLineData>(source.Count);

            foreach (var src in source)
            {
                var copy = new LevelLineData();
                foreach (var p in src.Points)
                {
                    int x = p.X;
                    int y = p.Y;
                    if (swapAxes)
                        (x, y) = (y, x);
                    if (mirrorX)
                        x = -x;
                    if (mirrorY)
                        y = -y;
                    copy.Points.Add(new GridPoint(x, y));
                }

                result.Add(copy);
            }

            return result;
        }
    }
}
