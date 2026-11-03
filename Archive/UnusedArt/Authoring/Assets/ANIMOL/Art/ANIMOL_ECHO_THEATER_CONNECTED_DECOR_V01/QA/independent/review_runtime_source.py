"""Independent lexical/source-contract audit. NEVER a C# compilation or Unity test."""
from pathlib import Path
import hashlib
import json
import re
import shutil

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent / 'runtime_exports'
files = sorted(ROOT.rglob('*.cs'))
source = {p.relative_to(ROOT).as_posix(): p.read_text() for p in files}
PREFIX = 'TheaterConnectedDecorV01'
C = f'Runtime/{PREFIX}Controller.cs'
P = f'Runtime/{PREFIX}Planner.cs'
K = f'Runtime/{PREFIX}Catalog.cs'
B = f'Editor/{PREFIX}CatalogBuilder.cs'
I = f'Editor/{PREFIX}SpriteImporter.cs'
J = f'Editor/{PREFIX}JsonSchema.cs'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def strip_literals(text):
    token = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
    return token.sub(lambda m: '\n' * m.group().count('\n'), text)
def balanced(text):
    stack, pairs = [], {'}': '{', ')': '(', ']': '['}
    for c in strip_literals(text):
        if c in '{([': stack.append(c)
        elif c in '})]':
            if not stack or stack.pop() != pairs[c]: return False
    return not stack
def has(file, *tokens): return file in source and all(t in source[file] for t in tokens)
def evidence(file, token):
    at = source.get(file, '').find(token)
    return {'file': file, 'line': source.get(file, '')[:at].count('\n') + 1 if at >= 0 else None, 'token': token}
checks = []
def check(id, description, condition, refs=()):
    checks.append({'id': id, 'description': description, 'result': 'PASS_SOURCE_REVIEW_ONLY' if condition else 'FAIL_SOURCE_REVIEW',
                   'evidence': [evidence(f, token) for f, token in refs]})

check('S01', 'Eight C# source files have balanced delimiters and preprocessor guards. This is lexical review only.',
      len(files) == 8 and all(balanced(t) and len(re.findall(r'^#if\b', t, re.M)) == len(re.findall(r'^#endif\b', t, re.M)) for t in source.values()))
check('S02', 'New isolated namespace and prefix are present, with no Ranch namespace/identifier leaking into code.',
      all('namespace ANIMOL.Art.' + PREFIX in t for f, t in source.items() if not f.startswith('Tests~'))
      and all('using ANIMOL.Art.' + PREFIX + ';' in t for f, t in source.items() if f.startswith('Tests~'))
      and not any(re.search(r'\bRanch\w*', strip_literals(t)) for t in source.values()))
