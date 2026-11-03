"""Independent layout/hash/footprint/GIF checks. Does not import preview builder."""
from pathlib import Path
from PIL import Image
import json,hashlib,math
W=Path(__file__).resolve().parent.parent;O=W/'audit_exports';P=W.parents[1]/'output/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01';V=W/'preview_exports'
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def stable(seed,x,y,layer,height,group):
 h=2166136261
 for i in range(0,len(group.encode('utf-16-le')),2):
  b=group.encode('utf-16-le');h=((h^(b[i]+(b[i+1]<<8)))*16777619)&0xffffffff
 h=((h^(seed&0xffffffff))*16777619)&0xffffffff
 for value,mult in [(x,0x9e3779b9),(y,0x85ebca6b),(layer,0xc2b2ae35),(height,0x27d4eb2f)]:h=(h^((value&0xffffffff)*mult))&0xffffffff
 h^=h>>16;h=(h*0x7feb352d)&0xffffffff;h^=h>>15;h=(h*0x846ca68b)&0xffffffff;return(h^(h>>16))&0xffffffff
def key(r):return(r['x'],r['y'],r['family'],r['connectionGroup'],r['logicalLayer'],r['heightKey'])
def mask(row,net):
 offs=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]if row['family']=='raised_flower_meadow'else[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
 return sum(bit for dx,dy,bit in offs if(row['x']+dx,row['y']+dy,*key(row)[2:])in net)

def main():
 errors=[];m=read(P/'manifest.json');assets={a['file']:a for a in m['assets']};j=read(V/'example_layout.json');qa=read(V/'preview_qa.json');sn=read(V/'edit_replay_snapshots.json')
 net={key(r):r for r in j['networkCells']};floor={tuple(p)for p in j['hostAllowedDecorCells']};plan=j['newDecorRenderPlan'];seed=j['seed'];spacing=j['postSpacing'];checked=[]
 if len(net)!=45 or len(j['networkCells'])!=45:errors.append('Network snapshot must contain45 unique source cells')
 if floor!={tuple(p)for p in j['baseContext']['floorCells']}:errors.append('Host-approved decor cells differ from source wholefloor')
 byKind={k:[r for r in plan if r['kind']==k]for k in {r['kind']for r in plan}}
 if len(byKind.get('network',[]))!=len(net):errors.append('Not exactlyone renderer per networkcell')
 expectedPosts=[];expectedFlowers=[];accents=sorted(a['file']for a in m['assets']if a['family']=='raised_flower_meadow'and a['kind']=='meadow_flower_accent')
 for k,row in net.items():
  if(row['x'],row['y'])not in floor:errors.append('Network noton approvedfloor '+str(k))
  n=mask(row,net);digits=3 if row['family']=='raised_flower_meadow'else 2;file=f'decor/connected/{row["family"]}/n{n:0{digits}d}.png';matches=[p for p in byKind['network']if key(p['row'])==k]
  if len(matches)!=1 or matches[0]['file']!=file or matches[0]['mask']!=n or matches[0]['worldCenter']!=[row['x']+.5,row['y']+.5]:errors.append('Wrong connection/mask/center '+str(k))
  h=stable(seed,row['x'],row['y'],row['logicalLayer'],row['heightKey'],row['connectionGroup'])
  if row['family']=='raised_flower_meadow':
   if h%5==0:expectedFlowers.append((k,accents[(h//5)%len(accents)]))
  elif n in [5,10]:
   phase=stable(seed,0,0,row['logicalLayer'],row['heightKey'],row['connectionGroup'])%spacing;coordinate=row['y']if n==5 else row['x'];dx,dy=(0,1)if n==5 else(1,0)
   along=[]
   for sign in [-1,1]:
    neighbor=dict(row,x=row['x']+sign*dx,y=row['y']+sign*dy);along.append(mask(neighbor,net)==n)
   if(coordinate-phase)%spacing==0 and all(along):expectedPosts.append((k,'decor/connected/'+row['family']+'/periodic_post.png'))
 if set(expectedPosts)!={(key(p['row']),p['file'])for p in byKind.get('periodic_post',[])}:errors.append('Periodic posts differ from current C#hash/straight-neighbor rule')
 if set(expectedFlowers)!={(key(p['row']),p['file'])for p in byKind.get('meadow_accent',[])}:errors.append('Flower accents differ from current C#FNV/seed rule')
 groupDefs={g['key']:g for g in m['groups']};claimed=set();groupRows=[]
 for row in j['groupPlacements']:
  g=groupDefs[row['groupKey']];w,h=g['footprintCells'];cells={(row['anchorX']+x,row['anchorY']+y)for x in range(w)for y in range(h)};matches=[p for p in byKind.get('multi_cell_group',[])if p['row']['placementId']==row['placementId']]
  if not cells<=floor:errors.append('Group footprint outside allowedfloor '+row['placementId'])
  if len(matches)!=1 or set(map(tuple,matches[0]['claimedCells']))!=cells or matches[0]['file']!=g['file']or matches[0]['worldCenter']!=[row['anchorX']+w/2,row['anchorY']+h/2]:errors.append('Group is duplicated/wronganchor/wrongfootprint '+row['placementId'])
  for x,y in cells:
   c=(x,y,row['logicalLayer'],row['heightKey'])
   if c in claimed:errors.append('Duplicate group claim '+str(c))
   claimed.add(c)
  a=assets[g['file']];lr=a['logicalRect'];pv=a['pivot'];expected=[(lr[0]+lr[2]/2)/a['width'],1-(lr[1]+lr[3]/2)/a['height']]
  if max(abs(pv[i]-expected[i])for i in range(2))>1e-5:errors.append('Wrong group ground pivot '+g['file'])
  groupRows.append({'id':row['placementId'],'groundCells':sorted(map(list,cells)),'worldCenter':matches[0]['worldCenter']if matches else None,'singleRenderer':len(matches)==1,'logicalRect':lr,'pivot':pv})
 for row in plan:
  f=row['file'];a=assets.get(f)
  if a is None or sha(P/f)!=a['sha256']:errors.append('Preview references missing/stale currentPNG '+f)
  else:checked.append(f)
 snapshotMasks=0
 for s in sn['snapshots']:
  cells={tuple(p)for p in s['cells']}
  for family,files in s['selectedFiles'].items():
   digit=3 if family=='raised_flower_meadow'else 2;group='edit';rows={key({'x':x,'y':y,'family':family,'connectionGroup':group,'logicalLayer':0,'heightKey':0}):{'x':x,'y':y,'family':family,'connectionGroup':group,'logicalLayer':0,'heightKey':0}for x,y in cells};expected=[]
   for x,y in sorted(cells):
    row={'x':x,'y':y,'family':family,'connectionGroup':group,'logicalLayer':0,'heightKey':0};n=mask(row,rows);expected.append(f'decor/connected/{family}/n{n:0{digit}d}.png')
   masksOnly=[f for f in files if f.startswith('decor/connected/'+family+'/n')]
   overlays=[f for f in files if f not in masksOnly]
   if sorted(masksOnly)!=sorted(expected):errors.append('Edit snapshot mask mismatch '+s['title']+' '+family)
   if any(f not in assets or assets[f]['kind']!='meadow_flower_accent'for f in overlays):errors.append('Unknown snapshot overlay '+s['title'])
   snapshotMasks+=len(masksOnly)
 gif=Image.open(V/'RANCH_CONNECTED_DECOR_EDIT_REPLAY.gif');duration=0;unique=set()
 for i in range(gif.n_frames):gif.seek(i);duration+=gif.info.get('duration',0);unique.add(hashlib.sha256(gif.convert('RGB').tobytes()).hexdigest())
 if gif.n_frames!=12 or duration!=6000 or len(unique)!=12:errors.append('Final editGIF frame/timing/unique differs')
 for row in qa['outputs']:
  name=row.get('file');p=V/name
  if p.is_file()and row.get('sha256')and sha(p)!=row['sha256']:errors.append('Output pin differs '+name)
 report={'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','errors':errors,'layoutSha256':sha(V/'example_layout.json'),'previewQaSha256':sha(V/'preview_qa.json'),'canonicalManifestSha256':sha(P/'manifest.json'),'networkCells':len(net),'floorCells':len(floor),'groupRenderers':len(byKind.get('multi_cell_group',[])),'groupGroundClaims':len(claimed),'groups':groupRows,'postHashChecks':len(expectedPosts),'flowerHashChecks':len(expectedFlowers),'currentScenePNGs':len(set(checked)),'editSnapshotMaskSelectionsVerified':snapshotMasks,'gifFrames':gif.n_frames,'gifDurationMs':duration,'gifDistinctDecodedFrames':len(unique),'actualUnityCapture':False,'actualUnityImport':'NOT_RUN','csharpCompile':'NOT_RUN','actualPrefabBindings':'NOT_RUN','actualPlayMode':'NOT_RUN'}
 (O/'preview_independent_qa.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='groups'},ensure_ascii=False))

if __name__=='__main__':main()
