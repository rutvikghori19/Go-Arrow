using UnityEngine;

namespace _Game.ProceduralLevels
{
  /// <summary>
  /// Handcrafted levels 31–40: Level 10 dense style inside circle / triangle / diamond silhouettes.
  /// </summary>
  public static class HandcraftedBand31To40Profile
  {
    public const int MinLevel = 31;
    public const int MaxLevel = 40;

    public struct BandSpec
    {
      public int Level;
      public int ArrowCount;
      public ShapeType Shape;
    }

    static readonly BandSpec[] Specs =
    {
      new BandSpec { Level = 31, ArrowCount = 62, Shape = ShapeType.Circle },
      new BandSpec { Level = 32, ArrowCount = 64, Shape = ShapeType.Triangle },
      new BandSpec { Level = 33, ArrowCount = 66, Shape = ShapeType.Diamond },
      new BandSpec { Level = 34, ArrowCount = 68, Shape = ShapeType.Circle },
      new BandSpec { Level = 35, ArrowCount = 70, Shape = ShapeType.Triangle },
      new BandSpec { Level = 36, ArrowCount = 72, Shape = ShapeType.Diamond },
      new BandSpec { Level = 37, ArrowCount = 74, Shape = ShapeType.Circle },
      new BandSpec { Level = 38, ArrowCount = 76, Shape = ShapeType.Triangle },
      new BandSpec { Level = 39, ArrowCount = 78, Shape = ShapeType.Diamond },
      new BandSpec { Level = 40, ArrowCount = 80, Shape = ShapeType.Circle },
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
      if (arrows <= 75) return 23;
      return 25;
    }
  }
}
