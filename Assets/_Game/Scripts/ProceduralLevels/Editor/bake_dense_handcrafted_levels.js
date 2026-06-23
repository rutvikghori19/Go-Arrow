#!/usr/bin/env node
/** Generate dense handcrafted Level 11-100 prefabs (Level 10 style) offline. */
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PROJECT_ROOT = path.resolve(__dirname, "..", "..", "..", "..", "..");
const LEVELS_DIR = path.join(PROJECT_ROOT, "Assets", "_Game", "Resources", "Levels");
const TEMPLATE_PATH = path.join(LEVELS_DIR, "Level 10.prefab");
const LINE_GUID = "5134ca6e224a9b84b8bd86f152cf67fc";
const LINES_PARENT_ID = "9045057546396753023";

const LEVELS_SCENE = path.join(PROJECT_ROOT, "Assets", "_Game", "Scenes", "GameScene.unity");
const LEVEL_MANAGER_TARGET = "7163049563802694962";
const LEVEL_MANAGER_GUID = "da4a9ff5ab27d3149a75c45e2ad3cda9";
const LEVEL_PREFAB_FILEID = "4408890181075704862";

const ARROW_TARGETS = {
  11: 40, 12: 40, 13: 40, 14: 40, 15: 40, 16: 40, 17: 40, 18: 40, 19: 40, 20: 40,
};

const LEVEL10_VARIANT_NAMES = [
  "Level10", "MirrorX", "MirrorY", "Rotate180", "SwapXY", "SwapXY+MirrorX", "SwapXY+MirrorY", "SwapXY+Rotate180",
];

const BAND_11_20 = {
  11: { shape: "SmallDiamond", arrows: 30, branches: 2, loops: 1, traps: 1 },
  12: { shape: "Diamond", arrows: 32, branches: 2, loops: 1, traps: 2 },
  13: { shape: "RoundedDiamond", arrows: 34, branches: 3, loops: 1, traps: 2 },
  14: { shape: "Square", arrows: 36, branches: 3, loops: 2, traps: 2 },
  15: { shape: "RoundedSquare", arrows: 38, branches: 3, loops: 2, traps: 3 },
  16: { shape: "Circle", arrows: 40, branches: 4, loops: 2, traps: 3 },
  17: { shape: "Oval", arrows: 41, branches: 4, loops: 2, traps: 4 },
  18: { shape: "Hexagon", arrows: 43, branches: 4, loops: 3, traps: 4 },
  19: { shape: "LargeDiamond", arrows: 44, branches: 5, loops: 3, traps: 4 },
  20: { shape: "LargeCircle", arrows: 45, branches: 5, loops: 3, traps: 5 },
};

const BAND_21_30 = {
  21: { shape: "Level10Dense", arrows: 42 },
  22: { shape: "Level10Dense", arrows: 44 },
  23: { shape: "Level10Dense", arrows: 46 },
  24: { shape: "Level10Dense", arrows: 48 },
  25: { shape: "Level10Dense", arrows: 50 },
  26: { shape: "Level10Dense", arrows: 52 },
  27: { shape: "Level10Dense", arrows: 54 },
  28: { shape: "Level10Dense", arrows: 56 },
  29: { shape: "Level10Dense", arrows: 58 },
  30: { shape: "Level10Dense", arrows: 60 },
};

function isBandLevel(levelNumber) {
  return levelNumber >= 11 && levelNumber <= 20;
}

const BAND_31_40 = {
  31: { shape: "Circle", arrows: 62 },
  32: { shape: "Triangle", arrows: 64 },
  33: { shape: "Diamond", arrows: 66 },
  34: { shape: "Circle", arrows: 68 },
  35: { shape: "Triangle", arrows: 70 },
  36: { shape: "Diamond", arrows: 72 },
  37: { shape: "Circle", arrows: 74 },
  38: { shape: "Triangle", arrows: 76 },
  39: { shape: "Diamond", arrows: 78 },
  40: { shape: "Circle", arrows: 80 },
};

function isBand31Level(levelNumber) {
  return levelNumber >= 31 && levelNumber <= 40;
}

function isBand41Level(levelNumber) {
  return levelNumber >= 41 && levelNumber <= 100;
}

const SHAPE_POOL_41_100 = [
  "Heart", "Star", "Hexagon", "Circle", "Diamond", "Triangle", "Plus", "Square", "Ring", "Crescent",
];

function buildShapeSequence41_100() {
  const seq = {};
  let prev = "Circle";
  const usage = {};
  for (let level = 41; level <= 100; level++) {
    const candidates = SHAPE_POOL_41_100
      .filter(s => s !== prev)
      .sort((a, b) => (usage[a] || 0) - (usage[b] || 0));
    const shape = candidates[0];
    seq[level] = shape;
    usage[shape] = level;
    prev = shape;
  }
  return seq;
}

const SHAPE_SEQUENCE_41_100 = buildShapeSequence41_100();

const TIGHT_SHAPE_ARROW_SCALE = {
  Heart: 0.75, Crescent: 0.82, Ring: 0.85, Star: 0.9,
};

function shapeAdjustedArrows(shape, baseArrows) {
  const scale = TIGHT_SHAPE_ARROW_SCALE[shape] || 1;
  return Math.max(44, Math.round(baseArrows * scale));
}

function buildBand41_100() {
  const band = {};
  for (let level = 41; level <= 100; level++) {
    const shape = SHAPE_SEQUENCE_41_100[level];
    const base = Math.min(64 + (level - 41) * 2, 150);
    band[level] = {
      shape,
      arrows: shapeAdjustedArrows(shape, base),
    };
  }
  return band;
}

const BAND_41_100 = buildBand41_100();

function isBand21Level(levelNumber) {
  return levelNumber >= 21 && levelNumber <= 30;
}

function getBand41Spec(levelNumber) {
  return BAND_41_100[levelNumber] || null;
}

function getShapedBandSpec(levelNumber) {
  if (isBand31Level(levelNumber)) return getBand31Spec(levelNumber);
  if (isBand41Level(levelNumber)) return getBand41Spec(levelNumber);
  return null;
}

function getMinFillRatio(levelNumber) {
  if (isBand31Level(levelNumber)) {
    const t = (levelNumber - 31) / 9;
    return 0.62 + t * 0.04;
  }
  if (isBand41Level(levelNumber)) {
    const t = (levelNumber - 41) / 59;
    return 0.66 + t * 0.06;
  }
  return 0.62;
}

function countLinesInsideMask(lines, mask) {
  return lines.filter(l => isLineInside(mask, l)).length;
}

function validateShapedLevel(lines, mask, minFill, minInsideRatio = 0.95) {
  if (!lines || !lines.length) return false;
  if (!hasDisjointEdgeCells(lines)) return false;
  if (!findRemovalOrder(lines)) return false;
  if (!isTightCluster(lines)) return false;
  if (hasOverlappingEdges(lines)) return false;
  if (computeMaskFillRatio(lines, mask) < minFill) return false;
  if (countLinesInsideMask(lines, mask) < Math.floor(lines.length * minInsideRatio)) return false;
  return true;
}

function getBand21Spec(levelNumber) {
  return BAND_21_30[levelNumber] || null;
}

function getBand31Spec(levelNumber) {
  return BAND_31_40[levelNumber] || null;
}

function getBandSpec(levelNumber) {
  return BAND_11_20[levelNumber] || null;
}

function isInsideBandShape(shape, x, y) {
  switch (shape) {
    case "SmallDiamond": return Math.abs(x) + Math.abs(y) <= 0.62;
    case "Diamond": return Math.abs(x) + Math.abs(y) <= 0.82;
    case "RoundedDiamond": {
      const m = Math.abs(x) + Math.abs(y);
      if (m <= 0.58) return true;
      if (m > 0.86) return false;
      return Math.max(Math.abs(x), Math.abs(y)) <= 0.62;
    }
    case "RoundedSquare": {
      const ax = Math.abs(x), ay = Math.abs(y), h = 0.76, r = 0.22;
      if (ax <= h - r && ay <= h - r) return true;
      if (ax > h || ay > h) return false;
      const dx = ax - (h - r), dy = ay - (h - r);
      return dx * dx + dy * dy <= r * r;
    }
    case "Oval": return (x * x) / (0.88 * 0.88) + (y * y) / (0.68 * 0.68) <= 1;
    case "LargeDiamond": return Math.abs(x) + Math.abs(y) <= 0.96;
    case "LargeCircle": return x * x + y * y <= 0.92;
    default: return isInsideShape(shape, x, y);
  }
}

function createBandMask(levelNumber) {
  const spec = getBandSpec(levelNumber);
  const gridSize = getGridSize(levelNumber);
  const size = Math.max(7, gridSize);
  const mask = Array.from({ length: size }, () => Array(size).fill(false));
  const center = (size - 1) * 0.5;
  const scale = 2 / size;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const nx = (x - center) * scale;
      const ny = (y - center) * scale;
      mask[x][y] = isInsideBandShape(spec.shape, nx, ny);
    }
  }
  return mask;
}

const SHAPES = ["Square", "Circle", "Triangle", "Heart", "Diamond", "Hexagon", "Star", "Plus"];
const LARGE_SHAPES = ["Square", "Circle", "Hexagon", "Diamond"];
const MID_SHAPES = ["Square", "Circle", "Diamond", "Hexagon"];

function getShape(levelNumber) {
  if (isBandLevel(levelNumber)) return getBandSpec(levelNumber).shape;
  if (isBand21Level(levelNumber)) return getBand21Spec(levelNumber).shape;
  const shaped = getShapedBandSpec(levelNumber);
  if (shaped) return shaped.shape;
  return SHAPES[(levelNumber - 11) % SHAPES.length];
}

function getArrowCount(levelNumber) {
  if (levelNumber <= 20) return ARROW_TARGETS[levelNumber];
  if (isBand21Level(levelNumber)) return getBand21Spec(levelNumber).arrows;
  const shaped = getShapedBandSpec(levelNumber);
  if (shaped) return shaped.arrows;
  return 82 + (levelNumber - 41) * 2;
}

function getGridSize(levelNumber) {
  if (isBandLevel(levelNumber)) {
    const arrows = getArrowCount(levelNumber);
    if (arrows <= 32) return 13;
    if (arrows <= 40) return 15;
    return 17;
  }
  if (isBand21Level(levelNumber)) {
    const arrows = getArrowCount(levelNumber);
    const shape = getShape(levelNumber);
    if (shape === "Diamond" && arrows >= 48) return 17;
    if (arrows <= 45) return 15;
    return 17;
  }
  if (isBand31Level(levelNumber) || isBand41Level(levelNumber)) {
    const arrows = getArrowCount(levelNumber);
    if (arrows <= 75) return 23;
    if (arrows <= 110) return 25;
    if (arrows <= 150) return 27;
    return 29;
  }
  const arrows = getArrowCount(levelNumber);
  if (arrows <= 90) return 21;
  if (arrows <= 130) return 23;
  if (arrows <= 170) return 25;
  return 27;
}

function isInsideShape(shape, x, y) {
  switch (shape) {
    case "Circle": return x * x + y * y <= 0.82;
    case "Square": return Math.max(Math.abs(x), Math.abs(y)) <= 0.78;
    case "Diamond": return Math.abs(x) + Math.abs(y) <= 0.95;
    case "Triangle": return y <= 0.75 && y >= -0.15 - Math.abs(x) * 0.9;
    case "Heart": {
      const hx = x * 1.06;
      const hy = y * 1.06;
      const a = hx * hx + hy * hy - 0.28;
      return a * a * a - hx * hx * hy * hy * hy <= 0.035;
    }
    case "Hexagon": {
      const ax = Math.abs(x), ay = Math.abs(y);
      return ax <= 0.65 && ay <= 0.55 && ax * 0.5 + ay <= 0.72;
    }
    case "Star": {
      const angle = Math.atan2(y, x);
      const radius = Math.sqrt(x * x + y * y);
      return radius <= 0.42 + 0.22 * Math.cos(5 * angle);
    }
    case "Plus":
      return (Math.abs(x) <= 0.18 && Math.abs(y) <= 0.75) ||
        (Math.abs(y) <= 0.18 && Math.abs(x) <= 0.75);
    case "Ring": {
      const r2 = x * x + y * y;
      return r2 <= 0.82 && r2 >= 0.32;
    }
    case "Crescent": {
      const d1 = (x + 0.18) * (x + 0.18) + y * y;
      const d2 = (x - 0.32) * (x - 0.32) + y * y;
      return d1 <= 0.78 && d2 >= 0.38;
    }
    default: return x * x + y * y <= 0.8;
  }
}

