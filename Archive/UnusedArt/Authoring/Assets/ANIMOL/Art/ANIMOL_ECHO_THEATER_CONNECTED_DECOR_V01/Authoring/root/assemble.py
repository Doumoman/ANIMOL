"""Assemble frozen native-art exports into one additive Unity package."""
from pathlib import Path
from PIL import Image
import hashlib
import importlib.util
import json
import shutil

W = Path(__file__).resolve().parent
R = W.parents[1]
ID = 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
O = R / 'output' / ID
PRODUCERS = ['networks_a_exports', 'networks_b_exports', 'groups_a_exports', 'groups_b_exports']

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def write(p, data):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def copy(src, dst):
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)

def copy_tree(src, dst):
    if src.exists():
        for f in src.rglob('*'):
            if f.is_file() and '__pycache__' not in f.parts and not f.name.endswith('.pyc'):
                copy(f, dst / f.relative_to(src))

for name in PRODUCERS:
    assert (W / name / 'manifest.json').is_file(), ('producer not ready', name)
assert (W / 'preview_exports' / 'example_layout.json').is_file(), 'preview not ready'

stage = W / 'package_staging' / ID
if stage.exists():
    shutil.rmtree(stage)
stage.mkdir(parents=True)
assets, groups, sources, producer_pins = [], [], {}, []
asset_paths, case_paths = set(), set()
for name in PRODUCERS:
    p = W / name
    mpath = p / 'manifest.json'
    manifest = json.loads(mpath.read_text())
    producer_pins.append({'producer': name, 'manifestSha256': sha(mpath)})
    copy(mpath, stage / 'QA' / ('producer_' + name + '_manifest.json'))
    for a in manifest['assets']:
        a = dict(a)
        rel = a['file']
        assert rel.startswith('decor/') and '..' not in Path(rel).parts
        assert rel not in asset_paths and rel.casefold() not in case_paths
        asset_paths.add(rel)
        case_paths.add(rel.casefold())
        assert sha(p / rel) == a['sha256'], ('stale producer PNG', rel)
        if not a.get('sourceFiles'):
            source = a.get('source_file') or a.get('sourceNative') or a.get('sourceFile')
            a['sourceFiles'] = source if isinstance(source, list) else [source]
        assert a['sourceFiles'] and all(s and (p / s).is_file() for s in a['sourceFiles']), rel
        copy(p / rel, stage / rel)
        assets.append(a)
    groups.extend(manifest.get('groups', []))
    for source in (p / 'sources').rglob('*.png'):
        rel = source.relative_to(p).as_posix()
        if rel in sources:
            assert sources[rel]['sha256'] == sha(source), ('source name collision', rel)
            continue
        with Image.open(source) as im:
            im.load()
            width, height = im.size
        sources[rel] = {'file': rel, 'sha256': sha(source), 'width': width, 'height': height,
                        'generatedBy': 'built-in imagegen', 'runtimeSelectable': False, 'producer': name}
        copy(source, stage / rel)
    for folder in ['production', 'Authoring', 'authoring', 'components']:
        copy_tree(p / folder, stage / 'Authoring' / name / folder)
    for folder in ['inspection', 'Inspection']:
        copy_tree(p / folder, stage / 'Inspection' / name)
    for folder in ['qa', 'QA']:
        copy_tree(p / folder, stage / 'QA' / name)
    for f in p.iterdir():
        if not f.is_file() or f.name == 'manifest.json':
            continue
        if f.suffix in ['.json', '.txt', '.py', '.md']:
            copy(f, stage / 'Authoring' / name / f.name)
        if f.suffix == '.json' and ('qa' in f.name.lower() or 'frozen' in f.name.lower()):
            copy(f, stage / 'QA' / (name + '_' + f.name))

assets.sort(key=lambda a: a['file'])
groups.sort(key=lambda g: g['key'])
manifest = {'schemaVersion': 1, 'packageId': ID, 'themeDisplayName': '잔영 극장',
            'decorationFamilyCountExcludesMaskVariants': 26,
            'networkFamilyCount': 6, 'multiCellGroupCount': 12, 'pointPropCount': 8,
            'neighborBits': {'N': 1, 'E': 2, 'S': 4, 'W': 8},
            'coordinateConvention': '+Y North; logicalRect uses image top-left; Unity pivot normalized bottom-left; group anchor is bottom-left cell.',
            'nativeImageGeneration': 'built-in imagegen, native sources and prompts included',
            'gameplayAuthorityModified': False, 'actualUnityImport': 'NOT_RUN',
            'actualCSharpCompile': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN', 'actualUnityPlay': 'NOT_RUN',
            'assets': assets, 'groups': groups}
