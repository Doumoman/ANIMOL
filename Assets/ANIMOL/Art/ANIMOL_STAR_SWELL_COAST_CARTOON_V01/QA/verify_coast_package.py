"""Independent Coast package closure audit. File/source checks only, never Unity execution."""
from pathlib import Path
import argparse,json,hashlib,re,struct,math
from PIL import Image
ap=argparse.ArgumentParser();ap.add_argument('--package',type=Path,required=True);ap.add_argument('--report',type=Path,required=True)
ap.add_argument('--runtime-only',action='store_true',help='Omit optional native/authoring file checks; runtime/catalog/import/alias/layout/old-art fingerprint checks remain enabled.')
args=ap.parse_args();R=args.package.resolve();A=Path(__file__).resolve().parent;W=A.parent
ID='ANIMOL_STAR_SWELL_COAST_CARTOON_V01';NS='ANIMOL.Art.StarSwellCoastV01';errors=[];skipped=[]
def need(ok,msg):
 if not ok:errors.append(msg)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def pairs(xs):
 d={}
 for k,v in xs:
  need(k not in d,'Duplicate JSON key:'+str(k));d[k]=v
 return d
def read(p):return json.loads(p.read_text(),object_pairs_hook=pairs)
def indexed(rows,label):
 d={};fold=set()
 for a in rows:
  f=a.get('file');need(isinstance(f,str),label+' asset lacks filename')
  if not isinstance(f,str):continue
  need(f not in d and f.lower() not in fold,label+' duplicate/casecollision:'+f)
  need(not Path(f).is_absolute() and '..' not in Path(f).parts,label+' unsafe path:'+f)
  d[f]=a;fold.add(f.lower())
 return d
def png_refs(v,out,prefixes=('sources/','production/')):
 if isinstance(v,str) and v.endswith('.png') and v.startswith(prefixes):out.add(v)
 elif isinstance(v,dict):
  for x in v.values():png_refs(x,out,prefixes)
 elif isinstance(v,list):
  for x in v:png_refs(x,out,prefixes)
def finite(v,label):
 if isinstance(v,dict):
  for k,x in v.items():finite(x,label+'.'+k)
 elif isinstance(v,list):
  for x in v:finite(x,label)
 elif type(v) in (float,int):need(math.isfinite(v),'Nonfinite layout numeric:'+label)
M=read(R/'manifest.json');C=read(R/'animation_catalog.json');S=read(R/'sprite_import_settings.json');E=read(R/'endpoint_aliases.json')
assets=indexed(M['assets'],'main');sprites=indexed(C['sprites'],'catalog');settings=indexed(S['spriteSettings'],'import')
need(R.name==ID and M['packageId']==C['packageId']==S['packageId']==ID,'Wrong package/root identity')
need(M['themeDisplayName']=='별너울 해안','Wrong coast display name')
need(M['pixelArtRules'] is False and M['pixelsPerUnit']==256 and M['tileSize']==256 and M['logicalCellWorldUnits']==1,'Old pixel/PPU/grid policy')
need(M.get('actualThemeId') is None and not M.get('themeIdVerifiedInUnityProject',False),'Actual theme ID invented/verified without host')
need(M['noLegacyFallback'] is True and E['noLegacyFallback'] is True,'Old-art fallback allowed')
for k in ['unityExecuted','actualProjectBindingsVerified','actualLegacyReferencesZeroVerified']:need(M[k] is False,'Actual Unity binding falsely verified:'+k)
for k in ['actualUnityImport','csharpCompile','shaderCompile']:need(M[k]=='NOT_RUN','Actual Unity status falsely passed:'+k)
need(set(assets)==set(sprites)==set(settings),'Main/catalog/import allowlists differ')
need((R/'animation_catalog_sha256.txt').read_text().strip()==sha(R/'animation_catalog.json'),'Catalog pinnedSHA mismatch')
cache=A/'rejected_prior_art_hashes.json';need(cache.is_file(),'Missing QA/rejected_prior_art_hashes.json required for old-art comparison')
prior=read(cache)['entries'] if cache.is_file() else[]
priorBytes={r['sha256'] for r in prior if not r['fullyTransparent']};priorRgba={r['decodedRgbaSha256'] for r in prior if not r['fullyTransparent']}
raw={};rgba={};decoded=0;oldMatches=[]
for f,a in assets.items():
 p=R/f;need(p.is_file(),'Missing runtime PNG:'+f)
 if not p.is_file():continue
 raw[f]=sha(p);need(raw[f]==a['sha256']==sprites[f]['sha256'],'Runtime SHA differs:'+f)
 for key in ['width','height','ppu','pivot']:need(a[key]==sprites[f][key]==settings[f][key],'Import/catalog metadata differs:'+f+':'+key)
 need(a['ppu']==256 and a['deriveColliderFromAlpha'] is False,'PPU/collider policy wrong:'+f)
 need(a.get('existingGameplayId') is None and a.get('actualGameplayFootprintCells') is None,'Unverified actual gameplay contract invented:'+f)
 need(a.get('registerGameplayCellFromThisRecord',False) is False,'Art record registers gameplay occupancy:'+f)
 try:
  with Image.open(p) as im:
   im.load();need(im.format=='PNG' and list(im.size)==[a['width'],a['height']],'Runtime PNG dims/format differs:'+f)
   rgba[f]=hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest();decoded+=1
  if 'decodedRgbaSha256' in a:need(rgba[f]==a['decodedRgbaSha256'],'Runtime decodedRGBA declaration differs:'+f)
  if raw[f] in priorBytes or rgba[f] in priorRgba:oldMatches.append(f)
 except Exception as ex:errors.append('Runtime decode:'+f+':'+type(ex).__name__)
