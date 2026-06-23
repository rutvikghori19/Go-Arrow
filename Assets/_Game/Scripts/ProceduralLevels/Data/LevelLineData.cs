using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    [Serializable]
    public struct GridPoint
    {
        public int X;
        public int Y;

        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2Int ToVector2Int() => new Vector2Int(X, Y);
    }

    [Serializable]
    public class LevelLineData
    {
        public List<GridPoint> Points = new List<GridPoint>();

        public int PointCount => Points?.Count ?? 0;

        public Vector2Int GetHead()
        {
            if (Points == null || Points.Count == 0)
                return Vector2Int.zero;
            var p = Points[Points.Count - 1];
            return new Vector2Int(p.X, p.Y);
        }

        public Vector2Int GetDirection()
        {
            if (Points == null || Points.Count < 2)
                return Vector2Int.up;

            var a = Points[Points.Count - 2];
            var b = Points[Points.Count - 1];
            var d = new Vector2Int(b.X - a.X, b.Y - a.Y);
            return SnapToAxis(d);
        }

        public static Vector2Int SnapToAxis(Vector2Int d)
        {
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
                return new Vector2Int(d.x >= 0 ? 1 : -1, 0);
            return new Vector2Int(0, d.y >= 0 ? 1 : -1);
        }
    }
}
