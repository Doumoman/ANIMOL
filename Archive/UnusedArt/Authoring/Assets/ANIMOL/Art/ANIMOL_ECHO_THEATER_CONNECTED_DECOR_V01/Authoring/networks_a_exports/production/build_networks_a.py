from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage
import numpy as np
import json, hashlib

ROOT=Path(__file__).resolve().parents[1]
SIZE=(320,384)
LOGICAL=(32,96,256,256)
PIVOT=[0.5,160/384]
SPECS={
 'velvet_aisle_barrier':{'rects':{'h':[20,110,1240,330],'v':[1260,10,1485,990],'post':[240,340,590,960],'joint':[730,600,1070,930]},'carrier':64,'postSize':[80,176],'postBottom':300,'periodic':True},
 'carved_stage_balustrade':{'rects':{'h':[0,120,1330,450],'v':[1320,0,1520,1010],'post':[220,490,510,970],'joint':[780,690,1090,950]},'carrier':96,'postSize':[92,172],'postBottom':300,'periodic':True},
 'prop_shelf_run':{'rects':{'h':[0,110,1220,500],'v':[1240,30,1520,930],'post':[150,510,600,980],'joint':[740,620,1120,980]},'carrier':112,'postSize':[116,140],'postBottom':296,'periodic':False}
}

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def save(im,path):
 path.parent.mkdir(parents=True,exist_ok=True);im.save(path);Image.open(path).load()
def write(path,data):path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
def clean_component(source,box):
 a=np.array(Image.open(source).convert('RGBA').crop(box))
 labels,_=ndimage.label(a[:,:,3]>=128,np.ones((3,3),bool));sizes=np.bincount(labels.ravel());sizes[0]=0
 body=labels==int(sizes.argmax());assert body.any()
 support=ndimage.binary_dilation(body,iterations=2)
 a[:,:,3]=np.where(support,a[:,:,3],0)
 a[:,:,3]=np.where(a[:,:,3]>=245,255,a[:,:,3]);a[a[:,:,3]<8,3]=0;a[a[:,:,3]==0,:3]=0
 bb=Image.fromarray(a[:,:,3]).getbbox();assert bb
 return Image.fromarray(a,'RGBA').crop(bb)
def carrier(im,axis,thickness):
 if axis=='h':
  im=im.resize((round(im.width*thickness/im.height),thickness),Image.Resampling.LANCZOS)
  assert im.width>=256
  x=(im.width-256)//2;im=im.crop((x,0,x+256,thickness))
  profile=im.crop((48,0,49,thickness)).resize((32,thickness),Image.Resampling.NEAREST)
  im.paste(profile,(0,0));im.paste(profile,(224,0))
 else:
  im=im.resize((thickness,round(im.height*thickness/im.width)),Image.Resampling.LANCZOS)
  assert im.height>=256
  y=(im.height-256)//2;im=im.crop((0,y,thickness,y+256))
  profile=im.crop((0,64,thickness,65)).resize((thickness,32),Image.Resampling.NEAREST)
  im.paste(profile,(0,0));im.paste(profile,(0,224))
 a=np.array(im);a[a[:,:,3]>=245,3]=255;a[a[:,:,3]<8,3]=0;a[a[:,:,3]==0,:3]=0
 return Image.fromarray(a,'RGBA')
