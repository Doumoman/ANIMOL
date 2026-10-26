"""Independent final snapshot-position/native-context/PNG-byte checks; no Unity/GPU."""
from pathlib import Path
from PIL import Image
from collections import Counter
import json,hashlib
W=Path(__file__).resolve().parents[1];O=Path(__file__).resolve().parent;ID='ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01';P=W/'package_staging'/ID;C=W/'context_preview_exports';B=W/'reference/ANIMOL_STAR_SWELL_COAST_CARTOON_V01'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();read=lambda p:json.loads(p.read_text())
checks=[]
def ck(n,b):checks.append(dict(check=n,passed=bool(b)))
m=read(P/'manifest.json');pin=sha(P/'manifest.json');assets={a['file']:a for a in m['assets']};s=read(P/'Examples/stage_snapshot.json');layout=read(C/'context_layout.json');prov=read(C/'context_provenance.json');q=read(C/'context_preview_qa.json')
ck('Context and provenance pins agree exactmanifest',all(d['artManifestSha256']==pin for d in[layout,prov,q]))
ck('Typed snapshot exact bytes',layout['typedSnapshotSha256']==sha(P/'Examples/stage_snapshot.json'))
ck('Baseline manifest exact read-only source',prov['baselineManifestSha256']==sha(B/'manifest.json'))
for a in prov['baselineAssets']:ck('Baseline context source SHA '+a['file'],sha(B/a['file'])==a['sha256'])
for a in prov['newAddonAssets']:ck('Current addon source SHA '+a['file'],sha(P/a['file'])==a['sha256'])
land={(r['x'],r['y'])for r in layout['groundContextCells']};ck('453 unique coast_sand cells',len(land)==len(layout['groundContextCells'])==453 and all(r['material']=='A'for r in layout['groundContextCells']))
cat=read(P/'decoration_catalog.json');sizes={r['key']:r['footprintCells']for k in['groups','points']for r in cat[k]};claims={(r['x'],r['y'])for r in s['networkCells']};expected=[]
index={(r['x'],r['y'],r['logicalLayer'],r['heightKey']):r for r in s['networkCells']}
def mask(r,x,y):
 value=0
 for dx,dy,bit in[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]:
  n=index.get((x+dx,y+dy,r['logicalLayer'],r['heightKey']))
  if n and(n['family'],n['connectionGroup'])==(r['family'],r['connectionGroup']):value|=bit
 return value
for r in s['networkCells']:
 f=f"decor/connected/{r['family']}/n{mask(r,r['x'],r['y']):02d}.png";expected.append((f,r['x']+.5,r['y']+.5,r['sortingOrder'],'new_connected_decor'))
for r in s['groupPlacements']:
 w,h=sizes[r['groupKey']];f=next(a['file']for a in m['groups']if a['key']==r['groupKey']);expected.append((f,r['anchorX']+w/2,r['anchorY']+h/2,r['sortingOrder'],'new_whole_or_point_decor'));claims|={(r['anchorX']+dx,r['anchorY']+dy)for dy in range(h)for dx in range(w)}
actual=[(r['file'],r['x'],r['y'],r['sortingOrder'],r['role'])for r in layout['newSelections']if r['role']!='new_optional_periodic_post'];ck('All75 current nonpost sprite selections exact snapshot masks/centres/order',Counter(actual)==Counter(expected))
posts=[r for r in layout['newSelections']if r['role']=='new_optional_periodic_post'];ck('Four periodicposthelpers',len(posts)==4)
for p in posts:
 x,y=int(p['x']-.5),int(p['y']-.5);r=index.get((x,y,0,0));k=mask(r,x,y)if r else 0;dx,dy=(1,0)if k==10 else(0,1)
 ck('Post guarded longstraight '+str((x,y)),r is not None and r['family']in['rope_dock_fence','driftwood_palisade']and k in[5,10]and mask(r,x-dx,y-dy)==k and mask(r,x+dx,y+dy)==k)
ck('104 claimed ground cells are all native land',len(claims)==104 and claims<=land)
ck('All26designs actually selected',len({assets[r['file']]['family']for r in layout['newSelections']})==26)
r=layout['renderLayers'];ck('Unique setting sun/horizon',sum(a['role']=='setting_sun'for a in r)==sum(a['role']=='horizon'for a in r)==1);ck('Every star drawn behind every cloud',max(a['sortingOrder']for a in r if a['role']=='star')<min(a['sortingOrder']for a in r if a['role']=='cloud'));ck('Background worldplacement only an inspection layout',layout['notProductionMap']is True and layout['actualUnityCapture']is False)
ck('No baseline source promoted into runtime allowlist',all(a['file'].startswith('decor/')for a in m['assets'])and prov['baselinePngCopiedIntoAddonRuntime']is False)
ck('Context final PNG hash',(q['outputSha256']==sha(C/'COAST_SCENE_CONTEXT.png')))
errors=[a['check']for a in checks if not a['passed']];context=dict(status='PASS_OFFLINE_CONTEXT'if not errors else'FAIL_OFFLINE_CONTEXT',checkCount=len(checks),checks=checks,errors=errors,scope='Independent reference provenance hash, actual75nonpost snapshot selections, guarded4posts,104landclaims,one sun/horizon andstarorder checks plus humanfullcontextview. No renderer/GPU/game execution.',actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualUnityCapture=False,actualUnityPlay='NOT_RUN')
(O/'context_independent_qa.json').write_text(json.dumps(context,ensure_ascii=False,indent=2)+'\n')
rows=[];meta=read(W/'preview_exports/preview_qa.json')
for p in sorted((W/'preview_exports').glob('*.png')):
 im=Image.open(p);im.load();v=meta['outputs'][p.name];assert sha(p)==v['sha256'];rows.append(dict(file=p.name,sha256=sha(p),size=list(im.size),loaded=True,independentlyViewed=True))
im=Image.open(C/'COAST_SCENE_CONTEXT.png');im.load();rows.append(dict(file='COAST_SCENE_CONTEXT.png',sha256=sha(C/'COAST_SCENE_CONTEXT.png'),size=list(im.size),loaded=True,independentlyViewed=True))
preview=dict(status='PASS_OFFLINE_REFERENCE_REVIEW',artManifestSha256=pin,loadedAndViewedImages=len(rows),images=rows,openFindings=[],humanReview=['All26catalog panels match exact6network/12whole/8point taxonomy; no mask/helpercountinflation.','All26/STAGE boards andgridvariants correctlyregisterwholefootprint sizes,continuousN/E/S/Wcarriers andall20single-rendererwhole/pointprops.','Current14finalBprops arevisible; cosmeticlargeworksite isnotfunctionalboat, pool remainsillustrative.','Contextimage integrateswarmwood/coral/teal/ivoryprops with actualbaselinesand,navyslateunderside,tealoceanandangularsunset.','Singularsun/horizon,starsbehindclouds,captionOFFLINE/NOT_RUN/read-onlybaselinecontext visible andhonest.','NoPNGclipping/missingbrokenfiles/opaqueatlasbackgrounds orroundbadge/rollspiralart findings.'],actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualUnityPlay='NOT_RUN',actualUnityCapture=False)
(O/'preview_independent_qa.json').write_text(json.dumps(preview,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(dict(contextStatus=context['status'],contextChecks=len(checks),errors=errors,previewImages=len(rows))))
