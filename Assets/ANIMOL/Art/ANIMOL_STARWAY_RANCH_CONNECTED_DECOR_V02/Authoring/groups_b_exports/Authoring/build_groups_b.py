#!/usr/bin/env python3
"""Native imagegen crop, uniform registration, metadata and offline QA only."""
from pathlib import Path
import json, hashlib, math, argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import label, binary_dilation
DEFAULT_ROOT=Path(__file__).resolve().parent.parent
ap=argparse.ArgumentParser(description='Rebuild this14-prop producer in a separate export directory; never overwrite a full merged package.')
ap.add_argument('--export-root',default=str(DEFAULT_ROOT));ap.add_argument('--catalog')
args=ap.parse_args();ROOT=Path(args.export_root).resolve()
if (ROOT/'manifest.json').exists():
 prior=json.loads((ROOT/'manifest.json').read_text())
 if len(prior.get('assets',[]))>14:raise RuntimeError('Refusing to overwrite a merged package manifest. Use a separate producer export directory with copied native sources.')
if not (ROOT/'sources').is_dir():raise RuntimeError('Native sources folder missing; pass --export-root for a separate producer export and --catalog for the26-design JSON.')
(ROOT/'Inspection').mkdir(exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
catalog_path=Path(args.catalog) if args.catalog else ROOT.parent/'catalog_design.json'
CAT=json.loads(catalog_path.read_text())
rows=[*CAT['groups'][6:],*CAT['points']]
spec={'harvest_cart_2x2': ('cart', None), 'chicken_coop_2x2': ('coop', None), 'apiary_corner_2x2': ('apiary', None), 'picnic_arbor_3x2': ('arbor_angular', None), 'produce_stall_3x2': ('stall', None), 'garden_workbench_2x1': ('workbench', None), 'milk_can_pair': ('points_a', 0), 'tool_basket': ('points_a', 1), 'ranch_notice_sign': ('points_a', 2), 'watering_can': ('points_a', 3), 'feed_sack_stack': ('points_b', 0), 'flower_bucket': ('points_b', 1), 'harvest_crate': ('points_b', 2), 'honey_jar_stand': ('points_b', 3)}
assets=[];groups=[];qa=[];native=[]
accepted_sources={f'sources/ranch_{v[0]}_native.png' for v in spec.values()}
for f in sorted((ROOT/'sources').glob('*.png')):
 im=Image.open(f)
 used=str(f.relative_to(ROOT)) in accepted_sources
 native.append({'file':str(f.relative_to(ROOT)),'sha256':sha(f),'width':im.width,'height':im.height,'mode':im.mode,'generator':'built-in image_gen','promptKey':f.stem[len('ranch_'):-len('_native')],'usedByRuntime':used,'role':'accepted_native' if used else 'superseded_draft_native'})
for row in rows:
 key=row['key'];src,q=spec[key];source=ROOT/'sources'/f'ranch_{src}_native.png'
 im=Image.open(source).convert('RGBA')
 if q is None: region=(0,0,im.width,im.height)
 else:
  # Atlas components are selected from native connected alpha bodies, not guessed equal quadrants.
  aa=np.array(im)[:,:,3];labs,n=label(aa>=32);ct=np.bincount(labs.ravel());ids=sorted(range(1,n+1),key=lambda n:ct[n],reverse=True)[:4]
  boxes=[]
  for ii in ids:
   yy,xx=np.where(labs==ii);boxes.append((int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)))
  assert len(boxes)==4,(src,boxes)
  top=sorted(boxes,key=lambda b:(b[1]+b[3])/2)[:2];bottom=sorted(boxes,key=lambda b:(b[1]+b[3])/2)[2:]
  ordered=sorted(top,key=lambda b:b[0])+sorted(bottom,key=lambda b:b[0]);bb=ordered[q]
  region=(max(0,bb[0]-3),max(0,bb[1]-3),min(im.width,bb[2]+3),min(im.height,bb[3]+3))

 sub=im.crop(region);arr=np.array(sub);a=arr[:,:,3]
 labs,n=label(a>=32);counts=np.bincount(labs.ravel());keep=np.isin(labs,[i for i in range(1,n+1) if counts[i]>=max(64,counts[1:].max()*.0001)])
 # Preserve generated antialiasing inside 2 native px around real source component;
 # remove detached one-pixel specks/neartransparent background. Never repaint RGB.
 keep=binary_dilation(keep,iterations=2)
 arr[:,:,3][~keep]=0;arr[arr[:,:,3]==0,:3]=0
 cleaned=Image.fromarray(arr,'RGBA');bbox=cleaned.getbbox();crop=cleaned.crop(bbox)
 cw,ch=row['footprintCells'];gw=cw*256;gh=ch*256
 targetw=gw # one uniform scale, exact full logical width; no nonuniform stretch
 scale=targetw/crop.width;rh=round(crop.height*scale)
 res=crop.resize((targetw,rh),Image.Resampling.LANCZOS)
 # Metadata ground slot has exact grid dimensions. Taller prop overhang is padding.
 top=32+max(0,rh-gh)
 w=gw+64;h=top+gh+32
 canvas=Image.new('RGBA',(w,h),(0,0,0,0))
 xy=(round(32+(gw-targetw)/2),top+gh-rh);canvas.alpha_composite(res,xy)
 kind='point' if [cw,ch]==[1,1] else 'group'
 rel=f"decor/{'points' if kind=='point' else 'groups'}/{key}.png"
 out=ROOT/rel;out.parent.mkdir(parents=True,exist_ok=True);canvas.save(out)
 pivot=[.5,(h-(top+gh/2))/h]
 r={'file':rel,'family':key,'kind':kind,'width':w,'height':h,'ppu':256,'pivot':pivot,'logicalRect':[32,top,gw,gh],'footprintCells':[cw,ch],'sha256':sha(out),'sourceFiles':[str(source.relative_to(ROOT))],'sourceSheetRegion':list(region),'sourceCropBox':[region[0]+bbox[0],region[1]+bbox[1],region[0]+bbox[2],region[1]+bbox[3]],'uniformScale':scale,'sourceAlphaCleanup':'Keep generated component alpha and 2px native AA fringe; remove detached tiny specks and zero RGB only where alpha0. No RGB repaint, no keying, no nonuniform scaling.','visualOverhangAllowed':rh>gh or targetw>gw,'registrationSideOverhangPixels':[max(0,(targetw-gw)/2),max(0,(targetw-gw)/2)],'nativeRgbRepainted':False,'deriveColliderFromAlpha':False,'noGameplayColliderOrCost':True}
 assets.append(r);groups.append({'key':key,'file':rel,'footprintCells':[cw,ch]})
 aa=np.array(canvas)[:,:,3];g=aa[top:top+gh,32:32+gw]>=128;ys,xs=np.where(g)
 ex=(int(xs.max()-xs.min()+1)/gw,int(ys.max()-ys.min()+1)/gh)
 density=[float(np.mean(g[y:y+256])) for y in range(0,gh,256)]
 checks={'key':key,'size':[w,h],'logicalRect':r['logicalRect'],'groundOpaqueExtentFraction':list(ex),'rowOpaqueDensity':density,'widthPass':ex[0]>=.9,'depthPass':ch==1 or(ex[1]>=.85 and min(density)>=.35),'rgba':True,'sha256':r['sha256']}
 qa.append(checks)
