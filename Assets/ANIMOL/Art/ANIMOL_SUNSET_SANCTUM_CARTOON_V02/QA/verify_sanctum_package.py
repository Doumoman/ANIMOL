"""Independent new-package closure audit. Never imports Unity or modifies package files."""
from pathlib import Path
import argparse,json,hashlib,re,collections,struct,math
from PIL import Image
ap=argparse.ArgumentParser();ap.add_argument('--package',type=Path,required=True);ap.add_argument('--report',type=Path,required=True)
ap.add_argument('--runtime-only',action='store_true',help='Verify the merged 01_CORE+02_FLOOR+03_BRIDGES install without optional 04_SOURCES authoring/provenance files. Runtime, catalog, importer, aliases, terrain and scene/layout checks remain enabled.')
args=ap.parse_args();R=args.package.resolve();W=Path(__file__).resolve().parent.parent;errors=[];warnings=[];stats={};skipped=[]
def require(c,msg):
 if not c:errors.append(msg)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def pairs(xs):
 d={}
 for k,v in xs:
  if k in d:errors.append('Duplicate JSON key:'+str(k))
  d[k]=v
 return d
def read(p):return json.loads(p.read_text(),object_pairs_hook=pairs)
def rec(v,refs):
 if isinstance(v,str) and v.endswith('.png') and v.startswith(('sources/','production/')):refs.add(v)
 elif isinstance(v,dict):
  for x in v.values():rec(x,refs)
 elif isinstance(v,list):
  for x in v:rec(x,refs)
def mapping(rows,label):
 d={};fold=set()
 for a in rows:
  f=a.get('file');require(isinstance(f,str),label+' missing file')
  if not isinstance(f,str):continue
  require(f not in d and f.lower() not in fold,label+' duplicate/case collision:'+f);d[f]=a;fold.add(f.lower())
 return d
ID='ANIMOL_SUNSET_SANCTUM_CARTOON_V02';M=read(R/'manifest.json');C=read(R/'animation_catalog.json');S=read(R/'sprite_import_settings.json');A=read(R/'endpoint_aliases.json');assets=mapping(M['assets'],'main');sprites=mapping(C['sprites'],'catalog');settings=mapping(S['spriteSettings'],'import settings')
require(M['packageId']==C['packageId']==S['packageId']==ID,'Wrong package identity')
require(R.name==ID,'Wrong package-root basename')
require(M['pixelArtRules'] is False and M['pixelsPerUnit']==256 and M['logicalCellWorldUnits']==1 and M['tileSize']==256,'Obsolete art/grid contract')
require(M['themeDisplayName']=='낙일 성소' and M['postStoryDisplayName']=='천구 날조국','Theme display names wrong')
require(M['actualThemeId'] is None and M['themeIdVerifiedInUnityProject'] is False,'Actual theme ID incorrectly verified')
require('story' in M['themeNamePolicy'].lower() and 'ID' in M['themeNamePolicy'],'Story-name-only policy absent')
require(M['noLegacyFallback'] is True and A['noLegacyFallback'] is True,'Legacy fallback permitted')
for key in ['unityExecuted','actualProjectBindingsVerified','actualLegacyReferencesZeroVerified']:require(M[key] is False,'Actual Unity status falsely verified:'+key)
for key in ['actualUnityImport','csharpCompile','shaderCompile']:require(M[key]=='NOT_RUN','Actual Unity status not NOT_RUN:'+key)
require(set(assets)==set(sprites)==set(settings),'Main/catalog/import inventory differs')
require((R/'animation_catalog_sha256.txt').read_text().strip()==sha(R/'animation_catalog.json'),'Catalog pinned SHA mismatch')
rawHashes={};decoded=0
for f,a in assets.items():
 p=R/f;require(p.is_file(),'Missing runtime PNG:'+f)
 if not p.is_file():continue
 require(not Path(f).is_absolute() and '..' not in Path(f).parts,'Unsafe runtime path:'+f)
 rawHashes[f]=sha(p)
 require(a['sha256']==rawHashes[f],'Main PNG SHA mismatch:'+f)
 for other,label in [(sprites[f],'catalog'),(settings[f],'import')]:
  for k in ['width','height','ppu','pivot']:require(a[k]==other[k],label+' metadata mismatch:'+f+':'+k)
 require(sprites[f]['sha256']==rawHashes[f],'Catalog PNG SHA mismatch:'+f)
 try:
  with Image.open(p) as im:
   im.load();require(im.format=='PNG' and list(im.size)==[a['width'],a['height']],'PNG dimensions/format mismatch:'+f);decoded+=1
 except Exception as e:errors.append('Runtime PNG decode:'+f+':'+type(e).__name__)
 require(a['ppu']==256 and a['deriveColliderFromAlpha'] is False,'PPU or alpha-derived collider wrong:'+f)
 require(a.get('existingGameplayId') is None,'Unverified actual gameplay ID invented:'+f)
