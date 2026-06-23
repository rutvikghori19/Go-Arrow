using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    /// <summary>
    /// Builds short grid-snapped arrows like Arrows – Puzzle Escape:
    /// paired up/down columns and left/right rows, 1–2 segment bodies.
    /// </summary>
    public static class ArrowsLatticeBuilder
    {
        public static LevelLineData CreateArrow(int headX, int headY, Vector2Int direction, int bodySegments = 1)
        {
            var line = new LevelLineData();
            int dx = direction.x;
            int dy = direction.y;

            for (int i = bodySegments; i >= 0; i--)
            {
                line.Points.Add(new GridPoint(headX - dx * i, headY - dy * i));
            }

            return line;
        }

        /// <summary>Side-by-side column: left points up, right points down.</summary>
        public static void AddVerticalPair(List<LevelLineData> lines, int x, int y, int bodySegments = 1)
        {
            lines.Add(CreateArrow(x, y, Vector2Int.up, bodySegments));
            lines.Add(CreateArrow(x + 1, y, Vector2Int.down, bodySegments));
        }

        /// <summary>Stacked row: top points left, bottom points right.</summary>
        public static void AddHorizontalPair(List<LevelLineData> lines, int x, int y, int bodySegments = 1)
        {
            lines.Add(CreateArrow(x, y, Vector2Int.left, bodySegments));
            lines.Add(CreateArrow(x, y + 1, Vector2Int.right, bodySegments));
        }

        public static void AddSingle(List<LevelLineData> lines, int x, int y, Vector2Int dir, int bodySegments = 1)
        {
            lines.Add(CreateArrow(x, y, dir, bodySegments));
        }
    }
}
