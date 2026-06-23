namespace _Game.ProceduralLevels
{
    public static class ProceduralLevelGenerator
    {
        public static LevelDefinition Generate(int levelNumber)
        {
            return TemplateLevelGenerator.Generate(levelNumber);
        }
    }
}
