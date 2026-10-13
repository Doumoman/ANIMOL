"""Package complete v6 output only after current checks; verify archive before atomic rename."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--baseline-v5', default=str(root.parent / 'ANIMOL_FreeShape_Sprites_hole_finish_v5'))
args = parser.parse_args()
output = Path(args.output).resolve()
baseline = Path(args.baseline_v5).resolve()
manifest_path = root / 'PACKAGE_MANIFEST.json'
read = lambda rel: json.loads((root / rel).read_text())
sha = lambda content: hashlib.sha256(content).hexdigest()
lookup = read('Data/sprite_lookup.json')
assert lookup['artVersion'] == 6 and lookup['schemaVersion'] == 1
assert lookup['contractId'] == 'ANIMOL_FREE_SHAPE_BLOB47_V1'
assert len(lookup['styles']) == 20
assert all(len(s['variants']) == 4 and all(len(v['cells']) == 47 for v in s['variants']) for s in lookup['styles'])
qa = read('Docs/theme_finish_validation.json')
assert qa['pass'] is True and qa['artVersion'] == 6 and qa['failureCount'] == 0
assert qa['checks']['lockedT01A']['byteMismatches'] == 0
assert qa['checks']['approvedSourceAndMotifs']['byteMismatches'] == 0
assert qa['checks']['ports']['unexpectedInnerFinPixels'] == 0
assert len(qa['checks']['revisedStyles']) == 19
for style in qa['checks']['revisedStyles']:
    current = root / 'SourceArt/NativeV6' / style['styleId'] / 'Material64.png'
    assert sha(current.read_bytes()) == style['materialSha256'], f"Stale native QA: {style['styleId']}"
for file, field in [('Docs/sprite_file_validation.json', 'status'), ('Docs/topology_validation.json', 'result'), ('Docs/gallery_validation.json', 'result')]:
    assert read(file)[field] == 'PASS', f'Current validation failed: {file}'
assert read('Docs/gallery_build.json')['artVersion'] == 6
assert read('Docs/theme_finish_comparison_build.json')['artVersion'] == 6
assert read('Docs/theme_finish_comparison_build.json')['result'] == 'PASS'
for rel in ['README.md', 'Docs/ANIMOL_THEME_FINISH_V6_APPLY.txt', 'Docs/THEME_FINISH_V6_EXPORT_QA_KO.md', 'Previews/ANIMOL_theme_finish_v6_overview.png']:
    assert (root / rel).is_file(), f'Missing deliverable: {rel}'
for theme in range(1, 6):
    assert (root / f'Previews/T{theme:02d}_theme_finish_v6_comparison.png').is_file()

# Byte checks use actual output paths, rather than trusting style-name metadata.
preservation = read('Data/v5_preservation_manifest.json')
assert preservation['schemaVersion'] == 1 and preservation['provenance']['sourceArtVersion'] == 5
has_baseline = (baseline / 'Data/sprite_lookup.json').is_file()
old_lookup = json.loads((baseline / 'Data/sprite_lookup.json').read_text()) if has_baseline else None
if has_baseline:
    assert old_lookup['artVersion'] == 5
    assert sha((baseline / 'Data/sprite_lookup.json').read_bytes()) == preservation['provenance']['sourceLookupSha256']
changed_styles = []
locked_paths = {r['file'] for r in preservation['lockedT01APngs']}
motif_records = {r['styleId']: r for r in preservation['motifPngs']}
for style in lookup['styles']:
    changed_cells = None
    if has_baseline:
        old = next(s for s in old_lookup['styles'] if s['styleId'] == style['styleId'])
        changed_cells = 0
        for variant, old_variant in zip(style['variants'], old['variants']):
            assert variant['id'] == old_variant['id']
            for cell, old_cell in zip(variant['cells'], old_variant['cells']):
                assert cell['mask'] == old_cell['mask']
                different = (root / cell['file']).read_bytes() != (baseline / old_cell['file']).read_bytes()
                changed_cells += int(different)
                if style['styleId'] == 'T01_A':
                    assert not different, f"Locked v5 cell changed: {cell['file']}"
    if style['styleId'] != 'T01_A':
        if has_baseline:
            assert changed_cells > 0, f"Style was not revised: {style['styleId']}"
        changed_styles.append({'styleId': style['styleId'], 'changedCellPngs': changed_cells,
                               'changedNativeBodyFromV5': True})
    motif = motif_records[style['styleId']]
    content = (root / style['panels']['motif']).read_bytes()
    assert len(content) == motif['bytes'] and sha(content) == motif['sha256'], f"Original motif changed: {style['styleId']}"
    if has_baseline:
        assert content == (baseline / old['panels']['motif']).read_bytes()
for record in preservation['lockedT01APngs'] + preservation['originalSourceArtPngs']:
    content = (root / record['file']).read_bytes()
    assert len(content) == record['bytes'] and sha(content) == record['sha256'], f"Preserved v5 artwork changed: {record['file']}"
    if has_baseline:
        assert content == (baseline / record['file']).read_bytes()
approved = read('Data/approved_design_sources.json')
for source in approved['sources']:
    content = (root / source['sourceFile']).read_bytes()
    assert sha(content) == source['sha256'] and len(content) == source['byteLength']
    if has_baseline:
        assert content == (baseline / source['sourceFile']).read_bytes()

paths = sorted(p for p in root.rglob('*') if p.is_file()
               and p not in {manifest_path, output, output.with_suffix(output.suffix + '.building')} and 'node_modules' not in p.parts
               and '__pycache__' not in p.parts
               and not any(part.startswith('.') for part in p.relative_to(root).parts))
cells = sorted({c['file'] for s in lookup['styles'] for v in s['variants'] for c in v['cells']})
assert len(cells) == 3760
digest = hashlib.sha256()
for filename in cells:
    digest.update((filename + '\n').encode())
    digest.update((root / filename).read_bytes())
manifest = {
    'package': root.name, 'assetStatus': 'revised-output-for-review',
    'artVersion': 6, 'contractId': lookup['contractId'], 'schemaVersion': 1,
    'changedStyles': [s['styleId'] for s in changed_styles], 'changedStyleCells': changed_styles,
    'preservedStyles': ['T01_A'], 'lockedV5ArtworkPngs': len(locked_paths),
    'preservedMotifCount': 20, 'historicalApprovedSourceCount': len(approved['sources']),
    'preservationAuthority': 'actual v5 package and captured SHA256' if has_baseline else 'bundled exact v5 PNG/RGBA SHA256 and complete T01_A metadata',
    'preservationManifest': 'Data/v5_preservation_manifest.json',
    'unityIntegration': 'NOT RUN',
    'pixelQualityVerdict': 'Declared material/role planes, actual quarter/cell/atlas RGBA and small-hole/port regressions PASS. Visual finishing reviewed with actual output. Universal aesthetic jaggies approval is not implied.',
    'currentQaReport': 'Docs/theme_finish_validation.json',
    'previewSource': 'Actual lookup-selected exported atlas rects; material swatches crop actual material panels. No hand-painted corrections.',
    'cellPngDigestMethod': 'SHA256(sorted relative cell path + LF + PNG bytes)',
    'cellPngDigestSha256': digest.hexdigest(),
    'files': [{'path': p.relative_to(root).as_posix(), 'bytes': p.stat().st_size,
               'sha256': sha(p.read_bytes())} for p in paths],
}
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
temporary = output.with_suffix(output.suffix + '.building')
output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for p in sorted(paths + [manifest_path]):
        archive.write(p, root.name + '/' + p.relative_to(root).as_posix())
with zipfile.ZipFile(temporary) as archive:
    assert archive.testzip() is None
    assert len(archive.infolist()) == len(manifest['files']) + 1
    for item in manifest['files']:
        content = archive.read(root.name + '/' + item['path'])
        assert len(content) == item['bytes']
        assert sha(content) == item['sha256']
os.replace(temporary, output)
print(json.dumps({'result': 'PASS', 'archive': str(output), 'artVersion': 6,
                  'changedStyles': len(changed_styles), 'preservedStyles': 1,
                  'cellPngs': len(cells), 'files': len(paths) + 1,
                  'bytes': output.stat().st_size, 'sha256': sha(output.read_bytes())}))