function createMask(shape, gridSize) {
  const size = Math.max(7, gridSize);
  const mask = Array.from({ length: size }, () => Array(size).fill(false));
  const center = (size - 1) * 0.5;
  const scale = 2 / size;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const nx = (x - center) * scale;
      const ny = (y - center) * scale;
      mask[x][y] = isInsideShape(shape, nx, ny);
    }
  }
  return mask;
}

function maskCenter(gridSize) { return Math.floor((gridSize - 1) / 2); }

function isPointInside(mask, cx, cy) {
  const center = maskCenter(mask.length);
  const mx = cx + center;
  const my = cy + center;
  if (mx < 0 || my < 0 || mx >= mask.length || my >= mask[0].length) return false;
  return mask[mx][my];
}

function isLineInside(mask, line) {
  for (const p of line.points) {
    if (!isPointInside(mask, p.x, p.y)) return false;
  }
  return true;
}

function computeMaskFillRatio(lines, mask) {
  const center = maskCenter(mask.length);
  const occupied = new Set();
  for (const line of lines) {
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = line.points[i];
      const b = line.points[i + 1];
      for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
        occupied.add(packCellVec(x, y));
      }
    }
  }
  let total = 0, hit = 0;
  for (let y = 0; y < mask.length; y++) {
    for (let x = 0; x < mask.length; x++) {
      if (!mask[x][y]) continue;
      total++;
      const cx = x - center, cy = y - center;
      if (occupied.has(packCellVec(cx, cy))) hit++;
    }
  }
  return total === 0 ? 0 : hit / total;
}

function scaleToFit(lines, mask, fill = 0.88) {
  const gridSize = mask.length;
  const allowed = gridSize * fill * 0.5;
  let maxExtent = 0;
  for (const line of lines) {
    for (const p of line.points) {
      maxExtent = Math.max(maxExtent, Math.abs(p.x), Math.abs(p.y));
    }
  }
  if (maxExtent <= allowed || maxExtent < 0.001) return;
  const scale = allowed / maxExtent;
  if (Math.abs(scale - 1) < 0.08) return;
  for (const line of lines) {
    line.points = line.points.map(p => new GridPoint(Math.round(p.x * scale), Math.round(p.y * scale)));
  }
}

function snapshotLines(lines) {
  return lines.map(l => l.points.map(p => [p.x, p.y]));
}

function restoreLines(lines, snap) {
  for (let i = 0; i < lines.length; i++) {
    lines[i].points = snap[i].map(([x, y]) => new GridPoint(x, y));
  }
}

function scaleToFitPreserveDisjoint(lines, mask, fill = 0.88) {
  const gridSize = mask.length;
  const allowed = gridSize * fill * 0.5;
  let maxExtent = 0;
  for (const line of lines) {
    for (const p of line.points) {
      maxExtent = Math.max(maxExtent, Math.abs(p.x), Math.abs(p.y));
    }
  }
  if (maxExtent <= allowed || maxExtent < 0.001) return false;
  const targetScale = allowed / maxExtent;
  if (Math.abs(targetScale - 1) < 0.05) return false;
  const backup = snapshotLines(lines);
  const steps = Math.max(10, Math.ceil((1 - targetScale) / 0.025));
  for (let step = steps; step >= 1; step--) {
    const s = 1 - (1 - targetScale) * (step / steps);
    restoreLines(lines, backup);
    for (const line of lines) {
      line.points = line.points.map(p => new GridPoint(Math.round(p.x * s), Math.round(p.y * s)));
    }
    if (hasDisjointEdgeCells(lines)) return true;
  }
  restoreLines(lines, backup);
  return false;
}

function clipOutside(lines, mask) {
  return lines.filter(line => isLineInside(mask, line));
}

function hasOverlappingEdges(lines) {
  const occupied = new Set();
  for (const line of lines) {
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      const key = packEdge(a, b).toString();
      if (occupied.has(key)) return true;
      occupied.add(key);
    }
  }
  return false;
}

function removeOverlapping(lines) {
  const kept = [];
  const occupied = new Set();
  for (const line of lines) {
    let conflicts = false;
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      if (occupied.has(packEdge(a, b).toString())) { conflicts = true; break; }
    }
    if (conflicts) continue;
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      occupied.add(packEdge(a, b).toString());
    }
    kept.push(line);
  }
  return kept;
}

function collectMaskAnchors(mask) {
  const center = maskCenter(mask.length);
  const anchors = [];
  for (let y = 0; y < mask.length; y++) {
    for (let x = 0; x < mask.length; x++) {
      if (mask[x][y]) anchors.push([x - center, y - center]);
    }
  }
  return anchors;
}

function tryCreateLineInMask(rng, mask, segments, occupiedEdges, occupiedCells) {
  const anchors = collectMaskAnchors(mask);
  if (!anchors.length) return null;
  const center = maskCenter(mask.length);
  for (let attempt = 0; attempt < 64; attempt++) {
    const head = anchors[Math.floor(rng() * anchors.length)];
    const dist = Math.hypot(head[0], head[1]);
    if (occupiedCells && occupiedCells.has(packCellVec(head[0], head[1]))) continue;
    if (rng() > 0.35 && dist > 4) continue;
    const exitDir = CARDINALS[Math.floor(rng() * 4)];
    const points = [new GridPoint(head[0], head[1])];
    const visited = new Set([`${head[0]},${head[1]}`]);
    const current = [head[0] - exitDir[0], head[1] - exitDir[1]];
    if (!isPointInside(mask, current[0], current[1])) continue;
    if (occupiedCells && occupiedCells.has(packCellVec(current[0], current[1]))) continue;
    points.unshift(new GridPoint(current[0], current[1]));
    visited.add(`${current[0]},${current[1]}`);
    let failed = false;
    for (let s = 1; s < segments; s++) {
      const options = [];
      const tail = points[0];
      for (const [dx, dy] of CARDINALS) {
        const nxt = [tail.x + dx, tail.y + dy];
        if (!isPointInside(mask, nxt[0], nxt[1])) continue;
        if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
        if (occupiedCells && occupiedCells.has(packCellVec(nxt[0], nxt[1]))) continue;
        options.push(nxt);
      }
      if (!options.length) { failed = true; break; }
      const chosen = options[Math.floor(rng() * options.length)];
      points.unshift(new GridPoint(chosen[0], chosen[1]));
      visited.add(`${chosen[0]},${chosen[1]}`);
    }
    if (failed) continue;
    const line = new LevelLineData(points);
    if (occupiedCells) {
      if (conflictsOccupancy(line, occupiedEdges, occupiedCells)) continue;
    } else if (conflictsWithOccupied(line, occupiedEdges)) {
      continue;
    }
    return line;
  }
  return null;
}

function tryCreateLineInMaskEdgeOnly(rng, mask, segments, occupiedEdges, occupiedCells) {
  const anchors = collectMaskAnchors(mask);
  if (!anchors.length) return null;
  for (let attempt = 0; attempt < 64; attempt++) {
    const head = anchors[Math.floor(rng() * anchors.length)];
    const dist = Math.hypot(head[0], head[1]);
    if (occupiedCells.has(packCellVec(head[0], head[1]))) continue;
    if (rng() > 0.3 && dist > 5) continue;
    const exitDir = CARDINALS[Math.floor(rng() * 4)];
    const points = [new GridPoint(head[0], head[1])];
    const visited = new Set([`${head[0]},${head[1]}`]);
    const current = [head[0] - exitDir[0], head[1] - exitDir[1]];
    if (!isPointInside(mask, current[0], current[1])) continue;
    if (occupiedCells.has(packCellVec(current[0], current[1]))) continue;
    points.unshift(new GridPoint(current[0], current[1]));
    visited.add(`${current[0]},${current[1]}`);
    let failed = false;
    for (let s = 1; s < segments; s++) {
      const options = [];
      const tail = points[0];
      for (const [dx, dy] of CARDINALS) {
        const nxt = [tail.x + dx, tail.y + dy];
        if (!isPointInside(mask, nxt[0], nxt[1])) continue;
        if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
        if (occupiedCells.has(packCellVec(nxt[0], nxt[1]))) continue;
        options.push(nxt);
      }
      if (!options.length) { failed = true; break; }
      const chosen = options[Math.floor(rng() * options.length)];
      points.unshift(new GridPoint(chosen[0], chosen[1]));
      visited.add(`${chosen[0]},${chosen[1]}`);
    }
    if (failed) continue;
    const line = new LevelLineData(points);
    let edgeConflict = false;
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      if (occupiedEdges.has(packEdge(a, b).toString())) { edgeConflict = true; break; }
    }
    if (!edgeConflict) return line;
  }
  return null;
}

function tryGrowLineFromDir(mask, head, segments, exitDir, occupiedEdges, occupiedCells, rng = null) {
  const points = [new GridPoint(head.x, head.y)];
  const visited = new Set([`${head.x},${head.y}`]);
  let current = [head.x - exitDir[0], head.y - exitDir[1]];
  if (!isPointInside(mask, current[0], current[1])) return null;
  if (occupiedCells.has(packCellVec(current[0], current[1]))) return null;
  points.unshift(new GridPoint(current[0], current[1]));
  visited.add(`${current[0]},${current[1]}`);
  for (let s = 1; s < segments; s++) {
    const options = [];
    const tail = points[0];
    for (const [dx, dy] of CARDINALS) {
      const nxt = [tail.x + dx, tail.y + dy];
      if (!isPointInside(mask, nxt[0], nxt[1])) continue;
      if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
      if (occupiedCells.has(packCellVec(nxt[0], nxt[1]))) continue;
      options.push(nxt);
    }
    if (!options.length) return null;
    const pick = rng ? options[Math.floor(rng() * options.length)] : options[0];
    points.unshift(new GridPoint(pick[0], pick[1]));
    visited.add(`${pick[0]},${pick[1]}`);
  }
  const line = new LevelLineData(points);
  if (conflictsOccupancy(line, occupiedEdges, occupiedCells)) return null;
  return line;
}

function buildDenseChainReverse(mask, target, seed, opts = {}) {
  const tough = opts.tough !== false;
  const strict = opts.strict === true;
  const rings = buildRings(mask);
  const allSlots = [...rings.inner, ...rings.mid, ...rings.outer]
    .sort((a, b) => a.dist - b.dist || a.x - b.x || a.y - b.y);
  if (!allSlots.length) return null;

  const maxAttempts = opts.skipOnlyRemovable
    ? Math.max(35, Math.min(70, Math.ceil(target * 0.45)))
    : Math.max(120, Math.min(500, Math.ceil(target * 1.5)));

  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const rng = mulberry32(seed + attempt * 97);
    const lines = [];
    const occupiedEdges = new Set();
    const occupiedCells = new Set();
    const occupiedEdgeCells = new Map();
    let failed = false;

    for (let i = 0; i < target; i++) {
      const slot = allSlots[(i + attempt) % allSlots.length];
      const head = { x: slot.x, y: slot.y };
      let placed = false;
      const tries = target > 120 ? 48 : 36;
      for (let t = 0; t < tries && !placed; t++) {
        const exitDir = CARDINALS[(attempt + i * 3 + t) % 4];
        const segments = tough
          ? 1 + Math.floor(rng() * (target > 100 ? 4 : 3))
          : 1 + (i < target * 0.4 ? (rng() < 0.55 ? 1 : 2) : 1 + Math.floor(rng() * 3));
        const line = strict
          ? tryGrowLineStrictInMask(mask, head, segments, exitDir, occupiedEdges, occupiedEdgeCells, rng)
          : tryGrowLineFromDir(mask, head, segments, exitDir, occupiedEdges, occupiedCells, rng);
        if (!line) continue;
        const trial = lines.concat([line]);
        if (!opts.skipOnlyRemovable && !isOnlyRemovable(trial, trial.length - 1)) continue;
        if (opts.requireCanExit && !canNewLineExit(lines, line)) continue;
        if (strict) {
          registerStrictEdgeCells(line, occupiedEdgeCells);
          for (let j = 0; j < line.pointCount - 1; j++) {
            const a = [line.points[j].x, line.points[j].y];
            const b = [line.points[j + 1].x, line.points[j + 1].y];
            occupiedEdges.add(packEdge(a, b).toString());
          }
        } else {
          registerOccupancy(line, occupiedEdges, occupiedCells);
        }
        lines.push(line);
        placed = true;
      }
      if (!placed) { failed = true; break; }
    }

    if (failed || lines.length !== target) continue;
    if (!findRemovalOrder(lines)) continue;
    if (hasOverlappingEdges(lines)) continue;
    if (!isTightCluster(lines)) continue;
    if (strict && !hasDisjointEdgeCells(lines)) continue;
    if (!strict && hasOverlappingCells(lines)) continue;
    if (!hasDenseCore(lines, target)) continue;
    return lines;
  }

  return null;
}

