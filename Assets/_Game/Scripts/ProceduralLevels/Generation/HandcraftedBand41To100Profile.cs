using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
  /// <summary>
  /// Handcrafted levels 41–100: dense shaped silhouettes with rising arrow counts.
  /// Shape sequence avoids consecutive duplicate silhouettes and spreads repeats.
  /// </summary>
  public static class HandcraftedBand41To100Profile
  {
    public const int MinLevel = 41;
    public const int MaxLevel = 100;

    static readonly ShapeType[] ShapeSequence =
    {
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
      ShapeType.Heart, ShapeType.Star, ShapeType.Hexagon, ShapeType.Circle, ShapeType.Diamond,
      ShapeType.Triangle, ShapeType.Plus, ShapeType.Square, ShapeType.Ring, ShapeType.Crescent,
    };

    static readonly Dictionary<ShapeType, float> TightShapeArrowScale = new()
    {
      { ShapeType.Heart, 0.75f },
      { ShapeType.Crescent, 0.82f },
      { ShapeType.Ring, 0.85f },
      { ShapeType.Star, 0.9f },
    };

    public static bool IsBandLevel(int levelNumber) =>
      levelNumber >= MinLevel && levelNumber <= MaxLevel;

    public static int GetArrowCount(int levelNumber)
    {
      if (!IsBandLevel(levelNumber))
        return 0;

      int baseCount = Mathf.Min(64 + (levelNumber - MinLevel) * 2, 150);
      var shape = GetShape(levelNumber);
      if (TightShapeArrowScale.TryGetValue(shape, out float scale))
        return Mathf.Max(44, Mathf.RoundToInt(baseCount * scale));
      return baseCount;
    }

    public static ShapeType GetShape(int levelNumber)
    {
      if (!IsBandLevel(levelNumber))
        return ShapeType.Square;
      return ShapeSequence[levelNumber - MinLevel];
    }

    public static int GetGridSize(int levelNumber)
    {
      int arrows = GetArrowCount(levelNumber);
      if (arrows <= 75) return 23;
      if (arrows <= 110) return 25;
      if (arrows <= 150) return 27;
      return 29;
    }
  }
}
