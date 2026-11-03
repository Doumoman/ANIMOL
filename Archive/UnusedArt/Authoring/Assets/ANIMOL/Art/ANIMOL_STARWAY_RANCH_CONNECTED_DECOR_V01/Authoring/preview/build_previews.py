"""Offline reference+new-art compositor; no production RGB painting or Unity claim."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,math,random
R=Path('/workspace/scratch/b0cb6eab55f7');W=R/'work/ranch_connected_v01';O=Path(__file__).resolve().parent
B=W/'reference/ANIMOL_STARWAY_RANCH_CARTOON_V12'
for temporary in list(O.glob('*.writing.png'))+list(O.glob('*.writing.gif')):temporary.unlink()
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
def save(im,p):
 temp=p.with_suffix('.writing.png');im.save(temp,format='PNG');Image.open(temp).load();temp.replace(p)

META={};PATH={};GROUPS={};MANIFESTS={};USED_NEW=set();USED_BASE=set()
for group in ['rails','meadow','groups']:
 p=W/(group+'_exports')/(group+'_manifest.json');j=read(p);MANIFESTS[group]={'path':str(p),'sha256':sha(p)}
 for a in j['assets']:
  assert a['file']not in META,a['file'];META[a['file']]=a;PATH[a['file']]=p.parent/a['file']
  assert sha(PATH[a['file']])==a['sha256'],('Stale producer source',group,a['file'])
  if a['kind']=='multi_cell_group':GROUPS[a.get('key',a['id'])]=a
BASE={a['file']:a for a in read(B/'manifest.json')['assets']}
for a in read(B/'background_manifest.json')['assets']:BASE[a['file']]=a
for a in read(B/'span_manifest.json')['assets']:BASE[a['file']]=a
SEED=20261038;POSTSPACING=4
N4=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
N8=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]
MEADOW='raised_flower_meadow';FAMILIES=['cream_fence','meadow_stone_wall',MEADOW]
def h32(n):return n&0xffffffff
def stable_hash(seed,x,y,layer,height,group):
 h=2166136261
 # Preview groups use ASCII; the character loop is exactly C# UTF16 char for ASCII.
 for c in group:h=h32((h^ord(c))*16777619)
 h=h32((h^h32(seed))*16777619);h^=h32(x)*0x9e3779b9&0xffffffff;h^=h32(y)*0x85ebca6b&0xffffffff
 h^=h32(layer)*0xc2b2ae35&0xffffffff;h^=h32(height)*0x27d4eb2f&0xffffffff
 h^=h>>16;h=h32(h*0x7feb352d);h^=h>>15;h=h32(h*0x846ca68b);return h32(h^(h>>16))
def mask_for(cells,p,family):return sum(b for dx,dy,b in(N8 if family==MEADOW else N4)if(p[0]+dx,p[1]+dy)in cells)
def periodic(mask,p,layer,height,group,masks):
 if mask not in[5,10]:return False
 phase=stable_hash(SEED,0,0,layer,height,group)%POSTSPACING
 coordinate=p[1]if mask==5 else p[0]
 if(coordinate-phase)%POSTSPACING:return False
 neighbors=[(p[0],p[1]-1),(p[0],p[1]+1)]if mask==5 else[(p[0]-1,p[1]),(p[0]+1,p[1])]
 return all(masks.get(q)==mask for q in neighbors)
ACCENTS=sorted(a['file']for a in META.values()if a['kind']=='meadow_flower_accent'and a['family']==MEADOW)
assert len(ACCENTS)==4
def select_cells(cells,family,group,layer=0,height=0,order=1000):
 masks={p:mask_for(cells,p,family)for p in cells};out=[]
 for p in sorted(cells,key=lambda p:(p[1],p[0])):
  x,y=p;m=masks[p];file=f'decor/connected/{family}/n{m:03d}.png'if family==MEADOW else f'decor/connected/{family}/n{m:02d}.png'
  row={'x':x,'y':y,'logicalLayer':layer,'heightKey':height,'sortingOrder':order(x,y)if callable(order)else order,'worldZ':0,'family':family,'connectionGroup':group}
  h=stable_hash(SEED,x,y,layer,height,group)
  out.append({'row':row,'file':file,'mask':m,'kind':'network','worldCenter':[x+.5,y+.5],'order':row['sortingOrder']})
  if family!=MEADOW and periodic(m,p,layer,height,group,masks):out.append({'row':row,'file':f'decor/connected/{family}/periodic_post.png','mask':m,'kind':'periodic_post','worldCenter':[x+.5,y+.5],'order':row['sortingOrder']+1})
  if family==MEADOW and h%5==0:out.append({'row':row,'file':ACCENTS[(h//5)%len(ACCENTS)],'mask':m,'accentHash':h,'kind':'meadow_accent','worldCenter':[x+.5,y+.5],'order':row['sortingOrder']+1})
 return out

IM={};SCALED={}
def bitmap(file,base=False):
 k=(file,base)
 if k not in IM:
  im=Image.open(B/file if base else PATH[file]).convert('RGBA');im.load();IM[k]=im
 (USED_BASE if base else USED_NEW).add(file);return IM[k]
def scaled(file,ppw,scale=1,base=False,opacity=1):
 k=(file,round(ppw,8),round(scale,8),base,round(opacity,8))
 if k not in SCALED:
  meta=BASE[file]if base else META[file];im=bitmap(file,base);ppu=meta.get('ppu',meta.get('pixelsPerUnit',256));z=(max(1,round(im.width*ppw/ppu*scale)),max(1,round(im.height*ppw/ppu*scale)))
  im=im.resize(z,Image.Resampling.LANCZOS)
  if opacity!=1:im.putalpha(im.getchannel('A').point(lambda a:round(a*opacity)))
  SCALED[k]=im
 return SCALED[k]
def blit(out,file,x,y,ppw=80,origin=(140,1540),scale=1,base=False,opacity=1):
 meta=BASE[file]if base else META[file];im=scaled(file,ppw,scale,base,opacity);px=origin[0]+x*ppw;py=origin[1]-y*ppw;pivot=meta['pivot']
 out.alpha_composite(im,(round(px-pivot[0]*im.width),round(py-(1-pivot[1])*im.height)))

# The WHOLE snapshot uses Unity +Y North and bottom-left group anchors.
FLOOR={(x,y)for y in range(12)for x in range(10)if(y!=0 or 1<=x<=8)and(y!=11 or 2<=x<=7)}
FENCE={(x,10)for x in range(10)}|{(0,y)for y in range(6,10)}|{(9,y)for y in range(7,10)}|{(4,9)}
STONE={(x,0)for x in range(1,9)}|{(8,y)for y in range(1,5)}
WEST=({(x,y)for x in range(1,4)for y in range(1,4)}-{(2,2)})|{(1,4)}
EAST={(x,y)for x in range(5,8)for y in range(2,4)}
for cells in[FENCE,STONE,WEST,EAST]:assert cells<=FLOOR
PLAN=select_cells(FENCE,'cream_fence','pasture_perimeter',0,0,lambda x,y:2000-round((y+.14)*100))
PLAN+=select_cells(STONE,'meadow_stone_wall','south_stone_border',0,0,lambda x,y:2000-round((y+.14)*100))
PLAN+=select_cells(WEST,MEADOW,'west_flowerbed',1,0,100)
PLAN+=select_cells(EAST,MEADOW,'east_flowerbed',1,0,100)
PLACEMENTS=[]
for key,x,y,pid in [('hay_store_2x1',1,5,'hay_store_main'),('feeding_corner_3x1',5,5,'feeding_station_main'),('tree_flower_corner_2x2',1,8,'tree_garden_main'),('barnyard_3x2',5,8,'barnyard_main')]:
 a=GROUPS[key];w,h=a['footprintCells'];claims={(x+dx,y+dy)for dx in range(w)for dy in range(h)};assert claims<=FLOOR
 row={'placementId':pid,'groupKey':key,'anchorX':x,'anchorY':y,'logicalLayer':2,'heightKey':0,'sortingOrder':2000-round((y+.03)*100)+2,'worldZ':0,'widthCells':w,'heightCells':h}
 PLACEMENTS.append(row);PLAN.append({'row':row,'file':a['file'],'kind':'multi_cell_group','worldCenter':[x+w/2,y+h/2],'order':row['sortingOrder'],'claimedCells':sorted(map(list,claims))})
NETROWS=[p['row']for p in PLAN if p['kind']=='network']
assert len(PLACEMENTS)==len({p['row']['placementId']for p in PLAN if p['kind']=='multi_cell_group'})==4
assert len({(p['x'],p['y'],p['logicalLayer'],p['heightKey'])for p in NETROWS})==len(NETROWS)
for layer in range(3):
 claims=set()
 for p in PLAN:
  if p['row']['logicalLayer']!=layer or p['kind']not in['network','multi_cell_group']:continue
  add={tuple(q)for q in(p['claimedCells']if'claimedCells'in p else[[p['row']['x'],p['row']['y']]])}
  assert not add&claims;claims|=add

SUPPORTS=[]
for y in range(12):
 cols=sorted(x for x in range(10)if(x,y)in FLOOR and(x,y-1)not in FLOOR)
 while cols:
  x=cols.pop(0);run=[x]
  while cols and cols[0]==run[-1]+1:run.append(cols.pop(0))
  length=len(run);parts=[length]if length<=4 else([3,3,2]if length==8 else[3,length-3]);offset=0
  for i,n in enumerate(parts):
   assert 1<=n<=4
   left='end'if i==0 else'continue';right='end'if i==len(parts)-1 else'continue';file=f'skirts/spans/grass/exterior/l{n}_L{left}_R{right}_v0.png';assert file in BASE,file
   SUPPORTS.append({'file':file,'worldCenter':[x+offset+n/2,y+.5],'spanCells':n,'runLength':length,'sourceRole':'existing_base_context_only'});offset+=n

BG=[]
randomgen=random.Random(1167)
for i in range(38):
 x=randomgen.uniform(-1.5,11.5);y=randomgen.uniform(12.0,18.6)if i<25 else randomgen.choice([-2.5,-.7])+randomgen.random()*2
 file='backgrounds/stars/'+(['star_01.png','star_02.png','star_03.png','star_04.png','star_05.png','star_06.png'][i%6]if i%3==0 else['dot_cream.png','dot_gray.png'][i%2])
 BG.append({'file':file,'x':x,'y':y,'scale':randomgen.uniform(.08,.24)if'dot_'in file else randomgen.uniform(.25,.55),'opacity':.95,'kind':'star','sortingOrder':-90})
for i,(x,y,s,opacity)in enumerate([(-.3,15.0,.68,.9),(10.1,12.8,.64,.86),(-1.8,2.0,.52,.82),(11.2,-.6,.60,.84)]):
 BG.append({'file':'backgrounds/cloud_bank.png','x':x,'y':y,'scale':s,'opacity':opacity,'kind':'cloud','sortingOrder':-70+i})

layout={'schemaVersion':1,'packageId':'ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01','themeDisplayName':'별길 목장','previewOnly':True,
 'coordinateConvention':'+Y North. Group anchor is bottom-left cell. worldOrigin is bottom-left of cell(0,0).',
 'worldOrigin':[0,0,0],'cellSize':1,'seed':SEED,'postSpacing':POSTSPACING,'camera':{'position':[5,7.25,-10],'orthographicSize':12,'aspect':.5625},
 'referenceCanvas':[1080,1920],'referencePixelsPerCell':80,'referenceBottomLeftPixel':[140,1540],
 'networkCells':NETROWS,'groupPlacements':PLACEMENTS,'hostAllowedDecorCells':sorted(map(list,FLOOR)),
 'newDecorRenderPlan':PLAN,'baseContext':{'existingPackageId':'ANIMOL_STARWAY_RANCH_CARTOON_V12','newRuntimeAssetsCopied':False,
  'floorCells':sorted(map(list,FLOOR)),'supports':SUPPORTS,'background':BG,'oldDeviceActorFunctionalWindmillCount':0},
 'actualUnityExecuted':False,'actualHostBindingsVerified':False}
write(O/'example_layout.json',layout)

def frame(debug=False):
 out=Image.new('RGBA',(1080,1920),(17,32,51,255));sky=bitmap('backgrounds/sky_base_native.png',True)
 scale=max(1080/sky.width,1920/sky.height)*1.06;sky=sky.resize((round(sky.width*scale),round(sky.height*scale)),Image.Resampling.LANCZOS);out.alpha_composite(sky,((1080-sky.width)//2,(1920-sky.height)//2))
 for b in sorted(BG,key=lambda b:b['sortingOrder']):blit(out,b['file'],b['x'],b['y'],scale=b['scale'],base=True,opacity=b['opacity'])
 for row in SUPPORTS:blit(out,row['file'],*row['worldCenter'],base=True)
 for x,y in sorted(FLOOR,key=lambda p:(-p[1],p[0])):
  mask=mask_for(FLOOR,(x,y),MEADOW);file=f'tiles/grass/organic256/n{mask:03d}.png';blit(out,file,x+.5,y+.5,base=True)
 for i,p in sorted(enumerate(PLAN),key=lambda z:(z[1]['order'],z[0])):blit(out,p['file'],*p['worldCenter'])
 d=ImageDraw.Draw(out)
 if debug:
  for x,y in FLOOR:
   px=140+x*80;py=1540-(y+1)*80;d.rectangle((px,py,px+80,py+80),outline=(20,81,74,130),width=1)
  for p in PLAN:
   if p['kind']!='multi_cell_group':continue
   r=p['row'];x,y=r['anchorX'],r['anchorY'];w,h=r['widthCells'],r['heightCells'];px=140+x*80;py=1540-(y+h)*80
   d.rectangle((px,py,px+w*80,py+h*80),outline=(247,190,70,255),width=3);d.text((px+5,py+5),str(w)+'x'+str(h),font=font(18),fill=(9,26,36))
  d.text((38,1710),'Gold boxes = full group footprints / ONE renderer per group.',font=font(22),fill=(255,231,174))
 d.rectangle((24,26,1056,176),fill=(8,22,37,238));d.text((46,43),'STARWAY RANCH / CONNECTED DECOR',font=font(32),fill=(255,232,174))
 d.text((46,94),'NATIVE MULTI-CELL GROUPS + NEIGHBOR-AWARE DETAILS',font=font(19),fill=(119,223,195));d.text((46,132),'OFFLINE ART PREVIEW / UNITY IMPORT + PLAY: NOT_RUN',font=font(18),fill=(189,205,212))
 d.text((38,1804),'2x1  /  3x1  /  2x2  /  3x2     EACH GROUP = ONE SPRITE',font=font(22),fill=(255,231,174))
 d.text((38,1843),'Fences, low walls and raised flowerbeds join at tile borders.',font=font(19),fill=(205,226,220));d.text((38,1878),'Existing base terrain/sky are context; no actor/device changes.',font=font(17),fill=(175,199,204))
 return out.convert('RGB')
save(frame(False),O/'RANCH_CONNECTED_DECOR_PREVIEW.png');save(frame(True),O/'RANCH_CONNECTED_DECOR_GRID.png')

CASES=[('ISOLATED / 1 CELL',{(2,2)}),('2 CELLS / ONE RUN',{(2,2),(3,2)}),('3 CELLS / ONE RUN',{(1,2),(2,2),(3,2)}),('4 CELLS / ONE RUN',{(1,2),(2,2),(3,2),(4,2)}),
 ('ELBOW / NO INNER CAP',{(1,1),(2,1),(3,1),(3,2),(3,3)}),('T + CROSS / SHARED JOINT',{(2,1),(2,2),(2,3),(1,2),(3,2),(4,2),(4,3),(5,2)}),
 ('CLOSED LOOP / CLEAR HOLE',{(x,y)for x in range(1,4)for y in range(1,4)}-{(2,2)}),('REMOVE TILE / OPEN RIM',{(x,y)for x in range(1,4)for y in range(1,4)}-{(2,2),(2,1)})]
def small_plan(cells,family):return select_cells(cells,family,'topology_review_'+family,1 if family==MEADOW else 0,0,100)
overview=Image.new('RGBA',(1800,1720),(10,24,35,255));d=ImageDraw.Draw(overview)
d.text((28,22),'CONNECTED DECOR / SAME ART, WHOLE-SNAPSHOT NEIGHBOR SELECTION',font=font(29),fill=(255,231,174));d.text((28,70),'Every panel uses actual producer PNGs at1world spacing / no alpha-fit / UNITY NOT_RUN',font=font(20),fill=(122,218,197))
for ci,(title,cells)in enumerate(CASES):
 x=ci%4*450+12;y=ci//4*790+126;d.rectangle((x,y,x+426,y+766),outline=(54,93,101),width=2);d.text((x+14,y+16),title,font=font(18),fill=(255,231,174))
 for fi,family in enumerate(FAMILIES):
  top=y+64+fi*230;d.text((x+14,top),family,font=font(16),fill=(122,218,197));origin=(x+64,top+202)
  for yy in range(5):
   for xx in range(7):d.rectangle((origin[0]+xx*44,origin[1]-(yy+1)*44,origin[0]+(xx+1)*44,origin[1]-yy*44),outline=(25,49,58),width=1)
  for p in sorted(small_plan(cells,family),key=lambda p:(p['order'],p['worldCenter'][1],p['worldCenter'][0])):blit(overview,p['file'],*p['worldCenter'],ppw=44,origin=origin)
save(overview.convert('RGB'),O/'RANCH_CONNECTED_DECOR_CONNECTIONS.png')

SNAPSHOTS=CASES[:6]+[('FILL REGION',{(x,y)for x in range(1,4)for y in range(1,4)})]+CASES[6:]+[('RESTORE HOLE',CASES[6][1]),('RESTORE4RUN',CASES[3][1]),('CLEAR LAYOUT',set())]
def editframe(index):
 title,cells=SNAPSHOTS[index];out=Image.new('RGBA',(1200,640),(10,24,35,255));d=ImageDraw.Draw(out)
 d.text((24,18),'TILE ADD / REMOVE / WHOLE DECOR SNAPSHOT',font=font(28),fill=(255,231,174));d.text((24,62),str(index+1).zfill(2)+' / '+title,font=font(22),fill=(122,218,197))
 for fi,family in enumerate(FAMILIES):
  x=fi*400;origin=(x+30,500);d.text((x+25,125),family,font=font(18),fill=(255,231,174))
  for yy in range(6):
   for xx in range(7):d.rectangle((origin[0]+xx*48,origin[1]-(yy+1)*48,origin[0]+(xx+1)*48,origin[1]-yy*48),outline=(39,66,72),width=1)
  for p in sorted(small_plan(cells,family),key=lambda p:(p['order'],p['worldCenter'][1],p['worldCenter'][0])):blit(out,p['file'],*p['worldCenter'],ppw=48,origin=origin)
 d.text((24,552),'Native static sprites selected again after each authoring edit.',font=font(22),fill=(205,226,220));d.text((24,592),'OFFLINE ILLUSTRATION / no real Unity input, collision or gameplay is executed.',font=font(18),fill=(175,199,204))
 return out.convert('RGB')
gifframes=[editframe(i)for i in range(len(SNAPSHOTS))];sample=Image.new('RGB',(1200*4,640*3))
for i,im in enumerate(gifframes):sample.paste(im,((i%4)*1200,(i//4)*640))
pal=sample.quantize(colors=256,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE);indexed=[im.quantize(palette=pal,dither=Image.Dither.NONE)for im in gifframes]
gif=O/'RANCH_CONNECTED_DECOR_EDIT_REPLAY.gif';temp=gif.with_suffix('.writing.gif');indexed[0].save(temp,save_all=True,append_images=indexed[1:],duration=500,loop=0,disposal=2,optimize=False);temp.replace(gif)
decoded=Image.open(gif);duration=0;unique=set()
for i in range(decoded.n_frames):decoded.seek(i);duration+=decoded.info.get('duration',0);unique.add(hashlib.sha256(decoded.convert('RGB').tobytes()).hexdigest())
assert decoded.n_frames==len(SNAPSHOTS)and duration==len(SNAPSHOTS)*500 and len(unique)==len(SNAPSHOTS)
write(O/'edit_replay_snapshots.json',{'schemaVersion':1,'gridConvention':'+Y North','seed':SEED,'postSpacing':POSTSPACING,'snapshotDurationMs':500,
 'snapshots':[{'title':title,'cells':sorted(map(list,cells)),'selectedFiles':{f:[p['file']for p in small_plan(cells,f)]for f in FAMILIES}}for title,cells in SNAPSHOTS],
 'actualUnityExecuted':False,'staticSpriteSelectionDemoOnly':True})

# Explicitly compare a repeated old2x1 prop with the new8-cell connective run.
# This is an illustrative art arrangement, NOT a capture of actual old Unity behavior.
comparison=Image.new('RGBA',(1800,790),(10,24,35,255));d=ImageDraw.Draw(comparison)
d.text((28,20),'REFERENCE PROP PANELS / NEW CONTINUOUS NETWORK',font=font(32),fill=(255,231,174));d.text((28,67),'Same100px perworldcell. Old V12 PNG already declares2x1. This is an art comparison, not a Unity capture.',font=font(20),fill=(122,218,197))
old='props/fence_horizontal.png';assert BASE[old]['footprintCells']==[2,1]
for i in range(4):blit(comparison,old,1+i*2,.5,ppw=100,origin=(680,333),base=True)
d.text((28,202),'OLD V12: separate2x1 panel props',font=font(25),fill=(255,231,174));d.text((28,248),'Repeated posts do not select a shared join.',font=font(20),fill=(175,199,204))
newcells={(x,0)for x in range(8)}
for p in small_plan(newcells,'cream_fence'):blit(comparison,p['file'],*p['worldCenter'],ppw=100,origin=(680,610))
d.text((28,480),'NEW: one continuous run',font=font(25),fill=(255,231,174));d.text((28,526),'Native ends + stable shared post spacing.',font=font(20),fill=(175,199,204));d.text((28,733),'Source pivots and PPU preserved. No whole-sprite fit-to-one-cell; no gameplay Collider change.',font=font(20),fill=(122,218,197))
save(comparison.convert('RGB'),O/'RANCH_CONNECTED_DECOR_BEFORE_AFTER.png')

for group,record in MANIFESTS.items():assert sha(Path(record['path']))==record['sha256'],('Producer changed during rendering',group)
for f in USED_NEW:assert sha(PATH[f])==META[f]['sha256'],('Producer PNG changed',f)
for p in O.glob('*.png'):
 if '.writing.'not in p.name:Image.open(p).load()
newsource=[{'file':f,'sha256':sha(PATH[f]),'ppu':META[f]['ppu'],'pivot':META[f]['pivot'],'logicalRect':META[f]['logicalRect'],'footprintCells':META[f]['footprintCells']}for f in sorted(USED_NEW)]
basesource=[{'file':f,'sha256':sha(B/f),'ppu':BASE[f].get('ppu',BASE[f].get('pixelsPerUnit',256)),'pivot':BASE[f]['pivot'],'role':'read-only base context/comparison; not a new runtime asset'}for f in sorted(USED_BASE)]
qa={'status':'PASS_OFFLINE','producerManifests':MANIFESTS,'referenceCanvas':[1080,1920],'logicalPixelsPerCell':256,'reviewPixelsPerCell':80,
 'hostApprovedFloorCells':len(FLOOR),'networkCells':len(NETROWS),'groupPlacements':len(PLACEMENTS),'groupRendererCount':4,'allGroupClaimsInsideApprovedFloor':True,
 'allNewRuntimePNGShaVerified':True,'nativeProductionPixelsMutated':False,'ppuAndPivotHonoredWithoutAlphaFit':True,'staticTopologyIsHostSnapshotOnly':True,
 'periodicPostsUseCSharpStableHashAndTwoAlongAxisStraightNeighbors':True,'flowerAccentsUseCSharpHashModulo5AndSortedCurrentFiles':True,
 'baseTerrainAndSkyAreReadOnlyContext':True,'noActorDeviceOrFunctionalWindmillArt':True,'oldPropAlreadyMultiCellNotMisrepresented':True,
 'gifFrames':decoded.n_frames,'gifDurationMs':duration,'gifDistinctDecodedFrames':len(unique),'newRuntimeSourcesUsed':newsource,'baseContextSourcesUsed':basesource,
 'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPrefabBinding':'NOT_RUN','actualPlayMode':'NOT_RUN','actualLegacyReferencesZeroVerified':False,
 'outputs':[{'file':p.name,'sha256':sha(p),'sizeBytes':p.stat().st_size}for p in O.glob('*')if p.suffix.lower()in['.png','.gif']and'.writing.'not in p.name]}
write(O/'preview_qa.json',qa)
for temporary in list(O.glob('*.writing.png'))+list(O.glob('*.writing.gif')):temporary.unlink()
print(json.dumps({'status':qa['status'],'networkCells':len(NETROWS),'groups':4,'newPngSourcesUsed':len(USED_NEW),'baseContextSources':len(USED_BASE),'gifFrames':decoded.n_frames,'gifDurationMs':duration}))
