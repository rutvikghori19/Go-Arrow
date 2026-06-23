#if UNITY_EDITOR
using System.Text;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public static class HandcraftedLevelValidationMenu
    {
        [MenuItem("Go-Arrow/Procedural Levels/Validate Handcrafted Levels 11-50")]
        public static void ValidateDenseHandcraftedLevels()
        {
            var report = new StringBuilder();
            int failures = 0;

            for (int level = DenseHandcraftedProfile.MinLevel; level <= DenseHandcraftedProfile.MaxLevel; level++)
            {
                var prefab = Resources.Load<Level>($"Levels/Level {level}");
                if (prefab == null)
                {
                    failures++;
                    report.AppendLine($"Level {level}: MISSING prefab");
                    continue;
                }

                var definition = HandcraftedLevelExtractor.Extract(prefab, level);
                int target = DenseHandcraftedProfile.GetArrowCount(level);
                bool solvable = LevelSolvabilityValidator.IsSolvable(definition.Lines);
                bool cluster = LevelClusterValidator.IsTightCluster(definition.Lines);
                bool allInShape = AllLinesInsideShape(definition.Lines, level);
                bool hasOverlap = DenseShapeConformer.HasOverlappingEdges(definition.Lines) ||
                                  CenterOutDenseBuilder.HasOverlappingCellsPublic(definition.Lines);
                var order = LevelSolvabilityValidator.FindRemovalOrder(definition.Lines);
                var shape = DenseHandcraftedProfile.GetShape(level);

                if (!solvable || definition.LineCount < target || !cluster || !allInShape || hasOverlap)
                {
                    failures++;
                    report.AppendLine(
                        $"Level {level}: FAIL | shape={shape} | arrows={definition.LineCount}/{target} | " +
                        $"solvable={solvable} | cluster={cluster} | inShape={allInShape} | overlap={hasOverlap}");
                }
                else
                {
                    report.AppendLine(
                        $"Level {level}: OK | shape={shape} | arrows={definition.LineCount} | removalSteps={order?.Count ?? 0}");
                }
            }

            string summary = failures == 0
                ? "All handcrafted levels 11–50 passed validation."
                : $"{failures} handcrafted level(s) failed validation.";

            Debug.Log($"[HandcraftedLevels]\n{summary}\n{report}");
            EditorUtility.DisplayDialog("Handcrafted Validation 11–50", $"{summary}\n\nSee Console for details.", "OK");
        }

        static bool AllLinesInsideShape(System.Collections.Generic.List<LevelLineData> lines, int level)
        {
            if (lines == null || lines.Count == 0)
                return false;

            var mask = DenseShapeConformer.CreateMask(
                DenseHandcraftedProfile.GetShape(level),
                DenseHandcraftedProfile.GetGridSize(level));

            foreach (var line in lines)
            {
                if (!DenseShapeConformer.IsLineInside(mask, line))
                    return false;
            }

            return true;
        }
    }
}
#endif