function tryGrowLineStrictInMask(mask, head, segments, exitDir, occupiedEdges, occupiedEdgeCells, rng) {
  if (strictCellBlocked(head.x, head.y, occupiedEdgeCells)) return null;
  const points = [new GridPoint(head.x, head.y)];
  const visited = new Set([`${head.x},${head.y}`]);
  let current = [head.x - exitDir[0], head.y - exitDir[1]];
  if (!isPointInside(mask, current[0], current[1])) return null;
  if (strictCellBlocked(current[0], current[1], occupiedEdgeCells)) return null;
  points.unshift(new GridPoint(current[0], current[1]));
  visited.add(`${current[0]},${current[1]}`);
  for (let s = 1; s < segments; s++) {
    const options = [];
    const tail = points[0];
    for (const [dx, dy] of CARDINALS) {
      const nxt = [tail.x + dx, tail.y + dy];
      if (!isPointInside(mask, nxt[0], nxt[1])) continue;
      if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
      if (strictCellBlocked(nxt[0], nxt[1], occupiedEdgeCells)) continue;
      options.push(nxt);
    }
    if (!options.length) return null;
    const pick = options[Math.floor(rng() * options.length)];
    points.unshift(new GridPoint(pick[0], pick[1]));
    visited.add(`${pick[0]},${pick[1]}`);
  }
  const line = new LevelLineData(points);
  if (lineHasStrictConflict(line, occupiedEdges, occupiedEdgeCells)) return null;
  if (!isLineInside(mask, line)) return null;
  return line;
}

function buildSolvableCore(mask, rings, coreTarget, seed) {
  const allAnchors = rings.all;
  for (let attempt = 0; attempt < 40; attempt++) {
    const rng = mulberry32(seed + attempt * 173);
    const lines = [];
    const occupiedEdges = new Set();
    const occupiedCells = new Set();
    let failed = false;
    for (let i = 0; i < coreTarget; i++) {
      let success = false;
      for (let t = 0; t < 300 && !success; t++) {
        const anchor = pickAnchor(allAnchors, occupiedCells, rng);
        if (!anchor) continue;
        const ringIndex = anchor.dist <= rings.innerMaxDist ? 0 : anchor.dist <= rings.midMaxDist ? 1 : 2;
        const line = tryGrowLine(mask, anchor, pickSegmentCount(rng, ringIndex), occupiedEdges, occupiedCells, rng);
        if (!line) continue;
        const trial = lines.concat([line]);
        if (!isOnlyRemovable(trial, trial.length - 1)) continue;
        registerOccupancy(line, occupiedEdges, occupiedCells);
        lines.push(line);
        success = true;
      }
      if (!success) { failed = true; break; }
    }
    if (!failed && lines.length === coreTarget && findRemovalOrder(lines)) return lines;
  }
  return null;
}

function extendToTargetInMask(lines, mask, target, seed) {
  const working = [...lines];
  const occupiedEdges = new Set();
  const occupiedCells = new Set();
  for (const line of working) registerOccupancy(line, occupiedEdges, occupiedCells);
  const rng = mulberry32(seed * 23 + 97);
  while (working.length < target) {
    let placed = false;
    for (let t = 0; t < 1000 && !placed; t++) {
      const segments = 1 + Math.floor(rng() * 4);
      const line = tryCreateLineInMask(rng, mask, segments, occupiedEdges, occupiedCells);
      if (!line) continue;
      if (!canNewLineExit(working, line)) continue;
      registerOccupancy(line, occupiedEdges, occupiedCells);
      working.push(line);
      placed = true;
    }
    if (!placed) return null;
  }
  return working;
}

function packCellVec(x, y) {
  return ((BigInt(x + 512) << 16n) | BigInt(y + 512)).toString();
}

function cellsOnEdge(ax, ay, bx, by) {
  const cells = [];
  if (ax === bx) {
    const y0 = Math.min(ay, by);
    const y1 = Math.max(ay, by);
    for (let y = y0; y <= y1; y++) cells.push([ax, y]);
  } else if (ay === by) {
    const x0 = Math.min(ax, bx);
    const x1 = Math.max(ax, bx);
    for (let x = x0; x <= x1; x++) cells.push([x, ay]);
  }
  return cells;
}

function edgeConflictsCells(ax, ay, bx, by, occupiedCells) {
  for (const [x, y] of cellsOnEdge(ax, ay, bx, by)) {
    if (occupiedCells.has(packCellVec(x, y))) return true;
  }
  return false;
}

function lineHasSelfCellOverlap(line) {
  const seen = new Set();
  for (const p of line.points) {
    const k = packCellVec(p.x, p.y);
    if (seen.has(k)) return true;
    seen.add(k);
  }
  return false;
}

function hasOverlappingCells(lines) {
  const occupied = new Set();
  for (const line of lines) {
    const local = new Set();
    for (const p of line.points) {
      const key = packCellVec(p.x, p.y);
      if (local.has(key)) return true;
      local.add(key);
      if (occupied.has(key)) return true;
      occupied.add(key);
    }
  }
  return false;
}

function hasDisjointEdgeCells(lines) {
  const occ = new Map();
  for (let li = 0; li < lines.length; li++) {
    const line = lines[li];
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = line.points[i];
      const b = line.points[i + 1];
      for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
        const k = packCellVec(x, y);
        if (occ.has(k) && occ.get(k) !== li) return false;
        occ.set(k, li);
      }
    }
  }
  return true;
}

function registerStrictEdgeCells(line, occupiedEdgeCells) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = line.points[i];
    const b = line.points[i + 1];
    for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
      occupiedEdgeCells.set(packCellVec(x, y), true);
    }
  }
}

function strictCellBlocked(x, y, occupiedEdgeCells) {
  return occupiedEdgeCells.has(packCellVec(x, y));
}

function lineHasStrictConflict(line, occupiedEdges, occupiedEdgeCells) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = line.points[i];
    const b = line.points[i + 1];
    const ea = [a.x, a.y];
    const eb = [b.x, b.y];
    if (occupiedEdges.has(packEdge(ea, eb).toString())) return true;
    for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
      if (strictCellBlocked(x, y, occupiedEdgeCells)) return true;
    }
  }
  return false;
}

function rebuildStrictOccupancy(lines, occupiedEdges, occupiedEdgeCells) {
  occupiedEdges.clear();
  occupiedEdgeCells.clear();
  for (const line of lines) {
    registerStrictEdgeCells(line, occupiedEdgeCells);
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      occupiedEdges.add(packEdge(a, b).toString());
    }
  }
}

function tryCreateStrictExtensionLine(rng, radius, segments, occupiedEdges, occupiedEdgeCells, mask = null) {
  const exitDir = CARDINALS[Math.floor(rng() * 4)];
  let head;
  if (mask && rng() < 0.92) {
    const anchors = collectMaskAnchors(mask)
      .map(([x, y]) => ({ x, y }))
      .filter(a => !strictCellBlocked(a.x, a.y, occupiedEdgeCells));
    if (!anchors.length) return null;
    const pick = anchors[Math.floor(rng() * anchors.length)];
    head = [pick.x, pick.y];
  } else {
    head = [Math.floor(rng() * (radius * 2 + 1)) - radius, Math.floor(rng() * (radius * 2 + 1)) - radius];
    if (mask && !isPointInside(mask, head[0], head[1])) return null;
  }
  if (strictCellBlocked(head[0], head[1], occupiedEdgeCells)) return null;
  const points = [new GridPoint(head[0], head[1])];
  const visited = new Set([`${head[0]},${head[1]}`]);
  let current = [head[0] - exitDir[0], head[1] - exitDir[1]];
  if (Math.max(Math.abs(current[0]), Math.abs(current[1])) > radius + 1) return null;
  if (mask && !isPointInside(mask, current[0], current[1])) return null;
  if (strictCellBlocked(current[0], current[1], occupiedEdgeCells)) return null;
  points.unshift(new GridPoint(current[0], current[1]));
  visited.add(`${current[0]},${current[1]}`);

  for (let s = 1; s < segments; s++) {
    const options = [];
    const tail = points[0];
    for (const [dx, dy] of CARDINALS) {
      const nxt = [tail.x + dx, tail.y + dy];
      if (Math.max(Math.abs(nxt[0]), Math.abs(nxt[1])) > radius + 1) continue;
      if (mask && !isPointInside(mask, nxt[0], nxt[1])) continue;
      if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
      if (strictCellBlocked(nxt[0], nxt[1], occupiedEdgeCells)) continue;
      options.push(nxt);
    }
    if (!options.length) return null;
    const chosen = options[Math.floor(rng() * options.length)];
    points.unshift(new GridPoint(chosen[0], chosen[1]));
    visited.add(`${chosen[0]},${chosen[1]}`);
  }

  const line = new LevelLineData(points);
  if (lineHasStrictConflict(line, occupiedEdges, occupiedEdgeCells)) return null;
  if (mask && !isLineInside(mask, line)) return null;
  return line;
}

function registerExtensionOccupancy(line, occupiedEdgeCells, occupiedVertices) {
  for (const p of line.points) occupiedVertices.add(packCellVec(p.x, p.y));
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = line.points[i];
    const b = line.points[i + 1];
    for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
      occupiedEdgeCells.add(packCellVec(x, y));
    }
  }
}

function extensionCellConflict(x, y, occupiedEdgeCells, occupiedVertices) {
  const k = packCellVec(x, y);
  if (!occupiedEdgeCells.has(k)) return false;
  if (occupiedVertices.has(k)) return false;
  return true;
}

function lineHasExtensionConflict(line, occupiedEdges, occupiedEdgeCells, occupiedVertices) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = line.points[i];
    const b = line.points[i + 1];
    const ea = [a.x, a.y];
    const eb = [b.x, b.y];
    if (occupiedEdges.has(packEdge(ea, eb).toString())) return true;
    for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
      if (extensionCellConflict(x, y, occupiedEdgeCells, occupiedVertices)) return true;
    }
  }
  return false;
}

function hasAddedLineTJunctions(lines, baseCount) {
  const edgeCells = new Map();
  const vertexLines = new Map();

  function noteVertex(lineIndex, x, y) {
    const k = packCellVec(x, y);
    if (!vertexLines.has(k)) vertexLines.set(k, new Set());
    vertexLines.get(k).add(lineIndex);
  }

  for (let li = 0; li < lines.length; li++) {
    const line = lines[li];
    for (const p of line.points) noteVertex(li, p.x, p.y);
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = line.points[i];
      const b = line.points[i + 1];
      for (const [x, y] of cellsOnEdge(a.x, a.y, b.x, b.y)) {
        const k = packCellVec(x, y);
        if (!edgeCells.has(k)) {
          edgeCells.set(k, new Set([li]));
          continue;
        }
        const owners = edgeCells.get(k);
        for (const other of owners) {
          if (other === li) continue;
          const verts = vertexLines.get(k);
          const sharedVertex = verts && verts.has(li) && verts.has(other);
          if (!sharedVertex && (li >= baseCount || other >= baseCount)) return true;
        }
        owners.add(li);
      }
    }
  }
  return false;
}

function lineConflictsCells(line, occupiedCells) {
  for (const p of line.points) {
    if (occupiedCells.has(packCellVec(p.x, p.y))) return true;
  }
  return false;
}

function registerLineCells(line, occupiedCells) {
  for (const p of line.points) occupiedCells.add(packCellVec(p.x, p.y));
}