old=W/'reference/ANIMOL_CLOCK_DOME_V01/ANIMOL_CLOCK_DOME_V01';oldHashes={sha(p) for p in old.rglob('*.png')};oldMatches=[f for f,h in rawHashes.items() if h in oldHashes];require(not oldMatches,'Obsolete pixel runtime PNG reused:'+str(oldMatches))
oldBaselineStatus='CHECKED' if oldHashes else 'NOT_AVAILABLE'
if not oldHashes:skipped.append('External old-pixel baseline byte comparison: original baseline is not installed; no zero-reuse claim is made by this run.')
# Exact C# importer entries, with no missing/new-other asset accepted by its dictionary.
I=(R/'Editor/SunsetSanctumV02SpriteImporter.cs').read_text();specs={}
pat=r'\{\s*"([^"\\]+)"\s*,\s*new SpriteSpec\(\s*([0-9.eE+\-]+)f\s*,\s*([0-9.eE+\-]+)f\s*,\s*([0-9.eE+\-]+)f\s*\)\s*\}'
for f,ppu,px,py in re.findall(pat,I):require(f not in specs,'Duplicate exact importer entry:'+f);specs[f]=[float(ppu),float(px),float(py)]
require(set(specs)==set(assets),'Importer exact-file set differs')
for f,a in assets.items():
 expected=[float(a['ppu'])]+[float(x) for x in a['pivot']];actual=specs.get(f,[])
 require(len(actual)==3 and all(struct.pack('<f',u)==struct.pack('<f',v) for u,v in zip(actual,expected)),'Importer ppu/pivot single-precision mismatch:'+f)
for token in ['"/'+ID+'/"','FilterMode.Bilinear','TextureImporterCompression.Uncompressed','mipmapEnabled = false','TextureWrapMode.Clamp','TextureImporterNPOTScale.None','SpriteMeshType.FullRect','spriteGenerateFallbackPhysicsShape = false']:
 require(token in I,'Importer missing declared policy:'+token)
keys=set();identities=set();animated=set();generated=set();clipSummary=[]
for c in C['clips']:
 key=c['key'];t=(c['family'],c['heading'],c['state']);require(key not in keys and t not in identities,'Duplicate clip key/tuple:'+key);keys.add(key);identities.add(t)
 require(type(c['loop']) is bool and c['fps']>0 and c['frames'],'Invalid clip loop/fps/frames:'+key)
 require(all(f in assets for f in c['frames']),'Clip references foreign/missing frame:'+key)
 if len(c['frames'])>1:
  require(c['fps']>=10,'Motion fps below10:'+key);require(len({rawHashes.get(f) for f in c['frames']})>=2,'Animation has no distinct native frames:'+key)
 animated.update(c['frames']);g=re.sub('[^a-zA-Z0-9_-]','_',key)[:70]+'_'+hashlib.sha256(key.encode()).hexdigest()[:12]+'.anim';require(g not in generated,'Generated animation filename collision:'+g);generated.add(g)
 clipSummary.append({'family':c['family'],'heading':c['heading'],'state':c['state'],'frames':len(c['frames']),'fps':c['fps']})
parts={}
for p in C['parts']:
 require(p['key']==p['file'],'Static BindExactFile cannot resolve non-path part key:'+p['key']);require(p['key'] not in parts,'Duplicate static part key:'+p['key']);require(p['file'] in assets,'Part foreign/missing frame:'+p['file']);parts[p['key']]=p['file']
