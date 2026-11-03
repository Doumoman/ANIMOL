"""Independent same-typed-board/source/pivot/claim validation of the offline R2 preview."""
from pathlib import Path
from PIL import Image
import json
import hashlib

O = Path(__file__).resolve().parent; C = O.parent; W = C.parent
R = C / 'preview_exports'; P = Path('/workspace/scratch/b0cb6eab55f7/output/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01')
def read(p): return json.loads(p.read_text())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
checks = []; errors = []
def test(name, value):
    checks.append({'check': name, 'status': 'PASS_REFERENCE_OFFLINE' if value else 'FAIL_REFERENCE_OFFLINE'})
    if not value: errors.append(name)
m = read(P / 'manifest.json'); a = {x['file']: x for x in m['assets']}; groups = {x['key']: x for x in m['groups']}
sfile = C / 'examples_exports/Examples/all26_snapshot.json'; s = read(sfile)
j = read(R / 'all26_preview_layout.json'); q = read(R / 'all26_preview_qa.json'); freeze = read(R / 'ALL26_PREVIEW_FROZEN.json')
test('Exact unchanged art manifest and same typed example bytes', sha(P / 'manifest.json') == s['sourceManifestSha256'] == j['sourceManifestSha256'] == q['manifestSha256'] == '8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069' and sha(sfile) == j['snapshotSha256'] == q['snapshotSha256'])
for r in freeze['files']:
    p = R / r['file']; test('Frozen preview file ' + r['file'], p.is_file() and sha(p) == r['sha256'] and p.stat().st_size == r['bytes'])
rows = {(r['x'], r['y'], r['logicalLayer'], r['heightKey']): r for r in s['networkCells']}
def mask(row):
    bits = 0
    for dx, dy, bit in [(0, 1, 1), (1, 0, 2), (0, -1, 4), (-1, 0, 8)]:
        other = rows.get((row['x'] + dx, row['y'] + dy, row['logicalLayer'], row['heightKey']))
        if other and other['family'] == row['family'] and other['connectionGroup'] == row['connectionGroup']: bits |= bit
    return bits
allowed = {(r['x'], r['y'], r['logicalLayer'], r['heightKey']) for r in s['allowedDecorCells']}; claimed = set()
netplan = [r for r in j['renderPlan'] if r['kind'] == 'network']; groupplan = [r for r in j['renderPlan'] if r['kind'] in ['group', 'point']]
test('All78 actual network cells are represented once', len(netplan) == len(rows) == 78 and {tuple(r['claimedCells'][0]) for r in netplan} == set(rows))
for r in netplan:
    row = r['row']; key = (row['x'], row['y'], row['logicalLayer'], row['heightKey']); n = mask(row)
    test('Native mask and center ' + str(key), rows.get(key) == row and r['mask'] == n and r['file'] == f"decor/connected/{row['family']}/n{n:02d}.png" and r['worldCenter'] == [row['x'] + .5, row['y'] + .5])
    test('Unique approved network claim ' + str(key), key in allowed and key not in claimed); claimed.add(key)
for row in s['groupPlacements']:
    r = [p for p in groupplan if p['row']['placementId'] == row['placementId']]
    g = groups[row['groupKey']]; art = a[g['file']]; w, h = g['footprintCells']
    cells = {(row['anchorX'] + dx, row['anchorY'] + dy, row['logicalLayer'], row['heightKey']) for dx in range(w) for dy in range(h)}
    test('One complete prop with correct center ' + row['groupKey'], len(r) == 1 and r[0]['row'] == row and r[0]['file'] == g['file'] and r[0]['worldCenter'] == [row['anchorX'] + w / 2, row['anchorY'] + h / 2] and set(map(tuple, r[0]['claimedCells'])) == cells)
    test('Approved nonoverlapping whole prop ' + row['groupKey'], cells <= allowed and not cells & claimed); claimed |= cells
test('All20 whole/point rows and123 approved claims', len(groupplan) == 20 and len(claimed) == 123)
test('All26 unique labels match actual semantic designs', len(j['labels']) == 26 and {r['family'] for r in j['labels']} == {r['family'] for r in a.values()} and {r['number'] for r in j['labels']} == set(range(1, 27)))
# A post requires an interior straight cell with both same-mask straight neighbors.
post_eligible = 0
for row in rows.values():
    n = mask(row)
    if row['family'] not in ['velvet_aisle_barrier', 'carved_stage_balustrade'] or n not in [5, 10]: continue
    dx, dy = (0, 1) if n == 5 else (1, 0)
    neighbors = [rows.get((row['x'] + sign * dx, row['y'] + sign * dy, row['logicalLayer'], row['heightKey'])) for sign in [-1, 1]]
    if all(other and other['family'] == row['family'] and other['connectionGroup'] == row['connectionGroup'] and mask(other) == n for other in neighbors): post_eligible += 1
test('Zero eligible periodic-post interiors explains no posts', post_eligible == 0 and j['periodicPostCount'] == q['periodicPosts'] == 0)
for ref in q['referencedRuntimePngs']:
    art = a[ref['file']]
    test('Exact source bytes and ground metadata ' + ref['file'], sha(P / ref['file']) == ref['sha256'] == art['sha256'] and all(ref[k] == art[k] for k in ['ppu', 'pivot', 'logicalRect', 'footprintCells']))
for ref in q['baseContextSources']:
    p = W / 'reference/ANIMOL_ECHO_THEATER_CARTOON_V03' / ref['file']
    test('Read-only base context ' + ref['file'], sha(p) == ref['sha256'] and ref['file'] not in a)
for name in ['ALL26_BOARD.png', 'ALL26_BOARD_GRID.png']:
    im = Image.open(R / name); im.load();test('Decoded canvas ' + name, list(im.size) == [1600, 2760])
code = (R / 'build_all26_board.py').read_text()
test('Fixed cell scale and source pivot, no alpha-fit art substitution', 'im.width*PPW/ppu' in code and 'px-pivot[0]*im.width' in code and 'py-(1-pivot[1])*im.height' in code and 'getbbox(' not in code)
test('Offline reference flags remain explicit', not j['actualUnityExecuted'] and all(q[k] == 'NOT_RUN' for k in ['actualUnityImport', 'actualCSharpCompile', 'actualUnityPlay', 'actualPrefabBinding']))
report = {'status': 'PASS_REFERENCE_OFFLINE' if not errors else 'FAIL_REFERENCE_OFFLINE', 'checkCount': len(checks), 'checks': checks, 'errors': errors,
          'snapshotSha256': sha(sfile), 'manifestSha256': sha(P / 'manifest.json'), 'previewFrozenSha256': sha(R / 'ALL26_PREVIEW_FROZEN.json'),
          'networkCells': 78, 'wholeGroupPointPlacements': 20, 'claimedCells': len(claimed), 'semanticKinds': 26, 'runtimeSourcesReferenced': len(q['referencedRuntimePngs']),
          'manualVisualReview': 'All26normal preview directly viewed: each labelled design visible, correct contiguous network silhouettes and square two-row pile retained. Technical overlays only; no new production art.',
          'scope': 'Source-byte/metadata/coordinate/mask/claim arithmetic and PNG decode/manual reference review only. No Unity/GPU execution.',
          'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualPrefabScenePlay': 'NOT_RUN'}
(O / 'all26_preview_independent_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checks), 'claims': len(claimed), 'sourcePngs': report['runtimeSourcesReferenced'], 'errors': errors}))
