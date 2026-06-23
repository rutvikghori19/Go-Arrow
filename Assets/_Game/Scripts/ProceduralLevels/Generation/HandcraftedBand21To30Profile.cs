using UnityEngine;

namespace _Game.ProceduralLevels
{
  /// <summary>
  /// Handcrafted levels 21–30: recognizable silhouettes with rising arrow count.
  /// </summary>
  public static class HandcraftedBand21To30Profile
  {
    public const int MinLevel = 21;
    public const int MaxLevel = 30;

    public struct BandSpec
    {
      public int Level;
      public int ArrowCount;
      public ShapeType Shape;
    }

    static readonly BandSpec[] Specs =
    {
      new BandSpec { Level = 21, ArrowCount = 42, Shape = ShapeType.Square },
      new BandSpec { Level = 22, ArrowCount = 44, Shape = ShapeType.Square },
      new BandSpec { Level = 23, ArrowCount = 46, Shape = ShapeType.Square },
      new BandSpec { Level = 24, ArrowCount = 48, Shape = ShapeType.Square },
      new BandSpec { Level = 25, ArrowCount = 50, Shape = ShapeType.Square },
      new BandSpec { Level = 26, ArrowCount = 52, Shape = ShapeType.Square },
      new BandSpec { Level = 27, ArrowCount = 54, Shape = ShapeType.Square },
      new BandSpec { Level = 28, ArrowCount = 56, Shape = ShapeType.Square },
      new BandSpec { Level = 29, ArrowCount = 58, Shape = ShapeType.Square },
      new BandSpec { Level = 30, ArrowCount = 60, Shape = ShapeType.Square },
    };

    public static bool IsBandLevel(int levelNumber) =>
      levelNumber >= MinLevel && levelNumber <= MaxLevel;

    public static BandSpec GetSpec(int levelNumber)
    {
      if (!IsBandLevel(levelNumber))
        return default;

      return Specs[levelNumber - MinLevel];
    }

    public static int GetArrowCount(int levelNumber) => GetSpec(levelNumber).ArrowCount;

    public static ShapeType GetShape(int levelNumber) => GetSpec(levelNumber).Shape;

    public static int GetGridSize(int levelNumber)
    {
      int arrows = GetArrowCount(levelNumber);
      var shape = GetShape(levelNumber);
      if (shape == ShapeType.Diamond && arrows >= 48)
        return 17;
      if (arrows <= 45)
        return 15;
      return 17;
    }
  }
}
