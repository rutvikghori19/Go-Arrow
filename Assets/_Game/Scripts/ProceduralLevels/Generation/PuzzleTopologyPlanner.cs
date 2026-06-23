using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public enum PuzzleTopologyType
    {
        LinearChain,
        DualMerge,
        HiddenMerge,
        FalseChoice
    }

    /// <summary>
    /// Planned solve structure: removal order and branching goals per level band.
    /// </summary>
    public sealed class PuzzlePlan
    {
        public PuzzleTopologyType Topology;
        public List<int> RemovalOrder = new List<int>();
        public int TargetStartMoves = 1;
        public int DecoyCount;
        public int ArrowCount;
    }

    public static class PuzzleTopologyPlanner
    {
        public static PuzzlePlan Create(int levelNumber, System.Random rng)
        {
            int level = Mathf.Clamp(levelNumber, 1, ProceduralLevelConstants.TotalLevelCount);
            var plan = new PuzzlePlan();

            if (level <= 25)
            {
                plan.Topology = PuzzleTopologyType.LinearChain;
                plan.ArrowCount = Mathf.Clamp(3 + (level - 11) / 3, 3, 6);
                plan.TargetStartMoves = 1;
            }
            else if (level <= 45)
            {
                plan.Topology = PuzzleTopologyType.LinearChain;
                plan.ArrowCount = Mathf.Clamp(5 + (level - 26) / 4, 5, 9);
                plan.TargetStartMoves = 1;
            }
            else if (level <= 70)
            {
                plan.Topology = PuzzleTopologyType.DualMerge;
                plan.ArrowCount = Mathf.Clamp(6 + (level - 46) / 5, 6, 11);
                plan.TargetStartMoves = 2;
            }
            else if (level <= 85)
            {
                plan.Topology = PuzzleTopologyType.HiddenMerge;
                plan.ArrowCount = Mathf.Clamp(8 + (level - 71) / 4, 8, 13);
                plan.TargetStartMoves = 1;
            }
            else
            {
                plan.Topology = PuzzleTopologyType.FalseChoice;
                plan.ArrowCount = Mathf.Clamp(10 + (level - 86) / 3, 10, 16);
                plan.TargetStartMoves = 2;
                plan.DecoyCount = 1;
            }

            plan.RemovalOrder = BuildRemovalOrder(plan, rng);
            return plan;
        }

        static List<int> BuildRemovalOrder(PuzzlePlan plan, System.Random rng)
        {
            var order = new List<int>();
            int n = plan.ArrowCount;

            switch (plan.Topology)
            {
                case PuzzleTopologyType.LinearChain:
                    for (int i = 0; i < n; i++)
                        order.Add(i);
                    break;

                case PuzzleTopologyType.DualMerge:
                case PuzzleTopologyType.HiddenMerge:
                case PuzzleTopologyType.FalseChoice:
                {
                    int leftCount = (n + 1) / 2;
                    int rightCount = n - leftCount;
                    int depth = Mathf.Max(leftCount, rightCount);
                    for (int row = 0; row < depth; row++)
                    {
                        if (row < leftCount)
                            order.Add(row);
                        if (row < rightCount)
                            order.Add(leftCount + row);
                    }
                    break;
                }
            }

            return order;
        }
    }
}
