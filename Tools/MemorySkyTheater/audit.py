"""Read-only package/import, Game View pixel and protected-asset audit. Run at repo root."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

SOURCE = Path('Docs/Inbox/MemorySkyTheater/ANIMOL_MEMORY_SKYTHEATER_V02')
OUTPUT = Path('Docs/Validation/MemorySkyTheater')

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def target(source):
    if source.startswith('tiles/'):
        return 'Assets/ANIMOL/Sprites/Tiles/Floor/memory_theater/SkyTheater/' + source[6:]
    if source.startswith('props/'):
        return 'Assets/ANIMOL/Sprites/Props/memory_theater/SkyTheater/' + source[6:]
    if source.startswith('animation/'):
        return 'Assets/ANIMOL/Sprites/Props/memory_theater/SkyTheater/Animation/' + source[10:]
    return 'Assets/ANIMOL/Sprites/Background/memory_theater/SkyTheater/' + source[12:]

def audit(require_captures=False):
    count = 0
    for line in (SOURCE / 'SHA256SUMS.txt').read_text(encoding='utf-8').splitlines():
        digest, name = line.split(None, 1)
        assert sha(SOURCE / name.strip().lstrip('*')) == digest, name
        count += 1
    manifest = json.loads((SOURCE / 'manifest.json').read_text())
    files = [b['file'] for m in manifest['materials'] for b in m['blob47']]
    files += [p['file'] for p in manifest['props']]
    files += [f['file'] for a in manifest['animations'] for f in a['frames']]
    files += [b['file'] for b in manifest['backgrounds']]
    assert len(files) == 165
    active = {tuple(bytes.fromhex(c[1:])) for c in manifest['activePalette']}
    master = {tuple(bytes.fromhex(c[1:])) for c in manifest['palette']}
    imports = []
    for source in files:
        dest = target(source)
        assert sha(SOURCE / source) == sha(dest), dest
        im = Image.open(dest).convert('RGBA')
        colors = im.getcolors(65536)
        assert colors is not None
        palette = master if source.startswith('backgrounds/') else active
        for n, color in colors:
            assert color[3] in (0, 255), dest
            assert color[:3] in palette if color[3] else color[:3] == (0, 0, 0), dest
        imports.append(dict(source=source, target=dest, size=im.size, sha256=sha(dest)))
    fixed_checks = []
    for animation in manifest['animations']:
        if 'fixedOutsideRect' not in animation:
            continue
        first = Image.open(SOURCE / animation['frames'][0]['file']).convert('RGBA')
        x, y, w, h = animation['fixedOutsideRect']
        regions = [(0, 0, first.width, y), (0, y+h, first.width, first.height),
                   (0, y, x, y+h), (x+w, y, first.width, y+h)]
        for frame in animation['frames']:
            im = Image.open(SOURCE / frame['file']).convert('RGBA')
            assert im.size == first.size
            for rect in regions:
                assert im.crop(rect).tobytes() == first.crop(rect).tobytes(), frame['file']
        fixed_checks.append(animation['name'])
    for index, name in enumerate(['footlights_off_80x80', 'footlights_dim_80x80', 'footlights_80x80']):
        assert Image.open(SOURCE / ('animation/footlights_reveal/frame_0%d.png' % index)).convert('RGBA').tobytes() == Image.open(SOURCE / ('props/' + name + '.png')).convert('RGBA').tobytes()
    comparisons = []
    pairs = [('stage_join_1x.png', 'inspection/stage_join_1x.png'),
             ('stage_join_4x.png', 'inspection/stage_join_4x.png'),
             ('velvet_join_1x.png', 'inspection/velvet_join_1x.png'),
             ('velvet_join_4x.png', 'inspection/velvet_join_4x.png'),
             ('proscenium_3.png', 'inspection/proscenium_3_cells.png'),
             ('proscenium_5.png', 'inspection/proscenium_5_cells.png'),
             ('proscenium_9.png', 'inspection/proscenium_9_cells.png'),
             ('background_A.png', 'camera/A_origin_composite.png'),
             ('background_B.png', 'camera/B_right_down_composite.png'),
             ('background_C.png', 'camera/C_left_down_composite.png')]
    for actual, expected in pairs:
        if not (OUTPUT / actual).exists() and not require_captures:
            continue
        a = Image.open(OUTPUT / actual).convert('RGB'); b = Image.open(SOURCE / expected).convert('RGB')
        assert a.size == b.size and ImageChops.difference(a, b).getbbox() is None, actual
        comparisons.append(dict(capture=actual, reference=expected, size=a.size, differing_rgb_pixels=0))
    baseline = json.loads((OUTPUT / 'before_hashes.json').read_text(encoding='utf-8-sig'))
    changed = [f for f, digest in baseline.items() if sha(f) != digest]
    allowed = ['Assets/ANIMOL/Scripts/Runtime/' + n + '.cs' for n in ['PuzzleTheme', 'PuzzleMapView', 'PuzzleBackdrop', 'SkyIslandBackdrop']]
    allowed += ['Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Art_QA.unity']
    assert sorted(changed) == sorted(allowed), changed
    report = dict(source_sha256_verified=count, original_png_imports=imports, palette_alpha_and_transparent_rgb_verified=True,
                  fixed_curtain_frame_pixels_verified=fixed_checks, reveal_frames_match_off_dim_on=True,
                  game_view_comparisons=comparisons, protected_assets=len(baseline), changed_existing_assets=changed)
    (OUTPUT / 'audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print('Source SHA256:', count, '/ PNG imports:', len(imports), '/ exact Game View comparisons:', len(comparisons), '/ protected assets:', len(baseline))

if __name__ == '__main__':
    audit(require_captures=True)
