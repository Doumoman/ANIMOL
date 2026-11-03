"""Read-only handoff/API/data crosscheck; not a Unity command execution."""
from pathlib import Path
import argparse,json,hashlib,re
FIELDS={'schemaVersion','packageId','sourceManifestSha256','boardId','revision','isWholeBoard','isExampleOnly','originX','originY','originZ','cellSize','seed','postSpacing','networkCells','groupPlacements','allowedDecorCells'}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--docs',required=True);ap.add_argument('--source',required=True);ap.add_argument('--examples',required=True);ap.add_argument('--report');a=ap.parse_args();d=Path(a.docs);s=Path(a.source);e=Path(a.examples)
 read=lambda p:p.read_text(encoding='utf-8');cli=read(d/'UNITY_CLI_KO.txt');rm=read(d/'README_KO.txt');alltext=cli+'\n'+rm;bt=read(s/'Editor/CoastConnectedDecorV01BoardTools.cs');bw=read(s/'Editor/CoastConnectedDecorV01BoardWindow.cs');bs=read(s/'Runtime/CoastConnectedDecorV01BoardSnapshot.cs');checks=[]
 def ck(n,b):checks.append(dict(check=n,passed=bool(b)))
 ck('Correct package and namespace in command','ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01'in cli and'ANIMOL.Art.CoastConnectedDecorV01'in cli)
 ck('Exact executeMethod and real method declared','ANIMOL.Art.CoastConnectedDecorV01.CoastConnectedDecorV01BoardTools.BuildExamplesFromCommandLine'in cli and'public static void BuildExamplesFromCommandLine()'in bt)
 flags=['-coastDecorManifest','-coastDecorStageSnapshot','-coastDecorAllSnapshot','-coastDecorSortingLayer','-coastDecorCreateScenes'];ck('Five actual CLI flags',all(f in cli and'"'+f+'"'in bt for f in flags))
 menu='Tools/ANIMOL/Coast Connected Decor V01/Build example boards…';ck('Exact actual menu Unicode ellipsis',menu in cli and menu in bw)
 ck('Optional scenes only when requested','명시적으로 true일 때만 .unity'in cli and'bool createScenes = false'in bt)
 ck('Exact sixteen DTO root fields documented',all(f in cli for f in FIELDS))
 ck('Actual binding and publish calls instructed','Binding.Bind(controller,catalog,currentPin,actualSortingLayer,source,out error)'in cli and'PublishedSnapshotSource.PublishSnapshot(snapshot,out error)'in cli)
 ck('Example production opt-in explicit','allowExampleSource'in cli and'false'in cli and'isExampleOnly=false'in cli)
 ck('Pool stale source invalidation/fresh publish documented','freshPublish'in cli and'비활성화'in cli)
 ck('Correct +Y/bottom-left coordinates and PPU','월드+Y=N'in cli and'anchorX+w/2,anchorY+h/2'in cli and'PPU256'in cli)
 ck('All four connection-domain fields',all(f in cli for f in['family','connectionGroup','logicalLayer','heightKey']))
 ck('Actual material IDs and water distinction','A=coast_sand,W=wood_bridge,S=stone_bridge'in alltext and'물칸은 육지가 아니며'in cli)
 ck('Static skiff separated from functional playerboat','beached_skiff_3x2'in cli and'실제 플레이어의1칸 나룻배가 아니다'in cli)
 ck('Existing animated palms preserved','움직이는 야자수 리그를 정적 소품으로 덮지 않는다'in cli)
 ck('One fixed world sun/horizon excluded from rig','정확히1개씩 월드고정'in cli and'BackgroundRig에서 제외'in cli and'cameraMultiplier0만으로 월드고정이라고 해석하지 않는다'in cli)
 ck('OceanField is not actual water-cell authority','원경 OceanField와 실제 물칸/배 경로 렌더러를 구분'in cli)
 ck('No actual compile/play claim','NOT_RUN'in rm and'실제 프로젝트 실행을 대신하지 않는다'in cli)
 ck('No prior theme identifiers leaked',not re.search(r'SanctumConnectedDecor|SUNSET_SANCTUM|ECHO_THEATER|TheaterConnectedDecor|sanctumDecor|theaterDecor',alltext))
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
