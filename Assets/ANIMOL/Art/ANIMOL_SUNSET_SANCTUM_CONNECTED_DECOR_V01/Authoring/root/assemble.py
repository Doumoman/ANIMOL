"""Assemble owner exports without modifying original theme assets or game authority."""
from pathlib import Path
from PIL import Image
import hashlib,json,shutil,subprocess,sys
W=Path(__file__).resolve().parent;R=W.parents[1];ID='ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01'
S=W/'package_staging'/ID;S.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
def copy(p,q):q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
def tree(p,q):
    if not p.exists():return
    for f in p.rglob('*'):
        if f.is_file() and '__pycache__'not in f.parts and f.suffix!='.pyc' and '.private.'not in f.name:copy(f,q/f.relative_to(p))
owners=['networks_a_exports','networks_b_exports','groups_a_exports','groups_b_exports']
assets=[];groups=[];source_rows=[];paths=set();sources={};producer=[]
for owner in owners:
    D=W/owner;p=D/'manifest.json';assert p.exists(),owner+' not ready'
    m=json.loads(p.read_text());assert m['packageId']==ID
    producer.append(dict(producer=owner,manifestSha256=sha(p)))
    copy(p,S/'QA'/('producer_'+owner+'_manifest.json'))
    for row in m['assets']:
        a=dict(row);f=a['file'];assert f.startswith('decor/')and'..'not in Path(f).parts and f.casefold()not in paths
        paths.add(f.casefold());assert sha(D/f)==a['sha256'],f
        if not a.get('sourceFiles'):
            t=a.get('source_file')or a.get('sourceNative')or a.get('sourceFile')
            a['sourceFiles']=t if isinstance(t,list)else[t]
        assert all(s and(D/s).is_file()for s in a['sourceFiles']),f
        a['deriveColliderFromAlpha']=False;a['noGameplayColliderOrCost']=True
        copy(D/f,S/f);assets.append(a)
    groups+=m.get('groups',[])
    for p in(D/'sources').glob('*.png'):
        f=p.relative_to(D).as_posix();dg=sha(p)
        assert f not in sources or sources[f]==dg,'source name collision'
        if f in sources:continue
        sources[f]=dg;im=Image.open(p);im.load()
        source_rows.append(dict(file=f,sha256=dg,width=im.width,height=im.height,generatedBy='built-in imagegen',runtimeSelectable=False,producer=owner));copy(p,S/f)
    for p in D.iterdir():
        if p.is_file()and p.name!='manifest.json'and p.suffix in['.py','.txt','.json','.md']:
            copy(p,S/'Authoring'/owner/p.name)
            if p.suffix=='.json'and('qa'in p.name.lower()or'frozen'in p.name.lower()):copy(p,S/'QA'/owner/p.name)
    for name in['production','Authoring','authoring','components']:tree(D/name,S/'Authoring'/owner/name)
    for name in['Inspection','inspection']:tree(D/name,S/'Inspection'/owner)
    for name in['QA','qa']:tree(D/name,S/'QA'/owner)
assets.sort(key=lambda a:a['file']);groups.sort(key=lambda a:a['key'])
assert len(assets)==118 and len(groups)==20
m=dict(schemaVersion=1,packageId=ID,themeDisplayName='낙일 성소',postStoryDisplayName='천구 날조국',
    decorationFamilyCountExcludesMaskVariants=26,networkFamilyCount=6,multiCellGroupCount=12,pointPropCount=8,
    neighborBits=dict(N=1,E=2,S=4,W=8),coordinateConvention='+Y north; logicalRect image top-left; Unity pivot bottom-left; group anchor bottom-left cell',
    nativeImageGeneration='built-in imagegen; native sources/full prompts included',gameplayAuthorityModified=False,
    actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualHostBindings='NOT_RUN',actualUnityPlay='NOT_RUN',assets=assets,groups=groups)
