namespace _Game.ProceduralLevels
{
    public static class ProceduralLevelUtility
    {
        public static bool IsProceduralLevel(int levelNumber)
        {
            return levelNumber >= 1 && levelNumber <= ProceduralLevelConstants.TotalLevelCount;
        }

        public static bool IsHandcraftedLevel(int levelNumber)
        {
            return ProceduralLevelConstants.HandcraftedLevelCount > 0 &&
                   levelNumber >= 1 &&
                   levelNumber <= ProceduralLevelConstants.HandcraftedLevelCount;
        }

        public static int ClampLevelNumber(int levelNumber)
        {
            if (levelNumber < 1)
                return 1;
            if (levelNumber > ProceduralLevelConstants.TotalLevelCount)
                return ProceduralLevelConstants.TotalLevelCount;
            return levelNumber;
        }
    }
}
