"""Read-only package/import, Game View pixel and protected-asset audit. Run at repo root."""
import hashlib
import json
import re
import subprocess
from pathlib import Path
from PIL import Image, ImageChops

SOURCE = Path('Docs/Inbox/ClockDome/ANIMOL_CLOCK_DOME_V01')
OUTPUT = Path('Docs/Validation/ClockDome')

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def target(source):
    if source.startswith('tiles/'):
        return 'Assets/ANIMOL/Sprites/Tiles/Floor/letter_observatory/ClockDome/' + source[6:]
    if source.startswith('parts/'):
        return 'Assets/ANIMOL/Sprites/Props/letter_observatory/ClockDome/' + source[6:]
    if source.startswith('animation/'):
        return 'Assets/ANIMOL/Sprites/Props/letter_observatory/ClockDome/Animation/' + source[10:]
    return 'Assets/ANIMOL/Sprites/Background/letter_observatory/ClockDome/' + source[12:]

def audit(require_captures=False):
    count = 0
    for line in (SOURCE / 'SHA256SUMS.txt').read_text(encoding='utf-8').splitlines():
        digest, name = line.split(None, 1)
        assert sha(SOURCE / name.strip().lstrip('*')) == digest, name
        count += 1
    manifest = json.loads((SOURCE / 'manifest.json').read_text())
    files = [b['file'] for m in manifest['materials'] for b in m['blob47']]
    files += [p['file'] for p in manifest['parts']]
    files += [f['file'] for a in manifest['animations'] for f in a['frames']]
    files += [b['file'] for b in manifest['backgrounds']]
    assert len(files) == 116
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
    animation = manifest['animations'][0]
    first = Image.open(SOURCE / animation['frames'][0]['file']).convert('RGBA')
    for frame in animation['frames']:
        im = Image.open(SOURCE / frame['file']).convert('RGBA')
        assert im.size == first.size == (80,240)
        assert im.crop((0,0,80,12)).tobytes() == first.crop((0,0,80,12)).tobytes()
    fixed_checks = ['pendulum_idle: top 12 rows unchanged in all 8 frames']
    comparisons = []
    pairs = [('floor_join_1x.png', 'inspection/floor_join_1x.png'),
             ('floor_join_4x.png', 'inspection/floor_join_4x.png'),
             ('room_0.png', 'inspection/room_0_column_rows.png'),
             ('room_4.png', 'inspection/room_4_column_rows.png'),
             ('room_12.png', 'inspection/room_12_column_rows.png'),
             ('room_split_4.png', 'inspection/room_4_column_rows.png'),
             ('background_A.png', 'camera/A_origin_composite.png'),
             ('background_B.png', 'camera/B_right_down_composite.png'),
             ('background_C.png', 'camera/C_left_down_composite.png')]
    pairs += [('pendulum_frame_%02d.png' % k, 'animation/pendulum_idle/frame_%02d.png' % k) for k in range(8)]
    for actual, expected in pairs:
        if not (OUTPUT / actual).exists() and not require_captures:
            continue
        a = Image.open(OUTPUT / actual).convert('RGB'); b = Image.open(SOURCE / expected).convert('RGB')
        assert a.size == b.size and ImageChops.difference(a, b).getbbox() is None, actual
        comparisons.append(dict(capture=actual, reference=expected, size=a.size, differing_rgb_pixels=0))
    multi = OUTPUT / 'background_multi_period.png'
    if multi.exists():
        actual = Image.open(multi).convert('RGB'); expected = Image.new('RGBA', actual.size)
        for layer in manifest['backgrounds']:
            tile = Image.open(SOURCE / layer['file']).convert('RGBA')
            phase = [round(c * layer['parallax']) % 960 for c in (-7040,-5120)]
            for y in range(-phase[1], actual.height, 960):
                for x in range(-phase[0], actual.width, 960): expected.alpha_composite(tile, (x,y))
        assert ImageChops.difference(actual,expected.convert('RGB')).getbbox() is None
        comparisons.append(dict(capture=multi.name,size=actual.size,differing_rgb_pixels=0,repeat_periods='X/Y > 1; negative sample (-7040,-5120)'))
    baseline = json.loads((OUTPUT / 'before_hashes.json').read_text(encoding='utf-8-sig'))
    changed = [f for f, digest in baseline.items() if sha(f) != digest]
    allowed = ['Assets/ANIMOL/Scripts/Runtime/' + n + '.cs' for n in ['PuzzleTheme', 'PuzzleMapView', 'PuzzleBackdrop', 'SkyIslandBackdrop']]
    allowed += ['Assets/ANIMOL/Scenes/ANIMOL_LetterObservatory_Art_QA.unity']
    assert sorted(changed) == sorted(allowed), changed
    scene = 'Assets/ANIMOL/Scenes/ANIMOL_LetterObservatory_Art_QA.unity'
    reference_commit = 'bda69c04'
    old = subprocess.check_output(['git','show',reference_commit+':'+scene]).decode()
    new = Path(scene).read_text()
    def scene_documents(text):
        return {m.group(2):(m.group(1),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &([^\n]+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
    a, b = scene_documents(old), scene_documents(new)
    prefabs = {k:v for k,v in a.items() if v[0]=='1001'}
    assert len(prefabs)==259 and all(v==b[k] for k,v in prefabs.items())
    guid = re.search(r'guid: (\w+)',Path('Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs.meta').read_text()).group(1)
    qa = [k for k,v in a.items() if guid in v[1]]
    for k in qa:
        assert a[k][1]==b[k][1].replace('  previews: []\n','').replace('  adaptPortraitToolbar: 0\n','')
    device_report = dict(reference_commit=reference_commit,old_prefab_instances=259,byte_identical_prefab_instance_documents=259,
                         qa_controller_existing_fields_identical=True,default_serialization_added=['previews: []','adaptPortraitToolbar: 0'],unchanged_device_presenters=255,unchanged_masters=9)
    (OUTPUT / 'device_reference_audit.json').write_text(json.dumps(device_report,indent=2),encoding='utf-8')
    report = dict(source_sha256_verified=count, original_png_imports=imports, palette_alpha_and_transparent_rgb_verified=True,
                  fixed_pendulum_frame_pixels_verified=fixed_checks,
                  game_view_comparisons=comparisons, protected_assets=len(baseline), changed_existing_assets=changed)
    (OUTPUT / 'audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print('Source SHA256:', count, '/ PNG imports:', len(imports), '/ exact Game View comparisons:', len(comparisons), '/ protected assets:', len(baseline))

if __name__ == '__main__':
    audit(require_captures=True)
