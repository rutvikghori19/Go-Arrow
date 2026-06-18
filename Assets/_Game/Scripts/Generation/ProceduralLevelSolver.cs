namespace _Game.Generation
{
    /// <summary>
    /// Pure-C# (Unity-independent) solver that independently verifies a generated level is fully
    /// completable, i.e. there exists an order to remove every line such that each line's head,
    /// when it sweeps forward, is never blocked by a line that is still present.
    ///
    /// Greedy is correct here: removing a line can only clear another line's forward ray, never
    /// block it, so "removability" is monotonic — if any line is removable now, removing it can
    /// never make the rest unsolvable. We therefore repeatedly remove any currently-removable line;
    /// if we can remove them all, the level is solvable.
    /// </summary>
    public static class ProceduralLevelSolver
    {
        public static bool IsSolvable(GeneratedLevel level)
        {
            int n = level.Lines.Count;
            if (n == 0) return true;

            bool[] present = new bool[n];
            for (int i = 0; i < n; i++) present[i] = true;
            int remaining = n;

            while (remaining > 0)
            {
                bool removedAny = false;

                for (int i = 0; i < n; i++)
                {
                    if (!present[i]) continue;
                    if (ForwardRayClear(level, i, present))
                    {
                        present[i] = false;
                        remaining--;
                        removedAny = true;
                        break;
                    }
                }

                if (!removedAny)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ForwardRayClear(GeneratedLevel level, int idx, bool[] present)
        {
            GeneratedLine line = level.Lines[idx];
            int hx = line.HeadX;
            int hy = line.HeadY;
            int n = level.Lines.Count;

            for (int step = 1; ; step++)
            {
                int cx = hx + line.DirX * step;
                int cy = hy + line.DirY * step;

                // Left the board: the head exits safely from here on.
                if (cx < 0 || cx >= level.Width || cy < 0 || cy >= level.Height)
                {
                    return true;
                }

                for (int j = 0; j < n; j++)
                {
                    if (j == idx || !present[j]) continue;
                    if (level.Lines[j].OccupiesCell(cx, cy))
                    {
                        return false;
                    }
                }
            }
        }
    }
}
