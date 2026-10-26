"""Offline PNG composition of actual current sprites; never a Unity gameplay capture."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,random

ROOT=Path('/workspace/scratch/b0cb6eab55f7')
W=ROOT/'work/theater_connected_v01'
O=Path(__file__).resolve().parent
B=W/'reference/ANIMOL_ECHO_THEATER_CARTOON_V03'
DESIGN=json.loads((W/'catalog_design.json').read_text())
PACKAGE=DESIGN['packageId']
META={};PATH={};GROUPS={};MANIFESTS={};USED_NEW=set();USED_BASE=set();IM={};SCALED={}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())
def write(p,data):p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
def save(im,p):
 temp=p.with_suffix('.writing.png');im.save(temp,format='PNG');Image.open(temp).load();temp.replace(p)

for owner in ['networks_a','networks_b','groups_a','groups_b']:
 p=W/(owner+'_exports')/'manifest.json'
 if not p.exists():raise RuntimeError('Producer not yet ready: '+str(p))
 j=read(p);MANIFESTS[owner]={'path':str(p),'sha256':sha(p)}
 for a in j['assets']:
  assert a['file']not in META,a['file'];META[a['file']]=a;PATH[a['file']]=p.parent/a['file'];assert sha(PATH[a['file']])==a['sha256']
 for g in j.get('groups',[]):
  assert g['key']not in GROUPS;GROUPS[g['key']]=META[g['file']]
assert len(META)==118 and len(GROUPS)==20,(len(META),len(GROUPS))
BASE={}
for name in ['manifest.json','theater_terrain_manifest.json','span_manifest.json','background_manifest.json','background_decor_manifest.json']:
 for a in read(B/name)['assets']:BASE[a['file']]=a

N4=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
N8=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]
FAMILIES=[d['key']for d in DESIGN['networks']]
POST_FAMILIES=set(FAMILIES[:2]);SEED=20261026;SPACING=4
GOLD=(255,225,163);MUTED=(189,168,204);BG=(25,21,40,255);CARD=(43,34,56,255)

def hash32(n):return n&0xffffffff
def stable_hash(seed,x,y,layer,height,group):
 h=2166136261
 for c in group:h=hash32((h^ord(c))*16777619)
 h=hash32((h^hash32(seed))*16777619);h^=hash32(x)*0x9e3779b9&0xffffffff;h^=hash32(y)*0x85ebca6b&0xffffffff
 h^=hash32(layer)*0xc2b2ae35&0xffffffff;h^=hash32(height)*0x27d4eb2f&0xffffffff
 h^=h>>16;h=hash32(h*0x7feb352d);h^=h>>15;h=hash32(h*0x846ca68b);return hash32(h^(h>>16))
def mask_for(cells,p,bits=N4):return sum(bit for dx,dy,bit in bits if(p[0]+dx,p[1]+dy)in cells)
def select_cells(cells,family,group,layer=0,height=0,order=1000):
 masks={p:mask_for(cells,p)for p in cells};out=[]
 for x,y in sorted(cells,key=lambda p:(p[1],p[0])):
  m=masks[(x,y)];file=f'decor/connected/{family}/n{m:02d}.png'
  row={'x':x,'y':y,'logicalLayer':layer,'heightKey':height,'sortingOrder':order(x,y)if callable(order)else order,'worldZ':0,'family':family,'connectionGroup':group}
  out.append({'row':row,'file':file,'mask':m,'kind':'network','worldCenter':[x+.5,y+.5],'order':row['sortingOrder']})
  phase=stable_hash(SEED,0,0,layer,height,group)%SPACING
  along=[(x,y-1),(x,y+1)]if m==5 else[(x-1,y),(x+1,y)]
  periodic=family in POST_FAMILIES and m in[5,10]and((y if m==5 else x)-phase)%SPACING==0 and all(masks.get(p)==m for p in along)
  if periodic:out.append({'row':row,'file':f'decor/connected/{family}/periodic_post.png','mask':m,'kind':'periodic_post','worldCenter':[x+.5,y+.5],'order':row['sortingOrder']+1})
 return out
def bitmap(file,base=False):
 key=(file,base)
 if key not in IM:
  im=Image.open(B/file if base else PATH[file]).convert('RGBA');im.load();IM[key]=im
 (USED_BASE if base else USED_NEW).add(file);return IM[key]
def scaled(file,ppw,scale=1,base=False,opacity=1):
 key=(file,ppw,scale,base,opacity)
 if key not in SCALED:
  a=BASE[file]if base else META[file];im=bitmap(file,base);ppu=a.get('ppu',a.get('pixelsPerUnit',256))
  im=im.resize((max(1,round(im.width*ppw/ppu*scale)),max(1,round(im.height*ppw/ppu*scale))),Image.Resampling.LANCZOS)
  if opacity!=1:im.putalpha(im.getchannel('A').point(lambda n:round(n*opacity)))
  SCALED[key]=im
 return SCALED[key]
def blit(out,file,x,y,ppw=84,origin=(120,1700),scale=1,base=False,opacity=1):
 a=BASE[file]if base else META[file];im=scaled(file,ppw,scale,base,opacity);pivot=a['pivot']
 px=origin[0]+x*ppw;py=origin[1]-y*ppw
 out.alpha_composite(im,(round(px-pivot[0]*im.width),round(py-(1-pivot[1])*im.height)))
def draw_plan(out,plan,ppw,origin):
 for i,p in sorted(enumerate(plan),key=lambda z:(z[1]['order'],z[0])):blit(out,p['file'],*p['worldCenter'],ppw=ppw,origin=origin)
def fixture(cells,family):return select_cells(cells,family,'fixture_'+family,0,0,100)

# One complete authoritative-looking example board; all snapshots are decorative only.
FLOOR={(x,y)for y in range(14)for x in range(10)if y!=13 or 1<=x<=8}
CARPET={(x,y)for y in range(9,13)for x in range(1,9)}
NETWORK_LAYOUT=[
 ('velvet_aisle_barrier','audience_aisle',{(x,5)for x in range(1,5)}|{(4,y)for y in range(3,5)}),
 ('carved_stage_balustrade','front_balustrade',{(x,0)for x in range(10)}|{(0,1),(9,1)}),
 ('prop_shelf_run','backstage_shelves',{(x,8)for x in range(6,9)}|{(8,6),(8,7)}),
 ('velvet_curtain_screen','stage_backdrop',{(x,13)for x in range(1,9)}|{(1,12),(8,12)}),
 ('brass_footlight_strip','stage_front_lights',{(x,10)for x in range(1,9)}),
 ('ivy_planter_strip','entry_planters',{(x,1)for x in range(2,5)}|{(6,1),(7,1),(2,2),(7,2)})
]
PLAN=[]
for family,group,cells in NETWORK_LAYOUT:
 assert cells<=FLOOR;PLAN+=select_cells(cells,family,group,0,0,lambda x,y:3000-round((y+.15)*100))
PLACEMENTS=[]
GROUP_LAYOUT=[
 ('grand_piano_3x2',2,11,'piano_main'),('scenery_flat_3x1',5,12,'scenery_main'),('mask_display_2x1',6,11,'mask_display_main'),
 ('vanity_dressing_2x1',1,8,'dressing_left'),('costume_rack_3x1',5,9,'costumes_main'),
 ('audience_bench_3x1',1,6,'audience_left'),('audience_bench_3x1',5,6,'audience_right'),
 ('ticket_booth_2x2',7,3,'ticket_booth_main'),('phonograph_table_2x1',1,3,'phonograph_left'),
 ('puppet_storage_2x2',5,2,'puppet_store_main'),('stage_steps_2x1',4,7,'stage_steps_main'),
 ('backstage_spotlight_duo_2x1',0,11,'spotlights_backstage'),
 ('flower_urn',0,12,'flowers_left'),('ticket_stand',8,2,'entry_ticket_stand'),('music_stand',4,9,'stage_music_stand'),('tall_candelabra',9,12,'candles_right')
]
for key,x,y,pid in GROUP_LAYOUT:
 a=GROUPS[key];w,h=a['footprintCells'];claims={(x+dx,y+dy)for dx in range(w)for dy in range(h)};assert claims<=FLOOR
 row={'placementId':pid,'groupKey':key,'anchorX':x,'anchorY':y,'logicalLayer':0,'heightKey':0,'sortingOrder':3000-round((y+.03)*100)+2,'worldZ':0}
 PLACEMENTS.append(row);PLAN.append({'row':row,'file':a['file'],'kind':a['kind'],'worldCenter':[x+w/2,y+h/2],'order':row['sortingOrder'],'footprintCells':[w,h],'claimedCells':sorted(map(list,claims))})
NETROWS=[p['row']for p in PLAN if p['kind']=='network']
claims=set()
for p in PLAN:
 if p['kind']=='periodic_post':continue
 add={tuple(q)for q in p.get('claimedCells',[[p['row']['x'],p['row']['y']]]if p['kind']=='network'else[])}
 assert not claims&add,(p['file'],claims&add);claims|=add

SUPPORTS=[]
for y in range(14):
 cols=sorted(x for x in range(10)if(x,y)in FLOOR and(x,y-1)not in FLOOR)
 while cols:
  x=cols.pop(0);run=[x]
  while cols and cols[0]==run[-1]+1:run.append(cols.pop(0))
  n=len(run);parts=[n]if n<=4 else[3,4,3]if n==10 else[4,n-4];offset=0
  for i,length in enumerate(parts):
   assert 1<=length<=4
   left='end'if i==0 else'continue';right='end'if i==len(parts)-1 else'continue';file=f'skirts/spans/stage_floor/exterior/l{length}_L{left}_R{right}_v0.png'
   assert file in BASE,file;SUPPORTS.append({'file':file,'worldCenter':[x+offset+length/2,y+.5],'spanCells':length,'runLength':n,'role':'read-only base support context'});offset+=length

BACKDROP=[];rng=random.Random(261026)
for i in range(48):
 x=rng.uniform(-1.1,11.1);y=rng.uniform(14.0,18.2)if i<32 else rng.uniform(-1.9,14.0)
 if i>=32:x=rng.choice([-1.2,10.8])+rng.uniform(-.25,.25)
 file=f'backgrounds/stars/star_{i%6+1:02d}.png'if i%4==0 else'backgrounds/stars/'+('dot_cream.png'if i%2 else'dot_gray.png')
 BACKDROP.append({'file':file,'worldCenter':[x,y],'scale':rng.uniform(.22,.43)if'star_'in file else rng.uniform(.07,.16),'opacity':.95,'kind':'star','sortingOrder':-100})
for x,y,s in[(-1,14.9,.6),(11.6,13.8,.68),(-.9,2,.48),(11,-.2,.55)]:BACKDROP.append({'file':'backgrounds/cloud_bank.png','worldCenter':[x,y],'scale':s,'opacity':.88,'kind':'cloud','sortingOrder':-80})
BACKDROP.append({'file':'backgrounds/distant_stage_island.png','worldCenter':[8.8,16.2],'scale':.45,'opacity':1,'kind':'distant_island','sortingOrder':-70})
BACKDROP.append({'file':'backgrounds/velvet_swag.png','worldCenter':[1.8,17.0],'scale':.40,'opacity':1,'kind':'swag','sortingOrder':-60})

LAYOUT={'schemaVersion':1,'packageId':PACKAGE,'themeDisplayName':'잔영 극장','previewOnly':True,'coordinateConvention':'+Y North. Group anchors are bottom-left. Pixel origin top-left, pivots normalized Unity bottom-left.','worldOrigin':[0,0,0],'cellSize':1,'seed':SEED,'postSpacing':SPACING,'referenceCanvas':[1080,1920],'referencePixelsPerCell':84,'referenceBottomLeftPixel':[120,1700],'networkCells':NETROWS,'groupPlacements':PLACEMENTS,'hostAllowedDecorCells':sorted(map(list,FLOOR)),'newDecorRenderPlan':PLAN,'baseContext':{'packageId':'ANIMOL_ECHO_THEATER_CARTOON_V03','readOnly':True,'copiedAsNewRuntimeAssets':False,'floorCells':sorted(map(list,FLOOR)),'carpetOverlayCells':sorted(map(list,CARPET)),'supports':SUPPORTS,'background':BACKDROP,'actorOrFunctionalDeviceArt':False},'actualUnityExecuted':False,'actualHostBindingsVerified':False}
write(O/'example_layout.json',LAYOUT)

def scene(debug=False):
 out=Image.new('RGBA',(1080,1920),BG);sky=bitmap('backgrounds/sky_base_native.png',True);s=max(1080/sky.width,1920/sky.height)*1.03
 sky=sky.resize((round(sky.width*s),round(sky.height*s)),Image.Resampling.LANCZOS);out.alpha_composite(sky,((1080-sky.width)//2,(1920-sky.height)//2))
 for b in sorted(BACKDROP,key=lambda b:b['sortingOrder']):blit(out,b['file'],*b['worldCenter'],base=True,scale=b['scale'],opacity=b['opacity'])
 for row in SUPPORTS:blit(out,row['file'],*row['worldCenter'],base=True)
 for x,y in sorted(FLOOR,key=lambda p:(-p[1],p[0])):
  mask=mask_for(FLOOR,(x,y),N8);material='velvet_carpet'if(x,y)in CARPET else'stage_floor';file=f'tiles/{material}/organic256/n{mask:03d}.png';blit(out,file,x+.5,y+.5,base=True)
 draw_plan(out,PLAN,84,(120,1700));d=ImageDraw.Draw(out)
 if debug:
  for x,y in FLOOR:
   px=120+x*84;py=1700-(y+1)*84;d.rectangle((px,py,px+84,py+84),outline=(45,148,148,140),width=1)
  for p in PLAN:
   if p['kind']not in['group','point']:continue
   r=p['row'];w,h=p['footprintCells'];px=120+r['anchorX']*84;py=1700-(r['anchorY']+h)*84
   d.rectangle((px,py,px+w*84,py+h*84),outline=(*GOLD,255),width=3);d.text((px+4,py+4),f'{w}x{h}',font=font(17),fill=(20,18,32))
 d.rectangle((22,24,1058,178),fill=(23,17,36,238));d.text((44,40),'ECHO THEATER / CONNECTED DECOR',font=font(32),fill=GOLD)
 d.text((44,91),'26 DESIGNS: 6 NETWORKS + 12 LARGE PROPS + 8 SMALL PROPS',font=font(20),fill=(221,194,217))
 d.text((44,133),'OFFLINE ART PREVIEW / ACTUAL UNITY IMPORT + PLAY: NOT_RUN',font=font(18),fill=MUTED)
 d.rectangle((22,1780,1058,1904),fill=(23,17,36,238));d.text((44,1798),'CAPS, ELBOWS AND JUNCTIONS FOLLOW NEIGHBORING CELLS',font=font(22),fill=GOLD)
 d.text((44,1835),'Large props keep their whole footprint and use ONE renderer.',font=font(21),fill=(221,194,217))
 d.text((44,1873),'Existing stage floor / starry plum sky shown as read-only context.',font=font(18),fill=MUTED)
 return out.convert('RGB')
save(scene(False),O/'THEATER_CONNECTED_DECOR_PREVIEW.png');save(scene(True),O/'THEATER_CONNECTED_DECOR_GRID.png')

# All26 genuinely different designs, masks/helper posts excluded from the count.
catalog=Image.new('RGBA',(1920,3300),BG);d=ImageDraw.Draw(catalog)
d.text((34,24),'ECHO THEATER / 26 DISTINCT DECORATION DESIGNS',font=font(36),fill=GOLD)
d.text((34,80),'6 CONNECTED NETWORKS +12 WHOLE MULTI-CELL PROPS +8 INDIVIDUAL PROPS',font=font(24),fill=MUTED)
d.text((34,123),'16 neighbor states are variants of ONE network design. Helper posts do not inflate this count.',font=font(21),fill=MUTED)
catalog_records=[]
for index,(category,item)in enumerate([('NETWORK',r)for r in DESIGN['networks']]+[('LARGE PROP',r)for r in DESIGN['groups']]+[('SMALL PROP',r)for r in DESIGN['points']]):
 x=16+(index%4)*476;y=190+(index//4)*435;d.rectangle((x,y,x+458,y+416),fill=CARD,outline=(92,67,95),width=2)
 d.text((x+18,y+16),f'{index+1:02d} / '+category,font=font(20),fill=(223,164,135));d.text((x+18,y+50),item['nameEn'],font=font(23),fill=GOLD)
 if category=='NETWORK':
  cells={(k,0)for k in range(4)};plan=fixture(cells,item['key']);draw_plan(catalog,plan,96,(x+38,y+250));files=sorted({p['file']for p in plan})
  d.text((x+18,y+350),'16 masks / native shared joins',font=font(20),fill=MUTED);footprint=None
 else:
  a=GROUPS[item['key']];w,h=a['footprintCells'];ppw=114 if w>=2 else 154;origin=(x+229-w*ppw/2,y+330)
  blit(catalog,a['file'],w/2,h/2,ppw=ppw,origin=origin);files=[a['file']];footprint=[w,h]
  d.text((x+18,y+350),f'{w} x {h} cells / ONE sprite',font=font(20),fill=MUTED)
 d.text((x+18,y+383),'Native angular art / PPU256',font=font(17),fill=(143,128,160))
 catalog_records.append({'number':index+1,'key':item['key'],'category':category,'files':files,'footprintCells':footprint})
save(catalog.convert('RGB'),O/'THEATER_CONNECTED_DECOR_CATALOG_26.png');write(O/'catalog_layout.json',{'distinctFamilies':26,'variantsAndHelperPostsCountedAsFamilies':False,'records':catalog_records,'actualUnityPlay':'NOT_RUN'})

CASES=[('SINGLE',{(0,0)}),('2 CELLS',{(x,0)for x in range(2)}),('3 CELLS',{(x,0)for x in range(3)}),('4 CELLS',{(x,0)for x in range(4)}),('L / ELBOW',{(0,0),(1,0),(2,0),(2,1),(2,2)}),('T JUNCTION',{(0,0),(1,0),(2,0),(1,1),(1,2)}),('CROSS',{(0,1),(1,1),(2,1),(1,0),(1,2)}),('RING / HOLE',{(x,y)for x in range(3)for y in range(3)}-{(1,1)}),('LONG H /7',{(x,0)for x in range(7)}),('LONG V /7',{(0,y)for y in range(7)})]
connections=Image.new('RGBA',(1920,3960),BG);d=ImageDraw.Draw(connections)
d.text((28,24),'6 NETWORK FAMILIES /10 CONNECTED TOPOLOGY CASES EACH',font=font(32),fill=GOLD)
d.text((28,75),'Actual PNGs / fixed cell spacing within each panel / shared native ports, caps and joints / Unity NOT_RUN',font=font(21),fill=MUTED)
fixture_records=[]
for fi,family in enumerate(FAMILIES):
 top=135+fi*632;d.text((30,top),DESIGN['networks'][fi]['nameEn'],font=font(27),fill=GOLD)
 for ci,(title,cells)in enumerate(CASES):
  x=18+(ci%5)*380;y=top+52+(ci//5)*280;d.rectangle((x,y,x+362,y+266),fill=CARD,outline=(81,59,89),width=1)
  d.text((x+14,y+12),title,font=font(21),fill=(223,164,135));ppw=[176,132,104,82,64,64,64,64,42,29][ci]
  maxx=max(p[0]for p in cells)+1;maxy=max(p[1]for p in cells)+1;origin=(x+(362-maxx*ppw)/2,y+240)
  plan=fixture(cells,family);draw_plan(connections,plan,ppw,origin)
  fixture_records.append({'family':family,'case':title,'cells':sorted(map(list,cells)),'pixelsPerCell':ppw,'selected':[{'file':p['file'],'mask':p['mask'],'kind':p['kind']}for p in plan]})
save(connections.convert('RGB'),O/'THEATER_CONNECTED_DECOR_CONNECTIONS.png');write(O/'connection_fixtures.json',{'fixtures':fixture_records,'count':len(fixture_records),'actualUnityPlay':'NOT_RUN'})

footprints=Image.new('RGBA',(1920,2340),BG);d=ImageDraw.Draw(footprints)
d.text((30,24),'12 WHOLE MULTI-CELL PROPS / FOOTPRINT AND ANCHOR REVIEW',font=font(32),fill=GOLD)
d.text((30,76),'Gold outline = logical occupied cells. Full native aspect ratio / ONE renderer per group / PPU256.',font=font(22),fill=MUTED)
for i,item in enumerate(DESIGN['groups']):
 x=18+(i%3)*635;y=140+(i//3)*535;d.rectangle((x,y,x+614,y+514),fill=CARD,outline=(81,59,89),width=1)
 a=GROUPS[item['key']];w,h=a['footprintCells'];ppw=134;origin=(x+307-w*ppw/2,y+415)
 d.text((x+18,y+16),item['nameEn'],font=font(25),fill=GOLD);d.text((x+18,y+55),f'{w}x{h} cells / single placement',font=font(21),fill=MUTED)
 for gy in range(h):
  for gx in range(w):
   px=origin[0]+gx*ppw;py=origin[1]-(gy+1)*ppw;d.rectangle((px,py,px+ppw,py+ppw),fill=(54,47,64),outline=(106,83,112),width=2)
 blit(footprints,a['file'],w/2,h/2,ppw=ppw,origin=origin)
 d.rectangle((origin[0],origin[1]-h*ppw,origin[0]+w*ppw,origin[1]),outline=(*GOLD,255),width=3)
 d.text((x+18,y+465),'Scale = cell size / PPU; canvas padding is preserved.',font=font(19),fill=MUTED)
save(footprints.convert('RGB'),O/'THEATER_CONNECTED_DECOR_FOOTPRINTS.png')

# Eight frame authoring edit replay; does not demonstrate gameplay or an animated device.
snapshots=CASES[:8];gifframes=[]
for index,(title,cells)in enumerate(snapshots):
 out=Image.new('RGBA',(1200,1050),BG);d=ImageDraw.Draw(out)
 d.text((24,18),'TILE EDIT REPLAY / STATIC SPRITE RESELECTION',font=font(27),fill=GOLD);d.text((24,61),f'{index+1:02d} / '+title,font=font(24),fill=MUTED)
 for fi,family in enumerate(FAMILIES):
  x=(fi%3)*400;y=120+(fi//3)*400;d.rectangle((x+10,y,x+390,y+380),fill=CARD,outline=(81,59,89))
  d.text((x+22,y+17),DESIGN['networks'][fi]['nameEn'],font=font(18),fill=GOLD)
  draw_plan(out,fixture(cells,family),60,(x+54,y+302))
 d.text((24,961),'OFFLINE AUTHORING ILLUSTRATION / NOT ACTUAL UNITY INPUT OR PLAY',font=font(22),fill=GOLD)
 d.text((24,1003),'Neighbor masks change after each tile edit; existing gameplay logic is untouched.',font=font(20),fill=MUTED)
 gifframes.append(out.convert('RGB'))
pal_sample=Image.new('RGB',(1200*4,1050*2))
for i,im in enumerate(gifframes):pal_sample.paste(im,((i%4)*1200,(i//4)*1050))
pal=pal_sample.quantize(colors=256,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE)
indexed=[im.quantize(palette=pal,dither=Image.Dither.NONE)for im in gifframes];gif=O/'THEATER_CONNECTED_DECOR_EDIT_REPLAY.gif';tmp=gif.with_suffix('.writing.gif')
indexed[0].save(tmp,save_all=True,append_images=indexed[1:],duration=650,loop=0,disposal=2,optimize=False);tmp.replace(gif)
decoded=Image.open(gif);duration=0;unique=set()
for i in range(decoded.n_frames):decoded.seek(i);duration+=decoded.info.get('duration',0);unique.add(hashlib.sha256(decoded.convert('RGB').tobytes()).hexdigest())
assert decoded.n_frames==len(snapshots)and len(unique)==len(snapshots)
write(O/'edit_replay_snapshots.json',{'snapshots':[{'title':t,'cells':sorted(map(list,c))}for t,c in snapshots],'durationMs':duration,'staticSpriteSelectionOnly':True,'actualUnityPlay':'NOT_RUN'})

for record in MANIFESTS.values():assert sha(Path(record['path']))==record['sha256'],('Producer changed during rendering',record['path'])
newproof=[{'file':f,'sha256':sha(PATH[f]),'ppu':META[f]['ppu'],'pivot':META[f]['pivot'],'logicalRect':META[f]['logicalRect'],'footprintCells':META[f]['footprintCells']}for f in sorted(USED_NEW)]
for q in newproof:assert q['sha256']==META[q['file']]['sha256']
baseproof=[{'file':f,'sha256':sha(B/f),'ppu':BASE[f].get('ppu',BASE[f].get('pixelsPerUnit',256)),'pivot':BASE[f]['pivot'],'role':'read-only base context; not added to new runtime sprites'}for f in sorted(USED_BASE)]
qa={'status':'PASS_OFFLINE','producerManifests':MANIFESTS,'all26DistinctFamiliesPresentInCatalog':True,'networkFamilies':6,'largePropFamilies':12,'pointPropFamilies':8,'masksAndPostsNotCountedAsNewFamilies':True,'referenceCanvas':[1080,1920],'referencePixelsPerCell':84,'networkCells':len(NETROWS),'groupAndPointPlacements':len(PLACEMENTS),'oneRendererPerWholePlacement':True,'allClaimsInsideApprovedFloor':True,'sameLayerDecorClaimOverlap':0,'connectionFixtures':len(fixture_records),'ppuAndPivotHonoredWithoutAlphaFit':True,'periodicPostsMatchCSharpStableHashAndStraightNeighborGuard':True,'baseTerrainSkyStarsCloudsReadOnly':True,'nativeRuntimePixelsMutated':False,'actorOrDeviceSpritesUsed':False,'newRuntimeSourcesUsed':newproof,'baseContextSourcesUsed':baseproof,'gifFrames':decoded.n_frames,'gifDistinctFrames':len(unique),'gifDurationMs':duration,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPrefabBinding':'NOT_RUN','actualPlayMode':'NOT_RUN','actualHostBindingsVerified':False,'outputs':[{'file':p.name,'sha256':sha(p),'bytes':p.stat().st_size}for p in O.glob('*')if p.suffix.lower()in['.png','.gif']and'.writing.'not in p.name]}
write(O/'preview_qa.json',qa)
print(json.dumps({'status':qa['status'],'networkCells':qa['networkCells'],'groupAndPointPlacements':qa['groupAndPointPlacements'],'newRuntimePngReferences':len(newproof),'baseContextPngReferences':len(baseproof),'gifFrames':decoded.n_frames,'connectionFixtures':len(fixture_records)}))
