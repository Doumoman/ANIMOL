from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,shutil,os,math,zipfile,subprocess,sys,importlib.util,re

W=Path(__file__).resolve().parent;ROOT=W.parents[1]
ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'
B=ROOT/'work/ranch_devices_v13/reference'/ID/ID
BASE=ROOT/'work/ranch_devices_v13/reference/ANIMOL_STARWAY_RANCH_CARTOON_V12/ANIMOL_STARWAY_RANCH_CARTOON_V12'
P=ROOT/'output'/(ID+'.tmp')
FINAL=ROOT/'output'/ID
OUT=ROOT/'output'
FONT='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
BOLD='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(Path(p).read_text())
def text(p,v):
 p=Path(p);p.parent.mkdir(parents=True,exist_ok=True);t=p.with_name(p.name+'.tmp')
 with t.open('w',encoding='utf-8')as f:f.write(v);f.flush();os.fsync(f.fileno())
 os.replace(t,p)
def write(p,v):text(p,json.dumps(v,ensure_ascii=False,indent=2)+'\n')
def copy(a,b):
 b=Path(b);b.parent.mkdir(parents=True,exist_ok=True);t=b.with_name(b.name+'.tmp');shutil.copy2(a,t);os.replace(t,b)
def save(im,p):
 p=Path(p);p.parent.mkdir(parents=True,exist_ok=True);t=p.with_name(p.name+'.tmp')
 with t.open('wb')as f:im.save(f,format='PNG',optimize=True);f.flush();os.fsync(f.fileno())
 os.replace(t,p)
def bbox(im):return im.getchannel('A').point(lambda a:255 if a>=16 else 0).getbbox()
def font(n,bold=False):return ImageFont.truetype(BOLD if bold else FONT,n)

def init():
 if P.exists():shutil.rmtree(P)
 P.mkdir(parents=True)
 for name in ['Runtime','Editor','Tests~','Examples','decor']:
  shutil.copytree(B/name,P/name)
 copy(B/'decoration_catalog.json',P/'decoration_catalog.json')
 return read(B/'manifest.json')