function removeCellOverlapping(lines) {
  const kept = [];
  const occupied = new Set();
  for (const line of lines) {
    if (lineHasSelfCellOverlap(line)) continue;
    let conflicts = false;
    for (const p of line.points) {
      if (occupied.has(packCellVec(p.x, p.y))) { conflicts = true; break; }
    }
    if (conflicts) continue;
    for (const p of line.points) occupied.add(packCellVec(p.x, p.y));
    kept.push(line);
  }
  return kept;
}

function resolveCellOverlaps(lines) {
  const centroid = computeCentroid(lines);
  const order = lines
    .map((line, i) => ({ i, score: lineDistanceFromCentroid(line, centroid) }))
    .sort((a, b) => a.score - b.score);
  const keep = new Array(lines.length).fill(false);
  const occupied = new Set();
  for (const { i } of order) {
    const line = lines[i];
    if (lineHasSelfCellOverlap(line)) continue;
    let conflicts = false;
    for (const p of line.points) {
      if (occupied.has(packCellVec(p.x, p.y))) { conflicts = true; break; }
    }
    if (conflicts) continue;
    for (const p of line.points) occupied.add(packCellVec(p.x, p.y));
    keep[i] = true;
  }
  return lines.filter((_, i) => keep[i]);
}

function fitToTarget(lines, mask, target, seed) {
  let working = clipOutside([...lines], mask);
  working = removeOverlapping(working);
  working = trimToTarget(working, target, seed);
  if (!working) return null;
  working = extendToTarget(working, target, seed);
  if (!working || working.length !== target) return null;
  if (!isTightCluster(working)) return null;
  if (hasOverlappingEdges(working)) return null;
  if (!findRemovalOrder(working)) return null;
  return working;
}

const CARDINALS = [[0, 1], [0, -1], [-1, 0], [1, 0]];

class GridPoint {
  constructor(x, y) { this.x = x; this.y = y; }
}

class LevelLineData {
  constructor(points) { this.points = points; }
  get pointCount() { return this.points.length; }
  getHead() { const p = this.points[this.points.length - 1]; return [p.x, p.y]; }
  getDirection() {
    if (this.points.length < 2) return [0, 1];
    const a = this.points[this.points.length - 2];
    const b = this.points[this.points.length - 1];
    const dx = b.x - a.x, dy = b.y - a.y;
    if (Math.abs(dx) >= Math.abs(dy)) return [dx >= 0 ? 1 : -1, 0];
    return [0, dy >= 0 ? 1 : -1];
  }
}

function packEdge(a, b) {
  if (a[0] > b[0] || (a[0] === b[0] && a[1] > b[1])) [a, b] = [b, a];
  return ((BigInt(a[0] + 512) << 40n) | (BigInt(a[1] + 512) << 20n) | (BigInt(b[0] + 512) << 10n) | BigInt(b[1] + 512));
}

function computeCentroid(lines) {
  let sumX = 0, sumY = 0, count = 0;
  for (const line of lines) {
    for (const p of line.points) {
      sumX += p.x; sumY += p.y; count++;
    }
  }
  return count === 0 ? [0, 0] : [sumX / count, sumY / count];
}

function lineDistanceFromCentroid(line, centroid) {
  let max = 0;
  for (const p of line.points) {
    const d = Math.hypot(p.x - centroid[0], p.y - centroid[1]);
    if (d > max) max = d;
  }
  return max;
}

function pickMostPeripheral(lines, removable, rng) {
  const centroid = computeCentroid(lines);
  let best = removable[0];
  let bestScore = -Infinity;
  for (const index of removable) {
    const score = lineDistanceFromCentroid(lines[index], centroid);
    if (score > bestScore) { bestScore = score; best = index; }
  }
  if (rng() < 0.15) return removable[Math.floor(rng() * removable.length)];
  return best;
}

function edgesTouch(a1, b1, a2, b2) {
  if (a1[0] === a2[0] && a1[1] === a2[1]) return true;
  if (a1[0] === b2[0] && a1[1] === b2[1]) return true;
  if (b1[0] === a2[0] && b1[1] === a2[1]) return true;
  if (b1[0] === b2[0] && b1[1] === b2[1]) return true;
  if (a1[0] === b1[0] && a2[0] === b2[0] && a1[0] === a2[0]) {
    const min1 = Math.min(a1[1], b1[1]), max1 = Math.max(a1[1], b1[1]);
    const min2 = Math.min(a2[1], b2[1]), max2 = Math.max(a2[1], b2[1]);
    return max1 >= min2 - 1 && max2 >= min1 - 1;
  }
  if (a1[1] === b1[1] && a2[1] === b2[1] && a1[1] === a2[1]) {
    const min1 = Math.min(a1[0], b1[0]), max1 = Math.max(a1[0], b1[0]);
    const min2 = Math.min(a2[0], b2[0]), max2 = Math.max(a2[0], b2[0]);
    return max1 >= min2 - 1 && max2 >= min1 - 1;
  }
  return false;
}

function areAllEdgesConnected(lines) {
  const edges = [];
  for (const line of lines) {
    for (let i = 0; i < line.pointCount - 1; i++) {
      edges.push([
        [line.points[i].x, line.points[i].y],
        [line.points[i + 1].x, line.points[i + 1].y],
      ]);
    }
  }
  if (!edges.length) return false;
  const visited = new Set([0]);
  const queue = [0];
  while (queue.length) {
    const current = queue.shift();
    const edge = edges[current];
    for (let i = 0; i < edges.length; i++) {
      if (visited.has(i)) continue;
      const other = edges[i];
      if (edgesTouch(edge[0], edge[1], other[0], other[1])) {
        visited.add(i);
        queue.push(i);
      }
    }
  }
  return visited.size === edges.length;
}

function arePointsWithinCoreBounds(lines, margin = 1) {
  const allPoints = [];
  for (const line of lines) {
    for (const p of line.points) allPoints.push([p.x, p.y]);
  }
  let centroid = [0, 0];
  for (const p of allPoints) { centroid[0] += p[0]; centroid[1] += p[1]; }
  centroid[0] /= allPoints.length; centroid[1] /= allPoints.length;

  let coreMinX = Infinity, coreMinY = Infinity, coreMaxX = -Infinity, coreMaxY = -Infinity;
  for (const p of allPoints) {
    const dist = Math.hypot(p[0] - centroid[0], p[1] - centroid[1]);
    if (dist > 8) continue;
    coreMinX = Math.min(coreMinX, p[0]); coreMinY = Math.min(coreMinY, p[1]);
    coreMaxX = Math.max(coreMaxX, p[0]); coreMaxY = Math.max(coreMaxY, p[1]);
  }
  if (coreMinX === Infinity) return true;
  coreMinX -= margin; coreMinY -= margin; coreMaxX += margin; coreMaxY += margin;
  for (const p of allPoints) {
    if (p[0] < coreMinX || p[0] > coreMaxX || p[1] < coreMinY || p[1] > coreMaxY) return false;
  }
  return true;
}

function minDistanceBetweenLines(a, b) {
  let min = Infinity;
  for (const pa of a.points) {
    for (const pb of b.points) {
      const d = Math.abs(pa.x - pb.x) + Math.abs(pa.y - pb.y);
      if (d < min) min = d;
    }
  }
  return min;
}

function hasIsolatedLines(lines, maxGap = 2) {
  for (let i = 0; i < lines.length; i++) {
    let nearOther = false;
    for (let j = 0; j < lines.length; j++) {
      if (i === j) continue;
      if (minDistanceBetweenLines(lines[i], lines[j]) <= maxGap) {
        nearOther = true;
        break;
      }
    }
    if (!nearOther) return true;
  }
  return false;
}

function isTightCluster(lines) {
  const centroid = computeCentroid(lines);
  let maxDist = 0;
  for (const line of lines) {
    for (const p of line.points) {
      const d = Math.hypot(p.x - centroid[0], p.y - centroid[1]);
      if (d > maxDist) maxDist = d;
    }
  }
  if (maxDist > 18) return false;
  return !hasIsolatedLines(lines, 2);
}

function centerLines(lines) {
  const xs = lines.flatMap(l => l.points.map(p => p.x));
  const ys = lines.flatMap(l => l.points.map(p => p.y));
  const offX = Math.floor((Math.min(...xs) + Math.max(...xs)) / 2);
  const offY = Math.floor((Math.min(...ys) + Math.max(...ys)) / 2);
  for (const line of lines) {
    line.points = line.points.map(p => new GridPoint(p.x - offX, p.y - offY));
  }
}

function distancePointToSegment(p, a, b) {
  const ab = [b[0] - a[0], b[1] - a[1]];
  const denom = ab[0] * ab[0] + ab[1] * ab[1];
  const t = denom < 0.0001 ? 0 : Math.max(0, Math.min(1, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / denom));
  const closest = [a[0] + ab[0] * t, a[1] + ab[1] * t];
  return Math.hypot(p[0] - closest[0], p[1] - closest[1]);
}

function hitsBody(body, point) {
  for (let i = 0; i < body.pointCount - 1; i++) {
    const a = [body.points[i].x, body.points[i].y];
    const b = [body.points[i + 1].x, body.points[i + 1].y];
    if (distancePointToSegment(point, a, b) <= 0.42) return true;
  }
  return false;
}

function canNewLineExit(placed, candidate) {
  if (candidate.pointCount < 2) return false;
  if (!placed.length) return true;
  const head = candidate.getHead();
  const direction = candidate.getDirection();
  const len = Math.hypot(direction[0], direction[1]);
  if (len === 0) return false;
  const dir = [direction[0] / len, direction[1] / len];
  for (let dist = 0.35; dist <= 64; dist += 0.35) {
    const sample = [head[0] + dir[0] * dist, head[1] + dir[1] * dist];
    for (const line of placed) {
      if (hitsBody(line, sample)) return false;
    }
  }
  return true;
}

function canExit(lines, lineIndex, active) {
  const line = lines[lineIndex];
  if (line.pointCount < 2) return false;
  const head = line.getHead();
  const direction = line.getDirection();
  const len = Math.hypot(direction[0], direction[1]);
  if (len === 0) return false;
  const dir = [direction[0] / len, direction[1] / len];
  for (let dist = 0.35; dist <= 64; dist += 0.35) {
    const sample = [head[0] + dir[0] * dist, head[1] + dir[1] * dist];
    for (const other of active) {
      if (other === lineIndex) continue;
      if (hitsBody(lines[other], sample)) return false;
    }
  }
  return true;
}

function findRemovalOrder(lines) {
  const remaining = lines.map((_, i) => i);
  const order = [];
  while (remaining.length) {
    let removable = -1;
    for (let i = remaining.length - 1; i >= 0; i--) {
      if (canExit(lines, remaining[i], remaining)) {
        removable = remaining[i];
        break;
      }
    }
    if (removable < 0) return null;
    order.push(removable);
    remaining.splice(remaining.indexOf(removable), 1);
  }
  return order;
}

function conflictsWithOccupied(line, occupied) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = [line.points[i].x, line.points[i].y];
    const b = [line.points[i + 1].x, line.points[i + 1].y];
    if (occupied.has(packEdge(a, b).toString())) return true;
  }
  return false;
}

function registerEdges(line, occupied) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = [line.points[i].x, line.points[i].y];
    const b = [line.points[i + 1].x, line.points[i + 1].y];
    occupied.add(packEdge(a, b).toString());
  }
}

function unregisterOccupancy(line, occupiedEdges, occupiedCells) {
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = [line.points[i].x, line.points[i].y];
    const b = [line.points[i + 1].x, line.points[i + 1].y];
    occupiedEdges.delete(packEdge(a, b).toString());
    for (const [x, y] of cellsOnEdge(a[0], a[1], b[0], b[1])) {
      occupiedCells.delete(packCellVec(x, y));
    }
  }
}

