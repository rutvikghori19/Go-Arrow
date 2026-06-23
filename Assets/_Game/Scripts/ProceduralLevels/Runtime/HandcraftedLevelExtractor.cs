using System.Collections.Generic;
using GameLine = _Game.Line.Line;
using SerapKeremGameKit._LevelSystem;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class HandcraftedLevelExtractor
    {
        public static LevelDefinition Extract(Level prefab, int levelNumber)
        {
            var definition = new LevelDefinition
            {
                LevelNumber = levelNumber,
                GridSize = 1,
                CellSize = 1f,
                Shape = ShapeType.Square,
                Tier = DifficultyTier.Tutorial,
                Lines = new List<LevelLineData>()
            };

            if (prefab == null)
                return definition;

            Transform linesParent = prefab.transform.Find("LINES");
            if (linesParent == null)
                return definition;

            var lines = linesParent.GetComponentsInChildren<GameLine>(true);
            foreach (var line in lines)
            {
                LineRenderer renderer = line.LineRenderer;
                if (renderer == null || renderer.positionCount < 2)
                    continue;

                var data = new LevelLineData();
                Vector3 offset = line.transform.localPosition;

                for (int p = 0; p < renderer.positionCount; p++)
                {
                    Vector3 pos = renderer.GetPosition(p) + offset;
                    data.Points.Add(new GridPoint(
                        Mathf.RoundToInt(pos.x),
                        Mathf.RoundToInt(pos.y)));
                }

                definition.Lines.Add(data);
            }

            definition.TargetLineCount = definition.LineCount;
            return definition;
        }
    }
}
