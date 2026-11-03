"""Independent completion source/ownership/caller audit. Not C# compilation or Unity execution."""
from pathlib import Path
import hashlib
import json
import re
import shutil

O = Path(__file__).resolve().parent; C = O.parent; W = C.parent
PACKAGE = Path('/workspace/scratch/b0cb6eab55f7/output/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01')
PREFIX = 'TheaterConnectedDecorV01'
sources = {}
paths = {}
for owner in ['runtime_exports', 'editor_exports']:
    root = C / owner
    for p in root.rglob('*.cs'):
        key = p.relative_to(root).as_posix()
        assert key not in sources
        sources[key] = p.read_text(); paths[key] = p
B = 'Runtime/' + PREFIX + 'Binding.cs'
S = 'Runtime/' + PREFIX + 'BoardSnapshot.cs'
A = 'Runtime/' + PREFIX + 'BoardAsset.cs'
P = 'Runtime/' + PREFIX + 'PublishedSnapshotSource.cs'
H = 'Runtime/' + PREFIX + 'AuthoredSnapshotSource.cs'
E = 'Runtime/' + PREFIX + 'SnapshotSource.cs'
J = 'Editor/' + PREFIX + 'BoardJson.cs'
T = 'Editor/' + PREFIX + 'BoardTools.cs'
I = 'Editor/' + PREFIX + 'BoardWindow.cs'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text())
def clean(text):
    pattern = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
    return pattern.sub(lambda m: '\n' * m.group().count('\n'), text)
def balanced(text):
    stack = []; pair = {'}': '{', ')': '(', ']': '['}
    for ch in clean(text):
        if ch in '{([': stack.append(ch)
        elif ch in '})]':
            if not stack or stack.pop() != pair[ch]: return False
    return not stack
def has(file, *tokens): return all(t in sources.get(file, '') for t in tokens)
def before(file, first, second):
    text = sources.get(file, '')
    return first in text and second in text and text.index(first) < text.index(second)
def ev(file, token):
    text = sources.get(file, ''); at = text.find(token)
    return {'file': file, 'line': text[:at].count('\n') + 1 if at >= 0 else None, 'token': token}
checks = []
def check(id, description, condition, evidence=()):
    checks.append({'id': id, 'description': description, 'status': 'PASS_SOURCE_REVIEW_ONLY' if condition else 'FAIL_SOURCE_REVIEW',
                   'evidence': [ev(f, t) for f, t in evidence]})

baseline = read(O / 'before_completion_baseline.json')
original_core = all((PACKAGE / r['file']).is_file() and sha(PACKAGE / r['file']) == r['sha256'] for r in baseline['coreFiles'])
original_png = all((PACKAGE / r['file']).is_file() and sha(PACKAGE / r['file']) == r['sha256'] for r in baseline['runtimeAssets'])
check('C01', 'Original8 core C# bytes are unchanged.', len(baseline['coreFiles']) == 8 and original_core)
check('C02', 'Original118 runtime PNGs and exact art manifest pin are unchanged; prior108 art-filtering evidence is reused.',
      len(baseline['runtimeAssets']) == 118 and original_png and sha(PACKAGE / 'manifest.json') == baseline['manifestSha256'] == '8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069')
check('C03', 'Nine new non-test sources and two optional-test sources are lexically balanced. This is not compilation.',
      len(sources) == 11 and all(balanced(t) and len(re.findall(r'^#if\b', t, re.M)) == len(re.findall(r'^#endif\b', t, re.M)) for t in sources.values()))
check('C04', 'All new runtime/editor types stay in the existing package namespace; optional tests import it.',
      all('namespace ANIMOL.Art.' + PREFIX in t for f, t in sources.items() if not f.startswith('Tests~'))
      and all('using ANIMOL.Art.' + PREFIX + ';' in t or 'namespace ANIMOL.Art.' + PREFIX in t for f, t in sources.items() if f.startswith('Tests~')))
check('C05', 'Binding is the previously missing concrete Controller.Configure + SetLayout caller.',
      has(B, 'controller.Configure(catalog, expectedManifestSha256, sortingLayerName)', 'controller.SetLayout(snapshot.networkCells, snapshot.groupPlacements,', 'snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing)'),
      [(B, 'controller.Configure('), (B, 'controller.SetLayout(')])
