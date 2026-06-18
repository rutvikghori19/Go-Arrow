using System;

namespace _Game.Generation
{
    /// <summary>
    /// Pure-C# (Unity-independent) procedural level builder.
    ///
    /// Difficulty scales with the level number (1..MaxLevel): later levels have more lines on a
    /// larger, denser board, which forces the player to discover a longer removal order.
    ///
    /// GUARANTEED SOLVABLE BY CONSTRUCTION:
    /// In this game a tapped line fails only if its head, sweeping forward, hits another line that
    /// is still present. We place lines one at a time and only accept a new line if its forward ray
    /// is clear of every previously-placed line's body (and bodies never overlap). That invariant
    /// means the lines can always be cleared in reverse-placement order (each line, when removed, is
    /// only blocked by lines placed before it, which by construction are not on its ray). Therefore a
    /// full-clear (100% completable) solution always exists. <see cref="ProceduralLevelSolver"/>
    /// independently verifies this.
    /// </summary>
    public static class ProceduralLevelBuilder
    {
        public const int MaxLevel = 500;

        private static readonly (int dx, int dy)[] Directions =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        public struct Difficulty
        {
            public int LineCount;
            public int Width;
            public int Height;
            public int MaxLength;
        }

        public static Difficulty DifficultyFor(int level)
        {
            if (level < 1) level = 1;
            if (level > MaxLevel) level = MaxLevel;

            double t = (level - 1) / (double)(MaxLevel - 1); // 0..1

            // Number of lines grows from 3 up to 35 across the 500 levels.
            int lineCount = (int)Math.Round(3 + t * 32);
            if (lineCount < 2) lineCount = 2;

            // Longer segments unlock as difficulty rises.
            int maxLength = t < 0.33 ? 2 : 3;

            // Board grows to comfortably fit the requested line count (with room for rejection
            // sampling), and expands a little extra at higher levels for added spacing/complexity.
            int side = (int)Math.Ceiling(Math.Sqrt(lineCount * 5.0)) + 2 + (int)Math.Round(t * 3);
            if (side < 6) side = 6;

            return new Difficulty
            {
                LineCount = lineCount,
                Width = side,
                Height = side,
                MaxLength = maxLength
            };
        }

        /// <summary>Deterministically builds a guaranteed-solvable level for the given number.</summary>
        public static GeneratedLevel Build(int level)
        {
            if (level < 1) level = 1;
            if (level > MaxLevel) level = MaxLevel;

            Difficulty d = DifficultyFor(level);
            // Deterministic seed: the same level number always produces the same layout.
            var rng = new Random(level * 7919 + 13);

            var result = new GeneratedLevel
            {
                LevelNumber = level,
                Width = d.Width,
                Height = d.Height
            };

            bool[,] occupied = new bool[d.Width, d.Height];
            const int attemptsPerLine = 400;

            for (int k = 0; k < d.LineCount; k++)
            {
                bool placed = false;

                for (int a = 0; a < attemptsPerLine && !placed; a++)
                {
                    var (dx, dy) = Directions[rng.Next(Directions.Length)];
                    int len = 1 + rng.Next(d.MaxLength); // 1..MaxLength
                    int tx = rng.Next(d.Width);
                    int ty = rng.Next(d.Height);

                    int hx = tx + dx * len;
                    int hy = ty + dy * len;

                    // Whole body must lie inside the board.
                    if (hx < 0 || hx >= d.Width || hy < 0 || hy >= d.Height)
                    {
                        continue;
                    }

                    // Body must not overlap any already-placed line.
                    bool ok = true;
                    for (int i = 0; i <= len && ok; i++)
                    {
                        if (occupied[tx + dx * i, ty + dy * i]) ok = false;
                    }
                    if (!ok) continue;

                    // Forward ray (beyond the head, within the board) must be clear of all
                    // already-placed lines. Cells beyond the board edge are the exit (always clear).
                    for (int step = 1; ok; step++)
                    {
                        int cx = hx + dx * step;
                        int cy = hy + dy * step;
                        if (cx < 0 || cx >= d.Width || cy < 0 || cy >= d.Height) break;
                        if (occupied[cx, cy]) ok = false;
                    }
                    if (!ok) continue;

                    // Accept: mark body cells as occupied.
                    for (int i = 0; i <= len; i++)
                    {
                        occupied[tx + dx * i, ty + dy * i] = true;
                    }

                    result.Lines.Add(new GeneratedLine
                    {
                        TailX = tx,
                        TailY = ty,
                        DirX = dx,
                        DirY = dy,
                        Length = len
                    });
                    placed = true;
                }

                if (!placed)
                {
                    // Board is too full to safely add more lines; stop early (still solvable).
                    break;
                }
            }

            return result;
        }
    }
}
