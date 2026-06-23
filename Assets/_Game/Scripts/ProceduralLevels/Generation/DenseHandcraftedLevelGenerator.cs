using System.Collections.Generic;

using SerapKeremGameKit._LevelSystem;

using UnityEngine;



namespace _Game.ProceduralLevels

{

    /// <summary>

    /// Builds dense handcrafted levels 11–50 inside rotating silhouettes.

    /// </summary>

    public static class DenseHandcraftedLevelGenerator

    {

        public static LevelDefinition Generate(int levelNumber)

        {

            return Generate(levelNumber, 0);

        }



        public static LevelDefinition Generate(int levelNumber, int seedOffset)

        {

            if (!DenseHandcraftedProfile.IsDenseHandcraftedLevel(levelNumber))

                return DensePolylineGenerator.Generate(levelNumber, seedOffset);



            int target = DenseHandcraftedProfile.GetArrowCount(levelNumber);

            var profile = DenseHandcraftedProfile.ForLevel(levelNumber);

            var shape = profile.Shape;

            int gridSize = profile.GridSize;

            List<LevelLineData> lines = HandcraftedBand11To20Profile.IsBandLevel(levelNumber)
                ? HandcraftedBand11To20Builder.Build(levelNumber, levelNumber + seedOffset * 17)
                : DenseShapeLevelBuilder.Build(shape, gridSize, target, levelNumber + seedOffset * 17);

            if (lines != null && lines.Count == target && LevelSolvabilityValidator.IsSolvable(lines))

            {

                return new LevelDefinition

                {

                    LevelNumber = levelNumber,

                    Shape = shape,

                    Tier = profile.Tier,

                    GridSize = gridSize,

                    CellSize = ProceduralLevelConstants.DefaultCellSize,

                    DifficultyScore = profile.ComputeDifficultyScore(),

                    TargetLineCount = target,

                    Lines = lines

                };

            }



            return DensePolylineGenerator.Generate(levelNumber, seedOffset + 99);

        }

    }

}


