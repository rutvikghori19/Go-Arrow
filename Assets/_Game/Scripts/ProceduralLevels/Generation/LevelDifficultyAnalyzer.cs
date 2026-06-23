using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class LevelDifficultyAnalyzer
    {
        public static bool MeetsPlan(IReadOnlyList<LevelLineData> lines, PuzzlePlan plan)
        {
            if (lines == null || plan == null || lines.Count == 0)
                return false;

            var all = BuildAllIndices(lines.Count);
            int startMoves = LevelSolvabilityValidator.GetRemovableIndices(lines, all).Count;

            if (startMoves < 1)
                return false;

            if (plan.TargetStartMoves == 1 && startMoves > 4)
                return false;

            if (plan.TargetStartMoves == 2 && startMoves < 2)
                return false;

            if (plan.TargetStartMoves == 2 && startMoves > 5)
                return false;

            int decisions = CountDecisionPoints(lines);
            int minDecisions = GetMinDecisions(plan.Topology);
            return decisions >= minDecisions;
        }

        public static bool HasFalseChoice(IReadOnlyList<LevelLineData> lines, PuzzlePlan plan)
        {
            if (plan.DecoyCount <= 0 || lines == null || lines.Count < 3)
                return true;

            var all = BuildAllIndices(lines.Count);
            var startMoves = LevelSolvabilityValidator.GetRemovableIndices(lines, all);
            if (startMoves.Count < 2)
                return false;

            foreach (int decoy in startMoves)
            {
                var remaining = new List<int>(all);
                remaining.Remove(decoy);
                if (!LevelSolvabilityValidator.IsSolvableSubset(lines, remaining))
                    return true;
            }

            return false;
        }

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

        static int GetMinDecisions(PuzzleTopologyType topology)
        {
            switch (topology)
            {
                case PuzzleTopologyType.LinearChain: return 0;
                case PuzzleTopologyType.DualMerge: return 1;
                case PuzzleTopologyType.HiddenMerge: return 2;
                case PuzzleTopologyType.FalseChoice: return 2;
                default: return 0;
            }
        }
    }
}
