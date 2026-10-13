import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const require=createRequire(import.meta.url);
const sharp=require('sharp');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sources=JSON.parse(await fs.readFile(path.join(root,'Data/source_paths.json'),'utf8'));
let previous={};
try{previous=JSON.parse(await fs.readFile(path.join(root,'Data/source_rects.json'),'utf8'));}catch(error){if(error.code!=='ENOENT')throw error;}
function bands(values, threshold){
  const out=[]; let start=-1;
  for(let i=0;i<=values.length;i++){
    if(i<values.length && values[i]>threshold){if(start<0)start=i;}
    else if(start>=0){if(i-start>80)out.push([start,i]);start=-1;}
  }
  return out;
}
function trimRect(data,width,bounds){
  let x0=width,y0=Infinity,x1=-1,y1=-1;
  for(let y=bounds.y;y<bounds.y+bounds.h;y++)for(let x=bounds.x;x<bounds.x+bounds.w;x++){
    if(data[(y*width+x)*4+3]<217)continue;
    x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);
  }
  if(x1<0)throw new Error('Empty artwork panel');
  return {x:x0,y:y0,w:x1-x0+1,h:y1-y0+1};
}
function holeRect(data,width,rect){
  const seen=new Set();let best=[];
  for(let y=rect.y+1;y<rect.y+rect.h-1;y++)for(let x=rect.x+1;x<rect.x+rect.w-1;x++){
    const key=y*width+x;if(seen.has(key)||data[key*4+3]>=217)continue;
    const queue=[[x,y]];seen.add(key);let exterior=false;
    for(let i=0;i<queue.length;i++){
      const [xx,yy]=queue[i];
      if(xx===rect.x||yy===rect.y||xx===rect.x+rect.w-1||yy===rect.y+rect.h-1)exterior=true;
      for(const [nx,ny] of [[xx-1,yy],[xx+1,yy],[xx,yy-1],[xx,yy+1]]){
        if(nx<rect.x||ny<rect.y||nx>=rect.x+rect.w||ny>=rect.y+rect.h)continue;
        const nk=ny*width+nx;
        if(!seen.has(nk)&&data[nk*4+3]<217){seen.add(nk);queue.push([nx,ny]);}
      }
    }
    if(!exterior && queue.length>best.length)best=queue;
  }
  if(best.length<200)throw new Error('No enclosed hole in ring panel');
  const xs=best.map(p=>p[0]),ys=best.map(p=>p[1]);
  return {x:Math.min(...xs),y:Math.min(...ys),w:Math.max(...xs)-Math.min(...xs)+1,h:Math.max(...ys)-Math.min(...ys)+1};
}
const themes=[];
for(const [themeId,sourcePath] of Object.entries(sources)){
  const previousTheme=previous.themes?.find(t=>t.themeId===themeId)??{};
  const inputPath=path.isAbsolute(sourcePath)?sourcePath:path.resolve(root,sourcePath);
  const {data,info}=await sharp(inputPath).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const rows=Array(info.height).fill(0),cols=Array(info.width).fill(0);
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=217){rows[y]++;cols[x]++;}
  const rb=bands(rows,info.width*.2),cb=bands(cols,info.height*.15);
  if(rb.length!==4||cb.length!==4)throw new Error(`${themeId}: expected four artwork rows/columns, got ${rb.length}/${cb.length}`);
  const xCuts=[0,...cb.slice(0,-1).map((c,i)=>Math.floor((c[1]+cb[i+1][0])/2)),info.width];
  const yCuts=[0,...rb.slice(0,-1).map((r,i)=>Math.floor((r[1]+rb[i+1][0])/2)),info.height];
  const styles=[];
  for(let row=0;row<4;row++){
    const styleId=`${themeId}_${'ABCD'[row]}`;
    const previousStyle=previousTheme.styles?.find(s=>s.styleId===styleId)??{};
    // Revised single-style sources have their own manually measured panels.
    // Do not replace those coordinates with crops from the base theme sheet.
    if(previousStyle.sourcePath){styles.push(previousStyle);continue;}
    const rects={};
    for(let col=0;col<4;col++)rects[['material','frame','ring','motif'][col]]=trimRect(data,info.width,{x:xCuts[col],y:yCuts[row],w:xCuts[col+1]-xCuts[col],h:yCuts[row+1]-yCuts[row]});
    rects.innerHole=holeRect(data,info.width,rects.ring);
    styles.push({...previousStyle,styleId,...rects});
  }
  themes.push({...previousTheme,themeId,sourcePath,width:info.width,height:info.height,styles});
}
const out={...previous,schemaVersion:1,alphaThreshold:217,note:'Source rectangles locate actual generated art, not assumed guide positions. innerHole uses original-image coordinates.',themes};
await fs.writeFile(path.join(root,'Data/source_rects.json'),JSON.stringify(out,null,2)+'\n');
console.log(`Located ${themes.length*4} style rows and ${themes.length*16} actual artwork panels.`);
