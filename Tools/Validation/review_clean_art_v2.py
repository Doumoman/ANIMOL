"""Create labeled, nearest-neighbor QA sheets from actual Unity comparison renders.
No runtime art is changed. Run after FreeShapeArtV2ParityTests.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path("Docs/Validation/CleanArtV2")
OUT = ROOT / "Review"
OUT.mkdir(parents=True, exist_ok=True)


def render(style, fixture):
    return Image.open(ROOT / "Renders" / f"{style}-{fixture}-Scene.png").convert("RGBA")


def paste(board, im, xy):
    board.paste(im, xy, im)


for theme in range(1, 6):
    # Each render is pasted at native pixel size; labels describe the source.
    board = Image.new("RGB", (2200, 1300), "#202936")
    draw = ImageDraw.Draw(board)
    for index, letter in enumerate("ABCD"):
        style = f"T{theme:02}_{letter}"
        ox, oy = (index % 2) * 1100, (index // 2) * 650
        draw.text((ox+8, oy+6), style + " / Unity Scene / native 1x", fill="white")
        entries = [("rectangle_5x3", 8, 48), ("open_u_pit", 185, 48),
                   ("closed_hole", 510, 48), ("thin_vertical_1x16", 1048, 48),
                   ("thin_horizontal_16x1", 8, 294), ("asymmetric_stairs", 8, 370),
                   ("overhang", 340, 370), ("branch_t", 670, 370)]
        for fixture, x, y in entries:
            draw.text((ox+x, oy+y-16), fixture, fill="#9bc5c8")
            paste(board, render(style, fixture), (ox+x, oy+y))
    board.save(OUT/f"T{theme:02}-native.png")

for theme in range(1, 6):
    for letter in "ABCD":
        style = f"T{theme:02}_{letter}"
        board = Image.new("RGB", (1160, 560), "#202936")
        draw = ImageDraw.Draw(board)
        entries = [("closed_hole", (0, 0, 128, 128), 4, (8, 38)),
                   ("maximum_rectangle_16x16", (64, 256, 128, 320), 8, (540, 38))]
        for fixture, box, scale, xy in entries:
            crop = render(style, fixture).crop(box)
            paste(board, crop.resize((crop.width*scale, crop.height*scale), Image.Resampling.NEAREST), xy)
            draw.text((xy[0], 8), f"{style} {fixture} PNG top-left {box} / {scale}x nearest", fill="white")
        board.save(OUT/f"{style}-zoom.png")
for theme in range(1, 6):
    board = Image.new("RGB", (2100, 1120), "#202936")
    draw = ImageDraw.Draw(board)
    for index, letter in enumerate("ABCD"):
        style = f"T{theme:02}_{letter}"
        ox, oy = (index % 2)*1050, (index // 2)*560
        im = render(style, "maximum_rectangle_16x16")
        paste(board, im, (ox+8, oy+32))
        draw.text((ox+8, oy+8), f"{style} / 16x16 cells / native 1x", fill="white")
        anchors = []
        for y in (2, 10):
            for x in (2, 10):
                h = 2166136261
                for c in f"{style}|{x}|{y}|0":
                    h = ((h ^ ord(c))*16777619) & 0xffffffff
                if h % 4:
                    anchors.append((x,y))
        if anchors:
            x, y = anchors[0]
            box = (x*32, (16-y-4)*32, (x+4)*32, (16-y)*32)
            paste(board, im.crop(box).resize((512, 512), Image.Resampling.NEAREST), (ox+530, oy+32))
            draw.text((ox+530, oy+8), f"motif anchor cell ({x},{y}), PNG {box} / 4x", fill="white")
    board.save(OUT/f"T{theme:02}-motifs.png")
print("5 native overview + 20 4x/8x crop + 5 native/motif sheets")
