"""Verify supplied bytes and make nearest-neighbour review crops; no operational art edits."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw

SOURCE = Path('Tools/ArtSources/ANIMOL_Original_Design_Correction_v2')
OUTPUT = Path('Docs/Validation/OriginalCorrectionV2')
REVIEW = OUTPUT / 'Review'
REVIEW.mkdir(parents=True, exist_ok=True)
manifest = json.loads((SOURCE / 'manifest.json').read_text(encoding='utf-8'))
assets = []
for entry in manifest['assets']:
    path = SOURCE / entry['path']
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    im = Image.open(path).convert('RGBA')
    assert list(im.size) == entry['size'] == [352, 704]
    assert digest == entry['sha256']
    assert all(c[3] in (0, 255) and (not c[3] or '%02x%02x%02x' % c[:3] in manifest['palette']) for c in set(im.getdata()))
    installed = Path('Assets/ANIMOL/UI/OriginalDesignCorrectionV2') / entry['path']
    assert hashlib.sha256(installed.read_bytes()).hexdigest() == digest
    assets.append(dict(id=entry['id'], sha256=digest, size=list(im.size)))
retained = []
config = json.loads((SOURCE / 'ui-config/campaign-style-corrections.json').read_text(encoding='utf-8'))
for entry in config['retainedArt']:
    path = Path('Assets/ANIMOL/UI/ProductionV1/Art') / (entry['resourceKey'] + '.png')
    actual = hashlib.sha256(path.read_bytes()).hexdigest()
    retained.append(dict(path=path.as_posix(), matches=actual == entry['sha256'], actual=actual, expected=entry['sha256']))
for theme in range(1, 6):
    name = f'T{theme:02}'
    native = Image.open(SOURCE / f'previews/{name}_Composite_352x704.png')
    native.save(REVIEW / f'{name}-package-native.png')
    native.resize((1408, 2816), Image.Resampling.NEAREST).save(REVIEW / f'{name}-package-4x.png')
    platform = Image.open(SOURCE / f'runtime/main-background/{name}-platform.png').convert('RGBA')
    old = Image.open(f'Assets/ANIMOL/UI/MainUiV7/Textures/{name}-platform.png').convert('RGBA')
    board = Image.new('RGBA', (1408, 1440), '#1a1c2c')
    draw = ImageDraw.Draw(board)
    for row, (label, im) in enumerate([('existing platform', old), ('correction v2 platform', platform)]):
        draw.text((8, row * 720 + 4), f'{name} {label}: PNG top-left crop (0,470)-(352,640), Point 4x', fill='white')
        board.alpha_composite(im.crop((0, 470, 352, 640)).resize((1408, 680), Image.Resampling.NEAREST), (0, row * 720 + 30))
    board.save(REVIEW / f'{name}-bridge-4x.png')
    assert all(platform.getpixel((x, 526))[3] == 255 for x in range(70, 283))
    actual = OUTPUT / f'Native_{name}_c1_r1.png'
    if actual.exists():
        Image.open(actual).resize((1408, 2816), Image.Resampling.NEAREST).save(REVIEW / f'{name}-unity-4x.png')
for name in ['UI_Campaign_ThemeCard_Frame', 'UI_Common_Panel_Main', 'UI_Common_Button_Secondary'] + [f'Thumb_Theme_T{i:02}' for i in range(1, 6)]:
    im = Image.open(f'Assets/ANIMOL/UI/ProductionV1/Art/{name}.png')
    im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST).save(REVIEW / f'{name}-4x.png')
(OUTPUT / 'PackageVerification.json').write_text(json.dumps(dict(verifiedAssets=assets, retainedArt=retained, floorY526='PASS x70..282, five platforms'), indent=2) + '\n', encoding='utf-8')
print('PASS 20 source/installed hashes, dimensions, binary alpha, Sweetie16; five floor rows. Retained-art mismatches:', sum(not r['matches'] for r in retained))
