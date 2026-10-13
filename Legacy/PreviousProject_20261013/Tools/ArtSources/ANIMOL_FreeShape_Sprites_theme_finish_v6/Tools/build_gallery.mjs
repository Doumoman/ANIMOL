/** Create one fully offline HTML using actual generated atlases, not diagrams. */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const readJSON = file => JSON.parse(fs.readFileSync(path.join(root, file), 'utf8'));
const lookup = readJSON('Data/sprite_lookup.json'), topology = readJSON('Data/topology_catalog.json'), fixtures = readJSON('Data/logical_fixtures.json');
if (lookup.artVersion !== 6) throw new Error('Current gallery requires theme_finish_v6 artVersion 6.');
const nativeProfiles = readJSON('SourceArt/THEME_FINISH_NATIVE_REGISTRATION_V6.json');
const partial = process.argv.includes('--allow-partial');
if (!partial && lookup.styles.length !== 20) throw new Error('Final gallery requires all 20 styles. Use --allow-partial only for a smoke preview.');
const embed = file => {
  const resolved = path.resolve(root, file);
  if (!resolved.startsWith(`${root}${path.sep}`)) throw new Error(`Image outside package: ${file}`);
  return `data:image/png;base64,${fs.readFileSync(resolved).toString('base64')}`;
};
const styles = lookup.styles.map(style => {
  if (style.variants.length !== 4) throw new Error(`${style.styleId}: expected all four deterministic phases`);
  return {
    id: style.styleId, themeId: style.themeId, name: style.displayName,
    materialDescription: nativeProfiles.styles.find(s => s.styleId === style.styleId)?.materialDescription ?? '기존 옥판·흰 하이라이트·산호색 그림자와 둥근 모서리 유지',
    motif: embed(style.panels.motif),
    variants: style.variants.map(variant => {
      if (variant.cells.length !== 47) throw new Error(`${style.styleId}/${variant.id}: expected 47 sprites`);
      for (const [index, cell] of variant.cells.entries()) {
        if (cell.index !== index || cell.mask !== topology.canonicalMasks[index]) throw new Error(`${style.styleId}/${variant.id}: canonical order mismatch`);
      }
      return { id: variant.id, atlas: embed(variant.atlas), size: variant.atlasPixels, rects: variant.cells.map(c => c.rect) };
    }),
  };
});
const payload = { styles, themes: topology.themes, fixtures: fixtures.fixtures, palette: lookup.palette, canonicalMasks: topology.canonicalMasks, partial };
const safeJSON = JSON.stringify(payload).replace(/</gu, '\\u003c');
const browserSources = ['terrain_topology.mjs', 'terrain_composition.mjs', 'terrain_preview_model.mjs'].map(filename => {
  const source = fs.readFileSync(path.join(root, 'Tools', filename), 'utf8');
  return source.replace(/^import[\s\S]*?;\s*$/gmu, '').replace(/^export /gmu, '');
}).join('\n');

