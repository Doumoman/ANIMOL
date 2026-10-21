"""Read-only independent offline scene audit. Does not execute preview producer."""
from pathlib import Path
from PIL import Image
import json,hashlib,math
R=Path('/workspace/scratch/b0cb6eab55f7')
P=R/'output/ANIMOL_STAR_SWELL_COAST_CARTOON_V01'
S=R/'work/coast_v01/scene_preview_exports'
O=Path(__file__).resolve().parent
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
M=read(P/'manifest.json');A={a['file']:a for a in M['assets']}
C={c['key']:c for c in read(P/'animation_catalog.json')['clips']}
L=read(S/'review_scene_layout.json');G=read(S/'review_map_layout.json')
rows=G['rowsPositiveYDown'];land={(x,y)for y,row in enumerate(rows)for x,ch in enumerate(row)if ch in 'AWS'}
water={tuple(q)for q in G['waterCells']};errors=[];notes=[];positions=[]
if land&water:errors.append('land_water_overlap')
if len(rows)!=12 or any(len(r)!=9 for r in rows):errors.append('board_size')
bits=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]
for a in G['tiles']:
 x,y=a['column'],a['row'];mask=sum(b for dx,dy,b in bits if(x+dx,y+dy)in land)
 if a['neighborMask']!=mask:errors.append('mask '+str((x,y)))
 if a['worldCenter']!=[x-4,(960-(510+(y+.5)*80))/80]:errors.append('tile_world '+str((x,y)))
def xy_to_grid(x,y):return x+4,(960-y*80-510)/80-.5
def bbox_pixels(a,row):
 im=Image.open(P/a['file']);bb=im.getbbox()
 if bb is None:return None
 sx=row.get('scaleX',1);sy=row.get('scaleY',sx);scaleX=80/a['ppu']*sx;scaleY=80/a['ppu']*sy
 px=540+row['x']*80;py=960-row['y']*80
 left=px-a['pivot'][0]*a['width']*scaleX;top=py-(1-a['pivot'][1])*a['height']*scaleY
 return [round(left+bb[0]*scaleX,3),round(top+bb[1]*scaleY,3),round(left+bb[2]*scaleX,3),round(top+bb[3]*scaleY,3)]
def check_anchor(a,name):
 rect=a.get('groundFootprintRect') or a.get('logicalRect');foot=a.get('footprintCells')
 if not rect or not foot:return
 gx,gy=rect[0]+rect[2]/2,rect[1]+rect[3]/2
 px,py=a['pivot'][0]*a['width'],(1-a['pivot'][1])*a['height']
 if abs(gx-px)>0.001 or abs(gy-py)>0.001:errors.append('ground_pivot '+name+' '+a['file'])
declared_decor=set();used=set();shore_checks=[]
for row in L['sprites']:
 a=A[row['file']];used.add(row['file']);name=row['name'];f=row['file']
 if f.startswith(('devices/','props/','undersides/')):check_anchor(a,name)
 if f.startswith('props/'):
  declared_decor.add(f);x,y=xy_to_grid(row['x'],row['y']);fw,fh=a['footprintCells'];xx=x-(fw-1)/2;yy=y-(fh-1)/2
  occupied=[(round(xx+dx),round(yy+dy))for dx in range(fw)for dy in range(fh)]
  if abs(xx-round(xx))>.0001 or abs(yy-round(yy))>.0001:errors.append('decor_halfcell '+name)
  if any(q not in land for q in occupied):errors.append('decor_footprint_off_land '+name)
  positions.append({'name':name,'file':f,'declaredGroundCells':occupied,'nativeAlphaPixelBounds':bbox_pixels(a,row)})
 if f.startswith('fx/water/shore/'):
  x,y=xy_to_grid(row['x'],row['y']);q=(round(x),round(y));h=Path(f).stem
  dx,dy={'north':(0,-1),'east':(1,0),'south':(0,1),'west':(-1,0)}[h]
  ok=q in water and(q[0]+dx,q[1]+dy)in land
  shore_checks.append({'name':name,'waterCell':q,'landNormal':h,'validHostWaterAndLandNormal':ok})
  if not ok:errors.append('shore_host_water_or_normal '+name)
