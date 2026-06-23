using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Difficulty by decision count and level band (Rules #3–#10).
    /// Levels 1–20 = handcrafted prefabs; procedural 21–100 follow bands below.
    /// </summary>
    public readonly struct DifficultyProfile
    {
        public readonly int LevelNumber;
        public readonly int GridSize;
        public readonly int LineCount;
        public readonly int MinPathLength;
        public readonly int MaxPathLength;
        public readonly ShapeType Shape;
        public readonly DifficultyTier Tier;
        public readonly float OccupancyTarget;

        public DifficultyProfile(
            int levelNumber,
            int gridSize,
            int lineCount,
            int minPathLength,
            int maxPathLength,
            ShapeType shape,
            DifficultyTier tier,
            float occupancyTarget)
        {
            LevelNumber = levelNumber;
            GridSize = gridSize;
            LineCount = lineCount;
            MinPathLength = minPathLength;
            MaxPathLength = maxPathLength;
            Shape = shape;
            Tier = tier;
            OccupancyTarget = occupancyTarget;
        }

        public static DifficultyProfile ForLevel(int levelNumber)
        {
            int level = Mathf.Clamp(levelNumber, 1, ProceduralLevelConstants.TotalLevelCount);
            int gridSize = GetGridSize(level);
            int lineCount = EstimateArrowCount(level);
            var tier = GetTier(level);
            var shape = PickShape(level);

            return new DifficultyProfile(
                level,
                gridSize,
                lineCount,
                2,
                level <= 45 ? 2 : 3,
                shape,
                tier,
                0.55f);
        }

        static int GetGridSize(int level)
        {
            if (level <= 25) return 9;
            if (level <= 45) return 11;
            if (level <= 70) return 13;
            return 15;
        }

        static int EstimateArrowCount(int level)
        {
            if (level <= 25) return Mathf.Clamp(3 + (level - 11) / 3, 3, 6);
            if (level <= 45) return Mathf.Clamp(5 + (level - 26) / 4, 5, 9);
            if (level <= 70) return Mathf.Clamp(6 + (level - 46) / 5, 6, 11);
            if (level <= 85) return Mathf.Clamp(8 + (level - 71) / 4, 8, 13);
            return Mathf.Clamp(10 + (level - 86) / 3, 10, 16);
        }

        static DifficultyTier GetTier(int level)
        {
            if (level <= 25) return DifficultyTier.Tutorial;
            if (level <= 45) return DifficultyTier.Easy;
            if (level <= 70) return DifficultyTier.Medium;
            if (level <= 85) return DifficultyTier.Hard;
            if (level <= 95) return DifficultyTier.Expert;
            return DifficultyTier.Nightmare;
        }

        static ShapeType PickShape(int level)
        {
            ShapeType[] early =
            {
                ShapeType.Plus, ShapeType.Arrow, ShapeType.Triangle, ShapeType.Diamond
            };

            ShapeType[] mid =
            {
                ShapeType.Cross, ShapeType.Plus, ShapeType.Diamond, ShapeType.Hexagon, ShapeType.Star
            };

            ShapeType[] late =
            {
                ShapeType.Cross, ShapeType.Star, ShapeType.Heart, ShapeType.Ring, ShapeType.Flower
            };

            if (level <= 25)
                return early[level % early.Length];
            if (level <= 70)
                return mid[level % mid.Length];
            return late[level % late.Length];
        }

        public int ComputeDifficultyScore()
        {
            return GridSize + LineCount * 8 + LevelNumber;
        }
    }
}
