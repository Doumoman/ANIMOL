"""Independent native-export geometry/PNG/mask checks; never imports producer code."""
from pathlib import Path
from PIL import Image
import argparse, hashlib, json

HERE=Path(__file__).resolve().parent
WORK=HERE.parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())

def audit_group(group, manifest):
 root=WORK/(group+'_exports');p=root/manifest
 errors=[];warnings=[];stats={};images={};assets={};sourceFiles=set();shapeRows=[]
 if not p.exists():return {'status':'NOT_READY','errors':['Missing '+str(p)],'stats':{}}
 j=read(p)
 for a in j.get('assets',[]):
  f=a.get('file');q=root/str(f)
  if not isinstance(f,str)or f.startswith('/')or'..'in Path(f).parts:errors.append('Unsafe file path '+str(f));continue
  if f in assets:errors.append('Duplicate asset '+f);continue
  assets[f]=a
  if not q.is_file():errors.append('Missing PNG '+f);continue
  try:
   im=Image.open(q);im.load();im=im.convert('RGBA');images[f]=im
  except Exception as ex:errors.append('Decode '+f+': '+type(ex).__name__);continue
  if list(im.size)!=[a.get('width'),a.get('height')]:errors.append('Dimensions '+f)
  if sha(q)!=a.get('sha256'):errors.append('SHA '+f)
  if a.get('ppu')!=256:errors.append('PPU '+f)
  lr=a.get('logicalRect',a.get('logical_rect'));fp=a.get('footprintCells');pv=a.get('pivot')
  if not(isinstance(lr,list)and len(lr)==4 and isinstance(fp,list)and len(fp)==2 and isinstance(pv,list)and len(pv)==2):errors.append('Ground metadata '+f);continue
  if [lr[2],lr[3]]!=[fp[0]*256,fp[1]*256]:errors.append('Ground pixel size '+f)
  expected=[(lr[0]+lr[2]/2)/im.width,1-(lr[1]+lr[3]/2)/im.height]
  if max(abs(pv[i]-expected[i])for i in range(2))>1e-5:errors.append('Ground center/pivot '+f)
  if a.get('actualGameplayFootprintCells','ABSENT')is not None:errors.append('Invented/missing actualGameplayFootprintCells '+f)
  if a.get('deriveColliderFromAlpha')is not False:errors.append('Collider alpha policy '+f)
  refs=a.get('sourceFiles',[])
  if not refs:errors.append('Missing sourceFiles '+f)
  for ref in refs:
   sf=ref if isinstance(ref,str)else ref.get('file')if isinstance(ref,dict)else None
   if not sf or not(root/sf).is_file():errors.append('Missing native source '+str(sf)+' <- '+f)
   else:sourceFiles.add(sf)
  bb=im.getbbox()
  if not bb:errors.append('Empty runtime art '+f)
  else:shapeRows.append({'file':f,'alphaBBox':list(bb),'visibleWidthLogicalCells':(bb[2]-bb[0])/256,'visibleHeightLogicalCells':(bb[3]-bb[1])/256,'groundCells':fp,'pivotMatchesLogicalCenter':True})
 stats.update(assets=len(assets),pngDecoded=len(images),nativeSourcesReferenced=len(sourceFiles),manifestSha256=sha(p))
 return {'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','errors':errors,'warnings':warnings,'stats':stats,'shapeRows':shapeRows,'_assets':assets,'_images':images,'_root':root}

def check_masks(report,family,count,digits):
 expected={f'decor/connected/{family}/n{n:0{digits}d}.png'for n in range(count)}
 missing=sorted(expected-set(report['_assets']))
 if missing:report['errors'].append('Incomplete '+family+': '+str(len(missing))+' masks missing')
 return not missing

def edge_pairs(report,family,diagonal):
 # Actual fullcanvas overlap at logical cell spacing256, not source arbitrary crop.
 # Native padded320 cells overlap64px; a producer may explicitly use port-only
 # partitioning, which is reported as needing its declared port audit, not hidden.
 imgs=report['_images'];digits=3 if diagonal else 2;size=256 if diagonal else 16
 p=lambda n:f'decor/connected/{family}/n{n:0{digits}d}.png'
 pairs=0;nonEqual=0;nonEqualPixels=0;sharedAlphaPixels=0;unsafeOverlapPixels=0;safeOpaqueOverlapPixels=0;examples=[]
 if not all(p(n)in imgs and imgs[p(n)].size==(320,320)for n in range(size)):
  return {'status':'NOT_READY','pairs':0,'reason':'All native masks must be320x320 before overlap checks.'}
 for axis in ['h','v']:
  for a in range(size):
   for b in range(size):
    if diagonal:
     if axis=='h':
      compatible=(a&4)and(b&64)and all(bool(a&x)==bool(b&y)for x,y in [(1,128),(2,1),(16,32),(8,16)])
     else:
      compatible=(a&1)and(b&16)and all(bool(a&x)==bool(b&y)for x,y in [(64,32),(128,64),(4,8),(2,4)])
    else:compatible=((a&2)and(b&8))if axis=='h'else((a&1)and(b&4))
    if not compatible:continue
    pairs+=1
    aa=imgs[p(a)].crop((256,0,320,320)if axis=='h'else(0,0,320,64))
    bb=imgs[p(b)].crop((0,0,64,320)if axis=='h'else(0,256,320,320))
    for x,y in zip(aa.getdata(),bb.getdata()):
     if x[3]>0 and y[3]>0:
      sharedAlphaPixels+=1
      if x[3]==255 and y[3]==255 and x[:3]==y[:3]:safeOpaqueOverlapPixels+=1
      else:unsafeOverlapPixels+=1
    if aa.tobytes()!=bb.tobytes():
     nonEqual+=1
     bad=sum(x!=y for x,y in zip(aa.getdata(),bb.getdata()));nonEqualPixels+=bad
     if len(examples)<8:examples.append({'axis':axis,'maskA':a,'maskB':b,'differentPixels':bad})
 return {'status':'PASS_SAFE_OVERLAP'if unsafeOverlapPixels==0 else'JOIN_COMPOSITION_REVIEW_REQUIRED','pairs':pairs,'expectedPairs':2048 if diagonal else 128,'sharedVisibleAlphaPixels':sharedAlphaPixels,'safeIdenticalOpaqueOverlapPixels':safeOpaqueOverlapPixels,'unsafeOverlapPixels':unsafeOverlapPixels,'rawNonEqualPairs':nonEqual,'rawNonEqualPixels':nonEqualPixels,'examples':examples,'note':'EqualRGB+alpha255 overlap is harmless and preserves bilinear coverage. DifferentRGB or partialalpha overlap is unsafe. Remaining fringe uses single ownership. Native field reference and independently filtered sprite-scale views are separate checks.'}

