import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import {createQAStage, QA_CONFIG} from '../Runtime/temporary_player.mjs';
import {qaEntity} from '../Runtime/qa_entity_pixels.mjs';
import {resolveVisual} from '../Runtime/obstacle_visual_resolver.mjs';
import { mechanism, THEMES, COMMON_NAMES, DIRECTIONS, POSES_BY_KIND, frameCount, FRAME_MS, FRAGMENTS } from '../Runtime/pixel_mechanisms.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const require=createRequire(import.meta.url);
let sharp;try{sharp=require('sharp');}catch{sharp=require(path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES||'', 'sharp'));}
const source=path.resolve(process.argv[2]||'');
if(!process.argv[2])throw new Error('Usage: node Tools/build_pack.mjs <extracted v4 package root>');
const lookup=JSON.parse(await fs.readFile(path.join(source,'Data/sprite_lookup.json'),'utf8'));
if(lookup.artVersion!==4||lookup.cellPixels!==32||lookup.contractId!=='ANIMOL_FREE_SHAPE_BLOB47_V1')throw new Error('v4 source contract mismatch');
for(const dir of ['Art/Mechanisms','Art/V4Shell','Data','Validation'])await fs.mkdir(path.join(root,dir),{recursive:true});
const sha=data=>crypto.createHash('sha256').update(data).digest('hex');
const assets={},preserved=[];
for(const style of lookup.styles){
 for(const variant of style.variants){
  const bytes=await fs.readFile(path.join(source,variant.atlas));
  const out=`Art/V4Shell/${path.basename(variant.atlas)}`;
  await fs.writeFile(path.join(root,out),bytes);
  preserved.push({source:variant.atlas,output:out,sha256:sha(bytes)});
  assets[`${style.styleId}/${variant.id}`]=`data:image/png;base64,${bytes.toString('base64')}`;
 }
}
const entries=[],sprites=[];
for(const theme of Object.keys(THEMES)){
 const kinds=Object.keys(COMMON_NAMES).filter(k=>k.startsWith('C')||(k==='M02'&&theme==='T01')||(k.startsWith('R')&&theme==='T05'));
 for(const kind of kinds)for(const facing of DIRECTIONS(kind))for(const pose of POSES_BY_KIND[kind])for(let frame=0;frame<frameCount(kind,pose);frame++){
  const {data,roles}=mechanism(theme,kind,facing,pose,frame);
  const file=`Art/Mechanisms/${theme}_${kind}_${facing}_${pose}_f${frame}.png`;
  const bytes=await sharp(data,{raw:{width:32,height:32,channels:4}}).png().toBuffer();
  await fs.writeFile(path.join(root,file),bytes);
  const entry={id:`${theme}/${kind}/${facing}/${pose}/f${frame}`,themeId:theme,kind,facing,pose,frame,frameDurationMs:FRAME_MS[kind]||0,file,width:32,height:32};
  entries.push(entry);sprites.push({entry,data,roles});
 }
}
await fs.mkdir(path.join(root,'Art/C05Fragments'),{recursive:true});
const fragmentEntries=[];
for(const theme of Object.keys(THEMES)){
 const pixels=mechanism(theme,'C05','UP','warn',0).data;
 for(const f of FRAGMENTS){const file=`Art/C05Fragments/${theme}_C05_piece${f.id}.png`,r=f.rect;
  await sharp(pixels,{raw:{width:32,height:32,channels:4}}).extract({left:r.x,top:r.y,width:r.width,height:r.height}).png().toFile(path.join(root,file));
  fragmentEntries.push({themeId:theme,id:f.id,file,sourceRect:r,pixelsPerUnit:32,pivot:{x:.5,y:.5}});
 }
}
await fs.writeFile(path.join(root,'Data/c05_fragment_catalog.json'),JSON.stringify({visualVersion:3,entries:fragmentEntries},null,2));
const cols=16,stride=36,rows=Math.ceil(entries.length/cols),atlas=new Uint8Array(cols*stride*rows*stride*4);
const registered=new Set(entries.map(e=>path.basename(e.file)));
for(const name of await fs.readdir(path.join(root,'Art/Mechanisms')))if(name.endsWith('.png')&&!registered.has(name))await fs.unlink(path.join(root,'Art/Mechanisms',name));
for(let n=0;n<sprites.length;n++){
 const sx=(n%cols)*stride+2,sy=Math.floor(n/cols)*stride+2;
 for(let yy=0;yy<32;yy++)for(let xx=0;xx<32;xx++)atlas.set(sprites[n].data.subarray(4*(yy*32+xx),4*(yy*32+xx)+4),4*((sy+yy)*cols*stride+sx+xx));
 entries[n].atlasRect={x:sx,y:sy,width:32,height:32};
}
await fs.writeFile(path.join(root,'Art/MechanismAtlas.png'),await sharp(atlas,{raw:{width:cols*stride,height:rows*stride,channels:4}}).png().toBuffer());
await fs.writeFile(path.join(root,'Data/mechanism_catalog.json'),JSON.stringify({schemaVersion:1,visualVersion:3,terrainArtVersion:4,cellPixels:32,pixelsPerUnit:32,atlas:'Art/MechanismAtlas.png',atlasWidth:cols*stride,atlasHeight:rows*stride,rectOrigin:'top-left',paddingPixels:2,poses:['idle','warn','active','inactive'],commonThemeSkins:45,regionalCandidates:['T01/M02','T05/R01','T05/R03'],retiredKinds:['C09'],entries},null,2));
await fs.writeFile(path.join(root,'Data/v4_sprite_lookup.json'),JSON.stringify(lookup,null,2));
await fs.writeFile(path.join(root,'Data/body_atlas_paths.json'),JSON.stringify({terrainArtVersion:4,sourceLookup:'Data/v4_sprite_lookup.json',paths:Object.fromEntries(preserved.map(e=>[e.source,e.output]))},null,2));
await fs.writeFile(path.join(root,'Data/palette.hex'),lookup.palette.map(x=>x.slice(1)).join('\n')+'\n');
await fs.writeFile(path.join(root,'Validation/v4_source_preservation.json'),JSON.stringify({atlasCount:preserved.length,method:'original PNG byte copy',entries:preserved},null,2));
await fs.writeFile(path.join(root,'Data/c05_fragment_effect.json'),JSON.stringify({visualVersion:3,sourcePose:'warn',sourceFacing:'UP',rectOrigin:'top-left',pixelsPerUnit:32,trigger:'authoritative C05 surfaceEnabled true -> false / final exit event',loadedCue:'active',durationMs:400,fadeStartMs:80,gravity:5,pixelSnap:true,rotationDegrees:0,colliders:false,fragments:FRAGMENTS},null,2));
await fs.writeFile(path.join(root,'Data/temporary_qa_stage.json'),JSON.stringify(createQAStage(),null,2));
await fs.writeFile(path.join(root,'Data/temporary_player_config.json'),JSON.stringify(QA_CONFIG,null,2));
// A typed, fixed QA fixture manifest for the isolated Unity editor scene builder.
// Its two art snapshots are selected by the same resolver used by the playable HTML.
const qaStage=createQAStage(),qaMap=new Map(qaStage.cells.map(c=>[`${c.x},${c.y}`,{...c,themeId:'T01',styleId:'T01_A',artVersion:4,surfaceEnabled:true,pose:c.kind==='C04'?'active':'idle'}]));
const qaRead=(x,y)=>qaMap.get(`${x},${y}`),qaStyle=lookup.styles.find(s=>s.styleId==='T01_A');
function qaPlanParts(c){const p=resolveVisual(qaRead,c.x,c.y,0),v=qaStyle.variants[p.variant];
 const part=index=>({file:`Art/V4Shell/${path.basename(v.atlas)}`,rect:v.cells[index].rect});
 let cap=null;
 if(!p.replaceCap&&(p.capOnly||p.capPatch)){const s=part(p.capIndex);cap={...s,rect:{...s.rect,height:8}};}
 return {body:p.drawBody?part(p.bodyIndex):null,cap};
}
const qaGraphics=qaStage.cells.map(c=>{
 const frames=entries.filter(e=>e.themeId==='T01'&&e.kind===c.kind&&e.facing===c.facing&&e.pose==='active');
 const idle=entries.find(e=>e.themeId==='T01'&&e.kind===c.kind&&e.facing===c.facing&&e.pose===(c.kind==='C04'?'active':'idle'));
 return {...c,initial:qaPlanParts(c),idleFile:idle?.file||'',activeFiles:frames.map(e=>e.file),loadedFile:c.kind==='C05'?'Art/Mechanisms/T01_C05_UP_active_f0.png':'',fragmentFiles:c.kind==='C05'?fragmentEntries.filter(f=>f.themeId==='T01').map(f=>f.file):[]};
});
for(const c of qaMap.values())if(c.kind==='C05'){c.surfaceEnabled=false;c.pose='inactive';}
for(const c of qaGraphics)c.collapsed=qaPlanParts(c);
await fs.writeFile(path.join(root,'Data/temporary_qa_graphics.json'),JSON.stringify({schemaVersion:1,styleId:'T01_A',seed:0,cells:qaGraphics},null,2));
await fs.mkdir(path.join(root,'Art/QAEntities'),{recursive:true});
const entityEntries=[];
for(const [kind,state,facing]of [['player','idle',1],['player','idle',-1],['bell','idle',1],['start','idle',1],['goal','locked',1],['goal','open',1]]){
 const q=qaEntity(kind,state,facing),file=`Art/QAEntities/${kind}_${state}_${facing}.png`;
 await sharp(q.data,{raw:{width:q.width,height:q.height,channels:4}}).png().toFile(path.join(root,file));entityEntries.push({kind,state,facing,file,width:q.width,height:q.height,pixelsPerUnit:32});
}
await fs.writeFile(path.join(root,'Data/qa_entity_catalog.json'),JSON.stringify({visualVersion:3,entries:entityEntries},null,2));
const embedded={lookup,mechanisms:{entries},assets};
for(const [kind,file]of [['resolver','Runtime/obstacle_visual_resolver.mjs'],['topology','Runtime/v4_terrain_topology.mjs'],['composition','Runtime/v4_terrain_composition.mjs'],['glyphs','Runtime/pixel_mechanisms.mjs'],['player','Runtime/temporary_player.mjs'],['entities','Runtime/qa_entity_pixels.mjs']]){
 const code=(await fs.readFile(path.join(root,file),'utf8')).replace(/^import .*;\n/gm,'').replace(/\bexport /g,'');
 embedded[kind]=code;
}
const template=await fs.readFile(path.join(root,'Tools/preview_template.html'),'utf8');
let html=template.replace('/*__TOPOLOGY__*/',embedded.topology).replace('/*__COMPOSITION__*/',embedded.composition).replace('/*__GLYPHS__*/',embedded.glyphs).replace('/*__RESOLVER__*/',embedded.resolver).replace('/*__PLAYER__*/',embedded.player).replace('/*__ENTITIES__*/',embedded.entities);
html=html.replace('__DATA__',JSON.stringify({lookup:embedded.lookup,mechanisms:embedded.mechanisms,assets:embedded.assets}));
await fs.writeFile(path.join(root,'ANIMOL_Obstacle_V4_Preview.html'),html);
console.log(JSON.stringify({shellAtlases:preserved.length,mechanismFrames:entries.length,commonThemeSkins:45,regionalCandidates:3,preview:'ANIMOL_Obstacle_V4_Preview.html'}));
