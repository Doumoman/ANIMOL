"""Assemble a decoration-only additive release; never overwrites the base Ranch."""
from pathlib import Path
from PIL import Image
import json,hashlib,shutil,copy
R=Path('/workspace/scratch/b0cb6eab55f7');W=R/'work/ranch_connected_v01';ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01';P=R/'output'/ID
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
def put(s,d):
 assert s.is_file()and s.stat().st_size>0,s
 if d.exists():assert sha(s)==sha(d),('different shared path',d)
 else:d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(s,d)
def refs(j):
 if isinstance(j,dict):
  for v in j.values():yield from refs(v)
 elif isinstance(j,list):
  for v in j:yield from refs(v)
 elif isinstance(j,str)and j.endswith('.png')and j.startswith(('sources/','production/components/','components/')):yield j
P.mkdir(parents=True,exist_ok=True);assets={};sources={};reports=[]
for group,name in [('rails','rails_manifest.json'),('meadow','meadow_manifest.json'),('groups','groups_manifest.json')]:
 src=W/(group+'_exports');j=read(src/name);write(P/name,j)
 for a0 in j['assets']:
  a=copy.deepcopy(a0);f=a['file'];s=src/f
  assert f not in assets,('duplicate runtime path',f)
  assert sha(s)==a['sha256'],(group,f)
  with Image.open(s)as im:
   im.load();assert im.size==(a['width'],a['height'])and im.mode=='RGBA',(f,im.size,im.mode)
  lr=a.get('logicalRect',a.get('logical_rect'));assert lr and len(lr)==4,(f,lr)
  a['logicalRect']=lr;a['logical_rect']=lr
  assert a['ppu']==256 and len(a['pivot'])==2 and a['footprintCells'][0]*256==lr[2]and a['footprintCells'][1]*256==lr[3],f
  expected=[(lr[0]+lr[2]/2)/a['width'],1-(lr[1]+lr[3]/2)/a['height']]
  assert max(abs(x-y)for x,y in zip(expected,a['pivot']))<1e-6,(f,expected,a['pivot'])
  a.update(actualGameplayFootprintCells=None,deriveColliderFromAlpha=False,registerGameplayCellFromThisRecord=False,actualUnityBindingVerified=False)
  assets[f]=a;put(s,P/f)
 for f in set(refs(j)):put(src/f,P/f)
 for s in (src/'sources').rglob('*'):
  if s.is_file()and s.suffix.lower()=='.png':
   f=s.relative_to(src).as_posix();put(s,P/f);sources[f]={'file':f,'sha256':sha(s),'runtimeSelectable':False,'generatedBy':'built-in image_gen'}
  elif s.is_file()and s.suffix.lower()in('.json','.txt'):put(s,P/'Authoring'/group/'source_metadata'/s.relative_to(src/'sources'))
 for s in (src/'inspection').rglob('*'):
  if s.is_file()and s.suffix.lower()in('.png','.gif','.json'):put(s,P/'Inspection'/group/s.relative_to(src/'inspection'))
 for s in (src/'production').rglob('*'):
  if s.is_file()and s.suffix.lower()in('.py','.json','.txt'):put(s,P/'Authoring'/group/s.relative_to(src/'production'))
 for s in src.glob('*.json'):
  if 'qa'in s.stem.lower():put(s,P/'QA'/s.name)
  elif 'prompt'in s.stem.lower():put(s,P/'Authoring'/group/s.name)
 for s in src.glob('README*.txt'):put(s,P/'Documentation'/s.name)
 reports.append({'group':group,'runtimeSprites':len(j['assets']),'producerManifestSha256':sha(src/name),'actualUnityExecuted':False})
