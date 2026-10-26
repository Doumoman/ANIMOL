from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from scipy import ndimage
import numpy as np,json,hashlib,math

R=Path(__file__).resolve().parents[1]
for d in ['production/components','inspection','decor/connected']: (R/d).mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(im,p):p.parent.mkdir(parents=True,exist_ok=True);im.save(p);Image.open(p).load()
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
SPECS={
 'cream_fence':{'source':'sources/ranch_v02_cream_fence_parts_native.png','rects':{'h':[70,160,1150,405],'v':[1215,0,1435,510],'post':[325,465,595,1008],'joint':[870,640,1125,900]},'postSize':[84,188],'postBottom':252},
 'meadow_stone_wall':{'source':'sources/ranch_v02_stone_wall_parts_native.png','rects':{'h':[100,155,1055,440],'v':[1140,10,1395,565],'post':[345,505,690,1000],'joint':[900,660,1190,940]},'postSize':[112,164],'postBottom':246}}

def component(source,box,keep):
 im=Image.open(R/source).convert('RGBA').crop(box);a=np.array(im)
 labels,n=ndimage.label(a[:,:,3]>=32,np.ones((3,3),bool));sizes=np.bincount(labels.ravel());sizes[0]=0
 chosen=np.argsort(sizes)[-keep:];mask=np.isin(labels,chosen)&(labels>0)
 # Retain native AA within two pixels of the actual body, not invisible RGB haze.
 support=ndimage.binary_dilation(mask,iterations=2);a[:,:,3]=np.where(support,a[:,:,3],0)
 # Normalizing a near-opaque core is alpha preparation only; native RGB is never painted.
 a[:,:,3]=np.where(a[:,:,3]>=245,255,a[:,:,3]);a[a[:,:,3]<8,3]=0
 bb=Image.fromarray(a[:,:,3]).getbbox();assert bb,(source,box)
 return Image.fromarray(a,'RGBA').crop(bb)

