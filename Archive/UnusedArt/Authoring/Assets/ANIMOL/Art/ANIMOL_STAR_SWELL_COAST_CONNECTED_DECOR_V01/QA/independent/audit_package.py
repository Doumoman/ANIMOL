"""Independent Coast source/art/data audit. Never imports producer code or runs Unity."""
from pathlib import Path
from PIL import Image
import argparse, hashlib, json, math, re

O=Path(__file__).resolve().parent
# Approved taxonomy captured from the task contract, independent of inspected package.
APPROVED_DESIGNS={'networks': [{'key': 'rope_dock_fence'}, {'key': 'driftwood_palisade'}, {'key': 'fishing_net_screen'}, {'key': 'raised_dune_grass'}, {'key': 'coral_rock_border'}, {'key': 'harbor_lantern_channel'}], 'groups': [{'key': 'harbor_market_stall_3x2', 'footprintCells': [3, 2]}, {'key': 'fisher_workbench_3x1', 'footprintCells': [3, 1]}, {'key': 'driftwood_bench_3x1', 'footprintCells': [3, 1]}, {'key': 'net_drying_rack_3x1', 'footprintCells': [3, 1]}, {'key': 'cargo_pallet_2x2', 'footprintCells': [2, 2]}, {'key': 'shell_display_2x1', 'footprintCells': [2, 1]}, {'key': 'picnic_setting_2x1', 'footprintCells': [2, 1]}, {'key': 'folded_sail_store_2x2', 'footprintCells': [2, 2]}, {'key': 'dock_notice_board_2x1', 'footprintCells': [2, 1]}, {'key': 'tidepool_garden_2x2', 'footprintCells': [2, 2]}, {'key': 'beached_skiff_3x2', 'footprintCells': [3, 2]}, {'key': 'shore_steps_2x1', 'footprintCells': [2, 1]}], 'points': [{'key': 'dock_lantern', 'footprintCells': [1, 1]}, {'key': 'mooring_bollard', 'footprintCells': [1, 1]}, {'key': 'flotsam_bundle', 'footprintCells': [1, 1]}, {'key': 'coral_planter', 'footprintCells': [1, 1]}, {'key': 'shell_cairn', 'footprintCells': [1, 1]}, {'key': 'message_post', 'footprintCells': [1, 1]}, {'key': 'folded_deck_chair', 'footprintCells': [1, 1]}, {'key': 'beacon_marker', 'footprintCells': [1, 1]}]}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--package',required=True);ap.add_argument('--expected-kinds',type=int,default=26);ap.add_argument('--report',help='Optional output JSON path; omitted writes no files.');args=ap.parse_args()
 P=Path(args.package).resolve(); checks=[];errors=[];warnings=[];footprints=[];joins=[];assets={};ims={}
 def ck(name,yes):
  checks.append({'check':name,'passed':bool(yes)})
  if not yes:errors.append(name)
 def warn(name):warnings.append(name)
 m=read(P/'manifest.json');catalog=read(P/'decoration_catalog.json');pin=sha(P/'manifest.json')
 ck('Exact Coast package identity',m.get('packageId')=='ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01')
 ck('Manifest pin exact bytes',(P/'manifest_sha256.txt').read_text().strip()==pin)
 ck('Canonical neighbor bit schema',m.get('neighborBits')=={'N':1,'E':2,'S':4,'W':8})
 nets={a['key'] for a in catalog['networks']};groups={a['key']:a['footprintCells'] for a in catalog['groups']};points={a['key'] for a in catalog['points']}
 ck('Semantic categories separate/no duplicate keys',not(nets&set(groups) or nets&points or set(groups)&points)and len(nets)+len(groups)+len(points)==args.expected_kinds)
 ck('Catalog semantic count excludes mask/helper variants',catalog.get('familyCountExcludesMasksAndHelperPosts')==args.expected_kinds)
 ck('Root semantic count consistent',m.get('decorationFamilyCountExcludesMaskVariants')==args.expected_kinds)
 contract=APPROVED_DESIGNS
 ck('Exact approved semantic network designs',nets=={a['key']for a in contract['networks']})
 ck('Exact approved semantic whole designs',groups=={a['key']:a['footprintCells']for a in contract['groups']})
 ck('Exact approved semantic point designs',points=={a['key']for a in contract['points']})
 for a in m['assets']:
  f=a['file'];p=P/f
  ck('Safe unique asset path '+f,not f.startswith('/') and '..'not in Path(f).parts and f not in assets);assets[f]=a
  ck('PNG exists '+f,p.is_file())
  if not p.is_file():continue
  ck('PNG file SHA '+f,sha(p)==a.get('sha256'))
  im=Image.open(p);im.load();ck('Explicit RGBA mode '+f,im.mode=='RGBA');im=im.convert('RGBA');ims[f]=im
  ck('PNG dimensions '+f,list(im.size)==[a['width'],a['height']]);ck('PPU256 '+f,a.get('ppu')==256)
  lr,fp,pv=a['logicalRect'],a['footprintCells'],a['pivot']
  ck('Integer positive footprint '+f,len(fp)==2 and all(type(n)is int and n>0 for n in fp))
  ck('Logical extent agrees with footprint '+f,lr[2:]==[fp[0]*256,fp[1]*256])
  ck('Logical rect fits canvas '+f,lr[0]>=0 and lr[1]>=0 and lr[0]+lr[2]<=im.width and lr[1]+lr[3]<=im.height)
  expected=[(lr[0]+lr[2]/2)/im.width,1-(lr[1]+lr[3]/2)/im.height]
  ck('Ground-centre Unity pivot '+f,len(pv)==2 and all(math.isfinite(n)for n in pv)and max(abs(pv[i]-expected[i])for i in range(2))<1e-6)
  alpha=im.getchannel('A');bb=alpha.getbbox();ck('Nonempty real transparency '+f,bb is not None and alpha.getextrema()[0]==0)
  ck('No alpha-derived collider '+f,a.get('deriveColliderFromAlpha')is False)
  for src in a.get('sourceFiles',[]):ck('Source PNG exists '+src,(P/src).is_file())
  ck('Native source references supplied '+f,bool(a.get('sourceFiles')))
  if a['family']in nets:
   ck('Network canonical register '+f,(im.size==(320,384) and lr==[32,96,256,256] and fp==[1,1]))
  if a['family']in groups or a['family']in points:
   expect=groups.get(a['family'],[1,1]);ck('Catalog group footprint '+f,fp==expect)
   ground=alpha.crop((lr[0],lr[1],lr[0]+lr[2],lr[1]+lr[3]));opaque=ground.point(lambda n:255 if n>=128 else 0);gb=opaque.getbbox()
   rows=[]
   for y in range(fp[1]):rows.append(sum(opaque.crop((0,y*256,lr[2],(y+1)*256)).getdata())/(255*lr[2]*256))
   record={'file':f,'family':a['family'],'footprint':fp,'spriteBBox':list(bb)if bb else None,'groundOpaqueBBox':list(gb)if gb else None,'groundRowOpaqueDensity':rows,'wholeVisibleWidthFill':(bb[2]-bb[0])/lr[2]if bb else 0}
   footprints.append(record)
   if a['family']in groups:
    ck('Whole-group width fill >=90% '+f,record['wholeVisibleWidthFill']>=.9)
    if fp[1]>1:
     ck('Whole-group depth extent >=85% '+f,gb is not None and (gb[3]-gb[1])>=.85*lr[3])
     ck('Whole-group each depth row >=35% opaque '+f,min(rows)>=.35)
 ck('Manifest only declared semantic families',set(a['family']for a in assets.values())==nets|set(groups)|points)
 ck('Group mapping includes every whole and point kind',set(g['key']for g in m['groups'])==set(groups)|points)
 for family in sorted(nets):
  fs=[f'decor/connected/{family}/n{i:02d}.png'for i in range(16)]
  ck('Complete16 masks '+family,all(f in assets and f in ims for f in fs))
  if not all(f in ims for f in fs):continue
  ck('All16 masks distinct '+family,len({hashlib.sha256(ims[f].tobytes()).hexdigest()for f in fs})==16)
  strips={n:{'N':im.crop((0,96,320,97)),'E':im.crop((287,0,288,384)),'S':im.crop((0,351,320,352)),'W':im.crop((32,0,33,384))}for n,im in enumerate(ims[f]for f in fs)}
  missing=[];badports=[];badpixels=0;pairs=0;shared=0
  for n in range(16):
   for side,bit in [('N',1),('E',2),('S',4),('W',8)]:
    if n&bit and not strips[n][side].getchannel('A').getbbox():missing.append([n,side])
  for a in range(16):
   for b in range(16):
    for side,abit,other,bbit in [('E',2,'W',8),('N',1,'S',4)]:
     if not(a&abit and b&bbit):continue
     pairs+=1
     if strips[a][side].tobytes()!=strips[b][other].tobytes():badports.append([a,side,b,other])
     aa=ims[fs[a]].crop((256,0,320,384)if side=='E'else(0,0,320,128));bb=ims[fs[b]].crop((0,0,64,384)if side=='E'else(0,256,320,384))
     for p,q in zip(aa.getdata(),bb.getdata()):
      if p[3]and q[3]:
       shared+=1
       if not(p[3]==q[3]==255 and p[:3]==q[:3]):badpixels+=1
  ck('Open ports reach boundary '+family,not missing);ck('Exact opposed RGBA profiles128pairs '+family,pairs==128 and not badports);ck('Safe padded overlap128pairs '+family,pairs==128 and badpixels==0)
  joins.append({'family':family,'compatiblePairs':pairs,'missingOpenPorts':missing,'differentProfiles':badports,'unsafeOverlapPixels':badpixels,'sharedVisiblePixels':shared})
 # Typed boards independently compute claim sets; all26 must actually contain each semantic kind.
 example_reports=[]
 for path in sorted((P/'Examples').glob('*snapshot.json')):
  d=read(path);ck('Board pin '+path.name,d.get('sourceManifestSha256')==pin);ck('Board identity/example flag '+path.name,d.get('packageId')==m['packageId'] and d.get('isWholeBoard')is True and d.get('isExampleOnly')is True)
  ck('Flat nonnull typed arrays '+path.name,all(isinstance(d.get(k),list)and all(isinstance(v,dict)for v in d[k])for k in ['networkCells','groupPlacements','allowedDecorCells']))
  allowed={(a['x'],a['y'],a['logicalLayer'],a['heightKey'])for a in d['allowedDecorCells']};claims=set();used=set();dup=[];denied=[]
  for n in d['networkCells']:
   c=(n['x'],n['y'],n['logicalLayer'],n['heightKey']);used.add(n['family'])
   if c in claims:dup.append(c)
   if c not in allowed:denied.append(c)
   claims.add(c)
  ids=set()
  for g in d['groupPlacements']:
   used.add(g['groupKey']);ck('Unique placementID '+path.name+'/'+g['placementId'],g['placementId']not in ids);ids.add(g['placementId'])
   fp=groups.get(g['groupKey'],[1,1]if g['groupKey']in points else None);ck('Known group '+str(g['groupKey']),fp is not None)
   if not fp:continue
   for dy in range(fp[1]):
    for dx in range(fp[0]):
     c=(g['anchorX']+dx,g['anchorY']+dy,g['logicalLayer'],g['heightKey'])
     if c in claims:dup.append(c)
     if c not in allowed:denied.append(c)
     claims.add(c)
  ck('No overlapping atomic claims '+path.name,not dup);ck('All logical claims approved '+path.name,not denied)
  if 'all26'in path.stem:ck('All26 genuine kinds present in one board',used==nets|set(groups)|points)
  example_reports.append({'file':path.relative_to(P).as_posix(),'sha256':sha(path),'networkCells':len(d['networkCells']),'groupPlacements':len(d['groupPlacements']),'claimedCells':len(claims),'allowedCells':len(allowed),'semanticKinds':sorted(used)})
 # Concrete caller and guarded integration source; lexical evidence only.
 runtime={p.name:p.read_text()for p in(P/'Runtime').glob('*.cs')};editor={p.name:p.read_text()for p in(P/'Editor').glob('*.cs')};allcode='\n'.join(list(runtime.values())+list(editor.values()))
 ck('Independent namespace no prior-theme identifier leftovers',not re.search(r'TheaterConnectedDecor|TheaterDecor|ECHO_THEATER|theaterDecor|SanctumConnectedDecor|SanctumDecor|SUNSET_SANCTUM|sanctumDecor',allcode))
 ck('No unresolved manifest placeholder','__COAST_MANIFEST_SHA256__'not in allcode)
 binding=runtime.get('CoastConnectedDecorV01Binding.cs','');tools=editor.get('CoastConnectedDecorV01BoardTools.cs','')
 for x in ['controller.Configure(','controller.SetLayout(','source.TryCaptureSnapshot(','.Changed += OnSourceChanged','.Changed -= OnSourceChanged']:
  ck('Binding concrete wiring '+x,x in binding)
 for x in ['root.AddComponent<CoastConnectedDecorV01Controller>()','root.AddComponent<CoastConnectedDecorV01AuthoredSnapshotSource>()','root.AddComponent<CoastConnectedDecorV01Binding>()','source.AssignBoard(board, out error)','Require(binding.RefreshNow(out error), error)','PrefabUtility.SaveAsPrefabAsset(']:
  ck('Editor concrete creation '+x,x in tools)
 ck('No fabricated Unity generated binary/text assets',not any(p.suffix in['.prefab','.unity','.asset','.meta']for p in P.rglob('*')if p.is_file()))
 ck('No gameplay physics or input creation',not re.search(r'AddComponent<(?:BoxCollider2D|Collider2D|Rigidbody2D|PlayerInput)>',allcode))
 ck('Actual status honest',m.get('actualUnityImport')=='NOT_RUN'and m.get('actualCSharpCompile')=='NOT_RUN'and m.get('actualUnityPlay')=='NOT_RUN')
 checksums=P/'package_checksums_sha256.txt';checksum_count=0
 if checksums.exists():
  for line in checksums.read_text().splitlines():
   dg,rel=line.split('  ',1);ck('Package checksum '+rel,(P/rel).is_file()and sha(P/rel)==dg);checksum_count+=1
 else:warn('Canonical package checksums not yet built')
 report={'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','scope':'Independent PNG bytes/alpha/ground-register/native-source/cardinal-port/approved-claim/concrete-source wiring audit; no producer imports, C# compile or Unity execution.','packageId':m['packageId'],'manifestSha256':pin,'runtimePngCount':len(assets),'semanticDesignCount':len(nets)+len(groups)+len(points),'checks':checks,'checkCount':len(checks),'errors':errors,'warnings':warnings,'footprints':footprints,'networkJoins':joins,'typedExamples':example_reports,'packageChecksumEntries':checksum_count,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualTestRunner':'NOT_RUN','actualPrefabSceneGeneration':'NOT_RUN','actualUnityPlay':'NOT_RUN','actualProductionHostBindings':'NOT_RUN'}
 if args.report:Path(args.report).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({k:report[k]for k in['status','checkCount','runtimePngCount','semanticDesignCount','errors','warnings']},ensure_ascii=False))
if __name__=='__main__':main()