require(set(parts)==set(assets),'Static parts do not cover every latest runtime file')
aliases=A['aliases'];catAliases={a['file']:a['latestFrame'] for a in C['endpointAliases']};require(aliases==catAliases,'Alias catalog vs endpoint manifest disagreement')
for f,v in aliases.items():require(f in assets and v in assets and rawHashes.get(f)==rawHashes.get(v),'Alias is not exact latest endpoint:'+f)
refs=set()
for p in R.glob('*.json'):rec(read(p),refs)
for f in sorted(refs) if not args.runtime_only else []:
 p=R/f;require(p.is_file(),'Missing source/production reference:'+f)
 if p.is_file():
  try:
   with Image.open(p) as im:im.verify()
  except Exception as e:errors.append('Source/production PNG invalid:'+f+':'+type(e).__name__)
sourceFiles=[]
for a in M['nativeSources']:
 f=a['file'];sourceFiles.append(f);require(f.startswith('sources/') and f not in assets,'Native source entered runtime inventory:'+f);require(a['runtimeSelectable'] is False,'Native source selectable:'+f)
 if not args.runtime_only:require((R/f).is_file() and sha(R/f)==a['sha256'],'Native source SHA mismatch:'+f)
require(len(sourceFiles)==len(set(sourceFiles))==M['counts']['nativeSources'] and len(sourceFiles)>0,'Native source count or duplicate unexpected')
if args.runtime_only:skipped.extend(['Optional sources/production PNG existence, decode and byte SHA checks.','Optional native atlas file existence and byte SHA checks.','External frozen authoring exports: package-local group metadata and every runtime SHA are checked instead.'])
# Copy closure across frozen native groups: runtime RGB never changed during central assembly.
configs=[('terrain','terrain_assets.json'),('scenery','scenery_manifest.json'),('common','common_devices_manifest.json'),('movement','movement_manifest.json'),('optics','optics_manifest.json')];groupAssets={};groupClips=[];groupReports=[]
for group,filename in configs:
 external=W/(group+'_exports')/filename
 useExternal=not args.runtime_only and external.is_file()
 p=external if useExternal else R/filename;j=read(p)
 if not args.runtime_only and not useExternal:skipped.append('External frozen '+group+' export is not available; package-local group manifest and runtime SHA closure are checked instead.')
 for a in j['assets']:
  require(a['file'] not in groupAssets,'Group filename ownership collision:'+a['file']);groupAssets[a['file']]=a
  require(a['file'] in assets and a['sha256']==assets[a['file']]['sha256'],'Assembled runtime differs from frozen native export:'+a['file'])
 groupClips.extend(j.get('clips',[]));groupReports.append({'group':group,'manifestSha256':sha(p),'assets':len(j['assets']),'clips':len(j.get('clips',[])),'comparisonSource':'external frozen native export' if useExternal else 'package-local group manifest'})
require(set(groupAssets)==set(assets),'Assembled assets contain extra/missing group output')
require({(c['key'],tuple(c['frames']),c['fps'],c['loop']) for c in groupClips}=={(c['key'],tuple(c['frames']),c['fps'],c['loop']) for c in C['clips']},'Assembled clips differ from frozen native groups')
T=read(R/'terrain_manifest.json');bits={'N':1,'NE':2,'E':4,'SE':8,'S':16,'SW':32,'W':64,'NW':128};require(T['neighborBits']==bits,'Terrain8neighbor convention wrong')
for material,row in T['materials'].items():
 files=row['neighborMaskToFile'];require(len(files)==256 and len(set(files))==256,'Raw256terrain mapping incomplete:'+material);require(all(f in assets for f in files),'Terrain mapping references unregistered tile:'+material)
 require(all('/full256/' in f for f in files),'Old47canonical terrain selected:'+material)
