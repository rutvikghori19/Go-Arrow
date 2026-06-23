#!/usr/bin/env python3
"""Generate dense handcrafted Level 11-20 prefabs (Level 10 style) offline."""
from __future__ import annotations

import math
import os
import random
import re
import uuid
from typing import List, Optional, Tuple

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
LEVELS_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Game", "Resources", "Levels")
TEMPLATE_PATH = os.path.join(LEVELS_DIR, "Level 10.prefab")
LINE_GUID = "5134ca6e224a9b84b8bd86f152cf67fc"
LINES_PARENT_ID = "9045057546396753023"

ARROW_TARGETS = {11: 30, 12: 32, 13: 34, 14: 36, 15: 38, 16: 40, 17: 41, 18: 43, 19: 44, 20: 45}

CARDINALS = [(0, 1), (0, -1), (-1, 0), (1, 0)]


class GridPoint:
    def __init__(self, x: int, y: int):
        self.x = x
        self.y = y


class LevelLineData:
    def __init__(self, points: List[GridPoint]):
        self.points = points

    @property
    def point_count(self) -> int:
        return len(self.points)

    def get_head(self) -> Tuple[int, int]:
        p = self.points[-1]
        return p.x, p.y

    def get_direction(self) -> Tuple[int, int]:
        if len(self.points) < 2:
            return 0, 1
        a = self.points[-2]
        b = self.points[-1]
        dx, dy = b.x - a.x, b.y - a.y
        if abs(dx) >= abs(dy):
            return (1 if dx >= 0 else -1, 0)
        return (0, 1 if dy >= 0 else -1)


def pack_edge(a: Tuple[int, int], b: Tuple[int, int]) -> int:
    if a[0] > b[0] or (a[0] == b[0] and a[1] > b[1]):
        a, b = b, a
    return ((a[0] + 512) << 40) | ((a[1] + 512) << 20) | ((b[0] + 512) << 10) | (b[1] + 512)


def center_lines(lines: List[LevelLineData]) -> None:
    xs = [p.x for line in lines for p in line.points]
    ys = [p.y for line in lines for p in line.points]
    off_x = (min(xs) + max(xs)) // 2
    off_y = (min(ys) + max(ys)) // 2
    for line in lines:
        line.points = [GridPoint(p.x - off_x, p.y - off_y) for p in line.points]


def distance_point_to_segment(p, a, b) -> float:
    ab = (b[0] - a[0], b[1] - a[1])
    denom = ab[0] * ab[0] + ab[1] * ab[1]
    t = 0.0 if denom < 0.0001 else max(0.0, min(1.0, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / denom))
    closest = (a[0] + ab[0] * t, a[1] + ab[1] * t)
    return math.hypot(p[0] - closest[0], p[1] - closest[1])


def hits_body(body: LevelLineData, point, moving: LevelLineData) -> bool:
    for i in range(body.point_count - 1):
        a = (body.points[i].x, body.points[i].y)
        b = (body.points[i + 1].x, body.points[i + 1].y)
        if distance_point_to_segment(point, a, b) <= 0.42:
            return True
    return False


def can_new_line_exit(placed: List[LevelLineData], candidate: LevelLineData) -> bool:
    if candidate.point_count < 2:
        return False
    if not placed:
        return True
    head = candidate.get_head()
    direction = candidate.get_direction()
    length = math.hypot(direction[0], direction[1])
    if length == 0:
        return False
    dir_norm = (direction[0] / length, direction[1] / length)
    dist = 0.35
    while dist <= 64.0:
        sample = (head[0] + dir_norm[0] * dist, head[1] + dir_norm[1] * dist)
        for line in placed:
            if hits_body(line, sample, candidate):
                return False
        dist += 0.35
    return True


def find_removal_order(lines: List[LevelLineData]) -> Optional[List[int]]:
    remaining = list(range(len(lines)))
    order: List[int] = []
    while remaining:
        removable = -1
        for idx in reversed(remaining):
            if can_exit(lines, idx, remaining):
                removable = idx
                break
        if removable < 0:
            return None
        order.append(removable)
        remaining.remove(removable)
    return order


def can_exit(lines: List[LevelLineData], line_index: int, active) -> bool:
    line = lines[line_index]
    if line.point_count < 2:
        return False
    head = line.get_head()
    direction = line.get_direction()
    length = math.hypot(direction[0], direction[1])
    if length == 0:
        return False
    dir_norm = (direction[0] / length, direction[1] / length)
    dist = 0.35
    while dist <= 64.0:
        sample = (head[0] + dir_norm[0] * dist, head[1] + dir_norm[1] * dist)
        for other in active:
            if other == line_index:
                continue
            if hits_body(lines[other], sample, line):
                return False
        dist += 0.35
    return True


def conflicts_with_occupied(line: LevelLineData, occupied) -> bool:
    for i in range(line.point_count - 1):
        a = (line.points[i].x, line.points[i].y)
        b = (line.points[i + 1].x, line.points[i + 1].y)
        if pack_edge(a, b) in occupied:
            return True
    return False


