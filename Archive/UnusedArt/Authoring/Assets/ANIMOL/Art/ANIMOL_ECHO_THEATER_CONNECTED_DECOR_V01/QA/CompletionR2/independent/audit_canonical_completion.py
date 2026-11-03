"""Independent canonical R2 bytes/data/caller wiring audit. No Unity/C# execution."""
from pathlib import Path
import argparse
import hashlib
import json
import re

O = Path(__file__).resolve().parent; C = O.parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text())
ap = argparse.ArgumentParser(); ap.add_argument('--package', required=True); args = ap.parse_args()
P = Path(args.package).resolve(); errors = []; checked = []
def test(name, condition):
    checked.append({'check': name, 'status': 'PASS_CANONICAL_OFFLINE' if condition else 'FAIL_CANONICAL_OFFLINE'})
    if not condition: errors.append(name)
baseline = read(O / 'before_completion_baseline.json')
test('Original exact art manifest and pin retained', sha(P / 'manifest.json') == baseline['manifestSha256'] == (P / 'manifest_sha256.txt').read_text().strip())
for row in baseline['coreFiles'] + baseline['runtimeAssets']:
    test('Original bytes ' + row['file'], (P / row['file']).is_file() and sha(P / row['file']) == row['sha256'])
newfiles = []
for owner in ['runtime_exports', 'editor_exports']:
    for p in (C / owner).rglob('*.cs'):
        rel = p.relative_to(C / owner).as_posix(); newfiles.append(rel)
        test('Current new source ' + rel, (P / rel).is_file() and sha(P / rel) == sha(p))
test('19actual primary C# files =8old+11new', len(newfiles) == 11 and sum(len(list((P / category).glob('*.cs'))) for category in ['Runtime', 'Editor', 'Tests~']) == 19)
test('No source file name collision with original core', not set(newfiles) & {r['file'] for r in baseline['coreFiles']})
for name in ['stage_snapshot.json', 'all26_snapshot.json', 'inspection_layout_guide.json']:
    source = C / 'examples_exports/Examples' / name
    test('Canonical typed example bytes ' + name, sha(P / 'Examples' / name) == sha(source))
for source in (C / 'docs_exports').iterdir():
    if source.is_file(): test('Canonical current guide/CLI ' + source.name, (P / source.name).is_file() and sha(P / source.name) == sha(source))
for row in read(C / 'preview_exports/ALL26_PREVIEW_FROZEN.json')['files']:
    target = P / 'Inspection/CompletionR2' / row['file']
    test('Canonical all26 reference ' + row['file'], target.is_file() and sha(target) == row['sha256'])
rev = read(P / 'installation_revision.json')
test('Installation R2 with unchanged art identity', rev['installationRevision'] == 2 and rev['artRevision'] == 'V01' and rev['runtimePngCount'] == 118)
binding = (P / 'Runtime/TheaterConnectedDecorV01Binding.cs').read_text()
tools = (P / 'Editor/TheaterConnectedDecorV01BoardTools.cs').read_text()
test('Delivered binding has concrete calls, subscription and current capture', all(x in binding for x in ['controller.Configure(', 'controller.SetLayout(', 'source.TryCaptureSnapshot(', '.Changed += OnSourceChanged', '.Changed -= OnSourceChanged']))
test('Delivered Editor builds connected components and explicitly refreshes before prefab', all(x in tools for x in ['root.AddComponent<TheaterConnectedDecorV01Controller>()', 'root.AddComponent<TheaterConnectedDecorV01AuthoredSnapshotSource>()', 'root.AddComponent<TheaterConnectedDecorV01Binding>()', 'source.AssignBoard(board, out error)', 'binding.Bind(controller, catalog, pin, sortingLayer, source, out error, true)', 'Require(binding.RefreshNow(out error), error)', 'PrefabUtility.SaveAsPrefabAsset(']))
test('Example scenes/assets have not been invented as placeholders', not any(p.suffix in ['.unity', '.prefab', '.asset'] for p in P.rglob('*') if p.is_file()))
sha_entries = 0
for line in (P / 'package_checksums_sha256.txt').read_text().splitlines():
    digest, rel = line.split('  ', 1); test('Package checksum ' + rel, sha(P / rel) == digest); sha_entries += 1
report = {'status': 'PASS_CANONICAL_OFFLINE' if not errors else 'FAIL_CANONICAL_OFFLINE', 'packageId': 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01',
          'installationRevision': 2, 'artManifestSha256': sha(P / 'manifest.json'), 'originalCore8Unchanged': True if not any(x.startswith('Original bytes') for x in errors) else False,
          'original118PNGUnchanged': True if not any(x.startswith('Original bytes decor/') for x in errors) else False,
          'newCompletionCSharpFiles': len(newfiles), 'totalPrimaryCSharpFiles': 19, 'packageChecksumEntries': sha_entries,
          'newSourceFiles': newfiles, 'checks': checked, 'checkCount': len(checked), 'errors': errors,
          'scope': 'Independent canonical-to-frozen-producer/source/data/docs/reference byte comparisons, original8core+118PNG preservation, concrete delivered caller/import source presence and full package checksum validation. Does not execute C#/Unity or create actual prefab/scene.',
          'actualCSharpCompile': 'NOT_RUN', 'actualUnityImport': 'NOT_RUN', 'actualGeneratedPrefabScene': 'NOT_RUN', 'actualTestRunner': 'NOT_RUN', 'actualScenePlay': 'NOT_RUN', 'actualProductionHostBindings': 'NOT_RUN'}
(O / 'canonical_completion_independent_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checked), 'packageChecksums': sha_entries, 'original8And118Unchanged': not errors, 'newCSharp': len(newfiles), 'errors': errors}))
