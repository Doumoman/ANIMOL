"""Independent Ranch V02 source/art/data audit. Never imports producer code or runs Unity."""
from pathlib import Path
from PIL import Image,ImageChops
import argparse, hashlib, json, math, re

O=Path(__file__).resolve().parent
# Approved taxonomy captured from the task contract, independent of inspected package.
APPROVED_DESIGNS={'networks': [{'key': 'cream_fence', 'connectivity': 'cardinal16'}, {'key': 'meadow_stone_wall', 'connectivity': 'cardinal16'}, {'key': 'raised_flower_meadow', 'connectivity': 'raw8_256'}, {'key': 'orchard_trellis', 'connectivity': 'cardinal16'}, {'key': 'herb_planter_strip', 'connectivity': 'cardinal16'}, {'key': 'ranch_lantern_strip', 'connectivity': 'cardinal16'}], 'groups': [{'key': 'hay_store_2x1', 'footprintCells': [2, 1]}, {'key': 'feeding_corner_3x1', 'footprintCells': [3, 1]}, {'key': 'tree_flower_corner_2x2', 'footprintCells': [2, 2]}, {'key': 'barnyard_3x2', 'footprintCells': [3, 2]}, {'key': 'orchard_bench_3x1', 'footprintCells': [3, 1]}, {'key': 'seedling_rack_3x1', 'footprintCells': [3, 1]}, {'key': 'harvest_cart_2x2', 'footprintCells': [2, 2]}, {'key': 'chicken_coop_2x2', 'footprintCells': [2, 2]}, {'key': 'apiary_corner_2x2', 'footprintCells': [2, 2]}, {'key': 'picnic_arbor_3x2', 'footprintCells': [3, 2]}, {'key': 'produce_stall_3x2', 'footprintCells': [3, 2]}, {'key': 'garden_workbench_2x1', 'footprintCells': [2, 1]}], 'points': [{'key': 'milk_can_pair', 'footprintCells': [1, 1]}, {'key': 'tool_basket', 'footprintCells': [1, 1]}, {'key': 'ranch_notice_sign', 'footprintCells': [1, 1]}, {'key': 'watering_can', 'footprintCells': [1, 1]}, {'key': 'feed_sack_stack', 'footprintCells': [1, 1]}, {'key': 'flower_bucket', 'footprintCells': [1, 1]}, {'key': 'harvest_crate', 'footprintCells': [1, 1]}, {'key': 'honey_jar_stand', 'footprintCells': [1, 1]}]}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8'))

def overlap_stats(a,b):
 mask=lambda im,kind:im.getchannel('A').point((lambda n:255 if n else 0)if kind=='shared'else(lambda n:255 if n==255 else 0))
 shared=ImageChops.multiply(mask(a,'shared'),mask(b,'shared'))
 diff=ImageChops.lighter(ImageChops.lighter(ImageChops.difference(a.getchannel('R'),b.getchannel('R')),ImageChops.difference(a.getchannel('G'),b.getchannel('G'))),ImageChops.difference(a.getchannel('B'),b.getchannel('B')))
 equal=diff.point(lambda n:255 if n==0 else 0)
 safe=ImageChops.multiply(ImageChops.multiply(mask(a,'opaque'),mask(b,'opaque')),equal)
 unsafe=ImageChops.subtract(shared,safe)
 return shared.histogram()[255],unsafe.histogram()[255]

