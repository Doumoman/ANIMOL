from pathlib import Path
import hashlib
import importlib.util
import json
import shutil

W=Path(__file__).resolve().parent
PROJECT=W.parents[2]
BASE=W/'baseline_package'
ID='ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
STAGE=W/'package_staging'/ID
OUT=PROJECT/'output'/ID
PIN='8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def copy(s,d):d.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(s,d)
def tree(s,d):
    for p in s.rglob('*'):
        if p.is_file() and '__pycache__' not in p.parts and '.writing.' not in p.name:copy(p,d/p.relative_to(s))

for name in ['runtime_exports','editor_exports']:
    assert (W/name/'EXPORT_FROZEN.json').is_file(),('not frozen',name)
assert (W/'audit_exports/COMPLETION_AUDIT_FROZEN.json').is_file(),'independent audit not frozen'
assert sha(BASE/'manifest.json')==PIN
if STAGE.exists():shutil.rmtree(STAGE)
shutil.copytree(BASE,STAGE)
(STAGE/'package_checksums_sha256.txt').unlink()
core=[]
for folder in ['Runtime','Editor','Tests~']:
    for p in sorted((BASE/folder).glob('*.cs')):
        core.append({'file':p.relative_to(BASE).as_posix(),'sha256':sha(p)})
assert len(core)==8
new=[]
for owner in ['runtime_exports','editor_exports']:
    source=W/owner
    for folder in ['Runtime','Editor','Tests~']:
        if not (source/folder).exists():continue
        for p in sorted((source/folder).glob('*.cs')):
            rel=p.relative_to(source)
            assert not (STAGE/rel).exists(),('would overwrite core',rel)
            copy(p,STAGE/rel);new.append({'file':rel.as_posix(),'sha256':sha(p),'producer':owner})
    for p in source.iterdir():
        if p.is_file() and p.suffix in ['.json','.py','.txt','.md']:
            copy(p,STAGE/'QA/CompletionR2'/owner/p.name)
tree(W/'examples_exports/Examples',STAGE/'Examples')
copy(W/'examples_exports/typed_examples_qa.json',STAGE/'QA/CompletionR2/typed_examples_qa.json')
tree(W/'docs_exports',STAGE)
tree(W/'preview_exports',STAGE/'Inspection/CompletionR2')
tree(W/'audit_exports',STAGE/'QA/CompletionR2/independent')
copy(W/'verify_completion.py',STAGE/'QA/CompletionR2/verify_completion.py')
for n in ['build_examples.py','build_docs.py','assemble_completion.py','package_completion.py']:
    copy(W/n,STAGE/'Authoring/CompletionR2'/n)
copy(W/'CONTRACT.txt',STAGE/'Documentation/COMPLETION_R2_CONTRACT.txt')
write(STAGE/'completion_source_checksums.json',{'unchangedOriginalCore':core,'newCompletionSources':new})
write(STAGE/'installation_revision.json',{
    'packageId':ID,'installationRevision':2,'artRevision':'V01','artManifestSha256':PIN,
    'decorationDesignCount':26,'runtimePngCount':118,'originalCoreSourceFilesUnchanged':8,
    'newCompletionSourceFiles':len(new),'concreteCallerAdded':True,
    'typedStageAndAll26BoardsIncluded':True,'generatedUnityAssetsBundled':False,
    'scope':'Theater decoration snapshot, binding and Editor generation source completion. Original project adapter still requires actual host code.',
    'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualTestRunner':'NOT_RUN',
    'actualPrefabSceneGeneration':'NOT_RUN','actualProductionHostBindings':'NOT_RUN','actualUnityPlay':'NOT_RUN'})
write(STAGE/'board_sources_manifest.json',{'boards':[{'file':'Examples/'+n,'sha256':sha(STAGE/'Examples'/n),'inspectionOnly':True}
      for n in ['stage_snapshot.json','all26_snapshot.json']]})
for p in core:assert sha(STAGE/p['file'])==p['sha256']
manifest=json.loads((STAGE/'manifest.json').read_text())
for a in manifest['assets']:assert sha(STAGE/a['file'])==a['sha256']
assert sha(STAGE/'manifest.json')==PIN
assert not [p for p in STAGE.rglob('*') if p.suffix in ['.asset','.prefab','.unity']]
sums=[sha(p)+'  '+p.relative_to(STAGE).as_posix() for p in sorted(STAGE.rglob('*')) if p.is_file()]
(STAGE/'package_checksums_sha256.txt').write_text('\n'.join(sums)+'\n')
spec=importlib.util.spec_from_file_location('completion_verify',W/'verify_completion.py');v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
result=v.verify(STAGE)
oldspec=importlib.util.spec_from_file_location('art_verify',BASE/'QA/verify_addon.py');oldv=importlib.util.module_from_spec(oldspec);oldspec.loader.exec_module(oldv)
art=oldv.verify(STAGE)
if OUT.exists():shutil.rmtree(OUT)
shutil.copytree(STAGE,OUT)
write(W/'assembled_completion_verification.json',{'completion':result,'unchangedArtValidation':art})
print(json.dumps({'package':str(OUT),'completion':result,'unchangedArt':art['status']},ensure_ascii=False,indent=2))
