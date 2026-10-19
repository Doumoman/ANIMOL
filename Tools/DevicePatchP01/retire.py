"""Remove P01 wind sprite candidates only after their generated owners are unreferenced."""
import argparse
import json
import re
from pathlib import Path

from overlay import PROJECT, ROOTS


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    ranch = ROOTS["ANIMOL_STARWAY_RANCH_CARTOON_V12"]
    records = json.loads((ranch / "p01_retired_assets.json").read_text(encoding="utf-8"))["files"]
    old = [ranch / row["file"] for row in records]
    generated = [p for p in (ranch / "_Generated").rglob("*")
                 if "flow_effect" in p.name and p.suffix in {".anim", ".asset"}]
    assert len(old) == 17 and len(generated) == 8
    candidates = old + generated
    text = [(p, p.read_text(encoding="utf-8", errors="ignore"))
            for p in (PROJECT / "Assets").rglob("*")
            if p.is_file() and p.suffix in {".prefab", ".asset", ".anim", ".unity",
                                               ".controller", ".overrideController", ".spriteatlas", ".cs"}]
    external = []
    for target in candidates:
        meta = Path(str(target) + ".meta")
        guid = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
        assert guid, meta
        refs = [str(p.relative_to(PROJECT)).replace("\\", "/") for p, content in text
                if p not in candidates and (guid.group(1) in content or
                                            (target.name == "wind_effect_manifest.json" and
                                             "wind_effect_manifest.json" in content))]
        if refs:
            external.append({"file": str(target.relative_to(PROJECT)), "references": refs})
    report = {"candidates": [str(p.relative_to(PROJECT)).replace("\\", "/") for p in candidates],
              "externalReferences": external, "deletionExecuted": args.apply and not external}
    output = PROJECT / "Docs/Validation/DevicePatchP01/retirement_audit.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"candidates": len(candidates), "externalReferences": len(external),
                      "applied": report["deletionExecuted"]}))
    if external:
        raise SystemExit(1)
    if args.apply:
        for target in candidates:
            target.unlink()
            Path(str(target) + ".meta").unlink()


if __name__ == "__main__":
    main()
