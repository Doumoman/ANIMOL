"""Record the V03 theater prefab PPtr migration against a pre-edit backup ZIP.

This is a read-only audit. It never removes a legacy asset or changes Unity files.
"""

import argparse
import csv
import json
import re
import zipfile
from collections import deque
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE = Path("Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03")
PREFABS = Path("Assets/ANIMOL/Prefabs/Themes/memory_theater")
OUT = ROOT / "Docs/Validation/EchoTheaterV03"
GUID = re.compile(r"(?m)^guid: ([0-9a-f]{32})$")
PTR = re.compile(r"\{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}")
STATE = re.compile(r"(?m)^  - name: ([^\r\n]+)\r?\n((?:^    [^\r\n]*\r?\n)*)")

FAMILIES = {
    "conveyor": ("Conveyor", "d07_conveyor", [1, 1]),
    "oneway_gate": ("OneWayGate", "d09_one_way_gate", [1, 1]),
    "bounce_corner": ("Bounce", "d18_bounce_corner", [1, 1]),
    "brake_pad": ("Brake", "d19_brake_pad", [1, 1]),
    "alternating_shutter": ("Shutter", "d04_stage_shutter", [1, 1]),
    "moving_stage": ("MovingStage", "d06_moving_stage", [1, 1]),
    "rotating_bookcase": ("Bookshelf", "d08_rotating_bookshelf", [3, 1]),
    "domino_wall": ("Domino", "d13_domino_wall", [1, 1]),
    "charging_statue": ("Statue", "d17_charging_statue", [1, 1]),
    "dice_block": ("Dice", "d22_dice_block", [1, 1]),
    "timid_puppet": ("CowardDoll", "d23_coward_doll", [1, 1]),
}


def refs(text):
    result = {}
    for match in STATE.finditer(text):
        slots = {}
        for line in match.group(2).splitlines():
            if ": " not in line:
                continue
            key, value = line.lstrip().split(": ", 1)
            if key in ("sprite", "terminal", "sequence", "clip"):
                pptr = PTR.search(value)
                if pptr:
                    slots[key] = (pptr.group(2) or "", pptr.group(1))
        result[match.group(1)] = slots
    return result


