"""Verify and apply the P01 changed-files overlay without touching existing Unity meta files."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
DEFAULT_PATCH = PROJECT.parent / "ANIMOL_DevicePatchP01_Extracted_20261019" / "ANIMOL_DEVICE_PATCH_P01"
ROOTS = {
    "ANIMOL_STARWAY_RANCH_CARTOON_V12": PROJECT / "Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12",
    "ANIMOL_ECHO_THEATER_CARTOON_V03": PROJECT / "Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03",
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def destination(entry):
    root = entry["packageRoot"]
    relative = entry["relativeFile"]
    if root in ROOTS:
        if relative.startswith("Editor/"):
            return PROJECT / "Assets/ANIMOL/Scripts/Editor" / Path(relative).name
        if relative.startswith(("sources/", "production/")):
            return None
        return ROOTS[root] / relative
    if root == "ANIMOL_DEVICE_PATCH_P01":
        name = Path(relative).name
        if relative.startswith("Runtime/"):
            return PROJECT / "Assets/ANIMOL/Scripts/Runtime" / name
        if relative.startswith("Editor/"):
            return PROJECT / "Assets/ANIMOL/Scripts/Editor" / name
        if relative.startswith("Tests~/"):
            return PROJECT / "Assets/ANIMOL/Scripts/Tests/Editor" / name
        if relative.startswith("Shaders/"):
            return PROJECT / "Assets/ANIMOL/Shaders" / name
        if relative == "wind_runtime_manifest.json":
            return PROJECT / "Assets/ANIMOL/Data/Art/DevicePatchP01" / name
    return None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--patch", type=Path, default=DEFAULT_PATCH)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    manifest = json.loads((args.patch / "patch_manifest.json").read_text(encoding="utf-8"))
    rows = []
    errors = []
    for report in manifest["packageReports"]:
        root = ROOTS["ANIMOL_STARWAY_RANCH_CARTOON_V12" if report["theme"] == "ranch"
                     else "ANIMOL_ECHO_THEATER_CARTOON_V03"]
        for field, name in (("baselineManifestSha256", "manifest.json"),
                            ("baselineCatalogSha256", "animation_catalog.json")):
            actual = digest(root / name)
            if actual != report[field]:
                errors.append(f"BASELINE {root / name}: {actual} != {report[field]}")
    for entry in manifest["files"]:
        src = args.patch / entry["overlayFile"]
        dst = destination(entry)
        if not src.is_file() or digest(src) != entry["sha256"]:
            errors.append("PACKAGE " + str(src))
            continue
        if dst is None:
            rows.append({"source": entry["overlayFile"], "action": "excluded from runtime"})
            continue
        old = entry.get("expectedBaselineSha256")
        if old is not None:
            if not dst.is_file() or digest(dst) != old:
                errors.append("BASELINE " + str(dst))
                continue
        elif dst.is_file():
            errors.append("UNEXPECTED EXISTING " + str(dst))
            continue
        rows.append({"source": entry["overlayFile"], "destination": str(dst.relative_to(PROJECT)).replace("\\", "/"),
                     "action": "replace" if old else "add",
                     "beforeSha256": old, "afterSha256": entry["sha256"]})
    report = {
        "baselineAccepted": not errors, "applied": args.apply and not errors,
        "errors": errors, "files": rows,
    }
    out = PROJECT / "Docs/Validation/DevicePatchP01/overlay_audit.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if errors:
        raise SystemExit("\n".join(errors))
    if args.apply:
        for entry in manifest["files"]:
            dst = destination(entry)
            if dst is None:
                continue
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(args.patch / entry["overlayFile"], dst)
    print(f"baseline OK; {len(rows)} runtime/editor files; applied={report['applied']}")


if __name__ == "__main__":
    main()
