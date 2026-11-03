"""R2 source/DTO checks only: never claims Unity/C# execution."""
from pathlib import Path
import argparse
import ast
import hashlib
import json
import math
import re
import shutil

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[3]
parser = argparse.ArgumentParser()
parser.add_argument('--baseline', type=Path, default=PROJECT / 'output/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01')
parser.add_argument('--examples', type=Path, default=HERE.parent / 'examples_exports/Examples')
args = parser.parse_args()
PIN = '8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069'
PACKAGE = 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
sources = {p.name: p.read_text() for p in sorted((HERE / 'Runtime').glob('*.cs'))}
checks = []
def check(name, condition, detail):
    assert condition, name
    checks.append({'name': name, 'status': 'PASS_SOURCE_OR_DTO_REVIEW_ONLY', 'detail': detail})

# Reuse the previously reviewed lexical scanner without executing its surrounding audit module.
lexer_file = PROJECT / 'work/theater_connected_v01/runtime_exports/review_source.py'
syntax = ast.parse(lexer_file.read_text())
lexer_node = next(node for node in syntax.body if isinstance(node, ast.FunctionDef) and node.name == 'stripped')
scope = {}
exec(compile(ast.Module(body=[lexer_node], type_ignores=[]), 'reviewed_literal_scanner', 'exec'), scope)
for name, text in sources.items():
    clean = scope['stripped'](text); stack = []; match = {')': '(', ']': '[', '}': '{'}
    for char in clean:
        if char in '([{': stack.append(char)
        elif char in ')]}': assert stack and stack.pop() == match[char], name
    assert not stack, name
check('six additive runtime files lexically balanced', len(sources) == 6,
      'Comments/literals/delimiters checked only; no C# parser/compiler.')
joined = '\n'.join(sources.values())
snapshot = sources['TheaterConnectedDecorV01BoardSnapshot.cs']
asset = sources['TheaterConnectedDecorV01BoardAsset.cs']
source = sources['TheaterConnectedDecorV01SnapshotSource.cs']
published = sources['TheaterConnectedDecorV01PublishedSnapshotSource.cs']
authored = sources['TheaterConnectedDecorV01AuthoredSnapshotSource.cs']
binding = sources['TheaterConnectedDecorV01Binding.cs']
check('flat DTO and current identity/pin', 'public float originX, originY, originZ, cellSize;' in snapshot
      and 'TheaterDecorAllowedCell[] allowedDecorCells' in snapshot and PIN in snapshot and 'revision < 0 || !isWholeBoard' in snapshot,
      'No nested arrays/Dictionary/interface fields in serialized DTO.')
check('required arrays are explicit', 'networkCells == null || groupPlacements == null || allowedDecorCells == null' in snapshot,
      'Null/missing required arrays fail; explicit empty arrays can clear.')
check('full plan and exact plane whitelist validation', 'TheaterConnectedDecorV01Planner.CreatePlan(' in snapshot
      and 'claim.logicalLayer, claim.heightKey' in snapshot and '!keys.Add(' in snapshot,
      'Existing planner validates every footprint; approval callback captures its own HashSet.')
check('defensive copies include all mutable rows',
      all(value in snapshot for value in ['new TheaterDecorNetworkCell(row.x', 'new TheaterDecorGroupPlacement(row.placementId', 'new TheaterDecorAllowedCell(row.x']),
      'Outer arrays and each network/group/allowed record are cloned; strings are immutable.')
check('finite input and derived renderer positions', 'RequireWorldPosition' in snapshot
      and 'float.MaxValue' in snapshot and '!Finite(originX)' in snapshot,
      'NaN/infinite inputs and derived float-coordinate overflow fail before publication.')
check('asset stores and returns validated copies', 'var next = value.DeepCopy()' in asset
      and 'if (!next.TryValidate(out error)) return false;' in asset and 'var copy = snapshot.DeepCopy()' in asset,
      'Invalid asset edit is not committed; getter returns independent validated data.')
check('published invalid input/example invalidates old state',
      published.count('InvalidateSnapshot(error)') >= 3 and 'if (next.isExampleOnly)' in published,
      'No last-valid authority fallback after an invalid publish.')