check('C06', 'Snapshot is a flat typed DTO with explicit nullable required arrays, exact art pin/whole-board/nonnegative revision and finite origin/cell size.',
      has(S, 'public long revision;', 'public bool isWholeBoard, isExampleOnly;', 'TheaterDecorNetworkCell[] networkCells;', 'TheaterDecorGroupPlacement[] groupPlacements;', 'TheaterDecorAllowedCell[] allowedDecorCells;',
          'revision < 0 || !isWholeBoard', 'networkCells == null || groupPlacements == null || allowedDecorCells == null', 'cellSize <= 0')
      and not re.search(r'\[\]\s*\[\]', clean(sources.get(S, ''))))
check('C07', 'All three mutable row arrays and every row are independently copied; no shallow array clone is used.',
      has(S, 'new TheaterDecorNetworkCell[networkCells.Length]', 'new TheaterDecorNetworkCell(row.x, row.y, row.family,',
          'new TheaterDecorGroupPlacement[groupPlacements.Length]', 'new TheaterDecorGroupPlacement(row.placementId,',
          'new TheaterDecorAllowedCell[allowedDecorCells.Length]', 'new TheaterDecorAllowedCell(row.x, row.y, row.logicalLayer, row.heightKey)'))
check('C08', 'Approval closure captures an immutable private plane-aware whitelist; duplicates and missing cells fail via existing whole-group Planner.',
      has(S, 'new HashSet<AllowedKey>()', '!keys.Add(new AllowedKey(cell.x, cell.y, cell.logicalLayer, cell.heightKey))',
          'return claim => keys.Contains(new AllowedKey(claim.cell.x, claim.cell.y, claim.logicalLayer, claim.heightKey))',
          'TheaterConnectedDecorV01Planner.CreatePlan('))
check('C09', 'Snapshot validation also rejects rendered world coordinate overflow before BoardAsset creation.',
      has(S, 'RequireWorldPosition(row.x + .5, row.y + .5, row.worldZ)', 'RequireWorldPosition(row.CenterX, row.CenterY, row.worldZ)', 'x > float.MaxValue', 'depth > float.MaxValue'))
check('C10', 'BoardAsset validates a private defensive copy before commit and returns a validated defensive copy.',
      has(A, 'var next = value.DeepCopy();', 'var copy = snapshot.DeepCopy();', 'if (!next.TryValidate(out error)) return false;', 'snapshot = next;', 'value = copy; return true;')
      and before(A, 'next.TryValidate(', 'snapshot = next;'))
check('C11', 'Authoritative source publishes/captures defensive copies, rejects examples, invalidates rejected publishes and notifies listeners.',
      has(P, 'var next = value.DeepCopy();', 'var copy = current.DeepCopy();', 'if (next.isExampleOnly)', 'InvalidateSnapshot(error); return false;', 'current = next; unavailableReason = null; RaiseChanged(); return true;'))
check('C12', 'Published source invalidates on pool disable; an old board cannot silently resurrect on source enable.',
      has(P, 'protected override void OnDisable()', 'InvalidateSnapshot("Authoritative source disabled; the host must publish a fresh snapshot before reuse.")'),
      [(P, 'protected override void OnDisable()')])
check('C13', 'Authored source stores an explicit BoardAsset and notifies only on explicit assign/board change or source availability.',
      has(H, '[SerializeField] private TheaterConnectedDecorV01BoardAsset board;', 'AssignBoard(', 'NotifyBoardChanged(', 'RaiseChanged(); return valid;', 'return board.TryGetSnapshot(out snapshot, out error);'))
check('C14', 'Source lifecycle raises availability events; one throwing listener cannot suppress the remaining listeners.',
      has(E, 'public event Action Changed;', 'foreach (Action callback in callbacks.GetInvocationList())', 'try { callback(); }',
          'protected virtual void OnEnable() { RaiseChanged(); }', 'protected virtual void OnDisable() { RaiseChanged(); }'))
check('C15', 'Binding executes after default Controller callbacks, subscribes/captures fresh on enable, unsubscribes and clears on disable/destroy.',
      has(B, '[DefaultExecutionOrder(100)]', 'private void OnEnable() { ReconcileSubscription(); string ignored; RefreshNow(out ignored); }',
          'private void OnDisable() { Unsubscribe(); ClearVisuals(); }', 'private void OnDestroy() { Unsubscribe(); ClearVisuals(); }',
          'subscribedSource.Changed += OnSourceChanged', 'subscribedSource.Changed -= OnSourceChanged'))
