"""Offline lexical/contract audit only. This does not compile or execute C# or Unity."""
from pathlib import Path
import hashlib
import json
import re
import shutil

HERE = Path(__file__).resolve().parent
FAMILIES = [
    'velvet_aisle_barrier', 'carved_stage_balustrade', 'prop_shelf_run',
    'velvet_curtain_screen', 'brass_footlight_strip', 'ivy_planter_strip',
]
GROUPS = {
    'grand_piano_3x2': [3, 2], 'audience_bench_3x1': [3, 1],
    'ticket_booth_2x2': [2, 2], 'costume_rack_3x1': [3, 1],
    'vanity_dressing_2x1': [2, 1], 'phonograph_table_2x1': [2, 1],
    'scenery_flat_3x1': [3, 1], 'stacked_props_2x2': [2, 2],
    'mask_display_2x1': [2, 1], 'puppet_storage_2x2': [2, 2],
    'backstage_spotlight_duo_2x1': [2, 1], 'stage_steps_2x1': [2, 1],
    'ticket_stand': [1, 1], 'folded_chair': [1, 1], 'music_stand': [1, 1],
    'tall_candelabra': [1, 1], 'mask_pedestal': [1, 1], 'script_pile': [1, 1],
    'sandbag_stack': [1, 1], 'flower_urn': [1, 1],
}

def stripped(source):
    """Erase C# comments/char/string literals while preserving source newlines."""
    chars = list(source)
    i = 0
    def erase(a, b):
        for j in range(a, b):
            if chars[j] not in '\r\n':
                chars[j] = ' '
    while i < len(source):
        start = i
        if source.startswith('//', i):
            end = source.find('\n', i)
            i = len(source) if end < 0 else end
            erase(start, i)
        elif source.startswith('/*', i):
            end = source.find('*/', i + 2)
            assert end >= 0, 'Unclosed block comment'
            i = end + 2
            erase(start, i)
        elif source.startswith('@"', i):
            i += 2
            while True:
                assert i < len(source), 'Unclosed verbatim string'
                if source[i] == '"':
                    if source.startswith('""', i):
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            erase(start, i)
        elif source[i] in '\"\'':
            quote = source[i]
            i += 1
            while True:
                assert i < len(source), 'Unclosed string/character'
                if source[i] == '\\':
                    i += 2
                    continue
                if source[i] == quote:
                    i += 1
                    break
                assert source[i] not in '\r\n', 'Newline in ordinary C# literal'
                i += 1
            erase(start, i)
        else:
            i += 1
    return ''.join(chars)

files = sorted(HERE.glob('Runtime/*.cs')) + sorted(HERE.glob('Editor/*.cs')) + sorted(HERE.glob('Tests~/*.cs'))
source = {str(p.relative_to(HERE)): p.read_text() for p in files}
checks = []
def check(name, condition, detail):
    assert condition, name
    checks.append({'name': name, 'status': 'PASS_SOURCE_REVIEW_ONLY', 'detail': detail})

for file, text in source.items():
    clean = stripped(text)
    stack = []
    matching = {')': '(', ']': '[', '}': '{'}
    for c in clean:
        if c in '([{': stack.append(c)
        elif c in ')]}':
            assert stack and stack.pop() == matching[c], file + ' lexical delimiter mismatch'
    assert not stack, file + ' unclosed lexical delimiter'
    depth = 0
    for line in clean.splitlines():
        if re.match(r'\s*#if\b', line): depth += 1
        elif re.match(r'\s*#endif\b', line): depth -= 1
        assert depth >= 0, file + ' preprocessor mismatch'
    assert depth == 0, file + ' unclosed preprocessor block'
check('eight C# files have closed literals/comments and balanced delimiters/directives', len(files) == 8,
      'Lexical sanity only; no C# parser/compiler/type checker was available.')
all_source = '\n'.join(source.values())
runtime = '\n'.join(t for f, t in source.items() if f.startswith('Runtime/'))
planner = source['Runtime/TheaterConnectedDecorV01Planner.cs']
controller = source['Runtime/TheaterConnectedDecorV01Controller.cs']
catalog = source['Runtime/TheaterConnectedDecorV01Catalog.cs']
builder = source['Editor/TheaterConnectedDecorV01CatalogBuilder.cs']
importer = source['Editor/TheaterConnectedDecorV01SpriteImporter.cs']
schema = source['Editor/TheaterConnectedDecorV01JsonSchema.cs']
tests = source['Tests~/TheaterConnectedDecorV01PlannerTests.cs']
check('independent theater namespace and package identity',
      'ANIMOL.Art.TheaterConnectedDecorV01' in all_source and 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01' in catalog,
      'Separate namespace/prefix/catalog; no other-theme code is edited.')