check('published pool disable requires fresh publication', 'protected override void OnDisable()' in published
      and 'Authoritative source disabled; the host must publish a fresh snapshot before reuse.' in published,
      'Fresh explicit publication while inactive remains possible after the disable invalidation.')
check('source events cover lifetime and pure captures', 'public event Action Changed' in source
      and 'protected virtual void OnEnable()' in source and 'protected virtual void OnDisable()' in source
      and 'protected virtual void OnDestroy()' in source,
      'Availability changes reach bindings; one subscriber exception does not prevent others.')
check('authored state and example role stay explicit', 'AssignBoard(' in authored and 'NotifyBoardChanged(' in authored
      and 'value.isExampleOnly' in authored and 'return board.TryGetSnapshot' in authored,
      'Static authored assets do not masquerade as fresh dynamic game state.')
check('concrete Configure and SetLayout calls exist', 'controller.Configure(catalog, expectedManifestSha256, sortingLayerName)' in binding
      and 'controller.SetLayout(snapshot.networkCells, snapshot.groupPlacements,' in binding,
      'The new Binding calls both existing APIs; it is not only an unused public wrapper.')
check('controller ownership and explicit construction', 'controller.gameObject != gameObject' in binding
      and 'GetComponent<TheaterConnectedDecorV01Controller>()' in binding and 'ownedController.ClearLayout()' in binding,
      'Binding resolves its own required Controller even after an Inspector field edit; foreign visuals are never cleared.')
check('actual source subscription and cross-source unsubscribe', 'subscribedSource.Changed += OnSourceChanged' in binding
      and 'subscribedSource.Changed -= OnSourceChanged' in binding and 'Unsubscribe(); ClearVisuals(); controller = target' in binding,
      'Active binding observes the current provider only; inactive/destroyed binding detaches.')
check('inactive construction and disabled source policy', '(isActiveAndEnabled && !source.isActiveAndEnabled)' in binding
      and 'source == null || !source.enabled' in binding,
      'Enabled inactive root can be authored; disabled source or inactive source under an active binding fails.')
check('example data requires explicit opt-in', 'bool allowExampleSource = false' in binding
      and 'snapshot.isExampleOnly != source.IsExampleSource' in binding
      and '(snapshot.isExampleOnly && !allowExampleSource)' in binding,
      'Published production source also independently rejects example payloads.')
check('execution order and failed layout clearing', '[DefaultExecutionOrder(100)]' in binding
      and 'private void OnDisable() { Unsubscribe(); ClearVisuals(); }' in binding
      and 'ClearVisuals(); error =' in binding,
      'Binding runs after default-order Controller; rejects current input without old visual fallback.')
check('idempotence validates hierarchy before cache', binding.index('if (!ValidateHierarchy(') < binding.index('if (lastAppliedHash == content')
      and 'lastAppliedWorldMatrix.Equals(worldMatrix)' in binding and 'lastAppliedControllerActive == controller.isActiveAndEnabled' in binding,
      'Cache includes world transform and active state; inactive-to-active Editor refresh really applies.')
check('transform and configuration changes force reapply', 'lastAppliedCatalog == catalog && lastAppliedSource == source' in binding
      and 'lastAppliedSortingLayer == sortingLayerName' in binding and 'ReferenceEquals(controller.CurrentPlan, lastAppliedPlan)' in binding,
      'Changed parent/configuration/externally replaced plan cannot reuse a stale binding cache.')
check('no polling/game authority introduced', not re.search(r'\b(?:Start|Update|FixedUpdate|LateUpdate)\s*\(', '\n'.join(scope['stripped'](s) for s in sources.values()))
      and 'AddComponent<Collider' not in joined and 'AddComponent<Rigidbody' not in joined,
      'Event-driven decoration visuals only; no timer/input/turn/collision generation.')
test_file = HERE / 'Tests~/TheaterConnectedDecorV01CompletionTests.cs'
tests = test_file.read_text()
check('nineteen meaningful optional source tests', tests.count('[Test]') == 19
      and 'PublishedSourceRequiresFreshPublicationAfterPoolDisable' in tests
      and 'RefreshRepositionsMovedParentAndRejectsInvalidChangedHierarchy' in tests,
      'NOT_RUN. Five asset-backed tests require a real generated catalog; absence explicitly Ignore.')

