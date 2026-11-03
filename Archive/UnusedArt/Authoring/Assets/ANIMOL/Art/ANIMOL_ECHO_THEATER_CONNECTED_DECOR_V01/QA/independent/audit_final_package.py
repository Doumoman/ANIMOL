"""Read-only final assembled add-on vs frozen producer bytes. No Unity execution."""
from pathlib import Path
import argparse
import hashlib
import json

HERE = Path(__file__).resolve().parent
WORK = HERE.parent
PACKAGE_ID = 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
EXPECTED_GROUPS = {'grand_piano_3x2': [3, 2], 'audience_bench_3x1': [3, 1], 'ticket_booth_2x2': [2, 2],
                   'costume_rack_3x1': [3, 1], 'vanity_dressing_2x1': [2, 1], 'phonograph_table_2x1': [2, 1],
                   'scenery_flat_3x1': [3, 1], 'stacked_props_2x2': [2, 2], 'mask_display_2x1': [2, 1],
                   'puppet_storage_2x2': [2, 2], 'backstage_spotlight_duo_2x1': [2, 1], 'stage_steps_2x1': [2, 1],
                   'ticket_stand': [1, 1], 'folded_chair': [1, 1], 'music_stand': [1, 1], 'tall_candelabra': [1, 1],
                   'mask_pedestal': [1, 1], 'script_pile': [1, 1], 'sandbag_stack': [1, 1], 'flower_urn': [1, 1]}
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text())
ap = argparse.ArgumentParser(); ap.add_argument('--package', required=True); args = ap.parse_args()
root = Path(args.package).resolve(); errors = []; pins = {}; expected = {}
for owner in ['networks_a', 'networks_b', 'groups_a', 'groups_b']:
    producer = WORK / (owner + '_exports'); mp = producer / 'manifest.json'
    if not mp.is_file():
        options = list(producer.glob('*manifest.json'))
        if len(options) == 1: mp = options[0]
    if not mp.is_file(): raise SystemExit('Producer missing: ' + owner)
    pins[owner] = sha(mp)
    for a in load(mp)['assets']:
        assert a['file'] not in expected, 'Producer path collision'
        expected[a['file']] = (a, producer / a['file'])
j = load(root / 'manifest.json')
if j.get('schemaVersion') != 1 or j.get('packageId') != PACKAGE_ID: errors.append('Wrong merged package/schema identity')
if sha(root / 'manifest.json') != (root / 'manifest_sha256.txt').read_text().strip(): errors.append('Merged manifest byte pin differs')
actual = {a['file']: a for a in j['assets']}
if len(actual) != len(j['assets']): errors.append('Duplicate merged asset paths')
if len({f.lower() for f in actual}) != len(actual): errors.append('Case-colliding paths')
if set(actual) != set(expected): errors.append('Canonical runtime path union differs from producers')
core_keys = ['file', 'width', 'height', 'ppu', 'pivot', 'logicalRect', 'footprintCells', 'family', 'kind', 'sha256']
for name, a in actual.items():
    p = root / name
    if not p.is_file(): errors.append('Missing canonical PNG ' + name); continue
    if sha(p) != a.get('sha256'): errors.append('Canonical asset SHA ' + name)
    if name in expected:
        row, source = expected[name]
        if sha(p) != sha(source): errors.append('Canonical/producers bytes differ ' + name)
        if any(a.get(k) != row.get(k) for k in core_keys): errors.append('Canonical metadata differs ' + name)
groups = {g['key']: g for g in j['groups']}
if len(groups) != len(j['groups']) or set(groups) != set(EXPECTED_GROUPS): errors.append('Whole group/point union differs')
for key, g in groups.items():
    if g.get('footprintCells') != EXPECTED_GROUPS.get(key): errors.append('Group dimensions ' + key)
    a = actual.get(g['file'])
    if not a or a['family'] != key or a['footprintCells'] != g['footprintCells']: errors.append('Group semantic/file/footprint correspondence ' + key)
for path in (WORK / 'runtime_exports').rglob('*.cs'):
    target = root / path.relative_to(WORK / 'runtime_exports')
    if not target.is_file() or sha(target) != sha(path): errors.append('Canonical C# differs ' + str(target.relative_to(root)))
base = WORK / 'reference/ANIMOL_ECHO_THEATER_CARTOON_V03'
old = load(base / 'manifest.json')
old_hashes = {a.get('sha256') for a in old.get('assets', []) if a.get('sha256')}
reuse = [f for f, a in actual.items() if a['sha256'] in old_hashes]
report = {'status': 'PASS_OFFLINE' if not errors else 'FAIL_OFFLINE', 'packageId': PACKAGE_ID,
          'manifestSha256': sha(root / 'manifest.json'), 'producerManifestPins': pins,
          'runtimeAssets': len(actual), 'wholeGroupPointRecords': len(groups), 'errors': errors,
          'exactByteRuntimeReuseFromV03': reuse,
          'scope': 'Exact canonical/producer runtime PNG metadata+file-byte union, manifest byte pin and copied C# identity only. Does not execute C# or Unity.',
          'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN'}
(HERE / 'canonical_package_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'assets': len(actual), 'groups': len(groups), 'errors': errors, 'oldByteReuses': len(reuse)}))
