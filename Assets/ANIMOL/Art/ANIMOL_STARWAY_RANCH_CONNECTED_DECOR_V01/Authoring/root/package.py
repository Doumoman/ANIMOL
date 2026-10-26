"""Freeze, archive, and independently extract the additive decoration pack."""
from pathlib import Path
import json,hashlib,zipfile,shutil
R=Path('/workspace/scratch/b0cb6eab55f7');W=R/'work/ranch_connected_v01';ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01';PREFIX=ID;P=R/'output'/ID;O=R/'output'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def cp(a,b):b.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(a,b)
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
for directory in ['audit_exports','runtime_review_exports','runtime_exports','handoff_exports','preview_exports']:
 for f in (W/directory).glob('*.json'):cp(f,P/'QA'/(directory.replace('_exports','')+'_'+f.name))
for n in ['README_KO.txt','UNITY_CLI_KO.txt']:cp(W/'handoff_exports'/n,P/n)
cp(W/'verify_addon.py',P/'QA/verify_addon.py')
for f in (W/'audit_exports').glob('*.py'):cp(f,P/'QA'/f.name)
for f in (W/'runtime_review_exports').glob('*.py'):cp(f,P/'QA'/f.name)
for f in (W/'runtime_exports').glob('*.txt'):cp(f,P/'Documentation'/f.name)
for f in ['assemble.py','package.py']:cp(W/f,P/'Authoring/root'/f)
for f in (W/'preview_exports').glob('*'):
 if f.is_file()and f.suffix.lower()in('.png','.gif','.json'):cp(f,P/'Inspection/examples'/f.name)
for f in (W/'preview_exports').glob('*.py'):cp(f,P/'Authoring/preview'/f.name)
for f in (W/'preview_exports').glob('*.txt'):cp(f,P/'Documentation'/f.name)
if (W/'preview_exports/example_layout.json').exists():cp(W/'preview_exports/example_layout.json',P/'example_layout.json')
# A user-facing editing pack must preserve actual prompts, source parts and production code.
write(P/'download_parts.json',{'packageRoot':ID,'ingameArchive':PREFIX+'_INGAME.zip','sourcesArchive':PREFIX+'_SOURCES.zip','fullArchive':ID+'.zip','instructions':'INGAME 하나가 설치용 전체 장식 팩이다. SOURCES는 편집용 선택 파일이며 같은 루트로 합칠 수 있다. 기존 목장 전체 팩을 다시 덮어쓰지 않는다.','actualUnityImportCompilePlay':'NOT_RUN'})
def optional(f):
 p=f.relative_to(P).parts
 return p[0]in('sources','production','Authoring')or('draft'in f.name.lower()and p[0]=='Inspection')
def runtime_files():return[f for f in sorted(P.rglob('*'))if f.is_file()and not optional(f)and f.name not in('runtime_checksums_sha256.txt','checksums_sha256.txt')and f.suffix.lower()!='.gif']
for f in P.rglob('*'):
 if f.is_file():assert f.stat().st_size>0 and '__pycache__'not in f.parts and f.suffix!='.pyc',f
(P/'runtime_checksums_sha256.txt').write_text(''.join(sha(f)+'  '+f.relative_to(P).as_posix()+'\n'for f in runtime_files()))
allfiles=[f for f in sorted(P.rglob('*'))if f.is_file()and f.name!='checksums_sha256.txt']
(P/'checksums_sha256.txt').write_text(''.join(sha(f)+'  '+f.relative_to(P).as_posix()+'\n'for f in allfiles))
allfiles=[f for f in sorted(P.rglob('*'))if f.is_file()]
def zipit(name,files):
 p=O/name
 with zipfile.ZipFile(p,'w',zipfile.ZIP_DEFLATED,compresslevel=6)as z:
  for f in files:z.write(f,ID+'/'+f.relative_to(P).as_posix())
 with zipfile.ZipFile(p)as z:
  assert z.testzip()is None;names=z.namelist();assert len(names)==len(set(names))==len({x.casefold()for x in names});assert all(n.startswith(ID+'/')and'..'not in Path(n).parts for n in names)
 return {'file':name,'sizeBytes':p.stat().st_size,'entries':len(files),'sha256':sha(p)}
core=[f for f in allfiles if not optional(f)and f.suffix.lower()!='.gif'and f.name!='checksums_sha256.txt']
sources=[f for f in allfiles if optional(f)]
reports=[zipit(PREFIX+'_INGAME.zip',core),zipit(PREFIX+'_SOURCES.zip',sources),zipit(ID+'.zip',allfiles)]
assert all(r['sizeBytes']<50*1024*1024 for r in reports[:2]),reports
dest=W/'release_extract'
dest.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(O/(PREFIX+'_INGAME.zip'))as z:z.extractall(dest)
count=0
for line in (dest/ID/'runtime_checksums_sha256.txt').read_text().splitlines():
 h,f=line.split('  ',1);assert sha(dest/ID/f)==h,f;count+=1
for name in ['RANCH_CONNECTED_DECOR_PREVIEW.png','RANCH_CONNECTED_DECOR_GRID.png','RANCH_CONNECTED_DECOR_CONNECTIONS.png','RANCH_CONNECTED_DECOR_EDIT_REPLAY.gif','RANCH_CONNECTED_DECOR_BEFORE_AFTER.png']:
 f=W/'preview_exports'/name
 if f.exists():cp(f,O/(PREFIX+'_'+name.replace('RANCH_CONNECTED_DECOR_','')))
cp(P/'UNITY_CLI_KO.txt',O/(PREFIX+'_CLI_KO.txt'))
write(O/(PREFIX+'_DELIVERY_QA.json'),{'status':'PASS_OFFLINE_ARCHIVES_AND_EXTRACTED_CHECKSUMS','runtimeFilesVerified':count,'archives':reports,'originalBaseNotOverwritten':True,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN'})
(O/(PREFIX+'_DOWNLOAD_CHECKSUMS.txt')).write_text(''.join(r['sha256']+'  '+r['file']+'\n'for r in reports))
print(json.dumps({'status':'PACKAGED','archives':reports,'extracted':str(dest/ID),'runtimeChecksumsVerified':count},ensure_ascii=False))
