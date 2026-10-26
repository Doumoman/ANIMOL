"""Independent CPU sprite-first filtering audit. This is not Unity/GPU capture."""
from pathlib import Path
from PIL import Image,ImageFilter
import argparse,hashlib,json
O=Path(__file__).resolve().parent

def mask(cells,x,y):return sum(bit for dx,dy,bit in[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]if(x+dx,y+dy)in cells)
def sample(im,size,mode):
 if mode=='BILINEAR_2_TAP_CPU':return im.transform(size,Image.Transform.AFFINE,(im.width/size[0],0,0,0,im.height/size[1],0),Image.Resampling.BILINEAR)
 return im.resize(size,Image.Resampling.LANCZOS)
def check(p,family,rows,px,mode):
 cells={(x,y)for y,row in enumerate(rows)for x,v in enumerate(row)if v=='X'}
 whole=Image.new('RGBA',(max(map(len,rows))*256+64,len(rows)*256+128));individual=Image.new('RGBA',(round(whole.width*px/256),round(whole.height*px/256)));pins={}
 for x,y in sorted(cells,key=lambda xy:(xy[1],xy[0])):
  rel=f'decor/connected/{family}/n{mask(cells,x,y):02d}.png';src=p/rel;im=Image.open(src).convert('RGBA');pins[rel]=hashlib.sha256(src.read_bytes()).hexdigest();whole.alpha_composite(im,(256*x,256*y));individual.alpha_composite(sample(im,(round(im.width*px/256),round(im.height*px/256)),mode),(px*x,px*y))
 once=sample(whole,individual.size,mode);lo=once.filter(ImageFilter.MinFilter(5));hi=once.filter(ImageFilter.MaxFilter(5));flat=badflat=maxflat=opaque=badalpha=maxalpha=0
 for a,b,mn,mx in zip(once.getdata(),individual.getdata(),lo.getdata(),hi.getdata()):
  if a[3]==255:
   opaque+=1;loss=max(0,a[3]-b[3]);maxalpha=max(maxalpha,loss)
   if loss>1:badalpha+=1
  if mn==mx and mn[3]==255:
   flat+=1;diff=max(abs(a[i]-b[i])for i in range(4));maxflat=max(maxflat,diff)
   if diff>1:badflat+=1
 return {'status':'PASS_CPU'if badflat==0 and(mode!='BILINEAR_2_TAP_CPU'or badalpha==0)else'FAIL_CPU','flatOpaque5x5Pixels':flat,'flatBadOver1Pixels':badflat,'flatMaxRGBA':maxflat,'referenceOpaquePixels':opaque,'opaqueAlphaLossOver1Pixels':badalpha,'maxAlphaLossIncludingAntialiasOvershoot':maxalpha,'sourceSha256':pins}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--package',required=True);ap.add_argument('--report',help='Optional JSON output path; omitted writes no files.');args=ap.parse_args();p=Path(args.package).resolve();families=[x['key']for x in json.loads((p/'decoration_catalog.json').read_text())['networks']];fixtures={'H9':['X'*9],'V9':['X']*9,'RING':['XXX','X.X','XXX']};checks=[]
 for family in families:
  for name,rows in fixtures.items():
   for px in[64,100,256]:
    for mode in['BILINEAR_2_TAP_CPU','LANCZOS']:
     result=check(p,family,rows,px,mode);result.update(family=family,fixture=name,cellPixels=px,filter=mode);checks.append(result)
 failed=[c for c in checks if c['status']!='PASS_CPU'];report={'status':'PASS_CPU'if not failed else'FAIL_CPU','checkCount':len(checks),'checks':checks,'scope':'Native assembled field filtered once versus individual Sprite PNGs filtered before cell spacing composition. Compare only flat eroded5x5 opaque plateaus (RGBA tolerance1) and BILINEAR2tap opaque alpha loss<=1. LANCZOS antialias/overshoot differences remain reported. Native CPU model only; does not establish GPU textureatlas seam-free output.','actualUnityImport':'NOT_RUN','actualGPUFilter':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
 if args.report:Path(args.report).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'status':report['status'],'checkCount':len(checks),'failures':[{k:c[k]for k in['family','fixture','cellPixels','filter','flatBadOver1Pixels','opaqueAlphaLossOver1Pixels']}for c in failed]}))
if __name__=='__main__':main()