def export_art(m):
 records=read(W/'generation_records.json');guides={r['key']:r for r in read(W/'guide_manifest.json')}
 assets={a['file']:a for a in m['assets']};changes=[]
 groups={g['key']:g for g in m['groups']}
 for r in records:
  key=r['key'];row=assets[groups[key]['file']];fw,fh=row['footprintCells'];T=256
  source=Path(r['native']);im=Image.open(source).convert('RGBA');im.load();b=bbox(im);assert b
  cut=im.crop(b);scale=(fw*T-32)/cut.width
  size=(round(cut.width*scale),round(cut.height*scale))
  e=max(guides[key]['extra'],math.ceil(max(0,size[1]-fh*T+8)/32)*32)
  canvas=Image.new('RGBA',(fw*T+64,fh*T+64+e))
  at=(round((canvas.width-size[0])/2),32+e+fh*T-12-size[1])
  assert at[0]>=16 and at[1]>=16,(key,size,at)
  canvas.alpha_composite(cut.resize(size,Image.Resampling.LANCZOS),at)
  native='sources/perspective_r2/'+key+'_native.png';copy(source,P/native)
  guide='Authoring~/perspective_r2/guides/'+key+'.png';copy(guides[key]['guide'],P/guide)
  old=sha(B/row['file']);save(canvas,P/row['file'])
  row.update(width=canvas.width,height=canvas.height,pivot=[.5,(32+fh*T/2)/canvas.height],logicalRect=[32,32+e,fw*T,fh*T],logical_rect=[32,32+e,fw*T,fh*T],sha256=sha(P/row['file']),sourceFiles=[native],sourceCrop=list(b),nativeRedraw=True,nativeRgbaRepainted=False,perspectiveRevision='R2',extraTopPx=e,registrationMethod='uniform native scale by world width; frontmost ground-contact baseline, no nonuniform warp',sourceUniformScale=scale,sourceRegisteredOrigin=list(at),groundAnchor='logical footprint centre',sortAnchor='front ground edge, supplied by existing host',noGameplayColliderOrCost=True)
  for legacy in ['componentFiles','sourceNative','source_file','sourceFile','groundOccupancyChecks','groundDepthRows','nativeCanvas','sourceByteSha256']:
   row.pop(legacy,None)
  previous=next(a for a in read(B/'manifest.json')['assets']if a['file']==row['file'])
  changes.append(dict(file=row['file'],key=key,method='native_imagegen_redraw_and_ground_registration',oldSha256=old,newSha256=row['sha256'],oldCanvas=[previous['width'],previous['height']],canvas=[canvas.width,canvas.height],footprint=[fw,fh],pivot=row['pivot'],logicalRect=row['logicalRect'],extraTopPx=e,source=native,sourceNativeSha256=sha(source)))
 # Single-cell props already use compatible frontal/top geometry. Re-anchor their support area.
 for g in read(B/'decoration_catalog.json')['points']:
  row=assets[groups[g['key']]['file']];im=Image.open(B/row['file']).convert('RGBA');b=bbox(im);assert b
  e=max(128,math.ceil(max(0,b[3]-192)/32)*32);canvas=Image.new('RGBA',(320,320+e))
  # Base contact is near the cell centre, rather than the far front edge of the reserved cell.
  target_front=32+e+128+32;dy=target_front-b[3]
  assert dy>=0 and dy+im.height<=canvas.height
  canvas.alpha_composite(im,(0,dy));old=sha(B/row['file']);save(canvas,P/row['file'])
  row.update(width=320,height=canvas.height,pivot=[.5,160/canvas.height],logicalRect=[32,32+e,256,256],logical_rect=[32,32+e,256,256],sha256=sha(P/row['file']),perspectiveRevision='R2',extraTopPx=e,nativeRedraw=False,registrationMethod='original native RGBA preserved; support baseline moved to cell-centre region',sourceRegistrationTranslationPx=[0,dy],noGameplayColliderOrCost=True)
  changes.append(dict(file=row['file'],key=g['key'],method='contact_anchor_and_canvas_only_original_art_preserved',oldSha256=old,newSha256=row['sha256'],canvas=[320,canvas.height],footprint=[1,1],pivot=row['pivot'],logicalRect=row['logicalRect'],extraTopPx=e,translationPx=[0,dy]))
 # Remove superseded producer-only sizing fields from changed records.
 currentFields={'file','width','height','ppu','pivot','logicalRect','logical_rect','footprintCells','sha256','kind','family','sourceFiles','sourceCrop','nativeRedraw','nativeRgbaRepainted','perspectiveRevision','extraTopPx','registrationMethod','sourceUniformScale','sourceRegisteredOrigin','groundAnchor','sortAnchor','noGameplayColliderOrCost','actualGameplayFootprintCells','deriveColliderFromAlpha','registerGameplayCellFromThisRecord','sourceRegistrationTranslationPx'}
 for changed in changes:
  row=assets[changed['file']]
  for key in list(row):
   if key not in currentFields:row.pop(key)
 # Keep only originals actually referenced by the current runtime manifest.
 needed={s for a in m['assets']for s in a['sourceFiles']}
 for source in sorted(needed):
  if not(P/source).exists():copy(B/source,P/source)
 native=[]
 for s in sorted(needed):
  im=Image.open(P/s);im.load();native.append(dict(file=s,width=im.width,height=im.height,sha256=sha(P/s),generatedBy='built-in imagegen',runtimeSelectable=False))
 write(P/'native_sources_manifest.json',dict(sources=native))
 write(P/'QA/perspective_changes.json',dict(checkedFamilies=26,redrawnMultiCellProps=12,reanchoredSingleCellProps=8,byteIdenticalConnectedAndAccentSprites=342,changes=changes))
 write(P/'Authoring~/perspective_r2/generation_prompts.json',dict(mode='built-in imagegen',assets=[dict(key=r['key'],nativeFile='sources/perspective_r2/'+r['key']+'_native.png',geometryGuide='Authoring~/perspective_r2/guides/'+r['key']+'.png',initialPrompt=r['prompt'],finalEditPrompt=r.get('postEditPrompt'))for r in records]))
 copy(W/'prepare.py',P/'Authoring~/perspective_r2/prepare_guides.py')
 copy(__file__,P/'Authoring~/perspective_r2/build_package.py')
 copy(ROOT/'output/ANIMOL_STARWAY_RANCH_DEVICES_V13_DECOR_PERSPECTIVE_OCCUPANCY_PROMPT_KO.txt',P/'DECOR_PERSPECTIVE_OCCUPANCY_PROMPT_R2_KO.txt')
 return changes

