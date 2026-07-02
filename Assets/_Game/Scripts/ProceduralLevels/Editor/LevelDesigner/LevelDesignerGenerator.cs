#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor.LevelDesigner
{
    /// <summary>
    /// Difficulty-aware generator following docs/level-generation-rules.md: bent, interlocking
    /// arrows (no 1-unit stubs, no long straight rows), balanced exit directions, real dependency
    /// chains, dense boards, and no isolated arrows.
    ///
    /// SOLUTION-FIRST construction: every new arrow is grown as a self-avoiding axis-aligned
    /// polyline and must still exit past all already-placed arrows
    /// (LevelSolvabilityValidator.CanNewLineExit). That guarantees the level is solvable
    /// (peel order = reverse of placement); later bent arrows crossing earlier ones create the
    /// locking dependencies that make it hard. Several candidates are generated; best is kept.
    /// </summary>
    public static class LevelDesignerGenerator
    {
        static readonly Vector2Int[] Dirs = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        // Reused scratch lists for the pairwise "does A block B" test (avoids per-call allocation).
        static readonly List<LevelLineData> _pair = new List<LevelLineData> { null, null };
        static readonly List<int> _pairIdx = new List<int> { 0, 1 };

        const int MaxSegment = 4; // Rule 4: cap a single straight run.

        // Back-compat entry point (defaults to Medium).
        public static LevelDesignBoard Generate(LevelDesignBoard board, int count, HashSet<Vector2Int> maskCells, int seed)
            => Generate(board, count, maskCells, seed, DesignDifficulty.Medium);

        public static LevelDesignBoard Generate(LevelDesignBoard board, int count, HashSet<Vector2Int> maskCells, int seed, DesignDifficulty difficulty)
        {
            count = Mathf.Clamp(count, 2, 60);
            int candidates = Mathf.Clamp(160 / count, 6, 16);

            LevelDesignBoard best = null; int bestScore = int.MinValue;
            LevelDesignBoard bestPass = null; int bestPassScore = int.MinValue;

            // Build the "seed" layout of ~count solution-first arrows (varied, bent, dependency-rich).
            for (int r = 0; r < candidates; r++)
            {
                var lines = BuildOne(count, maskCells, seed + r * 9173 + 1, difficulty);
                if (lines.Count < 2) continue;
                int score = Score(lines);
                if (score > bestScore) { bestScore = score; best = ToBoard(board, lines); }
                if (PassesGates(lines, difficulty, count) && score > bestPassScore)
                { bestPassScore = score; bestPass = ToBoard(board, lines); }
            }

            var winner = bestPass ?? best;
            if (winner != null)
            {
                // The requested count is a MINIMUM: add as many extra arrows as fit to FILL the board,
                // keeping only fillers that leave the whole level completable (global solvability, like
                // a manual add). Then extend arrows into any single-cell gaps the fillers can't take.
                FillBoardWithArrows(winner, maskCells, new System.Random(seed ^ 0x51ED270B));
                ExtendToFillGaps(winner, maskCells);
            }
            return winner;
        }

        // ---------------------------------------------------------------- Fill the board with arrows

        /// <summary>Adds extra filler arrows into empty cells until the board is full, keeping only
        /// fillers that leave the WHOLE level solvable (FindRemovalOrder) — same test as a manual add,
        /// so it can drop arrows into pockets that solution-first construction can't. Fillers are BENT
        /// (grown with the same self-avoiding walk as real arrows), preferring 2- then 1-bend shapes so
        /// they look good and add dependencies; a tiny straight one is only a last resort for a pocket
        /// too small to bend in. The requested count is thus a minimum; this fills to a complete level.</summary>
        static void FillBoardWithArrows(LevelDesignBoard board, HashSet<Vector2Int> mask, System.Random rng)
        {
            if (board.Arrows.Count == 0) return;

            var occupied = new HashSet<Vector2Int>();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var a in board.Arrows)
            {
                AddBodyCells(a, occupied);
                foreach (var p in a.Points)
                {
                    if (p.x < minX) minX = p.x; if (p.y < minY) minY = p.y;
                    if (p.x > maxX) maxX = p.x; if (p.y > maxY) maxY = p.y;
                }
            }

            HashSet<Vector2Int> domain;
            if (mask != null && mask.Count > 0) domain = mask;
            else
            {
                domain = new HashSet<Vector2Int>();
                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    domain.Add(new Vector2Int(x, y));
            }

            // Filler shape ladder: try a 2-bend arrow first, then 1-bend, then a length-2 straight
            // stub only as a last resort (keeps the board from filling with ugly little straights).
            int[] bendTry = { 2, 2, 1, 1, 0 };
            int[] minLenTry = { 3, 3, 3, 2, 2 };

            bool progress = true;
            int pass = 0;
            while (progress && pass++ < 6)
            {
                progress = false;
                var empties = new List<Vector2Int>();
                foreach (var c in domain) if (!occupied.Contains(c)) empties.Add(c);

                foreach (var c in empties)
                {
                    if (occupied.Contains(c)) continue;
                    for (int k = 0; k < bendTry.Length; k++)
                    {
                        if (!TryGrow(rng, domain, occupied, c, bendTry[k], minLenTry[k], out var pts, out var cells))
                            continue;

                        var arrow = new DesignArrow();
                        foreach (var p in pts) arrow.Points.Add(new Vector2Int(p.X, p.Y));
                        board.Arrows.Add(arrow);

                        if (LevelSolvabilityValidator.FindRemovalOrder(board.ToLineData()) != null)
                        {
                            foreach (var cell in cells) occupied.Add(cell);
                            progress = true;
                            break;
                        }
                        board.Arrows.RemoveAt(board.Arrows.Count - 1); // filler broke solvability — drop it
                    }
                }
            }
        }

        // ---------------------------------------------------------------- Gap filling by extension

        /// <summary>Grows arrow TAILS into leftover empty cells so the board looks full. A tail
        /// extension never changes an arrow's own exit (head), but can block others — so every
        /// extension is kept only if the whole level is still solvable. Runs once on the final board.
        /// Trapped pockets that no extension can reach solvably are left as a few stray dots.</summary>
        static void ExtendToFillGaps(LevelDesignBoard board, HashSet<Vector2Int> mask)
        {
            var arrows = board.Arrows;
            if (arrows.Count == 0) return;

            var occupied = new HashSet<Vector2Int>();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var a in arrows)
            {
                AddBodyCells(a, occupied);
                foreach (var p in a.Points)
                {
                    if (p.x < minX) minX = p.x; if (p.y < minY) minY = p.y;
                    if (p.x > maxX) maxX = p.x; if (p.y > maxY) maxY = p.y;
                }
            }

            // Fill within the mask (shape mode) or the arrows' own footprint (open mode).
            HashSet<Vector2Int> domain;
            if (mask != null && mask.Count > 0) domain = mask;
            else
            {
                domain = new HashSet<Vector2Int>();
                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    domain.Add(new Vector2Int(x, y));
            }

            var tailMap = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < arrows.Count; i++) tailMap[arrows[i].Points[0]] = i;

            var offsets = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

            bool progress = true;
            int pass = 0;
            while (progress && pass++ < 16)
            {
                progress = false;
                foreach (var cell in domain)
                {
                    if (occupied.Contains(cell)) continue;
                    foreach (var off in offsets)
                    {
                        var nb = cell + off;
                        if (!tailMap.TryGetValue(nb, out int ai)) continue;
                        var a = arrows[ai];
                        if (a.Points.Count >= 2 && a.Points[1] == cell) continue; // no 180° reversal

                        a.Points.Insert(0, cell); // extend tail into the empty cell
                        if (LevelSolvabilityValidator.FindRemovalOrder(board.ToLineData()) != null)
                        {
                            occupied.Add(cell);
                            tailMap.Remove(nb);
                            tailMap[cell] = ai;
                            progress = true;
                            break;
                        }
                        a.Points.RemoveAt(0); // reverted — extension broke solvability
                    }
                }
            }
        }

        static void AddBodyCells(DesignArrow a, HashSet<Vector2Int> cells)
        {
            for (int i = 0; i < a.Points.Count - 1; i++)
            {
                var p = a.Points[i];
                var q = a.Points[i + 1];
                int sx = q.x > p.x ? 1 : q.x < p.x ? -1 : 0;
                int sy = q.y > p.y ? 1 : q.y < p.y ? -1 : 0;
                int x = p.x, y = p.y;
                while (x != q.x || y != q.y) { cells.Add(new Vector2Int(x, y)); x += sx; y += sy; }
                cells.Add(new Vector2Int(q.x, q.y));
            }
        }

        // ---------------------------------------------------------------- Fast bent pack (high counts)

        /// <summary>Fast, solvable, BENT dense fill for large counts (&gt;60) where the geometric
        /// solution-first path would freeze the Editor. Trick: on an integer grid a cardinal exit
        /// corridor is clear iff no other arrow occupies a cell straight ahead of the head — an O(1)
        /// hash lookup per cell instead of raycasting every arrow. Each arrow is committed only if its
        /// corridor is clear past all already-placed arrows, so reverse-placement is a valid peel
        /// order ⇒ solvable by construction (no expensive final verify). Bent via the same walk.</summary>
        public static LevelDesignBoard FastBentPack(LevelDesignBoard board, int count, HashSet<Vector2Int> mask, int seed, DesignDifficulty difficulty)
        {
            count = Mathf.Clamp(count, 2, 500);
            var rng = new System.Random(seed);
            bool masked = mask != null && mask.Count > 0;

            HashSet<Vector2Int> domain;
            List<Vector2Int> domainList;
            int dMinX, dMinY, dMaxX, dMaxY;
            if (masked)
            {
                domain = mask;
                domainList = new List<Vector2Int>(mask);
                dMinX = int.MaxValue; dMinY = int.MaxValue; dMaxX = int.MinValue; dMaxY = int.MinValue;
                foreach (var c in mask)
                {
                    if (c.x < dMinX) dMinX = c.x; if (c.y < dMinY) dMinY = c.y;
                    if (c.x > dMaxX) dMaxX = c.x; if (c.y > dMaxY) dMaxY = c.y;
                }
            }
            else
            {
                // Tight region so the fixed ~solution-first density still fills the board well.
                float f = difficulty switch
                {
                    DesignDifficulty.Easy   => 3.2f,
                    DesignDifficulty.Medium => 4f,
                    DesignDifficulty.Hard   => 5f,
                    _                       => 6f, // Expert
                };
                int side = Mathf.Max(2, Mathf.RoundToInt((Mathf.Sqrt(count * f) - 1f) / 2f));
                domain = new HashSet<Vector2Int>();
                domainList = new List<Vector2Int>();
                for (int y = -side; y <= side; y++)
                for (int x = -side; x <= side; x++)
                {
                    var c = new Vector2Int(x, y);
                    domain.Add(c);
                    domainList.Add(c);
                }
                dMinX = -side; dMinY = -side; dMaxX = side; dMaxY = side;
            }

            int minLen = difficulty == DesignDifficulty.Easy ? 2 : 3;
            var bendBag = BuildBendBag(count, difficulty, rng);
            var occupied = new HashSet<Vector2Int>();
            var placed = new List<LevelLineData>();

            bool CorridorClear(Vector2Int head, Vector2Int dir)
            {
                var c = head + dir;
                while (c.x >= dMinX && c.x <= dMaxX && c.y >= dMinY && c.y <= dMaxY)
                {
                    if (occupied.Contains(c)) return false; // an arrow body blocks the exit lane
                    c += dir;
                }
                return true; // ray leaves the board without hitting anything
            }

            bool TryFast(int want, int ml)
            {
                for (int t = 0; t < 30; t++)
                {
                    var start = domainList[rng.Next(domainList.Count)];
                    int bt = want - (t % (want + 1)); // ladder: want, want-1, … 0
                    if (!TryGrow(rng, domain, occupied, start, bt, ml, out var pts, out var cells))
                        continue;

                    var head = new Vector2Int(pts[pts.Count - 1].X, pts[pts.Count - 1].Y);
                    var pen = new Vector2Int(pts[pts.Count - 2].X, pts[pts.Count - 2].Y);
                    var dir = LevelLineData.SnapToAxis(head - pen);
                    if (!CorridorClear(head, dir)) continue;

                    foreach (var cc in cells) occupied.Add(cc);
                    var line = new LevelLineData();
                    foreach (var p in pts) line.Points.Add(p);
                    placed.Add(line);
                    return true;
                }
                return false;
            }

            for (int i = 0; i < count; i++)
                if (!TryFast(bendBag[i], minLen)) TryFast(0, 2);

            // Fill the remaining gaps with bent arrows, verified by a fast GRID-based global peel
            // check (lets fillers be removed mid-solve, so the board packs far fuller than the
            // solution-first base alone) — the same O(1)-per-cell trick keeps it quick at big counts.
            int[] bendTry = { 2, 2, 1, 1, 0 };
            int[] mlTry = { 3, 3, 3, 2, 2 };
            bool progress = true;
            int pass = 0;
            while (progress && pass++ < 6)
            {
                progress = false;
                var empties = new List<Vector2Int>();
                foreach (var c in domain) if (!occupied.Contains(c)) empties.Add(c);
                foreach (var c in empties)
                {
                    if (occupied.Contains(c)) continue;
                    for (int k = 0; k < bendTry.Length; k++)
                    {
                        if (!TryGrow(rng, domain, occupied, c, bendTry[k], mlTry[k], out var pts, out var cells))
                            continue;
                        var line = new LevelLineData();
                        foreach (var p in pts) line.Points.Add(p);
                        placed.Add(line);
                        if (GridSolvable(placed, dMinX, dMinY, dMaxX, dMaxY))
                        {
                            foreach (var cc in cells) occupied.Add(cc);
                            progress = true;
                            break;
                        }
                        placed.RemoveAt(placed.Count - 1);
                    }
                }
            }

            // Absorb any leftover empty cells (trapped single dots the bent fillers can't take, since
            // the smallest arrow is 2 cells) into adjacent arrow tails — verified by the same fast grid
            // peel check. This brings the fast path up to the near-complete fill of the slow path.
            GridExtendTails(placed, domain, occupied, dMinX, dMinY, dMaxX, dMaxY);

            return ToBoard(board, placed);
        }

        /// <summary>Grid-based tail extension: grows arrow TAILS into leftover empty cells, kept only
        /// if the whole board still peels (O(1)-per-cell grid check). The fast counterpart to
        /// ExtendToFillGaps — lets FastBentPack reach ~complete fill without the costly FindRemovalOrder.</summary>
        static void GridExtendTails(List<LevelLineData> placed, HashSet<Vector2Int> domain,
                                    HashSet<Vector2Int> occupied, int minX, int minY, int maxX, int maxY)
        {
            // GridSolvable is O(n²); running it per candidate is only affordable up to a few hundred
            // arrows. On very large boards the fast gap-fill's ~90% already looks full at that
            // resolution, so skip the extra squeeze rather than risk a multi-second stall.
            if (placed.Count > 300) return;

            var offsets = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            bool progress = true;
            int pass = 0;
            while (progress && pass++ < 10)
            {
                progress = false;
                var tailMap = new Dictionary<Vector2Int, int>();
                for (int i = 0; i < placed.Count; i++)
                {
                    var t = placed[i].Points[0];
                    tailMap[new Vector2Int(t.X, t.Y)] = i;
                }

                var empties = new List<Vector2Int>();
                foreach (var c in domain) if (!occupied.Contains(c)) empties.Add(c);

                foreach (var c in empties)
                {
                    if (occupied.Contains(c)) continue;
                    foreach (var off in offsets)
                    {
                        var nb = c + off;
                        if (!tailMap.TryGetValue(nb, out int ai)) continue;
                        var a = placed[ai];
                        if (a.PointCount >= 2 && a.Points[1].X == c.x && a.Points[1].Y == c.y) continue; // no 180° reversal
                        a.Points.Insert(0, new GridPoint(c.x, c.y)); // extend tail into the empty cell
                        if (GridSolvable(placed, minX, minY, maxX, maxY))
                        {
                            occupied.Add(c);
                            tailMap.Remove(nb);
                            tailMap[c] = ai;
                            progress = true;
                            break;
                        }
                        a.Points.RemoveAt(0); // reverted — extension broke solvability
                    }
                }
            }
        }

        static void AddLineCells(LevelLineData l, List<Vector2Int> cells)
        {
            for (int i = 0; i < l.PointCount - 1; i++)
            {
                var a = l.Points[i];
                var b = l.Points[i + 1];
                int sx = b.X > a.X ? 1 : b.X < a.X ? -1 : 0;
                int sy = b.Y > a.Y ? 1 : b.Y < a.Y ? -1 : 0;
                int x = a.X, y = a.Y;
                while (x != b.X || y != b.Y) { cells.Add(new Vector2Int(x, y)); x += sx; y += sy; }
                cells.Add(new Vector2Int(b.X, b.Y));
            }
        }

        /// <summary>Grid-based peel solvability: repeatedly remove any arrow whose cardinal exit lane
        /// is free of OTHER remaining arrows' cells (O(1) hash lookups). Solvable iff all peel.</summary>
        static bool GridSolvable(List<LevelLineData> lines, int minX, int minY, int maxX, int maxY)
        {
            int n = lines.Count;
            var owner = new Dictionary<Vector2Int, int>();
            var cellsOf = new List<List<Vector2Int>>(n);
            var active = new HashSet<Vector2Int>();
            for (int i = 0; i < n; i++)
            {
                var cs = new List<Vector2Int>();
                AddLineCells(lines[i], cs);
                cellsOf.Add(cs);
                foreach (var c in cs) { owner[c] = i; active.Add(c); }
            }

            var heads = new Vector2Int[n];
            var dirs = new Vector2Int[n];
            for (int i = 0; i < n; i++) { heads[i] = lines[i].GetHead(); dirs[i] = lines[i].GetDirection(); }

            var remaining = new List<int>(n);
            for (int i = 0; i < n; i++) remaining.Add(i);

            while (remaining.Count > 0)
            {
                int found = -1;
                for (int r = remaining.Count - 1; r >= 0; r--)
                {
                    int idx = remaining[r];
                    var d = dirs[idx];
                    var c = heads[idx] + d;
                    bool clear = true;
                    while (c.x >= minX && c.x <= maxX && c.y >= minY && c.y <= maxY)
                    {
                        if (active.Contains(c) && owner.TryGetValue(c, out int ow) && ow != idx) { clear = false; break; }
                        c += d;
                    }
                    if (clear)
                    {
                        found = idx;
                        remaining.RemoveAt(r);
                        foreach (var cc in cellsOf[idx]) active.Remove(cc);
                        break;
                    }
                }
                if (found < 0) return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- Build one candidate

        static List<LevelLineData> BuildOne(int count, HashSet<Vector2Int> mask, int seed, DesignDifficulty difficulty, float regionScale = 1f)
        {
            var rng = new System.Random(seed);
            bool masked = mask != null && mask.Count > 0;

            List<Vector2Int> domainList;
            HashSet<Vector2Int> domain;
            if (masked)
            {
                domain = mask;
                domainList = new List<Vector2Int>(mask);
            }
            else
            {
                // Region scales with arrow length (grows with difficulty) so harder levels still
                // fit, while staying tight enough that arrows interlock instead of scattering
                // (Rule 15: dense board, Rule 10: fewer isolated arrows).
                float f = difficulty switch
                {
                    DesignDifficulty.Easy   => 6f,
                    DesignDifficulty.Medium => 8f,
                    DesignDifficulty.Hard   => 11f,
                    _                       => 14f, // Expert
                };
                int side = Mathf.Max(2, Mathf.RoundToInt((Mathf.Sqrt(count * f * regionScale) - 1f) / 2f));
                domain = new HashSet<Vector2Int>();
                domainList = new List<Vector2Int>();
                for (int y = -side; y <= side; y++)
                for (int x = -side; x <= side; x++)
                {
                    var c = new Vector2Int(x, y);
                    domain.Add(c);
                    domainList.Add(c);
                }
            }

            int minLen = difficulty == DesignDifficulty.Easy ? 2 : 3; // Rule 3: no 1-unit arrows.
            int maxPerDir = Mathf.Max(2, Mathf.CeilToInt(0.4f * count)); // Rule 8: no direction > 40%.
            var bendBag = BuildBendBag(count, difficulty, rng);

            var occupied = new HashSet<Vector2Int>();
            var laneCount = new Dictionary<long, int>();
            int[] dirCount = new int[4];
            var placed = new List<LevelLineData>();

            // Places one arrow. relax 0 = strict (exact bends, lane/dir caps); 1 = ladder (fewer
            // bends / shorter as needed); 2 = gap-fill (caps off, tiny fillers to saturate the
            // board and remove empty dots). Returns true if an arrow was committed.
            bool TryPlace(int relax, int want)
            {
                int[] bendOpts = relax == 0 ? new[] { want }
                               : relax == 1 ? DescendingBends(want)
                               : new[] { 0, 1 };
                int[] mlOpts = relax == 0 ? new[] { minLen }
                             : relax == 1 ? new[] { minLen, 2 }
                             : new[] { 2 };
                int attempts = relax < 2 ? 40 : 60;

                List<GridPoint> bestPts = null;
                List<Vector2Int> bestCells = null;
                Vector2Int bestExitDir = default;
                long bestKey = long.MinValue;

                for (int t = 0; t < attempts; t++)
                {
                    var start = domainList[rng.Next(domainList.Count)];
                    int bt = bendOpts[t % bendOpts.Length];
                    int ml = mlOpts[t % mlOpts.Length];
                    if (!TryGrow(rng, domain, occupied, start, bt, ml, out var pts, out var cells))
                        continue;

                    var cand = new LevelLineData();
                    foreach (var p in pts) cand.Points.Add(p);
                    var exitDir = cand.GetDirection();
                    bool horizontal = exitDir.x != 0;
                    var head = cand.GetHead();
                    long lk = LaneKey(horizontal, horizontal ? head.y : head.x);

                    if (relax < 2)
                    {
                        if (laneCount.TryGetValue(lk, out int lc) && lc >= 3) continue;       // Rule 7
                        if (dirCount[DirIndex(exitDir)] >= maxPerDir) continue;               // Rule 8
                    }

                    // Rule 6: must remain removable past everything already placed.
                    if (!LevelSolvabilityValidator.CanNewLineExit(placed, cand)) continue;

                    int bends = Mathf.Max(0, pts.Count - 2);
                    bool blocks = placed.Count == 0 || BlocksAny(cand, placed);               // Rule 11
                    long key = (blocks ? 1_000_000L : 0) + (long)bends * 10_000 + cells.Count;

                    if (key > bestKey)
                    {
                        bestKey = key; bestPts = pts; bestCells = cells; bestExitDir = exitDir;
                    }
                    if (blocks && bends >= want && relax == 0) break; // good enough.
                }

                if (bestPts == null) return false;

                foreach (var c in bestCells) occupied.Add(c);
                dirCount[DirIndex(bestExitDir)]++;
                bool horiz = bestExitDir.x != 0;
                int laneCoord = horiz ? bestPts[bestPts.Count - 1].Y : bestPts[bestPts.Count - 1].X;
                long laneKey2 = LaneKey(horiz, laneCoord);
                laneCount[laneKey2] = laneCount.TryGetValue(laneKey2, out int v) ? v + 1 : 1;

                var line = new LevelLineData();
                foreach (var p in bestPts) line.Points.Add(p);
                placed.Add(line);
                return true;
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryPlace(0, bendBag[i]))
                    TryPlace(1, bendBag[i]); // fallback ladder
            }

            // Gap-fill with proper exitable fillers (bounded — the board saturates fast).
            int guard = 0;
            while (placed.Count < count && guard++ < count * 2)
            {
                if (!TryPlace(2, 0)) break;
            }

            return placed;
        }

        static int[] DescendingBends(int want)
        {
            var a = new int[want + 1];
            for (int i = 0; i <= want; i++) a[i] = want - i;
            return a;
        }

        // ---------------------------------------------------------------- Bent self-avoiding walk

        /// <summary>Grows one arrow: a self-avoiding, axis-aligned polyline with up to
        /// <paramref name="wantBends"/> turns and total length ≥ <paramref name="minLen"/>. Returns
        /// only corner points (tail, each bend, head). Fails if it can't make the minimum length.</summary>
        static bool TryGrow(System.Random rng, HashSet<Vector2Int> domain,
                            HashSet<Vector2Int> occupied, Vector2Int start, int wantBends, int minLen,
                            out List<GridPoint> points, out List<Vector2Int> cells)
        {
            points = null;
            cells = null;

            // Bound the walk to the region in BOTH modes: in shape mode `domain` is the mask; in
            // open mode it's the sized square. Without this, open-mode arrows wander freely and the
            // board ends up sparse (lots of empty space between arrows).
            bool Free(Vector2Int p, HashSet<Vector2Int> visited)
                => domain.Contains(p) && !occupied.Contains(p) && !visited.Contains(p);

            if (occupied.Contains(start) || !domain.Contains(start))
                return false;

            var visitedCells = new HashSet<Vector2Int> { start };
            var body = new List<Vector2Int> { start };
            var corners = new List<Vector2Int> { start };
            var cur = start;

            // Pick a first direction with room.
            Vector2Int dir = default;
            bool haveDir = false;
            foreach (var d in Shuffled(Dirs, rng))
                if (Free(cur + d, visitedCells)) { dir = d; haveDir = true; break; }
            if (!haveDir) return false;

            int totalLen = 0;
            int segTarget = wantBends + 1;

            for (int si = 0; si < segTarget; si++)
            {
                if (si > 0)
                {
                    // Turn perpendicular (never reverse) — Rule 5.
                    Vector2Int nd = default;
                    bool turned = false;
                    foreach (var pd in Shuffled(Perp(dir), rng))
                        if (Free(cur + pd, visitedCells)) { nd = pd; turned = true; break; }
                    if (!turned) break; // boxed in — stop with fewer bends.
                    dir = nd;
                }

                int segLen = 1 + rng.Next(MaxSegment); // 1..MaxSegment
                int stepped = 0;
                for (int s = 0; s < segLen; s++)
                {
                    var n = cur + dir;
                    if (!Free(n, visitedCells)) break;
                    cur = n;
                    visitedCells.Add(n);
                    body.Add(n);
                    stepped++;
                    totalLen++;
                }

                if (stepped == 0)
                {
                    if (si == 0) return false;
                    break;
                }

                corners.Add(cur);
            }

            if (totalLen < minLen || corners.Count < 2)
                return false;

            points = new List<GridPoint>(corners.Count);
            foreach (var c in corners) points.Add(new GridPoint(c.x, c.y));
            cells = body;
            return true;
        }

        static Vector2Int[] Perp(Vector2Int d) =>
            d.x != 0
                ? new[] { new Vector2Int(0, 1), new Vector2Int(0, -1) }
                : new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0) };

        static Vector2Int[] Shuffled(Vector2Int[] src, System.Random rng)
        {
            var a = (Vector2Int[])src.Clone();
            for (int i = a.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        // ---------------------------------------------------------------- Scoring (Rules 10-12)

        static int Score(List<LevelLineData> lines)
        {
            int n = lines.Count;
            double sumLen = 0, sumBends = 0;
            foreach (var l in lines)
            {
                for (int i = 0; i < l.PointCount - 1; i++)
                {
                    var a = l.Points[i];
                    var b = l.Points[i + 1];
                    sumLen += Mathf.Abs(b.X - a.X) + Mathf.Abs(b.Y - a.Y);
                }
                sumBends += Mathf.Max(0, l.PointCount - 2);
            }
            double avgLen = n > 0 ? sumLen / n : 0;
            double avgBends = n > 0 ? sumBends / n : 0;

            int edges = 0;
            var hasIn = new bool[n];
            var hasOut = new bool[n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                if (Blocks(lines[i], lines[j])) { edges++; hasIn[i] = true; hasOut[j] = true; } // i blocked by j
            }

            int isolated = 0;
            for (int i = 0; i < n; i++) if (!hasIn[i] && !hasOut[i]) isolated++;

            int decisions = LevelDifficultyAnalyzer.CountDecisionPoints(lines); // Rule 12 branching proxy

            // + 3*n rewards fuller boards (more arrows placed in the region ⇒ tighter packing,
            // fewer gaps between arrows).
            return 3 * edges + 2 * decisions + (int)(2 * avgLen) + (int)(4 * avgBends) + 3 * n - 5 * isolated;
        }

        // ---------------------------------------------------------------- Accept/reject gates (Rules 8-13)

        /// <summary>Hard gates a finished candidate must satisfy, mirroring the checklist in
        /// docs/level-generation-rules.md. Thresholds are tuned to what solution-first greedy
        /// generation can actually reach (validated in the Python sim).</summary>
        static bool PassesGates(List<LevelLineData> lines, DesignDifficulty difficulty, int req)
        {
            int n = lines.Count;
            if (n < 2) return false;

            // Rule 6: solvable.
            if (LevelSolvabilityValidator.FindRemovalOrder(lines) == null) return false;

            // Rule 9: used cells span ≥ ceil(sqrt(n)) distinct rows AND columns.
            var rows = new HashSet<int>();
            var cols = new HashSet<int>();
            foreach (var l in lines) CollectRowsCols(l, rows, cols);
            int spanTarget = Mathf.CeilToInt(Mathf.Sqrt(n));
            if (rows.Count < spanTarget || cols.Count < spanTarget) return false;

            // Rules 10/12: dependency graph — isolated count + longest must-precede chain.
            var succ = new List<int>[n];
            for (int i = 0; i < n; i++) succ[i] = new List<int>();
            var hasIn = new bool[n];
            var hasOut = new bool[n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                if (Blocks(lines[i], lines[j])) { hasIn[i] = true; hasOut[j] = true; succ[j].Add(i); } // j precedes i
            }

            int isolated = 0;
            for (int i = 0; i < n; i++) if (!hasIn[i] && !hasOut[i]) isolated++;
            int allowIsolated = difficulty == DesignDifficulty.Easy ? n : (n <= 6 ? 1 : Mathf.CeilToInt(0.12f * n));
            if (isolated > allowIsolated) return false; // Rule 10

            int depTarget = difficulty switch
            {
                DesignDifficulty.Medium => 1,
                DesignDifficulty.Hard   => 2,
                DesignDifficulty.Expert => 2,
                _                       => 0, // Easy
            };
            if (depTarget > 0)
            {
                int need = Mathf.Min(depTarget, Mathf.Max(1, n / 3));
                if (LongestChain(succ, n) < need) return false; // Rule 12
            }

            // Rule 13: not too many arrows removable on the opening tap (not trivial), but ≥1.
            int openings = LevelSolvabilityValidator.GetRemovableIndices(lines).Count;
            int openCap = Mathf.Max(4, Mathf.CeilToInt(0.6f * n));
            if (openings < 1 || openings > openCap) return false;

            // Rule 8: no exit direction over the per-direction cap (also enforced during placement).
            int[] dc = new int[4];
            foreach (var l in lines) dc[DirIndex(l.GetDirection())]++;
            int maxPerDir = Mathf.Max(2, Mathf.CeilToInt(0.4f * req));
            for (int i = 0; i < 4; i++) if (dc[i] > maxPerDir) return false;

            return true;
        }

        static void CollectRowsCols(LevelLineData l, HashSet<int> rows, HashSet<int> cols)
        {
            for (int i = 0; i < l.PointCount - 1; i++)
            {
                var a = l.Points[i];
                var b = l.Points[i + 1];
                // NOTE: not Mathf.Sign — it returns 1 for 0, which would make an axis-aligned
                // segment walk diagonally and never reach its endpoint (infinite loop).
                int sx = b.X > a.X ? 1 : b.X < a.X ? -1 : 0;
                int sy = b.Y > a.Y ? 1 : b.Y < a.Y ? -1 : 0;
                int x = a.X, y = a.Y;
                while (x != b.X || y != b.Y) { rows.Add(y); cols.Add(x); x += sx; y += sy; }
                rows.Add(b.Y); cols.Add(b.X);
            }
        }

        /// <summary>Longest path in the must-precede DAG (difficulty proxy) with a cycle guard.</summary>
        static int LongestChain(List<int>[] succ, int n)
        {
            var memo = new int[n];
            for (int i = 0; i < n; i++) memo[i] = -1;
            var stack = new HashSet<int>();
            int best = 0;
            for (int i = 0; i < n; i++) best = Mathf.Max(best, ChainDfs(i, succ, memo, stack));
            return best;
        }

        static int ChainDfs(int u, List<int>[] succ, int[] memo, HashSet<int> stack)
        {
            if (memo[u] >= 0) return memo[u];
            stack.Add(u);
            int best = 0;
            foreach (int v in succ[u])
            {
                if (stack.Contains(v)) continue; // cycle guard
                best = Mathf.Max(best, 1 + ChainDfs(v, succ, memo, stack));
            }
            stack.Remove(u);
            memo[u] = best;
            return best;
        }

        // ---------------------------------------------------------------- Helpers

        static bool BlocksAny(LevelLineData mover, List<LevelLineData> bodies)
        {
            for (int i = 0; i < bodies.Count; i++)
                if (Blocks(mover, bodies[i])) return true;
            return false;
        }

        /// <summary>True if <paramref name="mover"/> cannot exit because <paramref name="body"/> is
        /// in its forward corridor (i.e. body blocks mover -> a dependency mover-after-body).</summary>
        static bool Blocks(LevelLineData mover, LevelLineData body)
        {
            _pair[0] = mover;
            _pair[1] = body;
            return !LevelSolvabilityValidator.CanExit(_pair, 0, _pairIdx);
        }

        static List<int> BuildBendBag(int count, DesignDifficulty difficulty, System.Random rng)
        {
            // Weighted bend counts per difficulty (Rule 4 + ramp table G).
            int[] weights = difficulty switch
            {
                DesignDifficulty.Easy    => new[] { 30, 50, 20, 0, 0 },  // 0..4 bends
                DesignDifficulty.Medium  => new[] { 10, 40, 40, 10, 0 },
                DesignDifficulty.Hard    => new[] { 0, 30, 40, 30, 0 },
                _                        => new[] { 0, 20, 30, 30, 20 }, // Expert
            };

            var bag = new List<int>(count);
            for (int i = 0; i < count; i++)
                bag.Add(WeightedPick(weights, rng));
            for (int i = bag.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (bag[i], bag[j]) = (bag[j], bag[i]); }
            return bag;
        }

        static int WeightedPick(int[] weights, System.Random rng)
        {
            int total = 0;
            foreach (int w in weights) total += w;
            int roll = rng.Next(total);
            for (int i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return 0;
        }

        static int DirIndex(Vector2Int d)
        {
            if (d.y == 1) return 0;
            if (d.y == -1) return 1;
            if (d.x == -1) return 2;
            return 3;
        }

        static long LaneKey(bool horizontal, int coord) =>
            ((long)(horizontal ? 0 : 1) << 32) ^ (uint)(coord + 1000000);

        static LevelDesignBoard ToBoard(LevelDesignBoard src, List<LevelLineData> lines)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var l in lines)
            foreach (var p in l.Points)
            {
                if (p.X < minX) minX = p.X; if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X; if (p.Y > maxY) maxY = p.Y;
            }
            int ox = (minX + maxX) / 2, oy = (minY + maxY) / 2;

            var b = new LevelDesignBoard { Width = src.Width, Height = src.Height };
            foreach (var l in lines)
            {
                var a = new DesignArrow();
                foreach (var p in l.Points) a.Points.Add(new Vector2Int(p.X - ox, p.Y - oy));
                b.Arrows.Add(a);
            }
            return b;
        }
    }
}
#endif
