"""Independent frozen preview source, selection, claims and metadata audit."""
from pathlib import Path
from PIL import Image, ImageSequence
from collections import Counter
import hashlib
import json

O = Path(__file__).resolve().parent; W = O.parent; R = W / 'preview_exports'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text())
errors = []; checks = []
def test(label, condition):
    checks.append({'check': label, 'status': 'PASS_OFFLINE' if condition else 'FAIL_OFFLINE'})
    if not condition: errors.append(label)
meta = {}; paths = {}; groups = {}; producer_pins = {}
for owner in ['networks_a', 'networks_b', 'groups_a', 'groups_b']:
    p = W / (owner + '_exports') / 'manifest.json'; j = read(p); producer_pins[owner] = sha(p)
    for a in j['assets']: meta[a['file']] = a; paths[a['file']] = p.parent / a['file']
    for g in j.get('groups', []): groups[g['key']] = g
freeze = read(R / 'PREVIEW_FROZEN.json')
for f in freeze['files']:
    p = R / f['file']; test('Frozen file ' + f['file'], p.is_file() and p.stat().st_size == f['bytes'] and sha(p) == f['sha256'])
layout = read(R / 'example_layout.json'); qa = read(R / 'preview_qa.json')
test('Preview is explicit offline-only', layout['previewOnly'] and not layout['actualUnityExecuted'] and not layout['actualHostBindingsVerified'])
test('Current four producer pins', all(qa['producerManifests'][k]['sha256'] == pin for k, pin in producer_pins.items()))
test('Full-canvas dimensions and floor coordinate convention', layout['referenceCanvas'] == [1080, 1920] and layout['referencePixelsPerCell'] == 84 and layout['referenceBottomLeftPixel'] == [120, 1700] and '+Y North' in layout['coordinateConvention'])

def h32(n): return n & 0xffffffff
def phase(seed, layer, height, group, spacing):
    h = 2166136261
    for ch in group: h = h32((h ^ ord(ch)) * 16777619)
    h = h32((h ^ h32(seed)) * 16777619)
    h ^= h32(layer * 0xc2b2ae35); h ^= h32(height * 0x27d4eb2f)
    h ^= h >> 16; h = h32(h * 0x7feb352d); h ^= h >> 15; h = h32(h * 0x846ca68b); h ^= h >> 16
    return h32(h) % spacing
def cardinal(cells, x, y):
    return sum(bit for dx, dy, bit in [(0, 1, 1), (1, 0, 2), (0, -1, 4), (-1, 0, 8)] if (x + dx, y + dy) in cells)
def choose(cells, family, group, layer=0, height=0):
    rows = []
    for x, y in sorted(cells, key=lambda xy: (xy[1], xy[0])):
        m = cardinal(cells, x, y); rows.append((f'decor/connected/{family}/n{m:02d}.png', m, 'network'))
        if family in ['velvet_aisle_barrier', 'carved_stage_balustrade'] and m in [5, 10]:
            dx, dy = (0, 1) if m == 5 else (1, 0)
            coordinate = y if m == 5 else x
            if (coordinate - phase(layout['seed'], layer, height, group, layout['postSpacing'])) % layout['postSpacing'] == 0 and all(cardinal(cells, x + s * dx, y + s * dy) == m for s in [-1, 1]):
                rows.append((f'decor/connected/{family}/periodic_post.png', m, 'periodic_post'))
    return rows

floor = {tuple(c) for c in layout['hostAllowedDecorCells']}; claimed = set(); network_by_identity = {}
for row in layout['networkCells']:
    key = (row['family'], row['connectionGroup'], row['logicalLayer'], row['heightKey'])
    network_by_identity.setdefault(key, set()).add((row['x'], row['y']))
    claim = (row['logicalLayer'], row['heightKey'], row['x'], row['y'])
    test('Unique approved network claim ' + str(claim), claim not in claimed and (row['x'], row['y']) in floor); claimed.add(claim)
expected_networks = Counter()
for key, cells in network_by_identity.items():
    expected_networks.update(choose(cells, *key))
actual_networks = Counter((p['file'], p['mask'], p['kind']) for p in layout['newDecorRenderPlan'] if p['kind'] in ['network', 'periodic_post'])
test('Scene masks and optional posts match independent cardinal/C#hash selection', expected_networks == actual_networks)
ids = set()
for row in layout['groupPlacements']:
    pid, key = row['placementId'], row['groupKey']; g = groups[key]; w, h = g['footprintCells']
    test('Unique whole placement identity ' + pid, pid not in ids); ids.add(pid)
    cells = {(row['anchorX'] + dx, row['anchorY'] + dy) for dx in range(w) for dy in range(h)}
    keys = {(row['logicalLayer'], row['heightKey'], x, y) for x, y in cells}
    test('Whole footprint approved atomically ' + pid, cells <= floor and not keys & claimed); claimed |= keys
    plans = [p for p in layout['newDecorRenderPlan'] if p['kind'] in ['group', 'point'] and p['row']['placementId'] == pid]
    test('One visual with exact footprint/center ' + pid, len(plans) == 1 and plans[0]['file'] == g['file'] and plans[0]['worldCenter'] == [row['anchorX'] + w / 2, row['anchorY'] + h / 2] and set(map(tuple, plans[0]['claimedCells'])) == cells)
