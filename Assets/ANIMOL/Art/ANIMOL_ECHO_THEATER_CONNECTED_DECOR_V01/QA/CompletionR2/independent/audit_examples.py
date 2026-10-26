"""Independent typed example JSON and actual art-metadata claim validation.

No Unity JsonUtility/C# code is executed. Checks root-produced sample bytes only.
"""
from pathlib import Path
from collections import Counter
import hashlib
import json
import math
import struct

O = Path(__file__).resolve().parent
C = O.parent
W = C.parent
ROOT = Path('/workspace/scratch/b0cb6eab55f7')
PACKAGE = ROOT / 'output/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
EXAMPLES = C / 'examples_exports/Examples'
EXPECTED_PIN = '8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069'
ROOT_FIELDS = {'schemaVersion', 'packageId', 'sourceManifestSha256', 'boardId', 'revision', 'isWholeBoard', 'isExampleOnly',
               'originX', 'originY', 'originZ', 'cellSize', 'seed', 'postSpacing', 'networkCells', 'groupPlacements', 'allowedDecorCells'}
NETWORK_FIELDS = {'x', 'y', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ', 'family', 'connectionGroup'}
GROUP_FIELDS = {'placementId', 'groupKey', 'anchorX', 'anchorY', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ'}
ALLOWED_FIELDS = {'x', 'y', 'logicalLayer', 'heightKey'}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def pairs(rows):
    out = {}
    for key, value in rows:
        if key in out: raise ValueError('Duplicate JSON key ' + key)
        out[key] = value
    return out
def read(p):
    return json.loads(p.read_text(), object_pairs_hook=pairs, parse_constant=lambda s: (_ for _ in ()).throw(ValueError('Non-JSON number ' + s)))
def i32(v): return type(v) is int and -(2 ** 31) <= v < 2 ** 31
def i64(v): return type(v) is int and -(2 ** 63) <= v < 2 ** 63
def f32(v):
    if type(v) not in [int, float] or not math.isfinite(v): return False
    try: return math.isfinite(struct.unpack('f', struct.pack('f', v))[0])
    except (OverflowError, struct.error): return False
def f32_value(v): return struct.unpack('f', struct.pack('f', v))[0]
def exact_fields(row, fields): return type(row) is dict and set(row) == fields
def same_fields(row, old, fields): return {k: row[k] for k in fields} == {k: old[k] for k in fields}

manifest = read(PACKAGE / 'manifest.json')
assert sha(PACKAGE / 'manifest.json') == EXPECTED_PIN
assets = {a['file']: a for a in manifest['assets']}
groups = {g['key']: g for g in manifest['groups']}
networks = {a['family'] for a in manifest['assets'] if a['kind'] == 'connected_tile'}
old_example = read(W / 'preview_exports/example_layout.json')
reports = []
for filename in ['stage_snapshot.json', 'all26_snapshot.json']:
    path = EXAMPLES / filename
    j = read(path); errors = []
    def test(label, condition):
        if not condition: errors.append(label)
    test('Exact flat required root fields', exact_fields(j, ROOT_FIELDS))
    test('Explicit whole-board example identity', j['schemaVersion'] == 1 and type(j['schemaVersion']) is int and j['packageId'] == manifest['packageId'] and j['sourceManifestSha256'] == EXPECTED_PIN and j['isWholeBoard'] is True and j['isExampleOnly'] is True)
    test('Board identity and Int64 revision', isinstance(j['boardId'], str) and bool(j['boardId'].strip()) and i64(j['revision']))
    test('Finite float32 origin and positive nonunderflow cellSize', all(f32(j[k]) for k in ['originX', 'originY', 'originZ', 'cellSize']) and f32_value(j['cellSize']) > 0)
    test('Int32 seed and exact post spacing', i32(j['seed']) and type(j['postSpacing']) is int and j['postSpacing'] in [3, 4])
    test('Required explicit arrays', all(type(j[k]) is list for k in ['networkCells', 'groupPlacements', 'allowedDecorCells']))
    allowed = set()
    for row in j['allowedDecorCells']:
        test('Exact allowed-cell fields/types', exact_fields(row, ALLOWED_FIELDS) and all(i32(row[k]) for k in ALLOWED_FIELDS))
        claim = (row['logicalLayer'], row['heightKey'], row['x'], row['y'])
        test('No duplicate whitelist cell', claim not in allowed); allowed.add(claim)
    occupied = set(); counts = Counter(); ids = set()
    for row in j['networkCells']:
        test('Exact network row fields/types', exact_fields(row, NETWORK_FIELDS) and all(i32(row[k]) for k in ['x', 'y', 'logicalLayer', 'heightKey', 'sortingOrder']) and f32(row['worldZ']))
        test('Exact network family and connection ID', row['family'] in networks and isinstance(row['connectionGroup'], str) and bool(row['connectionGroup'].strip()))
        test('Network order/domain', -32768 <= row['sortingOrder'] <= 32766 and row['x'] not in [-2 ** 31, 2 ** 31 - 1] and row['y'] not in [-2 ** 31, 2 ** 31 - 1])
        claim = (row['logicalLayer'], row['heightKey'], row['x'], row['y'])
        test('Approved unique network claim', claim in allowed and claim not in occupied); occupied.add(claim);counts[row['family']] += 1
    for row in j['groupPlacements']:
        test('Exact group row fields/types', exact_fields(row, GROUP_FIELDS) and all(i32(row[k]) for k in ['anchorX', 'anchorY', 'logicalLayer', 'heightKey', 'sortingOrder']) and f32(row['worldZ']))
        test('Exact group and placement identity', row['groupKey'] in groups and isinstance(row['placementId'], str) and bool(row['placementId'].strip()) and row['placementId'] not in ids); ids.add(row['placementId'])
        test('Group order/domain', -32768 <= row['sortingOrder'] <= 32767)
        group = groups[row['groupKey']]; art = assets[group['file']]; w, h = group['footprintCells']
        test('Actual art group semantics/footprint', art['family'] == row['groupKey'] and art['footprintCells'] == [w, h])
        for dy in range(h):
            for dx in range(w):
                x, y = row['anchorX'] + dx, row['anchorY'] + dy
                claim = (row['logicalLayer'], row['heightKey'], x, y)
                test('Whole group claim is Int32 approved and unique', i32(x) and i32(y) and claim in allowed and claim not in occupied); occupied.add(claim)
        counts[row['groupKey']] += 1
    if filename == 'stage_snapshot.json':
        test('Original stage network rows preserved byte-value projection', j['networkCells'] == old_example['networkCells'])
        test('Original stage group rows preserved byte-value projection', j['groupPlacements'] == old_example['groupPlacements'])
        test('Original +Y origin/cellsize preserved', [j['originX'], j['originY'], j['originZ']] == old_example['worldOrigin'] and j['cellSize'] == old_example['cellSize'])
        test('Original allowed floor projection is not flipped or shifted', {(r['x'], r['y']) for r in j['allowedDecorCells']} == set(map(tuple, old_example['hostAllowedDecorCells'])) and all(r['logicalLayer'] == r['heightKey'] == 0 for r in j['allowedDecorCells']))
        test('Expected stage counts', len(j['networkCells']) == 48 and len(j['groupPlacements']) == 16 and len(occupied) == 88 and len(counts) == 21)
    else:
        test('All26 actual semantic categories, no variant-count inflation', set(counts) == networks | set(groups) and len(counts) == 26)
        test('All20 whole/point keys once', len(j['groupPlacements']) == 20 and {r['groupKey'] for r in j['groupPlacements']} == set(groups))
        test('Expected all26 counts', len(j['networkCells']) == 78 and len(occupied) == 123)
    reports.append({'file': filename, 'sha256': sha(path), 'status': 'PASS_DATA_OFFLINE' if not errors else 'FAIL_DATA_OFFLINE', 'errors': errors,
                    'networkCells': len(j['networkCells']), 'wholeGroupPointPlacements': len(j['groupPlacements']), 'allowedCells': len(allowed), 'claimedCells': len(occupied),
                    'semanticKinds': len(counts), 'kindPlacementCounts': dict(counts), 'wholeClaimsApproved': not errors,
                    'claimPointNotes': '+Y North and bottom-left anchors; equality to previous stage proves no row sign/one-cell shift.'})
result = {'status': 'PASS_DATA_OFFLINE' if all(r['status'] == 'PASS_DATA_OFFLINE' for r in reports) else 'FAIL_DATA_OFFLINE',
          'sourceManifestSha256': EXPECTED_PIN, 'examples': reports,
          'scope': 'Independent Python strict JSON shape/type/numeric-range and actual art-metadata footprint/whitelist/identity validation. Does not execute supplied C# parser, Unity serialization or binding.',
          'actualCSharpParserExecution': 'NOT_RUN', 'actualUnitySerialization': 'NOT_RUN', 'actualScenePlay': 'NOT_RUN'}
(O / 'examples_independent_qa.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': result['status'], 'examples': [{k: r[k] for k in ['file', 'status', 'networkCells', 'wholeGroupPointPlacements', 'allowedCells', 'claimedCells', 'semanticKinds', 'errors']} for r in reports]}))
