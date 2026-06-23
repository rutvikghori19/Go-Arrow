using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class ShapeMaskGenerator
    {
        public static bool[,] CreateMask(ShapeType shape, int gridSize)
        {
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
                    mask[x, y] = IsInside(shape, nx, ny);
                }
            }

            EnsureMinimumCells(mask, 12);
            return mask;
        }

        static bool IsInside(ShapeType shape, float x, float y)
        {
            switch (shape)
            {
                case ShapeType.Circle:
                    return x * x + y * y <= 0.82f;
                case ShapeType.Square:
                    return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) <= 0.78f;
                case ShapeType.Diamond:
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.95f;
                case ShapeType.Triangle:
                    return y <= 0.75f && y >= -0.15f - Mathf.Abs(x) * 0.9f;
                case ShapeType.Heart:
                    return IsHeart(x, y);
                case ShapeType.Star:
                    return IsStar(x, y);
                case ShapeType.Crescent:
                    return IsCrescent(x, y);
                case ShapeType.Cross:
                    return Mathf.Abs(x) <= 0.22f || Mathf.Abs(y) <= 0.22f;
                case ShapeType.Arrow:
                    return IsArrow(x, y);
                case ShapeType.Rocket:
                    return IsRocket(x, y);
                case ShapeType.Plus:
                    return (Mathf.Abs(x) <= 0.18f && Mathf.Abs(y) <= 0.75f) ||
                           (Mathf.Abs(y) <= 0.18f && Mathf.Abs(x) <= 0.75f);
                case ShapeType.Ring:
                    float r2 = x * x + y * y;
                    return r2 <= 0.85f && r2 >= 0.35f;
                case ShapeType.Butterfly:
                    return IsButterfly(x, y);
                case ShapeType.Fish:
                    return IsFish(x, y);
                case ShapeType.Cat:
                    return IsCat(x, y);
                case ShapeType.Tree:
                    return IsTree(x, y);
                case ShapeType.Cake:
                    return IsCake(x, y);
                case ShapeType.Flower:
                    return IsFlower(x, y);
                case ShapeType.Hexagon:
                    return IsHexagon(x, y);
                case ShapeType.Paw:
                    return IsPaw(x, y);
                default:
                    return x * x + y * y <= 0.8f;
            }
        }

        static bool IsHeart(float x, float y)
        {
            float hx = x * 1.06f;
            float hy = y * 1.06f;
            float a = hx * hx + hy * hy - 0.28f;
            return a * a * a - hx * hx * hy * hy * hy <= 0.035f;
        }

        static bool IsStar(float x, float y)
        {
            float angle = Mathf.Atan2(y, x);
            float radius = Mathf.Sqrt(x * x + y * y);
            float star = 0.42f + 0.22f * Mathf.Cos(5f * angle);
            return radius <= star;
        }

        static bool IsCrescent(float x, float y)
        {
            float d1 = (x + 0.18f) * (x + 0.18f) + y * y;
            float d2 = (x - 0.32f) * (x - 0.32f) + y * y;
            return d1 <= 0.78f && d2 >= 0.38f;
        }

        static bool IsArrow(float x, float y)
        {
            bool shaft = Mathf.Abs(x) <= 0.14f && y >= -0.75f && y <= 0.35f;
            bool head = y > 0.1f && y < 0.85f && Mathf.Abs(x) <= 0.55f - y * 0.45f;
            return shaft || head;
        }

        static bool IsRocket(float x, float y)
        {
            bool body = Mathf.Abs(x) <= 0.22f && y >= -0.55f && y <= 0.55f;
            bool nose = y > 0.35f && y < 0.9f && Mathf.Abs(x) <= 0.45f - (y - 0.35f);
            bool fins = y < -0.2f && Mathf.Abs(x) <= 0.5f && Mathf.Abs(x) > 0.18f;
            return body || nose || fins;
        }

        static bool IsButterfly(float x, float y)
        {
            float left = ((x + 0.35f) * (x + 0.35f) + y * y);
            float right = ((x - 0.35f) * (x - 0.35f) + y * y);
            bool wings = (left <= 0.35f || right <= 0.35f) && Mathf.Abs(y) <= 0.55f;
            bool body = Mathf.Abs(x) <= 0.08f && Mathf.Abs(y) <= 0.65f;
            return wings || body;
        }

        static bool IsFish(float x, float y)
        {
            bool body = x * x * 1.2f + y * y * 0.9f <= 0.45f && x > -0.55f;
            bool tail = x < -0.15f && Mathf.Abs(y) <= 0.45f - Mathf.Abs(x + 0.15f) * 0.8f;
            return body || tail;
        }

        static bool IsCat(float x, float y)
        {
            bool face = x * x + y * y <= 0.42f;
            bool earL = (x + 0.28f) * (x + 0.28f) + (y - 0.35f) * (y - 0.35f) <= 0.06f;
            bool earR = (x - 0.28f) * (x - 0.28f) + (y - 0.35f) * (y - 0.35f) <= 0.06f;
            return face || earL || earR;
        }

        static bool IsTree(float x, float y)
        {
            bool trunk = Mathf.Abs(x) <= 0.12f && y >= -0.75f && y <= -0.15f;
            bool crown = x * x + (y - 0.15f) * (y - 0.15f) <= 0.55f;
            return trunk || crown;
        }

        static bool IsCake(float x, float y)
        {
            bool baseLayer = Mathf.Abs(x) <= 0.65f && y >= -0.75f && y <= -0.25f;
            bool mid = Mathf.Abs(x) <= 0.5f && y > -0.25f && y <= 0.15f;
            bool top = Mathf.Abs(x) <= 0.35f && y > 0.15f && y <= 0.45f;
            bool candle = Mathf.Abs(x) <= 0.05f && y > 0.45f && y <= 0.75f;
            return baseLayer || mid || top || candle;
        }

        static bool IsFlower(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float petals = 0.28f + 0.18f * Mathf.Cos(6f * Mathf.Atan2(y, x));
            bool bloom = r <= petals && r >= 0.08f;
            bool center = r <= 0.12f;
            bool stem = Mathf.Abs(x) <= 0.08f && y >= -0.8f && y < -0.1f;
            return bloom || center || stem;
        }

        static bool IsHexagon(float x, float y)
        {
            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            return ax <= 0.65f && ay <= 0.55f && ax * 0.5f + ay <= 0.72f;
        }

        static bool IsPaw(float x, float y)
        {
            bool pad = x * x + (y + 0.15f) * (y + 0.15f) <= 0.28f;
            bool toe1 = (x + 0.28f) * (x + 0.28f) + (y - 0.35f) * (y - 0.35f) <= 0.05f;
            bool toe2 = (x + 0.1f) * (x + 0.1f) + (y - 0.45f) * (y - 0.45f) <= 0.05f;
            bool toe3 = (x - 0.1f) * (x - 0.1f) + (y - 0.45f) * (y - 0.45f) <= 0.05f;
            bool toe4 = (x - 0.28f) * (x - 0.28f) + (y - 0.35f) * (y - 0.35f) <= 0.05f;
            return pad || toe1 || toe2 || toe3 || toe4;
        }

        static void EnsureMinimumCells(bool[,] mask, int minimum)
        {
            int count = CountCells(mask);
            if (count >= minimum)
                return;

            int size = mask.GetLength(0);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    if (dx * dx + dy * dy <= (size * 0.35f) * (size * 0.35f))
                        mask[x, y] = true;
                }
            }
        }

        static int CountCells(bool[,] mask)
        {
            int count = 0;
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mask[x, y]) count++;
            return count;
        }

        public static bool IsInsideMask(bool[,] mask, int x, int y)
        {
            if (mask == null) return false;
            if (x < 0 || y < 0 || x >= mask.GetLength(0) || y >= mask.GetLength(1))
                return false;
            return mask[x, y];
        }

        public static bool TryGetOutwardDirection(bool[,] mask, int x, int y, out Vector2Int direction)
        {
            var options = new List<Vector2Int>(4);
            if (CollectOutwardDirections(mask, x, y, options) == 0)
            {
                direction = Vector2Int.zero;
                return false;
            }

            direction = options[0];
            return true;
        }

        public static int CollectOutwardDirections(bool[,] mask, int x, int y, List<Vector2Int> results)
        {
            results?.Clear();
            if (results == null)
                return 0;

            var candidates = new[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var d in candidates)
            {
                int nx = x + d.x;
                int ny = y + d.y;
                if (!IsInsideMask(mask, nx, ny))
                    results.Add(d);
            }

            return results.Count;
        }
    }
}
