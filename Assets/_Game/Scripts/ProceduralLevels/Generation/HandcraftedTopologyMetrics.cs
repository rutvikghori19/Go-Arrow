using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels
{
  public struct HandcraftedTopologyReport
  {
    public int Branches;
    public int Loops;
    public int Traps;
    public int DecisionPoints;
    public int SolutionCount;
    public bool HasUniqueSolution;
  }

  public static class HandcraftedTopologyMetrics
  {
    public static HandcraftedTopologyReport Analyze(IReadOnlyList<LevelLineData> lines)
    {
      var report = new HandcraftedTopologyReport
      {
        Branches = CountStructuralBranches(lines),
        Loops = CountLoops(lines),
        Traps = CountTraps(lines),
        DecisionPoints = LevelDifficultyAnalyzer.CountDecisionPoints(lines),
        SolutionCount = CountSolutionPaths(lines, 4),
        HasUniqueSolution = false
      };
      report.HasUniqueSolution = report.SolutionCount == 1;
      return report;
    }

    public static bool MeetsTargets(IReadOnlyList<LevelLineData> lines, HandcraftedLevelSpec spec, int tolerance = 1)
    {
      var report = Analyze(lines);
      return report.HasUniqueSolution
             && Mathf.Abs(report.Branches - spec.Branches) <= tolerance
             && Mathf.Abs(report.Loops - spec.Loops) <= tolerance
             && Mathf.Abs(report.Traps - spec.Traps) <= tolerance;
    }

    static int CountStructuralBranches(IReadOnlyList<LevelLineData> lines)
    {
      var degree = new Dictionary<long, int>();
      foreach (var line in lines)
      {
        for (int i = 0; i < line.PointCount - 1; i++)
        {
          var a = line.Points[i].ToVector2Int();
          var b = line.Points[i + 1].ToVector2Int();
          Inc(degree, Pack(a));
          Inc(degree, Pack(b));
        }
      }

      int branches = 0;
      foreach (var kv in degree)
      {
        if (kv.Value >= 3)
          branches++;
      }

      return Mathf.Max(branches / 2, LevelDifficultyAnalyzer.CountDecisionPoints(lines) / 2);
    }

    static int CountLoops(IReadOnlyList<LevelLineData> lines)
    {
      var edges = new HashSet<long>();
      int totalSegments = 0;
      foreach (var line in lines)
      {
        for (int i = 0; i < line.PointCount - 1; i++)
        {
          var a = line.Points[i].ToVector2Int();
          var b = line.Points[i + 1].ToVector2Int();
          edges.Add(PackEdge(a, b));
          totalSegments++;
        }
      }

      int nodes = 0;
      var seen = new HashSet<long>();
      foreach (var line in lines)
      {
        foreach (var p in line.Points)
        {
          long key = Pack(p.ToVector2Int());
          if (seen.Add(key))
            nodes++;
        }
      }

      int cycles = totalSegments - nodes + CountComponents(lines);
      return Mathf.Max(0, cycles);
    }

    static int CountComponents(IReadOnlyList<LevelLineData> lines)
    {
      var adj = new Dictionary<long, List<long>>();
      foreach (var line in lines)
      {
        for (int i = 0; i < line.PointCount - 1; i++)
        {
          var a = Pack(line.Points[i].ToVector2Int());
          var b = Pack(line.Points[i + 1].ToVector2Int());
          Link(adj, a, b);
          Link(adj, b, a);
        }
      }

      var visited = new HashSet<long>();
      int components = 0;
      foreach (var node in adj.Keys)
      {
        if (visited.Contains(node))
          continue;
        components++;
        var stack = new Stack<long>();
        stack.Push(node);
        visited.Add(node);
        while (stack.Count > 0)
        {
          long cur = stack.Pop();
          if (!adj.TryGetValue(cur, out var list))
            continue;
          foreach (long nxt in list)
          {
            if (visited.Add(nxt))
              stack.Push(nxt);
          }
        }
      }

      return Mathf.Max(1, components);
    }

    static int CountTraps(IReadOnlyList<LevelLineData> lines)
    {
      if (lines == null || lines.Count < 2)
        return 0;

      var all = new List<int>();
      for (int i = 0; i < lines.Count; i++)
        all.Add(i);

      int traps = 0;
      var startMoves = LevelSolvabilityValidator.GetRemovableIndices(lines, all);
      foreach (int move in startMoves)
      {
        var remaining = new List<int>(all);
        remaining.Remove(move);
        if (!LevelSolvabilityValidator.IsSolvableSubset(lines, remaining))
          traps++;
      }

      return traps;
    }

    public static int CountSolutionPaths(IReadOnlyList<LevelLineData> lines, int cap)
    {
      if (lines == null || lines.Count == 0)
        return 0;

      var remaining = new List<int>();
      for (int i = 0; i < lines.Count; i++)
        remaining.Add(i);

      return CountDfs(lines, remaining, cap);
    }

    static int CountDfs(IReadOnlyList<LevelLineData> lines, List<int> remaining, int cap)
    {
      if (remaining.Count == 0)
        return 1;

      var moves = LevelSolvabilityValidator.GetRemovableIndices(lines, remaining);
      if (moves.Count == 0)
        return 0;

      int total = 0;
      foreach (int move in moves)
      {
        var next = new List<int>(remaining);
        next.Remove(move);
        total += CountDfs(lines, next, cap);
        if (total >= cap)
          return total;
      }

      return total;
    }

    static void Inc(Dictionary<long, int> map, long key)
    {
      if (!map.ContainsKey(key))
        map[key] = 0;
      map[key]++;
    }

    static void Link(Dictionary<long, List<long>> adj, long a, long b)
    {
      if (!adj.TryGetValue(a, out var list))
      {
        list = new List<long>();
        adj[a] = list;
      }
      list.Add(b);
    }

    static long Pack(Vector2Int c) => ((long)(c.x + 512) << 16) | (long)(c.y + 512);

    static long PackEdge(Vector2Int a, Vector2Int b)
    {
      if (a.x > b.x || (a.x == b.x && a.y > b.y))
        (a, b) = (b, a);
      return ((long)(a.x + 512) << 40) | ((long)(a.y + 512) << 20) |
             ((long)(b.x + 512) << 10) | (long)(b.y + 512);
    }
  }
}