def compose(mask,h,v,post,bottom):
 out=Image.new('RGBA',SIZE)
 t=h.height
 if mask&8:out.alpha_composite(h.crop((0,0,128,t)),(32,224-t//2))
 if mask&2:out.alpha_composite(h.crop((128,0,256,t)),(160,224-t//2))
 if mask&1:out.alpha_composite(v.crop((0,0,t,128)),(160-t//2,96))
 if mask&4:out.alpha_composite(v.crop((0,128,t,256)),(160-t//2,224))
 if mask not in [5,10]:out.alpha_composite(post,(160-post.width//2,bottom-post.height))
 a=np.array(out)
 for bit,axis,k,dest in [(8,'x',32,range(28,32)),(2,'x',287,range(288,292)),(1,'y',96,range(92,96)),(4,'y',351,range(352,356))]:
  if not mask&bit:continue
  profile=a[:,k,:].copy()if axis=='x'else a[k,:,:].copy()
  profile[profile[:,3]!=255]=0
  for n in dest:
   if axis=='x':a[:,n,:]=profile
   else:a[n,:,:]=profile
 a[a[:,:,3]==0,:3]=0
 return Image.fromarray(a,'RGBA')

assets=[];sources=[];checks=[]
for family,spec in SPECS.items():
 src=ROOT/'sources'/f'{family}_parts_native.png'
 sources.append({'file':str(src.relative_to(ROOT)),'sha256':sha(src),'generatedBy':'built-in image_gen','runtimeSelectable':False})
 parts={name:clean_component(src,box)for name,box in spec['rects'].items()}
 for name,im in parts.items():save(im,ROOT/f'production/components/{family}_{name}_native.png')
 h=carrier(parts['h'],'h',spec['carrier']);v=carrier(parts['v'],'v',spec['carrier'])
 save(h,ROOT/f'production/components/{family}_horizontal_carrier.png');save(v,ROOT/f'production/components/{family}_vertical_carrier.png')
 pw,ph=spec['postSize'];post=parts['post'];scale=min(pw/post.width,ph/post.height)
 post=post.resize((round(post.width*scale),round(post.height*scale)),Image.Resampling.LANCZOS)
 a=np.array(post);a[a[:,:,3]>=245,3]=255;a[a[:,:,3]<8,3]=0;a[a[:,:,3]==0,:3]=0;post=Image.fromarray(a,'RGBA')
 save(post,ROOT/f'production/components/{family}_post_registered.png')
 for mask in range(16):
  file=f'decor/connected/{family}/n{mask:02d}.png';save(compose(mask,h,v,post,spec['postBottom']),ROOT/file)
  assets.append({'file':file,'width':320,'height':384,'ppu':256,'pivot':PIVOT,'logicalRect':list(LOGICAL),'footprintCells':[1,1],'family':family,'kind':'connected_tile','neighborMask':mask,'neighborBits':{'N':1,'E':2,'S':4,'W':8},'sha256':sha(ROOT/file),'sourceFiles':[str(src.relative_to(ROOT))],'junctionPost':mask not in[5,10],'straightCarrierHasCenterPost':False,'noGameplayColliderOrCost':True,'nativeRgbRepainted':False})
 if spec['periodic']:
  file=f'decor/connected/{family}/periodic_post.png';im=Image.new('RGBA',SIZE);im.alpha_composite(post,(160-post.width//2,spec['postBottom']-post.height));save(im,ROOT/file)
  assets.append({'file':file,'width':320,'height':384,'ppu':256,'pivot':PIVOT,'logicalRect':list(LOGICAL),'footprintCells':[1,1],'family':family,'kind':'periodic_post','sha256':sha(ROOT/file),'sourceFiles':[str(src.relative_to(ROOT))],'placementPolicy':'Optional stable world-coordinate spacing3/4 only on masks5/10 with both along-axis neighbors also same straight mask. Never everycell or nexttoend/junction.','noGameplayColliderOrCost':True,'nativeRgbRepainted':False})
 images={m:np.array(Image.open(ROOT/f'decor/connected/{family}/n{m:02d}.png'))for m in range(16)}
 pairs=0;unsafe=0
 for m,a in images.items():
  outside=a[:,:,3].copy();outside[96:352,32:288]=0
  assert not np.any((outside>0)&(outside<255)),(family,m,'partial-alpha padding')
  for edge,bit,profile in [('N',1,a[96,:,:]),('E',2,a[:,287,:]),('S',4,a[351,:,:]),('W',8,a[:,32,:])]:
   assert (profile[:,3].max()>0)==bool(m&bit),(family,m,edge,'port availability')
 for ma,a in images.items():
  for mb,b in images.items():
   if ma&2 and mb&8:
    assert np.array_equal(a[:,287,:],b[:,32,:]),(family,ma,mb,'E/W port')
    aa=a[:,256:320,:];bb=b[:,:64,:];over=(aa[:,:,3]>0)&(bb[:,:,3]>0)
    bad=over&((aa[:,:,3]!=255)|(bb[:,:,3]!=255)|np.any(aa!=bb,axis=2));unsafe+=int(bad.sum());pairs+=1
   if ma&4 and mb&1:
    assert np.array_equal(a[351,:,:],b[96,:,:]),(family,ma,mb,'S/N port')
    aa=a[256:384,:,:];bb=b[:128,:,:];over=(aa[:,:,3]>0)&(bb[:,:,3]>0)
    bad=over&((aa[:,:,3]!=255)|(bb[:,:,3]!=255)|np.any(aa!=bb,axis=2));unsafe+=int(bad.sum());pairs+=1
 assert unsafe==0,(family,unsafe)
 checks.append({'family':family,'masks':16,'portPairComparisons':pairs,'exactNativePortRgba':True,'unsafeOverlappingPixels':unsafe,'straightCarriersHaveNoCenterPost':True})

manifest={'schemaVersion':1,'packageId':'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01','themeDisplayName':'잔영 극장','assets':assets,'groups':[],'sources':sources,'sourcePrompts':'generation_prompts.json','coordinateConvention':'logicalRect top-left; pivot normalized Unity bottom-left; cardinal N1 E2 S4 W8; +Yworld north','portPolicy':{'opaqueSafeBleedPixels':4,'partialAlphaPaddingPixels':0,'opaqueOverlapRequiresIdenticalRgba':True},'productionPolicy':'New built-in imagegen native components; alpha isolation/registration/uniform resizing/cropping/native cross-section extrusion/kit composition only. No generated-RGB repaint or recolored Ranch source.','actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
write(ROOT/'manifest.json',manifest)
qa={'status':'PASS_OFFLINE','runtimePngFiles':len(assets),'distinctDecorationFamilies':3,'nativeSources':len(sources),'checks':checks,'manifestSha256':sha(ROOT/'manifest.json'),'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
write(ROOT/'networks_a_qa.json',qa)

sheet=Image.new('RGBA',(1840,1710),(25,21,40,255));d=ImageDraw.Draw(sheet)
d.text((24,18),'ECHO THEATER / THREE CONNECTED DECORATION FAMILIES',font=font(27),fill=(255,225,162))
d.text((24,57),'Native source kits /16 cardinal states each /periodic posts on first2families /Unity NOT_RUN',font=font(17),fill=(174,158,194))
for g,family in enumerate(SPECS):
 x0=g*610;d.text((x0+18,104),family,font=font(21),fill=(255,225,162))
 for m in range(16):
  x=x0+12+(m%4)*148;y=153+(m//4)*325
  d.rectangle((x,y,x+140,y+280),fill=(47,37,62),outline=(90,70,96))
  spr=Image.open(ROOT/f'decor/connected/{family}/n{m:02d}.png').resize((160,192),Image.Resampling.LANCZOS);sheet.alpha_composite(spr,(x-10,y+30))
  d.text((x+8,y+244),f'n{m:02d}',font=font(21),fill=(255,225,162))
save(sheet.convert('RGB'),ROOT/'inspection/networks_a_all_masks.png')

CELLS={(x,1)for x in range(1,11)}|{(3,y)for y in range(1,7)}|{(x,6)for x in range(3,8)}|{(7,y)for y in range(3,7)}|{(x,3)for x in range(7,11)}|{(10,y)for y in range(1,4)}
NEIGH=[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]
layout=Image.new('RGBA',(1740,1800),(25,21,40,255));d=ImageDraw.Draw(layout)
d.text((28,18),'CONTINUOUS RUNS / ELBOWS / T / CROSS / FINISHED CAPS',font=font(28),fill=(255,225,162));placements=[]
for g,family in enumerate(SPECS):
 cell=82;ox=35;oy=96+g*560;d.text((ox,oy),family,font=font(23),fill=(255,225,162))
 masks={p:sum(bit for dx,dy,bit in NEIGH if(p[0]+dx,p[1]+dy)in CELLS)for p in CELLS}
 for (x,y),m in sorted(masks.items(),key=lambda i:(i[0][1],i[0][0])):
  file=f'decor/connected/{family}/n{m:02d}.png';spr=Image.open(ROOT/file).resize((round(320*cell/256),round(384*cell/256)),Image.Resampling.LANCZOS)
  px=ox+x*cell-round(32*cell/256);py=oy+y*cell-round(96*cell/256);layout.alpha_composite(spr,(px,py))
  along=[(x-1,y),(x+1,y)]if m==10 else[(x,y-1),(x,y+1)]
  periodic=SPECS[family]['periodic']and m in[5,10]and((x if m==10 else y)%4==0)and all(masks.get(p)==m for p in along)
  if periodic:layout.alpha_composite(Image.open(ROOT/f'decor/connected/{family}/periodic_post.png').resize(spr.size,Image.Resampling.LANCZOS),(px,py))
  placements.append({'family':family,'column':x,'rowPositiveDown':y,'mask':m,'file':file,'periodicPost':periodic})
 d.text((1100,oy+140),'One shared native port profile',font=font(22),fill=(255,225,162))
 d.text((1100,oy+180),'One end/junction cap percell',font=font(22),fill=(187,166,202))
 d.text((1100,oy+220),'No endposts on straight carriers',font=font(21),fill=(187,166,202))
save(layout.convert('RGB'),ROOT/'inspection/networks_a_connected_runs.png')
write(ROOT/'inspection/networks_a_layout.json',{'cellPixels':82,'placements':placements,'actualUnityPlay':'NOT_RUN'})
print(json.dumps(qa,ensure_ascii=False))
