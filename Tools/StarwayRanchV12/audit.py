"""Read-only V12 package and serialized legacy-reference audit."""
import csv
import hashlib
import json
import re
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[2]
ROOT = PROJECT / "Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12"
EVIDENCE = PROJECT / "Docs/Validation/StarwayRanchV12"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    manifest = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
    catalog = json.loads((ROOT / "animation_catalog.json").read_text(encoding="utf-8"))
    mismatches = [row["file"] for row in manifest["assets"]
                  if not (ROOT / row["file"]).is_file()
                  or sha(ROOT / row["file"]) != row["sha256"]]
    catalog_hash = sha(ROOT / "animation_catalog.json")
    expected_hash = (ROOT / "animation_catalog_sha256.txt").read_text(encoding="utf-8").strip().split()[0]
    generated = ROOT / "_Generated"
    clips = list(generated.glob("*.anim"))
    sequences = list((generated / "Sequences").glob("*.asset"))
    build = json.loads((generated / "animation_build_report.json").read_text(encoding="utf-8"))
    old_rows = list(csv.DictReader((EVIDENCE / "legacy_guid_fileid_dry_run.csv").open(encoding="utf-8")))
    old_guids = {row["old_guid"] for row in old_rows}
    refs = []
    suffixes = {".unity", ".asset", ".prefab", ".anim", ".controller",
                ".overridecontroller", ".spriteatlas", ".spriteatlasv2", ".playable", ".mat"}
    for path in (PROJECT / "Assets").rglob("*"):
        if path.suffix.lower() not in suffixes:
            continue
        try:
            found = set(re.findall(r"guid: ([0-9a-f]{32})", path.read_text(errors="ignore"))) & old_guids
        except OSError:
            continue
        refs.extend({"path": str(path.relative_to(PROJECT)).replace("\\", "/"), "guid": guid}
                    for guid in sorted(found))
    prefab_root = PROJECT / "Assets/ANIMOL/Prefabs/Themes/starway"
    result = {
        "manifest_runtime_png": len(manifest["assets"]),
        "runtime_png_sha_mismatches": mismatches,
        "catalog_sha256": catalog_hash,
        "catalog_hash_matches_sidecar": catalog_hash == expected_hash,
        "catalog_sprites": len(catalog["sprites"]),
        "catalog_clips": len(catalog["clips"]),
        "catalog_parts": len(catalog["parts"]),
        "generated_anim_count": len(clips),
        "generated_sequence_count": len(sequences),
        "builder_success": build["success"],
        "builder_hash_matches_catalog": build["manifestSha256"] == catalog_hash,
        "starway_prefab_count": len(list(prefab_root.glob("*.prefab"))),
        "old_v10_sprite_guid_count": len(old_guids),
        "reachable_old_v10_serialized_references": refs,
        "remaining_old_v10_png": len([p for p in (ROOT.parent).rglob("*.png") if ROOT not in p.parents]),
    }
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    (EVIDENCE / "final_file_audit.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k != "reachable_old_v10_serialized_references"},
                     ensure_ascii=False, indent=2))
    if (mismatches or catalog_hash != expected_hash or len(clips) != 46 or len(sequences) != 46
            or not build["success"] or result["reachable_old_v10_serialized_references"]):
        raise SystemExit(1)


if __name__ == "__main__":
    main()
