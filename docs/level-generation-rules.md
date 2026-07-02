# Go-Arrow — Level Generation Rules

Design spec for the AI level generator (both **shape** and **open / non-shape** levels).
Grounded in the actual game mechanic (`LevelSolvabilityValidator`) and measured from the
hand-authored reference levels **9** and **10**.

---

## 0. The mechanic (why these rules exist)

- An arrow is an ordered polyline of integer grid points. **Head** = last point;
  **exit direction** = the arrow's *last segment*.
- Tapping fires a ray from the head along the exit direction (ray step `0.35`, range `64`).
  It is **blocked** if any point on that ray comes within **`0.42` units** of *another*
  arrow's body.
- A level is solvable **iff a peel order exists** — repeatedly remove any arrow whose
  corridor is currently clear.
- **Arrow B locks arrow A** when B's body sits on the straight path in front of A's head, so
  B must be removed first. A **turn/bend** is powerful because a bent body lies across two
  directions at once — one L/U/Z arrow can block several arrows and can only clear after its
  own blockers move. Bends are how difficulty (dependency depth) is manufactured.

## Reference measurements (levels 9 & 10)

| Metric | Level 9 | Level 10 |
|---|---|---|
| Arrows | 24 | 40 |
| Grid | 1-unit integer | 1-unit integer |
| Points/arrow | 2–15 (avg 6.0) | 2–11 (avg 4.3) |
| Body length/arrow | 1–32 (avg 9.75) | 1–20 (avg 7.7) |
| Segment length | 1–9 (avg 1.93) | 1–15 (avg 2.33) |
| Bends/arrow | 0–11 (avg 3.83) | 0–7 (avg 2.0) |
| Straight arrows | 5/24 (~21%) | 13/40 (~33%) |
| Exit directions | axis-aligned, up-biased | axis-aligned, 4-way balanced |

---

## A. Universal geometry rules (both modes)

1. **Integer grid, axis-aligned only.** Every point is an integer cell; every segment is
   horizontal or vertical (the 0.42 / 0.35 constants assume 1-unit spacing).
2. **No overlap.** No two arrows share a body cell; no arrow crosses another's body cell.
3. **Minimum size — no 1-unit arrows.** Total body length ≥ **2 units** (Tutorial), ≥ **3
   units** from Easy onward. Never emit a length-1 arrow.
4. **Bends mandatory above Tutorial.** Target avg bends by band (see G). Segment length between
   bends 1–4 units; cap any single straight run at ~5 units.
5. **No self-touching.** A bent path can't re-enter a cell it already occupies and can't
   reverse 180° in one step.
6. **Solution-first construction (the hard gate).** Place arrows one at a time; each new arrow
   must still exit past everything already placed (`CanNewLineExit`). Peel order = reverse of
   placement ⇒ guaranteed solvable.

## B. "Don't just line them up" rules

7. **Lane cap.** ≤ 3 arrows may share the same exit row (horizontal exits) / column (vertical
   exits).
8. **Direction balance.** Spread exits across ↑↓←→; no single direction > ~40% of arrows
   (Level 10 = 12/9/10/9). Tutorial may bias one direction for teaching.
9. **Multi-row/column occupancy.** Used cells span ≥ ⌈√count⌉ distinct rows *and* columns.

## C. Locking / difficulty rules

10. **No isolated arrows** (above Easy): every arrow blocks someone or is blocked by someone.
11. **Prefer bend-to-lock placement:** favor routes where a turn puts the body directly in
    front of an existing arrow's head. One arrow blocking ≥2 others (a "hub") is high-value.
12. **Chain-depth target** (longest must-precede chain) is the real difficulty knob — see G.
13. **Limit opening moves:** ~2–4 arrows removable on the first tap (never 0).
14. **No deadlock:** reject if any arrow can never exit (`StuckArrows` non-empty).

## D. Density rule

15. **Board fill 55–85%.** Size the region so a full placement fills ~65–85% of cells (open) or
    ~55–75% of the mask (shape).

## E. Shape-mode extra rules (mask provided)

16. **Bodies stay inside the mask.**
17. **Heads exit through the silhouette;** keep the outline legible (no long straights that
    blur the shape). Favor arrows tracing the contour with a turn toward the exit.
18. **Symmetry-aware, not symmetry-locked:** stagger lengths/bends so mirrored halves aren't
    the same puzzle twice.
19. **Coverage ~55–75%** of the mask; reject if large regions are empty.

## F. Open-mode extra rules

20. **Compact, near-square bounding box** (avoid long thin strips).
21. **Interlace, don't stack:** bias toward comb/interleaved layouts where bent arrows nest
    into each other's gaps.

## G. Difficulty ramp (aligned with `DifficultyProfile.cs`)

| Band | Levels | Arrows | Avg bends | Chain depth | Shape |
|---|---|---|---|---|---|
| Tutorial | 1–25 | 3–6 | 0–1 | 1–2 | Plus/Arrow/Triangle |
| Easy | 26–45 | 5–9 | 1–2 | 2–3 | Diamond/Hexagon |
| Medium | 46–70 | 6–11 | 2–3 | 3–5 | Star/Cross |
| Hard | 71–85 | 8–13 | 3 | 5–7 | Heart/Ring |
| Expert/Nightmare | 86–100 | 10–16 | 3–4 | 7+ | Flower/Ring |

## H. Genre conventions (Parking Jam / Traffic Escape / Unpuzzle family)

22. Always solvable; introduce one new idea per early level.
23. Readable at a glance (distinct color per arrow, clear head, no ambiguous overlaps).
24. Allow multiple valid peel orders but keep ≥1 non-obvious dependency.
25. Milestone shape/picture levels (~every 10) as reward beats.
26. Undo-friendly, no soft-locks (guaranteed by the peel-order property).

## Accept/reject gates (regenerate on any failure)

Implemented in `LevelDesignerGenerator.PassesGates`: the generator builds N candidates and keeps the
best-scoring one that passes every gate (falls back to best-scoring if none pass). Thresholds are
tuned to what solution-first greedy generation can actually reach (validated in a Python sim + live
in-editor), so the gate filters the bad tail rather than rejecting everything:

- ✅ Solvable (`FindRemovalOrder != null`) — Rule 6
- ✅ No overlaps, legal bends, no length-1 arrows — Rules 2–5
- ✅ Isolated arrows ≤ tolerance (Easy: unbounded; n≤6: 1; else ⌈0.12·n⌉) — Rule 10
- ✅ No exit direction over ⌈0.4·n⌉ (also enforced during placement) — Rule 8
- ✅ Used cells span ≥ ⌈√n⌉ distinct rows AND columns — Rule 9
- ✅ Longest must-precede chain ≥ target (Medium 1 / Hard 2 / Expert 2, capped at ⌊n/3⌋) — Rule 12
- ✅ Opening moves within [1, max(4, ⌈0.6·n⌉)] — not trivially half the board — Rule 13
- ⬜ (Shape) bodies in mask ✅ enforced; coverage 55–75% & silhouette legibility (Rules 17–19) — NOT yet

---

## Implementation status

`Assets/_Game/Scripts/ProceduralLevels/Editor/LevelDesigner/LevelDesignerGenerator.cs` is the
solution-first generator (Go-Arrow ▸ Level Designer ▸ Auto-Fill, for counts ≤ 60). As of the
bent-arrow upgrade it emits multi-bend, interlocking, difficulty-scaled arrows per the rules
above; larger counts fall back to `LevelDesignerOps.DensePack`.