baseline_manifest = json.loads((args.baseline / 'manifest.json').read_text())
check('existing art identity retained', hashlib.sha256((args.baseline / 'manifest.json').read_bytes()).hexdigest() == PIN
      and len(baseline_manifest['assets']) == 118,
      'Source files are additive; original118PNG/art manifest were not written by this owner.')
prior_audit = json.loads((PROJECT / 'work/theater_connected_v01/completion_review_runtime/operational_gap_audit.json').read_text())
check('eight original C# files unchanged', all(hashlib.sha256((args.baseline / item['file']).read_bytes()).hexdigest() == item['sha256']
      for item in prior_audit['reviewedFiles']), 'Original planner/controller/catalog/importer APIs retained byte-for-byte.')

groups = {row['key']: tuple(row['footprintCells']) for row in baseline_manifest['groups']}
families = {row['family'] for row in baseline_manifest['assets'] if row['kind'] == 'connected_tile'}
example_checks = []
for filename in ['stage_snapshot.json', 'all26_snapshot.json']:
    path = args.examples / filename; data = json.loads(path.read_text())
    assert len(data) == 16 and data['packageId'] == PACKAGE and data['sourceManifestSha256'] == PIN
    assert data['isWholeBoard'] is True and data['isExampleOnly'] is True
    assert data['cellSize'] > 0 and all(math.isfinite(data[key]) for key in ['originX', 'originY', 'originZ', 'cellSize'])
    approved = {(row['x'], row['y'], row['logicalLayer'], row['heightKey']) for row in data['allowedDecorCells']}
    assert len(approved) == len(data['allowedDecorCells'])
    occupied = set(); ids = set(); used_families = set()
    for row in data['networkCells']:
        assert row['family'] in families and row['connectionGroup'].strip()
        cell = (row['x'], row['y'], row['logicalLayer'], row['heightKey'])
        assert cell in approved and cell not in occupied; occupied.add(cell); used_families.add(row['family'])
    for row in data['groupPlacements']:
        assert row['placementId'] not in ids; ids.add(row['placementId']); width, height = groups[row['groupKey']]
        used_families.add(row['groupKey'])
        for y in range(height):
            for x in range(width):
                cell = (row['anchorX'] + x, row['anchorY'] + y, row['logicalLayer'], row['heightKey'])
                assert cell in approved and cell not in occupied; occupied.add(cell)
    if filename.startswith('all26'): assert len(used_families) == 26 and {row['groupKey'] for row in data['groupPlacements']} == set(groups)
    example_checks.append({'file': filename, 'status': 'PASS_DTO_REVIEW_ONLY', 'networks': len(data['networkCells']),
        'groups': len(data['groupPlacements']), 'uniqueFamilies': len(used_families), 'claims': len(occupied),
        'approvedCells': len(approved), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
check('flat stage and all26 examples have complete approved claims', len(example_checks) == 2,
      'DTO validation is offline Python inspection, not JsonUtility or Unity execution.')

report = {'status': 'PASS_SOURCE_AND_DTO_REVIEW_ONLY', 'checksCount': len(checks), 'checks': checks,
    'exampleChecks': example_checks, 'optionalNUnitSourceTests': 19, 'actualNUnitExecution': 'NOT_RUN',
    'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualGeneratedPrefab': 'NOT_RUN',
    'actualGeneratedScenePlay': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN',
    'toolchain': {name: shutil.which(name) for name in ['Unity', 'unity-editor', 'dotnet', 'csc', 'mcs']},
    'limits': 'Read/lexical/DTO-contract inspection only. No C# parser, compiler, Unity runtime or original game project was available.',
    'files': [{'file': str(p.relative_to(HERE)), 'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
              for p in sorted(list((HERE / 'Runtime').glob('*.cs')) + list((HERE / 'Tests~').glob('*.cs')))]}
(HERE / 'runtime_completion_source_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checks), 'examples': example_checks, 'newCSharp': len(sources), 'actualUnity': 'NOT_RUN'}))
