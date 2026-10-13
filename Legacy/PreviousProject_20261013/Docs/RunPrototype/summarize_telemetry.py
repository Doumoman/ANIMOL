"""Summarize measured CSV samples, without changing raw physics evidence."""
import csv
import json
from pathlib import Path
from statistics import median

ROOT = Path(__file__).resolve().parent


def crossing(rows, x, direction, after_turn):
    for previous, current in zip(rows, rows[1:]):
        if (int(current["launches"]) > 0) != after_turn:
            continue
        a, b = float(previous["x"]), float(current["x"])
        if (direction > 0 and a <= x < b) or (direction < 0 and a >= x > b):
            t = (x - a) / (b - a)
            return {key: round(float(previous[key]) + t * (float(current[key]) - float(previous[key])), 3)
                    for key in ("seconds", "stamina")}
    return None


results = []
for path in sorted((ROOT / "Telemetry").glob("*.csv")):
    with path.open(encoding="utf-8-sig", newline="") as source:
        rows = list(csv.DictReader(source))
    summary = json.loads(path.with_suffix(".json").read_text(encoding="utf-8-sig"))
    legs = []
    for name, direction, after in (("A", 1, False), ("B", -1, True)):
        start = crossing(rows, 0 if direction > 0 else 80, direction, after)
        end = crossing(rows, 80 if direction > 0 else 0, direction, after)
        legs.append({"leg": name, "continuousFloorTiles": 80, "start": start, "end": end,
                     "elapsed": round(end["seconds"] - start["seconds"], 3) if start and end else None})
    speeds = {}
    for pace in ("Walk", "Run"):
        values = [abs(float(row["velocityX"])) for row in rows
                  if row["pace"] == pace and row["grounded"] == "True" and abs(float(row["velocityX"])) > 1]
        speeds[pace] = round(median(values), 3) if values else None
    results.append({"scenario": summary["scenario"], "strategy": summary["strategy"],
                    "legs": legs, "medianGroundSpeed": speeds, "result": summary})
(ROOT / "leg-measurements.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
print(json.dumps(results, indent=2))