function tryCreateLine(rng, radius, segments, occupiedEdges, occupiedCells) {
  const exitDir = CARDINALS[Math.floor(rng() * 4)];
  const head = [Math.floor(rng() * (radius * 2 + 1)) - radius, Math.floor(rng() * (radius * 2 + 1)) - radius];
  if (occupiedCells.has(packCellVec(head[0], head[1]))) return null;
  const points = [new GridPoint(head[0], head[1])];
  const visited = new Set([`${head[0]},${head[1]}`]);
  let current = [head[0] - exitDir[0], head[1] - exitDir[1]];
  if (Math.max(Math.abs(current[0]), Math.abs(current[1])) > radius) return null;
  if (occupiedCells.has(packCellVec(current[0], current[1]))) return null;
  points.unshift(new GridPoint(current[0], current[1]));
  visited.add(`${current[0]},${current[1]}`);

  for (let s = 1; s < segments; s++) {
    const options = [];
    const tail = points[0];
    for (const [dx, dy] of CARDINALS) {
      const nxt = [tail.x + dx, tail.y + dy];
      if (Math.max(Math.abs(nxt[0]), Math.abs(nxt[1])) > radius) continue;
      if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
      if (occupiedCells.has(packCellVec(nxt[0], nxt[1]))) continue;
      options.push(nxt);
    }
    if (!options.length) return null;
    const chosen = options[Math.floor(rng() * options.length)];
    points.unshift(new GridPoint(chosen[0], chosen[1]));
    visited.add(`${chosen[0]},${chosen[1]}`);
  }

  const line = new LevelLineData(points);
  if (conflictsOccupancy(line, occupiedEdges, occupiedCells)) return null;
  return line;
}

function mulberry32(seed) {
  return function () {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

function getRemovableIndices(lines) {
  const removable = [];
  const all = lines.map((_, i) => i);
  for (const index of all) {
    if (canExit(lines, index, all)) removable.push(index);
  }
  return removable;
}

function extractLinesFromLevel10() {
  const text = fs.readFileSync(TEMPLATE_PATH, "utf8");
  const lines = [];
  const chunks = text.split("value: Line (");
  for (const chunk of chunks.slice(1)) {
    const sizeM = chunk.match(/m_Positions\.Array\.size\s*\n\s*value: (\d+)/);
    if (!sizeM) continue;
    const n = parseInt(sizeM[1], 10);
    const pts = [];
    for (let i = 0; i < n; i++) {
      const xm = chunk.match(new RegExp(`m_Positions\\.Array\\.data\\[${i}\\]\\.x\\s*\\n\\s*value: ([-\\d.]+)`));
      const ym = chunk.match(new RegExp(`m_Positions\\.Array\\.data\\[${i}\\]\\.y\\s*\\n\\s*value: ([-\\d.]+)`));
      if (xm && ym) pts.push(new GridPoint(Math.round(parseFloat(xm[1])), Math.round(parseFloat(ym[1]))));
    }
    if (pts.length >= 2) {
      let ox = 0, oy = 0;
      const oxm = chunk.match(/m_LocalPosition\.x\s*\n\s*value: ([-\d.]+)/);
      const oym = chunk.match(/m_LocalPosition\.y\s*\n\s*value: ([-\d.]+)/);
      if (oxm) ox = parseFloat(oxm[1]);
      if (oym) oy = parseFloat(oym[1]);
      if (ox || oy) {
        for (let i = 0; i < pts.length; i++) {
          pts[i] = new GridPoint(Math.round(pts[i].x + ox), Math.round(pts[i].y + oy));
        }
      }
      lines.push(new LevelLineData(pts));
    }
  }
  return lines;
}

function transformLines(source, variant) {
  const mirrorX = (variant & 1) === 1;
  const mirrorY = (variant & 2) === 2;
  const swapAxes = (variant & 4) === 4;
  return source.map(src => {
    const pts = src.points.map(p => {
      let x = p.x, y = p.y;
      if (swapAxes) [x, y] = [y, x];
      if (mirrorX) x = -x;
      if (mirrorY) y = -y;
      return new GridPoint(x, y);
    });
    return new LevelLineData(pts);
  });
}

function trimToTarget(lines, target, seed) {
  const working = [...lines];
  const rng = mulberry32(seed * 17 + 31);
  while (working.length > target) {
    const removable = getRemovableIndices(working);
    if (!removable.length) return null;
    const pick = pickMostPeripheral(working, removable, rng);
    working.splice(pick, 1);
  }
  return working;
}

function computeRadius(lines) {
  let maxAbs = 0;
  for (const line of lines) {
    for (const p of line.points) {
      maxAbs = Math.max(maxAbs, Math.abs(p.x), Math.abs(p.y));
    }
  }
  return Math.max(6, maxAbs);
}

function cloneLines(lines) {
  return lines.map(l => new LevelLineData(l.points.map(p => new GridPoint(p.x, p.y))));
}

function extendToTargetChunked(lines, target, seed, strictDisjoint = true, mask = null) {
  let working = cloneLines(lines);
  if (working.length > target) return null;
  if (working.length === target) return working;

  if (working.length < 100 && target > 100) {
    for (let attempt = 0; attempt < 32; attempt++) {
      const to100 = extendToTarget(cloneLines(working), 100, seed + attempt * 41, strictDisjoint, mask);
      if (to100 && to100.length === 100) { working = to100; break; }
    }
    if (working.length < 100 && target > 100) return null;
  }

  const chunkSize = target > 160 ? 40 : target > 130 ? 30 : 25;
  let round = 0;
  while (working.length < target) {
    const nextTarget = Math.min(target, working.length + chunkSize);
    let placed = null;
    const tries = target > 150 ? 24 : 32;
    for (let attempt = 0; attempt < tries && !placed; attempt++) {
      const trial = extendToTarget(
        cloneLines(working), nextTarget, seed + round * 991 + attempt * 37, strictDisjoint, mask);
      if (trial && trial.length === nextTarget) placed = trial;
    }
    if (!placed) return null;
    working = placed;
    round++;
    if (round > 10) return null;
  }
  return working.length === target ? working : null;
}

function extendToTarget(lines, target, seed, strictDisjoint = false, mask = null) {
  const working = [...lines];
  const occupiedEdges = new Set();
  const occupiedEdgeCells = strictDisjoint ? new Map() : new Set();
  const occupiedVertices = strictDisjoint ? null : new Set();
  if (strictDisjoint) rebuildStrictOccupancy(working, occupiedEdges, occupiedEdgeCells);
  else rebuildExtensionOccupancy(working, occupiedEdges, occupiedEdgeCells, occupiedVertices);
  const radius = computeRadius(working);
  const rng = mulberry32(seed * 23 + 97);

  while (working.length < target) {
    let placed = false;
    for (let t = 0; t < (strictDisjoint ? (mask ? 2400 : 1600) : 800) && !placed; t++) {
      const segments = 1 + Math.floor(rng() * (strictDisjoint ? 4 : 5));
      const line = strictDisjoint
        ? tryCreateStrictExtensionLine(rng, radius + (mask ? 4 : 2), segments, occupiedEdges, occupiedEdgeCells, mask)
        : tryCreateExtensionLine(rng, radius, segments, occupiedEdges, occupiedEdgeCells, occupiedVertices);
      if (!line) continue;
      if (!canNewLineExit(working, line)) continue;
      if (strictDisjoint) {
        registerStrictEdgeCells(line, occupiedEdgeCells);
        for (let i = 0; i < line.pointCount - 1; i++) {
          const a = [line.points[i].x, line.points[i].y];
          const b = [line.points[i + 1].x, line.points[i + 1].y];
          occupiedEdges.add(packEdge(a, b).toString());
        }
      } else {
        registerExtensionOccupancy(line, occupiedEdgeCells, occupiedVertices);
        for (let i = 0; i < line.pointCount - 1; i++) {
          const a = [line.points[i].x, line.points[i].y];
          const b = [line.points[i + 1].x, line.points[i + 1].y];
          occupiedEdges.add(packEdge(a, b).toString());
        }
      }
      working.push(line);
      if (!isTightCluster(working)) {
        working.pop();
        if (strictDisjoint) rebuildStrictOccupancy(working, occupiedEdges, occupiedEdgeCells);
        else rebuildExtensionOccupancy(working, occupiedEdges, occupiedEdgeCells, occupiedVertices);
        continue;
      }
      placed = true;
    }
    if (!placed) return null;
  }
  if (strictDisjoint) {
    if (!hasDisjointEdgeCells(working)) return null;
  } else if (hasAddedLineTJunctions(working, lines.length)) {
    return null;
  }
  return working;
}

function buildRings(mask) {
  const center = maskCenter(mask.length);
  const inner = [], mid = [], outer = [];
  let maxDist = 0;
  const all = [];
  for (let y = 0; y < mask.length; y++) {
    for (let x = 0; x < mask.length; x++) {
      if (!mask[x][y]) continue;
      const px = x - center, py = y - center;
      const dist = Math.hypot(px, py);
      if (dist > maxDist) maxDist = dist;
      all.push({ x: px, y: py, dist });
    }
  }
  for (const a of all) {
    const t = maxDist < 0.001 ? 0 : a.dist / maxDist;
    if (t <= 0.38) inner.push(a);
    else if (t <= 0.72) mid.push(a);
    else outer.push(a);
  }
  return {
    inner, mid, outer,
    innerMaxDist: maxDist * 0.38,
    midMaxDist: maxDist * 0.72,
    all,
  };
}

function pickAnchor(anchors, occupiedCells, rng) {
  const candidates = [], weights = [];
  const CARD = [[0,1],[0,-1],[-1,0],[1,0]];
  for (const a of anchors) {
    if (occupiedCells.has(packCellVec(a.x, a.y))) continue;
    let adjacent = 0;
    for (const [dx, dy] of CARD) {
      if (occupiedCells.has(packCellVec(a.x + dx, a.y + dy))) adjacent++;
    }
    candidates.push(a);
    const centerWeight = 4 / (0.35 + a.dist * a.dist);
    const packWeight = occupiedCells.size === 0 ? centerWeight : (1 + adjacent * 1.8) * centerWeight;
    weights.push(packWeight);
  }
  if (!candidates.length) return null;
  const total = weights.reduce((s, w) => s + w, 0);
  let roll = rng() * total;
  for (let i = 0; i < candidates.length; i++) {
    roll -= weights[i];
    if (roll <= 0) return candidates[i];
  }
  return candidates[candidates.length - 1];
}

function pickSegmentCount(rng, ringIndex) {
  const roll = rng();
  if (ringIndex === 0) {
    if (roll < 0.45) return 1;
    if (roll < 0.85) return 1 + Math.floor(rng() * 2);
    return 2 + Math.floor(rng() * 2);
  }
  if (roll < 0.25) return 1;
  if (roll < 0.60) return 1 + Math.floor(rng() * 2);
  if (roll < 0.85) return 3 + Math.floor(rng() * 2);
  return 5 + Math.floor(rng() * 2);
}

function conflictsOccupancy(line, occupiedEdges, occupiedCells) {
  for (const p of line.points) {
    if (occupiedCells.has(packCellVec(p.x, p.y))) return true;
  }
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = [line.points[i].x, line.points[i].y];
    const b = [line.points[i + 1].x, line.points[i + 1].y];
    if (occupiedEdges.has(packEdge(a, b).toString())) return true;
  }
  return false;
}

function registerOccupancy(line, occupiedEdges, occupiedCells) {
  for (const p of line.points) occupiedCells.add(packCellVec(p.x, p.y));
  for (let i = 0; i < line.pointCount - 1; i++) {
    const a = [line.points[i].x, line.points[i].y];
    const b = [line.points[i + 1].x, line.points[i + 1].y];
    occupiedEdges.add(packEdge(a, b).toString());
  }
}

function rebuildOccupancy(lines, occupiedEdges, occupiedCells) {
  occupiedEdges.clear();
  occupiedCells.clear();
  for (const line of lines) registerOccupancy(line, occupiedEdges, occupiedCells);
}

function rebuildExtensionOccupancy(lines, occupiedEdges, occupiedEdgeCells, occupiedVertices) {
  occupiedEdges.clear();
  occupiedEdgeCells.clear();
  occupiedVertices.clear();
  for (const line of lines) {
    registerExtensionOccupancy(line, occupiedEdgeCells, occupiedVertices);
    for (let i = 0; i < line.pointCount - 1; i++) {
      const a = [line.points[i].x, line.points[i].y];
      const b = [line.points[i + 1].x, line.points[i + 1].y];
      occupiedEdges.add(packEdge(a, b).toString());
    }
  }
}

function tryCreateExtensionLine(rng, radius, segments, occupiedEdges, occupiedEdgeCells, occupiedVertices) {
  const exitDir = CARDINALS[Math.floor(rng() * 4)];
  const head = [Math.floor(rng() * (radius * 2 + 1)) - radius, Math.floor(rng() * (radius * 2 + 1)) - radius];
  if (extensionCellConflict(head[0], head[1], occupiedEdgeCells, occupiedVertices)) return null;
  const points = [new GridPoint(head[0], head[1])];
  const visited = new Set([`${head[0]},${head[1]}`]);
  let current = [head[0] - exitDir[0], head[1] - exitDir[1]];
  if (Math.max(Math.abs(current[0]), Math.abs(current[1])) > radius) return null;
  if (extensionCellConflict(current[0], current[1], occupiedEdgeCells, occupiedVertices)) return null;
  points.unshift(new GridPoint(current[0], current[1]));
  visited.add(`${current[0]},${current[1]}`);

  for (let s = 1; s < segments; s++) {
    const options = [];
    const tail = points[0];
    for (const [dx, dy] of CARDINALS) {
      const nxt = [tail.x + dx, tail.y + dy];
      if (Math.max(Math.abs(nxt[0]), Math.abs(nxt[1])) > radius) continue;
      if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
      if (extensionCellConflict(nxt[0], nxt[1], occupiedEdgeCells, occupiedVertices)) continue;
      options.push(nxt);
    }
    if (!options.length) return null;
    const chosen = options[Math.floor(rng() * options.length)];
    points.unshift(new GridPoint(chosen[0], chosen[1]));
    visited.add(`${chosen[0]},${chosen[1]}`);
  }

  const line = new LevelLineData(points);
  if (lineHasExtensionConflict(line, occupiedEdges, occupiedEdgeCells, occupiedVertices)) return null;
  return line;
}

function tryGrowLine(mask, head, segments, occupiedEdges, occupiedCells, rng) {
  const center = maskCenter(mask.length);
  for (let attempt = 0; attempt < 32; attempt++) {
    const exitDir = CARDINALS[Math.floor(rng() * 4)];
    const points = [new GridPoint(head.x, head.y)];
    const visited = new Set([`${head.x},${head.y}`]);
    let current = [head.x - exitDir[0], head.y - exitDir[1]];
    if (!isPointInside(mask, current[0], current[1])) continue;
    points.unshift(new GridPoint(current[0], current[1]));
    visited.add(`${current[0]},${current[1]}`);
    let failed = false;
    for (let s = 1; s < segments; s++) {
      const options = [];
      const tail = points[0];
      for (const [dx, dy] of CARDINALS) {
        const nxt = [tail.x + dx, tail.y + dy];
        if (!isPointInside(mask, nxt[0], nxt[1])) continue;
        if (visited.has(`${nxt[0]},${nxt[1]}`)) continue;
        if (occupiedCells.has(packCellVec(nxt[0], nxt[1]))) continue;
        options.push(nxt);
      }
      if (!options.length) { failed = true; break; }
      const chosen = options[Math.floor(rng() * options.length)];
      points.unshift(new GridPoint(chosen[0], chosen[1]));
      visited.add(`${chosen[0]},${chosen[1]}`);
    }
    if (failed) continue;
    const line = new LevelLineData(points);
    if (!conflictsOccupancy(line, occupiedEdges, occupiedCells)) return line;
  }
  return null;
}

function hasDenseCore(lines, target) {
  let corePoints = 0;
  const coreRadius = target > 140 ? 5 : target > 100 ? 4 : 3;
  for (const line of lines) {
    for (const p of line.points) {
      if (Math.abs(p.x) <= coreRadius && Math.abs(p.y) <= coreRadius) corePoints++;
    }
  }
  return corePoints >= Math.max(8, Math.round(target * 0.18));
}

function placeRingQuota(lines, occupiedEdges, occupiedCells, mask, anchors, quota, rng, ringIndex) {
  if (quota <= 0) return true;
  if (!anchors.length) return false;
  for (let i = 0; i < quota; i++) {
    let success = false;
    for (let t = 0; t < 800 && !success; t++) {
      const anchor = pickAnchor(anchors, occupiedCells, rng, ringIndex);
      if (!anchor) continue;
      const segments = pickSegmentCount(rng, ringIndex);
      const line = tryGrowLine(mask, anchor, segments, occupiedEdges, occupiedCells, rng);
      if (!line) continue;
      registerOccupancy(line, occupiedEdges, occupiedCells);
      lines.push(line);
      success = true;
    }
    if (!success) return false;
  }
  return true;
}

function getRemovableIndices(lines) {
  const all = lines.map((_, i) => i);
  const removable = [];
  for (const index of all) {
    if (canExit(lines, index, all)) removable.push(index);
  }
  return removable;
}

function isOnlyRemovable(lines, newIndex) {
  const removable = getRemovableIndices(lines);
  return removable.length === 1 && removable[0] === newIndex;
}

function buildInMask(shape, gridSize, target, seed) {
  const mask = createMask(shape, gridSize);
  const rings = buildRings(mask);
  const allAnchors = rings.all;
  const maxAttempts = target > 50 ? 320 : 200;
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const rng = mulberry32(seed + attempt * 173);
    const lines = [];
    const occupiedEdges = new Set();
    const occupiedCells = new Set();
    let failed = false;
    for (let i = 0; i < target; i++) {
      let success = false;
      for (let t = 0; t < 2000 && !success; t++) {
        const anchor = pickAnchor(allAnchors, occupiedCells, rng);
        if (!anchor) continue;
        const ringIndex = anchor.dist <= rings.innerMaxDist ? 0 : anchor.dist <= rings.midMaxDist ? 1 : 2;
        const segments = pickSegmentCount(rng, ringIndex);
        const line = tryGrowLine(mask, anchor, segments, occupiedEdges, occupiedCells, rng);
        if (!line) continue;
        const trial = lines.concat([line]);
        if (!isOnlyRemovable(trial, trial.length - 1)) continue;
        registerOccupancy(line, occupiedEdges, occupiedCells);
        lines.push(line);
        success = true;
      }
      if (!success) { failed = true; break; }
    }
    if (failed || lines.length !== target) continue;
    if (!findRemovalOrder(lines)) continue;
    if (!isTightCluster(lines)) continue;
    if (hasOverlappingEdges(lines)) continue;
    if (hasOverlappingCells(lines)) continue;
    if (!hasDenseCore(lines, target)) continue;
    if (!lines.every(line => isLineInside(mask, line))) continue;
    lines._shape = shape;
    return lines;
  }
  return null;
}

