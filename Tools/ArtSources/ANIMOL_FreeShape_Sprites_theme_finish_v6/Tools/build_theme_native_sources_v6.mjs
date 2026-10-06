/** Native semantic material and junction registration. No palette guessing or resampling. */
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {getQuadrants,occupancyFromRows,resolveCell} from './terrain_topology.mjs';
import {resolveVariant} from './terrain_composition.mjs';
const require=createRequire(import.meta.url),sharp=require('sharp');
const ROOT=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const PROFILE_DIR=path.resolve(ROOT,process.argv[2]??'Data/native_v6');
const manifest=JSON.parse(await fs.readFile(path.join(ROOT,'Data/source_rects.json'),'utf8'));
const BASELINE=path.resolve(ROOT,'../ANIMOL_FreeShape_Sprites_hole_finish_v5');
const preservedPath=path.join(ROOT,'Data/native_v6_preserved_assets.json');
let baselineLookup,baselineAvailable=true;
try{baselineLookup=JSON.parse(await fs.readFile(path.join(BASELINE,'Data/sprite_lookup.json'),'utf8'));await fs.writeFile(preservedPath,JSON.stringify({baselineArtVersion:5,method:'Preserved v5 allowed palettes; original approved macro motifs are bundled as NativeV6/Motif128 and are never recolored by material updates.',styles:baselineLookup.styles.map(s=>({styleId:s.styleId,allowedPalette:s.allowedPalette,motif128:s.styleId==='T01_A'?'Art/T01_A/motif.png':'SourceArt/NativeV6/'+s.styleId+'/Motif128.png'}))},null,2)+'\n');}
catch(error){if(error.code!=='ENOENT')throw error;baselineAvailable=false;baselineLookup=JSON.parse(await fs.readFile(preservedPath,'utf8'));}
const palette=new Set(manifest.palette),INK='#1a1c2c';
const colors=new Map([...palette].map(c=>[c,Buffer.from([...c.slice(1).match(/../g).map(n=>parseInt(n,16)),255])]));
const blank=(w,h)=>({width:w,height:h,data:Buffer.alloc(w*h*4)}),idx=(im,x,y)=>(y*im.width+x)*4;
const mod=(a,b)=>(a%b+b)%b;
function put(im,x,y,color,toroidal=false){if(!Number.isInteger(x)||!Number.isInteger(y))throw Error('Native coordinates must be integers');if(toroidal){x=mod(x,im.width);y=mod(y,im.height);}if(x<0||y<0||x>=im.width||y>=im.height)return;if(color===null){im.data.fill(0,idx(im,x,y),idx(im,x,y)+4);return;}if(!colors.has(color))throw Error('Color outside exact palette: '+color);colors.get(color).copy(im.data,idx(im,x,y));}
function rectangle(im,op,color,toroidal=false){for(let y=op.y;y<op.y+op.h;y++)for(let x=op.x;x<op.x+op.w;x++)put(im,x,y,color,toroidal);}
function face(im,r,toroidal=true){const color=r.color??r.c;for(let y=0;y<r.h;y++)for(let x=0;x<r.w;x++){if((r.radius??r.cornerRadiusPixels??0)===1&&(x===0||x===r.w-1)&&(y===0||y===r.h-1))continue;const hi=r.highlightColor&&y===0&&x>=(r.highlightInset??r.highlightInsetPixels??2)&&x<r.w-(r.highlightInset??r.highlightInsetPixels??2);put(im,r.x+x,r.y+y,hi?r.highlightColor:r.shadowColor&&y===r.h-1?r.shadowColor:color,toroidal);}}
function polygon(im,op){const pts=op.points;if(!Array.isArray(pts)||pts.length<3||pts.some(p=>!Array.isArray(p)||p.length!==2||p.some(n=>!Number.isInteger(n))))throw Error('Invalid native polygon');const inside=(x,y)=>{let yes=false;for(let i=0,j=pts.length-1;i<pts.length;j=i++){const[a,b]=pts[i],[c,d]=pts[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;};const xs=pts.map(p=>p[0]),ys=pts.map(p=>p[1]),left=Math.min(...xs),right=Math.max(...xs),top=Math.min(...ys),bottom=Math.max(...ys);const periodicInside=(x,y)=>{for(let ky=Math.ceil((top-y)/64);ky<=Math.floor((bottom-y)/64);ky++)for(let kx=Math.ceil((left-x)/64);kx<=Math.floor((right-x)/64);kx++)if(inside(x+kx*64,y+ky*64))return true;return false;};for(let y=0;y<64;y++)for(let x=0;x<64;x++)if(periodicInside(x+.5,y+.5)){const boundary=op.outline&&[[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>!periodicInside(x+dx+.5,y+dy+.5));put(im,x,y,boundary?op.outline:op.color,true);}}
function normalized(profile){const b=profile.body??{},ops=b.ops??[...(b.faces??[]).map(o=>({kind:'face',...o})),...(b.lines??[]).map(o=>({kind:'rect',...o})),...(b.polygons??[]).map(o=>({kind:'polygon',...o}))];const t=profile.trim??{},e=profile.edges??t.edges??{};return {...profile,body:{...b,base:b.base??b.baseColor??INK,ops},edges:{N:e.N??t.top,S:e.S??t.bottom,W:e.W??t.side,E:e.E??t.side},outline:profile.outline??INK};}
function roleQuarter(profile,corner,state){const im=blank(16,16),west=corner.endsWith('W'),north=corner.startsWith('N'),h=profile.edges[north?'N':'S'],v=profile.edges[west?'W':'E'];const dx=x=>west?x:15-x,dy=y=>north?y:15-y;for(let y=0;y<16;y++)for(let x=0;x<16;x++){const a=dx(x),b=dy(y);let c=null;if(state==='TOP'||state==='BOTTOM')c=b<h.length?h[b]:null;else if(state==='SIDE')c=a<v.length?v[a]:null;else if(state==='OUTER'){if(a<v.length)c=v[a];if(b<h.length)c=h[b];if(a===0||b===0)c=profile.outline;}else if(state==='INNER'){
 // A missing diagonal joins two incoming trims at this vertex. An outer
 // union here would leave ink fins beyond the neighboring straight trim.
 if(a<v.length&&b<h.length){c=h[b];if(a===v.length-1)c=v[a];if(b===0)c=v[a];if(a===0)c=h[b];}
}if(c!==null)put(im,x,y,c);}
 // Only fully registered interior ornament faces are allowed; attachment
 // ports stay immutable. Coordinate positions are distances from the vertex.
 const ornaments=state==='OUTER'?profile.outerCorner?.ornamentFaces:state==='INNER'?profile.innerCorner?.ornamentFaces:null;
 for(const r of ornaments??[]){if(r.x<1||r.y<1||r.x+r.w>14||r.y+r.h>14)throw Error(profile.styleId+' corner ornament intersects immutable ports');if(state==='INNER'&&(r.x<2||r.y<2||r.x+r.w+1>v.length||r.y+r.h+1>h.length))throw Error(profile.styleId+' INNER ornament halo exceeds incoming trim intersection');const tmp=blank(16,16);rectangle(tmp,{x:r.x-1,y:r.y-1,w:r.w+2,h:r.h+2},profile.outline);face(tmp,r,false);for(let y=0;y<16;y++)for(let x=0;x<16;x++){if(!tmp.data[idx(tmp,x,y)+3])continue;const tx=west?x:15-x,ty=north?y:15-y;if(tx===0||tx===15||ty===0||ty===15)continue;tmp.data.copy(im.data,idx(im,tx,ty),idx(tmp,x,y),idx(tmp,x,y)+4);}}
 return im;}
function compose(core,overlay,corner,variant){const im=blank(16,16),sx=(variant&1)*32+(corner.endsWith('W')?0:16),sy=(variant>>1)*32+(corner.startsWith('N')?0:16);for(let y=0;y<16;y++)for(let x=0;x<16;x++){const op=idx(overlay,x,y),bp=idx(core,sx+x,sy+y);(overlay.data[op+3]?overlay.data:core.data).copy(im.data,idx(im,x,y),overlay.data[op+3]?op:bp,(overlay.data[op+3]?op:bp)+4);}return im;}
function blit(src,dst,dx,dy){for(let y=0;y<src.height;y++)for(let x=0;x<src.width;x++){const p=idx(src,x,y);if(src.data[p+3])src.data.copy(dst.data,idx(dst,dx+x,dy+y),p,p+4);}}
function reference(core,roles,rows){const grid=occupancyFromRows(rows),im=blank(128,128);for(const key of grid.occupied){const[x,y]=key.split(',').map(Number),r=resolveCell(grid.occupied,x,y),variant=resolveVariant(x,y,0);for(const q of r.quadrants)blit(compose(core,roles[q.moduleKey],q.corner,variant),im,x*32+q.column*16,(3-y)*32+q.row*16);}return im;}
async function save(im,rel){const file=path.join(ROOT,rel);await fs.mkdir(path.dirname(file),{recursive:true});await sharp(im.data,{raw:{width:im.width,height:im.height,channels:4}}).png({compressionLevel:9}).toFile(file);}
const documents=[];for(const file of (await fs.readdir(PROFILE_DIR)).filter(f=>f.endsWith('.json')).sort()){const doc=JSON.parse(await fs.readFile(path.join(PROFILE_DIR,file),'utf8'));documents.push(...(doc.styles??[doc]));}
const expectedStyles=manifest.themes.flatMap(t=>t.styles).map(s=>s.styleId).filter(id=>id!=='T01_A');
if(documents.length!==19||expectedStyles.some(id=>!documents.some(p=>p.styleId===id)))throw Error('Native v6 registration requires all 19 unlocked style profiles; partial theme exports are rejected.');
const seen=new Set(),registrations=[];
for(const raw of documents){const p=normalized(raw);if(p.styleId==='T01_A')throw Error('T01_A is locked to v5 native registration');if(seen.has(p.styleId))throw Error('Duplicate native style '+p.styleId);seen.add(p.styleId);const definition=manifest.themes.flatMap(t=>t.styles).find(s=>s.styleId===p.styleId);if(!definition)throw Error('Unknown style '+p.styleId);
 for(const[side,band]of Object.entries(p.edges)){if(!Array.isArray(band)||band.length<2||band.length>8||band.some(c=>!palette.has(c))||band[0]!==p.outline||band.at(-1)!==p.outline)throw Error(p.styleId+' '+side+' requires 2..8 exact colors, ink at first and last distances');}
 const core=blank(64,64);rectangle(core,{x:0,y:0,w:64,h:64},p.body.base);
 for(const o of p.body.ops.filter(o=>o.kind==='face'))rectangle(core,{x:o.x-1,y:o.y-1,w:o.w+2,h:o.h+2},o.outline??p.outline,true);
 for(const o of p.body.ops){if(o.kind==='face')face(core,o);else if(o.kind==='rect'||o.kind==='line')rectangle(core,o,o.color??o.c,true);else if(o.kind==='polygon')polygon(core,o);else throw Error('Unsupported body operation '+o.kind);}
 const rel='SourceArt/NativeV6/'+p.styleId,roles={},rolePaths={};
 for(const corner of ['NW','NE','SW','SE'])for(const state of ['IN',corner.startsWith('N')?'TOP':'BOTTOM','SIDE','OUTER','INNER']){const key=corner+'_'+state;roles[key]=roleQuarter(p,corner,state);rolePaths[key]=rel+'/roles/'+key+'.png';await save(roles[key],rolePaths[key]);}
 const material=blank(128,128);for(let y=0;y<128;y++)for(let x=0;x<128;x++)core.data.copy(material.data,idx(material,x,y),idx(core,x%64,y%64),idx(core,x%64,y%64)+4);
 const paths={material64:rel+'/Material64.png',material128:rel+'/Material128.png',frame128:rel+'/Frame128.png',ring128:rel+'/Ring128.png',motif128:rel+'/Motif128.png'};
 await save(core,paths.material64);await save(material,paths.material128);await save(reference(core,roles,['####','####','####','####']),paths.frame128);await save(reference(core,roles,['####','#..#','#..#','####']),paths.ring128);
 if(baselineAvailable)await fs.copyFile(path.join(BASELINE,'Art',p.styleId,'motif.png'),path.join(ROOT,paths.motif128));else await fs.access(path.join(ROOT,paths.motif128));
 Object.assign(definition,{junctionOverlayProfile:'native-quarter-roles-v6',nativeMaterialPath:paths.material64,nativeMaterialReferencePath:paths.material128,nativeFramePath:paths.frame128,nativeRingPath:paths.ring128,nativeMotifPath:paths.motif128,nativeQuarterRolePaths:rolePaths,nativeStyleProfile:'Data/native_v6/'+p.styleId.slice(0,3)+'.json',nativeInnerHolePixels:{x:32,y:32,w:64,h:64},nativeFrameContentBounds:{x:0,y:0,w:128,h:128},decorativeFramePixels:128,decorativeRingPixels:128,trimWidth:Math.max(...Object.values(p.edges).map(e=>e.length)),stabilizeHorizontalTrims:false,completeOuterCornerPatches:true,completeInnerCornerPatches:true});
 delete definition.nativeFrameDecorationPath;delete definition.nativeRingDecorationPath;delete definition.horizontalTrimPaletteBands;delete definition.registeredContourPalettes;
 const historicalStyle=baselineLookup.styles.find(s=>s.styleId===p.styleId);if(!historicalStyle)throw Error('Missing preserved style palette '+p.styleId);
 const nativeVisibleColors=new Set();for(const im of [core,...Object.values(roles)])for(let offset=0;offset<im.data.length;offset+=4)if(im.data[offset+3])nativeVisibleColors.add('#'+im.data.subarray(offset,offset+3).toString('hex'));
 const permittedColors=new Set([...(p.allowedPalette??[]),...historicalStyle.allowedPalette,...nativeVisibleColors]);definition.allowedPalette=[...palette].filter(c=>permittedColors.has(c));
 registrations.push({styleId:p.styleId,profile:definition.nativeStyleProfile,edges:p.edges,bodyOperationCount:p.body.ops.length,paths,quarterRolePaths:rolePaths,materialDescription:p.materialDescription??null,notes:p.notes??[]});
}
manifest.artVersion=6;await fs.writeFile(path.join(ROOT,'Data/source_rects.json'),JSON.stringify(manifest,null,2)+'\n');
await fs.writeFile(path.join(ROOT,'SourceArt/THEME_FINISH_NATIVE_REGISTRATION_V6.json'),JSON.stringify({artVersion:6,method:'Native 64px toroidal material plus independently registered transparent native 16px quarter role planes. Outgoing ports derive the exact directional band; colors never select roles.',lockedStyle:'T01_A v5',styles:registrations},null,2)+'\n');
console.log(JSON.stringify({artVersion:6,registeredStyles:registrations.length,styles:registrations.map(s=>s.styleId)},null,2));