check('S03', 'Catalog enforces exact current package/root GUID/Sprite GUID/fileID/manifest pin and metadata fingerprint.',
      has(K, 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01', 'sourceRootGuid', 'assetGuid', 'localFileId', 'ComputeFingerprint()', 'sourceManifestSha256'),
      [(K, 'ValidateIntegrity('), (K, 'ComputeFingerprint()')])
check('S04', 'Builder checks exact manifest byte SHA before PNG import and preflights every declared native PNG.',
      has(B, PREFIX + 'JsonSchema.Validate(json)', 'Current manifest bytes differ from manifest_sha256.txt.', 'ValidateSourcePng(root, row)', 'ValidateImporter(path, row)'),
      [(B, 'LoadManifest('), (B, 'Current manifest bytes differ from manifest_sha256.txt.')])
check('S05', 'Builder validates actual imported Sprite/GUID/fileID and only saves its own generated catalog.',
      has(B, 'TryGetGUIDAndLocalFileIdentifier', 'AssetDatabase.AssetPathToGUID(root) == catalog.SourceRootGuid', 'SaveAssetIfDirty(output)')
      and not re.search(r'AssetDatabase\.SaveAssets\s*\(', strip_literals(source.get(B, ''))))
check('S06', 'Importer restricts new add-on root and current exact manifest paths; unknown/source/inspection PNGs remain Default with no old art fallback.',
      has(I, PREFIX + 'Catalog.PackageId', 'ExactRows(root).TryGetValue(relative, out row)', 'TextureImporterType.Default', 'ValidateSourcePng(root, row)')
      and not re.search(r'AssetDatabase\.(?:Refresh|ImportAsset|SaveAssets)\s*\(', strip_literals(source.get(I, ''))))
check('S07', 'Native Single/FullRect/PPU/custom ground pivot/uncompressed/no mip/no NPOT/no physics shape are explicit.',
      has(I, 'SpriteImportMode.Single', 'row.ppu', 'settings.spritePivot', 'SpriteMeshType.FullRect', 'spriteGenerateFallbackPhysicsShape = false', 'TextureImporterNPOTScale.None', 'ClearPlatformTextureSettings'))
check('S08', 'Schema rejects duplicate JSON fields and requires assets/group ground metadata.',
      has(J, 'Duplicate JSON property:', 'logicalRect', 'footprintCells', 'family', 'kind', 'root.fields["groups"]'))
check('S09', 'Six exact cardinal4 network families use N1/E2/S4/W8 and no diagonal meadow selector remains.',
      has(P, 'N4 = 1, E4 = 2, S4 = 4, W4 = 8', 'VelvetAisleBarrier', 'CarvedStageBalustrade', 'PropShelfRun', 'VelvetCurtainScreen', 'BrassFootlightStrip', 'IvyPlanterStrip')
      and not any(t in source.get(P, '') for t in ['N8 =', 'NE8 =', 'MeadowMask', 'RaisedFlowerMeadow']))
check('S10', 'Neighbor connection identity includes exact family + connectionGroup + logical layer + integer height plane.',
      has(P, 'new LayerCell(x, y, row.logicalLayer, row.heightKey)', 'neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup'))
check('S11', 'All group cells are validated atomically; duplicate placement IDs, overlapping claims or host rejection invalidate the whole snapshot.',
      has(P, 'for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)', '!placementIds.Add(row.placementId)', '!occupied.Add(key)', 'hostAllowDecorCell(claim)', 'no partial group may be placed.'))
check('S12', 'Exactly12 large +8 point group keys and declared dimensions are separate from sprite canvas size.',
      has(P, 'TryGetDeclaredGroupSize(', 'grand_piano_3x2', 'ticket_booth_2x2', 'stage_steps_2x1', 'flower_urn', 'IsPointGroup(')
      and has(K, 'groups.Length != 20', 'sprites.Length < 116', 'sprites.Length > 118')
      and has(B, 'input.groups.Length == 20', 'Group/point footprint/kind differs from its exact art metadata.'))
check('S13', 'Groups keep one logical center and renderer; uniform world cell scale never alpha-fits or canvas-fits one tile.',
      has(C, 'WorldPosition(worldOrigin, row.CenterX, row.CenterY', 'foreach (PendingSprite row in pending)', 'cellSize / parentScale.x, localY = cellSize / parentScale.y')
      and has(P, 'widthCells * .5', 'heightCells * .5'))
check('S14', 'Old owned sprites are hidden/cleared before deferred Destroy; replaced and empty snapshots cannot double-render.',
      has(C, 'renderer.enabled = false; renderer.sprite = null;', 'ClearOwned(); owned = next;', 'if (Application.isPlaying) UnityEngine.Object.Destroy(node); else UnityEngine.Object.DestroyImmediate(node);'))
check('S15', 'Null/invalid new config persists attempted values and clears old owned art, rather than restoring old config.',
      has(C, 'ClearOwned(); configured = false;', 'catalog = latest;', 'private bool Fail(string error)', 'hasExplicitLayout = false; currentPlan = null; ClearOwned();')
      and source[C].index('ClearOwned(); configured = false;') < source[C].index('catalog = latest;'))
check('S16', 'Inactive pre-Awake configuration survives first enable; ordinary disable resets explicit layout so stale pooled art is not resurrected.',
      has(C, 'private void Awake() { if (!hasExplicitLayout) HideOwned(); }', 'configured && hasExplicitLayout && catalog != null', 'configured = false; hasExplicitLayout = false; currentPlan = null; HideOwned();'))
check('S17', 'Optional posts are first2 families only and require long straight runs with straight neighbors on both sides; stable coordinate phase, no frame Random.',
      has(P, 'return family == VelvetAisleBarrier || family == CarvedStageBalustrade;', 'spacing != 3 && spacing != 4',
          'mask4 != (N4 | S4) && mask4 != (E4 | W4)', 'CardinalMask(same, checked(row.x - dx), checked(row.y - dy)) == mask',
          'CardinalMask(same, checked(row.x + dx), checked(row.y + dy)) == mask')
      and 'Random.' not in strip_literals(source.get(P, '')))
check('S18', 'No collider/rigidbody/input/update/turn authority is added by Planner or Controller.',
      not re.search(r'\b(?:GameObject\.Find|Input\.|Random\.|AddComponent\s*<\s*(?:BoxCollider|PolygonCollider|Collider|Rigidbody)|void\s+(?:Start|Update|FixedUpdate)\s*\()', strip_literals(source.get(C, '') + source.get(P, ''))))
check('S19', 'Row-down cell/group bottom-left helpers and top-left board-origin adapter are explicit and consistent.',
      has(P, 'new TheaterDecorCell(column, checked(-row))', 'checked(-topRow - heightCells + 1)') and has(C, 'boardTopLeft.y - cellSize'))
check('S20', 'Explicit catalog build invalidates importer row cache before reimports, including copies retaining timestamps/length.',
      has(I, 'public static void InvalidateRoot(string root) { ByRoot.Remove(root); }')
      and has(B, PREFIX + 'SpriteImporter.InvalidateRoot(root)', 'AssetDatabase.ImportAsset(path,')
      and source[B].index(PREFIX + 'SpriteImporter.InvalidateRoot(root)') < source[B].index('AssetDatabase.ImportAsset(path,'))
check('S21', 'Large/point group key is bound to art semantic family in both source preflight and loaded catalog.',
      has(B, 'art.family == group.key') and has(K, 'art.Family != group.Key'))
check('S22', 'JSON integer fields reject fractional/exponent literals before JsonUtility can coerce to int.',
      has(J, 'Integer(root.fields["schemaVersion"])', 'Integer(row.fields["width"])', 'Integer(row.fields["height"])',
          'foreach (string key in new[] { "logicalRect", "footprintCells" })', 'Expected an exact integer JSON literal.'))

test_file = next((f for f in source if f.startswith('Tests~') and f.endswith('PlannerTests.cs')), None)
tests = re.findall(r'\[Test\]\s+public void (\w+)\(', source.get(test_file, ''))
failed = [c for c in checks if c['result'].startswith('FAIL')]
report = {'status': 'PASS_SOURCE_REVIEW_ONLY' if not failed else 'FAIL_SOURCE_REVIEW',
          'scope': 'Independent reading and lexical/pattern checks only. No C# compilation, Unity import, NUnit, controller execution, scene loading or host gameplay was run.',
          'sourceFilesReviewed': len(files), 'files': [{'file': p.relative_to(ROOT).as_posix(), 'sha256': sha(p), 'lines': len(p.read_text().splitlines())} for p in files],
          'checks': checks, 'checkCount': len(checks), 'openFindings': failed,
          'resolvedFindings': [{'id': 'R01', 'finding': 'Group semantic family was not bound to group key.',
                                'resolution': 'Both current manifest builder and current catalog validate exact family/key correspondence.',
                                'status': 'RESOLVED_SOURCE_REVIEW_ONLY'}],
          'optionalNUnitSource': {'file': test_file, 'testMethods': tests, 'count': len(tests), 'execution': 'NOT_RUN'},
          'compilerAvailability': {n: shutil.which(n) for n in ['Unity', 'dotnet', 'csc', 'mcs']},
          'actualCSharpCompile': 'NOT_RUN', 'actualUnityImport': 'NOT_RUN', 'actualNUnit': 'NOT_RUN',
          'actualPlayMode': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN'}
(OUT / 'runtime_source_review.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'sourceFiles': len(files), 'sourceChecks': len(checks), 'openFindings': [f['id'] for f in failed], 'optionalNUnitTestsNOT_RUN': len(tests)}))