const LEVEL10_TEMPLATE = extractLinesFromLevel10();

function countSolutionPaths(lines, cap = 4) {
  const remaining = lines.map((_, i) => i);
  function dfs(rem) {
    if (!rem.length) return 1;
    let total = 0;
    for (const idx of rem) {
      if (!canExit(lines, idx, rem)) continue;
      const next = rem.filter(i => i !== idx);
      total += dfs(next);
      if (total >= cap) return total;
    }
    return total;
  }
  return dfs(remaining);
}

function countTraps(lines) {
  const all = lines.map((_, i) => i);
  const start = getRemovableIndices(lines);
  let traps = 0;
  for (const move of start) {
    const rem = all.filter(i => i !== move);
    if (!findRemovalOrderSubset(lines, rem)) traps++;
  }
  return traps;
}

function findRemovalOrderSubset(lines, active) {
  const remaining = [...active];
  const order = [];
  while (remaining.length) {
    let removable = -1;
    for (let i = remaining.length - 1; i >= 0; i--) {
      if (canExit(lines, remaining[i], remaining)) { removable = remaining[i]; break; }
    }
    if (removable < 0) return null;
    order.push(removable);
    remaining.splice(remaining.indexOf(removable), 1);
  }
  return order;
}

function meetsTopology(lines, spec, tolerance = 1) {
  if (countSolutionPaths(lines, 4) !== 1) return false;
  const traps = countTraps(lines);
  const decisions = countDecisionPoints(lines);
  const branches = Math.max(Math.floor(decisions / 2), 0);
  const loops = Math.max(0, countLoopsEstimate(lines));
  return Math.abs(branches - spec.branches) <= tolerance
    && Math.abs(loops - spec.loops) <= tolerance
    && Math.abs(traps - spec.traps) <= tolerance;
}

function countDecisionPoints(lines) {
  const remaining = lines.map((_, i) => i);
  let decisions = 0;
  while (remaining.length) {
    const moves = remaining.filter(i => canExit(lines, i, remaining));
    if (!moves.length) return decisions;
    if (moves.length > 1) decisions++;
    remaining.splice(remaining.indexOf(moves[0]), 1);
  }
  return decisions;
}

function countLoopsEstimate(lines) {
  let segments = 0;
  const nodes = new Set();
  for (const line of lines) {
    for (const p of line.points) nodes.add(`${p.x},${p.y}`);
    segments += Math.max(0, line.pointCount - 1);
  }
  return Math.max(0, segments - nodes.size + 1);
}

function fitBandLevel(candidate, mask, spec, seed, strictCells = true) {
  let working = clipOutside([...candidate], mask);
  working = removeOverlapping(working);
  if (strictCells) working = resolveCellOverlaps(working);
  else working = working.filter(l => !lineHasSelfCellOverlap(l));
  working = trimToTarget(working, spec.arrows, seed);
  if (!working) return null;
  working = extendToTargetInMask(working, mask, spec.arrows, seed);
  if (!working || working.length !== spec.arrows) {
    working = trimToTarget(clipOutside([...candidate], mask), spec.arrows, seed);
    if (!working) return null;
    working = extendToTarget(working, spec.arrows, seed);
  }
  if (!working || working.length !== spec.arrows) return null;
  if (!isTightCluster(working)) return null;
  if (hasOverlappingEdges(working)) return null;
  if (strictCells && hasOverlappingCells(working)) return null;
  if (!hasDenseCore(working, spec.arrows)) return null;
  if (!findRemovalOrder(working)) return null;
  if (!working.every(l => isLineInside(mask, l))) return null;
  return working;
}

function generateBandLevel(levelNumber, seedOffset = 0) {
  const target = 40;
  const baseVariant = (levelNumber - 11) & 7;
  for (let tryV = 0; tryV < 8; tryV++) {
    const variant = (baseVariant + tryV) & 7;
    const working = transformLines(LEVEL10_TEMPLATE, variant);
    if (working.length !== target) continue;
    if (!findRemovalOrder(working)) continue;
    working._shape = LEVEL10_VARIANT_NAMES[variant];
    return working;
  }
  const fallback = transformLines(LEVEL10_TEMPLATE, baseVariant);
  fallback._shape = LEVEL10_VARIANT_NAMES[baseVariant];
  return fallback;
}

function generateBand21Level(levelNumber, seedOffset = 0) {
  const target = getArrowCount(levelNumber);
  const baseVariant = (levelNumber - 11) & 7;
  const baseCount = LEVEL10_TEMPLATE.length;
  for (let attempt = 0; attempt < 128; attempt++) {
    const variant = (baseVariant + attempt) & 7;
    const seed = levelNumber * 131 + seedOffset * 17 + attempt * 53;
    let working = transformLines(LEVEL10_TEMPLATE, variant);
    centerLines(working);
    if (working.length > target) continue;
    if (!hasDisjointEdgeCells(working)) continue;
    if (target > baseCount) {
      working = extendToTarget(working, target, seed, true);
      if (!working || working.length !== target) continue;
    }
    if (!isTightCluster(working)) continue;
    if (hasOverlappingEdges(working)) continue;
    if (!hasDisjointEdgeCells(working)) continue;
    if (!findRemovalOrder(working)) continue;
    working._shape = LEVEL10_VARIANT_NAMES[variant] + (target > baseCount ? "+Dense" : "");
    return working;
  }
  return null;
}

function generateExtendedVariant(levelNumber, seedOffset = 0) {
  const target = getArrowCount(levelNumber);
  const baseVariant = (levelNumber - 11) & 7;
  const maxAttempts = target <= 52 ? 48 : 96;
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const variant = (baseVariant + attempt) & 7;
    const seed = levelNumber * 131 + seedOffset * 17 + attempt * 53;
    let working = transformLines(LEVEL10_TEMPLATE, variant);
    centerLines(working);
    if (working.length > target) continue;
    working = extendToTarget(working, target, seed);
    if (!working || working.length !== target) continue;
    if (!isTightCluster(working)) continue;
    if (hasOverlappingEdges(working)) continue;
    if (hasAddedLineTJunctions(working, LEVEL10_TEMPLATE.length)) continue;
    if (!findRemovalOrder(working)) continue;
    working._shape = LEVEL10_VARIANT_NAMES[variant] + "+Dense";
    return working;
  }
  return null;
}

