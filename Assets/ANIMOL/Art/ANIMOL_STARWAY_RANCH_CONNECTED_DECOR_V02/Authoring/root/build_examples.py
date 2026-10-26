"""Build strict, flat inspection-only board snapshots. Never production map authority."""
from pathlib import Path
import json, hashlib

W=Path(__file__).resolve().parent
ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'
O=W/'examples_exports'/'Examples'
O.mkdir(parents=True,exist_ok=True)
design=json.loads((W/'catalog_design.json').read_text())
pinpath=W/'package_staging'/ID/'manifest_sha256.txt'
PIN=pinpath.read_text().strip() if pinpath.exists() else '__RANCH_V02_MANIFEST_SHA256__'
sizes={r['key']:r['footprintCells'] for r in design['groups']+design['points']}
def doc(name,networks,groups,width,height):
    occupied=set()
    for r in networks:
        c=(r['x'],r['y'],r['logicalLayer'],r['heightKey'])
        assert c not in occupied
        occupied.add(c)
    for r in groups:
        for dy in range(sizes[r['groupKey']][1]):
            for dx in range(sizes[r['groupKey']][0]):
                c=(r['anchorX']+dx,r['anchorY']+dy,r['logicalLayer'],r['heightKey'])
                assert c not in occupied,(name,c)
                occupied.add(c)
    planes=sorted({(r['logicalLayer'],r['heightKey']) for r in networks+groups})
    allowed=[dict(x=x,y=y,logicalLayer=l,heightKey=h) for l,h in planes for y in range(height) for x in range(width)]
    assert occupied <= {(r['x'],r['y'],r['logicalLayer'],r['heightKey']) for r in allowed}
    result=dict(schemaVersion=1,packageId=ID,sourceManifestSha256=PIN,boardId=name,revision=1,
        isWholeBoard=True,isExampleOnly=True,originX=0.0,originY=0.0,originZ=0.0,cellSize=1.0,seed=20261027,
        postSpacing=4,networkCells=networks,groupPlacements=groups,allowedDecorCells=allowed)
    return result,dict(networkCells=len(networks),wholeOrPointPlacements=len(groups),uniqueApprovedClaims=len(occupied),
        allowedCells=len(allowed),distinctDesigns=len({r['family'] for r in networks}|{r['groupKey'] for r in groups}),boundsCells=[0,0,width,height])
def net(key,cells,group):
    return [dict(x=x,y=y,logicalLayer=0,heightKey=0,sortingOrder=1000-y*10,worldZ=0.0,family=key,connectionGroup=group) for x,y in sorted(cells)]
def group(key,x,y):
    return dict(placementId='inspection-'+key,groupKey=key,anchorX=x,anchorY=y,logicalLayer=2,heightKey=0,sortingOrder=1000-y*10,worldZ=0.0)

networks=[];panels=[]
for i,a in enumerate(design['networks']):
    ox=(i%2)*12;oy=32-(i//2)*6
    cells={(ox+x,oy+4) for x in range(1,8)}|{(ox+1,oy+y) for y in range(1,4)}|{(ox+7,oy+y) for y in range(2,4)}|{(ox+4,oy+5)}
    if a['connectivity']=='raw8_256':
        cells={(ox+x,oy+y) for x in range(1,9) for y in range(1,5)}-{(ox+4,oy+2),(ox+5,oy+2)}|{(ox+4,oy+5)}
    networks+=net(a['key'],cells,'inspection-'+a['key'])
    panels.append(dict(family=a['key'],labelPosition=[ox+1,oy+.3]))
groups=[group(a['key'],1+(i%4)*6,14-(i//4)*4) for i,a in enumerate(design['groups'])]
groups += [group(a['key'],1+i*3,2) for i,a in enumerate(design['points'])]
all26,allqa=doc('ranch-decor-inspection-all26',networks,groups,24,40)
assert allqa['distinctDesigns']==26

stage_n=[]
stage_n+=net(design['networks'][0]['key'],{(x,24) for x in range(1,16)}|{(1,y) for y in range(18,24)},'stage-courtyard')
stage_n+=net(design['networks'][1]['key'],{(16,y) for y in range(18,25)},'stage-lattice')
stage_n+=net(design['networks'][2]['key'],{(x,y) for x in range(3,9) for y in range(16,19)}-{(5,17),(6,17)},'stage-meadow')
stage_n+=net(design['networks'][3]['key'],{(x,17) for x in range(11,16)},'stage-orchard')
stage_n+=net(design['networks'][4]['key'],{(x,3) for x in range(2,8)}|{(2,4),(2,5)},'stage-garden')
stage_n+=net(design['networks'][5]['key'],{(x,3) for x in range(11,17)}|{(16,4),(16,5)},'stage-lights')
stage_g=[group(a['key'],2+(i%3)*5,13-(i//3)*3) for i,a in enumerate(design['groups'][:9])]
stage_g += [group(a['key'],3+i*5,21) for i,a in enumerate(design['groups'][9:])]
stage_g += [group(a['key'],2+i*2,1) for i,a in enumerate(design['points'])]
stage,stageqa=doc('ranch-decor-inspection-stage',stage_n,stage_g,18,28)
assert stageqa['distinctDesigns']==26
qa={}
for name,d,q in [('all26_snapshot.json',all26,allqa),('stage_snapshot.json',stage,stageqa)]:
    p=O/name;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    q['sha256']=hashlib.sha256(p.read_bytes()).hexdigest();qa[name]=q
(O/'inspection_layout_guide.json').write_text(json.dumps(dict(schema='inspection-only-layout-guide',networkPanels=panels,
    all26BoardBoundsCells=[0,0,24,40],stageBoardBoundsCells=[0,0,18,28],notProductionGameState=True,
    actualUnityImport='NOT_RUN',actualUnityCompile='NOT_RUN',actualScenePlay='NOT_RUN',snapshots=qa),ensure_ascii=False,indent=2)+'\n')
(W/'examples_exports'/'typed_examples_qa.json').write_text(json.dumps(dict(status='PASS_OFFLINE_DATA_VALIDATION',examples=qa,
    jaggedArrayFields=0,productionMapData=False,actualJsonUtilityExecution='NOT_RUN',actualUnityPlay='NOT_RUN'),indent=2)+'\n')
print(json.dumps(qa,indent=2))
