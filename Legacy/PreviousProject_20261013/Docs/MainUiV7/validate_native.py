"""Compare actual Unity GPU captures against an independent integer pixel compositor."""
from pathlib import Path
import json
import math
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Docs/Inbox/ANIMOL_main_ui_v7/runtime/assets"
CAPTURES = ROOT / "Docs/MainUiV7/Captures"
W, H = 352, 704


def direction(slot, salt):
    x = (((slot + 11) * 92821) ^ (salt * 0x9E3779B1)) & 0xFFFFFFFF
    x = (x + 117) & 0xFFFFFFFF
    x ^= (x << 13) & 0xFFFFFFFF
    x ^= x >> 17
    x ^= (x << 5) & 0xFFFFFFFF
    return -1 if x < 0x80000000 else 1


def draw(output, source, x, y, width, height):
    # Explicit point lookup at destination pixel centers, independent of Unity texture sampling.
    source = source.convert("RGBA")
    src = source.load()
    dst = output.load()
    for dy in range(max(0,y),min(H,y+height)):
        sy = math.floor((dy+.5-y)*source.height/height)
        for dx in range(max(0,x),min(W,x+width)):
            sx = math.floor((dx+.5-x)*source.width/width)
            pixel = src[sx,sy]
            if pixel[3] == 255:
                dst[dx,dy] = pixel[:3]



def scene(slot, p, t):
    output = Image.new("RGB", (W,H), (26,28,44))
    def plane(name, scale, speed):
        rnd = lambda value: math.floor(value + .5)
        width, height = rnd(W * scale), rnd(H * scale)
        x = rnd((W-width)/2 + direction(slot, 91)*88*(p-.5)*speed)
        y = rnd((H-height)/2 + 72*(p-.5)*speed)
        draw(output, Image.open(ASSETS / f"T{slot%5+1:02}-{name}.png"), x, y, width, height)
    plane("far", 1.16, .30)
    plane("mid", 1.36, .62)
    plane("platform", 1, 0)
    run = direction(slot, 314)
    frame = math.floor(t*14) % 8
    sheet = Image.open(ASSETS / ("rabbit-run-right.png" if run == 1 else "rabbit-run-left.png"))
    rabbit = sheet.crop((frame*64, 0, (frame+1)*64, 96))
    x = math.floor((-64+p*416 if run == 1 else 352-p*416) + .5)
    draw(output, rabbit, x, 436, 64, 96)
    plane("near", 1.6, 1.6)
    return output


def render(t):
    slot = math.floor(t/5)
    local = t-slot*5
    current = scene(slot, local/5, t)
    if slot > 0 and local < .36:
        previous = scene(slot-1, .999, t)
        for y in range(0,H,8):
            for x in range(0,W,8):
                if not .5*x/W+.5*y/H < local/.36*1.22-.11:
                    current.paste(previous.crop((x,y,x+8,y+8)), (x,y))
    return current


samples = {f"Lobby_T{i+1:02}": i*5+2.25 for i in range(5)}
samples.update({"Wipe_5.00": 5, "Wipe_5.18": 5.18, "Wipe_5.36": 5.36, "Wipe_25.18": 25.18})
results = []
for name, time in samples.items():
    actual = Image.open(CAPTURES / (name+"_native.png")).convert("RGB")
    expected = render(time)
    errors = sum(a != b for a,b in zip(actual.getdata(),expected.getdata()))
    results.append({"capture": name, "time": time, "pixels": W*H, "different_pixels": errors})
(ROOT / "Docs/MainUiV7/native-reference-comparison.json").write_text(json.dumps(results, indent=2))
print(json.dumps(results, indent=2))
raise SystemExit(1 if any(row["different_pixels"] for row in results) else 0)
