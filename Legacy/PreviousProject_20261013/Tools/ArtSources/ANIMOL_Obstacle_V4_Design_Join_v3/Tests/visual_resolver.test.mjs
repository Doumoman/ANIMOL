import test from 'node:test';
import assert from 'node:assert/strict';
import {resolveVisual,affectedArtCells,putVisual} from '../Runtime/obstacle_visual_resolver.mjs';
import { canonicalize, RAW_TO_INDEX, CANONICAL_MASKS, NEIGHBORS } from '../Runtime/v4_terrain_topology.mjs';
import { mechanism, COMMON_KINDS, THEMES, POSES_BY_KIND, frameCount, animationFrame, FRAGMENTS } from '../Runtime/pixel_mechanisms.mjs';
const cell=(kind='terrain',extra={})=>({kind,themeId:'T01',styleId:'T01_A',artVersion:4,pose:'idle',facing:'UP',...(kind==='C03'?{surfaceEnabled:true}:{}),...extra});
const read=map=>(x,y)=>map.get(`${x},${y}`);
test('all 256 neighbor contexts keep the original v4 masks for full devices; reader unchanged',()=>{
 for(const style of ['A','B','C','D'])for(const theme of ['T01','T02','T03','T04','T05'])for(let mask=0;mask<256;mask++){
  const mat={themeId:theme,styleId:`${theme}_${style}`},m=new Map([['0,0',cell('C08',{...mat,facing:'RIGHT'})]]);
  for(const n of NEIGHBORS)if(mask&n.bit)m.set(`${n.dx},${n.dy}`,cell('terrain',mat));
  const before=JSON.stringify([...m]); const p=resolveVisual(read(m),0,0,3);
  assert.equal(p.bodyMask,canonicalize(mask));assert.equal(p.bodyIndex,RAW_TO_INDEX[mask]);
  assert.equal(JSON.stringify([...m]),before);
 }
 assert.equal(CANONICAL_MASKS.length,47);
});
test('the adjoining terrain participates in the same full device art topology',()=>{
 const m=new Map([['0,0',cell()],['1,0',cell('C08',{facing:'RIGHT'})]]);
 assert.equal(resolveVisual(read(m),0,0).bodyMask,4);assert.equal(resolveVisual(read(m),1,0).bodyMask,64);
});
test('top-only devices join caps but leave the complete lower body absent',()=>{
 const m=new Map([['0,0',cell()],['1,0',cell('C01',{surfaceEnabled:true})]]);
 const a=resolveVisual(read(m),0,0),b=resolveVisual(read(m),1,0);
 assert.equal(a.bodyMask,0);assert.equal(a.capMask,4);assert.equal(a.capPatch,true);
 assert.equal(b.drawBody,false);assert.equal(b.capOnly,true);assert.equal(b.capMask,64);
});
test('inactive ledge restores the adjoining free block endcap',()=>{
 const m=new Map([['0,0',cell()],['1,0',cell('C05',{surfaceEnabled:false,pose:'inactive'})]]);
 assert.equal(resolveVisual(read(m),0,0).capMask,0);assert.equal(resolveVisual(read(m),1,0).capOnly,false);
});
test('air cells cannot join body or top, regardless of visible warning',()=>{
 for(const kind of ['C04','C07']){
  const m=new Map([['0,0',cell()],['1,0',cell(kind,{facing:'UP',pose:'active'})]]);
  const p=resolveVisual(read(m),1,0);assert.equal(p.drawBody,false);assert.equal(p.drawCap,false);
  assert.equal(resolveVisual(read(m),0,0).bodyMask,0);assert.equal(resolveVisual(read(m),0,0).capMask,0);
 }
});
test('different styles and versions keep their own architectural endcaps',()=>{
 for(const extra of [{styleId:'T01_B'},{artVersion:3},{joinGroup:'other'}]){
  const m=new Map([['0,0',cell()],['1,0',cell('terrain',extra)]]);
  assert.equal(resolveVisual(read(m),0,0).bodyMask,0);
 }
});
test('diagonal-only contact never hides the exposed corner',()=>{
 const m=new Map([['0,0',cell()],['1,1',cell('C03')]]);assert.equal(resolveVisual(read(m),0,0).bodyMask,0);
});
test('negative world coordinates and four seeds match the v4 phase expression',()=>{
 for(let x=-33;x<=33;x++)for(let y=-3;y<=3;y++)for(let seed=0;seed<4;seed++){
  const m=new Map([[`${x},${y}`,cell('C03')]]),p=resolveVisual(read(m),x,y,seed);
  const mod=n=>((n%2)+2)%2;assert.equal(p.variant,mod(x+(seed&1))+2*mod(-y+((seed>>1)&1)));
 }
});
test('invalid inputs, opaque overlap and unsupported art are explicit errors',()=>{
 const m=new Map();putVisual(m,0,0,cell());assert.throws(()=>putVisual(m,0,0,cell('C04')));
 assert.throws(()=>resolveVisual(()=>cell('C09'),0,0));assert.throws(()=>resolveVisual(()=>cell('C02',{facing:'UP'}),0,0));
 assert.throws(()=>resolveVisual(()=>cell('C03',{artVersion:3}),0,0));assert.throws(()=>resolveVisual(()=>cell('M02',{themeId:'T02',styleId:'T02_A',surfaceEnabled:true}),0,0));
});
test('blocked functional faces are detected without changing the map',()=>{
 const m=new Map([['0,0',cell('C02',{facing:'LEFT'})],['-1,0',cell()]]);
 assert.equal(resolveVisual(read(m),0,0).problems.length,1);
});
test('graphics invalidation includes local cells and every motif support anchor',()=>{
 const dirty=affectedArtCells(-1,16);assert.equal(dirty.cells.length,9);assert.equal(dirty.motifAnchors.length,36);
 assert.ok(dirty.cells.some(([x,y])=>x===0&&y===17));assert.ok(dirty.motifAnchors.some(([x,y])=>x===-5&&y===12));
});
test('regional candidates keep their own theme and one-cell graphics classification',()=>{
 const m=new Map([['0,0',cell('M02',{surfaceEnabled:true})]]);assert.equal(resolveVisual(read(m),0,0).drawBody,false);
 const r={themeId:'T05',styleId:'T05_A'};assert.equal(resolveVisual(()=>cell('R03',r),0,0).drawBody,true);
 assert.equal(resolveVisual(()=>cell('R01',{...r,surfaceEnabled:false,pose:'inactive'}),0,0).capOnly,false);
});
test('wind direction glyph tips visibly match right, left and up',()=>{
 const color=(data,x,y)=>Array.from(data.slice(4*(y*32+x),4*(y*32+x)+4));
 const accent=[239,125,87,255];
 assert.deepEqual(color(mechanism('T01','C07','RIGHT','idle').data,22,16),accent);
 assert.deepEqual(color(mechanism('T01','C07','LEFT','idle').data,17,16),accent);
 assert.deepEqual(color(mechanism('T01','C07','UP','idle').data,15,6),accent);
});
test('the revised catalog has nine common kinds, retired C09 and an always visible C04',()=>{
 assert.deepEqual(COMMON_KINDS,['C01','C02','C03','C04','C05','C06','C07','C08','C10']);
 assert.throws(()=>resolveVisual(()=>cell('C09',{surfaceEnabled:true,pose:'active'}),0,0));
 assert.throws(()=>mechanism('T01','C09','UP','active'));
 for(const pose of ['idle','warn','active','inactive']){
  const p=resolveVisual(()=>cell('C04',{pose}),0,0);assert.equal(p.pose,'active');assert.equal(p.overlay,'T01/C04/UP/active/f0');
 }
 assert.equal(resolveVisual(()=>cell('C04',{hazardEnabled:false}),0,0).problems[0],'retired blinking hazard state');
});
test('thin drop ledges have no sign and mochi has one fill color with a fixed base',()=>{
 for(const theme of Object.keys(THEMES)){
  assert.equal(mechanism(theme,'C01').data.some(v=>v!==0),false);
  for(let f=0;f<7;f++){
   const {data}=mechanism(theme,'C06','UP','active',f),colors=new Set(),ys=[];
   for(let y=0;y<32;y++)for(let x=0;x<32;x++){const c=Array.from(data.slice(4*(y*32+x),4*(y*32+x)+4));if(c[3]){colors.add(c.slice(0,3).join(','));ys.push(y);}}
   assert.equal(colors.size,2);assert.equal(Math.max(...ys),11);
   assert.equal(Math.min(...ys),[0,2,4,5,4,1,0][f]);
  }
 }
});
test('side spring faces mirror exactly, compress and restore; one shots clamp',()=>{
 for(let frame=0;frame<7;frame++){
  const a=mechanism('T01','C02','LEFT','active',frame).data,b=mechanism('T01','C02','RIGHT','active',frame).data;
  for(let y=0;y<32;y++)for(let x=0;x<32;x++)assert.deepEqual(a.slice(4*(y*32+x),4*(y*32+x)+4),b.slice(4*(y*32+31-x),4*(y*32+31-x)+4));
 }
 assert.deepEqual(mechanism('T01','C02','LEFT','active',0).data,mechanism('T01','C02','LEFT','active',6).data);
 assert.notDeepEqual(mechanism('T01','C02','LEFT','active',0).data,mechanism('T01','C02','LEFT','active',3).data);
 assert.equal(animationFrame('C06','active',999),6);assert.throws(()=>animationFrame('C06','active',-1));
});
test('conveyor tread pixels move with the requested direction and wrap every eight frames',()=>{
 const sample=(facing,frame,x)=>mechanism('T02','C08',facing,'idle',frame).data.slice(4*(32+x),4*(32+x)+4);
 for(const facing of ['LEFT','RIGHT'])for(let x=0;x<32;x++){
  const previous=(x+(facing==='LEFT'?1:-1)+32)%32;assert.deepEqual(sample(facing,1,x),sample(facing,0,previous));
 }
 assert.equal(animationFrame('C08','idle',640),0);assert.equal(animationFrame('C08','idle',80),1);
});
test('fractured cap replaces the intact cap and fragment rectangles cover the entire native surface',()=>{
 const p=resolveVisual(()=>cell('C05',{surfaceEnabled:true}),0,0);assert.equal(p.replaceCap,true);
 const rects=FRAGMENTS.map(f=>f.rect);assert.equal(rects.reduce((s,r)=>s+r.width*r.height,0),32*8);
 for(let x=0;x<32;x++)assert.equal(rects.filter(r=>x>=r.x&&x<r.x+r.width).length,1);
 for(const theme of Object.keys(THEMES))assert.equal(mechanism(theme,'C05','UP','inactive').data.some(v=>v!==0),false);
 assert.throws(()=>resolveVisual(()=>cell('C06',{surfaceEnabled:true,pose:'active',frame:7}),0,0));
 assert.throws(()=>resolveVisual(()=>cell('C08',{facing:'LEFT',frame:NaN}),0,0));
});

test('open side springs have no top/body, standing springs replace the cap and remain transparent around the coil',()=>{
 const side=resolveVisual(()=>cell('C02',{facing:'LEFT'}),0,0);
 assert.equal(side.drawBody,false);assert.equal(side.drawCap,false);
 const up=resolveVisual(()=>cell('C03'),0,0);assert.equal(up.drawBody,false);assert.equal(up.replaceCap,true);
 const sample=(kind,x,y)=>mechanism('T01',kind,kind==='C02'?'LEFT':'UP','idle').data[4*(y*32+x)+3];
 assert.equal(sample('C02',16,0),0);assert.equal(sample('C02',16,31),0);
 assert.equal(sample('C03',0,12),0);assert.equal(sample('C03',31,12),0);
});
