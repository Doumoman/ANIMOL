"""Typed inspection snapshots; no production game state or generated Unity GUIDs."""
from pathlib import Path
from collections import Counter
import hashlib
import json

W = Path(__file__).resolve().parent
BASE = W.parent
PACKAGE = 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
PIN = '8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069'
O = W / 'examples_exports' / 'Examples'
O.mkdir(parents=True, exist_ok=True)
design = json.loads((BASE / 'catalog_design.json').read_text())
layout = json.loads((BASE / 'preview_exports/example_layout.json').read_text())
NETWORK_FIELDS = ['x', 'y', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ', 'family', 'connectionGroup']
GROUP_FIELDS = ['placementId', 'groupKey', 'anchorX', 'anchorY', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ']
ROOT_FIELDS = {'schemaVersion','packageId','sourceManifestSha256','boardId','revision','isWholeBoard','isExampleOnly',
               'originX','originY','originZ','cellSize','seed','postSpacing','networkCells','groupPlacements','allowedDecorCells'}

def approved(cells, planes):
    return [dict(x=x,y=y,logicalLayer=layer,heightKey=height) for layer,height in sorted(planes) for x,y in sorted(cells)]

def document(board_id, networks, groups, allowed, seed=20261026):
    return dict(schemaVersion=1,packageId=PACKAGE,sourceManifestSha256=PIN,boardId=board_id,revision=1,
                isWholeBoard=True,isExampleOnly=True,originX=0.0,originY=0.0,originZ=0.0,cellSize=1.0,
                seed=seed,postSpacing=4,networkCells=networks,groupPlacements=groups,allowedDecorCells=allowed)

networks = [{key:r[key] for key in NETWORK_FIELDS} for r in layout['networkCells']]
groups = [{key:r[key] for key in GROUP_FIELDS} for r in layout['groupPlacements']]
planes = {(r['logicalLayer'],r['heightKey']) for r in networks + groups}
stage = document('theater-decor-inspection-stage',networks,groups,
                 approved({tuple(c) for c in layout['hostAllowedDecorCells']},planes),layout['seed'])

all_networks=[]
panels=[]
for i,a in enumerate(design['networks']):
    ox=(i%2)*12
    oy=32-(i//2)*6
    cells={(ox+x,oy+4) for x in range(1,8)} | {(ox+1,oy+y) for y in range(1,4)} | {(ox+7,oy+y) for y in range(2,4)} | {(ox+4,oy+5)}
    group='inspection-family-'+a['key']
    for x,y in sorted(cells):
        all_networks.append(dict(x=x,y=y,logicalLayer=0,heightKey=0,sortingOrder=1000-y*10,worldZ=0.0,
                                 family=a['key'],connectionGroup=group))
    panels.append(dict(family=a['key'],cellCount=len(cells),labelPosition=[ox+1,oy+0.3]))

all_groups=[]
for i,a in enumerate(design['groups']):
    x=1+(i%4)*6
    y=14-(i//4)*4
    all_groups.append(dict(placementId='inspection-'+a['key'],groupKey=a['key'],anchorX=x,anchorY=y,
                           logicalLayer=2,heightKey=0,sortingOrder=1000-y*10,worldZ=0.0))
for i,a in enumerate(design['points']):
    x=1+i*3
    y=2
    all_groups.append(dict(placementId='inspection-'+a['key'],groupKey=a['key'],anchorX=x,anchorY=y,
                           logicalLayer=2,heightKey=0,sortingOrder=1000-y*10,worldZ=0.0))
all26 = document('theater-decor-inspection-all26',all_networks,all_groups,
                 approved({(x,y) for y in range(40) for x in range(24)},[(0,0),(2,0)]))

sizes={a['key']:a['footprintCells'] for a in design['groups']+design['points']}
net_keys={a['key'] for a in design['networks']}
def verify(d):
    assert set(d)==ROOT_FIELDS and d['isWholeBoard'] is True and d['isExampleOnly'] is True
    assert d['sourceManifestSha256']==PIN and d['packageId']==PACKAGE
    allowed={(r['x'],r['y'],r['logicalLayer'],r['heightKey']) for r in d['allowedDecorCells']}
    assert len(allowed)==len(d['allowedDecorCells'])
    occupied=set(); ids=set(); families=set()
    for r in d['networkCells']:
        assert set(r)==set(NETWORK_FIELDS) and r['family'] in net_keys
        key=(r['x'],r['y'],r['logicalLayer'],r['heightKey'])
        assert key in allowed and key not in occupied
        occupied.add(key);families.add(r['family'])
    for r in d['groupPlacements']:
        assert set(r)==set(GROUP_FIELDS) and r['groupKey'] in sizes
        assert r['placementId'] not in ids;ids.add(r['placementId'])
        width,height=sizes[r['groupKey']]
        for dy in range(height):
            for dx in range(width):
                key=(r['anchorX']+dx,r['anchorY']+dy,r['logicalLayer'],r['heightKey'])
                assert key in allowed and key not in occupied
                occupied.add(key)
        families.add(r['groupKey'])
    return dict(networkCells=len(d['networkCells']),wholeOrPointPlacements=len(d['groupPlacements']),
                uniqueApprovedClaims=len(occupied),allowedCells=len(allowed),distinctDesigns=len(families),
                isExampleOnly=True,wholeBoard=True)

qa={}
for name,d in [('stage_snapshot.json',stage),('all26_snapshot.json',all26)]:
    qa[name]=verify(d)
    path=O/name
    path.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    qa[name]['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    assert json.loads(path.read_text())==d
assert qa['all26_snapshot.json']['distinctDesigns']==26
assert len(all_groups)==20 and {r['groupKey'] for r in all_groups}==set(sizes)
assert len({r['family'] for r in all_networks})==6
assert qa['stage_snapshot.json']['networkCells']==48 and qa['stage_snapshot.json']['wholeOrPointPlacements']==16
metadata={'schema':'inspection-only-layout-guide','networkPanels':panels,
          'all26BoardBoundsCells':[0,0,24,40],
          'notProductionGameState':True,'snapshotFiles':qa,
          'actualUnityImport':'NOT_RUN','actualUnityCompile':'NOT_RUN','actualScenePlay':'NOT_RUN'}
(O/'inspection_layout_guide.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(W/'examples_exports/typed_examples_qa.json').write_text(json.dumps({'status':'PASS_OFFLINE_DATA_VALIDATION','examples':qa,
    'preservesPriorStageRowCoordinates':True,'jaggedArrayFields':0,'productionMapData':False,
    'actualJsonUtilityExecution':'NOT_RUN','actualUnityPlay':'NOT_RUN'},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(qa,ensure_ascii=False,indent=2))
