"""Independent CPU sprite-first resampling: actual GPU/Unity filtering remains NOT_RUN."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import json,hashlib,argparse

O=Path(__file__).resolve().parent;W=O.parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
F=lambda n:ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
def resample(im,size,filtering):
 if filtering=='TWO_TAP_BILINEAR_MODEL':
  return im.transform(size,Image.Transform.AFFINE,(im.width/size[0],0,0,0,im.height/size[1],0),resample=Image.Resampling.BILINEAR)
 return im.resize(size,filtering)
def mask(cells,c,r,diagonal):
 offsets=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]if diagonal else[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]
 return sum(b for dx,dy,b in offsets if(c+dx,r+dy)in cells)
def render(root,family,rows,cellPx,filtering):
 cells={(x,y)for y,row in enumerate(rows)for x,ch in enumerate(row)if ch=='X'};diagonal=family=='raised_flower_meadow';digits=3 if diagonal else 2
 width=max(map(len,rows));height=len(rows);native=Image.new('RGBA',(width*256+64,height*256+64));scaled=Image.new('RGBA',(round((width*256+64)*cellPx/256),round((height*256+64)*cellPx/256)));used={}
 for c,r in sorted(cells,key=lambda p:(p[1],p[0])):
  file=f'decor/connected/{family}/n{mask(cells,c,r,diagonal):0{digits}d}.png';p=root/file;im=Image.open(p).convert('RGBA');used[file]=sha(p)
  native.alpha_composite(im,(c*256,r*256));spr=resample(im,(round(im.width*cellPx/256),round(im.height*cellPx/256)),filtering);scaled.alpha_composite(spr,(c*cellPx,r*cellPx))
 whole=resample(native,scaled.size,filtering)
 low=whole.filter(ImageFilter.MinFilter(5));high=whole.filter(ImageFilter.MaxFilter(5))
 core=0;maxLoss=0;coreLossPixels=0;sumLoss=0;coreRgbMax=0;flatCorePixels=0;flatCoreRgbaMax=0;flatCoreBadPixels=0
 for a,b,mn,mx in zip(whole.getdata(),scaled.getdata(),low.getdata(),high.getdata()):
  if a[3]==255:
   core+=1;loss=max(0,a[3]-b[3]);maxLoss=max(maxLoss,loss);sumLoss+=loss
   if loss>1:coreLossPixels+=1
   if b[3]==255:coreRgbMax=max(coreRgbMax,max(abs(a[k]-b[k])for k in range(3)))
  if mn==mx and mn[3]==255:
   flatCorePixels+=1;diff=max(abs(a[k]-b[k])for k in range(4));flatCoreRgbaMax=max(flatCoreRgbaMax,diff)
   if diff>1:flatCoreBadPixels+=1
 return {'referenceOpaquePixels':core,'maxOpaqueReferenceAlphaLossIncludingAAFilterOvershoot':maxLoss,'opaqueReferenceLossOver1Pixels':coreLossPixels,'sumOpaqueReferenceAlphaLoss':sumLoss,'maxRGBOnMutuallyOpaquePixels':coreRgbMax,'flatOpaque5x5Pixels':flatCorePixels,'flatOpaque5x5MaxRGBA':flatCoreRgbaMax,'flatOpaque5x5BadPixels':flatCoreBadPixels,'sourcePNGSha256':used},whole,scaled

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--meadow-only',action='store_true');args=ap.parse_args()
 specs=[('meadow_filled','meadow','raised_flower_meadow',['XXXXXX','XXXXXX','XXXXXX']),('meadow_hole','meadow','raised_flower_meadow',['XXXXXX','XX..XX','XX..XX','XXXXXX']),('meadow_neck','meadow','raised_flower_meadow',['XXX.XXX','XXXXXXX','XXX.XXX'])]
 if not args.meadow_only:
  specs[:0]=[(n,'rails',f,rows)for n,f,rows in [('fenceH','cream_fence',['XXXXXX']),('fenceV','cream_fence',['X','X','X','X','X','X']),('wallH','meadow_stone_wall',['XXXXXX']),('wallV','meadow_stone_wall',['X','X','X','X','X','X'])]]
 checks=[];panels=[];pins={}
 for name,group,family,rows in specs:
  root=W/(group+'_exports');p=root/(group+'_manifest.json');pins[group]=sha(p)
  for filtName,filt in [('TWO_TAP_BILINEAR_MODEL','TWO_TAP_BILINEAR_MODEL'),('BILINEAR',Image.Resampling.BILINEAR),('LANCZOS',Image.Resampling.LANCZOS)]:
   for px in [64,80,100,128,256]:
    result,whole,scaled=render(root,family,rows,px,filt)
    passed=result['flatOpaque5x5BadPixels']==0 and(filtName!='TWO_TAP_BILINEAR_MODEL'or result['opaqueReferenceLossOver1Pixels']==0)
    result.update(fixture=name,filter=filtName,cellPixels=px,status='PASS_OFFLINE'if passed else'FAIL_OFFLINE');checks.append(result)
    if px==80 and filtName=='LANCZOS':
     columnTwo=max(whole.width+48,550);page=Image.new('RGBA',(max(1100,columnTwo+scaled.width+20),max(110,whole.height)+80),(11,26,40,255));d=ImageDraw.Draw(page);d.text((16,14),name+' / 80px per cell / LANCZOS',font=F(20),fill=(255,230,173));d.text((16,43),'WHOLE FIELD FILTERED',font=F(16),fill=(105,218,201));d.text((columnTwo,43),'EACH SPRITE FILTERED FIRST',font=F(16),fill=(105,218,201));page.alpha_composite(whole,(16,72));page.alpha_composite(scaled,(columnTwo,72));panels.append(page.convert('RGB'))
 for group,pin in pins.items():assert sha(W/(group+'_exports')/(group+'_manifest.json'))==pin,'Producer changed during independent audit'
 report={'status':'PASS_OFFLINE'if all(c['status']=='PASS_OFFLINE'for c in checks)else'FAIL_OFFLINE','manifestPins':pins,'checks':checks,'note':'Reference is native tiled field composited before filtering; actual is each320PNG independently filtered before world-spaced composition. TWO_TAP_BILINEAR_MODEL is affine2x2 sampling without resize antialias prefilter; it is a CPU model, not actual GPU capture. Flat5x5 opaque reference plateaus must retainRGBA within1 and their pixel count is explicit. Raw alpha/color changes near AA/filterovershoot are retained separately and not silently described as zero. Actual Unity/importedtexture alpha handling remains NOT_RUN.','actualUnityImport':'NOT_RUN','csharpCompile':'NOT_RUN','actualGPUFilter':'NOT_RUN','actualPlayMode':'NOT_RUN'}
 suffix='_meadow'if args.meadow_only else'';out=O/('independent_scaled_connections'+suffix+'.json');out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 width=max(p.width for p in panels);height=sum(p.height+14 for p in panels);sheet=Image.new('RGB',(width,height),(9,21,34));y=0
 for page in panels:sheet.paste(page,(0,y));y+=page.height+14
 sheet.save(O/('independent_scaled_connections'+suffix+'.png'))
 print(json.dumps({'status':report['status'],'fixtures':len(specs),'checks':len(checks),'fails':[{'fixture':c['fixture'],'filter':c['filter'],'cellPixels':c['cellPixels'],'maxOpaqueAlphaLoss':c['maxOpaqueReferenceAlphaLossIncludingAAFilterOvershoot'],'lossPixels':c['opaqueReferenceLossOver1Pixels'],'flatRgbaMax':c['flatOpaque5x5MaxRGBA'],'flatBadPixels':c['flatOpaque5x5BadPixels']}for c in checks if c['status']!='PASS_OFFLINE'],'report':str(out)}))

if __name__=='__main__':main()