def carrier(part,axis):
 if axis=='h':
  im=part.resize((round(part.width*80/part.height),80),Image.Resampling.LANCZOS)
  if im.width>=256:im=im.crop(((im.width-256)//2,0,(im.width-256)//2+256,80))
  else:
   gap=256-im.width;mid=im.width//2;base=Image.new('RGBA',(256,80));base.paste(im.crop((0,0,mid,80)),(0,0));base.paste(im.crop((mid,0,mid+1,80)).resize((gap,80),Image.Resampling.NEAREST),(mid,0));base.paste(im.crop((mid,0,im.width,80)),(mid+gap,0));im=base
  # Preserve one complete native cross-section at both connected ends. Only
  # crop+uniform cross-section extrusion; no RGB drawing/recolor/posterization.
  profile=im.crop((48,0,49,80));port=profile.resize((32,80),Image.Resampling.NEAREST)
  im.paste(port,(0,0));im.paste(port,(224,0))
 else:
  im=part.resize((80,round(part.height*80/part.width)),Image.Resampling.LANCZOS)
  if im.height>=256:im=im.crop((0,(im.height-256)//2,80,(im.height-256)//2+256))
  else:
   gap=256-im.height;mid=im.height//2;base=Image.new('RGBA',(80,256));base.paste(im.crop((0,0,80,mid)),(0,0));base.paste(im.crop((0,mid,80,mid+1)).resize((80,gap),Image.Resampling.NEAREST),(0,mid));base.paste(im.crop((0,mid,80,im.height)),(0,mid+gap));im=base
  # End rows are a shared native cross-section through a full material face.
  row=64
  profile=im.crop((0,row,80,row+1));port=profile.resize((80,32),Image.Resampling.NEAREST)
  im.paste(port,(0,0));im.paste(port,(0,224))
 return im

def blit_raw(out,im,xy):out.alpha_composite(im,xy)
def compose(mask,h,v,post):
 im=Image.new('RGBA',(320,320))
 # Each cell owns partial alpha within [32,288)x[32,288). A tiny identical
 # opaque connector bleed added below protects this edge during minification.
 if mask&8:blit_raw(im,h.crop((0,0,128,80)),(32,120))
 if mask&2:blit_raw(im,h.crop((128,0,256,80)),(160,120))
 if mask&1:blit_raw(im,v.crop((0,0,80,128)),(120,32))
 if mask&4:blit_raw(im,v.crop((0,128,80,256)),(120,160))
 if mask not in [5,10]:blit_raw(im,post,(160-post.width//2,252-post.height))
 # Bilinear/LANCZOS minification samples transparent padding beyond a logically
 # owned edge. Copy ONLY an identical fully opaque native port core4px farther;
 # partial-alpha contour pixels keep half-open ownership and never double blend.
 a=np.array(im)
 for bit,axis,index,dest in [(8,'x',32,range(28,32)),(2,'x',287,range(288,292)),(1,'y',32,range(28,32)),(4,'y',287,range(288,292))]:
  if not mask&bit:continue
  profile=a[:,index,:].copy()if axis=='x'else a[index,:,:].copy();profile[profile[:,3]!=255]=0
  for k in dest:
   if axis=='x':a[:,k,:]=profile
   else:a[k,:,:]=profile
 return Image.fromarray(a,'RGBA')

ASSETS=[];PARTS={};SOURCES=[]
for family,spec in SPECS.items():
 src=R/spec['source'];SOURCES.append({'file':spec['source'],'sha256':sha(src),'generatedBy':'built-in image_gen','runtimeSelectable':False})
 parts={k:component(spec['source'],tuple(box),2 if family=='cream_fence'and k in ['h','v']else 1)for k,box in spec['rects'].items()}
 for name,im in parts.items():save(im,R/f'production/components/{family}_{name}_native.png')
 h=carrier(parts['h'],'h');v=carrier(parts['v'],'v')
 save(h,R/f'production/components/{family}_horizontal_carrier.png');save(v,R/f'production/components/{family}_vertical_carrier.png')
 # A uniform fit within a generous post slot. Keep the native aspect ratio.
 post=parts['post'];mw,mh=spec['postSize'];s=min(mw/post.width,mh/post.height)
 post=post.resize((round(post.width*s),round(post.height*s)),Image.Resampling.LANCZOS)
 save(post,R/f'production/components/{family}_post_registered.png')
 PARTS[family]=(h,v,post)
 for mask in range(16):
  im=compose(mask,h,v,post);f=f'decor/connected/{family}/n{mask:02d}.png';save(im,R/f)
  ASSETS.append({'file':f,'width':320,'height':320,'ppu':256,'pivot':[.5,.5],'logicalRect':[32,32,256,256],'footprintCells':[1,1],
   'sha256':sha(R/f),'kind':'connected_tile','family':family,'neighborMask':mask,'neighborBits':{'N':1,'E':2,'S':4,'W':8},'cardinalNeighborsOnly':True,
   'connectionKey':['family','connectionGroup','layer','height'],'junctionPost':mask not in[5,10],'straightCarrierHasCenterPost':False,
   'sourceFiles':[spec['source']],'componentFiles':[f'production/components/{family}_{q}'for q in ['horizontal_carrier.png','vertical_carrier.png','post_registered.png']],
   'actualGameplayFootprintCells':None,'deriveColliderFromAlpha':False,'registerGameplayCellFromThisRecord':False,'nativeRgbaRepainted':False,
   'connectionOwnership':'half-open logicalRect for partial alpha; connected native alpha255 core only has4px identicalRGB bleed; one center post per endpoint/junction',
   'artSizing':'Connected arm reaches logical cell edge. Unconnected arms are absent; native upright end/junction post is one shared vertex shape.'})
 f=f'decor/connected/{family}/periodic_post.png';im=Image.new('RGBA',(320,320));blit_raw(im,post,(160-post.width//2,252-post.height));save(im,R/f)
 ASSETS.append({'file':f,'width':320,'height':320,'ppu':256,'pivot':[.5,.5],'logicalRect':[32,32,256,256],'footprintCells':[1,1],
  'sha256':sha(R/f),'kind':'periodic_post','family':family,'sourceFiles':[spec['source']],
  'actualGameplayFootprintCells':None,'deriveColliderFromAlpha':False,'registerGameplayCellFromThisRecord':False,
  'placementPolicy':'Optional only on n05 or n10. Deterministic 3/4world-cell interval chosen by host, never every cell. End/corner/T/cross already owns its post.'})
manifest={'schemaVersion':1,'packageId':'ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02','themeDisplayName':'별길 목장','assets':ASSETS,'clips':[],
 'sources':SOURCES,'generationPrompts':'generation_prompts.json','pixelsPerUnit':256,'worldCellSize':1,'maskBits':{'N':1,'E':2,'S':4,'W':8},
 'imageCoordinates':'pixel origin top-left; pivot normalized Unity bottom-left','connectionFamilies':list(SPECS),
 'connectionConditions':'Same family, connectionGroup, decorative layer and host height only; never infer a link from any land neighbor.',
 'noGameplayColliderOrCost':True,'baseBridgeDecorPolicy':'Existing V12 bridge boundary rails/posts are independent. Never add these ranch decorative networks automatically over base bridge boundaries.',
 'portPolicy':{'logicalOwnershipPixels':[32,32,288,288],'nonzeroConnectedPadding':True,'opaquePortBleedPixels':4,'bleedAlphaValue':255,'partialAlphaPaddingPixels':0,'sameNativeRgbOverOpaqueOverlap':True,'edgeCrossSectionRgbaExact':True,'rgba64OverlapIdenticalClaimed':False,
  'whyNot64OverlapEquality':'The canvas overlap contains disjoint logical alpha plus identicalRGBA alpha255 connector core bleed. Test partial-alpha ownership and identical opaque overlap, not whole-overlap equality.'},
 'straightPostPolicy':{'eligibleMasks':[5,10],'periodicAccent':'decor/connected/{family}/periodic_post.png','globalDeterministicSpacingCells':[3,4],'noPerCellEndPosts':True},
 'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN','actualHostBindingsVerified':False}
write(R/'rails_manifest.json',manifest)

# Contact sheet and actual multi-cell assembly proof. All visible prop RGB comes
# from the newly generated native components. Only debug backgrounds/text/grid
# are drawn below; they are not runtime sprites.
def tile(f):return Image.open(R/f).convert('RGBA')
sheet=Image.new('RGBA',(1440,1000),(10,24,35,255));d=ImageDraw.Draw(sheet)
d.text((24,16),'STARWAY RANCH / CONTINUOUS DECORATION / 16 MASKS EACH',font=font(25),fill=(255,231,176))
d.text((24,52),'N1 E2 S4 W8 / native parts / one center post at ends and junctions / UNITY NOT_RUN',font=font(16),fill=(111,216,201))
for g,family in enumerate(SPECS):
 x0=g*720
 for mask in range(16):
  x=x0+16+(mask%4)*176;y=96+(mask//4)*218
  d.rectangle((x,y,x+164,y+164),fill=(26,47,53),outline=(65,106,111))
  spr=tile(f'decor/connected/{family}/n{mask:02d}.png').resize((200,200),Image.Resampling.LANCZOS);sheet.alpha_composite(spr,(x-18,y-18))
  d.text((x+7,y+176),f'{mask:02d} / '+('straight'if mask in[5,10]else'junction'),font=font(15),fill=(255,231,176))
 d.text((x0+20,970),family,font=font(19),fill=(111,216,201))
save(sheet.convert('RGB'),R/'inspection/rails_16mask_contact_sheet.png')

CELLS={(x,1)for x in range(1,12)}|{(3,y)for y in range(1,8)}|{(x,7)for x in range(3,9)}|{(8,y)for y in range(3,8)}|{(x,3)for x in range(8,12)}|{(11,y)for y in range(1,4)}
N=[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]
MASKS={p:sum(b for dx,dy,b in N if(p[0]+dx,p[1]+dy)in CELLS)for p in CELLS}
placements=[];maps=Image.new('RGBA',(1792,1344),(14,35,44,255));d=ImageDraw.Draw(maps)
d.text((28,20),'CONNECTED RUNS / END, ELBOW, T, CROSS / NO PER-CELL END POSTS',font=font(25),fill=(255,231,176))
for g,family in enumerate(SPECS):
 offset=(80,90+g*620);cell=64
 d.text((offset[0],offset[1]),family,font=font(22),fill=(111,216,201))
 for x,y in sorted(CELLS,key=lambda p:(p[1],p[0])):
  mask=sum(b for dx,dy,b in N if(x+dx,y+dy)in CELLS);f=f'decor/connected/{family}/n{mask:02d}.png'
  spr=tile(f).resize((80,80),Image.Resampling.LANCZOS);px=offset[0]+x*cell-8;py=offset[1]+y*cell-8
  maps.alpha_composite(spr,(px,py));axisNeighbors=[(x-1,y),(x+1,y)]if mask==10 else[(x,y-1),(x,y+1)]
  accent=mask in[5,10]and((x if mask==10 else y)%4==0)and all(MASKS.get(p)==mask for p in axisNeighbors)
  if accent:maps.alpha_composite(tile(f'decor/connected/{family}/periodic_post.png').resize((80,80),Image.Resampling.LANCZOS),(px,py))
  placements.append({'family':family,'column':x,'rowPositiveDown':y,'mask':mask,'file':f,'periodicPostAccent':accent})
 d.text((1050,offset[1]+125),'Continuous one-world-cell carrier',font=font(23),fill=(255,231,176))
 d.text((1050,offset[1]+168),'Ends and junctions share ONE post.',font=font(21),fill=(111,216,201))
 d.text((1050,offset[1]+210),'Straight runs use stable 4-cell accents.',font=font(20),fill=(111,216,201))
 d.text((1050,offset[1]+252),'Existing bridge rail system is separate.',font=font(19),fill=(170,191,195))
save(maps.convert('RGB'),R/'inspection/rails_connected_layout.png');write(R/'inspection/rails_connected_layout.json',{'cellPixels':64,'placements':placements,'actualUnityExecuted':False})

# Independent port/composition checks at exact runtime256pixel/world placement.
CHECKS=[];PAIRCOUNT=0
for family in SPECS:
 imgs={m:np.array(tile(f'decor/connected/{family}/n{m:02d}.png'))for m in range(16)}
 for m,a in imgs.items():
  assert a.shape==(320,320,4)
  outside=a[:,:,3].copy();outside[32:288,32:288]=0;assert not np.any((outside>0)&(outside<255)),(family,m,'partial-alpha padding')
  guard=outside.copy();guard[28:292,28:292]=0;assert not guard.any(),(family,m,'bleed farther than4px')
  for edge,bit,probe in [('N',1,a[32,:,:]),('E',2,a[:,287,:]),('S',4,a[287,:,:]),('W',8,a[:,32,:])]:
   if m&bit:assert probe[:,3].max()>0,(family,m,edge,'missing native port')
   else:assert probe[:,3].max()==0,(family,m,edge,'spurious port')
 for a in range(16):
  for b in range(16):
   if a&2 and b&8:assert np.array_equal(imgs[a][:,287,:],imgs[b][:,32,:]),(family,a,b,'E/W port mismatch');PAIRCOUNT+=1
   if a&4 and b&1:assert np.array_equal(imgs[a][287,:,:],imgs[b][32,:,:]),(family,a,b,'S/N port mismatch');PAIRCOUNT+=1
 for mask,axis in [(10,'h'),(5,'v')]:
  # Three-cell actual compositing: the shared edge has no duplicate renderer
  # blend and the native profile remains constant in the last/first4px.
  run=Image.new('RGBA',(832,832))
  for i in range(3):run.alpha_composite(tile(f'decor/connected/{family}/n{mask:02d}.png'),(i*256,0)if axis=='h'else(0,i*256))
  q=np.array(run)
  for i in [1,2]:
   k=32+i*256
   if axis=='h':
    for x in range(k-4,k+4):assert np.array_equal(q[:,x,:],q[:,k,:]),(family,'H seam stripe')
   else:
    for y in range(k-4,k+4):assert np.array_equal(q[y,:,:],q[k,:,:]),(family,'V seam stripe')
 minification=[]
 for ppw in [64,80,100]:
  for mask,axis in [(10,'h'),(5,'v')]:
   spr=tile(f'decor/connected/{family}/n{mask:02d}.png').resize((round(320*ppw/256),round(320*ppw/256)),Image.Resampling.LANCZOS)
   out=Image.new('RGBA',(ppw*4,ppw*4))
   for i in range(3):out.alpha_composite(spr,(i*ppw,0)if axis=='h'else(0,i*ppw))
   q=np.array(out)
   if axis=='v':q=np.swapaxes(q,0,1)
   seam=round(32*ppw/256)+ppw;ref=q[:,seam-6,3];fullRows=ref>=250
   rows=ndimage.binary_erosion(fullRows,iterations=2)
   assert rows.any();drop=max(255-int(q[rows,k,3].min())for k in range(seam-2,seam+3));assert drop<=2,(family,ppw,axis,drop)
   outlineDrop=max(255-int(q[fullRows,k,3].min())for k in range(seam-2,seam+3))
   minification.append({'pixelsPerCell':ppw,'axis':axis,'maxOpaqueCoreAlphaDrop':drop,'coreDefinition':'reference alpha>=250 eroded2screenPixels to exclude native AA outline','maxDropIncludingAntialiasedOuterBoundary':outlineDrop})
 CHECKS.append({'family':family,'all16Masks':True,'opaquePortBleedPixels':4,'partialAlphaPaddingPixels':0,'canonicalPortPairsExactRgba':True,'actualThreeCellSeamsNoGapOrDoubleBlend':True,'straightMasksHaveNoCenterPost':True,'periodicPostDrawnOnceOnly':True,'lanczosMinification':minification})
for a in ASSETS:
 p=R/a['file'];im=Image.open(p);im.load();assert sha(p)==a['sha256']and im.size==(a['width'],a['height'])
qa={'status':'PASS_OFFLINE','runtimePngFiles':len(ASSETS),'connectedMasks':32,'periodicAccents':2,'newNativeSources':len(SOURCES),
 'nativeRgbaRepainted':False,'portPairComparisons':PAIRCOUNT,'maxPortRgbaDifference':0,'allRuntimePngDecoded':True,'checks':CHECKS,
 'manifestSha256':sha(R/'rails_manifest.json'),'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlayMode':'NOT_RUN','actualGameCollisionChanged':False,
 'sourcePolicy':'New built-in imagegen native RGBA parts; crop, alpha cleanup/core normalization, uniform component registration, native cross-section extrusion and composition only.',
 'sources':[{'file':s['file'],'sha256':s['sha256']}for s in SOURCES]}
write(R/'rails_qa.json',qa)
print(json.dumps({'status':qa['status'],'runtimePngFiles':len(ASSETS),'portPairs':PAIRCOUNT,'manifestSha256':qa['manifestSha256']}))
