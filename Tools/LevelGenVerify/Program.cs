using System;
using _Game.Generation;

public static class Program
{
    public static int Main()
    {
        int maxLevel = ProceduralLevelBuilder.MaxLevel;
        int failures = 0;
        int emptyish = 0;
        int prevCount = 0;
        int nonMonotonic = 0;

        Console.WriteLine($"Verifying procedural levels 1..{maxLevel}");
        Console.WriteLine("level  lines  board  solvable  reverseOrderClears");

        for (int level = 1; level <= maxLevel; level++)
        {
            GeneratedLevel lvl = ProceduralLevelBuilder.Build(level);

            // Determinism: building again must be identical.
            GeneratedLevel lvl2 = ProceduralLevelBuilder.Build(level);
            if (lvl.Lines.Count != lvl2.Lines.Count)
            {
                Console.WriteLine($"  [DET FAIL] level {level} non-deterministic count");
                failures++;
            }

            bool solvable = ProceduralLevelSolver.IsSolvable(lvl);
            bool reverseClears = ReversePlacementOrderClears(lvl);

            if (!solvable) failures++;
            if (!reverseClears) failures++;
            if (lvl.Lines.Count < 2) emptyish++;
            if (lvl.Lines.Count < prevCount) nonMonotonic++;
            prevCount = lvl.Lines.Count;

            // Print a sampled subset to keep output readable, plus any failures.
            if (level <= 5 || level % 50 == 0 || level == maxLevel || !solvable || !reverseClears)
            {
                Console.WriteLine($"{level,5}  {lvl.Lines.Count,5}  {lvl.Width}x{lvl.Height,-3}  {solvable,-8}  {reverseClears}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"RESULT: {(failures == 0 ? "ALL SOLVABLE" : failures + " FAILURES")}");
        Console.WriteLine($"levels with <2 lines: {emptyish}");
        Console.WriteLine($"levels where line count dipped vs previous (local, expected with rejection sampling): {nonMonotonic}");
        Console.WriteLine($"line count: L1={ProceduralLevelBuilder.Build(1).Lines.Count}, "
            + $"L100={ProceduralLevelBuilder.Build(100).Lines.Count}, "
            + $"L250={ProceduralLevelBuilder.Build(250).Lines.Count}, "
            + $"L500={ProceduralLevelBuilder.Build(500).Lines.Count}");

        return failures == 0 ? 0 : 1;
    }

    // Independently confirm the construction's promised solution (reverse-placement order)
    // actually clears the board under the solver's collision rules.
    private static bool ReversePlacementOrderClears(GeneratedLevel level)
    {
        int n = level.Lines.Count;
        bool[] present = new bool[n];
        for (int i = 0; i < n; i++) present[i] = true;

        for (int idx = n - 1; idx >= 0; idx--)
        {
            if (!ForwardRayClear(level, idx, present))
            {
                return false;
            }
            present[idx] = false;
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
            if (cx < 0 || cx >= level.Width || cy < 0 || cy >= level.Height) return true;

            for (int j = 0; j < n; j++)
            {
                if (j == idx || !present[j]) continue;
                if (level.Lines[j].OccupiesCell(cx, cy)) return false;
            }
        }
    }
}