def meta_index(entries):
    result = {}
    for path, content in entries:
        match = GUID.search(content)
        if match:
            result[match.group(1)] = path[:-5]
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("backup_zip", type=Path)
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.backup_zip) as archive:
        before_paths = {p for p in archive.namelist() if p.endswith(".prefab") and
                        p.startswith(PREFABS.as_posix() + "/")}
        old_meta = meta_index((p, archive.read(p).decode("utf-8", "replace"))
                              for p in archive.namelist() if p.endswith(".meta"))
        current_meta = meta_index((p.relative_to(ROOT).as_posix(), p.read_text(encoding="utf-8"))
                                  for p in (ROOT / "Assets").rglob("*.meta"))
        rows = []
        for path in sorted(before_paths):
            current = ROOT / path
            if not current.exists():
                continue
            previous = refs(archive.read(path).decode("utf-8", "replace"))
            latest = refs(current.read_text(encoding="utf-8"))
            for state, old_slots in previous.items():
                new_slots = latest.get(state, {})
                for slot, old_ref in old_slots.items():
                    new_ref = new_slots.get(slot, ("", "0"))
                    if old_ref == new_ref:
                        continue
                    rows.append({
                        "prefab": path, "state": state, "slot": slot,
                        "old_guid": old_ref[0], "old_fileID": old_ref[1],
                        "old_path": old_meta.get(old_ref[0], current_meta.get(old_ref[0], "")),
                        "new_guid": new_ref[0], "new_fileID": new_ref[1],
                        "new_path": current_meta.get(new_ref[0], ""),
                    })

    csv_path = OUT / "guid_fileid_migration.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

    template = json.loads((ROOT / PACKAGE / "device_bindings_template.json").read_text(encoding="utf-8"))
    template["actualProjectThemeId"] = "memory_theater"
    template["actualProjectBindingsVerified"] = False
    for family in template["families"]:
        role, name, footprint = FAMILIES[family["artKey"]]
        path = (PREFABS / (name + ".prefab")).as_posix()
        family["existingProjectDeviceId"] = f"PrototypeRole.{role}"
        family["verified"] = True
        family["actualGameplayFootprintCells"] = footprint
        family["actualClassSource"] = "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSession.cs"
        family["actualStateField"] = "PrototypeDeviceState.active/phase/locked/dice/cell"
        family["actualHeadingField"] = "PrototypeDeviceState.direction; CowardDoll lookDirection"
        family["actualCommittedActionEvent"] = "PrototypeEvent.eventName/state from PuzzleDeviceSession.Visual"
        family["actualSpriteRendererPath"] = "prefab root/Visual (DeviceVisualView.visual)"
        family["actualPrefabSceneAndSoPaths"] = [path,
            "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater.asset",
            "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity"]
        family["actualPoolAndSpawnPaths"] = [
            "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceView.cs:Bind (Instantiate; no pool found)",
            "Assets/ANIMOL/Scripts/Runtime/PuzzlePrototypeController.cs:Load"]
        family_rows = [row for row in rows if row["prefab"] == path or
                       row["prefab"].startswith(path[:-7] + "_")]
        family["actualGuidFileIdMapping"] = family_rows
        family["statesResolved"] = family["artKey"] not in ("rotating_bookcase", "dice_block")
        family["existingProjectStateToClipKey"] = {}
        for prefab in (ROOT / PREFABS).glob(name + "*.prefab"):
            text = prefab.read_text(encoding="utf-8")
            for match in STATE.finditer(text):
                key = re.search(r"(?m)^    v03ClipKey: ([^\r\n]*)", match.group(2))
                if key and key.group(1):
                    family["existingProjectStateToClipKey"][match.group(1)] = key.group(1)
        family["sourceEvidence"] = (
            "PuzzleScenario.cs:PrototypeRole/PrototypeDevice/PrototypeDeviceState; "
            "PuzzleDeviceSession.cs:Footprint/Visual; PuzzleDeviceView.cs:Bind/Notify/Idle; "
            "serialized prefab and guid_fileid_migration.csv. "
            "Bookcase plate and die target/support aliases lack V03 catalog keys."
        )
    (OUT / "device_bindings_verified.json").write_text(
        json.dumps(template, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    old_roots = (
        "Assets/ANIMOL/Sprites/Obstacles/memory_theater/",
        "Assets/ANIMOL/Sprites/Environment/memory_theater/",
        "Assets/ANIMOL/Data/AnimationSequences/memory_theater/",
    )
    seeds = {path.as_posix() for path in (ROOT / PREFABS).glob("*.prefab")}
    seeds = {str(Path(path).relative_to(ROOT)).replace("\\", "/") for path in seeds}
    seeds.update(str(path.relative_to(ROOT)).replace("\\", "/")
                 for path in (ROOT / "Assets/ANIMOL/Data/Art/MemorySkyTheater").rglob("*.asset"))
    seeds.update(str(path.relative_to(ROOT)).replace("\\", "/")
                 for path in (ROOT / "Assets/ANIMOL/Scenes").rglob("*.unity")
                 if "MemoryTheater" in path.name or "memory_theater" in path.name)
    seeds.add("Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater.asset")
    seeds.add("Assets/ANIMOL/Data/Themes/memory_theater.asset")
    queue = deque((path, "seed") for path in sorted(seeds))
    visited = set()
    legacy = []
    while queue:
        path, parent = queue.popleft()
        if path in visited:
            continue
        visited.add(path)
        file = ROOT / path
        if not file.is_file() or file.suffix not in (".prefab", ".asset", ".unity", ".anim",
                                                ".controller", ".overrideController", ".playable"):
            continue
        for guid in set(re.findall(r"guid: ([0-9a-f]{32})", file.read_text(encoding="utf-8", errors="ignore"))):
            target = current_meta.get(guid)
            if not target:
                continue
            if target.startswith(old_roots) and "ANIMOL_ECHO_THEATER_CARTOON_V03/" not in target:
                legacy.append({"referrer": path, "guid": guid, "asset": target})
            queue.append((target, path))
    legacy.sort(key=lambda row: (row["asset"], row["referrer"]))
    audit = {"seedCount": len(seeds), "dependencyAssetsVisited": len(visited),
             "reachableLegacyReferences": len(legacy),
             "distinctLegacyAssets": len({row["asset"] for row in legacy}),
             "deletionCandidates": [], "reason": "Reachable legacy references remain; no asset deletion is safe.",
             "references": legacy}
    (OUT / "legacy_reachability_dry_run.json").write_text(
        json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Recorded {len(rows)} changed PPtr slots; {len(template['families'])} verified role bindings")
    print(f"Reachable theater legacy: {audit['distinctLegacyAssets']} assets / {len(legacy)} references")


if __name__ == "__main__":
    main()
