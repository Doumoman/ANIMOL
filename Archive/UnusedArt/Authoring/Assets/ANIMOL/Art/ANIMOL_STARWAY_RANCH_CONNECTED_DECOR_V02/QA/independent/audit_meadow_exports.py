"""Independent direct-export raw256 meadow audit; imports only sibling independent QA helpers."""
from pathlib import Path
from PIL import Image
import argparse,hashlib,json
from audit_package import check_meadow
O=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def audit(root):
 m=json.loads((root/'manifest.json').read_text());checks=[];errors=[];ims={};assets={};sources={}
 def ck(n,v):
  checks.append({'check':n,'passed':bool(v)})
  if not v:errors.append(n)
 ck('Exact Ranch V02 package identity',m.get('packageId')=='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02')
 for a in [row for row in m['assets']if row['family']=='raised_flower_meadow']:
  f=a['file'];assets[f]=a;p=root/f;ck('Runtime file SHA '+f,p.is_file()and sha(p)==a['sha256'])
  im=Image.open(p);im.load();ck('RGBA mode '+f,im.mode=='RGBA');im=im.convert('RGBA');ims[f]=im
  ck('Exact320square ground register '+f,im.size==(320,320)and a['logicalRect']==[32,32,256,256]and a['footprintCells']==[1,1]and a['ppu']==256 and a['pivot']==[.5,.5])
  ck('Nonempty native transparency or fullysurrounded255 '+f,im.getchannel('A').getextrema()==(0,255)or(f=='decor/connected/raised_flower_meadow/n255.png'and im.getchannel('A').getextrema()==(255,255)))
  ck('Visual-only noalphacollider '+f,a.get('deriveColliderFromAlpha')is False)
  ck('Native source refs '+f,bool(a.get('sourceFiles')))
  for s in a.get('sourceFiles',[]):ck('Native source exists '+s,(root/s).is_file());sources[s]=sha(root/s)
 for s in m.get('sources',m.get('nativeSources',[])):ck('Native source SHA '+s['file'],sha(root/s['file'])==s['sha256'])
 ck('Exactly260 meadowruntime assets',len(assets)==260)
 expected={f'decor/connected/raised_flower_meadow/n{i:03d}.png'for i in range(256)}|{f'decor/accents/meadow/flowers_{i:02d}.png'for i in range(1,5)}
 ck('Exactpreserved260 meadowpaths',set(assets)==expected)
 joins=check_meadow(ims,assets,ck)
 prior=json.loads((O/'baseline_runtime_pixel_sha256.json').read_text());old={r['file']:r for r in prior['assets']if r['family']=='raised_flower_meadow'}
 ck('Oldmeadow260 hashrecords',len(old)==260)
 for f,old in old.items():
  if f in ims:ck('DecodedRGBAnew '+f,hashlib.sha256(ims[f].tobytes()).hexdigest()!=old['decodedRgbaSha256'])
 return {'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','exportRoot':str(root),'manifestSha256':sha(root/'manifest.json'),'runtimePngCount':len(assets),'checkCount':len(checks),'checks':checks,'errors':errors,'joins':joins,'sourcePins':sources,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('exports');ap.add_argument('--report');a=ap.parse_args();r=audit(Path(a.exports).resolve())
 if a.report:Path(a.report).write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({k:r[k]for k in['status','checkCount','runtimePngCount','errors']}))
if __name__=='__main__':main()
