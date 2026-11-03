"""Independent direct whole-group/point PNG footprint and provenance audit."""
from pathlib import Path
from PIL import Image
import argparse,hashlib,json
EXPECTED={'hay_store_2x1': [2, 1], 'feeding_corner_3x1': [3, 1], 'tree_flower_corner_2x2': [2, 2], 'barnyard_3x2': [3, 2], 'orchard_bench_3x1': [3, 1], 'seedling_rack_3x1': [3, 1], 'harvest_cart_2x2': [2, 2], 'chicken_coop_2x2': [2, 2], 'apiary_corner_2x2': [2, 2], 'picnic_arbor_3x2': [3, 2], 'produce_stall_3x2': [3, 2], 'garden_workbench_2x1': [2, 1], 'milk_can_pair': [1, 1], 'tool_basket': [1, 1], 'ranch_notice_sign': [1, 1], 'watering_can': [1, 1], 'feed_sack_stack': [1, 1], 'flower_bucket': [1, 1], 'harvest_crate': [1, 1], 'honey_jar_stand': [1, 1]}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def audit(root):
 m=json.loads((root/'manifest.json').read_text());errors=[];checks=0;rows=[]
 def ck(n,b):
  nonlocal checks
  checks+=1
  if not b:errors.append(n)
 for a in m['assets']:
  f=a['file'];p=root/f;im=Image.open(p);im.load();ck('RGBA '+f,im.mode=='RGBA');im=im.convert('RGBA');ck('SHA '+f,sha(p)==a['sha256']);ck('Dimensions '+f,list(im.size)==[a['width'],a['height']]);ck('Declared footprint '+f,a['footprintCells']==EXPECTED[a['family']]);ck('PPU256 '+f,a['ppu']==256);lr=a['logicalRect'];fp=a['footprintCells'];pv=a['pivot'];ck('Ground rect extent '+f,lr[2:]==[256*fp[0],256*fp[1]]and lr[0]>=0 and lr[1]>=0 and lr[0]+lr[2]<=im.width and lr[1]+lr[3]<=im.height);ck('Groundcenter pivot '+f,max(abs(pv[i]-[(lr[0]+lr[2]/2)/im.width,1-(lr[1]+lr[3]/2)/im.height][i])for i in range(2))<1e-6);al=im.getchannel('A');bb=al.getbbox();ck('True transparent nonempty '+f,al.getextrema()==(0,255)and bb is not None);ck('Visual only no alphacollider '+f,a.get('deriveColliderFromAlpha')is False)
  ground=al.crop((lr[0],lr[1],lr[0]+lr[2],lr[1]+lr[3]));op=ground.point(lambda n:255 if n>=128 else 0);gb=op.getbbox();density=[sum(op.crop((0,y*256,lr[2],(y+1)*256)).getdata())/(255*lr[2]*256)for y in range(fp[1])];width=(bb[2]-bb[0])/lr[2];height=(gb[3]-gb[1])/lr[3]if gb else 0
  if a['kind']=='group':
   ck('Wholegroup visiblewidth>=90% '+f,width>=.9)
   if fp[1]>1:ck('Depthgroundextent>=85% '+f,height>=.85);ck('Eachdepthrowdensity>=35% '+f,min(density)>=.35)
  ck('Native source refs '+f,bool(a.get('sourceFiles')))
  for s in a.get('sourceFiles',[]):ck('Native source exists '+s,(root/s).is_file())
  rows.append({'file':f,'family':a['family'],'canvas':list(im.size),'footprint':fp,'visibleWidthFill':width,'groundOpaqueHeightFill':height,'groundRowOpaqueDensity':density,'alphaBBox':list(bb),'sourceFiles':a.get('sourceFiles')})
 for s in m.get('sources',m.get('nativeSources',[])):ck('Native source SHA '+s['file'],sha(root/s['file'])==s['sha256'])
 ck('Shared groupmappingexact',set(a['family']for a in m['assets'])==set(g['key']for g in m['groups']))
 return{'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','exportRoot':str(root),'manifestSha256':sha(root/'manifest.json'),'runtimePngCount':len(m['assets']),'checks':checks,'errors':errors,'footprintRows':rows,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
def main():
 ap=argparse.ArgumentParser();ap.add_argument('exports',nargs='+');ap.add_argument('--report');a=ap.parse_args();reports=[audit(Path(s).resolve())for s in a.exports];out={'status':'PASS_OFFLINE'if all(r['status']=='PASS_OFFLINE'for r in reports)else'FAIL_OFFLINE','scope':'Independent direct-export native hashes/PNG alpha/groundPPU/pivot/whole-group depth row occupancy audit. No producer script imported and no Unity execution.','reports':reports}
 if a.report:Path(a.report).write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'status':out['status'],'exports':[{'root':r['exportRoot'],'checks':r['checks'],'errors':r['errors']}for r in reports]}))
if __name__=='__main__':main()
