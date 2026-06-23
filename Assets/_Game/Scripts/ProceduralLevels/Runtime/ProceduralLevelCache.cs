using System.Collections.Generic;

namespace _Game.ProceduralLevels
{
    public static class ProceduralLevelCache
    {
        static readonly Dictionary<int, LevelDefinition> Cache = new Dictionary<int, LevelDefinition>();

        public static LevelDefinition GetOrGenerate(int levelNumber)
        {
            if (Cache.TryGetValue(levelNumber, out LevelDefinition cached))
                return cached;

            var definition = ProceduralLevelGenerator.Generate(levelNumber);
            Cache[levelNumber] = definition;
            return definition;
        }

        public static void Clear()
        {
            Cache.Clear();
        }

        public static void Prewarm(int fromLevel, int toLevel)
        {
            int start = fromLevel < toLevel ? fromLevel : toLevel;
            int end = fromLevel > toLevel ? fromLevel : toLevel;

            for (int i = start; i <= end; i++)
                GetOrGenerate(i);
        }
    }
}
