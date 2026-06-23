using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Rule #1–#10: solution-first generator with dependency chains and shape layouts.
    /// </summary>
    public static class SolutionFirstGenerator
    {
        public static LevelDefinition Generate(int levelNumber)
        {
            var profile = DifficultyProfile.ForLevel(levelNumber);
            int seed = levelNumber * 42013 + 7919;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                var rng = new System.Random(seed + attempt * 1663);
                var plan = PuzzleTopologyPlanner.Create(levelNumber, rng);
                var shape = profile.Shape;

                var lines = ReversePuzzleBuilder.Build(plan, shape, profile.GridSize, rng);
                if (lines == null || lines.Count < 2)
                    continue;

                if (!LevelSolvabilityValidator.IsSolvable(lines))
                    continue;

                if (!LevelDifficultyAnalyzer.MeetsPlan(lines, plan))
                    continue;

                if (plan.DecoyCount > 0 && !LevelDifficultyAnalyzer.HasFalseChoice(lines, plan))
            {
                // Prefer false-choice levels but accept strong branching if decoy layout fails.
                if (LevelSolvabilityValidator.GetRemovableIndices(lines).Count < 2)
                    continue;
            }

                CenterLines(lines);

                return new LevelDefinition
                {
                    LevelNumber = profile.LevelNumber,
                    Shape = shape,
                    Tier = profile.Tier,
                    GridSize = profile.GridSize,
                    CellSize = ProceduralLevelConstants.DefaultCellSize,
                    DifficultyScore = profile.ComputeDifficultyScore(),
                    TargetLineCount = lines.Count,
                    Lines = lines
                };
            }

            return BuildTutorialFallback(profile);
        }

        static LevelDefinition BuildTutorialFallback(DifficultyProfile profile)
        {
            var rng = new System.Random(profile.LevelNumber);
            var plan = new PuzzlePlan
            {
                Topology = PuzzleTopologyType.LinearChain,
                ArrowCount = 3,
                TargetStartMoves = 1
            };
            plan.RemovalOrder = new List<int> { 0, 1, 2 };

            var lines = ReversePuzzleBuilder.Build(plan, ShapeType.Plus, 9, rng);
            if (lines == null)
            {
                lines = new List<LevelLineData>
                {
                    CreateStraight(0, 4, Vector2Int.up),
                    CreateStraight(0, 1, Vector2Int.up),
                    CreateStraight(0, -2, Vector2Int.up)
                };
            }

            CenterLines(lines);
            return new LevelDefinition
            {
                LevelNumber = profile.LevelNumber,
                Shape = ShapeType.Plus,
                Tier = profile.Tier,
                GridSize = 9,
                CellSize = ProceduralLevelConstants.DefaultCellSize,
                DifficultyScore = profile.ComputeDifficultyScore(),
                TargetLineCount = lines.Count,
                Lines = lines
            };
        }

        static LevelLineData CreateStraight(int x, int headY, Vector2Int dir)
        {
            var line = new LevelLineData();
            line.Points.Add(new GridPoint(x, headY - dir.y));
            line.Points.Add(new GridPoint(x, headY));
            return line;
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
    }
}
