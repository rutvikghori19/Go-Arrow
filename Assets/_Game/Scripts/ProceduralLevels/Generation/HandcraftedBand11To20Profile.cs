using UnityEngine;

namespace _Game.ProceduralLevels
{
  public enum HandcraftedShapeVariant
  {
    SmallDiamond,
    Diamond,
    RoundedDiamond,
    Square,
    RoundedSquare,
    Circle,
    Oval,
    Hexagon,
    LargeDiamond,
    LargeCircle
  }

  public struct HandcraftedLevelSpec
  {
    public int Level;
    public int ArrowCount;
    public HandcraftedShapeVariant Shape;
    public int Branches;
    public int Loops;
    public int Traps;
    public float FillTarget;
  }

  /// <summary>
  /// Professional puzzle-design targets for handcrafted levels 11–20.
  /// </summary>
  public static class HandcraftedBand11To20Profile
  {
    public const int MinLevel = 11;
    public const int MaxLevel = 20;

    static readonly HandcraftedLevelSpec[] Specs =
    {
      new HandcraftedLevelSpec { Level = 11, ArrowCount = 40, Shape = HandcraftedShapeVariant.SmallDiamond, Branches = 2, Loops = 1, Traps = 1, FillTarget = 0.72f },
      new HandcraftedLevelSpec { Level = 12, ArrowCount = 40, Shape = HandcraftedShapeVariant.Diamond, Branches = 2, Loops = 1, Traps = 2, FillTarget = 0.74f },
      new HandcraftedLevelSpec { Level = 13, ArrowCount = 40, Shape = HandcraftedShapeVariant.RoundedDiamond, Branches = 3, Loops = 1, Traps = 2, FillTarget = 0.75f },
      new HandcraftedLevelSpec { Level = 14, ArrowCount = 40, Shape = HandcraftedShapeVariant.Square, Branches = 3, Loops = 2, Traps = 2, FillTarget = 0.76f },
      new HandcraftedLevelSpec { Level = 15, ArrowCount = 40, Shape = HandcraftedShapeVariant.RoundedSquare, Branches = 3, Loops = 2, Traps = 3, FillTarget = 0.77f },
      new HandcraftedLevelSpec { Level = 16, ArrowCount = 40, Shape = HandcraftedShapeVariant.Circle, Branches = 4, Loops = 2, Traps = 3, FillTarget = 0.78f },
      new HandcraftedLevelSpec { Level = 17, ArrowCount = 40, Shape = HandcraftedShapeVariant.Oval, Branches = 4, Loops = 2, Traps = 4, FillTarget = 0.78f },
      new HandcraftedLevelSpec { Level = 18, ArrowCount = 40, Shape = HandcraftedShapeVariant.Hexagon, Branches = 4, Loops = 3, Traps = 4, FillTarget = 0.79f },
      new HandcraftedLevelSpec { Level = 19, ArrowCount = 40, Shape = HandcraftedShapeVariant.LargeDiamond, Branches = 5, Loops = 3, Traps = 4, FillTarget = 0.80f },
      new HandcraftedLevelSpec { Level = 20, ArrowCount = 40, Shape = HandcraftedShapeVariant.LargeCircle, Branches = 5, Loops = 3, Traps = 5, FillTarget = 0.80f },
    };

    public static bool IsBandLevel(int levelNumber) =>
      levelNumber >= MinLevel && levelNumber <= MaxLevel;

    public static HandcraftedLevelSpec GetSpec(int levelNumber)
    {
      if (!IsBandLevel(levelNumber))
        return default;

      return Specs[levelNumber - MinLevel];
    }

    public static int GetArrowCount(int levelNumber) => GetSpec(levelNumber).ArrowCount;

    public static int GetGridSize(int levelNumber)
    {
      int arrows = GetArrowCount(levelNumber);
      if (arrows <= 32) return 13;
      if (arrows <= 40) return 15;
      return 17;
    }

    public static ShapeType GetBaseShape(HandcraftedShapeVariant variant)
    {
      switch (variant)
      {
        case HandcraftedShapeVariant.SmallDiamond:
        case HandcraftedShapeVariant.Diamond:
        case HandcraftedShapeVariant.RoundedDiamond:
        case HandcraftedShapeVariant.LargeDiamond:
          return ShapeType.Diamond;
        case HandcraftedShapeVariant.Square:
        case HandcraftedShapeVariant.RoundedSquare:
          return ShapeType.Square;
        case HandcraftedShapeVariant.Circle:
        case HandcraftedShapeVariant.Oval:
        case HandcraftedShapeVariant.LargeCircle:
          return ShapeType.Circle;
        case HandcraftedShapeVariant.Hexagon:
          return ShapeType.Hexagon;
        default:
          return ShapeType.Diamond;
      }
    }
  }
}
