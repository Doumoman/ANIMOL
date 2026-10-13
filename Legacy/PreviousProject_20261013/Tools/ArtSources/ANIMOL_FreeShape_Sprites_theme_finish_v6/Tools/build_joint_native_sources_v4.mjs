/**
 * Native export registration of existing flat source faces.
 * Each destination pixel copies an approved RGBA source-face pixel.
 * No gradient, smoothing, outline morphology or generated-image resizing.
 * Native geometry is declared in Data/joint_native_layout_v4.json.
 */
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
const require=createRequire(import.meta.url),sharp=require('sharp');
const ROOT=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const layout=JSON.parse(await fs.readFile(path.join(ROOT,'Data/joint_native_layout_v4.json'),'utf8'));
const {data:source,info}=await sharp(path.join(ROOT,layout.sourceFaces)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const faces=new Map();
for(let p=0;p<source.length;p+=4) if(source[p+3]===255){const color='#'+source.subarray(p,p+3).toString('hex');if(layout.palette.includes(color)&&!faces.has(color))faces.set(color,Buffer.from(source.subarray(p,p+4)));}
for(const c of layout.palette)if(!faces.has(c))throw Error('Source face missing '+c);
const blank=(w,h)=>({width:w,height:h,data:Buffer.alloc(w*h*4)});
function pixel(img,x,y,c){if(x<0||y<0||x>=img.width||y>=img.height)return;faces.get(c).copy(img.data,(y*img.width+x)*4);}
function plane(img,x,y,w,h,c){for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)pixel(img,xx,yy,c);}
function face(img,x,y,w,h,c,shade=null,radius=0,highlight=null,inset=2){for(let yy=0;yy<h;yy++)for(let xx=0;xx<w;xx++){
 if(radius&&((xx===0||xx===w-1)&&(yy===0||yy===h-1)))continue;
 pixel(img,x+xx,y+yy,highlight&&yy===0&&xx>=inset&&xx<w-inset?highlight:shade&&yy===h-1?shade:c);
}}
const core=blank(64,64);
plane(core,0,0,64,64,layout.boundaryPalette);
for(const row of layout.courses)for(let j=0;j<row.joints.length;j++){
 const x=row.joints[j]+1,right=row.joints[j+1]??64;
 face(core,x,row.y+1,right-x,row.height-1,row.colors[j],row.shadeColors?.[j],row.cornerRadiusPixels??0,row.highlightColors?.[j],row.highlightInsetPixels??2);
}
const field=blank(128,128);
for(let y=0;y<128;y++)for(let x=0;x<128;x++)core.data.copy(field.data,(y*128+x)*4,((y%64)*64+x%64)*4,((y%64)*64+x%64)*4+4);
const frame=blank(128,128),b=layout.frameBounds;
for(let y=b.y;y<b.y+b.h;y++)for(let x=b.x;x<b.x+b.w;x++)field.data.copy(frame.data,(y*128+x)*4,(y*128+x)*4,(y*128+x)*4+4);
function overlayFaces(img,rects){for(const r of rects)plane(img,r.x-1,r.y-1,r.w+2,r.h+2,layout.boundaryPalette);for(const r of rects)face(img,r.x,r.y,r.w,r.h,r.c,r.shadeColor,r.cornerRadiusPixels??0,r.highlightColor,r.highlightInsetPixels??2);}
overlayFaces(frame,layout.frameFaces);
for(const x of layout.capJointColumns??[])for(const y of [5,6,7,8,9,10,117,118,119,120])pixel(frame,x,y,layout.boundaryPalette);
for(let y=b.y;y<b.y+b.h;y++){pixel(frame,b.x,y,layout.boundaryPalette);pixel(frame,b.x+b.w-1,y,layout.boundaryPalette);}
for(let x=b.x;x<b.x+b.w;x++){pixel(frame,x,b.y,layout.boundaryPalette);pixel(frame,x,b.y+b.h-1,layout.boundaryPalette);}
const ring={...frame,data:Buffer.from(frame.data)};
overlayFaces(ring,layout.ringFaces);
const hole=layout.hole;
for(let y=hole.y;y<hole.y+hole.h;y++)for(let x=hole.x;x<hole.x+hole.w;x++)ring.data.fill(0,(y*128+x)*4,(y*128+x)*4+4);
async function save(img,file){await sharp(img.data,{raw:{width:img.width,height:img.height,channels:4}}).png({compressionLevel:9}).toFile(path.join(ROOT,file));}
await save(core,'SourceArt/Revised/T01_A_BrickRepeat64.png');
await save(field,'SourceArt/Revised/T01_A_BrickRepeat128.png');
await save(frame,'SourceArt/Revised/T01_A_Frame_Joint128.png');
await save(ring,'SourceArt/Revised/T01_A_Ring_Joint128.png');
await fs.writeFile(path.join(ROOT,'SourceArt/JOINT_FINISH_NATIVE_REGISTRATION.json'),JSON.stringify({artVersion:4,profile:'Data/joint_native_layout_v4.json',sourceFaces:layout.sourceFaces,generatedReference:'SourceArt/Revised/T01_A_Joint_Rectangle_Reference.png',method:layout.method,frameArchitecture:'white jade cap; purple terminals and side beams; salmon elbow braces and bottom cap',sameSourceFlatColors:layout.palette,notUserApproval:true},null,2)+'\n');
console.log('Native source planes registered: repeat64, repeat128, frame128, ring128.');