assert len({s.casefold()for s in assets})==len(assets)
groups=[{'key':f.rsplit('/',1)[1][:-4],'file':f,'footprintCells':a['footprintCells']}for f,a in assets.items()if f.startswith('decor/groups/')]
assert {g['key']:g['footprintCells']for g in groups}=={'hay_store_2x1':[2,1],'feeding_corner_3x1':[3,1],'tree_flower_corner_2x2':[2,2],'barnyard_3x2':[3,2]}
main={'schemaVersion':1,'packageId':ID,'themeDisplayName':'별길 목장','kind':'DECORATION_ONLY_ADDON','pixelArtRules':False,'pixelsPerUnit':256,'logicalCellWorldUnits':1,
 'baseReference':'ANIMOL_STARWAY_RANCH_CARTOON_V12, Library version9; read-only style/metadata reference, do not reinstall over newer device patches',
 'assets':[assets[k]for k in sorted(assets)],'groups':groups,
 'networkFamilies':[{'family':'cream_fence','maskBits':{'north':1,'east':2,'south':4,'west':8},'variants':16,'periodicPostFile':'decor/connected/cream_fence/periodic_post.png'},
 {'family':'meadow_stone_wall','maskBits':{'north':1,'east':2,'south':4,'west':8},'variants':16,'periodicPostFile':'decor/connected/meadow_stone_wall/periodic_post.png'},
 {'family':'raised_flower_meadow','maskBits':{'north':1,'northeast':2,'east':4,'southeast':8,'south':16,'southwest':32,'west':64,'northwest':128},'variants':256}],
 'connectionPolicy':'Same family + connectionGroup + logicalLayer + integer heightKey only. Query full board, not camera chunk. Different family/group/plane ends are capped.',
 'groupPlacementPolicy':'Bottom-left anchor claims every declared decorative footprint cell atomically; ONE renderer per placementId at entire logical footprint centre. No fit-to-one-cell/alpha-autoscale.',
 'collisionPolicy':'No new gameplay blockers/colliders/turns. Existing host must approve each DecorClaim. Visual decor cell reservations are independent of gameplay occupancy.',
 'baseBridgePolicy':'Existing base bridge boundary rail/post selector remains authoritative; do not double-render new fence on those edges.',
 'nativeSources':list(sources.values()),'groupReports':reports,'counts':{'runtimeSprites':len(assets),'nativeSources':len(sources),'networkFamilies':3,'multiCellGroups':len(groups)},
 'actualThemeId':None,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN','actualGameplayBindingsVerified':False,'actualLegacyReferencesZeroVerified':False,
 'previewDisclaimer':'Offline native sprite assemblies and layout-edit demonstrations; not actual Unity captures or proof of production-map application.'}
write(P/'manifest.json',main);(P/'manifest_sha256.txt').write_text(sha(P/'manifest.json')+'\n')
write(P/'native_sources_manifest.json',{'sources':list(sources.values()),'runtimeSelectable':False,'promptsLocation':'Authoring/**/generation_prompts.json and producer prompt files'})
write(P/'sprite_import_settings.json',{'default':{'spriteMode':'Single','meshType':'FullRect','filterMode':'Bilinear','compression':'Uncompressed','mipMaps':False,'wrapMode':'Clamp'},'spriteSettings':[{k:a[k]for k in ['file','width','height','ppu','pivot','logicalRect']}for a in main['assets']]})
for folder in ['Runtime','Editor','Tests~']:
 for s in (W/'runtime_exports'/folder).rglob('*'):
  if s.is_file():put(s,P/folder/s.relative_to(W/'runtime_exports'/folder))
for d in ['audit_exports','runtime_exports','handoff_exports']:
 for s in (W/d).glob('*.json'):put(s,P/'QA'/s.name)
for n in ['README_KO.txt','UNITY_CLI_KO.txt']:put(W/'handoff_exports'/n,P/n)
put(W/'CONTRACT.txt',P/'Documentation/ART_AND_RUNTIME_CONTRACT.txt')
print(json.dumps({'status':'ASSEMBLED','runtimeSprites':len(assets),'nativeSources':len(sources),'groups':groups,'package':str(P)},ensure_ascii=False))