def rail_ports(report,family):
 imgs=report['_images'];file=lambda n:f'decor/connected/{family}/n{n:02d}.png'
 if not all(file(n)in imgs for n in range(16)):return {'status':'NOT_READY'}
 errors=[];checked=0;profilePairs=0
 for n in range(16):
  im=imgs[file(n)]
  strips={'N':im.crop((0,32,320,33)),'E':im.crop((287,0,288,320)),'S':im.crop((0,287,320,288)),'W':im.crop((32,0,33,320))}
  for d,bit in [('N',1),('E',2),('S',4),('W',8)]:
   checked+=1;present=bool(strips[d].getchannel('A').getbbox())
   if present!=bool(n&bit):errors.append({'mask':n,'side':d,'expectedPort':bool(n&bit),'actualPort':present})
 for a in range(16):
  for b in range(16):
   if(a&2)and(b&8):
    profilePairs+=1
    if imgs[file(a)].crop((287,0,288,320)).tobytes()!=imgs[file(b)].crop((32,0,33,320)).tobytes():errors.append({'axis':'H','maskA':a,'maskB':b,'error':'Opposed native port profiles differ'})
   if(a&1)and(b&4):
    profilePairs+=1
    if imgs[file(a)].crop((0,32,320,33)).tobytes()!=imgs[file(b)].crop((0,287,320,288)).tobytes():errors.append({'axis':'V','maskA':a,'maskB':b,'error':'Opposed native port profiles differ'})
 return {'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','edgePresenceChecks':checked,'opposedPortPairs':profilePairs,'errors':errors,'scope':'Native logical boundary profile, exactRGBA; closedside has no alpha at theboundary.'}

def main():
 out={};summary=[]
 for group,manifest in [('rails','rails_manifest.json'),('meadow','meadow_manifest.json'),('groups','groups_manifest.json')]:
  r=audit_group(group,manifest)
  if r['status']!='NOT_READY':
   if group=='rails':
    r['joinChecks']={f:edge_pairs(r,f,False)for f in ['cream_fence','meadow_stone_wall']if check_masks(r,f,16,2)}
    r['portChecks']={f:rail_ports(r,f)for f in ['cream_fence','meadow_stone_wall']}
    for f,c in r['portChecks'].items():
     if c['status']=='FAIL_OFFLINE':r['errors'].append('Native logical port check failed '+f)
   elif group=='meadow':
    if check_masks(r,'raised_flower_meadow',256,3):r['joinChecks']={'raised_flower_meadow':edge_pairs(r,'raised_flower_meadow',True)}
   else:
    expected={'hay_store_2x1':[2,1],'feeding_corner_3x1':[3,1],'tree_flower_corner_2x2':[2,2],'barnyard_3x2':[3,2]}
    for n,fp in expected.items():
     f='decor/groups/'+n+'.png';a=r['_assets'].get(f)
     if a is None or a.get('footprintCells')!=fp:r['errors'].append('Missing/wrong whole native group '+n)
     elif f in r['_images']:
      im=r['_images'][f];bb=im.getbbox()
      if im.width!=fp[0]*256+64:r['errors'].append('Group canvaswidth '+n)
      if bb and(bb[2]-bb[0])<.9*fp[0]*256:r['warnings'].append('Visible longwidth below90% logicalwidth '+n)
   r['status']='PASS_OFFLINE'if not r['errors']else'FAIL_OFFLINE'
  out[group]={k:v for k,v in r.items()if not k.startswith('_')};summary.append({'group':group,'status':r['status'],'errors':len(r['errors'])})
 report={'status':'PASS_OFFLINE'if all(r['status']=='PASS_OFFLINE'for r in out.values())else'PENDING_OR_FAILED','groups':out,'actualUnityImport':'NOT_RUN','csharpCompile':'NOT_RUN','actualPrefabBindings':'NOT_RUN','actualPlayMode':'NOT_RUN','scope':'Independent producer PNG/metadata/raw-mask/overlap checks. Native art visual review and real host integration are separate.'}
 p=HERE/'independent_exports_qa.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':report['status'],'groups':summary,'report':str(p)},ensure_ascii=False))

if __name__=='__main__':main()