function tryL10ExtendAndScale(levelNumber, seedOffset, spec, mask, minFill, scaleFill) {
  const target = spec.arrows;
  const shape = spec.shape;
  const baseVariant = (levelNumber - 11) & 7;
  const fillThreshold = target > 120 ? minFill * 0.85 : minFill * 0.92;
  const maxAttempts = target >= 172 ? 48 : target > 150 ? 96 : target > 120 ? 160 : 256;

  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const variant = (baseVariant + attempt) & 7;
    const seed = levelNumber * 131 + seedOffset * 17 + attempt * 53;
    let working = transformLines(LEVEL10_TEMPLATE, variant);
    centerLines(working);
    if (!hasDisjointEdgeCells(working)) continue;
    working = extendToTargetChunked(working, target, seed, true, null);
    if (!working || working.length !== target) continue;
    const snap = snapshotLines(working);
    if (!scaleToFitPreserveDisjoint(working, mask, scaleFill)) {
      restoreLines(working, snap);
      continue;
    }
    if (!isTightCluster(working)) { restoreLines(working, snap); continue; }
    if (hasOverlappingEdges(working)) { restoreLines(working, snap); continue; }
    if (!hasDisjointEdgeCells(working)) { restoreLines(working, snap); continue; }
    if (!findRemovalOrder(working)) { restoreLines(working, snap); continue; }
    if (computeMaskFillRatio(working, mask) < fillThreshold) { restoreLines(working, snap); continue; }
    working._shape = shape;
    return working;
  }
  return null;
}

function buildForwardInMask(mask, target, seed, opts = {}) {
  const tough = opts.tough !== false;
  const radius = Math.max(6, Math.floor(mask.length / 2) - 1);
  const maxAttempts = Math.min(30, Math.max(16, Math.ceil(target * 0.22)));
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const rng = mulberry32(seed + attempt * 83);
    const lines = [];
    const occupiedEdges = new Set();
    const occupiedEdgeCells = new Map();
    let failed = false;
    for (let i = 0; i < target; i++) {
      let placed = false;
      for (let t = 0; t < 220 && !placed; t++) {
        const segments = tough
          ? 3 + Math.floor(rng() * (target > 100 ? 4 : 3))
          : 2 + Math.floor(rng() * 3);
        const line = tryCreateStrictExtensionLine(rng, radius, segments, occupiedEdges, occupiedEdgeCells, mask);
        if (!line) continue;
        if (!canNewLineExit(lines, line)) continue;
        registerStrictEdgeCells(line, occupiedEdgeCells);
        for (let j = 0; j < line.pointCount - 1; j++) {
          const a = [line.points[j].x, line.points[j].y];
          const b = [line.points[j + 1].x, line.points[j + 1].y];
          occupiedEdges.add(packEdge(a, b).toString());
        }
        lines.push(line);
        placed = true;
      }
      if (!placed) { failed = true; break; }
    }
    if (failed || lines.length !== target) continue;
    if (!findRemovalOrder(lines)) continue;
    if (!hasDisjointEdgeCells(lines)) continue;
    if (!isTightCluster(lines)) continue;
    if (hasOverlappingEdges(lines)) continue;
    if (!hasDenseCore(lines, target)) continue;
    return lines;
  }
  return null;
}

function buildForwardVertexInMask(mask, target, seed, opts = {}) {
  const tough = opts.tough !== false;
  const maxAttempts = Math.min(35, Math.max(18, Math.ceil(target * 0.24)));
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const rng = mulberry32(seed + attempt * 91);
    const lines = [];
    const occupiedEdges = new Set();
    const occupiedCells = new Set();
    let failed = false;
    for (let i = 0; i < target; i++) {
      let placed = false;
      for (let t = 0; t < 240 && !placed; t++) {
        const segments = tough ? 3 + Math.floor(rng() * (target > 100 ? 4 : 3)) : 2 + Math.floor(rng() * 3);
        const line = tryCreateLineInMask(rng, mask, segments, occupiedEdges, occupiedCells);
        if (!line) continue;
        if (!canNewLineExit(lines, line)) continue;
        registerOccupancy(line, occupiedEdges, occupiedCells);
        lines.push(line);
        placed = true;
      }
      if (!placed) { failed = true; break; }
    }
    if (failed || lines.length !== target) continue;
    if (!findRemovalOrder(lines)) continue;
    if (!hasDisjointEdgeCells(lines)) continue;
    if (!isTightCluster(lines)) continue;
    if (hasOverlappingEdges(lines)) continue;
    if (!hasDenseCore(lines, target)) continue;
    if (countLinesInsideMask(lines, mask) < Math.floor(target * 0.98)) continue;
    return lines;
  }
  return null;
}

function buildShapedMaskLevel(mask, target, seed) {
  return buildForwardVertexInMask(mask, target, seed, { tough: true })
    || buildForwardInMask(mask, target, seed + 2000, { tough: true })
    || buildDenseChainReverse(mask, target, seed + 5000, {
      tough: true, strict: false, skipOnlyRemovable: true, requireCanExit: true,
    });
}

function generateShapedDenseLevel(levelNumber, seedOffset = 0) {
  const spec = getShapedBandSpec(levelNumber);
  if (!spec) return null;
  const target = spec.arrows;
  const shape = spec.shape;
  const gridSize = getGridSize(levelNumber);
  const mask = createMask(shape, gridSize);
  const minFill = getMinFillRatio(levelNumber);
  const minInside = 0.96;
  const attempts = target > 130 ? 40 : target > 90 ? 32 : 28;

  for (let attempt = 0; attempt < attempts; attempt++) {
    const seed = levelNumber * 131 + seedOffset * 17 + attempt * 97;
    const working = buildShapedMaskLevel(mask, target, seed);
    if (!working) continue;
    if (!validateShapedLevel(working, mask, minFill, minInside)) continue;
    working._shape = shape;
    return working;
  }

  const relaxedFill = Math.max(0.55, minFill - 0.05);
  for (let attempt = 0; attempt < attempts; attempt++) {
    const seed = levelNumber * 131 + seedOffset * 17 + attempt * 137 + 9000;
    const working = buildShapedMaskLevel(mask, target, seed);
    if (!working) continue;
    if (!validateShapedLevel(working, mask, relaxedFill, 0.92)) continue;
    working._shape = shape;
    return working;
  }

  return null;
}

function generateBand31Level(levelNumber, seedOffset = 0) {
  return generateShapedDenseLevel(levelNumber, seedOffset);
}

function generateBand41Level(levelNumber, seedOffset = 0) {
  return generateShapedDenseLevel(levelNumber, seedOffset);
}

function generateLevel(levelNumber, seedOffset = 0) {
  if (isBandLevel(levelNumber)) return generateBandLevel(levelNumber, seedOffset);
  if (isBand21Level(levelNumber)) return generateBand21Level(levelNumber, seedOffset);
  if (isBand31Level(levelNumber)) return generateBand31Level(levelNumber, seedOffset);
  if (isBand41Level(levelNumber)) return generateBand41Level(levelNumber, seedOffset);
  const target = getArrowCount(levelNumber);
  const shape = getShape(levelNumber);
  const gridSize = getGridSize(levelNumber);
  const seed = levelNumber + seedOffset * 17;
  const mask = createMask(shape, gridSize);
  const fill = shape === "Diamond" ? 0.75 : 0.88;

  for (let v = 0; v < 24; v++) {
    let candidate = transformLines(LEVEL10_TEMPLATE, v);
    centerLines(candidate);
    candidate = fitToTarget(candidate, mask, target, seed + v * 31);
    if (candidate) {
      candidate._shape = shape;
      return candidate;
    }
  }

  return null;
}

function headRotationWz(dx, dy) {
  const angle = Math.atan2(dy, dx) - Math.PI / 2;
  return [Math.cos(angle / 2), Math.sin(angle / 2)];
}

function modEntry(target, prop, value) {
  const valueStr = typeof value === "number" && !Number.isInteger(value) ? String(value) : String(value);
  return `    - target: {fileID: ${target}, guid: ${LINE_GUID}, type: 3}\n      propertyPath: ${prop}\n      value: ${valueStr}\n      objectReference: {fileID: 0}\n`;
}

function buildLineInstance(lineIndex, line, levelNumber) {
  const instanceId = 1000000000 + levelNumber * 1000000 + lineIndex * 10000 + 1;
  const transformId = instanceId + 1;
  const name = `Line (${lineIndex + 1})`;
  const [headX, headY] = line.getHead();
  const [dx, dy] = line.getDirection();
  const [rotW, rotZ] = headRotationWz(dx, dy);

  const mods = [];
  mods.push(modEntry("579988495362921800", "destroyDelay", 1.5));
  mods.push(modEntry("1088355798733759430", "m_Enabled", 0));
  mods.push(modEntry("3898713965489042308", "thickness", 0.3));
  mods.push(modEntry("4852218959071952598", "m_Name", name));
  mods.push(modEntry("5703179247697745139", "m_Positions.Array.size", line.pointCount));
  for (let i = 0; i < line.pointCount; i++) {
    mods.push(modEntry("5703179247697745139", `m_Positions.Array.data[${i}].x`, line.points[i].x));
    mods.push(modEntry("5703179247697745139", `m_Positions.Array.data[${i}].y`, line.points[i].y));
    mods.push(modEntry("5703179247697745139", `m_Positions.Array.data[${i}].z`, 0));
  }
  mods.push(modEntry("5703179247697745139", "m_Parameters.widthMultiplier", 0.3));
  mods.push(modEntry("6705024964587633368", "m_LocalPosition.x", 0));
  mods.push(modEntry("6705024964587633368", "m_LocalPosition.y", 0));
  mods.push(modEntry("6705024964587633368", "m_LocalPosition.z", 0));
  mods.push(modEntry("6705024964587633368", "m_LocalRotation.w", 1));
  mods.push(modEntry("6705024964587633368", "m_LocalRotation.x", -0));
  mods.push(modEntry("6705024964587633368", "m_LocalRotation.y", -0));
  mods.push(modEntry("6705024964587633368", "m_LocalRotation.z", -0));
  mods.push(modEntry("6705024964587633368", "m_LocalEulerAnglesHint.x", 0));
  mods.push(modEntry("6705024964587633368", "m_LocalEulerAnglesHint.y", 0));
  mods.push(modEntry("6705024964587633368", "m_LocalEulerAnglesHint.z", 0));
  mods.push(modEntry("8852158018286561598", "m_LocalScale.x", 0.9));
  mods.push(modEntry("8852158018286561598", "m_LocalScale.y", 0.9));
  mods.push(modEntry("8852158018286561598", "m_LocalScale.z", 0.9));
  mods.push(modEntry("9200941245333506865", "m_LocalPosition.x", headX));
  mods.push(modEntry("9200941245333506865", "m_LocalPosition.y", headY));
  mods.push(modEntry("9200941245333506865", "m_LocalRotation.w", rotW));
  mods.push(modEntry("9200941245333506865", "m_LocalRotation.z", rotZ));

  const instance =
    `--- !u!1001 &${instanceId}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {fileID: ${LINES_PARENT_ID}}\n    m_Modifications:\n` +
    mods.join("") +
    `    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {fileID: 100100000, guid: ${LINE_GUID}, type: 3}\n`;

  const stripped =
    `--- !u!4 &${transformId} stripped\nTransform:\n  m_CorrespondingSourceObject: {fileID: 6705024964587633368, guid: ${LINE_GUID}, type: 3}\n  m_PrefabInstance: {fileID: ${instanceId}}\n  m_PrefabAsset: {fileID: 0}\n`;

  return { instance, stripped, transformId: String(transformId) };
}

function buildPrefab(levelNumber, lines) {
  let header = fs.readFileSync(TEMPLATE_PATH, "utf8").split("--- !u!1001")[0];
  header = header.replace("m_Name: Level 10", `m_Name: Level ${levelNumber}`);
  const band = levelNumber - 11;
  header = header.replace(
    "_timeThresholdsSec:\n  - 30\n  - 45\n  - 60",
    `_timeThresholdsSec:\n  - ${45 + band * 2}\n  - ${60 + band * 2}\n  - ${90 + band * 2}`
  );
  header = header.replace("_winCoins: 10", `_winCoins: ${10 + band}`);

  const parts = [];
  const transformIds = [];
  for (let i = 0; i < lines.length; i++) {
    const { instance, stripped, transformId } = buildLineInstance(i, lines[i], levelNumber);
    transformIds.push(transformId);
    parts.push(instance, stripped);
  }

  const childrenYaml = transformIds.map(tid => `  - {fileID: ${tid}}`).join("\n");
  header = header.replace(/  m_Children:\n(?:  - \{fileID: \d+\}\n)+/, `  m_Children:\n${childrenYaml}\n`);

  return header + parts.join("");
}

