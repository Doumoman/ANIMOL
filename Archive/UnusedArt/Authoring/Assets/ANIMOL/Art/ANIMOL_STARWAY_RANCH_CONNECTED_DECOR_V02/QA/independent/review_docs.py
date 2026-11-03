"""Read-only handoff/API/data crosscheck; not a Unity command execution."""
from pathlib import Path
import argparse,json,hashlib,re
FIELDS={'schemaVersion','packageId','sourceManifestSha256','boardId','revision','isWholeBoard','isExampleOnly','originX','originY','originZ','cellSize','seed','postSpacing','networkCells','groupPlacements','allowedDecorCells'}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--docs',required=True);ap.add_argument('--source',required=True);ap.add_argument('--examples',required=True);ap.add_argument('--report');a=ap.parse_args();d=Path(a.docs);s=Path(a.source);e=Path(a.examples)
 read=lambda p:p.read_text(encoding='utf-8');cli=read(d/'UNITY_CLI_KO.txt');rm=read(d/'README_KO.txt');alltext=cli+'\n'+rm;bt=read(s/'Editor/RanchConnectedDecorV02BoardTools.cs');bw=read(s/'Editor/RanchConnectedDecorV02BoardWindow.cs');bs=read(s/'Runtime/RanchConnectedDecorV02BoardSnapshot.cs');checks=[]
 def ck(n,b):checks.append(dict(check=n,passed=bool(b)))
 ck('Correct package and namespace in command','ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'in cli and'ANIMOL.Art.RanchConnectedDecorV02'in cli)
 ck('Exact executeMethod and real method declared','ANIMOL.Art.RanchConnectedDecorV02.RanchConnectedDecorV02BoardTools.BuildExamplesFromCommandLine'in cli and'public static void BuildExamplesFromCommandLine()'in bt)
 flags=['-ranchDecorManifest','-ranchDecorStageSnapshot','-ranchDecorAllSnapshot','-ranchDecorSortingLayer','-ranchDecorCreateScenes'];ck('Five actual CLI flags',all(f in cli and'"'+f+'"'in bt for f in flags))
 menu='Tools/ANIMOL/Ranch Connected Decor V02/Build example boards…';ck('Exact actual menu Unicode ellipsis',menu in cli and menu in bw)
 ck('Optional scenes only when requested','true일 때만 검사 .unity'in cli and'bool createScenes = false'in bt)
 ck('Exact sixteen DTO root fields documented',all(f in cli for f in FIELDS))
 ck('Actual binding and publish calls instructed','Binding.Bind(controller,catalog,currentPin,actualSortingLayer,source,out error)'in cli and'PublishedSnapshotSource.PublishSnapshot(snapshot,out error)'in cli)
 ck('Example production opt-in explicit','allowExampleSource'in cli and'false'in cli and'isExampleOnly=false'in cli)
 ck('Pool stale source invalidation/fresh publish documented','freshPublish'in cli and'비활성화'in cli)
 ck('Correct +Y/bottom-left coordinates and PPU','월드+Y=N'in cli and'anchorX+w/2,anchorY+h/2'in cli and'PPU256'in cli)
 ck('All four connection-domain fields',all(f in cli for f in['family','connectionGroup','logicalLayer','heightKey']))
 ck('Exact GWS materials and independent stone support','G=grass,W=wood_bridge,S=stone_bridge'in alltext and'G로 정규화하지 않는다'in cli)
 ck('Chick scale only visual child once','.62배 한번'in cli and'병아리4방향 idle/walk/눈광'in cli)
 ck('Current postV12 gate and wind patches preserved','문/바람·장치 수정본까지 보존'in cli and'한칸 일방향 문/힌지/거절봉·X'in cli and'바람 영역·굴절·셰이더'in cli)
 ck('Static coop/feed/cart not functional devices','병아리 생성기'in cli and'자동매핑하지 않는다'in cli)
 ck('No new moon planet or sun','달·행성·태양 없는 기존 배경'in cli)
 ck('Cardinal5 plus raw256 meadow distinction','4비트 N1E2S4W8,n00..n15'in cli and'N1NE2E4SE8S16SW32W64NW128,n000..n255'in cli and'16/47상태로 canonicalize하지 않는다'in cli)
 ck('Old flower density and variant policy preserved','hash%5==0'in cli and'(hash/5)%4'in cli and'추가 점유/Collider를 만들지 않는다'in cli)
 ck('AllRanch320square exact register','320×320,logicalRect[32,32,256,256],PPU256,pivot[0.5,0.5]'in cli)
 ck('26kind362runtime and old298plus64 accounting','26종/362PNG'in cli and'기존298상대경로'in cli and'추가 경로64개'in cli)
 ck('Readable portable offline audit command','python3 QA/independent/audit_package.py --package'in rm)
 ck('No actual compile/play claim','NOT_RUN'in rm and'실제 Unity 실행 증거가 아니다'in cli)
 ck('No prior theme identifiers leaked',not re.search(r'SanctumConnectedDecor|SUNSET_SANCTUM|ECHO_THEATER|TheaterConnectedDecor|sanctumDecor|theaterDecor|CoastConnectedDecor|coastDecor|STAR_SWELL_COAST',alltext))
 sizes={r['key']:r['footprintCells']for k in['groups','points']for r in json.loads(read(d/'decoration_catalog.json'))[k]};snapshots=[]
 for p in sorted(e.glob('*snapshot.json')):
  x=json.loads(read(p));claims={(r['x'],r['y'],r['logicalLayer'],r['heightKey'])for r in x['networkCells']}
  for r in x['groupPlacements']:
   w,h=sizes[r['groupKey']]
   claims|={(r['anchorX']+dx,r['anchorY']+dy,r['logicalLayer'],r['heightKey'])for dy in range(h)for dx in range(w)}
  n=len(x['networkCells']);g=len(x['groupPlacements']);c=len(claims)
  ck('Typed flat exact16 fields '+p.name,set(x)==FIELDS and x['isExampleOnly']is True and x['isWholeBoard']is True)
  ck('Exact board counts documented '+p.name,f'{n}연결칸+{g}묶음={c}'in cli)
  snapshots.append(dict(file=p.name,networkCells=n,placements=g,claims=c,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 pin=re.search(r'ArtManifestSha256 = "([^"]+)"',bs).group(1);ck('Matching declared final pin or pending placeholder',pin in rm and pin in cli)
 report=dict(status=('PASS_DOC_ALIGNMENT'if re.fullmatch('[0-9a-f]{64}',pin)else'PASS_DOC_ALIGNMENT_PENDING_FINAL_PIN')if all(r['passed']for r in checks)else'FINDINGS_DOC_ALIGNMENT',checkCount=len(checks),checks=checks,snapshots=snapshots,artManifestSha256=pin,actualUnityCommandExecution='NOT_RUN')
 if a.report:Path(a.report).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(dict(status=report['status'],checks=len(checks),failed=[r['check']for r in checks if not r['passed']]),ensure_ascii=False))
if __name__=='__main__':main()
