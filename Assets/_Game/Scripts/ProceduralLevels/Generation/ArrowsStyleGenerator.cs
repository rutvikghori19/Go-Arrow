using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class ArrowsStyleGenerator
    {
        public static LevelDefinition Generate(int levelNumber)
        {
            var profile = DifficultyProfile.ForLevel(levelNumber);
            int seed = levelNumber * 7919 + 104729;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                var rng = new System.Random(seed + attempt * 1337);
                var lines = BuildLayout(profile, attempt);
                if (lines == null || lines.Count < 2)
                    continue;

                if (!LevelSolvabilityValidator.IsSolvable(lines))
                    continue;

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

        static List<LevelLineData> BuildLayout(DifficultyProfile profile, int attempt)
        {
            int level = profile.LevelNumber;
            int body = level <= 40 ? 1 : level <= 200 ? 2 : 3;

            if (level <= 2)
                return BuildTutorial(level, body);

            if (level <= 12)
                return BuildIntroRow(level, body);

            if (profile.Shape == ShapeType.Cross || profile.Shape == ShapeType.Plus || level % 5 == 0)
                return BuildCross(profile, body, attempt);

            return BuildShapeLattice(profile, body, attempt);
        }

        static List<LevelLineData> BuildTutorial(int level, int body)
        {
            var lines = new List<LevelLineData>();
            if (level <= 1)
            {
                ArrowsLatticeBuilder.AddSingle(lines, 0, 0, Vector2Int.up, body);
                return lines;
            }

            ArrowsLatticeBuilder.AddVerticalPair(lines, -1, 0, body);
            return lines;
        }

        static List<LevelLineData> BuildIntroRow(int level, int body)
        {
            var lines = new List<LevelLineData>();
            int pairs = Mathf.Clamp(1 + level / 3, 1, 4);

            for (int i = 0; i < pairs; i++)
                ArrowsLatticeBuilder.AddVerticalPair(lines, i * 2 - pairs, 0, body);

            return lines;
        }

        /// <summary>
        /// Arrows-app cross: horizontal row of up/down pairs + vertical column of left/right pairs.
        /// Level 88 ≈ 3 arms → ~20 arrows (reference screenshot).
        /// </summary>
        static List<LevelLineData> BuildCross(DifficultyProfile profile, int body, int attempt)
        {
            var lines = new List<LevelLineData>();
            int level = profile.LevelNumber;
            int spacing = 2;

            int sidePairs = Mathf.Clamp(1 + (level - 1) / 30 + attempt / 8, 1, 12);

            for (int i = -sidePairs; i <= sidePairs; i++)
                ArrowsLatticeBuilder.AddVerticalPair(lines, i * spacing, 0, body);

            for (int j = -sidePairs; j <= sidePairs; j++)
            {
                if (j == 0)
                    continue;
                ArrowsLatticeBuilder.AddHorizontalPair(lines, 0, j * spacing, body);
            }

            if (level >= 40)
            {
                ArrowsLatticeBuilder.AddVerticalPair(lines, -1, 0, body);
                ArrowsLatticeBuilder.AddVerticalPair(lines, 1, 0, body);
            }

            TrimToTarget(lines, profile.LineCount);
            CenterLines(lines);
            return lines;
        }

        static List<LevelLineData> BuildShapeLattice(DifficultyProfile profile, int body, int attempt)
        {
            var lines = new List<LevelLineData>();
            bool[,] mask = ShapeMaskGenerator.CreateMask(profile.Shape, profile.GridSize);
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);

            int minX = w, minY = h, maxX = 0, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!mask[x, y]) continue;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            int cx = (minX + maxX) / 2;
            int cy = (minY + maxY) / 2;
            int arm = Mathf.Clamp(1 + profile.LevelNumber / 50 + attempt / 6, 1, 7);
            bool verticalPairs = (profile.LevelNumber + attempt) % 2 == 0;

            for (int i = -arm; i <= arm; i++)
            {
                for (int j = -arm; j <= arm; j++)
                {
                    int maskX = cx + i * 2;
                    int maskY = cy + j * 2;
                    int placeX = i * 2;
                    int placeY = j * 2;

                    if (verticalPairs)
                    {
                        if (!CanPlaceVerticalPair(mask, maskX, maskY))
                            continue;
                        ArrowsLatticeBuilder.AddVerticalPair(lines, placeX, placeY, body);
                    }
                    else
                    {
                        if (!CanPlaceHorizontalPair(mask, maskX, maskY))
                            continue;
                        ArrowsLatticeBuilder.AddHorizontalPair(lines, placeX, placeY, body);
                    }
                }
            }

            if (lines.Count < 4)
                return BuildCross(profile, body, attempt);

            TrimToTarget(lines, profile.LineCount);
            CenterLines(lines);
            return lines;
        }

        static bool CanPlaceVerticalPair(bool[,] mask, int x, int y)
        {
            return ShapeMaskGenerator.IsInsideMask(mask, x, y) &&
                   ShapeMaskGenerator.IsInsideMask(mask, x + 1, y);
        }

        static bool CanPlaceHorizontalPair(bool[,] mask, int x, int y)
        {
            return ShapeMaskGenerator.IsInsideMask(mask, x, y) &&
                   ShapeMaskGenerator.IsInsideMask(mask, x, y + 1);
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

        static void TrimToTarget(List<LevelLineData> lines, int target)
        {
            while (lines.Count > target && lines.Count > 2)
                lines.RemoveAt(lines.Count - 1);
        }

        static LevelDefinition BuildFallback(DifficultyProfile profile)
        {
            var lines = BuildCross(profile, 1, 0);
            return new LevelDefinition
            {
                LevelNumber = profile.LevelNumber,
                Shape = ShapeType.Cross,
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
