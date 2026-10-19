"""Audit P01 package bytes, existing sprite identity, and retired references."""
import csv
import hashlib
import json
import re
from pathlib import Path

from overlay import DEFAULT_PATCH, PROJECT, ROOTS, destination


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    manifest = json.loads((DEFAULT_PATCH / "patch_manifest.json").read_text(encoding="utf-8"))
    copied = []
    mismatches = []
    for entry in manifest["files"]:
        dst = destination(entry)
        if dst is None:
            continue
        actual = sha(dst) if dst.is_file() else None
        copied.append(str(dst.relative_to(PROJECT)).replace("\\", "/"))
        if actual != entry["sha256"]:
            mismatches.append({"file": copied[-1], "expected": entry["sha256"], "actual": actual})

    identity = []
    with (PROJECT / "Docs/Validation/DevicePatchP01/guid_fileid_before.csv").open(newline="", encoding="utf-8") as source:
        for row in csv.DictReader(source):
            asset = PROJECT / row["assetPath"]
            meta = Path(str(asset) + ".meta")
            match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
            after = match.group(1) if match else None
            identity.append({"assetPath": row["assetPath"], "guidBefore": row["guid"],
                             "guidAfter": after, "spriteFileIDBefore": row["spriteFileID"],
                             "guidPreserved": after == row["guid"]})

    ranch = ROOTS["ANIMOL_STARWAY_RANCH_CARTOON_V12"]
    retired = json.loads((ranch / "p01_retired_assets.json").read_text(encoding="utf-8"))
    candidates = []
    text_suffixes = {".prefab", ".asset", ".anim", ".unity", ".controller",
                     ".overrideController", ".spriteatlas", ".playable", ".meta"}
    haystack = []
    for path in (PROJECT / "Assets").rglob("*"):
        if path.is_file() and path.suffix in text_suffixes:
            haystack.append((path, path.read_text(encoding="utf-8", errors="ignore")))
    for entry in retired["files"]:
        asset = ranch / entry["file"]
        meta = Path(str(asset) + ".meta")
        guid_match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M) if meta.exists() else None
        guid = guid_match.group(1) if guid_match else None
        references = [str(path.relative_to(PROJECT)).replace("\\", "/")
                      for path, content in haystack if guid and path != meta and guid in content]
        candidates.append({"file": str(asset.relative_to(PROJECT)).replace("\\", "/"),
                           "guid": guid, "sha256": sha(asset) if asset.exists() else None,
                           "expectedSha256": entry["sha256"], "references": references})

    result = {"overlayFilesChecked": len(copied), "overlayMismatches": mismatches,
              "preexistingSpriteGuidsChecked": len(identity),
              "preexistingSpriteGuidChanges": [r for r in identity if not r["guidPreserved"]],
              "retiredCandidates": candidates,
              "retiredDeletionPerformed": False,
              "note": "GUID text scan covers serialized Assets files; runtime strings and external bundles need separate review."}
    output = PROJECT / "Docs/Validation/DevicePatchP01/asset_audit.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"overlay": len(copied), "mismatches": len(mismatches),
                      "guids": len(identity), "guidChanges": len(result["preexistingSpriteGuidChanges"]),
                      "retired": len(candidates), "referencedRetired": sum(bool(x["references"]) for x in candidates)}))
    if mismatches or result["preexistingSpriteGuidChanges"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