need(not oldMatches,'Old Coast/Sea/sanctuary art reused:'+str(oldMatches))
I=(R/'Editor/StarSwellCoastV01SpriteImporter.cs').read_text();specs={}
pattern=r'\{\s*"([^"\\]+)"\s*,\s*new SpriteSpec\(\s*([0-9.eE+\-]+)f\s*,\s*([0-9.eE+\-]+)f\s*,\s*([0-9.eE+\-]+)f\s*\)\s*\}'
for f,ppu,x,y in re.findall(pattern,I):need(f not in specs,'Duplicate importer entry:'+f);specs[f]=[float(ppu),float(x),float(y)]
need(set(specs)==set(assets),'C# importer exact filename set differs')
for f,a in assets.items():
 v=specs.get(f,[]);expected=[a['ppu']]+a['pivot'];need(len(v)==3 and all(struct.pack('<f',u)==struct.pack('<f',w) for u,w in zip(v,expected)),'Importer PPU/pivot Single differs:'+f)
for token in ['"/'+ID+'/"','FilterMode.Bilinear','TextureImporterCompression.Uncompressed','mipmapEnabled = false','TextureWrapMode.Clamp','TextureImporterNPOTScale.None','SpriteMeshType.FullRect','spriteGenerateFallbackPhysicsShape = false']:need(token in I,'Importer contract missing:'+token)
keys=set();tuples=set();generated=set();animated=set()
for clip in C['clips']:
 k=clip['key'];identity=(clip['family'],clip['heading'],clip['state']);need(k not in keys and identity not in tuples,'Duplicate clip key/tuple:'+k);keys.add(k);tuples.add(identity)
 need(clip['frames'] and type(clip['loop']) is bool and clip['fps']>0,'Clip timing invalid:'+k)
 need(all(f in assets for f in clip['frames']),'Clip missing/foreign runtime frame:'+k)
 if len(clip['frames'])>1:need(clip['fps']>=10 and len({rgba.get(f) for f in clip['frames']})>=2,'Motion lacks distinct native phases/fps:'+k)
 animated.update(clip['frames']);g=re.sub('[^a-zA-Z0-9_-]','_',k)[:70]+'_'+hashlib.sha256(k.encode()).hexdigest()[:12];need(g not in generated,'Generated animation filename collision:'+k);generated.add(g)
parts={}
for p in C['parts']:need(p['key']==p['file'] and p['key'] not in parts and p['file'] in assets,'Invalid exact-file static part:'+p['key']);parts[p['key']]=p['file']
need(set(parts)==set(assets),'Static parts do not cover every latest runtime file')
aliases=E['aliases'];need(aliases=={r['file']:r['latestFrame'] for r in C['endpointAliases']},'Endpoint alias manifests differ')
for f,t in aliases.items():need(f in assets and t in assets and raw.get(f)==raw.get(t),'Alias not byte-identical to latest endpoint:'+f)
native={a['file']:a for a in M['nativeSources']};need(len(native)==len(M['nativeSources'])>0,'Native source duplicate/empty')
for f,a in native.items():
 need(f.startswith('sources/') and f not in assets and a['runtimeSelectable'] is False,'Native source entered runtime selection:'+f)
 need(a['sha256'] not in priorBytes,'Prior bitmap declared as new native:'+f)
 if not args.runtime_only:
  need((R/f).is_file(),'Missing native source:'+f)
  if(R/f).is_file():
   need(sha(R/f)==a['sha256'],'Native source SHA differs:'+f)
   with Image.open(R/f) as im:im.load();need(hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest() not in priorRgba,'Reencoded prior native bitmap reused:'+f)