terrainSelector=(R/'Runtime/SunsetSanctumV02TerrainSelector.cs').read_text();require('full256' in terrainSelector and '/blob47/' not in terrainSelector,'Terrain runtime selector not latest full256')
Cmn=read(R/'common_devices_manifest.json');require(Cmn['semantics']['oneway_gate']['minimumOpenLogicalWidthFraction']==164/256 and Cmn['semantics']['oneway_gate']['occupiedCellCount']==1,'Onecell gate current geometry policy wrong')
Sc=read(R/'scenery_manifest.json');layers={l['key']:l for l in Sc['layerContract']['layers']};require(layers['stars']['sortingOrder']<layers['far_clouds']['sortingOrder']<layers['near_clouds']['sortingOrder'],'Stars rendered in front of clouds');require(layers['stars']['cameraParallax']<layers['far_clouds']['cameraParallax']<layers['near_clouds']['cameraParallax'],'Star/cloud sensitivity order wrong');require(Sc['layerContract']['moonAndPlanetsAbsent'] and Sc['layerContract']['allCloudsIndependent'],'Background source policy wrong')
for p in (R/'Runtime').glob('*.cs'):
 txt=p.read_text();require('namespace ANIMOL.Art.SunsetSanctumV02' in txt,'Foreign runtime namespace:'+p.name)
 for oldId in ['"ANIMOL_STARWAY_','"ANIMOL_ECHO_','"ANIMOL_CLOCK_']:
  require(oldId not in txt,'Old package runtime fallback/string:'+p.name+':'+oldId)
B=(R/'Editor/SunsetSanctumV02AnimationBuilder.cs').read_text();require('public static void BuildFromCommandLine()' in B and '"-sanctumV02Manifest"' in B,'Wrong documented catalog batch API');require('new AnimationEvent[0]' in B and 'propertyName = "m_Sprite"' in B,'Generated animation not sprite-only/event-free')
MB=(R/'Editor/SunsetSanctumV02MaterialBuilder.cs').read_text();require('public static void BuildFromCommandLine()' in MB and '"-sanctumV02BeamMaterial"' in MB,'Wrong documented material batch API')
# Layout references are runtime data. Both full and runtime-only modes verify them.
def finite_values(v,label):
 if isinstance(v,dict):
  for k,x in v.items():finite_values(x,label+'.'+k)
 elif isinstance(v,list):
  for i,x in enumerate(v):finite_values(x,label+'['+str(i)+']')
 elif type(v) in (int,float):require(math.isfinite(v),'Nonfinite layout value:'+label)
BG=read(R/'background_scene_layout.json');finite_values(BG,'background_scene_layout')
require(BG['packageId']==ID and BG['skyBase']['file'] in assets,'Background layout identity/sky reference wrong')
bgNames=set();bgStars=[];bgClouds=[]
for row in BG['layers']:
 require(row['id'] not in bgNames,'Background duplicate layer ID:'+row['id']);bgNames.add(row['id'])
 require(row['file'] in assets,'Background layout foreign sprite:'+row['file'])
 require(all(x>0 for x in row['localScale']),'Background nonpositive scale:'+row['id'])
 if '/stars/' in row['file']:bgStars.append(row)
 if '/clouds/' in row['file']:bgClouds.append(row)
