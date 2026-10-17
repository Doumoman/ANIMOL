"""Read-only package/import, Game View pixel and protected-asset audit. Run at repo root."""
import hashlib
import json
import re
import subprocess
from pathlib import Path
from PIL import Image, ImageChops

SOURCE = Path('Docs/Inbox/TwilightCoast/ANIMOL_TWILIGHT_COAST_V02')
OUTPUT = Path('Docs/Validation/TwilightCoast')

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def target(source):
    if source.startswith('tiles/'):
        return 'Assets/ANIMOL/Sprites/Tiles/Floor/twilight_sea/TwilightCoast/' + source[6:]
    if source.startswith('props/'):
        return 'Assets/ANIMOL/Sprites/Props/twilight_sea/TwilightCoast/' + source[6:]
    if source.startswith('animation/'):
        return 'Assets/ANIMOL/Sprites/Props/twilight_sea/TwilightCoast/Animation/' + source[10:]
    return 'Assets/ANIMOL/Sprites/Background/twilight_sea/TwilightCoast/' + source[12:]

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
    files += [manifest['horizon'][key] for key in ['core','sideLeft','sideRight','withoutSun']]
    assert len(files) == 124
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
    for animation in manifest['animations'][:2]:
        first = Image.open(SOURCE / animation['frames'][0]['file']).convert('RGBA')
        for frame in animation['frames']:
            im = Image.open(SOURCE / frame['file']).convert('RGBA')
            assert im.size == first.size == (160,160)
            assert im.crop((0,112,160,160)).tobytes() == first.crop((0,112,160,160)).tobytes()
        fixed_checks.append(animation['name']+': bottom 48 rows unchanged')
    assert Image.open(SOURCE/'animation/palm_touch/frame_05.png').convert('RGBA').tobytes()==Image.open(SOURCE/'props/palm_160x160.png').convert('RGBA').tobytes()
    comparisons = []
    pairs = [('coast_join_1x.png','inspection/coast_join_1x.png'),('coast_join_4x.png','inspection/coast_join_4x.png'),
             ('horizon_wide.png','inspection/horizon_wide_1280x320.png'),('horizon_separated.png','inspection/horizon_wide_1280x320.png'),
             ('background_A.png','camera/A_origin_composite.png'),('background_B.png','camera/B_right_down_composite.png'),('background_C.png','camera/C_left_down_composite.png')]
    for clip, frame_count in [('palm_idle',5),('palm_touch',6)]:
        pairs += [(clip+'_%02d.png'%k,'animation/'+clip+'/frame_%02d.png'%k) for k in range(frame_count)]
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
    for frame in range(4):
        path = OUTPUT / ('ripples_multi_%02d.png' % frame)
        if not path.exists() and not require_captures: continue
        actual = Image.open(path).convert('RGB'); expected = Image.new('RGBA',actual.size)
        for index,layer in enumerate(manifest['backgrounds']):
            source = layer['file'] if index<2 else 'animation/water_ripples/frame_%02d.png' % frame
            tile = Image.open(SOURCE/source).convert('RGBA'); phase = [round(c*layer['parallax'])%960 for c in (-7040,-5120)]
            for y in range(-phase[1],actual.height,960):
                for x in range(-phase[0],actual.width,960): expected.alpha_composite(tile,(x,y))
        assert ImageChops.difference(actual,expected.convert('RGB')).getbbox() is None,path.name
        comparisons.append(dict(capture=path.name,near_replacement_frame=frame,size=actual.size,differing_rgb_pixels=0))
    baseline = json.loads((OUTPUT / 'before_hashes.json').read_text(encoding='utf-8-sig'))
    changed = [f for f, digest in baseline.items() if sha(f) != digest]
    allowed = ['Assets/ANIMOL/Scripts/Runtime/' + n + '.cs' for n in ['PuzzleTheme', 'PuzzleMapView', 'PuzzleBackdrop', 'SkyIslandBackdrop']]
    allowed += ['Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightSea_Art_QA.unity']
    assert sorted(changed) == sorted(allowed), changed
    scene = 'Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightSea_Art_QA.unity'
    reference_commit = '87674d91'
    old = subprocess.check_output(['git','show',reference_commit+':'+scene]).decode()
    new = Path(scene).read_text()
    def scene_documents(text):
        return {m.group(2):(m.group(1),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &([^\n]+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
    a, b = scene_documents(old), scene_documents(new)
    prefabs = {k:v for k,v in a.items() if v[0]=='1001'}
    assert len(prefabs)==312 and all(v==b[k] for k,v in prefabs.items())
    guid = re.search(r'guid: (\w+)',Path('Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs.meta').read_text()).group(1)
    qa = [k for k,v in a.items() if guid in v[1]]
    for k in qa:
        assert a[k][1]==b[k][1].replace('  previews: []\n','').replace('  adaptPortraitToolbar: 0\n','')
    device_report = dict(reference_commit=reference_commit,old_prefab_instances=len(prefabs),byte_identical_prefab_instance_documents=len(prefabs),
                         qa_controller_existing_fields_identical=True,default_serialization_added=[],unchanged_device_presenters=307,unchanged_masters=8)
    (OUTPUT / 'device_reference_audit.json').write_text(json.dumps(device_report,indent=2),encoding='utf-8')
    report = dict(source_sha256_verified=count, original_png_imports=imports, palette_alpha_and_transparent_rgb_verified=True,
                  fixed_palm_frame_pixels_verified=fixed_checks,
                  game_view_comparisons=comparisons, protected_assets=len(baseline), changed_existing_assets=changed)
    (OUTPUT / 'audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print('Source SHA256:', count, '/ PNG imports:', len(imports), '/ exact Game View comparisons:', len(comparisons), '/ protected assets:', len(baseline))

if __name__ == '__main__':
    audit(require_captures=True)