CLI='''ANIMOL 별길 목장 장식 V02 — 원근 보정 R2 적용 명령문

현재 Unity 프로젝트의 AGENTS.md와 기존 별길 목장 장식 바인딩을 읽고 아래 수정을 적용하라.
이 패키지는 장식 26종의 원근/접지를 보정한 installationRevision=2다.
다칸 장식 12종은 새 원화, 한 칸 소품 8종은 기존 원화의 접지점/캔버스 보정이다.
연결형 6종(입체 잔디 256상태와 꽃 변형 포함)은 기존 접합을 유지한다. 전체 실사용 PNG는 362개다.

1. 기존 ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02 폴더에 이 폴더의 내용을 반영하라.
기존 .meta와 GUID를 보존하고, 같은 C# 클래스가 중복되는 두 번째 패키지 폴더를 만들지 마라.
기존 배경/기본 지형/장치 V13/스토리/게임플레이 코드는 별도 자산이다.
패키지의 Runtime/Editor와 manifest.json/manifest_sha256.txt를 같은 개정본으로 함께 갱신하라.

2. PNG 전체를 중앙 Pivot으로 일괄 맞추지 마라.
기준 PPU 256, 논리 한 칸 256px이며, 추가 위쪽 여백은 높이를 위한 시각 공간이다.
manifest.json의 width/height/logicalRect/pivot을 그대로 임포트하라.
원본 가로/세로를 서로 다른 비율로 늘리지 말고, 지면 중심의 게임 좌표와 실제 점유를 유지하라.
높아진 캔버스가 점유 칸 수·Collider·Cost를 늘리면 안 된다.
새 그림은 밑동과 다리의 지면 위치를 기준으로 배치한다. PNG의 전체 높이로 월드 크기를 다시 계산하지 마라.

3. manifest.json을 선택한 뒤
Tools > ANIMOL > Ranch Connected Decor V02 > Build selected manifest.json
을 실행하라. 생성된 _Generated/RanchConnectedDecorV02Catalog.asset을 실제 연결에 사용하라.
새 아트 핀은 manifest_sha256.txt와 RanchConnectedDecorV02BoardSnapshot.ArtManifestSha256에 동일하게 반영되어 있다.
옛 카탈로그와 옛 sourceManifestSha256을 사용하는 BoardAsset/씬/프리팹 바인딩을 찾아 새 카탈로그로 갱신하라.
실제 맵의 장식 배치를 지우고 예시 JSON으로 바꾸지 마라. 기존 배치 데이터를 새 핀/카탈로그에 다시 연결하라.
Examples 안의 두 JSON은 검사 보드용이며 실제 캠페인 스테이지를 대신하지 않는다.

선택적 배치 실행 형식:
"<Unity Editor>" -batchmode -quit -projectPath "<project>" -executeMethod ANIMOL.Art.RanchConnectedDecorV02.RanchConnectedDecorV02CatalogBuilder.BuildFromCommandLine -ranchDecorManifest "Assets/<existing path>/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/manifest.json" -logFile "<log path>"

4. 기존 RanchConnectedDecorV02Binding/Controller의 맵 로드·편집·Undo·재사용 연결을 유지하라.
다칸 소품은 점유 칸마다 복제하지 말고 한 그룹의 SpriteRenderer로 표시한다.
수문/병아리/별양/나무상자 등 실제 장치 Visual과 Collider를 정적 장식으로 덮지 마라.
게임플레이 통행/물/점유 판정과 장식 배치 검사를 기존 구조에 따라 유지하라.
새 높이와 Pivot 때문에 씬 루트를 옮겨 맞추지 말고 manifest의 지면 중심이 같은 위치에 오게 하라.

5. 캐릭터 앞뒤 가림 검사를 실제 프로젝트의 Sorting 체계에서 수행하라.
정렬 기준은 지면의 전면 접지점이며 그림 중앙이나 지붕 꼭대기를 기준으로 바꾸지 마라.
큰 건물과 복합 소품은 기존 전경/후경 처리에 연결하고 필요한 분리 범위를 보고하라.
이 파일만으로 캐릭터 Sorting이 자동 적용되었다고 보고하지 마라.

6. 실제 검사:
- 실제 임포트된 Sprite 362개와 카탈로그 20개 그룹/한 칸 소품을 확인한다.
- 12개 다칸 소품의 canvas/Pivot/logicalRect가 manifest와 같은지 검사한다.
- 8개 한 칸 소품의 새 접지 위치와 1×1 점유를 확인한다.
- 헛간 3×2, 닭장 2×2, 수레 2×2, 정자·가판대 3×2, 작업대 2×1 크기를 실제 격자에 놓는다.
- 같은 상자·기둥·문·벤치의 크기 관계를 캐릭터와 함께 실제 플레이 배율에서 확인한다.
- 물체 앞/뒤/좌/우에서 캐릭터 가림, 길 침범, 떠 보임을 점검한다.
- 기존 연결형 장식의 직선/코너/T/십자/끝과 잔디 안쪽 구멍/대각선 마감을 확인한다.
- 맵 로드/되돌리기/풀 재사용 후 옛 그림·옛 Pivot이 남지 않는지 확인한다.
- 편집기 컴파일, 실제 검사 보드 생성, 실제 프로젝트 연결, PlayMode를 각각 실행 여부와 함께 보고한다.

이 제공 환경에는 원 Unity 프로젝트와 Unity Editor가 없다.
오프라인 파일·접지 메타데이터·압축 검수와 Unity 실행 결과를 구분하라.
제공본의 Unity Import/C# Compile/Host Binding/PlayMode는 NOT_RUN이다.
'''

