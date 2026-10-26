from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,runpy
import numpy as np
W=Path(__file__).resolve().parents[1]
env=runpy.run_path(str(W/'production/build_meadow.py'))
ims={n:Image.open(W/f'decor/connected/raised_flower_meadow/n{n:03d}.png').convert('RGBA')for n in range(256)}
arr={n:np.asarray(im)for n,im in ims.items()}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,j):p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
BITS=env['BITS'];occ_for=env['occ_for'];render_field=env['render_field']
def mask_at(o,x,y):return sum(b for dx,dy,b in BITS if(x+dx,y+dy)in o)
def compatible_h(a,b):
 return bool(a&4)and bool(b&64)and all(bool(a&x)==bool(b&y)for x,y in [(1,128),(2,1),(16,32),(8,16)])
def compatible_v(a,b):
 # b is immediately SOUTH in the image-row convention.
 return bool(a&16)and bool(b&1)and all(bool(a&x)==bool(b&y)for x,y in [(64,128),(32,64),(4,2),(8,4)])
counts={};duplicates={};safe_opaque_duplicates={};max_reference_diff={};port_holes={}
for orientation,compatible,shift in [('horizontal',compatible_h,(1,0)),('vertical',compatible_v,(0,1))]:
 count=dup=safe_dup=badref=holes=0;largest=0
 for a in range(256):
  for b in range(256):
   if not compatible(a,b):continue
   count+=1;dx,dy=shift
   if orientation=='horizontal':rgba_a=arr[a][:,256:320];rgba_b=arr[b][:,:64]
   else:rgba_a=arr[a][256:320,:];rgba_b=arr[b][:64,:]
   aa=rgba_a[:,:,3];bb=rgba_b[:,:,3];both=(aa>0)&(bb>0)
   safe=both&(aa==255)&(bb==255)&np.all(rgba_a==rgba_b,axis=2)
   dup+=int(np.count_nonzero(both&~safe));safe_dup+=int(np.count_nonzero(safe))
   # Explicit single native field on the union of BOTH 3x3 neighborhoods.
   # Crop exactly the shared world window and compare composite PNG color+alpha.
   union=occ_for(a)|{(x+dx,y+dy)for x,y in occ_for(b)}
   if orientation=='horizontal':
    pair=Image.new('RGBA',(576,320));pair.alpha_composite(ims[a],(0,0));pair.alpha_composite(ims[b],(256,0));p=np.array(pair.crop((256,0,320,320)))
    full=render_field(union,(1088,832),(288,288));ref=np.array(full.crop((512,256,576,576)))
    occupied_union=union
   else:
    pair=Image.new('RGBA',(320,576));pair.alpha_composite(ims[a],(0,0));pair.alpha_composite(ims[b],(0,256));p=np.array(pair.crop((0,256,320,320)))
    full=render_field(union,(832,1088),(288,288));ref=np.array(full.crop((256,512,576,576)))
   # At outer-overhang pixels a THIRD occupied cell can be the rightful owner.
   # Add those exact current PNGs before the reference comparison, no fake blanks.
   complete=Image.new('RGBA',full.size)
   for x,y in sorted(union,key=lambda v:(v[1],v[0])):
    m=mask_at(union,x,y);complete.alpha_composite(ims[m],(256+x*256,256+y*256))
   box=(512,256,576,576)if orientation=='horizontal'else(256,512,576,576)
   actual=np.asarray(complete.crop(box));diff=int(np.abs(actual.astype(np.int16)-ref.astype(np.int16)).max());largest=max(largest,diff);badref+=int(diff!=0)
   # The actual shared interior port must be opaque, rather than an empty sprite
   # making the no-double-alpha assertion trivially pass.
   if orientation=='horizontal':port=np.asarray(complete)[320:512,543:545,3]
   else:port=np.asarray(complete)[543:545,320:512,3]
   holes+=int(np.count_nonzero(port!=255))
 counts[orientation]=count;duplicates[orientation]=dup;safe_opaque_duplicates[orientation]=safe_dup;max_reference_diff[orientation]={'maxRGBAChannelDifference':largest,'mismatchingPairs':badref};port_holes[orientation]=holes
 assert count==1024,(orientation,count)
 assert dup==0,(orientation,dup)
 assert largest==0,(orientation,largest,badref)
 assert holes==0,(orientation,holes)