require(bgStars and bgClouds,'Background missing independent stars/clouds')
require(max(r['sortingOrder'] for r in bgStars)<min(r['sortingOrder'] for r in bgClouds),'Background layout stars in front of clouds')
require(max(r['parallaxMultiplier'] for r in bgStars)<min(r['parallaxMultiplier'] for r in bgClouds),'Background layout star response too large')
require(all(not any(r['driftAmplitude']) for r in bgStars),'Background layout stars drift')
sceneStats={'backgroundLayersVerified':len(BG['layers']),'reviewFixture':'NOT_PRESENT'}
reviewFiles=['review_scene_layout.json','review_scene_effects.json','review_map_layout.json']
require(all((R/f).is_file() for f in reviewFiles),'Missing review scene/effects/map layout DTO: all three remain required in runtime-only mode')
if any((R/f).exists() for f in reviewFiles):
 require(all((R/f).is_file() for f in reviewFiles),'Partial review fixture: all three review DTOs are required')
 if all((R/f).is_file() for f in reviewFiles):
  L=read(R/reviewFiles[0]);E=read(R/reviewFiles[1]);G=read(R/reviewFiles[2])
  for obj,name in [(L,'review_scene_layout'),(E,'review_scene_effects'),(G,'review_map_layout')]:finite_values(obj,name)
  require(L['packageId']==ID and L['actualUnityExecuted'] is False and L['previewOnly'] is True,'Review fixture wrongly claims actual game/Unity execution')
  require(E['actualUnityExecuted'] is False and E['softwarePreviewTimingOnly'] is True,'Review effects wrongly claim Unity execution')
  fixed={};moving={};reviewRefs=set();clipsByKey={c['key']:c for c in C['clips']}
  for obj in L['sprites']:
   require(obj['name'] not in fixed,'Review duplicate static name:'+obj['name']);fixed[obj['name']]=obj
   require(obj['file'] in assets,'Review foreign sprite:'+obj['file']);reviewRefs.add(obj['file'])
   require(obj['scaleX']>0 and obj['scaleY']>0 and 0<=obj['opacity']<=1,'Review visual scale/opacity invalid:'+obj['name'])
  for obj in L['motions']:
   require(obj['name'] not in moving,'Review duplicate motion name:'+obj['name']);moving[obj['name']]=obj
   require(obj['secondsPerClip']>0,'Review nonpositive duration:'+obj['name'])
   for key in obj['clipKeys']:
    require(key in clipsByKey,'Review foreign clip:'+key)
    if key in clipsByKey:reviewRefs.update(clipsByKey[key]['frames'])
  require(not set(fixed)&set(moving),'Review static/motion duplicate binding')
  grid=G['rowsPositiveYDown'];height=len(grid);width=len(grid[0]);occupied={(x,y) for y,row in enumerate(grid) for x,v in enumerate(row) if v!='.'}
  require(height==12 and width==9 and len(occupied)==len(G['tiles'])==88,'Review tile fixture dimensions/count mismatch')
  nb=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]
  origin=G['worldGridOrigin'];cell=G['cellSize']
  require(cell==1 and E['cellSize']==cell and E['gridOrigin']==origin,'Review grid origin/cell size disagreement')
  def world(x,y):return [origin[0]+(x+.5)*cell,origin[1]+(height-1-y+.5)*cell]
  for tile in G['tiles']:
   x,y=tile['column'],tile['row'];mask=sum(b for dx,dy,b in nb if(x+dx,y+dy) in occupied)
   require((x,y) in occupied and mask==tile['neighborMask'],'Review full256 neighbor mask mismatch:'+str((x,y)))
   require(tile['worldCenter']==world(x,y) and tile['file'] in assets and tile['file'].endswith('/n%03d.png'%mask),'Review tile file/anchor mismatch:'+str((x,y)))
  positions=G['devicePositions']
  for name,pos in positions.items():
   if name.startswith('under_'):continue
   obj=fixed.get(name,moving.get(name));require(obj is not None,'Review grid position has no scene object:'+name)
   if obj:require([obj['x'],obj['y']]==world(*pos),'Review object ground anchor mismatch:'+name)
  for name,dimensions in [('bottom_library',[512,256]),('bottom_rail',[512,256]),('one_cell_gate',[256,256])]:
   obj=fixed.get(name,moving.get(name));require(obj is not None,'Review visual-footprint object missing:'+name)
   if obj:
    file=obj.get('file')
    if not file and obj['clipKeys'][0] in clipsByKey:file=clipsByKey[obj['clipKeys'][0]]['frames'][0]
    require(file in assets and assets[file]['logicalRect'][2:]==dimensions,'Review declared art footprint mismatch:'+name)
  jump=E['moonJumpGuide'];require(jump['distanceCells']==3 and jump['heading']=='east' and jump['toCell']==[jump['fromCell'][0]+3,jump['fromCell'][1]],'Review jump distance/heading mismatch')
  require(jump['fromWorld']==world(*positions['moon_jump']) and jump['toWorld']==world(*positions['moon_landing']),'Review jump endpoint world anchor mismatch')
  require(all((positions['moon_jump'][0]+i,positions['moon_jump'][1]) not in occupied for i in [1,2]),'Review jump intermediate cells not void')
  require(positions['portal_exit_travel']==[positions['portal_B'][0]+1,positions['portal_B'][1]] and tuple(positions['portal_exit_travel']) in occupied,'Review portal beyond-exit fixture mismatch')
  require(positions['wood_comparison'][0]<positions['metal_target'][0]<positions['metal_pull_direction'][0]<positions['magnet_west'][0] and positions['wood_comparison'][1]==positions['metal_target'][1]==positions['magnet_west'][1],'Review magnet pull/wood comparison mismatch')
  route=E['lightRoute'];mirror=[c for c in route['cells'] if c['kind']=='mirror'];glass=[c for c in route['cells'] if c['kind']=='glass'];receiver=[c for c in route['cells'] if c['kind']=='receiver']
  require(len(mirror)==len(glass)==len(receiver)==1,'Review glass/mirror/receiver route count wrong')
  if len(mirror)==len(glass)==len(receiver)==1:
   require(mirror[0]['incoming']=='east' and mirror[0]['outgoing']=='north' and route['cells'].index(glass[0])<route['cells'].index(mirror[0])<route['cells'].index(receiver[0]),'Review glass/slash route order/direction mismatch')
   require(receiver[0]['receiverKey']==route['receiverKey']=='review_sensor','Review shared receiver key mismatch')
  require(E['worldSegments']==[{'from':world(*positions['review_emitter']),'to':world(*positions['review_mirror'])},{'from':world(*positions['review_mirror']),'to':world(*positions['review_sensor'])}],'Review shared beam world segments mismatch')
  require(fixed['review_linked_gate']['file']=='devices/light_gate/open.png' and fixed['review_sensor']['file']=='devices/light_receiver/lit.png','Review linked gate/sensor state mismatch')
  stars=[o for o in fixed.values() if '/stars/' in o['file']];clouds=[o for o in fixed.values() if '/clouds/' in o['file']]
  require(stars and clouds and max(o['sortingOrder'] for o in stars)<min(o['sortingOrder'] for o in clouds),'Review stars not behind all clouds')
  require(max(o['parallaxMultiplier'] for o in stars)<min(o['parallaxMultiplier'] for o in clouds) and all(o['driftX']==o['driftY']==0 for o in stars),'Review star/cloud motion ordering wrong')
  sceneStats.update({'reviewFixture':'CHECKED','staticObjects':len(fixed),'motionObjects':len(moving),'runtimePngRefs':len(reviewRefs),'tileMasksAndAnchors':len(G['tiles'])})
