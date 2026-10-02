"""Independent v7 source, import-byte and near-mask verification (Pillow)."""
from pathlib import Path
import hashlib
import json
import math
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Docs/Inbox/ANIMOL_main_ui_v7/runtime'
manifest = json.loads((SOURCE / 'manifest.json').read_text(encoding='utf-8'))
palette = {tuple(bytes.fromhex(c[1:])) for c in manifest['palette']}
files = sorted((SOURCE / 'assets').glob('*.png'))
assert len(files) == 22
for path in files:
    image = Image.open(path).convert('RGBA')
    assert image.size == ((512, 96) if 'rabbit' in path.name else (352, 704))
    assert all(c[3] in (0, 255) and (c[3] == 0 or c[:3] in palette) for c in image.getdata())
    assert path.read_bytes() == (ROOT / 'Assets/ANIMOL/UI/MainUiV6/Textures' / path.name).read_bytes()

left = Image.open(SOURCE / 'assets/rabbit-run-left.png').convert('RGBA')
right = Image.open(SOURCE / 'assets/rabbit-run-right.png').convert('RGBA')
cells = {}
for frame in range(8):
    box = (frame*64, 0, (frame+1)*64, 96)
    assert left.crop(box).tobytes() == right.crop(box).transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes()
    for direction, sheet in [(-1, left), (1, right)]:
        cell = sheet.crop(box)
        l,t,r,b = cell.getbbox()
        assert l >= 4 and r <= 60 and t >= 4 and r-l <= 54 and b-t <= 84
        assert b-1 == (86 if frame in (3,7) else 90)
        assert all(cell.getpixel((x,y))[3] == 0 for x in range(64) for y in range(96) if x in (0,63) or y in (0,95))
        cells[direction,frame] = (cell, t+(b-t)*.6)
assert len({f['uniformScale'] for f in manifest['rabbit']['exportFrames']}) == 1
assert not any(f['sourceCanvasContact'] for f in manifest['rabbit']['exportFrames'])

rows = []
rnd = lambda v: math.floor(v+.5)
for theme in range(1,6):
    near = Image.open(SOURCE / f'assets/T{theme:02}-near.png').convert('RGBA')
    for p in [.2,.35,.5,.65,.8]:
        for camera in [-1,1]:
            width,height = rnd(352*1.6),rnd(704*1.6)
            nx = rnd((352-width)/2 + camera*88*(p-.5)*1.6)
            ny = rnd((704-height)/2 + 72*(p-.5)*1.6)
            for direction in [-1,1]:
                rx = rnd(-64+p*416 if direction == 1 else 352-p*416)
                for frame in range(8):
                    cell,head_bottom = cells[direction,frame]
                    opaque = overlap = head = head_overlap = 0
                    for y in range(96):
                        for x in range(64):
                            if not cell.getpixel((x,y))[3]: continue
                            opaque += 1
                            if y < head_bottom: head += 1
                            sx = math.floor((rx+x+.5-nx)*352/width)
                            sy = math.floor((436+y+.5-ny)*704/height)
                            covered = 0 <= sx < 352 and 0 <= sy < 704 and near.getpixel((sx,sy))[3] != 0
                            if covered:
                                overlap += 1
                                if y < head_bottom: head_overlap += 1
                    assert head_overlap == 0 and overlap/opaque <= .05
                    rows.append(dict(theme=theme,p=p,camera=camera,rabbit=direction,frame=frame,opaque=opaque,near_overlap=overlap,upper_overlap=head_overlap))
result = dict(source_files=22,mirrored_frames=8,validated_cells=16,near_samples=len(rows),max_near_pixels=max(r['near_overlap'] for r in rows),max_upper_pixels=max(r['upper_overlap'] for r in rows),samples=rows)
(ROOT / 'Docs/MainUiV7/independent-near-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print({k:v for k,v in result.items() if k != 'samples'})
