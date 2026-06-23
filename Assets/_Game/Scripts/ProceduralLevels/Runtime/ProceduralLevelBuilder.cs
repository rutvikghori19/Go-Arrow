using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class ProceduralLevelBuilder
    {
        public static void Build(LevelDefinition definition, Transform linesParent, _Game.Line.Line linePrefab)
        {
            if (definition == null || linesParent == null || linePrefab == null)
                return;

            ClearChildren(linesParent);

            float cell = definition.CellSize <= 0f ? ProceduralLevelConstants.DefaultCellSize : definition.CellSize;

            for (int i = 0; i < definition.Lines.Count; i++)
            {
                var lineData = definition.Lines[i];
                if (lineData == null || lineData.PointCount < 2)
                    continue;

                var instance = Object.Instantiate(linePrefab, linesParent);
                instance.name = $"Line ({i + 1})";

                LineRenderer renderer = instance.LineRenderer;
                if (renderer == null)
                    continue;

                renderer.positionCount = lineData.PointCount;
                for (int p = 0; p < lineData.PointCount; p++)
                {
                    var grid = lineData.Points[p];
                    // Handcrafted prefabs use grid coords as world units (1 cell = 1 unit).
                    renderer.SetPosition(p, new Vector3(grid.X * cell, grid.Y * cell, 0f));
                }
            }
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