count=M['counts'];require(count['runtimeSprites']==len(assets)==1024,'Runtime count mismatch');require(count['animationClips']==len(C['clips'])==106,'Clip count mismatch');require(count['catalogParts']==len(parts)==1024,'Parts count mismatch');require(count['endpointAliases']==len(aliases)==35,'Alias count mismatch')
stats={'runtimePngDecoded':decoded,'runtimeAssets':len(assets),'clips':len(keys),'exactFamilyHeadingStateTuples':len(identities),'uniqueAnimationFrameFiles':len(animated),'staticPartKeys':len(parts),'exactImporterEntries':len(specs),'endpointAliases':len(aliases),'nativeSourcesDeclared':len(sourceFiles),'nativeSourceFilesVerified':0 if args.runtime_only else len(sourceFiles),'sourceAndProductionPngRefsVerified':0 if args.runtime_only else len(refs),'sourceAndProductionPngRefsDeclared':len(refs),'oldPixelBaselineComparison':oldBaselineStatus,'oldPixelBaselineHashes':len(oldHashes),'oldPixelRuntimeMatches':len(oldMatches) if oldHashes else None,'groupReports':groupReports,'sceneLayout':sceneStats}
report={'schemaVersion':1,'packageId':ID,'mode':'runtime-only' if args.runtime_only else 'full','status':'PASS_OFFLINE' if not errors else'FAIL_OFFLINE','scope':'Runtime PNG decode/SHA, catalog/importer/alias/group closure, terrain and scene/layout metadata, source-code API contracts. Full mode also verifies authoring/native provenance files; runtime-only omits only those optional original-file checks. Unity execution and installed binding are not tested.','skippedChecks':skipped,'manifestSha256':sha(R/'manifest.json'),'catalogSha256':sha(R/'animation_catalog.json'),'stats':stats,'errors':errors,'errorCount':len(errors),'warnings':warnings,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualShaderCompile':'NOT_RUN','actualNUnit':'NOT_RUN','actualPlayMode':'NOT_RUN','actualProjectBindingVerified':False,'actualLegacyReferencesZeroVerified':False}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':report['status'],'errors':errors,'stats':stats},ensure_ascii=False));raise SystemExit(bool(errors))