check('C16', 'Binding source switch unsubscribes prior source and clears owned visuals before replacing config.',
      has(B, 'Unsubscribe(); ClearVisuals(); controller = target; catalog = latest;', 'subscribedSource == source && isActiveAndEnabled'))
check('C17', 'Binding requires its own enabled Controller, current catalog/pin/existing Sorting Layer and fresh validated source; examples require explicit opt-in.',
      has(B, 'controller.gameObject != gameObject', '!controller.enabled', 'source == null || !source.enabled', 'catalog.ValidateIntegrity(',
          'var snapshot = captured.DeepCopy();', 'if (!snapshot.TryValidate(out reason))', 'snapshot.isExampleOnly != source.IsExampleSource', '(snapshot.isExampleOnly && !allowExampleSource)',
          'bool allowExampleSource = false'))
check('C18', 'Idempotence is keyed by actual data/config/plan, full hierarchy matrix and previous active state, after hierarchy validation.',
      has(B, 'lastAppliedCatalog == catalog && lastAppliedSource == source', 'lastAppliedPin == expectedManifestSha256 && lastAppliedSortingLayer == sortingLayerName',
          'lastAppliedWorldMatrix.Equals(worldMatrix)', 'lastAppliedControllerActive == controller.isActiveAndEnabled', 'ReferenceEquals(controller.CurrentPlan, lastAppliedPlan)')
      and before(B, 'ValidateHierarchy(snapshot.cellSize', 'lastAppliedHash == content'), [(B, 'lastAppliedControllerActive == controller.isActiveAndEnabled')])
check('C19', 'Changed invalid hierarchy fails closed, including finite ancestor transforms/identity rotation/positive XY and representable uniform world scale.',
      has(B, 'for (Transform node = transform; node != null; node = node.parent)', 'Quaternion.Angle(rotation, Quaternion.identity) > .01f',
          'scale.x <= 0 || scale.y <= 0', 'float localX = cellSize / parentScale.x', 'worldMatrix[row, column]'))
check('C20', 'Even serialized controller ref null/foreign changes clear the same-GameObject owned Controller and never a foreign one.',
      has(B, 'var ownedController = GetComponent<TheaterConnectedDecorV01Controller>();', 'if (ownedController != null) ownedController.ClearLayout();')
      and not has(B, 'if (controller != null && controller.gameObject == gameObject) controller.ClearLayout();'), [(B, 'var ownedController = GetComponent<')])
check('C21', 'RefreshReport is defensive; reentrant capture is bounded and failure clears owned visuals with a diagnostic.',
      has(B, 'public RefreshReport LastReport { get { return lastReport.Copy(); } }', 'for (int attempt = 0; attempt < 3; attempt++)',
          'finally { refreshing = false; refreshRequested = false; }', 'ClearVisuals(); error = string.IsNullOrWhiteSpace(reason)'))
check('C22', 'No new runtime polling, input, colliders, rigidbodies, random/timer generation or ExecuteAlways is introduced.',
      not any(re.search(r'\b(?:void\s+(?:Start|Update|FixedUpdate|LateUpdate)\s*\(|Input\.|Random\.|InvokeRepeating\s*\(|AddComponent\s*<\s*(?:Collider|BoxCollider|PolygonCollider|Rigidbody)|ExecuteAlways|ExecuteInEditMode)', clean(t)) for f, t in sources.items() if f.startswith('Runtime/')))
check('C23', 'Strict flat parser rejects unknown/missing/duplicate keys, wrong types, Int32/Int64 overflow, malformed Unicode and nonfinite floats before typed decode.',
      has(J, 'ValidateShape(json);', 'Unknown or missing board JSON field.', 'Expected exact Int32:', 'Expected exact Int64:', 'Non-finite or overflowing float:', 'NumberStyles.AllowHexSpecifier')
      and before(J, 'ValidateShape(json);', 'JsonUtility.FromJson<'))
check('C24', 'Both input JSON/claims/pins and all26 inventory plus every output ownership are preflighted before catalog/board writes.',
      before(T, 'ReadExample(root, "All26"', 'CatalogBuilder.BuildFromManifest(')
      and before(T, 'PreflightOutputs(example, ownerRootLabel, createScenes)', 'CatalogBuilder.BuildFromManifest(')
      and before(T, 'ValidateAll26(all.snapshot)', 'CatalogBuilder.BuildFromManifest(')
      and has(T, 'snapshot.isExampleOnly && snapshot.isWholeBoard', 'new UTF8Encoding(false, true)'))
