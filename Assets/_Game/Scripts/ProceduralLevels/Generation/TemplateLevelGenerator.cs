using System.Collections.Generic;
using SerapKeremGameKit._LevelSystem;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Levels 21+ reuse handcrafted templates 1–20 (your prefab style / Arrows gameplay).
    /// </summary>
    public static class TemplateLevelGenerator
    {
        public static List<LevelLineData> LoadTemplateLines(int templateIndex)
        {
            var prefab = Resources.Load<Level>($"Levels/Level {templateIndex}");
            var baseDef = HandcraftedLevelExtractor.Extract(prefab, templateIndex);
            return baseDef?.Lines;
        }

        public static LevelDefinition Generate(int levelNumber)
        {
            int templateIndex = ((levelNumber - 1) % ProceduralLevelConstants.HandcraftedLevelCount) + 1;
            var prefab = Resources.Load<Level>($"Levels/Level {templateIndex}");
            var baseDef = HandcraftedLevelExtractor.Extract(prefab, templateIndex);
            if (baseDef == null || baseDef.Lines == null || baseDef.Lines.Count == 0)
                return null;

            int variant = levelNumber / ProceduralLevelConstants.HandcraftedLevelCount;
            var lines = TransformLines(baseDef.Lines, variant);

            if (!LevelSolvabilityValidator.IsSolvable(lines))
                lines = TransformLines(baseDef.Lines, variant + 7);

            return new LevelDefinition
            {
                LevelNumber = levelNumber,
                Shape = baseDef.Shape,
                Tier = DifficultyProfile.ForLevel(levelNumber).Tier,
                GridSize = 1,
                CellSize = 1f,
                DifficultyScore = levelNumber + lines.Count * 5,
                TargetLineCount = lines.Count,
                Lines = lines
            };
        }

        static List<LevelLineData> TransformLines(List<LevelLineData> source, int variant)
        {
            var result = new List<LevelLineData>(source.Count);
            bool mirrorX = (variant & 1) == 1;
            bool mirrorY = (variant & 2) == 2;
            bool swapAxes = (variant & 4) == 4;

            foreach (var src in source)
            {
                var copy = new LevelLineData();
                foreach (var p in src.Points)
                {
                    int x = p.X;
                    int y = p.Y;
                    if (swapAxes)
                        (x, y) = (y, x);
                    if (mirrorX)
                        x = -x;
                    if (mirrorY)
                        y = -y;
                    copy.Points.Add(new GridPoint(x, y));
                }

                result.Add(copy);
            }

            return result;
        }
    }
}