def update_data(m,changes):
 old=(B/'manifest_sha256.txt').read_text().strip()
 m.update(installationRevision=2,perspectiveRevision='R2',revisionScope='12 native multi-cell redraws;8 support-anchor corrections;342 connected/accent images byte-preserved',actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualHostBindings='NOT_RUN',actualUnityPlay='NOT_RUN')
 write(P/'manifest.json',m);pin=sha(P/'manifest.json');text(P/'manifest_sha256.txt',pin+'\n')
 for folder in ['Runtime','Editor','Tests~','Examples']:
  for p in(P/folder).rglob('*'):
   if p.is_file()and p.suffix in ['.cs','.json','.txt']:
    value=p.read_text();value=value.replace(old,pin)
    if p.suffix=='.json':
     j=json.loads(value)
     if 'revision'in j:j['revision']=2
     value=json.dumps(j,ensure_ascii=False,indent=2)+'\n'
    text(p,value)
 write(P/'sprite_import_settings.json',dict(packageId=ID,installationRevision=2,onlyRuntimeManifestAllowlist=True,defaultTextureTypeForUndeclaredImages='Default',textureType='Sprite',spriteImportMode='Single',meshType='FullRect',filterMode='Bilinear',wrapMode='Clamp',compression='Uncompressed',mipMaps=False,npotScale='None',generateFallbackPhysicsShape=False,atlasAllowRotation=False,atlasTightPacking=False,sprites=[{k:a[k]for k in ['file','width','height','ppu','pivot','logicalRect','footprintCells','sha256']}for a in m['assets']]))
 write(P/'baseline_reference.json',dict(packageId=ID,sourceLibraryFileId='libfile_f05541bf975081919baf6ce6755d983f',sourceVersion=1,sourceZipSha256=sha(ROOT/'work/ranch_devices_v13/reference_download'/f'{ID}.zip'),sourceArtManifestSha256=old,baseRuntimePathsPreserved=362,decorationFamiliesPreserved=26,gameplayProjectAvailable=False))
 write(P/'delivery_status.json',dict(packageId=ID,installationRevision=2,perspectiveRevision='R2',currentArtManifestSha256=pin,redrawnMultiCell=12,reanchoredPoints=8,unchangedConnectedSprites=342,runtimePngCount=362,actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualNUnit='NOT_RUN',actualProductionHostAdapter='NOT_RUN',actualUnityPlay='NOT_RUN'))
 text(P/'UNITY_CLI_KO.txt',CLI)
 text(P/'README_KO.txt','''ANIMOL 별길 목장 장식 V02 — 원근 보정 R2 / installationRevision 2

장식 26종·실사용 PNG 362개를 유지한다.
다칸 소품 12종은 내장 이미지 생성 도구로 원화를 다시 제작했다.
우유통·농기구 바구니·안내판·물뿌리개·사료 포대·꽃 바구니·수확 상자·꿀병 받침대 8종은
기존 그림의 RGBA를 유지하면서 지면 접지점, 캔버스와 Pivot을 보정했다.
연결형 6종의 342개 연결/꽃 PNG는 기존 바이트를 유지하고 연결 검수를 다시 수행했다.

새 원화는 공용 바닥판과 두꺼운 받침대를 줄이고, 물체 사이로 실제 맵 지면이 보이게 한다.
추가 위쪽 여백은 높이 표현용이다. 실제 타일 점유는 기존 W×H를 유지한다.
새 크기와 Pivot은 manifest.json에 기록되어 있으며 기존 배치 루트와 지면 중심을 공유한다.
전체 그림 중앙으로 Pivot을 일괄 바꾸거나 캔버스 높이로 Collider를 만들지 않는다.

먼저 UNITY_CLI_KO.txt를 원 프로젝트의 Unity 에이전트에게 전달한다.
DECOR_PERSPECTIVE_OCCUPANCY_PROMPT_R2_KO.txt에 제작 기준을 보관했다.
Inspection의 비교도는 원 PNG를 동일 PPU로 조립한 오프라인 검수 이미지다. Unity 캡처가 아니다.
sources의 PNG는 최종 이미지 생성 원본이며, Authoring~에는 프롬프트/도형 가이드를 포함한다.
Runtime/Editor는 새 핀을 사용하며, 스프라이트 파일 경로와 그룹 키는 기존과 같다.

실제 Unity 임포트·C# 컴파일·원 프로젝트 연결·PlayMode는 미실행이다.
캐릭터와의 앞뒤 Sorting 및 실제 게임 배율 최종 확인은 원 프로젝트에서 수행해야 한다.
''')
 return pin