def register_edges(line: LevelLineData, occupied) -> None:
    for i in range(line.point_count - 1):
        a = (line.points[i].x, line.points[i].y)
        b = (line.points[i + 1].x, line.points[i + 1].y)
        occupied.add(pack_edge(a, b))


def try_create_line(rng: random.Random, radius: int, segments: int, occupied) -> Optional[LevelLineData]:
    exit_dir = CARDINALS[rng.randrange(4)]
    head = (rng.randrange(-radius, radius + 1), rng.randrange(-radius, radius + 1))
    points = [GridPoint(*head)]
    visited = {head}
    current = (head[0] - exit_dir[0], head[1] - exit_dir[1])
    if max(abs(current[0]), abs(current[1])) > radius:
        return None
    points.insert(0, GridPoint(*current))
    visited.add(current)

    for _ in range(1, segments):
        options = []
        tail = points[0]
        for dx, dy in CARDINALS:
            nxt = (tail.x + dx, tail.y + dy)
            if max(abs(nxt[0]), abs(nxt[1])) > radius:
                continue
            if nxt in visited:
                continue
            options.append(nxt)
        if not options:
            return None
        chosen = options[rng.randrange(len(options))]
        points.insert(0, GridPoint(*chosen))
        visited.add(chosen)

    line = LevelLineData(points)
    if conflicts_with_occupied(line, occupied):
        return None
    return line


