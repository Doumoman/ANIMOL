"""NEW native canopy/edge composition. RGB is sampled from generated parts only.
Geometry operations affect placement/alpha ownership, never paint/recolor RGB.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import hashlib,json,math
W=Path(__file__).resolve().parents[1]
SRC=W/'sources/ranch_v02_meadow_construction_native.png'
ACC=W/'sources/ranch_v02_meadow_flowers_native.png'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
def save(im,p):p.parent.mkdir(parents=True,exist_ok=True);tmp=p.with_suffix('.writing');im.save(tmp,format='PNG');Image.open(tmp).load();tmp.replace(p)
def clean(im):
 a=np.array(im.convert('RGBA'));al=a[:,:,3];al[al<12]=0;al[al>=245]=255;a[:,:,3]=al;return Image.fromarray(a)
native=clean(Image.open(SRC));acc=clean(Image.open(ACC))
P=W/'production/components';P.mkdir(parents=True,exist_ok=True)
# Exact generated meadow swatch pixel: native RGB is expanded unchanged.
BASE=native.crop((780,220,781,221));assert BASE.getpixel((0,0))[3]==255

def wide_ribbon(box,height):
 im=native.crop(box)
 # Uniform scale to an ample, sculptural leaf height, then crop the center.
 im=im.resize((round(im.width*height/im.height),height),Image.Resampling.LANCZOS)
 assert im.width>=256
 x=(im.width-256)//2
 return clean(im.crop((x,0,x+256,height)))
N=wide_ribbon((24,268,1516,422),58)
S=wide_ribbon((24,438,1516,588),60)
# Native 1-row side material profiles are extruded along their tangent; no cell endcaps.
VLEFT=native.crop((65,830,190,831)).resize((48,1),Image.Resampling.LANCZOS)
VRIGHT=native.crop((1340,830,1488,831)).resize((48,1),Image.Resampling.LANCZOS)
WEST=clean(VLEFT.resize((48,256),Image.Resampling.NEAREST));EAST=clean(VRIGHT.resize((48,256),Image.Resampling.NEAREST))
TUFT=native.crop((580,710,915,970));TUFT=clean(TUFT.resize((64,round(TUFT.height*64/TUFT.width)),Image.Resampling.LANCZOS))
for name,im in [('canopy_native_sample',BASE),('north_fringe',N),('south_fringe',S),('west_profile',WEST),('east_profile',EAST),('short_grass',TUFT)]:save(im,P/(name+'.png'))
BITS=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]
def occ_for(mask):return {(0,0)}|{(x,y)for x,y,b in BITS if mask&b}
SIZE=832;ORIGIN=288
def render_field(occupied,canvas=(SIZE,SIZE),origin=(ORIGIN,ORIGIN)):
 im=Image.new('RGBA',canvas);ox,oy=origin
 for x,y in sorted(occupied,key=lambda p:(p[1],p[0])):
  im.alpha_composite(BASE.resize((256,256),Image.Resampling.NEAREST),(ox+x*256,oy+y*256))
 # Every exposed edge is one native ribbon/profile. Connected internal borders
 # are never drawn. Side sprigs are low independent native cutouts.
 for x,y in sorted(occupied,key=lambda p:(p[1],p[0])):
  px,py=ox+x*256,oy+y*256
  if(x,y-1)not in occupied:im.alpha_composite(N,(px,py-30))
  if(x,y+1)not in occupied:im.alpha_composite(S,(px,py+256-28))
  if(x-1,y)not in occupied:
   im.alpha_composite(WEST,(px-20,py))
   for t in [48,176]:im.alpha_composite(TUFT,(px-26,py+t-18))
  if(x+1,y)not in occupied:
   im.alpha_composite(EAST,(px+256-28,py))
   for t in [80,208]:im.alpha_composite(TUFT,(px+256-38,py+t-18))
 # Large straight chamfer cuts on the OUTSIDE overhang only, using alpha. The
 # actual native leaf/RGB profiles remain untouched. Concave diagonal holes get
 # a small alpha notch rather than a raised wall filling the hole.
 a=np.array(im)
 for vx in range(min(x for x,y in occupied),max(x for x,y in occupied)+2):
  for vy in range(min(y for x,y in occupied),max(y for x,y in occupied)+2):
   qs=[((vx-1,vy-1),(-1,-1)),((vx,vy-1),(1,-1)),((vx-1,vy),(-1,1)),((vx,vy),(1,1))]
   present=[(p,s)for p,s in qs if p in occupied]
   if len(present)in(1,2):
    # Two diagonal touching beds retain separate clipped corners.
    if len(present)==2 and not(present[0][1][0]==-present[1][1][0]and present[0][1][1]==-present[1][1][1]):continue
    for _,(sx,sy)in present:
     xx,yy=ox+vx*256,oy+vy*256;x0,x1=max(0,xx-24),min(canvas[0],xx+24);y0,y1=max(0,yy-24),min(canvas[1],yy+24)
     if x0>=x1 or y0>=y1:continue
     gy,gx=np.mgrid[y0:y1,x0:x1];inside=(gx-xx)*sx+(gy-yy)*sy>=-8
     # Never delete another occupied quadrant at a diagonal touch.
     other=np.zeros(inside.shape,dtype=bool)
     for (cx,cy),_ in present:
      if (cx,cy)==(vx-(sx<0),vy-(sy<0)):continue
      other|=(gx>=ox+cx*256)&(gx<ox+(cx+1)*256)&(gy>=oy+cy*256)&(gy<oy+(cy+1)*256)
     sub=a[y0:y1,x0:x1,3];sub[~(inside|other)]=0
   elif len(present)==3:
    missing=next((p,s)for p,s in qs if p not in occupied);sx,sy=missing[1]
    xx,yy=ox+vx*256,oy+vy*256;x0,x1=max(0,xx-10),min(canvas[0],xx+10);y0,y1=max(0,yy-10),min(canvas[1],yy+10)
    if x0>=x1 or y0>=y1:continue
    gy,gx=np.mgrid[y0:y1,x0:x1];u=(gx-xx)*sx;v=(gy-yy)*sy;cut=(u>=0)&(v>=0)&(u+v<7)
    a[y0:y1,x0:x1,3][cut]=0
 a[a[:,:,3]==0,:3]=0
 return Image.fromarray(a)

GY,GX=np.mgrid[-32:288,-32:288]
def ownership(occupied):
 # Closest occupied rectangle, with lexicographic WORLD-cell tie-break. Translating
 # both a cell and its neighbors preserves this ordering, including negative cells.
 best=np.full((320,320),2**31-1,dtype=np.int64);winner=np.zeros((320,320),dtype=bool)
 for x,y in sorted(occupied):
  dx=np.maximum(np.maximum(x*256-GX,0),GX-((x+1)*256-1));dy=np.maximum(np.maximum(y*256-GY,0),GY-((y+1)*256-1));d=dx*dx+dy*dy;take=d<best;best[take]=d[take];winner[take]=(x,y)==(0,0)
 return winner
assets=[];images={}
for mask in range(256):
 o=occ_for(mask);field=render_field(o);tile=np.array(field.crop((256,256,576,576)))
 # Identical FULLY OPAQUE connector padding is retained on both neighbors: it
 # cannot double-blend and prevents bilinear half-open alpha cuts making a grid.
 # Only partially transparent fringe has a unique geometric owner.
 tile[(~ownership(o))&(tile[:,:,3]<255),3]=0;tile[tile[:,:,3]==0,:3]=0
 im=Image.fromarray(tile);f=f'decor/connected/raised_flower_meadow/n{mask:03d}.png';save(im,W/f);images[mask]=im
 assets.append({'file':f,'width':320,'height':320,'ppu':256,'pivot':[.5,.5],'logicalRect':[32,32,256,256],'logical_rect':[32,32,256,256],'footprintCells':[1,1],'sha256':sha(W/f),'kind':'connected_meadow','family':'raised_flower_meadow','neighborMask':mask,'maskConvention':'N1 NE2 E4 SE8 S16 SW32 W64 NW128','sourceFiles':['sources/ranch_v02_meadow_construction_native.png'],'actualGameplayFootprintCells':None,'deriveColliderFromAlpha':False,'registerGameplayCellFromThisRecord':False,'alphaOwnership':'identical opaque connector support retained; partial-alpha fringe nearest occupied logical cell with lexicographic world x/y tie','nativeRedraw':True})
# Four separate low flower clusters. They are OPTIONAL visuals on an existing
# meadow cell, not new footprint reservations or blocking actors.
FLOWERS=[(0,305,418,705),(418,330,784,710),(785,305,1167,710),(1167,380,1536,710)]
for i,box in enumerate(FLOWERS,1):
 im=acc.crop(box);bb=im.getchannel('A').point(lambda a:255 if a>=24 else 0).getbbox();im=im.crop(bb);scale=min(154/im.width,114/im.height);im=clean(im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS));out=Image.new('RGBA',(320,320));out.alpha_composite(im,((320-im.width)//2,178-im.height//2));f=f'decor/accents/meadow/flowers_{i:02d}.png';save(out,W/f)
 assets.append({'file':f,'width':320,'height':320,'ppu':256,'pivot':[.5,.5],'logicalRect':[32,32,256,256],'logical_rect':[32,32,256,256],'footprintCells':[1,1],'sha256':sha(W/f),'kind':'meadow_flower_accent','family':'raised_flower_meadow','sourceFiles':['sources/ranch_v02_meadow_flowers_native.png'],'sourceCrop':list(box),'actualGameplayFootprintCells':None,'deriveColliderFromAlpha':False,'registerGameplayCellFromThisRecord':False,'nativeRedraw':True,'placement':'Optional seeded accent at existing meadow cell. Never a separate gameplay/decor occupancy claim.'})
manifest={'schemaVersion':1,'packageId':'ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02','family':'raised_flower_meadow','assets':assets,'clips':[],'sources':[{'file':f'sources/{p.name}','sha256':sha(p),'generatedBy':'built-in image_gen','runtimeSelectable':False}for p in [SRC,ACC]],'sourcePolicy':'NEW native meadow kit. Mechanical crop/uniform resize/native profile repeat/placement and alpha ownership only; no RGB paint/recolor. Generated corner-atlas caps retained as native source, exported flower crops use bottom row.','notTerrain':True,'createCollider':False,'gameplayChanged':False,'connectionKey':['family','connectionGroup','layer','height'],'rawMaskBits':{'N':1,'NE':2,'E':4,'SE':8,'S':16,'SW':32,'W':64,'NW':128},'alphaOwnership':'Fully opaque identical RGBA connector support may overlap safely to prevent bilinear alpha-cut grid lines. Partially transparent fringe has nearest occupied-cell owner with deterministic world x/y tie. QA rejects partial-alpha or different-RGB overlap, then compares complete composed reference.','optionalAccents':{'kind':'meadow_flower_accent','defaultProbabilityDenominator':5,'seedIsHostSupplied':True,'noFrameRandom':True},'actualUnityImport':'NOT_RUN','csharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN'}
write(W/'meadow_manifest.json',manifest)
print(json.dumps({'status':'EXPORTED','assets':len(assets),'uniqueCarrierRgba':len({hashlib.sha256(im.tobytes()).hexdigest()for im in images.values()})}))
