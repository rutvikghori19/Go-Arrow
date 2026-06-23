#!/usr/bin/env python3
"""Extract arrow polylines from Unity level prefabs into LevelDefinition JSON."""
import json, re, os, glob

LEVELS_DIR = r"C:\Unity Projects\Go-Arrow\Assets\_Game\Resources\Levels"
OUT_DIR = r"C:\Unity Projects\Go-Arrow\Assets\_Game\Resources\ProceduralLevels\Templates"

def extract_lines(text):
    lines = []
    chunks = text.split("value: Line (")
    for chunk in chunks[1:]:
        size_m = re.search(r"m_Positions\.Array\.size\s*\n\s*value: (\d+)", chunk)
        if not size_m:
            continue
        n = int(size_m.group(1))
        pts = []
        for i in range(n):
            xm = re.search(rf"m_Positions\.Array\.data\[{i}\]\.x\s*\n\s*value: ([-\d.]+)", chunk)
            ym = re.search(rf"m_Positions\.Array\.data\[{i}\]\.y\s*\n\s*value: ([-\d.]+)", chunk)
            if xm and ym:
                pts.append({"X": int(float(xm.group(1))), "Y": int(float(ym.group(1)))})
        if len(pts) >= 2:
            # local offset on line transform
            ox = oy = 0
            oxm = re.search(r"m_LocalPosition\.x\s*\n\s*value: ([-\d.]+)", chunk)
            oym = re.search(r"m_LocalPosition\.y\s*\n\s*value: ([-\d.]+)", chunk)
            if oxm:
                ox = int(round(float(oxm.group(1))))
            if oym:
                oy = int(round(float(oym.group(1))))
            if ox or oy:
                pts = [{"X": p["X"] + ox, "Y": p["Y"] + oy} for p in pts]
            lines.append({"Points": pts})
    return lines

os.makedirs(OUT_DIR, exist_ok=True)
for path in sorted(glob.glob(os.path.join(LEVELS_DIR, "Level *.prefab"))):
    name = os.path.basename(path)
    if name == "Level_Base.prefab":
        continue
    m = re.search(r"Level (\d+)", name)
    if not m:
        continue
    num = int(m.group(1))
    if num > 20:
        continue
    with open(path, encoding="utf-8") as f:
        text = f.read()
    arrow_lines = extract_lines(text)
    definition = {
        "LevelNumber": num,
        "Shape": 1,
        "Tier": 0,
        "GridSize": 1,
        "CellSize": 1.0,
        "DifficultyScore": num,
        "TargetLineCount": len(arrow_lines),
        "Lines": arrow_lines,
    }
    out_path = os.path.join(OUT_DIR, f"Level_{num:02d}.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(definition, f, indent=2)
    print(f"Level {num}: {len(arrow_lines)} arrows -> {out_path}")