for p in layout['newDecorRenderPlan']:
    a = meta[p['file']]; x, y, w, h = a['logicalRect']
    test('Registered pivot ' + p['file'], a['ppu'] == 256 and abs(a['pivot'][0] - (x + w / 2) / a['width']) < 1e-6 and abs(a['pivot'][1] - (1 - (y + h / 2) / a['height'])) < 1e-6)
    if p['kind'] in ['network', 'periodic_post']:
        row = p['row'];test('Network center ' + p['file'] + str(p['worldCenter']), p['worldCenter'] == [row['x'] + .5, row['y'] + .5])
for row in qa['newRuntimeSourcesUsed']:
    a = meta.get(row['file']);test('Current native PNG/metadata source ' + row['file'], a is not None and sha(paths[row['file']]) == row['sha256'] == a['sha256'] and all(row[k] == a[k] for k in ['ppu', 'pivot', 'logicalRect', 'footprintCells']))
base = W / 'reference/ANIMOL_ECHO_THEATER_CARTOON_V03'
for row in qa['baseContextSourcesUsed']:
    test('Read-only base context ' + row['file'], (base / row['file']).is_file() and sha(base / row['file']) == row['sha256'] and row['file'] not in meta)
catalog = read(R / 'catalog_layout.json'); keys = [r['key'] for r in catalog['records']]
test('26 semantic catalog designs, excluding variants/posts', len(keys) == len(set(keys)) == 26 and set(keys) == {a['family'] for a in meta.values()} and not catalog['variantsAndHelperPostsCountedAsFamilies'])
fixtures = read(R / 'connection_fixtures.json')['fixtures']; selected_checks = 0
for f in fixtures:
    expected = choose(set(map(tuple, f['cells'])), f['family'], 'fixture_' + f['family'])
    actual = [(p['file'], p['mask'], p['kind']) for p in f['selected']]
    test('Fixture independent selection ' + f['family'] + '/' + f['case'], expected == actual);selected_checks += len(actual)
test('Sixty topology fixture cases', len(fixtures) == 60 and len({(f['family'], f['case']) for f in fixtures}) == 60)
output_rows = []
for row in qa['outputs']:
    p = R / row['file']; im = Image.open(p); im.load()
    test('Output PNG/GIF byte hash ' + row['file'], sha(p) == row['sha256'] and p.stat().st_size == row['bytes'])
    output_rows.append({'file': row['file'], 'size': list(im.size), 'sha256': sha(p)})
gif = Image.open(R / 'THEATER_CONNECTED_DECOR_EDIT_REPLAY.gif'); gif_hashes = []; duration = 0
for frame in ImageSequence.Iterator(gif):
    gif_hashes.append(hashlib.sha256(frame.convert('RGBA').tobytes()).hexdigest());duration += frame.info.get('duration', 0)
test('Eight distinct authoring-replay frames/5200ms', len(gif_hashes) == len(set(gif_hashes)) == 8 and duration == 5200)
code = (R / 'build_previews.py').read_text()
test('Compositor uses PPU scaling and normalized ground pivot, preserving full canvas', 'im.width*ppw/ppu*scale' in code and 'px-pivot[0]*im.width' in code and 'py-(1-pivot[1])*im.height' in code and 'getbbox(' not in code)
test('Actual Unity remains NOT_RUN', all(qa[k] == 'NOT_RUN' for k in ['actualUnityImport', 'actualCSharpCompile', 'actualPrefabBinding', 'actualPlayMode']))
report = {'status': 'PASS_OFFLINE' if not errors else 'FAIL_OFFLINE', 'previewFrozenSha256': sha(R / 'PREVIEW_FROZEN.json'),
          'exampleLayoutSha256': sha(R / 'example_layout.json'), 'previewQASha256': sha(R / 'preview_qa.json'),
          'producerManifestPins': producer_pins, 'checkCount': len(checks), 'checks': checks, 'errors': errors,
          'scene': {'floorCells': len(floor), 'networkCells': len(layout['networkCells']), 'wholeGroupPointPlacements': len(ids),
                    'claimedCells': len(claimed), 'samePlaneClaimOverlap': 0 if not any('claim' in e.lower() or 'footprint' in e.lower() for e in errors) else 'REVIEW',
                    'newSourcePNGs': len(qa['newRuntimeSourcesUsed']), 'readOnlyBaseContextPNGs': len(qa['baseContextSourcesUsed'])},
          'topologyFixtures': len(fixtures), 'independentMaskAndPostSelections': selected_checks,
          'gif': {'frames': len(gif_hashes), 'distinctFrames': len(set(gif_hashes)), 'durationMs': duration, 'meaning': 'Tile add/remove static sprite reselection. No gameplay or device motion demonstrated.'},
          'outputs': output_rows, 'manualVisualReview': 'Main1080x1920preview reviewed: coherent theater palette, readable whole props, visible connected rails/curtain/shelves/lights/ivy, no obvious clipping. Catalog/footprint grids reviewed separately by producer and metadata geometry is independently checked here.',
          'scope': 'Independent selection arithmetic, current file hashes, fullcanvas PPU/pivot formula source review, footprint claims and image/GIF decoding only. Not pixel-for-pixel reimplementation of compositor, not actual Unity/GPU.',
          'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN'}
(O / 'preview_independent_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checks), 'scene': report['scene'], 'fixtureSelections': selected_checks, 'gif': report['gif'], 'errors': errors}))
