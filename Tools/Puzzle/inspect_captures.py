"""Summarize original real Game View evidence and make a visual contact sheet."""
import json
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
folder = root / "Docs/Validation/Puzzle"
report = json.loads((folder / "gameview.json").read_text(encoding="utf-8"))
shots = report["shots"]
sheet = Image.new("RGB", (6 * 270, ((len(shots) + 5) // 6) * 510), "#121722")
draw = ImageDraw.Draw(sheet)
for i, shot in enumerate(shots):
    source = Image.open(folder / (shot["name"] + ".png"))
    assert source.size == (1080, 1920), (shot["name"], source.size)
    x, y = i % 6 * 270, i // 6 * 510
    sheet.paste(source.resize((270, 480)), (x, y + 30))
    draw.text((x + 4, y + 2), shot["name"], fill="white")
    draw.text((x + 4, y + 15), f'{shot["width"]}x{shot["height"]} camera {shot["camera"]["x"]:.2f},{shot["camera"]["y"]:.2f}', fill="#a5c4d4")
sheet.save(folder / "contact_sheet.png")
idle = [Image.open(Path(tempfile.gettempdir()) / "ANIMOL_PuzzleBackground" / f"idle_{i:02d}.png").convert("RGB").resize((360, 640)) for i in range(24)]
palette = idle[0].quantize(colors=128)
idle = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in idle]
idle[0].save(folder / "fixed_background_idle.gif", save_all=True, append_images=idle[1:], duration=125, loop=0, optimize=True)
summary = {
    "source": report["source"], "resolution": [report["screenWidth"], report["screenHeight"]],
    "screenshots": len(shots), "movingFrames": len(report["frames"]), "failure": report["failure"],
    "cameraRows": sorted(set(s["rows"] for s in shots)),
    "backgroundSource": report["backgroundSource"], "backgroundPixels": report["backgroundPixels"], "hudSource": report["hudSource"],
    "maximumBackgroundDisplacementPixels": {name: {axis: report[key][axis] / report["backgroundFrameSize"][axis] * report["screenWidth" if axis == "x" else "screenHeight"] for axis in ("x", "y")} for name, key in (("Far", "maxFar"), ("Mid", "maxMid"), ("Near", "maxNear"))},
    "portalCameraX": [min(f["camera"]["x"] for f in report["frames"] if f["map"] == "EdgePortal"), max(f["camera"]["x"] for f in report["frames"] if f["map"] == "EdgePortal")],
    "checks": report["checks"], "shots": shots,
}
(folder / "capture_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
# Keep full frame evidence while making the commit readable: one frame per line.
metadata = dict(report)
frames = metadata.pop("frames")
compact = json.dumps(metadata, indent=2)[:-2] + ',\n  "frames": [\n' + ",\n".join("    " + json.dumps(frame, separators=(",", ":")) for frame in frames) + "\n  ]\n}\n"
assert json.loads(compact) == report
(folder / "gameview.json").write_text(compact, encoding="utf-8")
print(json.dumps({k: v for k, v in summary.items() if k not in ("checks", "shots")}, indent=2))
