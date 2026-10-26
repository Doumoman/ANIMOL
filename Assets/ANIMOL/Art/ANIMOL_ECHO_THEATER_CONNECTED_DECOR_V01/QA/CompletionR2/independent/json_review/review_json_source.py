"""Independent parser/import-chain source review. Not C# compilation/execution."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import shutil

OUT = Path(__file__).resolve().parent
WORK = OUT.parents[1]
EDITOR = WORK / 'editor_exports'
RUNTIME = WORK / 'runtime_exports'
FILES = {'json': EDITOR / 'Editor/TheaterConnectedDecorV01BoardJson.cs',
         'tools': EDITOR / 'Editor/TheaterConnectedDecorV01BoardTools.cs',
         'snapshot': RUNTIME / 'Runtime/TheaterConnectedDecorV01BoardSnapshot.cs',
         'asset': RUNTIME / 'Runtime/TheaterConnectedDecorV01BoardAsset.cs',
         'tests': EDITOR / 'Tests~/TheaterConnectedDecorV01BoardJsonTests.cs'}
S = {k: p.read_text() for k, p in FILES.items()}
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def has(file, *tokens): return all(t in S[file] for t in tokens)
def line(file, token):
    at = S[file].find(token)
    return {'file': str(FILES[file].relative_to(WORK)), 'line': S[file][:at].count('\n') + 1 if at >= 0 else None, 'token': token}
checks = []
def check(id, requirement, passed, refs=()):
    checks.append({'id': id, 'requirement': requirement, 'status': 'PASS_SOURCE_REVIEW_ONLY' if passed else 'FAIL_SOURCE_REVIEW',
                   'evidence': [line(f, t) for f, t in refs]})
check('J01', 'Root and rows must be objects with exact expected key sets.',
      has('json', "node.kind == 'o'", 'node.fields.Count == expected.Length / 2', 'node.fields.TryGetValue(name, out value)'), [('json', 'private static void Fields(')])
check('J02', 'All16root fields are explicit required typed fields before JsonUtility.',
      has('json', 'Fields(root, "schemaVersion"', '"sourceManifestSha256"', '"allowedDecorCells", \'a\'') and S['json'].index('ValidateShape(json);') < S['json'].index('JsonUtility.FromJson<'), [('json', 'Fields(root,')])
check('J03', 'Network8/group8/allowed4 row fields are explicit, including zero-valued coordinates.',
      has('json', 'Fields(row, "x", \'n\', "y", \'n\', "logicalLayer"', '"worldZ", \'n\', "family", \'s\', "connectionGroup", \'s\'', 'Fields(row, "placementId", \'s\', "groupKey", \'s\', "anchorX"'), [('json', 'foreach (Node row in root.fields["networkCells"].items)')])
check('J04', 'Unknown/missing/case-alias field names fail via exact count plus ordinal key lookup.',
      has('json', 'Unknown or missing board JSON field.', 'StringComparer.Ordinal', 'Missing/wrong JSON field:'))
check('J05', 'Escaped keys normalize before duplicate-key lookup; duplicate values cannot overwrite an earlier field.',
      has('json', 'string key = Quoted();', '!node.fields.ContainsKey(key)', 'node.fields.Add(key, Value(depth + 1))'), [('json', 'Duplicate board JSON key:')])
check('J06', 'Arrays are explicitly typed; null/missing arrays and jagged/null/scalar rows fail, while empty arrays remain legal.',
      has('json', '"networkCells", \'a\', "groupPlacements", \'a\', "allowedDecorCells", \'a\'', "if (Take(']')) return node;", 'Require(node != null && node.kind == \'o\'')
      and has('snapshot', 'networkCells == null || groupPlacements == null || allowedDecorCells == null'))
check('J07', 'Int32 coordinates/metadata use raw literal Int32.TryParse with exact signed range and invariant culture.',
      has('json', 'int.TryParse(row.fields[name].scalar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture', 'Int32(row, "x", "y", "logicalLayer", "heightKey", "sortingOrder")'), [('json', 'private static void Int32(')])
check('J08', 'Int64 revision uses raw literal long.TryParse rather than parsing through double.',
      has('json', 'Int64(root, "revision")', 'long.TryParse(row.fields[name].scalar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture')
      and has('snapshot', 'public long revision;'), [('json', 'private static void Int64(')])
check('J09', 'JSON number grammar rejects leading zero, plus sign, malformed fractional/exponent tokens and non-JSON NaN/Infinity.',
      has('json', 'private static readonly Regex Number', 'Require(Number.IsMatch(token)', 'Invalid board JSON scalar.'))
check('J10', 'Float fields must parse invariantly to finite values within float32 maximum range before JsonUtility.',
      has('json', 'double.TryParse(row.fields[name].scalar, NumberStyles.Float, CultureInfo.InvariantCulture', '!double.IsNaN(value)', '!double.IsInfinity(value)', 'value >= -float.MaxValue && value <= float.MaxValue'), [('json', 'private static void Float(')])
check('J11', 'Semantic validation checks exactpackage/art pin/wholeboard/nonnegative revision/positive finitecellSize/postSpacing3or4.',
      has('snapshot', 'packageId != TheaterConnectedDecorV01Catalog.PackageId', 'sourceManifestSha256, ArtManifestSha256', 'revision < 0 || !isWholeBoard', 'cellSize <= 0', 'postSpacing != 3 && postSpacing != 4'), [('snapshot', 'public bool TryValidate(')])
check('J12', 'Literal control characters, invalid escapes, short/non-hex Unicode escapes fail; Unicode Hex parsing excludes whitespace.',
      has('json', 'Require(c >= 32', 'Invalid board JSON escape.', 'cursor + 4 <= text.Length', 'NumberStyles.AllowHexSpecifier')
      and 'NumberStyles.HexNumber' not in S['json'], [('json', 'NumberStyles.AllowHexSpecifier')])
check('J13', 'Parser bounds JSON size/depth, accepts optional initial BOM and rejects trailing data.',
      has('json', 'json.Length > 16 * 1024 * 1024', 'depth <= 32', "text[0] == '\\ufeff'", 'cursor == text.Length', 'Trailing board JSON data.'))
check('J14', 'Parse invokes snapshot semantic validation after strictshape and before returning typed data.',
      has('json', 'snapshot == null || !snapshot.TryValidate(out error)', 'return snapshot;'), [('json', 'snapshot.TryValidate(out error)')])
check('J15', 'Semantic group/family/duplicate/full-footprint checks delegate to the existing purePlanner and exact whitelist.',
      has('snapshot', 'CreateApprovedCellQuery()', 'TheaterConnectedDecorV01Planner.CreatePlan(networkCells, groupPlacements', 'TryGetDeclaredGroupSize(key, out size)', 'Missing or duplicate approved decoration cell/plane.'), [('snapshot', 'TheaterConnectedDecorV01Planner.CreatePlan(')])
check('J16', 'Approved-cell predicate captures private value keys, so caller record mutation cannot grant new cells.',
      has('snapshot', 'var keys = new HashSet<AllowedKey>();', 'new AllowedKey(cell.x, cell.y, cell.logicalLayer, cell.heightKey)', 'return claim => keys.Contains(new AllowedKey('))
check('J17', 'Both example JSON inputs parse/validate/current-pin-check before catalog build or any board asset writes.',
      has('tools', 'new UTF8Encoding(false, true)', 'TheaterConnectedDecorV01BoardJson.Parse(json)', 'snapshot.isExampleOnly && snapshot.isWholeBoard', 'snapshot.sourceManifestSha256, pin')
      and S['tools'].index('var stage = ReadExample(') < S['tools'].index('var all = ReadExample(') < S['tools'].index('CatalogBuilder.BuildFromManifest('), [('tools', 'var stage = ReadExample('), ('tools', 'var all = ReadExample(')])
check('J18', 'BoardAsset setter defensively copies and validates before statecommit; getter validates private copy.',
      has('asset', 'var next = value.DeepCopy();', 'if (!next.TryValidate(out error)) return false;', 'snapshot = next;', 'var copy = snapshot.DeepCopy();', 'if (!copy.TryValidate(out error)) return false;'), [('asset', 'public bool TrySetSnapshot(')])
check('J19', 'Editor save commits only validated typed snapshot and current ownedoutput path; invalid snapshot does not create/save BoardAsset.',
      has('tools', 'PreflightOutputs(example, ownerRootLabel, createScenes)', 'RequireOwnedOrAbsent(example.boardPath', 'if (!output.TrySetSnapshot(example.snapshot, out error))')
      and S['tools'].index('if (!output.TrySetSnapshot(example.snapshot, out error))') < S['tools'].index('if (isNew) AssetDatabase.CreateAsset(output, example.boardPath);'), [('tools', 'private static TheaterConnectedDecorV01BoardAsset SaveBoard(')])
check('J20', 'OptionalEditor NUnit sources cover actual JsonUtilityInt64 roundtrip, malformed Unicode whitespace and exact numeric failures.',
      has('tests', '9007199254740993', '9223372036854775807', 'JsonUtilityPreservesExactInt64Revision', 'WhitespaceInsideUnicodeEscapeRejected', 'RevisionMustBeExactInt64', 'FloatOverflowRejected'))

spec = importlib.util.spec_from_file_location('board_shape_oracle', OUT / 'check_board_json_shape.py')
oracle = importlib.util.module_from_spec(spec);spec.loader.exec_module(oracle)
examples = []
for n in ['stage_snapshot.json', 'all26_snapshot.json']:
    p = WORK / 'examples_exports/Examples' / n
    if not p.exists(): continue
    data = oracle.validate_shape(p.read_text())
    examples.append({'file':str(p.relative_to(WORK)), 'sha256':sha(p), 'status':'PASS_INDEPENDENT_PYTHON_SHAPE_ORACLE',
                     'networkCells':len(data['networkCells']), 'groupPlacements':len(data['groupPlacements']), 'allowedCells':len(data['allowedDecorCells']), 'actualCSharpExecution':'NOT_RUN'})
corpus = json.loads((OUT / 'strict_json_negative_corpus.json').read_text())
test_cases = len(re.findall(r'\[Test\]', S['tests'])) + len(re.findall(r'\[TestCase\(', S['tests']))
failed = [c for c in checks if c['status'].startswith('FAIL')]
report = {'status':'PASS_SOURCE_REVIEW_ONLY' if not failed else 'FAIL_SOURCE_REVIEW',
          'scope':'Independent manual reading and source-contract checks of strictJSON/DTO/asset/importer chain. Python stdlib/Decimal oracle validates data and case expectations; it does not execute delivered C#.',
          'sources':[{'file':str(p.relative_to(WORK)), 'sha256':sha(p), 'lines':len(p.read_text().splitlines())}for p in FILES.values()],
          'checkCount':len(checks), 'checks':checks, 'openFindings':failed,
          'resolvedFindings':[{'id':'JSON01', 'finding':'NumberStyles.HexNumber accepted leading/trailing whitespace insidefourchar Unicodeescape.',
                               'resolution':'Uses NumberStyles.AllowHexSpecifier with existing exactfourchar guard; malformedUnicodewhitespace negative case added.', 'status':'RESOLVED_SOURCE_REVIEW_ONLY'}],
          'negativeCorpus':{'caseCount':corpus['caseCount'], 'status':corpus['status'], 'execution':'Independent Python oracle only; C# NOT_RUN'},
          'examples':examples, 'optionalNUnitCases':test_cases, 'optionalNUnitExecution':'NOT_RUN',
          'compilerAvailability':{n:shutil.which(n)for n in ['Unity','dotnet','csc','mcs']},
          'actualCSharpCompile':'NOT_RUN', 'actualCSharpParserExecution':'NOT_RUN', 'actualUnityImport':'NOT_RUN', 'actualTestRunner':'NOT_RUN'}
(OUT / 'strict_json_source_review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'status':report['status'],'checks':len(checks),'openFindings':[f['id']for f in failed],'resolved':1,'oracleCases':corpus['caseCount'],'examples':len(examples),'optionalNUnitCasesNOT_RUN':test_cases}))
