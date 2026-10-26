"""Independent direct producer-export PNG/register/port audit; no producer import."""
from pathlib import Path
from PIL import Image
import argparse,hashlib,json

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def audit(root):
 m=json.loads((root/'manifest.json').read_text());errors=[];checks=0;ims={};families=set();pins={}
 def ck(label,ok):
  nonlocal checks
  checks+=1
  if not ok:errors.append(label)
 for a in m['assets']:
  f=a['file'];p=root/f;ck('SHA '+f,sha(p)==a['sha256']);im=Image.open(p);im.load();ck('RGBA '+f,im.mode=='RGBA');im=im.convert('RGBA');ck('Canvas '+f,im.size==(320,384)and[a['width'],a['height']]==[320,384]);ck('Register '+f,a['logicalRect']==[32,96,256,256]and a['ppu']==256 and a['footprintCells']==[1,1]and max(abs(a['pivot'][i]-[.5,160/384][i])for i in range(2))<1e-6);ck('Alpha '+f,im.getchannel('A').getextrema()==(0,255));ck('NoRGBrepaint '+f,a.get('nativeRgbRepainted')is False)
  for s in a['sourceFiles']:ck('Native source exists '+s,(root/s).is_file());pins[s]=sha(root/s)
  if a['kind']=='connected_tile':ims[(a['family'],a['neighborMask'])]=im;families.add(a['family'])
 for s in m['sources']:ck('Native source pinned '+s['file'],sha(root/s['file'])==s['sha256'])
 joins=[]
 for fam in sorted(families):
  ck('All16 '+fam,{n for family,n in ims if family==fam}==set(range(16)))
  sprites=[ims[(fam,n)]for n in range(16)];ck('Unique16 '+fam,len({hashlib.sha256(im.tobytes()).hexdigest()for im in sprites})==16)
  strips=[{'N':im.crop((0,96,320,97)),'S':im.crop((0,351,320,352)),'E':im.crop((287,0,288,384)),'W':im.crop((32,0,33,384))}for im in sprites]
  missing=[];different=[];unsafe=shared=pairs=0
  for n in range(16):
   for side,bit in[('N',1),('E',2),('S',4),('W',8)]:
    if n&bit and strips[n][side].getchannel('A').getbbox()is None:missing.append([n,side])
  for a in range(16):
   for b in range(16):
    for side,abit,other,bbit in[('E',2,'W',8),('N',1,'S',4)]:
     if not(a&abit and b&bbit):continue
     pairs+=1
     if strips[a][side].tobytes()!=strips[b][other].tobytes():different.append([a,side,b,other])
     aa=sprites[a].crop((256,0,320,384)if side=='E'else(0,0,320,128));bb=sprites[b].crop((0,0,64,384)if side=='E'else(0,256,320,384))
     for p,q in zip(aa.getdata(),bb.getdata()):
      if p[3]and q[3]:
       shared+=1
       if not(p[3]==q[3]==255 and p[:3]==q[:3]):unsafe+=1
  ck('Openports '+fam,not missing);ck('Opposedprofiles '+fam,pairs==128 and not different);ck('Overlap '+fam,pairs==128 and unsafe==0)
  joins.append({'family':fam,'compatiblePairs':pairs,'openPortFailures':missing,'differentProfiles':different,'sharedPixels':shared,'unsafeOverlapPixels':unsafe})
 return{'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','exportRoot':str(root),'manifestSha256':sha(root/'manifest.json'),'runtimePngCount':len(m['assets']),'semanticNetworkFamilies':sorted(families),'sourcePins':pins,'checks':checks,'errors':errors,'joins':joins,'actualUnityImport':'NOT_RUN','actualGPUFilter':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('exports',nargs='+');ap.add_argument('--report');args=ap.parse_args();reports=[audit(Path(s).resolve())for s in args.exports];out={'status':'PASS_OFFLINE'if all(r['status']=='PASS_OFFLINE'for r in reports)else'FAIL_OFFLINE','scope':'Direct-export independent byte/alpha/register/native SHA and full128paired-native-port/overlap audit per family; not Unity.','reports':reports}
 if args.report:Path(args.report).write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'status':out['status'],'exports':[{'root':r['exportRoot'],'checks':r['checks'],'runtimePngCount':r['runtimePngCount'],'errors':r['errors']}for r in reports]}))
if __name__=='__main__':main()