for f,a in assets.items():
 refs=set();png_refs(a,refs,('sources/',));need(refs and refs<=set(native),'Asset lacks complete registered NEW-native lineage:'+f)
proofRefs=set()
for p in R.glob('*.json'):png_refs(read(p),proofRefs)
if args.runtime_only:skipped.append('Optional native/production file existence,decode,byteSHA checks; runtime lineage declarations/catalog/import/aliases/prior-art fingerprints and layouts remain checked.')
else:
 for f in proofRefs:
  need((R/f).is_file(),'Missing source/production PNG reference:'+f)
  if(R/f).is_file():
   try:
    with Image.open(R/f) as im:im.verify()
   except Exception as ex:errors.append('Source/production PNG decode:'+f+':'+type(ex).__name__)
configs=[('terrain','terrain_assets.json'),('scenery','scenery_manifest.json'),('common','common_devices_manifest.json'),('hazards','hazards_manifest.json'),('boat','boat_manifest.json')]
groupAssets={};groupClips=[];groupRows=[]
for group,filename in configs:
 external=W/(group+'_exports')/filename;useExternal=not args.runtime_only and external.is_file();p=external if useExternal else R/filename;j=read(p)
 if not useExternal:skipped.append('External '+group+' export not used; package-local group manifest runtimeSHA/clip closure checked.')
 for a in j['assets']:
  f=a['file'];need(f not in groupAssets,'Group ownership path collision:'+f);groupAssets[f]=a;need(f in assets and assets[f]['sha256']==a['sha256'],'Canonical differs from final native group:'+f)
 groupClips.extend(j.get('clips',[]));groupRows.append({'group':group,'manifestSha256':sha(p),'assets':len(j['assets']),'clips':len(j.get('clips',[])),'comparisonSource':'external frozen export' if useExternal else 'package-local manifest'})
need(set(groupAssets)==set(assets),'Canonical has extra/missing group runtime files')
need({(c['key'],tuple(c['frames']),c['fps'],c['loop']) for c in groupClips}=={(c['key'],tuple(c['frames']),c['fps'],c['loop']) for c in C['clips']},'Canonical clips differ from groups')
T=read(R/'terrain_manifest.json');need(T['neighborBits']=={'N':1,'NE':2,'E':4,'SE':8,'S':16,'SW':32,'W':64,'NW':128},'Raw8neighbor bit convention wrong')
need(set(T['materials'])=={'coast_sand','wood_bridge','stone_bridge'},'Water entered walkable-land terrain material mapping')
for material,row in T['materials'].items():
 fs=row['neighborMaskToFile'];need(len(fs)==len(set(fs))==256 and all(f in assets and '/full256/' in f for f in fs),'Land full256 mapping incomplete:'+material)
cmn=read(R/'common_devices_manifest.json');need(cmn['semantics']['oneway_gate']['occupiedCellCount']==1 and cmn['semantics']['oneway_gate']['minimumOpenLogicalWidthFraction']>=164/256,'Gate onecell/clearance contract changed')
for p in (R/'Runtime').glob('*.cs'):
 t=p.read_text();need('namespace '+NS in t,'Foreign runtime namespace:'+p.name)
 for foreign in ['"ANIMOL_SUNSET_SANCTUM_','"ANIMOL_TWILIGHT_','"ANIMOL_CLOCK_']:need(foreign not in t,'Old package runtime fallback:'+p.name)