write(S/'manifest.json',m);PIN=sha(S/'manifest.json');(S/'manifest_sha256.txt').write_text(PIN+'\n');(W/'final_manifest_sha256.txt').write_text(PIN+'\n')
write(S/'native_sources_manifest.json',dict(sources=source_rows));write(S/'producer_manifest_pins.json',dict(producers=producer))
write(S/'sprite_import_settings.json',dict(packageId=ID,onlyRuntimeManifestAllowlist=True,defaultTextureTypeForUndeclaredImages='Default',
    textureType='Sprite',spriteImportMode='Single',meshType='FullRect',filterMode='Bilinear',wrapMode='Clamp',compression='Uncompressed',mipMaps=False,
    npotScale='None',generateFallbackPhysicsShape=False,atlasAllowRotation=False,atlasTightPacking=False,atlasSettingsRequireActualHostValidation=True,
    sprites=[{k:a[k]for k in['file','width','height','ppu','pivot','logicalRect','footprintCells','sha256']}for a in assets]))
ref=W/'reference_download/ANIMOL/ANIMOL_SUNSET_SANCTUM_CARTOON_V02.zip'
write(S/'baseline_reference.json',dict(libraryFileId='libfile_7d77057f38548191af2f740a6e40639c',version=2,filename=ref.name,sha256=sha(ref),
    runtimePngCopiedFromBaseline=0,role='Read-only actual Sanctum V02 style and metadata reference',oldPropsFileCount=12,alreadyMultiCellProps=3,
    gameplayProjectAvailable=False,doNotReinstallOldFullBaseline=True,materialLegend=dict(G='sanctum_floor',W='wood_bridge',S='stone_bridge')))
if '--art-only'in sys.argv:
    print(json.dumps(dict(stage=str(S),manifestSha256=PIN,runtimePngCount=len(assets),nativeSources=len(source_rows))));sys.exit()
for script in['build_examples.py','build_docs.py','build_previews.py']:subprocess.run([sys.executable,str(W/script)],check=True,stdout=subprocess.DEVNULL)
rt=W/'runtime_exports'
assert PIN in(rt/'Runtime/SanctumConnectedDecorV01BoardSnapshot.cs').read_text(),'runtime final pin not frozen'
for name in['Runtime','Editor','Tests~']:tree(rt/name,S/name)
for p in rt.iterdir():
    if p.is_file()and p.suffix in['.txt','.json','.md']:copy(p,S/('QA'if p.suffix=='.json'else'Documentation')/p.name)
tree(rt/'QA',S/'QA/runtime')
tree(W/'examples_exports/Examples',S/'Examples');copy(W/'examples_exports/typed_examples_qa.json',S/'QA/typed_examples_qa.json')
tree(W/'handoff_exports',S);tree(W/'preview_exports',S/'Inspection/examples')
copy(W/'CONTRACT.txt',S/'Documentation/ART_AND_RUNTIME_CONTRACT.txt');copy(W/'catalog_design.json',S/'QA/approved_catalog_design.json')
for name in['assemble.py','build_docs.py','build_examples.py','build_previews.py']:copy(W/name,S/'Authoring/root'/name)
tree(W/'audit_exports',S/'QA/independent')
write(S/'delivery_status.json',dict(packageId=ID,installationRevision=1,currentArtManifestSha256=PIN,
    deliveredScope='New26 connected-decoration designs, native source,118 runtimePNG,complete binding/source/typedboard/editor builder code; offline art/data/source QA.',
    actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualNUnit='NOT_RUN',actualPrefabSceneGeneration='NOT_RUN',actualProductionHostAdapter='NOT_RUN',actualUnityPlay='NOT_RUN',actualReachableLegacyZero='NOT_RUN'))
def checksums():
    rows=[sha(p)+'  '+p.relative_to(S).as_posix()for p in sorted(S.rglob('*'))if p.is_file()and p.name!='package_checksums_sha256.txt']
    (S/'package_checksums_sha256.txt').write_text('\n'.join(rows)+'\n');return len(rows)
n=checksums();print(json.dumps(dict(stage=str(S),manifestSha256=PIN,runtimePngCount=len(assets),nativeSources=len(source_rows),checksumEntries=n)))