check('C25', 'Generated BoardAsset validates defensive data before CreateAsset and SaveAssetIfDirty.',
      before(T, 'output.TrySetSnapshot(example.snapshot', 'AssetDatabase.CreateAsset(output, example.boardPath)') and has(T, 'AssetDatabase.SaveAssetIfDirty(output)'))
check('C26', 'Outputs are package _Generated-only, typed, labelled with exact package-root GUID; unrelated/unimported/dirty existing outputs fail.',
      has(T, '"/_Generated/InspectionBoards/"', 'labels.Contains(OwnerLabel) && labels.Contains(rootLabel)', 'expectedType.IsInstanceOfType(prior)',
          '!EditorUtility.IsDirty(prior)', 'AssetDatabase.AssetPathToGUID(root)', 'Refusing to overwrite an unimported/unrelated path:'))
check('C27', 'Currently loaded target output scenes and open output Prefab Stage are rejected before all writes.',
      has(T, 'PrefabStageUtility.GetCurrentPrefabStage() == null', 'SceneManager.GetSceneAt(i).path, example.scenePath, StringComparison.OrdinalIgnoreCase'),
      [(T, 'PrefabStageUtility.GetCurrentPrefabStage()')])
check('C28', 'Actual example contains Controller/AuthoredSource/Binding/BoardAsset refs; activates then explicitly refreshes and checks enabled visible renderers before saving prefab.',
      has(T, 'root.AddComponent<TheaterConnectedDecorV01Controller>()', 'root.AddComponent<TheaterConnectedDecorV01AuthoredSnapshotSource>()',
          'root.AddComponent<TheaterConnectedDecorV01Binding>()', 'source.AssignBoard(board, out error)', 'binding.Bind(controller, catalog, pin, sortingLayer, source, out error, true)',
          'Require(binding.RefreshNow(out error), error)', 'renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null', 'PrefabUtility.SaveAsPrefabAsset(')
      and before(T, 'root.SetActive(true);', 'Require(binding.RefreshNow(out error), error)'), [(T, 'binding.Bind('), (T, 'Require(binding.RefreshNow(')])
check('C29', 'Scene creation is additive and isolated; finally restores prior active handle and closes only the new inspection scene, without saving/reopening user scenes.',
      has(T, 'NewSceneSetup.EmptyScene, NewSceneMode.Additive', 'SceneManager.SetActiveScene(inspection)', 'if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous)',
          'EditorSceneManager.CloseScene(inspection, true)', 'EditorSceneManager.SaveScene(inspection, example.scenePath)')
      and not re.search(r'(?:RestoreSceneManagerSetup|OpenScene|SaveOpenScenes|SaveCurrentModifiedScenesIfUserWantsTo)\s*\(', clean(sources.get(T, ''))))
check('C30', 'Camera/light helpers are only invoked inside opt-in isolated scene output, with no camera tags or render-pipeline asset changes.',
      has(T, 'if (saveScene)', 'FrameInspectionCamera(root);', 'lighting = CreateInspectionLight(sortingLayer);', 'THEATER_INSPECTION_CAMERA__UNTAGGED')
      and not re.search(r'(?:GraphicsSettings|QualitySettings|TagManager|EditorBuildSettings)\s*\.', clean(sources.get(T, ''))))
check('C31', 'Inspector refresh/clear targets only loaded nonpersistent binding; global Undo is explicit opt-in and returns without refresh when authoring fingerprint is unchanged.',
      has(I, 'if (!refreshAfterUndo || queued || target == null) return;', 'if (current == observedAuthoringState) return;', 'Fingerprint(target as TheaterConnectedDecorV01Binding)',
          'EditorApplication.delayCall -= RefreshAfterUndo')
      and not re.search(r'(?:FindObjectsOfType|FindObjectsByType|Resources\.FindObjectsOfTypeAll)', clean(sources.get(I, ''))))
check('C32', 'CLI requires actual manifest/stage/all26/sorting layer flags and scene generation is explicit, with actual generated GUID reporting.',
      has(T, '"-theaterDecorManifest"', '"-theaterDecorStageSnapshot"', '"-theaterDecorAllSnapshot"', '"-theaterDecorSortingLayer"',
          '"-theaterDecorCreateScenes"', 'bool createScenes = false;', 'TheaterConnectedDecorV01Catalog.IsHash(guid, 32)'))