def blit(canvas,p,row,cx,cy,cell=80):
 im=Image.open(p).convert('RGBA');im.load();ratio=cell/row['ppu'];im=im.resize((round(im.width*ratio),round(im.height*ratio)),Image.Resampling.LANCZOS)
 canvas.alpha_composite(im,(round(cx-row['pivot'][0]*im.width),round(cy-(1-row['pivot'][1])*im.height)))

def previews(m,changes):
 cat=read(P/'decoration_catalog.json');assets={a['file']:a for a in m['assets']};groups={g['key']:g for g in m['groups']}
 entries=[(r,'network')for r in cat['networks']]+[(r,'group')for r in cat['groups']]+[(r,'point')for r in cat['points']]
 w,h=1320,9*340+110
 def catalog(grid):
  im=Image.new('RGBA',(w,h),'#172638');d=ImageDraw.Draw(im)
  d.text((20,18),'STARWAY RANCH / PERSPECTIVE R2',font=font(30,True),fill='#ffcd75')
  d.text((20,62),'26 families / same PPU scale / ground anchors / offline sprite composition',font=font(18),fill='#b6ced0')
  for i,(r,kind)in enumerate(entries):
   xx=(i%3)*440;yy=110+(i//3)*340
   d.text((xx+18,yy+10),r['nameEn'],font=font(17,True),fill='white')
   fw,fh=(2,2)if kind=='network'else r['footprintCells'];cell=80
   cx=xx+220;cy=yy+205
   x=cx-fw*cell/2;y=cy-fh*cell/2
   d.rectangle((x,y,x+fw*cell,y+fh*cell),fill='#5f9c6d')
   if grid:
    for n in range(fw+1):d.line((x+n*cell,y,x+n*cell,y+fh*cell),fill='#9bc697')
    for n in range(fh+1):d.line((x,y+n*cell,x+fw*cell,y+n*cell),fill='#9bc697')
   if kind=='network':
    fam=r['key'];tiles={(0,0),(1,0),(0,1)}
    for tx,ty in sorted(tiles,key=lambda a:-a[1]):
     bits=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
     if fam=='raised_flower_meadow':bits=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]
     mask=sum(b for dx,dy,b in bits if(tx+dx,ty+dy)in tiles)
     f=f'decor/connected/{fam}/n{mask:03d}.png'if fam=='raised_flower_meadow'else f'decor/connected/{fam}/n{mask:02d}.png'
     blit(im,P/f,assets[f],x+(tx+.5)*cell,y+(1.5-ty)*cell,cell)
    foot='CONNECTED / masks and seams retained'
   else:
    row=assets[groups[r['key']]['file']];blit(im,P/row['file'],row,cx,cy,cell)
    foot=f'{fw} x {fh} ground / E {row.get("extraTopPx",0)}px'
   if grid:
    d.line((cx-6,cy,cx+6,cy),fill='#f9d776',width=2);d.line((cx,cy-6,cx,cy+6),fill='#f9d776',width=2)
   d.text((xx+18,yy+310),foot,font=font(14),fill='#b6ced0')
  return im
 inspection=P/'Inspection';inspection.mkdir(exist_ok=True)
 save(catalog(False),inspection/'CATALOG_26_DESIGNS.png');save(catalog(True),inspection/'ALL26_BOARD_GRID.png')
 oldassets={a['file']:a for a in read(B/'manifest.json')['assets']}
 # Same PPU, same ground centre and cell size; no per-thumbnail fit.
 im=Image.new('RGBA',(1440,4*430+130),'#172638');d=ImageDraw.Draw(im)
 d.text((24,18),'BEFORE / PERSPECTIVE R2',font=font(30,True),fill='#ffcd75')
 d.text((24,62),'Same world scale and unchanged tile claims. Yellow crosses mark ground centres.',font=font(18),fill='#b6ced0')
 keys=['barnyard_3x2','chicken_coop_2x2','garden_workbench_2x1','harvest_cart_2x2']
 for i,key in enumerate(keys):
  f=groups[key]['file'];fw,fh=assets[f]['footprintCells'];cell=126;y=130+i*430
  for j,(root,row,label)in enumerate([(B,oldassets[f],'BEFORE'),(P,assets[f],'R2')]):
   cx=360+j*720;cy=y+270;x=cx-fw*cell/2;top=cy-fh*cell/2
   d.text((cx-260,y+12),key+' / '+label,font=font(18,True),fill='white')
   d.rectangle((x,top,x+fw*cell,top+fh*cell),fill='#5f9c6d')
   for n in range(fw+1):d.line((x+n*cell,top,x+n*cell,top+fh*cell),fill='#9bc697')
   for n in range(fh+1):d.line((x,top+n*cell,x+fw*cell,top+n*cell),fill='#9bc697')
   blit(im,root/f,row,cx,cy,cell)
   d.line((cx-6,cy,cx+6,cy),fill='#ffdf82',width=2);d.line((cx,cy-6,cx,cy+6),fill='#ffdf82',width=2)
 save(im,inspection/'PERSPECTIVE_BEFORE_AFTER.png')