write(stage / 'manifest.json', manifest)
(stage / 'manifest_sha256.txt').write_text(sha(stage / 'manifest.json') + '\n')
write(stage / 'native_sources_manifest.json', {'sources': sorted(sources.values(), key=lambda a: a['file'])})
write(stage / 'producer_manifest_pins.json', {'producers': producer_pins})
write(stage / 'sprite_import_settings.json', {
    'packageId': ID, 'defaultTextureTypeForUndeclaredImages': 'Default', 'onlyRuntimeManifestAllowlist': True,
    'textureType': 'Sprite', 'spriteImportMode': 'Single', 'meshType': 'FullRect', 'filterMode': 'Bilinear',
    'wrapMode': 'Clamp', 'compression': 'Uncompressed', 'mipMaps': False, 'npotScale': 'None',
    'generateFallbackPhysicsShape': False, 'atlasAllowRotation': False, 'atlasTightPacking': False,
    'atlasSettingsRequireActualHostValidation': True,
    'sprites': [{k: a[k] for k in ['file', 'width', 'height', 'ppu', 'pivot', 'logicalRect', 'footprintCells', 'sha256']} for a in assets]})

rt = W / 'runtime_exports'
for folder in ['Runtime', 'Editor', 'Tests~']:
    copy_tree(rt / folder, stage / folder)
for f in rt.glob('*'):
    if f.is_file():
        folder = 'QA' if f.suffix in ['.json', '.py'] else 'Documentation'
        copy(f, stage / folder / f.name)
for name in ['README_KO.txt', 'UNITY_CLI_KO.txt', 'decoration_catalog.json']:
    copy(W / 'handoff_exports' / name, stage / name)
copy(W / 'CONTRACT.txt', stage / 'Documentation' / 'ART_AND_RUNTIME_CONTRACT.txt')
copy(W / 'verify_addon.py', stage / 'QA' / 'verify_addon.py')
copy(W / 'assemble.py', stage / 'Authoring' / 'root' / 'assemble.py')
copy(W / 'build_handoff.py', stage / 'Authoring' / 'root' / 'build_handoff.py')
copy_tree(W / 'audit_exports', stage / 'QA' / 'independent')
preview = W / 'preview_exports'
for f in preview.rglob('*'):
    if not f.is_file() or '__pycache__' in f.parts:
        continue
    rel = f.relative_to(preview)
    if f.suffix in ['.png', '.gif']:
        copy(f, stage / 'Inspection' / 'examples' / rel)
    elif f.suffix == '.py':
        copy(f, stage / 'Authoring' / 'preview' / rel)
    else:
        copy(f, stage / 'QA' / ('preview_' + str(rel).replace('/', '_')))
copy(preview / 'example_layout.json', stage / 'example_layout.json')

refzip = W / 'reference_download/ANIMOL/ANIMOL_ECHO_THEATER_CARTOON_V03.zip'
write(stage / 'baseline_reference.json', {
    'libraryFileId': 'libfile_e0e2c61d458481918f21fe2f7b60cae7', 'version': 3,
    'filename': refzip.name, 'sha256': sha(refzip), 'runtimePngCopiedFromBaseline': 0,
    'role': 'Read-only style, terrain/background preview context and prior decoration metadata.',
    'oldPropsFileCount': 14, 'alreadyMultiCellProps': 6,
    'gameplayProjectAvailable': False, 'doNotReinstallOldFullBaseline': True})

spec = importlib.util.spec_from_file_location('validator', W / 'verify_addon.py')
v = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v)
checked = v.verify(stage)
write(stage / 'QA' / 'assembled_offline_qa.json', checked)
checksums = []
for p in sorted(stage.rglob('*')):
    if p.is_file():
        checksums.append(sha(p) + '  ' + p.relative_to(stage).as_posix())
(stage / 'package_checksums_sha256.txt').write_text('\n'.join(checksums) + '\n')
final = v.verify(stage)
if O.exists():
    shutil.rmtree(O)
shutil.copytree(stage, O)
write(W / 'assembled_verification.json', final)
print(json.dumps({'package': str(O), 'manifestSha256': sha(O / 'manifest.json'), 'verification': final}, indent=2))
