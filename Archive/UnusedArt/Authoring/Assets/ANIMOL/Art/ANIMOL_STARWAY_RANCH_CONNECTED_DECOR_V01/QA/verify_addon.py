"""Portable offline asset/catalog verifier. Does not run or compile Unity."""
from pathlib import Path
from PIL import Image
import json,hashlib,argparse,re
ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def verify(root,runtime_only=False):
 errors=[];stats={};root=Path(root)
 def check(value,message):
  if not value:errors.append(message)
 j=json.loads((root/'manifest.json').read_text());assets=j['assets'];a={r['file']:r for r in assets}
 check(j['schemaVersion']==1 and j['packageId']==ID,'wrong manifest schema/package')
 check((root/'manifest_sha256.txt').read_text().strip()==sha(root/'manifest.json'),'manifest SHA pin differs')
 check(len(a)==len(assets)==len({p.casefold()for p in a}),'duplicate/case-colliding asset paths')
 rgba=[]
 for f,row in a.items():
  path=Path(f);check(not path.is_absolute()and'..'not in path.parts and f.startswith('decor/')and f.endswith('.png'),'invalid exact runtime path '+f)
  p=root/f;check(p.is_file(),'missing PNG '+f)
  if not p.is_file():continue
  check(sha(p)==row['sha256'],'PNG SHA differs '+f)
  try:
   with Image.open(p)as im:
    im.load();check(im.mode=='RGBA'and im.size==(row['width'],row['height']),'PNG mode/dimensions differ '+f);rgba.append(hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest())
  except Exception as e:errors.append('PNG decode failure '+f+': '+type(e).__name__)
  lr=row['logicalRect'];cells=row['footprintCells'];pivot=row['pivot'];w,h=row['width'],row['height']
  check(row['ppu']==256 and lr[2:]==[256*cells[0],256*cells[1]],'PPU/logical size differs '+f)
  check(lr[0]>=0 and lr[1]>=0 and lr[0]+lr[2]<=w and lr[1]+lr[3]<=h,'logical ground rectangle outside canvas '+f)
  expected=[(lr[0]+lr[2]/2)/w,1-(lr[1]+lr[3]/2)/h]
  check(max(abs(x-y)for x,y in zip(pivot,expected))<1e-6,'logical ground pivot differs '+f)
  check(row.get('actualGameplayFootprintCells')is None and row.get('deriveColliderFromAlpha')is False and row.get('registerGameplayCellFromThisRecord')is False,'invented gameplay footprint/collision '+f)
 for family,count in [('cream_fence',16),('meadow_stone_wall',16),('raised_flower_meadow',256)]:
  for n in range(count):
   f=f'decor/connected/{family}/n{n:0{3 if count==256 else 2}d}.png'
   check(f in a,'missing connected variant '+f)
   if f in a:check(a[f]['family']==family and a[f]['footprintCells']==[1,1],'wrong family/footprint '+f)
 for family in ['cream_fence','meadow_stone_wall']:check('decor/connected/'+family+'/periodic_post.png'in a,'missing periodic post '+family)
 required={'hay_store_2x1':[2,1],'feeding_corner_3x1':[3,1],'tree_flower_corner_2x2':[2,2],'barnyard_3x2':[3,2]}
 check({g['key']:g['footprintCells']for g in j['groups']}==required,'multi-cell group keys/size differ')
 for g in j['groups']:check(g['file']in a and a[g['file']]['footprintCells']==g['footprintCells'],'group art/footprint mismatch '+g['key'])
 for flag in ['actualUnityImport','actualCSharpCompile','actualPlayMode']:check(j.get(flag)=='NOT_RUN','false Unity verification claim '+flag)
 check(j.get('actualGameplayBindingsVerified')is False,'false actual host binding claim')
 source_verified=0
 if not runtime_only:
  for row in j['nativeSources']:
   p=root/row['file'];check(p.is_file(),'missing optional native source '+row['file'])
   if p.is_file():
    check(sha(p)==row['sha256'],'native source hash differs '+row['file'])
    with Image.open(p)as im:im.load()
    source_verified+=1
 # Runtime-only archives include no native originals, while every declared art PNG is required.
 observed={f.relative_to(root).as_posix()for f in (root/'decor').rglob('*.png')}
 check(observed==set(a),'undeclared/missing runtime decoration PNG set')
 settings=json.loads((root/'sprite_import_settings.json').read_text())['spriteSettings']
 check({r['file']for r in settings}==set(a)and len(settings)==len(a),'import settings exact set differs')
 # Static source checks are NOT C# compile/test results.
 imports=(root/'Editor/RanchConnectedDecorV01SpriteImporter.cs').read_text()
 check('TryGetValue(relative, out row)'in imports and 'ValidateSourcePng(root, row)'in imports,'importer lost exact current source checks')
 check('TextureImporterType.Default'in imports and 'SpriteMeshType.FullRect'in imports,'source exclusion/FullRect missing')
 controller=(root/'Runtime/RanchConnectedDecorV01Controller.cs').read_text()
 check('row.CenterX, row.CenterY'in controller and 'plan.GroupCount'in controller,'group single-anchor renderer absent')
 check('hostAllowDecorCell'in controller and 'cellSize / parentScale.x'in controller,'host policy/uniform world scale missing')
 stats.update(runtimeSprites=len(a),decodedRuntimePng=len(rgba),nativeSourcesDeclared=len(j['nativeSources']),nativeSourcesVerified=source_verified,multiCellGroups=len(j['groups']),networkFamilies=3)
 for name in ['runtime_checksums_sha256.txt']:
  p=root/name
  if p.exists():
   count=0
   for line in p.read_text().splitlines():
    h,f=line.split('  ',1);check((root/f).is_file()and sha(root/f)==h,'runtime checksum differs '+f);count+=1
   stats['runtimeChecksumsVerified']=count
 return {'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','errors':errors,'stats':stats,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN','actualHostBinding':'NOT_RUN','scope':'Offline file, JSON, dimensions, metadata and static-source checks only.'}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--package',required=True);p.add_argument('--runtime-only',action='store_true');p.add_argument('--report');args=p.parse_args()
 try:r=verify(args.package,args.runtime_only)
 except Exception as e:r={'status':'FAIL_OFFLINE','errors':[type(e).__name__+': '+str(e)],'actualUnityExecuted':False}
 if args.report:Path(args.report).write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(r,ensure_ascii=False));raise SystemExit(0 if r['status']=='PASS_OFFLINE'else 1)