def context(m):
 # Reuse the established native background composition, whose blitter already respects arbitrary pivots.
 source=(B/'Authoring/context/build_context_preview.py').read_text()
 source=source.replace("O=Path(__file__).resolve().parent\nW=O.parent",f"O=Path({str(P/'Inspection/context')!r});O.mkdir(parents=True,exist_ok=True)\nW=Path({str(W)!r})")
 source=source.replace("BASE=W/'reference/ANIMOL_STARWAY_RANCH_CARTOON_V12'",f"BASE=Path({str(BASE)!r})")
 source=source.replace("P=W/'package_staging'/PACKAGE",f"P=Path({str(P)!r})")
 source=source.replace("SNAP=W/'examples_exports/Examples/stage_snapshot.json'",f"SNAP=P/'Examples/stage_snapshot.json'")
 source=source.replace("sha(W/'reference_download/ANIMOL_STARWAY_RANCH_CARTOON_V12.zip')",f"sha(Path({str(ROOT/'work/ranch_devices_v13/reference_download/ANIMOL_STARWAY_RANCH_CARTOON_V12.zip')!r}))")
 source=source.replace('STARWAY RANCH / CONNECTED DECOR V02','STARWAY RANCH / PERSPECTIVE R2')
 source=source.replace("pending=p.with_suffix('.pending')","pending=p.with_name(p.name+'.tmp')")
 contextfile=W/'build_context_r2.py';text(contextfile,source);copy(P/'decoration_catalog.json',W/'catalog_design.json')
 subprocess.run([sys.executable,str(contextfile)],check=True)
 copy(contextfile,P/'Authoring~/perspective_r2/build_context_r2.py')

