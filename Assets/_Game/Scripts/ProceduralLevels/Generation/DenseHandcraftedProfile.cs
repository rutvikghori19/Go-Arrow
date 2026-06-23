using UnityEngine;



namespace _Game.ProceduralLevels

{

    /// <summary>

    /// Arrow-count targets for handcrafted dense levels 11–100 (Level 10 style).

    /// </summary>

    public static class DenseHandcraftedProfile

    {

        public const int MinLevel = 11;

        public const int MaxLevel = 100;



        public static bool IsDenseHandcraftedLevel(int levelNumber)

        {

            return levelNumber >= MinLevel && levelNumber <= MaxLevel;

        }



        public static int GetArrowCount(int levelNumber)

        {

            if (!IsDenseHandcraftedLevel(levelNumber))

                return 0;



            if (HandcraftedBand11To20Profile.IsBandLevel(levelNumber))

                return HandcraftedBand11To20Builder.Level10ArrowCount;



            if (HandcraftedBand21To30Profile.IsBandLevel(levelNumber))

                return HandcraftedBand21To30Profile.GetArrowCount(levelNumber);



            if (HandcraftedBand31To40Profile.IsBandLevel(levelNumber))

                return HandcraftedBand31To40Profile.GetArrowCount(levelNumber);



            if (HandcraftedBand41To100Profile.IsBandLevel(levelNumber))

                return HandcraftedBand41To100Profile.GetArrowCount(levelNumber);



            return 0;

        }



        public static ShapeType GetShape(int levelNumber)

        {

            if (!IsDenseHandcraftedLevel(levelNumber))

                return ShapeType.Square;



            if (HandcraftedBand11To20Profile.IsBandLevel(levelNumber))

                return HandcraftedBand11To20Profile.GetBaseShape(

                    HandcraftedBand11To20Profile.GetSpec(levelNumber).Shape);



            if (HandcraftedBand21To30Profile.IsBandLevel(levelNumber))

                return HandcraftedBand21To30Profile.GetShape(levelNumber);



            if (HandcraftedBand31To40Profile.IsBandLevel(levelNumber))

                return HandcraftedBand31To40Profile.GetShape(levelNumber);



            if (HandcraftedBand41To100Profile.IsBandLevel(levelNumber))

                return HandcraftedBand41To100Profile.GetShape(levelNumber);



            return ShapeType.Square;

        }



        public static int GetGridSize(int levelNumber)

        {

            if (!IsDenseHandcraftedLevel(levelNumber))

                return 9;



            if (HandcraftedBand11To20Profile.IsBandLevel(levelNumber))

                return HandcraftedBand11To20Profile.GetGridSize(levelNumber);



            if (HandcraftedBand21To30Profile.IsBandLevel(levelNumber))

                return HandcraftedBand21To30Profile.GetGridSize(levelNumber);



            if (HandcraftedBand31To40Profile.IsBandLevel(levelNumber))

                return HandcraftedBand31To40Profile.GetGridSize(levelNumber);



            if (HandcraftedBand41To100Profile.IsBandLevel(levelNumber))

                return HandcraftedBand41To100Profile.GetGridSize(levelNumber);



            return 21;

        }



        public static DifficultyProfile ForLevel(int levelNumber)

        {

            int lineCount = GetArrowCount(levelNumber);

            var tier = levelNumber <= 20

                ? DifficultyTier.Medium

                : levelNumber <= 40

                    ? DifficultyTier.Hard

                    : levelNumber <= 70

                        ? DifficultyTier.Expert

                        : DifficultyTier.Expert;



            return new DifficultyProfile(

                levelNumber,

                gridSize: GetGridSize(levelNumber),

                lineCount: lineCount,

                minPathLength: 2,

                maxPathLength: levelNumber >= 70 ? 8 : 6,

                shape: GetShape(levelNumber),

                tier: tier,

                occupancyTarget: levelNumber >= 41 ? 0.85f : 0.75f);

        }

    }

}