const html = `<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ANIMOL 자유 지형 아트 theme_finish_v6</title>
<style>
:root{color-scheme:dark;font-family:Inter,"Noto Sans KR","Malgun Gothic",system-ui,sans-serif;color:#d8e2ee;background:#101520}*{box-sizing:border-box}body{margin:0}button,input,select{font:inherit}button,select,input{color:#d8e2ee;background:#202a3b;border:1px solid #354258;border-radius:7px;padding:8px 10px}button{cursor:pointer}button:hover{border-color:#73eff7}button.active{border-color:#73eff7;background:#233e4b;color:#f4f4f4}button:disabled{opacity:.45;cursor:default}header{display:flex;justify-content:space-between;gap:20px;align-items:center;padding:18px 24px;border-bottom:1px solid #283348}h1{font-size:19px;margin:0 0 5px}p{margin:0}.muted{color:#94b0c2;font-size:12px;line-height:1.6}.layout{display:grid;grid-template-columns:230px minmax(0,1fr);gap:18px;padding:20px 24px}.sidebar{display:flex;flex-direction:column;gap:16px}.label{display:block;font-size:12px;color:#94b0c2;margin-bottom:6px}.wide{width:100%}.style-grid{display:grid;grid-template-columns:1fr 1fr;gap:7px}.style-card{padding:8px 5px;min-height:92px;font-size:11px;line-height:1.4}.style-card canvas{display:block;width:96px;max-width:100%;height:auto;margin:0 auto 6px;image-rendering:pixelated}.row{display:flex;align-items:center;gap:7px;flex-wrap:wrap}.row input{width:73px}.row select{max-width:100%}.toolbar{display:flex;justify-content:space-between;gap:14px;flex-wrap:wrap;margin-bottom:12px}.canvas-shell{border:1px solid #303c52;border-radius:10px;background:#151d2b;overflow:auto;max-height:76vh;padding:16px;min-height:350px}.canvas-shell canvas{display:block;image-rendering:pixelated;touch-action:none;cursor:crosshair;background:transparent;margin:auto}.status{margin-top:9px;font-size:12px;color:#94b0c2;min-height:20px}.controls-note{margin-top:10px}.check{font-size:12px;display:inline-flex;gap:6px;align-items:center}.check input{accent-color:#73eff7}details{border-top:1px solid #283348;margin:6px 24px 24px;padding-top:15px}summary{cursor:pointer;font-size:14px;color:#d8e2ee}.atlas-controls{margin:14px 0}.atlas-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(104px,1fr));gap:10px}.atlas-slot{background:#1a2333;border:1px solid #303c52;border-radius:8px;padding:8px;text-align:center;font-size:11px;color:#94b0c2}.atlas-slot canvas{display:block;margin:0 auto 8px;width:96px;height:96px;image-rendering:pixelated}.all-styles{display:grid;grid-template-columns:repeat(auto-fill,minmax(210px,1fr));gap:10px;margin-top:14px}.all-style{border:1px solid #303c52;border-radius:9px;background:#1a2333;padding:12px}.all-style canvas{display:block;width:100%;height:auto;image-rendering:pixelated;margin-bottom:8px}.all-style p{font-size:12px}footer{margin:0 24px 24px;max-width:1100px;font-size:12px;color:#94b0c2;line-height:1.7}#palette{display:flex;gap:3px;flex-wrap:wrap;max-width:290px}#palette span{display:block;width:12px;height:12px;border-radius:2px}@media(max-width:760px){header{padding:16px}.layout{grid-template-columns:1fr;padding:16px}.sidebar{display:grid;grid-template-columns:1fr 1fr}.canvas-shell{max-height:65vh;padding:8px}.sidebar .span{grid-column:1/-1}details,footer{margin-left:16px;margin-right:16px}.row input{width:65px}}
</style></head><body>
<header><div><h1>ANIMOL 자유 지형 아트 theme_finish_v6</h1><p class="muted">아트 버전 6 · 다섯 테마의 재료 무늬·경계 연결 정돈 · T01_A는 v5 유지 · 20스타일 전체 셀 확인</p></div><div id="palette" aria-label="Sweetie-16 팔레트"></div></header>
<main class="layout"><aside class="sidebar">
<div><label class="label" for="theme">지역</label><select id="theme" class="wide"></select></div>
<div><span class="label">디자인</span><div id="styles" class="style-grid"></div></div>
<div class="span"><label class="label" for="fixture">모양 예시</label><select id="fixture" class="wide"></select></div>
<div class="span"><span class="label">새 작업 영역 · 프리뷰 1~64셀</span><div class="row"><input id="width" type="number" min="1" max="64" value="16" aria-label="가로 셀 수"><span>×</span><input id="height" type="number" min="1" max="64" value="16" aria-label="세로 셀 수"><button id="new">새로</button></div></div>
<div class="span"><span class="label">전역 시작 좌표</span><div class="row"><input id="originX" type="number" step="1" value="0" aria-label="시작 x"><input id="originY" type="number" step="1" value="0" aria-label="시작 y"><button id="originApply">적용</button></div></div>
<div class="span"><span class="label">고정 시드</span><div class="row"><input id="seed" type="number" step="1" value="0" aria-label="아트 시드"><label class="check"><input id="motifs" type="checkbox" checked>큰 문양</label></div><p class="muted" style="margin-top:6px">문양은 4×4 점유와 사방 1셀 여유가 있을 때 배치돼요.</p></div>
<div class="span"><button id="exportPNG" class="wide">지형 PNG 저장</button><button id="exportMask" class="wide" style="margin-top:7px">점유 정보 JSON 저장</button></div>
</aside><section>
<div class="toolbar"><div class="row"><button id="paint" class="active">그리기</button><button id="erase">지우기</button><button id="undo" disabled>되돌리기</button><button id="clear">비우기</button></div><div class="row"><label class="check"><input id="grid" type="checkbox">격자</label><label class="check">배율 <select id="zoom"><option value="1">1×</option><option value="1.5" selected>1.5×</option><option value="2">2×</option></select></label></div></div>
<div class="canvas-shell"><canvas id="map" aria-label="자유 지형 편집 캔버스"></canvas></div>
<p id="status" class="status">아트 불러오는 중…</p><p class="muted controls-note">왼쪽 버튼으로 그리기 · 오른쪽 버튼으로 지우기 · 드래그 가능. PNG에는 격자와 커서가 들어가지 않아요.</p>
</section></main>
<details><summary>현재 디자인의 연결 스프라이트 47종</summary><div class="row atlas-controls"><label class="check">무늬 위상 <select id="atlasVariant"><option value="0">v0</option><option value="1">v1</option><option value="2">v2</option><option value="3">v3</option></select></label><span class="muted">47개 경계 유형 × 4개 고정 위상</span></div><div id="atlasSlots" class="atlas-grid"></div></details>
<details><summary>5개 지역 · 20개 디자인 전체 보기</summary><div id="allStyles" class="all-styles"></div></details>
<footer>이 파일은 theme_finish_v6 아트 버전 6의 실제 연결 셀을 확인하는 독립 프리뷰예요. T01_A는 v5 그림을 유지하고 나머지 19스타일은 각 테마의 재료 면과 명암, 외곽·내곽 연결을 정돈했어요. 맵 크기의 16셀 제한은 없으며, 이 화면의 새 작업 영역 입력은 64셀까지 제공해요. 점유 정보는 그림과 별도로 JSON에 저장할 수 있어요. Unity의 충돌은 이 점유를 기준으로 연결해야 하며, JSON은 프리뷰 형식이라 스테이지 데이터로 바로 적용되는 파일은 아니에요. 서로 다른 재료를 한 덩어리 안에서 연결하는 디자인은 별도 접합 규칙이 필요해요.</footer>
<script type="module">
${browserSources}
const DATA=${safeJSON};
const $=id=>document.getElementById(id), imageCache=new Map(), imageSets=new Map();
let grid=occupancyFromRows(DATA.fixtures.find(f=>f.id==='open_u_pit').rows,{x:0,y:0}), styleId=DATA.styles[0].id, mode='paint', hover=null, dragging=false, dragMode='paint', strokeStart=null, undoStack=[];
const artCanvas=document.createElement('canvas'), artContext=artCanvas.getContext('2d'), view=$('map'), viewContext=view.getContext('2d');
function imageFromURL(url){if(!imageCache.has(url))imageCache.set(url,new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=()=>reject(new Error('스프라이트 PNG를 읽지 못했어요.'));img.src=url;}));return imageCache.get(url);}
function selectedStyle(){return DATA.styles.find(s=>s.id===styleId);}
function seed(){const n=Number($('seed').value);return Number.isSafeInteger(n)?n:0;}
function snapshot(){return{rows:rowsFromGrid(grid),origin:{...grid.origin}};}
function sameSnapshot(a,b){return JSON.stringify(a)===JSON.stringify(b);}
function remember(old){if(!sameSnapshot(old,snapshot())){undoStack.push(old);if(undoStack.length>50)undoStack.shift();}$('undo').disabled=!undoStack.length;}
function restore(s){grid=occupancyFromRows(s.rows,s.origin);syncInputs();redraw();}
function syncInputs(){$('width').value=grid.width;$('height').value=grid.height;$('originX').value=grid.origin.x;$('originY').value=grid.origin.y;}
function drawPlan(target,plan,style){const ctx=target.getContext('2d');target.width=plan.width*32;target.height=plan.height*32;ctx.imageSmoothingEnabled=false;ctx.clearRect(0,0,target.width,target.height);const images=imageSets.get(style.id);for(const cell of plan.cells){const rect=style.variants[cell.variant].rects[cell.spriteIndex];ctx.drawImage(images.atlases[cell.variant],rect.x,rect.y,32,32,cell.column*32,cell.row*32,32,32);}for(const m of plan.motifs)ctx.drawImage(images.motif,m.column*32,m.row*32,m.width*32,m.height*32);}
function redraw(){const style=selectedStyle();if(!imageSets.has(style.id))return;const plan=createRenderPlan(grid,style.id,seed(),$('motifs').checked);drawPlan(artCanvas,plan,style);view.width=artCanvas.width;view.height=artCanvas.height;view.style.width=(view.width*Number($('zoom').value))+'px';view.style.height=(view.height*Number($('zoom').value))+'px';drawView();$('status').textContent=style.name+' · '+grid.width+'×'+grid.height+'셀 · 점유 '+grid.occupied.size+' · 문양 '+plan.motifs.length+(DATA.partial?' · 제작 중 프리뷰':'');}
function drawView(){viewContext.imageSmoothingEnabled=false;viewContext.clearRect(0,0,view.width,view.height);viewContext.drawImage(artCanvas,0,0);if($('grid').checked){viewContext.beginPath();viewContext.strokeStyle='rgba(148,176,194,.20)';viewContext.lineWidth=1;for(let x=0;x<=view.width;x+=32){viewContext.moveTo(x+.5,0);viewContext.lineTo(x+.5,view.height);}for(let y=0;y<=view.height;y+=32){viewContext.moveTo(0,y+.5);viewContext.lineTo(view.width,y+.5);}viewContext.stroke();}if(hover){viewContext.strokeStyle=(dragging?dragMode:mode)==='erase'?'#ef7d57':'#73eff7';viewContext.lineWidth=2;viewContext.strokeRect(hover.column*32+1,hover.row*32+1,30,30);}}
function point(event){const r=view.getBoundingClientRect();return{column:Math.floor((event.clientX-r.left)*view.width/r.width/32),row:Math.floor((event.clientY-r.top)*view.height/r.height/32)};}
function paintLine(from,to,brush){let x=from.column,y=from.row;const dx=Math.abs(to.column-x),sx=x<to.column?1:-1,dy=-Math.abs(to.row-y),sy=y<to.row?1:-1;let error=dx+dy,changed=false;while(true){changed=applyBrush(grid,x,y,brush)||changed;if(x===to.column&&y===to.row)break;const twice=error*2;if(twice>=dy){error+=dy;x+=sx;}if(twice<=dx){error+=dx;y+=sy;}}return changed;}
let previousPoint=null;
view.addEventListener('contextmenu',e=>e.preventDefault());
view.addEventListener('pointerdown',event=>{if(event.button!==0&&event.button!==2)return;event.preventDefault();view.setPointerCapture(event.pointerId);dragging=true;dragMode=event.button===2?'erase':mode;strokeStart=snapshot();previousPoint=point(event);hover=previousPoint;if(applyBrush(grid,hover.column,hover.row,dragMode))redraw();else drawView();});
view.addEventListener('pointermove',event=>{const p=point(event);hover=p;if(dragging){if(paintLine(previousPoint,p,dragMode))redraw();previousPoint=p;}else drawView();});
function endStroke(){if(dragging){dragging=false;remember(strokeStart);strokeStart=null;previousPoint=null;}}
view.addEventListener('pointerup',endStroke);view.addEventListener('pointercancel',endStroke);view.addEventListener('lostpointercapture',endStroke);view.addEventListener('pointerleave',()=>{if(!dragging){hover=null;drawView();}});
function setMode(next){mode=next;$('paint').classList.toggle('active',next==='paint');$('erase').classList.toggle('active',next==='erase');drawView();}
$('paint').onclick=()=>setMode('paint');$('erase').onclick=()=>setMode('erase');
$('undo').onclick=()=>{const old=undoStack.pop();if(old)restore(old);$('undo').disabled=!undoStack.length;};
$('clear').onclick=()=>{const old=snapshot();grid=makeEmptyGrid(grid.width,grid.height,grid.origin);remember(old);redraw();};
$('new').onclick=()=>{const width=Number($('width').value),height=Number($('height').value),x=Number($('originX').value),y=Number($('originY').value);if(!Number.isInteger(width)||!Number.isInteger(height)||width<1||height<1||width>64||height>64){$('status').textContent='프리뷰 크기는 1~64의 정수로 입력해주세요.';return;}if(!Number.isSafeInteger(x)||!Number.isSafeInteger(y)){$('status').textContent='시작 좌표는 정수로 입력해주세요.';return;}const old=snapshot();grid=makeEmptyGrid(width,height,{x,y});remember(old);syncInputs();redraw();};
$('originApply').onclick=()=>{const x=Number($('originX').value),y=Number($('originY').value);if(!Number.isSafeInteger(x)||!Number.isSafeInteger(y)){$('status').textContent='시작 좌표는 정수로 입력해주세요.';return;}const old=snapshot();grid=occupancyFromRows(rowsFromGrid(grid),{x,y});remember(old);syncInputs();redraw();};
$('fixture').onchange=()=>{const f=DATA.fixtures.find(f=>f.id===$('fixture').value),old=snapshot();grid=occupancyFromRows(f.rows,f.origin);remember(old);syncInputs();redraw();};
$('seed').oninput=redraw;$('motifs').onchange=redraw;$('grid').onchange=drawView;$('zoom').onchange=redraw;
function download(blob,name){const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);}
$('exportPNG').onclick=()=>artCanvas.toBlob(blob=>{if(blob)download(blob,styleId+'_terrain.png');},'image/png');
$('exportMask').onclick=()=>download(new Blob([JSON.stringify({schemaVersion:1,kind:'ANIMOL_FREE_SHAPE_PREVIEW_MASK',contractId:'ANIMOL_FREE_SHAPE_BLOB47_V1',styleId,seed:seed(),motifsEnabled:$('motifs').checked,cellUnits:1,rowsOrder:'north-first',origin:grid.origin,rows:rowsFromGrid(grid)},null,2)],{type:'application/json'}),styleId+'_occupancy.json');
const fixtureNames={single_1x1:'1×1',thin_vertical_1x16:'세로 1×16',thin_horizontal_16x1:'가로 16×1',rectangle_3x5:'직사각형 3×5',rectangle_5x3:'직사각형 5×3',maximum_rectangle_16x16:'큰 지형 16×16',asymmetric_stairs:'불규칙 계단',regular_stairs_ascending_right:'오른쪽 계단',regular_stairs_ascending_left:'왼쪽 계단',open_u_pit:'열린 구덩이',closed_hole:'둘러싸인 구멍',overhang:'돌출 지형',branch_t:'T자 분기',diagonal_separate:'대각선 접촉',multiple_components:'여러 덩어리',large_chunk_seam_34x6:'큰 지형 · 음수 좌표 34×6'};
function renderStyleButtons(){const theme=$('theme').value;$('styles').replaceChildren();for(const style of DATA.styles.filter(s=>s.themeId===theme)){const button=document.createElement('button');button.className='style-card'+(style.id===styleId?' active':'');button.title=style.name+' · '+style.materialDescription;const preview=document.createElement('canvas');const mini=occupancyFromRows(['####','####'],{x:0,y:0});drawPlan(preview,createRenderPlan(mini,style.id,0,false),style);const text=document.createElement('span');text.textContent=style.name;button.append(preview,text);button.onclick=()=>{styleId=style.id;renderStyleButtons();renderAtlasSlots();redraw();};$('styles').append(button);}}
$('theme').onchange=()=>{const first=DATA.styles.find(s=>s.themeId===$('theme').value);if(first){styleId=first.id;renderStyleButtons();renderAtlasSlots();redraw();}};
function renderAtlasSlots(){const style=selectedStyle(),variant=Number($('atlasVariant').value),atlas=imageSets.get(style.id)?.atlases[variant];if(!atlas)return;$('atlasSlots').replaceChildren();style.variants[variant].rects.forEach((rect,index)=>{const slot=document.createElement('div');slot.className='atlas-slot';const canvas=document.createElement('canvas');canvas.width=canvas.height=32;const ctx=canvas.getContext('2d');ctx.imageSmoothingEnabled=false;ctx.drawImage(atlas,rect.x,rect.y,32,32,0,0,32,32);const caption=document.createElement('span');caption.textContent='#'+index+' · mask '+DATA.canonicalMasks[index];slot.append(canvas,caption);$('atlasSlots').append(slot);});}
$('atlasVariant').onchange=renderAtlasSlots;
function renderAllStyles(){$('allStyles').replaceChildren();const f=DATA.fixtures.find(f=>f.id==='open_u_pit');for(const style of DATA.styles){const card=document.createElement('div');card.className='all-style';const canvas=document.createElement('canvas');drawPlan(canvas,createRenderPlan(occupancyFromRows(f.rows,f.origin),style.id,0,true),style);const label=document.createElement('p');label.textContent=style.id+' · '+style.name;const detail=document.createElement('p');detail.className='muted';detail.textContent=style.materialDescription;card.append(canvas,label,detail);$('allStyles').append(card);}}
for(const color of DATA.palette){const swatch=document.createElement('span');swatch.style.background=color;swatch.title=color;$('palette').append(swatch);}
for(const theme of DATA.themes){if(!DATA.styles.some(s=>s.themeId===theme.id))continue;const option=new Option(theme.name,theme.id);$('theme').add(option);}
for(const f of DATA.fixtures)$('fixture').add(new Option(fixtureNames[f.id]||f.id,f.id));$('fixture').value='open_u_pit';
try{await Promise.all(DATA.styles.map(async style=>{const atlases=await Promise.all(style.variants.map(v=>imageFromURL(v.atlas))),motif=await imageFromURL(style.motif);imageSets.set(style.id,{atlases,motif});}));syncInputs();renderStyleButtons();renderAtlasSlots();renderAllStyles();redraw();}catch(error){$('status').textContent=error.message;console.error(error);}
</script></body></html>`;
fs.writeFileSync(path.join(root, 'DESIGN_GALLERY.html'), html);
fs.writeFileSync(path.join(root, 'Docs/gallery_build.json'), `${JSON.stringify({ result: 'BUILT', artVersion: 6, nativeQuarterRoleStyles: nativeProfiles.styles.length, preservedV5Style: 'T01_A', offline: true, embeddedStyles: styles.length, embeddedAtlases: styles.length * 4, embeddedMotifs: styles.length, logicalFixtures: fixtures.fixtures.length, partial, browserTested: false }, null, 2)}\n`);
console.log(JSON.stringify({ gallery: 'DESIGN_GALLERY.html', bytes: Buffer.byteLength(html), styles: styles.length, atlasCount: styles.length * 4, offline: true, partial }));