def validate(m,changes,pin):
 assets=m['assets'];assert len(assets)==362 and len(m['groups'])==20
 old={a['file']:a for a in read(B/'manifest.json')['assets']};changed=[];unchanged=[]
 for a in assets:
  p=P/a['file'];im=Image.open(p);im.verify();im=Image.open(p);im.load()
  assert im.mode=='RGBA'and im.size==(a['width'],a['height'])
  assert sha(p)==a['sha256'];r=a['logicalRect'];fw,fh=a['footprintCells'];px,py=a['pivot']
  assert r[2:]==[fw*256,fh*256]and r[0]>=0 and r[1]>=0 and r[0]+r[2]<=im.width and r[1]+r[3]<=im.height
  assert abs(px*im.width-(r[0]+r[2]/2))<.001 and abs((1-py)*im.height-(r[1]+r[3]/2))<.001
  assert old[a['file']]['footprintCells']==a['footprintCells']
  assert a['noGameplayColliderOrCost']and not a['deriveColliderFromAlpha']
  (changed if sha(p)!=sha(B/a['file'])else unchanged).append(a['file'])
 assert len(changed)==20 and len(unchanged)==342
 assert len(list((P/'decor').rglob('*.png')))==362
 for s in read(P/'native_sources_manifest.json')['sources']:
  assert sha(P/s['file'])==s['sha256'];im=Image.open(P/s['file']);im.verify()
 assert pin==sha(P/'manifest.json')and pin in(P/'Runtime/RanchConnectedDecorV02BoardSnapshot.cs').read_text()
 for name in ['all26_snapshot.json','stage_snapshot.json']:assert read(P/'Examples'/name)['sourceManifestSha256']==pin
 # Every opposite-direction connected pair is byte-identical to the validated baseline.
 # This establishes seam preservation, not a new engine-rendered seam guarantee.
 paircases=0
 for fam in read(P/'decoration_catalog.json')['networks']:
  if fam['key']=='raised_flower_meadow':continue
  for bit,opp in [(1,4),(2,8),(4,1),(8,2)]:
   for a in range(16):
    for b in range(16):
     if a&bit and b&opp:
      for mask in [a,b]:
       f=f'decor/connected/{fam["key"]}/n{mask:02d}.png';assert sha(P/f)==sha(B/f)
      paircases+=1
 report=dict(status='PASS_OFFLINE_FILES_AND_GROUND_METADATA',packageId=ID,installationRevision=2,perspectiveRevision='R2',runtimePngs=362,semanticFamilies=26,redrawnMultiCellProps=12,reanchoredPointProps=8,bytePreservedConnectedAndAccentPngs=342,allPriorPathsAndFootprintsPreserved=True,allCanvasLogicalRectPivotEquationsPass=True,connectedOppositePairCasesPreserved=paircases,meadowRaw256VariantsPreserved=True,nativeRedrawMethod='built-in imagegen, 12 selected native assets; shared uniform registration; no nonuniform image warp',visualReview='before/after and same-PPU boards inspected offline',engineSorting='Requires host character/decor sorting validation in actual project',actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualNUnit='NOT_RUN',actualHostBindings='NOT_RUN',actualUnityPlay='NOT_RUN',changedRuntimeFiles=changed)
 write(P/'QA/DELIVERY_QA.json',report);return report