check('no superseded theme or meadow selector residue',
      not re.search(r'Ranch|ranch|cream_fence|meadow_stone_wall|raised_flower_meadow|EightNeighbor|AddMeadowAccent|accentHash', all_source),
      'Six cardinal families only; no inherited 8-neighbor/flower behavior.')
check('six exact network names', all(('"' + family + '"') in planner for family in FAMILIES),
      'Six artwork families; every mask uses N1/E2/S4/W8.')
check('all twenty known group/point keys and fixed footprints', all(('"' + key + '"') in planner for key in GROUPS),
      'Twelve large + eight one-cell props; unknown or changed footprint fails.')
check('family arrays cannot be changed by a caller', 'networkFamilies.Clone()' in planner and 'groupKeys.Clone()' in planner,
      'Public known-key lists return independent array copies.')
check('mask names are exact n00 through n15', 'mask > 15' in planner and 'mask.ToString("D2"' in planner,
      'No diagonal bits or implicit image rotation.')
check('four-part semantic adjacency key', 'neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup' in planner
      and 'new LayerCell(x, y, row.logicalLayer, row.heightKey)' in planner,
      'Family + shared connectionGroup + integer logicalLayer + integer heightKey.')
check('whole-board plan with host eligibility and atomic group claims', 'hostAllowDecorCell == null' in planner
      and 'if (!occupied.Add(key))' in planner and 'for (int y = 0; y < size.height; y++)' in planner,
      'Eligibility query is required before any render construction; host must keep it pure.')
check('duplicate placement identity and source footprint rejected', '!placementIds.Add(row.placementId)' in planner
      and 'size.width != expected.width || size.height != expected.height' in planner,
      'One group selection per unique placementId; point props share this API.')
check('optional post requires declared helper', 'familyHasPeriodicPost != null' in planner
      and 'familyHasPeriodicPost(row.family)' in planner and 'periodicPostFamilies.Contains(family)' in controller,
      'Missing optional helper produces no request; it does not trigger a fallback.')
check('posts restricted to two families and guarded straight interiors',
      'family == VelvetAisleBarrier || family == CarvedStageBalustrade' in planner
      and 'CardinalMask(same, checked(row.x - dx)' in planner and 'CardinalMask(same, checked(row.x + dx)' in planner,
      'Both along-axis neighbors must be straight; endpoints and junctions remain clear.')
check('selection deterministic in world coordinates', 'selected.Sort(' in planner and 'groups.Sort(' in planner
      and 'StableHash(seed, 0, 0, layer, height, group)' in planner,
      'Stable post phase with modulo corrected for negative coordinates; no frame randomness.')
check('explicit row-down conversions', 'checked(-row)' in planner and 'checked(-topRow - heightCells + 1)' in planner
      and 'boardTopLeft.y - cellSize' in controller,
      'Group top row is converted to its bottom-left anchor; worldOrigin denotes cell bottom-left.')
check('no gameplay or autonomous frame authority',
      not re.search(r'\b(?:Start|Update|LateUpdate|FixedUpdate)\s*\(', stripped(controller))
      and 'AddComponent<Collider' not in runtime and 'AddComponent<Rigidbody' not in runtime and 'using UnityEditor' not in runtime,
      'Owned static SpriteRenderer construction only; no input, movement, turn advancement or colliders.')
check('one whole group renderer and complete preflight', 'row.CenterX, row.CenterY' in controller
      and controller.index('foreach (PendingSprite row in pending)') > controller.index('plan.GetGroup(i)'),
      'All pending source paths are resolved before rendering; a 3x2 object is not six singleton copies.')
check('world cell scale and non-sheared hierarchy', 'cellSize / parentScale.x' in controller
      and 'cellSize / parentScale.y' in controller and 'node.localRotation' in controller
      and '!Finite(localX)' in controller,
      'Parent positive XY scale is canceled per axis; canvas/alpha bounds never determine scale.')
check('lifecycle fails closed and hides before Destroy', 'private void OnDisable()' in controller
      and 'configured = false; hasExplicitLayout = false; currentPlan = null; HideOwned();' in controller
      and 'HideOwned(); DestroyRenderers(owned)' in controller and 'hasExplicitLayout = false; currentPlan = null; ClearOwned();' in controller,
      'Invalid SetLayout clears previous owned visuals. OnEnable cannot revive a layout disabled earlier.')
check('manifest byte pin plus exact PNG SHA', 'manifest_sha256.txt' in catalog and 'Sha256(bytes)' in builder
      and 'Current PNG SHA mismatch' in builder,
      'Pin validates exact JSON bytes; source assets are checked against their declared byte hashes.')
check('runtime catalog identity and fingerprint', 'ComputeFingerprint()' in catalog and 'localFileId' in catalog
      and 'AssetGuid' in builder and 'actual == row.Sprite && guid == row.AssetGuid && fileId == row.LocalFileId' in builder,
      'Builder gathers actual project GUID/fileID; no fabricated .asset or .meta identities are shipped.')