FONT='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def font(n):return ImageFont.truetype(FONT,n)
fixtures={
 '2 CELLS / ONE BED':{(0,0),(1,0)},
 '3 CELLS / ONE BED':{(0,0),(1,0),(2,0)},
 '4 CELLS / ONE BED':{(0,0),(1,0),(2,0),(3,0)},
 'L / NATIVE OUTER RIM':{(0,0),(1,0),(2,0),(0,1),(0,2)},
 'T / NO INNER TILE BOXES':{(0,0),(1,0),(2,0),(1,1),(1,2)},
 'HOLE / CLEAR CENTRE':{(x,y)for x in range(3)for y in range(3)if(x,y)!=(1,1)},
 'DIAGONAL / SEPARATE TIPS':{(0,0),(1,1)},
 'IRREGULAR / FILLED REGION':{(0,0),(1,0),(2,0),(3,0),(0,1),(1,1),(2,1),(1,2),(2,2)},
}
def assemble(o):
 maxx,maxy=max(x for x,y in o),max(y for x,y in o);out=Image.new('RGBA',((maxx+1)*256+64,(maxy+1)*256+64))
 for x,y in sorted(o,key=lambda v:(v[1],v[0])):out.alpha_composite(ims[mask_at(o,x,y)],(x*256,y*256))
 return out
contact=Image.new('RGBA',(1536,1536),(10,25,38,255));d=ImageDraw.Draw(contact);d.text((24,15),'RANCH / CONNECTED RAISED MEADOW / OFFLINE ART',font=font(28),fill=(255,233,174,255))
fixture_rows=[]
for i,(label,o)in enumerate(fixtures.items()):
 out=assemble(o);full=render_field(o,out.size,(32,32));diff=int(np.abs(np.array(out).astype(np.int16)-np.array(full).astype(np.int16)).max());assert diff==0,(label,diff)
 x=(i%2)*768;y=78+(i//2)*350;d.rectangle((x+10,y,x+755,y+336),outline=(51,81,101),width=2);d.text((x+24,y+13),label,font=font(21),fill=(255,233,174,255));scale=min(650/out.width,258/out.height);size=(round(out.width*scale),round(out.height*scale));small=out.resize(size,Image.Resampling.LANCZOS);contact.alpha_composite(small,(x+48,y+68))
 if 'HOLE'in label:
  a=np.asarray(out);assert int(a[320:512,320:512,3].max())==0
 fixture_rows.append({'label':label,'cells':sorted(map(list,o)),'singleFieldReferenceMaxRGBA':diff,'rendererCount':len(o)})
contact.convert('RGB').save(W/'inspection/meadow_connected_regions.png')
grid=Image.new('RGBA',(1536,1536),(10,25,38,255));d=ImageDraw.Draw(grid)
for n in range(256):
 x=(n%16)*96;y=(n//16)*96;im=ims[n].resize((96,96),Image.Resampling.LANCZOS);grid.alpha_composite(im,(x,y));d.text((x+3,y+3),str(n),font=font(10),fill=(255,235,174,255))
grid.convert('RGB').save(W/'inspection/meadow_full256_contact.png')
flowers=Image.new('RGBA',(1120,300),(10,25,38,255));d=ImageDraw.Draw(flowers)
for i in range(4):
 im=Image.open(W/f'decor/accents/meadow/flowers_{i+1:02d}.png').convert('RGBA').resize((256,256),Image.Resampling.LANCZOS);flowers.alpha_composite(im,(i*280,5));d.text((i*280+15,266),f'NATIVE ACCENT {i+1}',font=font(17),fill=(255,235,174,255))
flowers.convert('RGB').save(W/'inspection/meadow_flower_accents.png')
manifest=json.loads((W/'meadow_manifest.json').read_text())
for a in manifest['assets']:
 p=W/a['file'];im=Image.open(p);im.load();assert im.size==(a['width'],a['height'])and sha(p)==a['sha256']
qa={'status':'PASS_OFFLINE','runtimePngs':len(manifest['assets']),'carrierMasks':256,'carrierDecodedRGBAUnique':len({hashlib.sha256(im.tobytes()).hexdigest()for im in ims.values()}),'compatiblePairs':counts,'totalCompatiblePairs':sum(counts.values()),'unsafeDuplicateAlphaPixels':duplicates,'safeIdenticalOpaqueOverlapPixels':safe_opaque_duplicates,'singleFieldReference':max_reference_diff,'opaqueInteriorPortHoles':port_holes,'fixtures':fixture_rows,'holeCentreAlpha':0,'sourceFiles':[{'file':s['file'],'sha256':sha(W/s['file'])}for s in manifest['sources']],'nativeRGBPainted':False,'nativeOperations':['native canopy swatch crop/uniform expansion','native rim crop/uniform resize','native side connector profile repeat','native short grass cutout placement','alpha-only chamfer/notch and partial-fringe nearest-cell ownership','safe opaque support padding retained to avoid bilinear alpha-cut seams'],'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN','actualColliderOrGameMapVerified':False}
write(W/'meadow_qa.json',qa)
print(json.dumps({'status':qa['status'],'pairs':qa['totalCompatiblePairs'],'maxRGBA':max(x['maxRGBAChannelDifference']for x in max_reference_diff.values()),'unsafeDuplicateAlphaPixels':sum(duplicates.values()),'safeIdenticalOpaqueOverlapPixels':sum(safe_opaque_duplicates.values()),'assets':len(manifest['assets'])}))