def check_meadow(ims,assets,ck):
 family='raised_flower_meadow';fs=[f'decor/connected/{family}/n{n:03d}.png'for n in range(256)]
 ck('Meadow RAW256 complete exact D3 paths',all(f in ims and f in assets for f in fs))
 flower=[f'decor/accents/meadow/flowers_{i:02d}.png'for i in range(1,5)]
 ck('Meadow exact4 optional flower overlays',all(f in ims and assets[f].get('kind')=='meadow_flower_accent'for f in flower))
 if not all(f in ims for f in fs):return {'family':family,'compatiblePairs':0,'status':'MISSING_RAW256'}
 ck('Meadow all256 raw masks genuinelydistinct',len({hashlib.sha256(ims[f].tobytes()).hexdigest()for f in fs})==256)
 borders={n:{'E':ims[f].crop((256,0,320,320)),'W':ims[f].crop((0,0,64,320)),'N':ims[f].crop((0,0,320,64)),'S':ims[f].crop((0,256,320,320))}for n,f in enumerate(fs)}
 pairs=shared=unsafe=portholes=0;examples=[]
 for axis in['E','N']:
  for a in range(256):
   for b in range(256):
    consistent=((a&4)and(b&64)and all(bool(a&x)==bool(b&y)for x,y in[(1,128),(2,1),(16,32),(8,16)]))if axis=='E'else((a&1)and(b&16)and all(bool(a&x)==bool(b&y)for x,y in[(64,32),(128,64),(4,8),(2,4)]))
    if not consistent:continue
    pairs+=1;s,u=overlap_stats(borders[a][axis],borders[b]['W'if axis=='E'else'S']);shared+=s;unsafe+=u
    # A no-overlap check must not pass through empty/gapped ports. This centre
    # 192px band excludes perimeter grass fringe and requires an opaque joined bed.
    pa=ims[fs[a]].crop((287,64,289,256)if axis=='E'else(64,31,256,33))
    pb=ims[fs[b]].crop((31,64,33,256)if axis=='E'else(64,287,256,289))
    joined=Image.alpha_composite(pa,pb);portholes+=joined.width*joined.height-joined.getchannel('A').histogram()[255]
    if u and len(examples)<8:examples.append({'axis':axis,'a':a,'b':b,'unsafeOverlapPixels':u})
 ck('Meadow2048 physicallyconsistent8neighbor pairs',pairs==2048)
 ck('Meadow2048 pairs safe paddedRGBA ownership',unsafe==0)
 ck('Meadow2048 joined interior ports opaque',portholes==0)
 ring={(x,y)for y in range(3)for x in range(3)if(x,y)!=(1,1)}
 bits=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]
 canvas=Image.new('RGBA',(832,832))
 for x,y in sorted(ring,key=lambda c:(c[1],c[0])):
  mask=sum(bit for dx,dy,bit in bits if(x+dx,y+dy)in ring);canvas.alpha_composite(ims[fs[mask]],(x*256,y*256))
 ck('Meadow ring hole192square remains clear',canvas.getchannel('A').crop((320,320,512,512)).getextrema()==(0,0))
 ds,du=overlap_stats(ims[fs[8]].crop((256,256,320,320)),ims[fs[128]].crop((0,0,64,64)))
 ck('Meadow isolated diagonal pair safe native tips',du==0)
 return {'family':family,'compatiblePairs':pairs,'unsafeOverlapPixels':unsafe,'sharedVisiblePixels':shared,'opaqueInteriorPortHoles':portholes,'examples':examples,'note':'Raw8-neighbor consistency constraints; duplicatevisible support is safe only equalRGB+alpha255. Partial fringe requires singleowner; no global fullstrip equality claim.'}

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--package',required=True);ap.add_argument('--expected-kinds',type=int,default=26);ap.add_argument('--report',help='Optional output JSON path; omitted writes no files.');args=ap.parse_args()
 P=Path(args.package).resolve(); checks=[];errors=[];warnings=[];footprints=[];joins=[];assets={};ims={}
 def ck(name,yes):
  checks.append({'check':name,'passed':bool(yes)})
  if not yes:errors.append(name)
 def warn(name):warnings.append(name)
 m=read(P/'manifest.json');catalog=read(P/'decoration_catalog.json');pin=sha(P/'manifest.json')
 ck('Exact Ranch V02 package identity',m.get('packageId')=='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02')
 ck('Manifest pin exact bytes',(P/'manifest_sha256.txt').read_text().strip()==pin)
 ck('Canonical neighbor bit schema',m.get('neighborBits')=={'N':1,'E':2,'S':4,'W':8})
 nets={a['key'] for a in catalog['networks']};groups={a['key']:a['footprintCells'] for a in catalog['groups']};points={a['key'] for a in catalog['points']}
 ck('Semantic categories separate/no duplicate keys',not(nets&set(groups) or nets&points or set(groups)&points)and len(nets)+len(groups)+len(points)==args.expected_kinds)
 ck('Catalog semantic count excludes mask/helper variants',catalog.get('familyCountExcludesMasksAndHelperPosts')==args.expected_kinds)
 ck('Root semantic count consistent',m.get('decorationFamilyCountExcludesMaskVariants')==args.expected_kinds)
 contract=APPROVED_DESIGNS
 ck('Exact approved semantic network designs',nets=={a['key']for a in contract['networks']})
 ck('Exact cardinal16/raw8_256 family metadata',{a['key']:a.get('connectivity')for a in catalog['networks']}=={a['key']:a.get('connectivity')for a in contract['networks']})
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
  alpha=im.getchannel('A');bb=alpha.getbbox();ck('Nonempty real transparency or fullysurroundedmeadow255 '+f,bb is not None and(alpha.getextrema()[0]==0 or(f=='decor/connected/raised_flower_meadow/n255.png'and alpha.getextrema()==(255,255))))
  ck('No alpha-derived collider '+f,a.get('deriveColliderFromAlpha')is False)
  for src in a.get('sourceFiles',[]):ck('Source PNG exists '+src,(P/src).is_file())
  ck('Native source references supplied '+f,bool(a.get('sourceFiles')))
  if a['family']in nets:
   ck('Network canonical register '+f,(im.size==(320,320) and lr==[32,32,256,256] and fp==[1,1]))
   kind=('meadow_flower_accent'if f.startswith('decor/accents/meadow/')else'periodic_post'if f.endswith('/periodic_post.png')else'connected_meadow'if a['family']=='raised_flower_meadow'else'connected_tile')
   ck('Exact hybrid network/overlay kind '+f,a['kind']==kind)
  if a['family']in groups or a['family']in points:
   expect=groups.get(a['family'],[1,1]);ck('Catalog group footprint '+f,fp==expect);ck('Exact whole/point kind '+f,a['kind']==('group'if a['family']in groups else'point'))
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
 ck('Group mapping includes every whole and point kind',len(m['groups'])==20 and set(g['key']for g in m['groups'])==set(groups)|points)
 for g in m['groups']:
  a=assets.get(g['file']);ck('Exact groupmapping art/footprint '+g['key'],a is not None and a['family']==g['key'] and g['footprintCells']==a['footprintCells'])
 ck('Exactly362 approved runtime assets',len(assets)==362)
 for family in sorted(nets):
  if family=='raised_flower_meadow':
   meadow=check_meadow(ims,assets,ck);joins.append(meadow);continue
  fs=[f'decor/connected/{family}/n{i:02d}.png'for i in range(16)]
  ck('Complete16 masks '+family,all(f in assets and f in ims for f in fs))
  if not all(f in ims for f in fs):continue
  ck('All16 masks distinct '+family,len({hashlib.sha256(ims[f].tobytes()).hexdigest()for f in fs})==16)
  strips={n:{'N':im.crop((0,32,320,33)),'E':im.crop((287,0,288,320)),'S':im.crop((0,287,320,288)),'W':im.crop((32,0,33,320))}for n,im in enumerate(ims[f]for f in fs)}
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
     aa=ims[fs[a]].crop((256,0,320,320)if side=='E'else(0,0,320,64));bb=ims[fs[b]].crop((0,0,64,320)if side=='E'else(0,256,320,320))
     for p,q in zip(aa.getdata(),bb.getdata()):
      if p[3]and q[3]:
       shared+=1
       if not(p[3]==q[3]==255 and p[:3]==q[:3]):badpixels+=1
  ck('Open ports reach boundary '+family,not missing);ck('Exact opposed RGBA profiles128pairs '+family,pairs==128 and not badports);ck('Safe padded overlap128pairs '+family,pairs==128 and badpixels==0)
  joins.append({'family':family,'compatiblePairs':pairs,'missingOpenPorts':missing,'differentProfiles':badports,'unsafeOverlapPixels':badpixels,'sharedVisiblePixels':shared})
 prior=read(O/'baseline_runtime_pixel_sha256.json')
 ck('Captured old298 pixel hashes present',len(prior['assets'])==298)
 for row in prior['assets']:
  f=row['file'];ck('Prior path preserved '+f,f in assets and f in ims)
  if f in ims:ck('Actual decoded RGBA replaced '+f,hashlib.sha256(ims[f].tobytes()).hexdigest()!=row['decodedRgbaSha256'])
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
 ck('Independent namespace no prior-theme identifier leftovers',not re.search(r'TheaterConnectedDecor|TheaterDecor|ECHO_THEATER|theaterDecor|SanctumConnectedDecor|SanctumDecor|SUNSET_SANCTUM|sanctumDecor|CoastConnectedDecor|CoastDecor|STAR_SWELL_COAST|coastDecor|RanchConnectedDecorV01',allcode))
 ck('No unresolved manifest placeholder','__RANCH_V02_MANIFEST_SHA256__'not in allcode)
 binding=runtime.get('RanchConnectedDecorV02Binding.cs','');tools=editor.get('RanchConnectedDecorV02BoardTools.cs','')
 for x in ['controller.Configure(','controller.SetLayout(','source.TryCaptureSnapshot(','.Changed += OnSourceChanged','.Changed -= OnSourceChanged']:
  ck('Binding concrete wiring '+x,x in binding)
 for x in ['root.AddComponent<RanchConnectedDecorV02Controller>()','root.AddComponent<RanchConnectedDecorV02AuthoredSnapshotSource>()','root.AddComponent<RanchConnectedDecorV02Binding>()','source.AssignBoard(board, out error)','Require(binding.RefreshNow(out error), error)','PrefabUtility.SaveAsPrefabAsset(']:
  ck('Editor concrete creation '+x,x in tools)
 ck('No fabricated Unity generated binary/text assets',not any(p.suffix in['.prefab','.unity','.asset','.meta']for p in P.rglob('*')if p.is_file()))
 ck('No gameplay physics or input creation',not re.search(r'AddComponent<(?:BoxCollider2D|Collider2D|Rigidbody2D|PlayerInput)>',allcode))
 ck('Actual status honest',m.get('actualUnityImport')=='NOT_RUN'and m.get('actualCSharpCompile')=='NOT_RUN'and m.get('actualUnityPlay')=='NOT_RUN')
 checksums=P/'package_checksums_sha256.txt';checksum_count=0
 if checksums.exists():
  for line in checksums.read_text().splitlines():
   dg,rel=line.split('  ',1);ck('Package checksum '+rel,(P/rel).is_file()and sha(P/rel)==dg);checksum_count+=1
 else:warn('Canonical package checksums not yet built')
 report={'status':'PASS_OFFLINE'if not errors else'FAIL_OFFLINE','scope':'Independent PNG bytes/alpha/ground-register/native-source/5cardinal+raw256meadow-overlap/old298decodedreplacement/approved-claim/concrete-source wiring audit; no producer imports, C# compile or Unity execution.','packageId':m['packageId'],'manifestSha256':pin,'runtimePngCount':len(assets),'semanticDesignCount':len(nets)+len(groups)+len(points),'checks':checks,'checkCount':len(checks),'errors':errors,'warnings':warnings,'footprints':footprints,'networkJoins':joins,'typedExamples':example_reports,'packageChecksumEntries':checksum_count,'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualTestRunner':'NOT_RUN','actualPrefabSceneGeneration':'NOT_RUN','actualUnityPlay':'NOT_RUN','actualProductionHostBindings':'NOT_RUN'}
 if args.report:Path(args.report).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({k:report[k]for k in['status','checkCount','runtimePngCount','semanticDesignCount','errors','warnings']},ensure_ascii=False))
if __name__=='__main__':main()
