from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import hashlib
import importlib.util
import json
import shutil

W=Path(__file__).resolve().parent
PROJECT=W.parents[2]
OUT=PROJECT/'output'
ID='ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
P=OUT/ID
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
spec=importlib.util.spec_from_file_location('verify_completion',W/'verify_completion.py');v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
before=v.verify(P)
archive=OUT/(ID+'.zip')
with ZipFile(archive,'w',ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(P.rglob('*')):
        if p.is_file():z.write(p,ID+'/'+p.relative_to(P).as_posix())
with ZipFile(archive) as z:
    assert z.testzip() is None
    extract=W/'release_extract'
    if extract.exists():shutil.rmtree(extract)
    extract.mkdir();z.extractall(extract);entry_count=len(z.infolist())
after=v.verify(extract/ID)
original={p.relative_to(P).as_posix():sha(p) for p in P.rglob('*') if p.is_file()}
extracted={p.relative_to(extract/ID).as_posix():sha(p) for p in (extract/ID).rglob('*') if p.is_file()}
assert original==extracted
cli=OUT/(ID+'_CLI_KO.txt');shutil.copy2(P/'UNITY_CLI_KO.txt',cli)
guide=OUT/(ID+'_COMPLETION_R2_KO.txt');shutil.copy2(P/'COMPLETION_R2_KO.txt',guide)
images=[]
for src_name,dest_suffix in [('ALL26_BOARD.png','_ALL26_BOARD.png'),('ALL26_BOARD_GRID.png','_ALL26_BOARD_GRID.png')]:
    dest=OUT/(ID+dest_suffix);shutil.copy2(W/'preview_exports'/src_name,dest);images.append(dest)
qa=OUT/(ID+'_DELIVERY_QA.json')
write(qa,{'status':'PASS_OFFLINE_COMPLETION_R2_DELIVERY','installationRevision':2,'archiveBytes':archive.stat().st_size,
          'archiveSha256':sha(archive),'archiveEntries':entry_count,'zipCrc':'PASS','extractedExactFileSetAndSha256':True,
          'before':before,'afterIndependentExtraction':after,
          'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualTestRunner':'NOT_RUN',
          'actualGeneratedPrefabOrScene':'NOT_RUN','actualProductionHostBindings':'NOT_RUN','actualUnityPlay':'NOT_RUN'})
checksums=OUT/(ID+'_DOWNLOAD_CHECKSUMS.txt')
download_files=[archive,cli]
for suffix in ['_CATALOG_26.png','_PREVIEW.png','_GRID.png','_CONNECTIONS.png','_FOOTPRINTS.png','_EDIT_REPLAY.gif']:
    download_files.append(OUT/(ID+suffix))
download_files += [qa,guide]+images
checksums.write_text('\n'.join(sha(p)+'  '+p.name for p in download_files)+'\n')
ids=json.loads((W/'delivery_library_identities.private.json').read_text())
replacements=[(archive,ids['zip']),(cli,ids['cli']),(qa,ids['delivery_qa']),(checksums,ids['download_checksums'])]
uploads=[{'local_path':str(p),'purpose':'replace_library_file','library_file_id':identity,
          'version_reason':'잔영 극장 배치 연결 보완 R2: 실제 호출부·typed 보드·생성기 추가, 기존 아트 유지'} for p,identity in replacements]
uploads += [{'local_path':str(p),'purpose':'create_library_file','library_artifact_type':'image'} for p in images]
uploads.append({'local_path':str(guide),'purpose':'create_library_file','library_artifact_type':'other'})
request=W/'delivery_upload_request.private.json';write(request,{'uploads':uploads});request.chmod(0o600)
write(W/'release_delivery_result.json',{'files':[{'name':p.name,'bytes':p.stat().st_size,'sha256':sha(p)} for p in [archive,cli,qa,checksums]+images+[guide]],'verification':after})
print(json.dumps({'files':[{'name':p.name,'bytes':p.stat().st_size} for p in [archive,cli,qa,checksums]+images+[guide]],'archiveEntries':entry_count,'archiveSha256':sha(archive),'verification':after},ensure_ascii=False,indent=2))