motionfamilies=set()
for row in L['motions']:
 for k in row['clipKeys']:
  c=C[k];motionfamilies.add(c['family'])
  for f in c['frames']:
   used.add(f);a=A[f]
   if f.startswith(('devices/','props/','animation/coast_palm/')):check_anchor(a,row['name'])
 if row['name']=='palm_west':declared_decor.add('props/coast/coast_palm.png')
 f=C[row['clipKeys'][0]]['frames'][0]
 if f.startswith(('devices/','animation/coast_palm/')):
  positions.append({'name':row['name'],'file':f,'grid':xy_to_grid(row['x'],row['y']),'nativeAlphaPixelBounds':bbox_pixels(A[f],row)})
for f in used:
 if sha(P/f)!=A[f]['sha256']:errors.append('source_sha '+f)
holes=set(map(tuple,G['enclosedWaterOrVoidCells']));supports=[]
for s in G['exposedSupportChunks']:
 cells=[(x,s['row'])for x in range(s['startColumn'],s['startColumn']+s['spanCells'])]
 if not 1<=s['spanCells']<=4:errors.append('span_size '+s['file'])
 if s['parentRunLength']>4 and s['spanCells']==1:errors.append('long_span_tail '+s['file'])
 if any(q not in land or(q[0],q[1]+1)in land for q in cells):errors.append('support_host '+s['file'])
 if any(((x,y+1)in holes)!=s['shallowHole']for x,y in cells):errors.append('hole_cap_wrong '+s['file'])
 im=Image.open(P/s['file']);bb=im.getbbox();deep=max(0,bb[3]-288)if bb else 0
 if s['shallowHole']and deep>20:errors.append('deep_hole_cap '+s['file'])
 supports.append(dict(s,belowGroundEdgePixels=deep))
sun=[s for s in L['sprites']if s['file'].endswith('/angular_setting_sun.png')]
stars=[s for s in L['sprites']if '/stars/'in s['file']];clouds=[s for s in L['sprites']if '/clouds/'in s['file']]
if len(sun)!=1:errors.append('sun_count')
if not stars or not clouds or max(s['sortingOrder']for s in stars)>=min(s['sortingOrder']for s in clouds):errors.append('stars_cloud_sort')
if any(abs(s['driftX'])+abs(s['driftY'])>0.001 for s in stars):errors.append('stars_drift')
if max(s['parallaxMultiplier']for s in stars)>=min(s['parallaxMultiplier']for s in clouds):errors.append('stars_cloud_parallax')
for f in ['COAST_FULL_SCENE_PREVIEW.png','COAST_GRID_FOOTPRINT_REVIEW.png']:
 im=Image.open(S/f);im.verify()
 if Image.open(S/f).size!=(1080,1920):errors.append('preview_size '+f)
gif=S/'COAST_FULL_SCENE_MOTION.gif';gifinfo=None
if gif.exists():
 im=Image.open(gif);duration=0;hashes=set()
 for i in range(im.n_frames):im.seek(i);duration+=im.info.get('duration',0);hashes.add(hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest())
 gifinfo={'frames':im.n_frames,'durationMs':duration,'distinctDecodedFrames':len(hashes)}
 if im.n_frames!=72 or duration!=6000 or len(hashes)<=30:errors.append('motion_gif')
if len(declared_decor)!=12:notes.append('Only '+str(len(declared_decor))+' of12 distinct decor shown')
required={'conveyor','oneway_gate','bounce_corner','brake_pad','leave_spike','steerable_ferry','rolling_barrel','three_turn_bomb'}
if not required<=motionfamilies:errors.append('missing_family '+str(sorted(required-motionfamilies)))
report={'status':'PASS_OFFLINE'if not errors else'FAIL','errors':errors,'notes':notes,
 'sourceManifestSha256':sha(P/'manifest.json'),'sourceLayoutSha256':sha(S/'review_scene_layout.json'),
 'gridCells':[9,12],'landCells':len(land),'waterCells':len(water),'decorTypes':len(declared_decor),'sourceAssetsChecked':len(used),
 'shoreChecks':shore_checks,'supportChecks':supports,'groundAnchorsAndActualAlphaBounds':positions,'gif':gifinfo,
 'actualUnityCapture':False,'actualUnityImport':'NOT_RUN','csharpCompile':'NOT_RUN','actualPlayMode':'NOT_RUN',
 'scope':'Independent offline file/math/metadata/alpha-bounds checks. Visual inspection is reported separately. No actual movement/collision/scene binding proof.'}
(O/'scene_independent_qa.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['supportChecks','groundAnchorsAndActualAlphaBounds']}))
