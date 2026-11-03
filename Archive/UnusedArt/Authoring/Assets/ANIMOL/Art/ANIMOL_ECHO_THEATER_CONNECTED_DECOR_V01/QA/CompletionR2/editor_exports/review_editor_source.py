"""Source/lexical contract review only. This does not compile or execute C# or Unity."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil

HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument('--sources', type=Path, default=HERE)
parser.add_argument('--output', type=Path, default=HERE / 'editor_source_review.json')
args = parser.parse_args()
SOURCE = args.sources.resolve()
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

strip = stripped
files = [SOURCE / 'Editor' / ('TheaterConnectedDecorV01' + name + '.cs') for name in ['BoardJson', 'BoardTools', 'BoardWindow']]
files.append(SOURCE / 'Tests~' / 'TheaterConnectedDecorV01BoardJsonTests.cs')
assert all(p.is_file() for p in files), 'Pass --sources with the source package root containing Editor/ and Tests~/.'
texts = {p.name: p.read_text() for p in files}
checks = []
def check(name, value):
    assert value, name
    checks.append({'name': name, 'status': 'PASS_SOURCE_REVIEW_ONLY'})
for p in files:
    clean = strip(p.read_text())
    stack = []
    pairs = {')': '(', ']': '[', '}': '{'}
    for char in clean:
        if char in '([{': stack.append(char)
        elif char in ')]}': assert stack and stack.pop() == pairs[char], p.name
    assert not stack, p.name
    check(p.name + ' has closed literals/comments and balanced lexical delimiters', True)
    check(p.name + ' is Editor/test preprocessor guarded', clean.strip().startswith('#if UNITY_EDITOR') and clean.strip().endswith('#endif'))
j = texts['TheaterConnectedDecorV01BoardJson.cs']
b = texts['TheaterConnectedDecorV01BoardTools.cs']
w = texts['TheaterConnectedDecorV01BoardWindow.cs']
t = texts['TheaterConnectedDecorV01BoardJsonTests.cs']
check('JSON shape guard runs before JsonUtility', j.index('ValidateShape(json);') < j.index('JsonUtility.FromJson'))
check('Exact object field counts and canonical duplicate guard exist', 'node.fields.Count == expected.Length / 2' in j and '!node.fields.ContainsKey(key)' in j)
check('Raw Int32/Int64 and finite float guards exist', all(x in j for x in ['int.TryParse', 'long.TryParse', 'double.IsInfinity', 'float.MaxValue']))
check('Unicode escapes disallow whitespace', 'NumberStyles.AllowHexSpecifier' in j and 'NumberStyles.HexNumber' not in j)
check('Both example JSON inputs preflight before catalog writes', b.index('ReadExample(root, "Stage"') < b.index('BuildFromManifest(root') and b.index('ReadExample(root, "All26"') < b.index('BuildFromManifest(root'))
check('Example flags, current art pin and all26 inventory are checked', all(x in b for x in ['snapshot.isExampleOnly && snapshot.isWholeBoard', 'snapshot.sourceManifestSha256, pin', 'ValidateAll26(all.snapshot)']))
check('Required CLI flags and optional scene opt-in exist', all(x in b for x in ['-theaterDecorManifest', '-theaterDecorStageSnapshot', '-theaterDecorAllSnapshot', '-theaterDecorSortingLayer', '-theaterDecorCreateScenes', 'bool createScenes = false']))
check('Concrete inactive Bind then explicit active Refresh and visible renderer validation exist', b.index('root.SetActive(false)') < b.index('binding.Bind(') < b.index('root.SetActive(true)') < b.index('binding.RefreshNow(') < b.index('PrefabUtility.SaveAsPrefabAsset(') and 'renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null' in b)
check('Generated outputs require own label, root GUID and expected type', all(x in b for x in ['labels.Contains(OwnerLabel)', 'labels.Contains(rootLabel)', 'expectedType.IsInstanceOfType(prior)']))
check('Opened output scenes, prefab stage and dirty assets are rejected', all(x in b for x in ['PrefabStageUtility.GetCurrentPrefabStage()', 'SceneManager.GetSceneAt(i).path', '!EditorUtility.IsDirty(prior)']))
check('Only new additive scene is closed and prior active scene restored', all(x in b for x in ['NewSceneMode.Additive', 'finally', 'SceneManager.SetActiveScene(previous)', 'EditorSceneManager.CloseScene(inspection, true)']) and 'SaveOpenScenes' not in b and 'OpenScene(' not in b)
check('No global asset save, deletion, refresh or project setting mutation', all(x not in b for x in ['AssetDatabase.SaveAssets(', 'AssetDatabase.DeleteAsset(', 'AssetDatabase.Refresh(', 'GraphicsSettings.', 'EditorBuildSettings.']))
check('Optional reflection light is inside saveScene path and reports lighting', b.index('if (saveScene)') < b.index('lighting = CreateInspectionLight(') and 'inspectionLighting = lighting.ToArray()' in b and 'Universal.Light2D' in b and 'MainCamera' not in b)
check('Undo refresh is explicit, scoped and authoring-fingerprint gated', all(x in w for x in ['if (!refreshAfterUndo', 'if (current == observedAuthoringState) return;', 'Fingerprint(target as TheaterConnectedDecorV01Binding)']) and 'FindObjects' not in w)
check('Menu title and runtime API order match source contracts', 'Build example boards…' in w and 'binding.Bind(controller, catalog, pin, sortingLayer, source, out error, true)' in b)
check('Optional tests include Int64 roundtrip and structural negative cases', all(x in t for x in ['9007199254740993', '9223372036854775807', 'WhitespaceInsideUnicodeEscapeRejected', 'NullRequiredArrayRejected', 'FloatOverflowRejected']))
report = {'status': 'PASS_SOURCE_REVIEW_ONLY', 'checks': checks,
          'files': [{'file': str(p.relative_to(SOURCE)), 'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in files],
          'sourceCheckCount': len(checks), 'optionalEditorNUnitCases': 12,
          'compilerAvailable': any(shutil.which(x) for x in ['dotnet','csc','mcs']),
          'actualCSharpCompile': 'NOT_RUN', 'actualUnityImport': 'NOT_RUN', 'actualEditorBuild': 'NOT_RUN',
          'actualGeneratedScenePlay': 'NOT_RUN', 'actualNUnit': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN'}
args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'status': report['status'], 'checks': len(checks), 'sourceFiles': len(files), 'actualUnity': 'NOT_RUN'}))
