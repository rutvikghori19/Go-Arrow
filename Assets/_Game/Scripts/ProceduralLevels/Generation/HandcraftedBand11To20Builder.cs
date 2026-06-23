using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
  /// <summary>
  /// Levels 11–20: Level 10 layout with small mirror/axis variants (same puzzle feel).
  /// </summary>
  public static class HandcraftedBand11To20Builder
  {
    public const int Level10ArrowCount = 40;

    public static List<LevelLineData> Build(int levelNumber, int seed)
    {
      if (!HandcraftedBand11To20Profile.IsBandLevel(levelNumber))
        return null;

      var template = TemplateLevelGenerator.LoadTemplateLines(10);
      if (template == null || template.Count == 0)
        return null;

      int baseVariant = (levelNumber - HandcraftedBand11To20Profile.MinLevel) & 7;
      for (int tryV = 0; tryV < 8; tryV++)
      {
        int variant = (baseVariant + tryV) & 7;
        var lines = TransformLines(template, variant);
        if (!LevelSolvabilityValidator.IsSolvable(lines))
          continue;

        if (DenseShapeConformer.HasOverlappingEdges(lines))
          continue;

        return lines;
      }

      return TransformLines(template, baseVariant);
    }

    static List<LevelLineData> TransformLines(List<LevelLineData> source, int variant)
    {
      bool mirrorX = (variant & 1) == 1;
      bool mirrorY = (variant & 2) == 2;
      bool swapAxes = (variant & 4) == 4;
      var result = new List<LevelLineData>(source.Count);

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
