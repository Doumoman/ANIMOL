"""Assemble comparisons from unmodified Game View captures; encode the input replay."""
from pathlib import Path
import json
import subprocess
from PIL import Image, ImageDraw, ImageFont
import imageio_ffmpeg

ROOT = Path(__file__).resolve().parent / "Captures"
FONT = "C:/Windows/Fonts/malgun.ttf"

def font(size):
    return ImageFont.truetype(FONT, size)

for height in (1920, 2400):
    paths = [ROOT / f"{side}_1080x{height}.png" for side in ("before", "after")]
    if not all(p.exists() for p in paths):
        continue
    sheet = Image.new("RGB", (2160, height + 110), "#101e32")
    draw = ImageDraw.Draw(sheet)
    for column, (path, label) in enumerate(zip(paths, ("기존 자산 표현 · 동일 QA 배치", "월궁 대표 화면 · QA 전용 개선"))):
        with Image.open(path) as image:
            assert image.size == (1080, height), (path, image.size)
            sheet.paste(image.convert("RGB"), (1080 * column, 110))
        draw.text((1080 * column + 42, 30), label, font=font(36), fill="#c8e5cb")
    sheet.save(ROOT / f"comparison_1080x{height}.png")

# No generated/redrawn gameplay pixels: crops are nearest-neighbour enlarged from the Game View.
groups = [
    ("M1 달빛 발판", (220, 900, 395, 1080), ("solid", "warning", "hidden", "recover"), ("밟기 가능", "소멸 예고", "숨김 · 충돌 없음", "복귀 중")),
    ("M2 옥 저울", (370, 725, 930, 1050), ("solid", "active", "recover"), ("대기", "점유 · 하강", "비점유 · 복귀")),
    ("M6 월상 계단", (490, 465, 830, 805), ("solid", "warning", "active", "recover"), ("수평", "점유 · 전환 보류", "공간 확보 · 회전", "수평 복귀")),
]
sheet = Image.new("RGB", (1920, 1440), "#101e32")
draw = ImageDraw.Draw(sheet)
draw.text((30, 18), "작동 상태 확대 · 실제 캡처 / 제어된 점유 시나리오", font=font(32), fill="#c8e5cb")
for row, (title, box, states, labels) in enumerate(groups):
    draw.text((30, 90 + row * 440), title, font=font(30), fill="#78bbae")
    for col, (state, label) in enumerate(zip(states, labels)):
        prefix = title.split()[0]
        path = ROOT / ("state_M1_solid.png" if state == "solid" else f"state_{prefix}_{state}.png")
        with Image.open(path) as full:
            crop = full.crop(box).convert("RGB")
            scale = min(445 / crop.width, 330 / crop.height)
            crop = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.Resampling.NEAREST)
            sheet.paste(crop, (col * 480 + 18, 145 + row * 440))
            crop.save(ROOT / f"detail_{prefix}_{state}.png")
        draw.text((col * 480 + 18, 478 + row * 440), label, font=font(23), fill="#dcb16d")
sheet.save(ROOT / "state_comparison.png")

record = json.loads((ROOT / "recording.json").read_text(encoding="utf-8"))
rate = record["frames"] / record["seconds"]
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error", "-framerate", str(rate),
    "-i", str(ROOT / "frames/play_%04d.png"), "-c:v", "libx264", "-preset", "medium", "-crf", "20",
    "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(ROOT / "moon_input_replay.mp4")], check=True)
print("Packaged two resolution comparisons, state details and real-time input replay.")

replay = Image.new("RGB", (2160, 1080), "#101e32")
for index, sample in enumerate((0, 12, 24, 36, 48, 60, 75, 99)):
    with Image.open(ROOT / f"frames/play_{sample:04d}.png") as frame:
        frame = frame.crop((130, 600, 900, 1200)).resize((540, 421), Image.Resampling.NEAREST)
        replay.paste(frame, ((index % 4) * 540, (index // 4) * 540 + 55))
    ImageDraw.Draw(replay).text(((index % 4) * 540 + 20, (index // 4) * 540 + 12), f"{sample / rate:.1f}s", font=font(26), fill="#c8e5cb")
replay.save(ROOT / "replay_keyframes.png")