def package(report):
 files=sorted(f for f in P.rglob('*')if f.is_file()and f.name!='package_checksums_sha256.txt')
 text(P/'package_checksums_sha256.txt','\n'.join(sha(f)+'  '+f.relative_to(P).as_posix()for f in files)+'\n')
 ztmp=OUT/(ID+'.zip.tmp')
 with zipfile.ZipFile(ztmp,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9)as z:
  for f in sorted(P.rglob('*')):
   if f.is_file():z.write(f,ID+'/'+f.relative_to(P).as_posix())
 with zipfile.ZipFile(ztmp)as z:
  assert z.testzip()is None
  extraction=W/'extract_check.tmp'
  if extraction.exists():shutil.rmtree(extraction)
  z.extractall(extraction)
  for name in z.namelist():assert sha(extraction/name)==sha(P/Path(name).relative_to(ID))
  count=len(z.namelist());shutil.rmtree(extraction)
 if FINAL.exists():shutil.rmtree(FINAL)
 os.replace(P,FINAL);target=OUT/(ID+'.zip');os.replace(ztmp,target)
 copies={
  '_CLI_KO.txt':'UNITY_CLI_KO.txt',
  '_CATALOG_26_DESIGNS.png':'Inspection/CATALOG_26_DESIGNS.png',
  '_ALL26_BOARD_GRID.png':'Inspection/ALL26_BOARD_GRID.png',
  '_ALL26_BOARD.png':'Inspection/CATALOG_26_DESIGNS.png',
  '_STAGE_BOARD.png':'Inspection/context/RANCH_SCENE_CONTEXT.png',
  '_RANCH_SCENE_CONTEXT.png':'Inspection/context/RANCH_SCENE_CONTEXT.png',
  '_DELIVERY_QA.json':'QA/DELIVERY_QA.json',
  '_PERSPECTIVE_BEFORE_AFTER.png':'Inspection/PERSPECTIVE_BEFORE_AFTER.png'}
 for suffix,source in copies.items():copy(FINAL/source,OUT/(ID+suffix))
 text(OUT/(ID+'_DOWNLOAD_CHECKSUMS.txt'),sha(target)+'  '+target.name+'\n'+f'Archive entries: {count}\nArchive CRC and extracted SHA-256: PASS\nPerspective revision: R2 / installationRevision 2\n')
 receipt=dict(zip=str(target),zipBytes=target.stat().st_size,zipSha256=sha(target),archiveEntries=count,checks='CRC plus actual extraction and every file SHA-256 pass',runtimePngs=362,redrawnGroups=12,reanchoredPoints=8,unchangedConnectedAndAccents=342)
 write(W/'final_receipt.json',receipt);print(json.dumps(receipt,indent=2))

if __name__=='__main__':
 m=init();changes=export_art(m);pin=update_data(m,changes);previews(m,changes);context(m);report=validate(m,changes,pin);package(report)