def build_lines(level_number: int, rng: random.Random) -> Optional[List[LevelLineData]]:
    target = ARROW_TARGETS[level_number]
    radius = max(8, min(12, 6 + target // 8))
    lines: List[LevelLineData] = []
    occupied = set()
    try_limit = 240

    for _ in range(target):
        placed = False
        for _ in range(try_limit):
            segments = rng.randint(1, 1)
            line = try_create_line(rng, radius, segments, occupied)
            if line is None:
                continue
            if not can_new_line_exit(lines, line):
                continue
            register_edges(line, occupied)
            lines.append(line)
            placed = True
            break
        if not placed:
            return None
    return lines


def generate_level(level_number: int, seed_offset: int = 0) -> Optional[List[LevelLineData]]:
    seed = level_number * 1543 + 27037 + seed_offset
    for attempt in range(50):
        rng = random.Random(seed + attempt * 911)
        lines = build_lines(level_number, rng)
        if not lines:
            continue
        if find_removal_order(lines) is None:
            continue
        center_lines(lines)
        return lines
    return None


def head_rotation_wz(dx: int, dy: int) -> Tuple[float, float]:
    angle = math.atan2(dy, dx) - math.pi / 2
    return math.cos(angle / 2), math.sin(angle / 2)


def mod_entry(target: str, prop: str, value) -> str:
    if isinstance(value, float):
        value_str = repr(value)
    else:
        value_str = str(value)
    return (
        f"    - target: {{fileID: {target}, guid: {LINE_GUID}, type: 3}}\n"
        f"      propertyPath: {prop}\n"
        f"      value: {value_str}\n"
        f"      objectReference: {{fileID: 0}}\n"
    )


def build_line_instance(line_index: int, line: LevelLineData, level_number: int) -> Tuple[str, str]:
    instance_id = 1_000_000_000 + level_number * 1_000_000 + line_index * 10_000 + 1
    transform_id = instance_id + 1
    name = f"Line ({line_index + 1})"
    head_x, head_y = line.get_head()
    dx, dy = line.get_direction()
    rot_w, rot_z = head_rotation_wz(dx, dy)

    mods = []
    mods.append(mod_entry("579988495362921800", "destroyDelay", 1.5))
    mods.append(mod_entry("1088355798733759430", "m_Enabled", 0))
    mods.append(mod_entry("3898713965489042308", "thickness", 0.3))
    mods.append(mod_entry("4852218959071952598", "m_Name", name))
    mods.append(mod_entry("5703179247697745139", "m_Positions.Array.size", line.point_count))
    for i, p in enumerate(line.points):
        mods.append(mod_entry("5703179247697745139", f"m_Positions.Array.data[{i}].x", p.x))
        mods.append(mod_entry("5703179247697745139", f"m_Positions.Array.data[{i}].y", p.y))
        mods.append(mod_entry("5703179247697745139", f"m_Positions.Array.data[{i}].z", 0))
    mods.append(mod_entry("5703179247697745139", "m_Parameters.widthMultiplier", 0.3))
    mods.append(mod_entry("6705024964587633368", "m_LocalPosition.x", -0.29))
    mods.append(mod_entry("6705024964587633368", "m_LocalPosition.y", 3))
    mods.append(mod_entry("6705024964587633368", "m_LocalPosition.z", 0))
    mods.append(mod_entry("6705024964587633368", "m_LocalRotation.w", 1))
    mods.append(mod_entry("6705024964587633368", "m_LocalRotation.x", -0))
    mods.append(mod_entry("6705024964587633368", "m_LocalRotation.y", -0))
    mods.append(mod_entry("6705024964587633368", "m_LocalRotation.z", -0))
    mods.append(mod_entry("6705024964587633368", "m_LocalEulerAnglesHint.x", 0))
    mods.append(mod_entry("6705024964587633368", "m_LocalEulerAnglesHint.y", 0))
    mods.append(mod_entry("6705024964587633368", "m_LocalEulerAnglesHint.z", 0))
    mods.append(mod_entry("8852158018286561598", "m_LocalScale.x", 0.9))
    mods.append(mod_entry("8852158018286561598", "m_LocalScale.y", 0.9))
    mods.append(mod_entry("8852158018286561598", "m_LocalScale.z", 0.9))
    mods.append(mod_entry("9200941245333506865", "m_LocalPosition.x", head_x))
    mods.append(mod_entry("9200941245333506865", "m_LocalPosition.y", head_y))
    mods.append(mod_entry("9200941245333506865", "m_LocalRotation.w", rot_w))
    mods.append(mod_entry("9200941245333506865", "m_LocalRotation.z", rot_z))

    instance = (
        f"--- !u!1001 &{instance_id}\n"
        f"PrefabInstance:\n"
        f"  m_ObjectHideFlags: 0\n"
        f"  serializedVersion: 2\n"
        f"  m_Modification:\n"
        f"    serializedVersion: 3\n"
        f"    m_TransformParent: {{fileID: {LINES_PARENT_ID}}}\n"
        f"    m_Modifications:\n"
        + "".join(mods)
        + "    m_RemovedComponents: []\n"
        + "    m_RemovedGameObjects: []\n"
        + "    m_AddedGameObjects: []\n"
        + "    m_AddedComponents: []\n"
        + f"  m_SourcePrefab: {{fileID: 100100000, guid: {LINE_GUID}, type: 3}}\n"
    )
    stripped = (
        f"--- !u!4 &{transform_id} stripped\n"
        f"Transform:\n"
        f"  m_CorrespondingSourceObject: {{fileID: 6705024964587633368, guid: {LINE_GUID}, type: 3}}\n"
        f"  m_PrefabInstance: {{fileID: {instance_id}}}\n"
        f"  m_PrefabAsset: {{fileID: 0}}\n"
    )
    return instance, stripped, str(transform_id)


def build_prefab(level_number: int, lines: List[LevelLineData]) -> str:
    with open(TEMPLATE_PATH, encoding="utf-8") as f:
        template = f.read()

    split_marker = "--- !u!1001"
    header, _ = template.split(split_marker, 1)

    header = header.replace("m_Name: Level 10", f"m_Name: Level {level_number}")

    band = level_number - 11
    header = re.sub(
        r"_timeThresholdsSec:\n  - 30\n  - 45\n  - 60",
        f"_timeThresholdsSec:\n  - {45 + band * 2}\n  - {60 + band * 2}\n  - {90 + band * 2}",
        header,
        count=1,
    )
    header = re.sub(r"_winCoins: 10", f"_winCoins: {10 + band}", header, count=1)

    transform_ids = []
    body_parts = []
    for i, line in enumerate(lines):
        instance, stripped, tid = build_line_instance(i, line, level_number)
        transform_ids.append(tid)
        body_parts.append(instance)
        body_parts.append(stripped)

    children_yaml = "\n".join(f"  - {{fileID: {tid}}}" for tid in transform_ids)
    header = re.sub(
        r"  m_Children:\n(?:  - \{fileID: \d+\}\n)+",
        f"  m_Children:\n{children_yaml}\n",
        header,
        count=1,
    )

    return header + "".join(body_parts)


def write_meta(prefab_path: str, guid: Optional[str] = None) -> str:
    meta_path = prefab_path + ".meta"
    if guid is None:
        guid = uuid.uuid4().hex
    content = (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "PrefabImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)
    return guid


def main() -> None:
    os.makedirs(LEVELS_DIR, exist_ok=True)
    guids = {}

    for level in range(11, 21):
        lines = None
        for offset in range(100):
            lines = generate_level(level, offset)
            if lines and len(lines) >= ARROW_TARGETS[level]:
                break
            lines = None

        if not lines:
            raise RuntimeError(f"Failed to generate solvable layout for level {level}")

        prefab_path = os.path.join(LEVELS_DIR, f"Level {level}.prefab")
        prefab_text = build_prefab(level, lines)
        with open(prefab_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(prefab_text)

        meta_path = prefab_path + ".meta"
        existing_guid = None
        if os.path.exists(meta_path):
            with open(meta_path, encoding="utf-8") as f:
                m = re.search(r"guid: ([0-9a-f]{32})", f.read())
                if m:
                    existing_guid = m.group(1)
        guids[level] = write_meta(prefab_path, existing_guid)
        order = find_removal_order(lines)
        print(f"Level {level}: {len(lines)} arrows, removal={len(order) if order else 0}, guid={guids[level]}")

    print("Done. Prefabs written to", LEVELS_DIR)
    print("GUID map for GameScene wiring:")
    for level, guid in guids.items():
        print(f"  Level {level}: {guid}")


if __name__ == "__main__":
    main()