manifest={'schemaVersion':1,'packageId':CAT['packageId'],'themeDisplayName':CAT['themeDisplayName'],'producer':'ranch_groups_b_points','assets':assets,'groups':groups,'nativeSources':native}
(ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
(ROOT/'manifest_sha256.txt').write_text(sha(ROOT/'manifest.json')+'\n')
(ROOT/'native_sources_manifest.json').write_text(json.dumps({'schemaVersion':1,'generator':'built-in image_gen','sources':native,'promptFiles':['Authoring/prompts.json','Authoring/edits.json']},ensure_ascii=False,indent=2)+'\n')
report={'status':'PASS_OFFLINE_ART_BYTES_AND_FOOTPRINTS' if all(x['widthPass']and x['depthPass'] for x in qa) else 'FAIL','assetCount':len(assets),'groupCount':6,'pointCount':8,'actualUnityImport':'NOT_RUN','actualUnityCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN','groundOpaqueAlphaThreshold':128,'checks':qa}
(ROOT/'groups_b_qa.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
# Technical contact sheet only. Sprite native RGB preserved.
sheet=Image.new('RGB',(1600,math.ceil(len(assets)/4)*440),(11,18,33));dr=ImageDraw.Draw(sheet)
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
for i,r in enumerate(assets):
 col=i%4;row=i//4;x=col*400;y=row*440
 dr.text((x+12,y+12),r['family'],font=font,fill='#f6d989')
 dr.text((x+12,y+35),f"{r['footprintCells'][0]}x{r['footprintCells'][1]} / PPU256 / OFFLINE",font=font,fill='#85bdca')
 sprite=Image.open(ROOT/r['file']);sprite.thumbnail((380,365),Image.Resampling.LANCZOS)
 sheet.paste(sprite,(x+(400-sprite.width)//2,y+65+(365-sprite.height)//2),sprite)
sheet.save(ROOT/'Inspection/GROUPS_B_14_CATALOG.png')
print(json.dumps({'status':report['status'],'assets':len(assets),'failed':[x for x in qa if not x['widthPass']or not x['depthPass']]},indent=2))

