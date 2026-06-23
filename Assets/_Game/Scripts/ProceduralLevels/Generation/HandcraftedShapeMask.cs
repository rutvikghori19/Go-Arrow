using UnityEngine;

namespace _Game.ProceduralLevels
{
  public static class HandcraftedShapeMask
  {
    public static bool[,] Create(HandcraftedShapeVariant variant, int gridSize)
    {
      var spec = HandcraftedBand11To20Profile.GetSpec(
        variant switch
        {
          HandcraftedShapeVariant.SmallDiamond => 11,
          HandcraftedShapeVariant.Diamond => 12,
          HandcraftedShapeVariant.RoundedDiamond => 13,
          HandcraftedShapeVariant.Square => 14,
          HandcraftedShapeVariant.RoundedSquare => 15,
          HandcraftedShapeVariant.Circle => 16,
          HandcraftedShapeVariant.Oval => 17,
          HandcraftedShapeVariant.Hexagon => 18,
          HandcraftedShapeVariant.LargeDiamond => 19,
          HandcraftedShapeVariant.LargeCircle => 20,
          _ => 11
        });

      return CreateForLevel(spec.Level, gridSize);
    }

    public static bool[,] CreateForLevel(int levelNumber, int gridSize)
    {
      var band = HandcraftedBand11To20Profile.GetSpec(levelNumber);
      int size = Mathf.Max(7, gridSize);
      var mask = new bool[size, size];
      float center = (size - 1) * 0.5f;
      float scale = 2f / size;

      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          float nx = (x - center) * scale;
          float ny = (y - center) * scale;
          mask[x, y] = IsInside(band.Shape, nx, ny);
        }
      }

      return mask;
    }

    static bool IsInside(HandcraftedShapeVariant variant, float x, float y)
    {
      switch (variant)
      {
        case HandcraftedShapeVariant.SmallDiamond:
          return Mathf.Abs(x) + Mathf.Abs(y) <= 0.62f;
        case HandcraftedShapeVariant.Diamond:
          return Mathf.Abs(x) + Mathf.Abs(y) <= 0.82f;
        case HandcraftedShapeVariant.RoundedDiamond:
          return RoundedDiamond(x, y, 0.86f, 0.28f);
        case HandcraftedShapeVariant.Square:
          return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) <= 0.76f;
        case HandcraftedShapeVariant.RoundedSquare:
          return RoundedSquare(x, y, 0.76f, 0.22f);
        case HandcraftedShapeVariant.Circle:
          return x * x + y * y <= 0.78f;
        case HandcraftedShapeVariant.Oval:
          return (x * x) / (0.88f * 0.88f) + (y * y) / (0.68f * 0.68f) <= 1f;
        case HandcraftedShapeVariant.Hexagon:
          return IsHexagon(x, y, 0.74f);
        case HandcraftedShapeVariant.LargeDiamond:
          return Mathf.Abs(x) + Mathf.Abs(y) <= 0.96f;
        case HandcraftedShapeVariant.LargeCircle:
          return x * x + y * y <= 0.92f;
        default:
          return Mathf.Abs(x) + Mathf.Abs(y) <= 0.82f;
      }
    }

    static bool RoundedDiamond(float x, float y, float radius, float round)
    {
      float manhattan = Mathf.Abs(x) + Mathf.Abs(y);
      if (manhattan <= radius - round)
        return true;
      if (manhattan > radius)
        return false;
      float corner = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
      return corner <= radius * 0.72f;
    }

    static bool RoundedSquare(float x, float y, float half, float round)
    {
      float ax = Mathf.Abs(x);
      float ay = Mathf.Abs(y);
      if (ax <= half - round && ay <= half - round)
        return true;
      if (ax > half || ay > half)
        return false;
      float cx = half - round;
      float cy = half - round;
      float dx = ax - cx;
      float dy = ay - cy;
      return dx * dx + dy * dy <= round * round;
    }

    static bool IsHexagon(float x, float y, float radius)
    {
      float ax = Mathf.Abs(x);
      float ay = Mathf.Abs(y);
      return ax <= radius * 0.82f && ay <= radius * 0.68f && ax * 0.5f + ay <= radius * 0.88f;
    }
  }
}
