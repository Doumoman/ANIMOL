"""Independent exact snapshot/native provenance/ground/context data and PNG audit; no producer import."""
from pathlib import Path
from PIL import Image
import argparse,json,hashlib,math,collections
O=Path(__file__).resolve().parent
BITS4=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
BITS8=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())
def stable(seed,x,y,layer,height,group):
 h=2166136261
 # Real C# hash walks UTF16 char units, including nonBMP group strings.
 b=group.encode('utf-16-le')
 for i in range(0,len(b),2):h=((h^(b[i]|b[i+1]<<8))*16777619)&0xffffffff
 h=((h^(seed&0xffffffff))*16777619)&0xffffffff
 for val,k in[(x,0x9e3779b9),(y,0x85ebca6b),(layer,0xc2b2ae35),(height,0x27d4eb2f)]:h^=((val&0xffffffff)*k)&0xffffffff
 h^=h>>16;h=(h*0x7feb352d)&0xffffffff;h^=h>>15;h=(h*0x846ca68b)&0xffffffff;return(h^(h>>16))&0xffffffff
def selections(d,sizes):
 idx={(r['x'],r['y'],r['logicalLayer'],r['heightKey']):r for r in d['networkCells']};out=[]
 def mask(r,x,y):
  total=0
  for dx,dy,bit in(BITS8 if r['family']=='raised_flower_meadow'else BITS4):
   n=idx.get((x+dx,y+dy,r['logicalLayer'],r['heightKey']))
   if n and n['family']==r['family']and n['connectionGroup']==r['connectionGroup']:total|=bit
  return total
 for r in d['networkCells']:
  m=mask(r,r['x'],r['y']);digits=3 if r['family']=='raised_flower_meadow'else 2
  out.append((f"decor/connected/{r['family']}/n{m:0{digits}d}.png",r['x']+.5,r['y']+.5,r['sortingOrder'],'new_connected_decor'))
  if r['family']=='raised_flower_meadow':
   h=stable(d['seed'],r['x'],r['y'],r['logicalLayer'],r['heightKey'],r['connectionGroup'])
   if h%5==0:out.append((f'decor/accents/meadow/flowers_{(h//5)%4+1:02d}.png',r['x']+.5,r['y']+.5,r['sortingOrder']+1,'new_seeded_meadow_flower'))
  elif r['family']in['cream_fence','meadow_stone_wall']and m in[5,10]:
   dx,dy=(1,0)if m==10 else(0,1);phase=stable(d['seed'],0,0,r['logicalLayer'],r['heightKey'],r['connectionGroup'])%d['postSpacing'];v=r['x']if m==10 else r['y']
   if(v-phase)%d['postSpacing']==0 and mask(r,r['x']-dx,r['y']-dy)==m and mask(r,r['x']+dx,r['y']+dy)==m:out.append((f"decor/connected/{r['family']}/periodic_post.png",r['x']+.5,r['y']+.5,r['sortingOrder']+1,'new_optional_periodic_post'))
 for r in d['groupPlacements']:
  w,h=sizes[r['groupKey']];out.append((('decor/points/'if[w,h]==[1,1]else'decor/groups/')+r['groupKey']+'.png',r['anchorX']+w/2,r['anchorY']+h/2,r['sortingOrder'],'new_whole_or_point_decor'))
 return sorted(out)
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--package',required=True);ap.add_argument('--work',required=True);ap.add_argument('--report');a=ap.parse_args();P=Path(a.package);W=Path(a.work);checks=[]
 def ck(n,b):checks.append(dict(check=n,passed=bool(b)))
 m=read(P/'manifest.json');pin=sha(P/'manifest.json');d=read(P/'Examples/stage_snapshot.json');design=read(P/'decoration_catalog.json');sizes={r['key']:r['footprintCells']for c in['groups','points']for r in design[c]}
 ctx=W/'context_preview_exports';l=read(ctx/'context_layout.json');prov=read(ctx/'context_provenance.json');q=read(ctx/'context_preview_qa.json');exp=selections(d,sizes);actual=sorted((r['file'],r['x'],r['y'],r['sortingOrder'],r['role'])for r in l['newSelections'])
 ck('Exact95 independentlyselected context rows',exp==actual and len(exp)==95)
 ck('Context pin and typedstage bytepin match',l['artManifestSha256']==pin==prov['artManifestSha256']and l['typedSnapshotSha256']==sha(P/'Examples/stage_snapshot.json'))
 counts=collections.Counter(r[4]for r in exp);ck('65networks20props4posts6flowers',counts==dict(new_connected_decor=65,new_whole_or_point_decor=20,new_optional_periodic_post=4,new_seeded_meadow_flower=6))
 ground={(r['x'],r['y'])for r in l['groundContextCells']};claims={(r['x'],r['y'])for r in d['networkCells']}
 for r in d['groupPlacements']:
  w,h=sizes[r['groupKey']];claims|={(r['anchorX']+dx,r['anchorY']+dy)for dy in range(h)for dx in range(w)}
 ck('All120 logical claims on455 nativeground cells',len(claims)==120 and len(ground)==455 and claims<=ground)
 holes={tuple(r)for r in l['internalVoidCells']};ck('All6 interiorvoidcells remain unclaimed/no nativeground',len(holes)==6 and not(holes&claims or holes&ground))
 ck('Meadow raw8ring hole preserved inactualnativeground',{(5,17),(6,17)}<=holes)
 ck('Overlay6flowers create0 extra claims',q['flowerOverlayExtraClaims']==0 and len(claims)==120)
 baseline=W/'reference/ANIMOL_STARWAY_RANCH_CARTOON_V12'
 ck('Current baselinearchive exactSHA',sha(W/'reference_download/ANIMOL_STARWAY_RANCH_CARTOON_V12.zip')==prov['baselineArchiveSha256'])
 for r in prov['baselineAssets']:ck('Read-only baseline asset SHA '+r['file'],sha(baseline/r['file'])==r['sha256'])
 for r in prov['newAddonAssets']:ck('Current newaddon asset SHA '+r['file'],sha(P/r['file'])==r['sha256'])
 ck('55baseline+63newsource files',len(prov['baselineAssets'])==55 and len(prov['newAddonAssets'])==63)
 roles=l['renderLayers'];stars=[r['sortingOrder']for r in roles if r['role']=='star'];clouds=[r['sortingOrder']for r in roles if'cloud'in r['role']]
 ck('Stars explicitly behind every cloud',bool(stars)and bool(clouds)and max(stars)<min(clouds))
 ck('No sun moon or planet layer',not any(any(bad in r['role'].lower()or bad in r['file'].lower()for bad in['moon','planet','sun'])for r in roles)and q['uniqueSunRenderers']==q['uniqueMoonRenderers']==q['uniquePlanetRenderers']==0)
 ck('Baselinepixels notcopiedintoRuntime',prov['baselinePngCopiedIntoAddonRuntime']==0 and prov['nativePixelsRepainted']is False)
 # Flood-fill exteriorair, independent of actor's internalHole flags.
 xs=[x for x,y in ground];ys=[y for x,y in ground];minx,maxx,miny,maxy=min(xs)-1,max(xs)+1,min(ys)-1,max(ys)+1
 todo=[(minx,miny)];exterior=set(todo)
 while todo:
  x,y=todo.pop()
  for dx,dy,b in BITS4:
   n=(x+dx,y+dy)
   if minx<=n[0]<=maxx and miny<=n[1]<=maxy and n not in ground and n not in exterior:exterior.add(n);todo.append(n)
 ck('Declaredinternalholes excludedfromfloodedexterior',not(holes&exterior))
 for i,r in enumerate(l['undersideBodyChunks']):
  x0=r['x']-r['spanCells']/2;y0=math.floor(r['y']);cols=[(round(x0)+dx,y0)for dx in range(r['spanCells'])]
  ck('Sparsebodyonlyexternalexposededge '+str(i),not r['internalHole']and all(c in ground and(c[0],c[1]-1)in exterior for c in cols))
 files=[P/'Inspection/examples'/n for n in['CATALOG_26_DESIGNS.png','ALL26_BOARD.png','ALL26_BOARD_GRID.png','STAGE_BOARD.png','STAGE_BOARD_GRID.png']]+[P/'Inspection/context/RANCH_SCENE_CONTEXT.png']
 for p in files:
  im=Image.open(p);im.load();ck('FullPNGdecode '+p.name,im.width>1000 and im.height>1500)
 ck('ContextPNGexactSHA',sha(files[-1])==q['outputSha256'])
 ck('Contexthonestofflinecapture',q['actualUnityCapture']is False and q['actualUnityPlay']=='NOT_RUN'and prov['actualUnityCapture']is False)
 out=dict(status='PASS_PREVIEW_CONTEXT_REVIEW'if all(r['passed']for r in checks)else'FINDINGS_PREVIEW_CONTEXT_REVIEW',checkCount=len(checks),checks=checks,artManifestSha256=pin,counts=dict(counts),actualCSharpCompile='NOT_RUN',actualUnityPlay='NOT_RUN')
 if a.report:Path(a.report).write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(dict(status=out['status'],checks=len(checks),failed=[r['check']for r in checks if not r['passed']])))
if __name__=='__main__':main()