check('exact group semantic family binding', 'art.Family != group.Key' in catalog and 'art.family == group.key' in builder,
      'A known group key cannot refer to another family or a generic umbrella family.')
check('116 required sprites and at most two optional posts',
      'sprites.Length < 116 || sprites.Length > 118' in catalog and 'groups.Length != 20' in catalog
      and 'allowedFiles.Count != files.Count' in catalog and 'allowedFiles.Count == files.Count' in builder,
      'All96 network masks +20 whole props required; undeclared extra art fails.')
check('exact network registration', 'art.Width == 320 && art.Height == 384' in catalog
      and 'ground.x == 32 && ground.y == 96' in catalog and 'r[0] == 32 && r[1] == 96' in builder,
      '320x384 canvas,256x256 ground; all actual source pivots validated against logical ground centers.')
check('JSON duplicate/type/integer guard', 'Duplicate JSON property' in schema
      and 'Expected an exact integer JSON literal.' in schema and 'Integer(row.fields["width"])' in schema,
      'No silent missing fields, duplicate properties, or fractional/exponent coercion into integer metadata.')
check('new-addon-only importer', '"/" + TheaterConnectedDecorV01Catalog.PackageId + "/"' in importer
      and 'if (!ExactRows(root).TryGetValue(relative, out row)) return;' in importer
      and 'importer.textureType = TextureImporterType.Default;' in importer,
      'Native originals/Inspection/undeclared PNGs remain non-Sprite; latest declared PNGs only.')
check('import settings explicit and no fallback physics', 'SpriteMeshType.FullRect' in importer
      and 'spriteGenerateFallbackPhysicsShape = false' in importer and 'TextureImporterCompression.Uncompressed' in importer
      and 'ClearPlatformTextureSettings(platform)' in importer,
      'Source dimensions/pivots retained; no tight mesh, compression or fallback physics. Existing SpriteAtlas packing must be configured separately by the host.')
check('only owned catalog saved and current cache refreshed', 'AssetDatabase.SaveAssetIfDirty(output)' in builder
      and 'SpriteImporter.InvalidateRoot(root)' in builder and 'AssetDatabase.SaveAssets(' not in builder,
      'No global scene/Prefab/save sweep; callbacks do not start reimport loops.')
check('sixteen optional NUnit contract tests provided', tests.count('[Test]') == 16,
      'NOT_RUN: source tests only, held in Tests~ until the actual host test assembly executes them.')

contract = {
    'packageId': 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01',
    'namespace': 'ANIMOL.Art.TheaterConnectedDecorV01', 'ppu': 256,
    'networkFamilies': FAMILIES, 'networkBits': {'N': 1, 'E': 2, 'S': 4, 'W': 8},
    'networkCanvas': [320, 384], 'networkLogicalRect': [32, 96, 256, 256],
    'networkPivot': [0.5, 160 / 384], 'groupFootprints': GROUPS,
    'requiredSpriteCount': 116, 'optionalPeriodicPostMaximum': 2, 'groupRecordCount': 20,
    'catalogRelativePath': '_Generated/TheaterConnectedDecorV01Catalog.asset',
    'commandLineFlag': '-theaterDecorManifest',
    'executeMethod': 'ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01CatalogBuilder.BuildFromCommandLine',
    'controllerApi': [
        'Configure(catalog, pinnedManifestSha256, actualSortingLayerName)',
        'SetLayout(networkCells, groupPlacements, worldOrigin, cellSize, hostAllowDecorCell, seed=0, postSpacing=4)',
        'ClearLayout()',
    ],
    'failurePolicy': 'Fail closed: rejected Configure/SetLayout clears prior owned visuals. No legacy fallback.',
    'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualTestRunner': 'NOT_RUN',
    'actualHostBindings': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN',
}
(HERE / 'runtime_contract.json').write_text(json.dumps(contract, ensure_ascii=False, indent=2) + '\n')
report = {
    'status': 'PASS_SOURCE_REVIEW_ONLY', 'checksCount': len(checks), 'checks': checks,
    'toolchain': {name: shutil.which(name) for name in ['dotnet', 'csc', 'mcs', 'Unity', 'unity-editor']},
    'limits': 'Lexical/contract review of sources only. No C# parser/compiler or original Unity project; no Unity execution occurred.',
    'optionalNUnitSourceTests': 16, 'actualNUnitExecution': 'NOT_RUN',
    'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN',
    'actualHostBindings': 'NOT_RUN',
    'files': [{'file': str(p.relative_to(HERE)), 'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in files],
}
(HERE / 'source_review.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'sourceChecks': len(checks), 'csFiles': len(files), 'nunitSourceTests': 16, 'actualCompile': 'NOT_RUN', 'actualUnity': 'NOT_RUN'}))
