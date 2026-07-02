using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class LevelDifficultyAnalyzer
    {
        public static int CountDecisionPoints(IReadOnlyList<LevelLineData> lines)
        {
            if (lines == null || lines.Count == 0)
                return 0;

            var remaining = BuildAllIndices(lines.Count);
            int decisions = 0;

            while (remaining.Count > 0)
            {
                var moves = LevelSolvabilityValidator.GetRemovableIndices(lines, remaining);
                if (moves.Count == 0)
                    return decisions;

                if (moves.Count > 1)
                    decisions++;

                remaining.Remove(moves[0]);
            }

            return decisions;
        }

        static List<int> BuildAllIndices(int count)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++)
                list.Add(i);
            return list;
        }
    }
}
