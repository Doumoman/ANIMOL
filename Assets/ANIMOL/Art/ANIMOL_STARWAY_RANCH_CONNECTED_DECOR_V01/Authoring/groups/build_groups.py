"""Register NEW native whole-group sprites. No RGB painting or shape synthesis."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
import numpy as np
from scipy.ndimage import label,binary_dilation

ROOT=Path(__file__).resolve().parents[1]
ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01'
SPEC=[('hay_store_2x1',2,1,0),('feeding_corner_3x1',3,1,0),
      ('tree_flower_corner_2x2',2,2,128),('barnyard_3x2',3,2,0)]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rgba_sha(im):return hashlib.sha256(np.asarray(im.convert('RGBA')).tobytes()).hexdigest()
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
def font(n):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
assets=[];proofs=[];checks=[]
for name,w,h,over in SPEC:
 source='sources/'+name+'_native.png';sp=ROOT/source;original=Image.open(sp).convert('RGBA');original.load()
 a=np.asarray(original).copy();alpha=a[:,:,3].copy()
 # Remove only isolated antialias specks and invisible-edge remnants; RGB is unchanged.
 lab,n=label(alpha>8,structure=np.ones((3,3),dtype='uint8'))
 sizes=np.bincount(lab.ravel());keep=np.flatnonzero(sizes>=16);keep=keep[keep!=0]
 core=np.isin(lab,keep);allowed=binary_dilation(core,iterations=2)
 alpha[~allowed]=0;a[:,:,3]=alpha
 assert np.array_equal(a[:,:,:3],np.asarray(original)[:,:,:3])
 cleaned=Image.fromarray(a,'RGBA');bb=cleaned.getbbox();assert bb
 crop=cleaned.crop(bb);groundw,groundh=w*256,h*256
 cw,ch=groundw+64,groundh+64+over;lr=[32,32+over,groundw,groundh]
 scale=min((groundw*.97)/crop.width,(groundh+over-16)/crop.height)
 size=(round(crop.width*scale),round(crop.height*scale))
 art=crop.resize(size,Image.Resampling.LANCZOS)
 canvas=Image.new('RGBA',(cw,ch),(0,0,0,0));left=32+(groundw-art.width)//2;bottom=lr[1]+groundh-8;top=bottom-art.height
 assert left>=0 and top>=0 and left+art.width<=cw and bottom<=ch
 canvas.alpha_composite(art,(left,top));file='decor/groups/'+name+'.png';op=ROOT/file;op.parent.mkdir(parents=True,exist_ok=True);canvas.save(op)
 pivot=[(lr[0]+groundw*.5)/cw,1-(lr[1]+groundh*.5)/ch]
 vis=Image.fromarray((np.asarray(canvas)[:,:,3]>32).astype('uint8')*255).getbbox()
 row={'id':name,'file':file,'key':name,'kind':'multi_cell_group','family':name,
  'width':cw,'height':ch,'ppu':256,'pixelsPerUnit':256,'pivot':pivot,
  'logicalRect':lr,'logical_rect':lr,'footprintCells':[w,h],
  'actualGameplayFootprintCells':None,'deriveColliderFromAlpha':False,'registerGameplayCellFromThisRecord':False,
  'nativeWholeGroup':True,'oneRendererPerGroup':True,'noCellSizedCopy':True,'topOverhangPixels':over,
  'anchorCellConvention':'Host decorative anchor is the bottom-left occupied visual cell (+Y North).',
  'occupiedDecorCellOffsets':[[x,y]for y in range(h)for x in range(w)],
  'sortingContactPixelTopLeft':[lr[0]+groundw*.5,lr[1]+groundh],
  'visualAlphaBounds':list(vis),'horizontalGroundFill':round((vis[2]-vis[0])/groundw,6),
  'sourceFiles':[source],'source':source,'sourceSha256':sha(sp),
  'sha256':sha(op),'newNativeBitmap':True,'generationTool':'built-in image_gen',
  'registration':{'nativeCropLTRB':list(bb),'nativeCropSize':list(crop.size),'uniformScale':scale,'resizedSize':list(size),
     'pasteTopLeft':[left,top],'contactBottomPixel':bottom,'alphaOnlyCleanup':True,'rgbRepaint':False,'sourceRGBBeforeResizeUnchanged':True}}
 assets.append(row)
 assert row['horizontalGroundFill']>=.90
 assert Image.open(op).size==(cw,ch)and np.asarray(canvas)[:,:,3].min()==0
 assert len(row['occupiedDecorCellOffsets'])==w*h
 assert abs(pivot[0]*cw-(lr[0]+groundw/2))<1e-6
 assert abs((1-pivot[1])*ch-(lr[1]+groundh/2))<1e-6
 checks.append({'id':name,'widthFill':row['horizontalGroundFill'],'exactPPUPivotGroundRect':True,
  'allDecorativeFootprintCells':w*h,'actualGameplayFootprintUnverified':True,'newNativeSource':source,
  'sourceSha256':row['sourceSha256'],'runtimeSha256':row['sha256'],'alphaOnlyBeforeUniformResize':True,
  'sourceCropRGBUnchanged':True,'nativeAlpha0Background':True,'sourceRGBASha256':rgba_sha(original),
  'runtimeRGBASha256':rgba_sha(canvas),'wholeSpriteRenderCountExpected':1})
 # Review-only cell overlay; no grid pixels are written into production sprites.
 preview=Image.new('RGBA',(cw,ch),(16,29,45,255));d=ImageDraw.Draw(preview)
 for yy in range(h):
  for xx in range(w):
   x0,y0=lr[0]+xx*256,lr[1]+yy*256
   d.rectangle((x0,y0,x0+256,y0+256),fill=(31,55,63,255)if(xx+yy)%2==0 else(35,61,67,255))
 preview.alpha_composite(canvas);d=ImageDraw.Draw(preview)
 for xx in range(w+1):d.line((lr[0]+xx*256,lr[1],lr[0]+xx*256,lr[1]+groundh),fill=(77,213,217,200),width=2)
 for yy in range(h+1):d.line((lr[0],lr[1]+yy*256,lr[0]+groundw,lr[1]+yy*256),fill=(77,213,217,200),width=2)
 preview.save(ROOT/'inspection'/('ground_'+name+'.png'));proofs.append(preview)

sources=[{'file':r['sourceFiles'][0],'sha256':r['sourceSha256'],'generatedBy':'built-in image_gen','runtimeSelectable':False}for r in assets]
manifest={'schemaVersion':1,'packageId':ID,'group':'native_multi_cell_groups','themeDisplayName':'별길 목장',
 'logicalCellWorldUnits':1,'pixelsPerUnit':256,'coordinateConvention':'Pixel rectangles top-left; Unity pivots bottom-left normalized; world +Y North.',
 'placementPolicy':'Reserve the full decorative footprint atomically and create ONE renderer at footprint center. Never fit a whole PNG to1cell or derive a Collider from alpha.',
 'sourcePolicy':'Four whole NEW native imagegen group compositions. Only alpha speck cleanup/crop/uniformresample/register; no RGB painting, recoloring, procedural artwork or old bitmap reuse.',
 'assets':assets,'clips':[],'sources':sources,'generationPrompts':['generation_prompts.json','edit_prompts.json'],
 'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN','actualHostDecorBindings':'NOT_RUN'}
write(ROOT/'groups_manifest.json',manifest)

contact=Image.new('RGB',(1800,1600),(10,22,37));d=ImageDraw.Draw(contact)
d.text((28,20),'STARWAY RANCH / NATIVE MULTI-CELL DECOR GROUPS',font=font(34),fill=(255,232,174))
d.text((28,70),'ONE complete prop / explicit full footprint / PPU256 / UNITY NOT_RUN',font=font(22),fill=(106,219,216))
for i,(a,proof)in enumerate(zip(assets,proofs)):
 x=24+(i%2)*890;y=125+(i//2)*710;d.rectangle((x,y,x+860,y+688),outline=(56,90,108),width=2)
 d.text((x+18,y+16),a['id'],font=font(26),fill=(255,232,174))
 w,h=a['footprintCells'];d.text((x+18,y+56),f'{w} x {h} decorative cells / ONE renderer / no fit-to1cell',font=font(19),fill=(106,219,216))
 ratio=min(808/proof.width,555/proof.height);small=proof.resize((round(proof.width*ratio),round(proof.height*ratio)),Image.Resampling.LANCZOS)
 contact.paste(small.convert('RGB'),(x+26+(808-small.width)//2,y+102+(555-small.height)//2))
 d.text((x+18,y+655),'Cyan grid is a review overlay; production PNG has no grid.',font=font(17),fill=(174,199,206))
contact.save(ROOT/'inspection/groups_contact_sheet.png')
qa={'status':'PASS_OFFLINE','assets':len(assets),'finalNativeSources':len(sources),'checks':checks,
 'allLongAxisFillAtLeast90Percent':True,'logicalFootprints':{r['id']:r['footprintCells']for r in assets},
 'newNativeWholeGroupsNotTiledCopies':True,'currentBaseArtRGBReused':False,'productionRGBPainted':False,
 'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN',
 'actualHostDecorBindings':'NOT_RUN','manifestSha256':sha(ROOT/'groups_manifest.json')}
write(ROOT/'groups_qa.json',qa)
print(json.dumps({'status':'PASS_OFFLINE','assets':len(assets),'manifestSha256':qa['manifestSha256'],'fills':{a['id']:a['horizontalGroundFill']for a in assets}}))