function writeMeta(prefabPath, existingGuid) {
  const guid = existingGuid || crypto.randomBytes(16).toString("hex");
  const content =
    "fileFormatVersion: 2\n" +
    `guid: ${guid}\n` +
    "PrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";
  fs.writeFileSync(prefabPath + ".meta", content, "utf8");
  return guid;
}

function patchGameScene(guids) {
  let scene = fs.readFileSync(LEVELS_SCENE, "utf8");
  const levelGuids = [];
  let maxLevel = 0;
  for (let i = 1; i <= 100; i++) {
    const metaPath = path.join(LEVELS_DIR, `Level ${i}.prefab.meta`);
    if (!fs.existsSync(metaPath)) continue;
    if (guids[i]) {
      levelGuids.push(guids[i]);
    } else {
      const m = fs.readFileSync(metaPath, "utf8").match(/guid: ([0-9a-f]{32})/);
      if (!m) throw new Error(`Missing guid in meta for Level ${i}`);
      levelGuids.push(m[1]);
    }
    maxLevel = i;
  }
  if (!maxLevel) throw new Error("No level prefabs found to patch into GameScene");

  const sizeIdx = scene.indexOf("propertyPath: _levels.Array.size");
  if (sizeIdx < 0) throw new Error("Could not find _levels.Array.size in GameScene");
  const blockStart = scene.lastIndexOf(`- target: {fileID: ${LEVEL_MANAGER_TARGET}`, sizeIdx);
  const blockEnd = scene.indexOf("m_RemovedComponents:", sizeIdx);
  const before = scene.slice(0, blockStart);
  const after = scene.slice(blockEnd);

  const entries = [
    `    - target: {fileID: ${LEVEL_MANAGER_TARGET}, guid: ${LEVEL_MANAGER_GUID}, type: 3}\n` +
    `      propertyPath: _levels.Array.size\n      value: ${maxLevel}\n      objectReference: {fileID: 0}`,
  ];
  for (let i = 0; i < maxLevel; i++) {
    entries.push(
      `    - target: {fileID: ${LEVEL_MANAGER_TARGET}, guid: ${LEVEL_MANAGER_GUID}, type: 3}\n` +
      `      propertyPath: '_levels.Array.data[${i}]'\n      value: \n` +
      `      objectReference: {fileID: ${LEVEL_PREFAB_FILEID}, guid: ${levelGuids[i]}, type: 3}`
    );
  }
  fs.writeFileSync(LEVELS_SCENE, before + entries.join("\n") + "\n    " + after, "utf8");
  console.log(`Patched GameScene._levels to ${maxLevel} entries.`);
}

function main() {
  const onlyArg = process.argv.find(a => a.startsWith("--only="));
  const minLevel = onlyArg ? parseInt(onlyArg.split("=")[1].split("-")[0], 10) : 11;
  const maxLevel = onlyArg ? parseInt(onlyArg.split("=")[1].split("-")[1] || onlyArg.split("=")[1], 10) : 100;
  const guids = {};
  for (let level = minLevel; level <= maxLevel; level++) {
    let lines = null;
    const maxOffsets = (isBand41Level(level) && getArrowCount(level) >= 172) ? 64 : 128;
    for (let offset = 0; offset < maxOffsets; offset++) {
      lines = generateLevel(level, offset);
      if (lines && lines.length === getArrowCount(level)) break;
      lines = null;
    }
    if (!lines) throw new Error(`Failed to generate solvable layout for level ${level}`);

    const prefabPath = path.join(LEVELS_DIR, `Level ${level}.prefab`);
    fs.writeFileSync(prefabPath, buildPrefab(level, lines), "utf8");

    let existingGuid = null;
    const metaPath = prefabPath + ".meta";
    if (fs.existsSync(metaPath)) {
      const m = fs.readFileSync(metaPath, "utf8").match(/guid: ([0-9a-f]{32})/);
      if (m) existingGuid = m[1];
    }
    guids[level] = writeMeta(prefabPath, existingGuid);
    const order = findRemovalOrder(lines);
    const clusterOk = isTightCluster(lines);
    console.log(`Level ${level} (${lines._shape || getShape(level)}): ${lines.length} arrows, removal=${order ? order.length : 0}, cluster=${clusterOk}, guid=${guids[level]}`);
    if (!clusterOk) throw new Error(`Level ${level} failed cluster validation after bake`);
  }
  patchGameScene(guids);
  console.log("Done.");
}

if (process.argv.includes("--probe")) {
  const level = parseInt(process.argv[process.argv.indexOf("--probe") + 1] || "35", 10);
  const shape = getShape(level);
  const target = getArrowCount(level);
  const gridSize = getGridSize(level);
  const mask = createMask(shape, gridSize);
  let candidate = transformLines(LEVEL10_TEMPLATE, 0);
  centerLines(candidate);
  scaleToFit(candidate, mask, shape === "Diamond" ? 0.92 : 0.88);
  const inside = clipOutside([...candidate], mask);
  console.log({ shape, target, gridSize, template: candidate.length, inside: inside.length });
} else if (process.argv.includes("--chain")) {
  const level = parseInt(process.argv[process.argv.indexOf("--chain") + 1] || "35", 10);
  const shape = getShape(level);
  const target = getArrowCount(level);
  const gridSize = getGridSize(level);
  const mask = createMask(shape, gridSize);
  for (let a = 0; a < 10; a++) {
    const lines = buildDenseChainReverse(mask, target, level + a * 97);
    console.log("attempt", a, lines && lines.length);
  }
} else if (process.argv.includes("--debug")) {
  const level = parseInt(process.argv[process.argv.indexOf("--debug") + 1] || "11", 10);
  const shape = getShape(level);
  const target = getArrowCount(level);
  const gridSize = getGridSize(level);
  const mask = createMask(shape, gridSize);
  const fill = shape === "Diamond" ? 0.72 : 0.88;
  console.log("template cells", !hasOverlappingCells(LEVEL10_TEMPLATE));
  for (let v = 0; v < 3; v++) {
    let c = transformLines(LEVEL10_TEMPLATE, v);
    centerLines(c);
    scaleToFit(c, mask, fill);
    let w = clipOutside([...c], mask);
    w = removeOverlapping(w);
    const t = trimToTarget(w, target, level + v * 31);
    console.log("v", v, "clip cells", !hasOverlappingCells(w), "trim cells", t && !hasOverlappingCells(t));
    const ext = t && extendToTargetInMask(t, mask, target, level + v * 31);
  }
} else if (process.argv.includes("--trace31")) {
  const level = parseInt(process.argv[process.argv.indexOf("--trace31") + 1] || "31", 10);
  const spec = getBand31Spec(level);
  const target = spec.arrows;
  const shape = spec.shape;
  const gridSize = getGridSize(level);
  const mask = createMask(shape, gridSize);
  const fill = shape === "Diamond" ? 0.82 : 0.92;
  const baseVariant = (level - 11) & 7;
  const stats = { disjoint: 0, disjointBase: 0, extend: 0, cluster: 0, overlap: 0, sol: 0, inside: 0, ok: 0 };
  for (let attempt = 0; attempt < 384; attempt++) {
    const variant = (baseVariant + attempt) & 7;
    const seed = level * 131 + attempt * 53;
    let working = transformLines(LEVEL10_TEMPLATE, variant);
    centerLines(working);
    if (!hasDisjointEdgeCells(working)) stats.disjointBase++;
    // scaleToFit breaks disjoint cells when rounding; skip in trace unless --withscale
    if (process.argv.includes("--withscale")) scaleToFit(working, mask, fill);
    if (working.length > target) continue;
    if (!hasDisjointEdgeCells(working)) { stats.disjoint++; continue; }
    if (working.length < target) {
      working = extendToTarget(working, target, seed, true, mask);
      if (!working || working.length !== target) { stats.extend++; continue; }
    }
    if (!isTightCluster(working)) { stats.cluster++; continue; }
    if (hasOverlappingEdges(working)) { stats.overlap++; continue; }
    if (!hasDisjointEdgeCells(working)) { stats.disjoint++; continue; }
    if (!findRemovalOrder(working)) { stats.sol++; continue; }
    const insideCount = working.filter(l => isLineInside(mask, l)).length;
    if (insideCount < Math.max(22, Math.floor(target * 0.5))) { stats.inside++; continue; }
    stats.ok++;
    console.log({ level, target, shape, attempt, insideCount, stats });
    break;
  }
  if (!stats.ok) console.log({ level, target, shape, stats, disjointBaseFails: stats.disjointBase });
} else if (process.argv.includes("--maxextend")) {
  let copy = transformLines(LEVEL10_TEMPLATE, 0);
  centerLines(copy);
  let ext = extendToTarget(copy, 100, 75 * 131, true, null);
  for (const t of [135, 140, 145, 150, 155, 160]) {
    let c2 = ext.map(l => new LevelLineData(l.points.map(p => new GridPoint(p.x, p.y))));
    const more = extendToTarget(c2, t, 75 * 131 + t, true, null);
    console.log("target", t, "got", more && more.length, more && !!findRemovalOrder(more));
  }
} else if (process.argv.includes("--trace")) {
  const level = parseInt(process.argv[process.argv.indexOf("--trace") + 1] || "41", 10);
  const spec = getShapedBandSpec(level);
  const mask = createMask(spec.shape, getGridSize(level));
  const rings = buildRings(mask);
  console.log("mask", spec.shape, "cells", rings.all.length);
  const rng = mulberry32(41);
  const occE = new Set(), occC = new Set();
  let placed = 0;
  for (let i = 0; i < 30; i++) {
    const line = tryCreateLineInMask(rng, mask, 2, occE, occC);
    if (line) { registerOccupancy(line, occE, occC); placed++; }
  }
  console.log("tryCreateLineInMask placed", placed);
  for (const target of [20, 30]) {
    const c = buildDenseChainReverse(mask, target, level * 131, { tough: false, strict: false });
    console.log("chain", target, c ? c.length : "fail");
  }
} else if (process.argv.includes("--bench")) {
  const level = parseInt(process.argv[process.argv.indexOf("--bench") + 1] || "41", 10);
  const targetOverride = parseInt(process.argv[process.argv.indexOf("--bench") + 2] || "0", 10) || getArrowCount(level);
  const shape = getShape(level);
  const mask = createMask(shape, getGridSize(level));
  console.log("bench", { level, shape, target: targetOverride, grid: mask.length });
  const t0 = Date.now();
  const result = buildShapedMaskLevel(mask, targetOverride, level * 131);
  console.log("ms", Date.now() - t0, "lines", result ? result.length : null);
  if (result) {
    console.log({
      fill: Math.round(computeMaskFillRatio(result, mask) * 1000) / 1000,
      inside: countLinesInsideMask(result, mask) + "/" + result.length,
      solvable: !!findRemovalOrder(result),
      disjoint: hasDisjointEdgeCells(result),
    });
  }
} else if (process.argv.includes("--quick")) {
  const level = parseInt(process.argv[process.argv.indexOf("--quick") + 1] || "11", 10);
  const gen = generateLevel(level, 0);
  const target = getArrowCount(level);
  const shape = getShape(level);
  const mask = (isBand31Level(level) || isBand41Level(level))
    ? createMask(shape, getGridSize(level)) : null;
  console.log({
    level, shape, ok: !!(gen && gen.length === target),
    sol: gen && !!findRemovalOrder(gen), cells: gen && !hasOverlappingCells(gen),
    core: gen && hasDenseCore(gen, target),
    fill: gen && mask ? Math.round(computeMaskFillRatio(gen, mask) * 1000) / 1000 : null,
    inside: gen && mask ? countLinesInsideMask(gen, mask) + "/" + gen.length : null,
  });
  if (gen && gen.length === target) {
    const order = findRemovalOrder(gen);
    console.log("removal order length", order && order.length);
  }
} else {
  main();
}