failed = [c for c in checks if c['status'].startswith('FAIL')]
raw_runtime = '\n'.join(clean(t) for f, t in sources.items() if f.startswith('Runtime/'))
calls = [{'file': f, 'line': n, 'call': line.strip()} for f, text in sources.items() for n, line in enumerate(text.splitlines(), 1)
         if f.startswith('Runtime/') and re.search(r'controller\.(?:Configure|SetLayout)\s*\(', clean(line))]
runtime_tests = sources.get('Tests~/' + PREFIX + 'CompletionTests.cs', '')
editor_tests = sources.get('Tests~/' + PREFIX + 'BoardJsonTests.cs', '')
resolved = [
    {'id': 'R2L01', 'finding': 'Published data survived provider disable/pool reuse.', 'resolution': 'Published OnDisable invalidates and signals; explicit inactive host publish can provide fresh data.'},
    {'id': 'R2L02', 'finding': 'Idempotence cache ignored changed hierarchy/invalid scale.', 'resolution': 'Hierarchy validation before cache and full world-matrix stamp force reapply or fail closed.'},
    {'id': 'R2L03', 'finding': 'Inactive Bind followed by active edit-mode Refresh could cache-hit while renderers stayed disabled.', 'resolution': 'Last applied Controller active state is part of cache; Builder asserts all renderers enabled.'},
    {'id': 'R2L04', 'finding': 'Inspector controller reference null/foreign change could strand prior own visuals.', 'resolution': 'ClearVisuals resolves required own same-GameObject Controller with GetComponent and never clears foreign component.'},
    {'id': 'R2E01', 'finding': 'Unicode escape parser accepted internal whitespace via HexNumber.', 'resolution': 'AllowHexSpecifier-only four-character parse; independent strict JSON review retained.'},
    {'id': 'R2E02', 'finding': 'Global unrelated Undo could refresh/dirty locked inspected scene.', 'resolution': 'Explicit target opt-in plus full authoring-state fingerprint comparison before queue.'},
    {'id': 'R2E03', 'finding': 'Rebuild could overwrite an owned output currently open/unsaved in Scene or Prefab Stage.', 'resolution': 'Loaded output scene, current output Prefab Stage and dirty output assets rejected in preflight.'}]
report = {'status': 'PASS_SOURCE_REVIEW_ONLY' if not failed else 'FAIL_SOURCE_REVIEW',
          'scope': 'Independent source reading plus lexical/pattern/hash checks. Concrete caller/data/import/scene workflow exists in source; no C# compilation, JsonUtility execution, Unity import, asset generation, Play Mode or production host wiring executed.',
          'files': [{'file': f, 'sha256': sha(paths[f]), 'lines': len(sources[f].splitlines())} for f in sorted(sources)],
          'sourceFiles': len(sources), 'checks': checks, 'checkCount': len(checks), 'concreteControllerCallSites': calls,
          'originalCore8Unchanged': original_core, 'originalRuntime118PNGUnchanged': original_png,
          'artManifestSha256': baseline['manifestSha256'], 'oldArtFiltering108Cases': 'REUSED_UNCHANGED_EVIDENCE_NOT_RERUN',
          'openFindings': failed, 'resolvedFindings': [dict(r, status='RESOLVED_SOURCE_REVIEW_ONLY') for r in resolved],
          'optionalNUnitSource': {'newRuntimeTestMethods': len(re.findall(r'\[Test\]\s+public void', runtime_tests)),
                                  'newEditorTestCaseAttributes': len(re.findall(r'\[Test(?:Case\([^\]]*\))?\]', editor_tests)), 'execution': 'NOT_RUN'},
          'compilerAvailability': {n: shutil.which(n) for n in ['Unity', 'dotnet', 'csc', 'mcs']},
          'actualCSharpCompile': 'NOT_RUN', 'actualCSharpParser': 'NOT_RUN', 'actualUnityImport': 'NOT_RUN',
          'actualGeneratedAssets': 'NOT_RUN', 'actualPrefabScenePlay': 'NOT_RUN', 'actualProductionHostBindings': 'NOT_RUN', 'actualLegacyRefZero': 'NOT_RUN'}
(O / 'completion_source_independent_qa.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checks), 'newCSharpFiles': len(sources), 'actualControllerCallSites': len(calls),
                  'openFindings': [f['id'] for f in failed], 'resolvedFindings': len(resolved), 'originalCore8Unchanged': original_core, 'originalArt118Unchanged': original_png, 'optionalNUnit': report['optionalNUnitSource']}))
