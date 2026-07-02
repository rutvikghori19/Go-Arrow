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

            var bitmap = GetBitmap(shape);
            bool[,] mask;
            if (bitmap != null)
            {
                mask = SampleBitmap(bitmap, size);
            }
            else
            {
                mask = new bool[size, size];
                float center = (size - 1) * 0.5f;
                float scale = 2f / size;
                for (int y = 0; y < size; y++)
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

        /// <summary>Scales a hand-drawn silhouette bitmap (rows[0] = top, '#' = filled) to fit an
        /// N×N mask, aspect-preserved and centred. Nearest-neighbour so it works at any grid size.</summary>
        static bool[,] SampleBitmap(string[] rows, int size)
        {
            int h = rows.Length;
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var mask = new bool[size, size];
            if (w == 0 || h == 0) return mask;

            float scale = Mathf.Min((float)size / w, (float)size / h) * 0.96f;
            float offx = (size - w * scale) * 0.5f;
            float offy = (size - h * scale) * 0.5f;

            for (int ty = 0; ty < size; ty++)
            for (int tx = 0; tx < size; tx++)
            {
                float u = (tx - offx) / scale;
                float v = (ty - offy) / scale;
                if (u < 0f || u >= w || v < 0f || v >= h) continue;
                int sc = (int)u, sr = (int)v;
                var row = rows[sr];
                bool filled = sc < row.Length && row[sc] == '#';
                mask[tx, size - 1 - ty] = filled; // bitmap top (row 0) -> highest y (upright)
            }
            return mask;
        }

        static string[] GetBitmap(ShapeType shape)
        {
            switch (shape)
            {
                case ShapeType.Trophy: return Trophy;
                case ShapeType.Anchor: return Anchor;
                case ShapeType.Dog: return Dog;
                case ShapeType.Duck: return Duck;
                case ShapeType.Cup: return Cup;
                case ShapeType.Car: return Car;
                default: return null;
            }
        }

        static readonly string[] Trophy =
        {
            "..###########..",
            ".#############.",
            "###############",
            "###############",
            "###############",
            ".#############.",
            ".#############.",
            "..###########..",
            "...#########...",
            ".....#####.....",
            ".....#####.....",
            ".....#####.....",
            "....#######....",
            "...#########...",
            "..###########..",
        };

        static readonly string[] Anchor =
        {
            ".....###.....",
            "....##.##....",
            "....##.##....",
            "....##.##....",
            ".....###.....",
            "...#######...",
            ".....###.....",
            ".....###.....",
            ".....###.....",
            ".....###.....",
            ".#...###...#.",
            ".##..###..##.",
            ".###.###.###.",
            "..#########..",
            "...#######...",
            ".....###.....",
        };

        static readonly string[] Duck =
        {
            "..........####...",
            ".........######..",
            ".........##.###..",
            ".........######..",
            "........#######..",
            "....############.",
            "..##############.",
            ".###############.",
            ".###############.",
            ".##############..",
            "..############...",
            "...##########....",
            "....#.....#......",
            "....#.....#......",
        };

        static readonly string[] Dog =
        {
            "...........###...",
            "..........#####..",
            ".........#######.",
            "........########.",
            ".#####..########.",
            "#######.########.",
            "################.",
            "################.",
            ".###############.",
            ".##############..",
            ".####.....####...",
            ".####.....####...",
            ".####.....####...",
            ".###.......###...",
        };

        static readonly string[] Cup =
        {
            "..#########..",
            ".###########.",
            ".###########.",
            ".###########.",
            "..#########..",
            "...#######...",
            ".....###.....",
            ".....###.....",
            "...#######...",
            "..#########..",
        };

        static readonly string[] Car =
        {
            "......########....",
            ".....##########...",
            "....############..",
            "..################",
            "##################",
            "##################",
            "##################",
            ".################.",
            "..###......###....",
            "..###......###....",
            "..###......###....",
        };

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
            bool body = Mathf.Abs(x) <= 0.08f && y >= -0.5f && y <= 0.6f;
            bool ulL = (x + 0.30f) * (x + 0.30f) + (y - 0.26f) * (y - 0.26f) <= 0.16f;
            bool llL = (x + 0.24f) * (x + 0.24f) + (y + 0.30f) * (y + 0.30f) <= 0.10f;
            bool ulR = (x - 0.30f) * (x - 0.30f) + (y - 0.26f) * (y - 0.26f) <= 0.16f;
            bool llR = (x - 0.24f) * (x - 0.24f) + (y + 0.30f) * (y + 0.30f) <= 0.10f;
            bool leftWing = (ulL || llL) && x <= -0.02f;
            bool rightWing = (ulR || llR) && x >= 0.02f;
            bool antennae = Mathf.Abs(Mathf.Abs(x) - 0.13f) <= 0.03f && y > 0.6f && y <= 0.85f;
            return body || leftWing || rightWing || antennae;
        }

        static bool IsFish(float x, float y)
        {
            bool body = (x - 0.05f) * (x - 0.05f) * 1.3f + y * y * 2.4f <= 0.42f;
            bool tail = x >= -0.72f && x <= -0.30f && Mathf.Abs(y) <= (-0.30f - x) * 1.1f;
            return body || tail;
        }

        static bool IsCat(float x, float y)
        {
            bool face = x * x + (y + 0.10f) * (y + 0.10f) <= 0.42f;
            return face || IsCatEar(x, y, -0.34f) || IsCatEar(x, y, 0.34f);
        }

        // Pointy triangular ear that narrows to a tip above the face.
        static bool IsCatEar(float x, float y, float cx)
        {
            if (y < 0.24f || y > 0.92f) return false;
            float t = (0.92f - y) / (0.92f - 0.24f); // 1 at base, 0 at tip
            return Mathf.Abs(x - cx) <= 0.28f * t;
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
            // Bloom sits in the upper half; stem + leaves below.
            float by = y - 0.18f;
            float r = Mathf.Sqrt(x * x + by * by);
            float petals = 0.34f + 0.20f * Mathf.Cos(6f * Mathf.Atan2(by, x));
            bool bloom = r <= petals;
            bool stem = Mathf.Abs(x) <= 0.07f && y >= -0.82f && y < 0f;
            bool leaf = (x - 0.18f) * (x - 0.18f) * 2.0f + (y + 0.35f) * (y + 0.35f) * 3.0f <= 0.06f
                     || (x + 0.18f) * (x + 0.18f) * 2.0f + (y + 0.35f) * (y + 0.35f) * 3.0f <= 0.06f;
            return bloom || stem || leaf;
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
