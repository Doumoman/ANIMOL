"""Render the strict all26 snapshot using exact immutable sprite metadata.
These PNGs are offline references, not Unity captures or new production artwork.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import hashlib,json

ROOT=Path('/workspace/scratch/b0cb6eab55f7')
W=ROOT/'work/theater_connected_v01'
O=Path(__file__).resolve().parent
P=ROOT/'output/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
SNAPSHOT=W/'completion_v02/examples_exports/Examples/all26_snapshot.json'
BASE=W/'reference/ANIMOL_ECHO_THEATER_CARTOON_V03'
DESIGN=json.loads((W/'catalog_design.json').read_text())
SECTIONS=[('networks','NETWORK'),('groups','LARGE PROP'),('points','SMALL PROP')]
INFO={a['key']:{**a,'number':n,'category':label}for n,(a,label)in enumerate([(a,label)for section,label in SECTIONS for a in DESIGN[section]],1)}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())
def write(p,data):p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
def save(im,p):
 tmp=p.with_suffix('.writing.png');im.save(tmp,format='PNG');Image.open(tmp).load();tmp.replace(p)

snapshot_bytes=SNAPSHOT.read_bytes();snapshot=read(SNAPSHOT);manifest_bytes=(P/'manifest.json').read_bytes();manifest=read(P/'manifest.json')
manifest_sha=sha(P/'manifest.json');snapshot_sha=sha(SNAPSHOT)
assert snapshot['sourceManifestSha256']==manifest_sha
assert snapshot['packageId']==manifest['packageId']=='ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
assert snapshot['schemaVersion']==1 and snapshot['isWholeBoard'] is True and snapshot['isExampleOnly'] is True
assert snapshot['cellSize']==1 and snapshot['originX']==snapshot['originY']==snapshot['originZ']==0
META={a['file']:a for a in manifest['assets']};GROUPS={a['key']:META[a['file']]for a in manifest['groups']}
assert len(META)==118 and len(GROUPS)==20 and len(INFO)==26
LOCKED={f:sha(P/f)for f in META}
for f,a in META.items():
 assert LOCKED[f]==a['sha256'];im=Image.open(P/f);im.load();assert im.size==(a['width'],a['height'])
assert len({r['family']for r in snapshot['networkCells']})==6
assert len(snapshot['groupPlacements'])==len({r['groupKey']for r in snapshot['groupPlacements']})==20
assert set(INFO)=={r['family']for r in snapshot['networkCells']}|{r['groupKey']for r in snapshot['groupPlacements']}

N4=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
def hash32(n):return n&0xffffffff
def stable_hash(seed,x,y,layer,height,group):
 h=2166136261
 for c in group:h=hash32((h^ord(c))*16777619)
 h=hash32((h^hash32(seed))*16777619);h^=hash32(x)*0x9e3779b9&0xffffffff;h^=hash32(y)*0x85ebca6b&0xffffffff
 h^=hash32(layer)*0xc2b2ae35&0xffffffff;h^=hash32(height)*0x27d4eb2f&0xffffffff
 h^=h>>16;h=hash32(h*0x7feb352d);h^=h>>15;h=hash32(h*0x846ca68b);return hash32(h^(h>>16))
def key(r):return r['x'],r['y'],r['logicalLayer'],r['heightKey']
rows={key(r):r for r in snapshot['networkCells']};assert len(rows)==len(snapshot['networkCells'])
allowed={(r['x'],r['y'],r['logicalLayer'],r['heightKey'])for r in snapshot['allowedDecorCells']}
assert len(allowed)==len(snapshot['allowedDecorCells'])
def same(r,x,y):
 n=rows.get((x,y,r['logicalLayer'],r['heightKey']))
 return n is not None and n['family']==r['family']and n['connectionGroup']==r['connectionGroup']
def mask(r):return sum(bit for dx,dy,bit in N4 if same(r,r['x']+dx,r['y']+dy))
masks={k:mask(r)for k,r in rows.items()}
PLAN=[];claims=set();family_cells={};post_count=0
for r in snapshot['networkCells']:
 k=key(r);assert k in allowed and k not in claims;claims.add(k);m=masks[k]
 f=f"decor/connected/{r['family']}/n{m:02d}.png";assert f in META
 item={'file':f,'kind':'network','number':INFO[r['family']]['number'],'family':r['family'],'worldCenter':[r['x']+.5,r['y']+.5],'row':r,'mask':m,'sortingOrder':r['sortingOrder'],'claimedCells':[list(k)]}
 PLAN.append(item);family_cells.setdefault(r['family'],[]).append(k)
 spacing=snapshot['postSpacing'];phase=stable_hash(snapshot['seed'],0,0,r['logicalLayer'],r['heightKey'],r['connectionGroup'])%spacing
 coordinate=r['y']if m==5 else r['x'];along=[(r['x'],r['y']-1),(r['x'],r['y']+1)]if m==5 else[(r['x']-1,r['y']),(r['x']+1,r['y'])]
 post=r['family']in['velvet_aisle_barrier','carved_stage_balustrade']and m in[5,10]and(coordinate-phase)%spacing==0 and all(same(r,x,y)and masks[(x,y,r['logicalLayer'],r['heightKey'])]==m for x,y in along)
 if post:
  pf=f"decor/connected/{r['family']}/periodic_post.png";assert pf in META;PLAN.append({**item,'file':pf,'kind':'periodic_post','sortingOrder':r['sortingOrder']+1});post_count+=1
for r in snapshot['groupPlacements']:
 a=GROUPS[r['groupKey']];w,h=a['footprintCells'];group_claims=[]
 for dx in range(w):
  for dy in range(h):
   k=(r['anchorX']+dx,r['anchorY']+dy,r['logicalLayer'],r['heightKey']);assert k in allowed and k not in claims;claims.add(k);group_claims.append(list(k))
 PLAN.append({'file':a['file'],'kind':a['kind'],'number':INFO[r['groupKey']]['number'],'family':r['groupKey'],'worldCenter':[r['anchorX']+w/2,r['anchorY']+h/2],'row':r,'sortingOrder':r['sortingOrder'],'footprintCells':[w,h],'claimedCells':group_claims})
assert len(claims)==123

PPW=60;CANVAS=[1600,2760];ORIGIN=[80,2580];BOARD=[24,40]
USED=set();images={};scaled={}
def world(x,y):return ORIGIN[0]+x*PPW,ORIGIN[1]-y*PPW
def bitmap(file):
 if file not in images:images[file]=Image.open(P/file).convert('RGBA');images[file].load()
 USED.add(file);return images[file]
def blit(out,item):
 file=item['file'];a=META[file]
 if file not in scaled:
  im=bitmap(file);ppu=a['ppu'];scaled[file]=im.resize((round(im.width*PPW/ppu),round(im.height*PPW/ppu)),Image.Resampling.LANCZOS)
 im=scaled[file];px,py=world(*item['worldCenter']);pivot=a['pivot']
 out.alpha_composite(im,(round(px-pivot[0]*im.width),round(py-(1-pivot[1])*im.height)))

labels=[]
for family,cells in family_cells.items():
 x=min(k[0]for k in cells);y=max(k[1]for k in cells)+1.3
 info=INFO[family];labels.append({'number':info['number'],'family':family,'text':f"{info['number']:02d}. {info['nameEn']}",'worldPosition':[x,y],'category':info['category'],'fontPixels':23,'anchor':'left'})
for r in snapshot['groupPlacements']:
 a=GROUPS[r['groupKey']];w,h=a['footprintCells'];info=INFO[r['groupKey']]
 labels.append({'number':info['number'],'family':r['groupKey'],'text':f"{info['number']:02d}. {info['nameEn']}",'worldPosition':[r['anchorX']+w/2,r['anchorY']-.45],'category':info['category'],'fontPixels':17 if info['category']=='SMALL PROP'else 18,'anchor':'center'})

SKY=BASE/'backgrounds/sky_base_native.png';sky_sha=sha(SKY)
def render(grid=False):
 sky=Image.open(SKY).convert('RGBA');s=max(CANVAS[0]/sky.width,CANVAS[1]/sky.height);sky=sky.resize((round(sky.width*s),round(sky.height*s)),Image.Resampling.LANCZOS)
 out=Image.new('RGBA',CANVAS,(25,21,40,255));out.alpha_composite(sky,((CANVAS[0]-sky.width)//2,(CANVAS[1]-sky.height)//2))
 overlay=Image.new('RGBA',CANVAS);d=ImageDraw.Draw(overlay)
 d.rectangle((80,180,1520,2580),fill=(24,18,37,242),outline=(125,96,132,255),width=2)
 # Neutral rectangular fills and lines are technical review overlays, never terrain sprites.
 for low,high in[(20,39),(5,17),(1,4)]:
  a,b=world(0,high);c,e=world(24,low);d.rectangle((a,b,c,e),fill=(39,29,51,242))
 if grid:
  for x in range(25):
   px,py=world(x,0);_,top=world(x,40);d.line((px,top,px,py),fill=(89,72,106,180),width=1)
  for y in range(41):
   px,py=world(0,y);right,_=world(24,y);d.line((px,py,right,py),fill=(89,72,106,180),width=1)
  for item in PLAN:
   if item['kind']=='periodic_post':continue
   for x,y,layer,height in item['claimedCells']:
    px,py=world(x,y+1);d.rectangle((px,py,px+PPW,py+PPW),fill=(53,114,124,48),outline=(69,150,158,160),width=1)
 out.alpha_composite(overlay)
 for _,item in sorted(enumerate(PLAN),key=lambda z:(z[1]['sortingOrder'],z[0])):blit(out,item)
 d=ImageDraw.Draw(out)
 for lab in labels:
  px,py=world(*lab['worldPosition']);ff=font(lab['fontPixels']);box=d.textbbox((0,0),lab['text'],font=ff);tw=box[2]-box[0];th=box[3]-box[1]
  if lab['anchor']=='center':px-=tw/2
  d.rectangle((px-5,py-3,px+tw+5,py+th+8),fill=(25,18,38,235));d.text((px,py),lab['text'],font=ff,fill=(255,224,162))
 if grid:
  for item in PLAN:
   if item['kind']not in['group','point']:continue
   r=item['row'];w,h=item['footprintCells'];px,py=world(r['anchorX'],r['anchorY']+h)
   d.rectangle((px,py,px+w*PPW,py+h*PPW),outline=(255,213,122,255),width=2)
  for x in range(0,25,2):px,py=world(x,0);d.text((px-5,py+11),str(x),font=font(16),fill=(200,179,211))
  for y in range(0,41,2):px,py=world(0,y);d.text((px-40,py-8),str(y),font=font(16),fill=(200,179,211))
 d.rectangle((28,24,1572,152),fill=(23,17,36,245));d.text((54,40),'ECHO THEATER / ALL 26 DESIGNS ON ONE TYPED BOARD',font=font(36),fill=(255,224,162))
 d.text((54,91),'OFFLINE_REFERENCE / SAME all26_snapshot.json AS THE UNITY EDITOR EXAMPLE',font=font(24),fill=(218,189,220))
 d.text((54,126),'6 network families +12 whole groups +8 small props / no new art / actual Unity capture: NOT_RUN',font=font(20),fill=(183,162,198))
 d.rectangle((28,2634,1572,2736),fill=(23,17,36,245));d.text((54,2646),'24 x 40 logical cells / +Y NORTH /60px percell / PPU256 / exact source pivots',font=font(24),fill=(255,224,162))
 d.text((54,2684),'Technical background and grid are reference overlays. Each large group is rendered ONCE at its full footprint.',font=font(20),fill=(202,179,214))
 d.text((54,2713),'The actual Unity sample scene must be generated and verified on the project host.',font=font(18),fill=(162,145,179))
 return out.convert('RGB')

save(render(False),O/'ALL26_BOARD.png');save(render(True),O/'ALL26_BOARD_GRID.png')
assert SNAPSHOT.read_bytes()==snapshot_bytes and(P/'manifest.json').read_bytes()==manifest_bytes
for f in META:assert sha(P/f)==LOCKED[f]
assert sha(SKY)==sky_sha
proof=[{'file':f,'sha256':LOCKED[f],'ppu':META[f]['ppu'],'pivot':META[f]['pivot'],'logicalRect':META[f]['logicalRect'],'footprintCells':META[f]['footprintCells']}for f in sorted(USED)]
layout={'schemaVersion':1,'packageId':snapshot['packageId'],'snapshotFile':'Examples/all26_snapshot.json','snapshotSha256':snapshot_sha,'sourceManifestSha256':manifest_sha,'canvasSize':CANVAS,'boardCells':BOARD,'pixelsPerCell':PPW,'pixelWorldOrigin':ORIGIN,'coordinateConvention':'+Y north; group anchor bottom-left; pivot Unity bottom-left.','labels':labels,'renderPlan':PLAN,'periodicPostCount':post_count,'actualUnityExecuted':False,'referenceOverlaysOnly':True}
write(O/'all26_preview_layout.json',layout)
qa={'status':'PASS_OFFLINE_REFERENCE','snapshotSha256':snapshot_sha,'manifestSha256':manifest_sha,'networkCells':len(snapshot['networkCells']),'groupPlacements':len(snapshot['groupPlacements']),'groupPlacementsByKind':{'group':sum(1 for p in PLAN if p['kind']=='group'),'point':sum(1 for p in PLAN if p['kind']=='point')},'distinctFamilies':26,'networkFamilyCount':6,'largeGroupCount':12,'pointFamilyCount':8,'allowedDecorCells':len(allowed),'claimedDecorCells':len(claims),'claimsAllInTypedAllowedCells':True,'samePlaneClaimOverlap':0,'periodicPosts':post_count,'periodicPolicy':'Same C# stable hash and spacing, first2network families only, straight masks5/10 and both along-axis neighbors also same straight mask. This layout has no eligible long pure straight interior.','cellPixels':PPW,'sourcePpuPivotAndFootprintPreserved':True,'oneRendererPerWholeGroupOrPoint':True,'noAlphaFitScaling':True,'all118ArtFilesByteIdenticalBeforeAfter':True,'sourceManifestAndTypedSnapshotByteIdenticalBeforeAfter':True,'referencedRuntimePngs':proof,'baseContextSources':[{'file':'backgrounds/sky_base_native.png','sha256':sky_sha,'role':'read-only V03 sky, not new production art'}],'productionRgbDrawn':False,'technicalOverlayDescription':'Neutral review panel, coordinate grid, claim rectangles and text labels only. No new terrain art.','actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN','actualPrefabBinding':'NOT_RUN','outputs':[{'file':p.name,'sha256':sha(p),'bytes':p.stat().st_size,'width':Image.open(p).width,'height':Image.open(p).height}for p in[O/'ALL26_BOARD.png',O/'ALL26_BOARD_GRID.png']]}
write(O/'all26_preview_qa.json',qa)
freeze={'status':'FROZEN','snapshotSha256':snapshot_sha,'manifestSha256':manifest_sha,'files':[{'file':p.name,'sha256':sha(p),'bytes':p.stat().st_size}for p in sorted(O.iterdir())if p.is_file()and p.name!='ALL26_PREVIEW_FROZEN.json'and'.writing.'not in p.name]}
write(O/'ALL26_PREVIEW_FROZEN.json',freeze)
print(json.dumps({'status':qa['status'],'distinctFamilies':26,'networkCells':qa['networkCells'],'groupPlacements':qa['groupPlacements'],'periodicPosts':post_count,'claimedDecorCells':len(claims),'snapshotSha256':snapshot_sha,'outputs':qa['outputs']},ensure_ascii=False))
