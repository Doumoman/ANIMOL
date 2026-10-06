"""Verify the supplied package without rewriting its manifest-covered QA records.

Run from the repository root: python Tools/Validation/verify_clean_art_v2.py
Requires Pillow. This checks source PNGs, not Unity rendering or aesthetic quality.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path("Tools/ArtSources/ANIMOL_FreeShape_Sprites_clean_v2")
OUTPUT = Path("Docs/Validation/CleanArtV2/PackageVerification.json")


def read(name):
    return json.loads((ROOT / name).read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    manifest = read("PACKAGE_MANIFEST.json")
    for record in manifest["files"]:
        path = (ROOT / record["path"]).resolve()
        assert path.is_relative_to(ROOT.resolve()), record["path"]
        assert path.stat().st_size == record["bytes"], path
        assert sha(path) == record["sha256"], path
    approved = read("Data/approved_design_sources.json")["sources"]
    assert len(approved) == 5
    for record in approved:
        path = ROOT / record["sourceFile"]
        assert path.stat().st_size == record["byteLength"] and sha(path) == record["sha256"]
    lookup = read("Data/sprite_lookup.json")
    topology = read("Data/topology_catalog.json")
    names = read("Data/style_catalog.json")
    assert lookup["schemaVersion"] == 1 and lookup["artVersion"] == 2
    assert lookup["contractId"] == "ANIMOL_FREE_SHAPE_BLOB47_V1"
    for key in ("canonicalMasks", "rawToCanonical", "rawToIndex"):
        assert lookup[key] == topology[key]
    assert {s["styleId"] for s in lookup["styles"]} == {s["styleId"] for s in names["styles"]}
    palette = {tuple(bytes.fromhex(c.lstrip("#"))) for c in lookup["palette"]}
    cells = atlases = motifs = 0
    for style in lookup["styles"]:
        for variant in style["variants"]:
            atlas = Image.open(ROOT / variant["atlas"]).convert("RGBA")
            assert atlas.size == (256, 192)
            for rgba in set(atlas.getdata()):
                assert rgba[3] in (0, 255) and (rgba[3] == 0 or rgba[:3] in palette)
            for cell in variant["cells"]:
                rect = cell["rect"]
                sprite = Image.open(ROOT / cell["file"]).convert("RGBA")
                assert sprite.size == (32, 32)
                assert sprite.tobytes() == atlas.crop((rect["x"], rect["y"], rect["x"]+32, rect["y"]+32)).tobytes()
                cells += 1
            atlases += 1
        motif = Image.open(ROOT / style["panels"]["motif"]).convert("RGBA")
        assert motif.size == (128, 128)
        for rgba in set(motif.getdata()):
            assert rgba[3] in (0, 255) and (rgba[3] == 0 or rgba[:3] in palette)
        motifs += 1
    assert (atlases, cells, motifs) == (80, 3760, 20)
    result = dict(status="PASS", verifiedPackageFiles=len(manifest["files"]),
                  manifestSha256=sha(ROOT/"PACKAGE_MANIFEST.json"), approved=approved,
                  atlases=atlases, cellPngsEqualAtlasRects=cells, motifs=motifs,
                  scope="Package bytes, approved sources, lookup, PNG rects, palette and alpha only; no aesthetic verdict or Unity validation.")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({k:v for k,v in result.items() if k != "approved"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