B=(R/'Editor/StarSwellCoastV01AnimationBuilder.cs').read_text();need('public static void BuildFromCommandLine()' in B and '"-coastV01Manifest"' in B,'Wrong catalog builder API/flag');need('new AnimationEvent[0]' in B and 'propertyName = "m_Sprite"' in B,'Generated clips contain gameplay events/foreign curves')
BG=read(R/'background_scene_layout.json');finite(BG,'background');need(BG['packageId']==ID and BG['skyBase']['file'] in assets,'Background identity/sky ref invalid')
for row in BG['layers']:need(row['file'] in assets,'Background foreign PNG:'+row['file'])
L=read(R/'review_scene_layout.json');finite(L,'review');need(L['packageId']==ID and L['actualUnityExecuted'] is False and L['previewOnly'] is True,'Review fixture wrongly claims actual Unity/gameplay')
fixed={};moving={};reviewRefs=set();byClip={c['key']:c for c in C['clips']}
for row in L['sprites']:
 need(row['name'] not in fixed and row['file'] in assets,'Review duplicate/foreign sprite:'+row['name']);fixed[row['name']]=row;reviewRefs.add(row['file'])
 need(row['scaleX']>0 and row['scaleY']>0 and 0<=row['opacity']<=1,'Review scale/opacity invalid:'+row['name'])
for row in L['motions']:
 need(row['name'] not in moving and row['secondsPerClip']>0,'Review duplicate/invalid motion:'+row['name']);moving[row['name']]=row
 for key in row['clipKeys']:
  need(key in byClip,'Review foreign clip:'+key)
  if key in byClip:reviewRefs.update(byClip[key]['frames'])
need(not set(fixed)&set(moving),'Review static/motion double binding')
def family(row):return assets[row['file']].get('family','').lower()
stars=[r for r in fixed.values() if family(r)=='star' or '/stars/' in r['file']];clouds=[r for r in fixed.values() if family(r)=='cloud' or '/clouds/' in r['file']]
suns=[r for r in fixed.values() if family(r) in ['sun','setting_sun','sunset_sun','sun_disc']]
need(len(suns)==1,'Review must contain exactly one independent setting sun')
need(stars and clouds and max(r['sortingOrder'] for r in stars)<min(r['sortingOrder'] for r in clouds),'Stars are not behind every cloud')
need(stars and clouds and max(r['parallaxMultiplier'] for r in stars)<min(r['parallaxMultiplier'] for r in clouds),'Star camera response too large')
need(all(r['driftX']==r['driftY']==0 for r in stars),'Stars drift')
G=read(R/'review_map_layout.json');finite(G,'reviewmap')
# Coordinate/boat/barrel/bomb scenario semantics and actual carrier/shore seams are
# independently reviewed in companion reports; this file does not simulate host logic.
count=M['counts']
for key,n in [('runtimeSprites',len(assets)),('animationClips',len(keys)),('catalogParts',len(parts)),('endpointAliases',len(aliases)),('nativeSources',len(native))]:need(count[key]==n,'Declared count differs:'+key)
stats={'runtimePngDecoded':decoded,'runtimeAssets':len(assets),'clips':len(keys),'exactImporterEntries':len(specs),'staticParts':len(parts),'endpointAliases':len(aliases),'nativeSourcesDeclared':len(native),'nativeSourceFilesVerified':0 if args.runtime_only else len(native),'sourceAndProductionRefsVerified':0 if args.runtime_only else len(proofRefs),'priorArtFingerprintEntries':len(prior),'oldRgbaOrByteMatches':oldMatches,'groups':groupRows,'reviewStatic':len(fixed),'reviewMotions':len(moving),'reviewRuntimePngRefs':len(reviewRefs),'independentSunInstances':len(suns)}
report={'schemaVersion':1,'packageId':ID,'status':'PASS_OFFLINE' if not errors else 'FAIL_OFFLINE','mode':'runtime-only' if args.runtime_only else 'full','scope':'PNG decode/SHA/prior-art fingerprints, complete new-native declarations/files, exact catalog/importer/alias/group and layout closure; no actual Unity execution.','manifestSha256':sha(R/'manifest.json'),'catalogSha256':sha(R/'animation_catalog.json'),'priorFingerprintFileSha256':sha(cache) if cache.is_file() else None,'stats':stats,'errors':errors,'errorCount':len(errors),'skippedChecks':skipped,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualShaderCompile':'NOT_RUN','actualNUnit':'NOT_RUN','actualPlayMode':'NOT_RUN','actualProjectBindingVerified':False,'actualLegacyReferencesZeroVerified':False}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'status':report['status'],'errors':errors,'stats':stats},ensure_ascii=False));raise SystemExit(bool(errors))
