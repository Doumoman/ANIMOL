import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const sharp = createRequire(import.meta.url)('sharp');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const versions = [
  {label:'v1 · Original structure', dir:path.resolve(root,'../ANIMOL_FreeShape_Sprites_v1')},
  {label:'v2 · Flattened interior', dir:path.resolve(root,'../ANIMOL_FreeShape_Sprites_clean_v2')},
  {label:'v3 · Restored masonry and caps', dir:root},
];
const rows = [
  {id:'single_1x1',label:'1 × 1 cell / end shape',scale:5,height:225},
  {id:'asymmetric_stairs',label:'Stairs / visible brick field',scale:2,height:385},
  {id:'closed_hole',label:'Hole / ceiling and inner corners',scale:2,height:513},
];
const width=2100, column=700, top=86, height=top+rows.reduce((n,r)=>n+r.height,0);
const composites=[];
let labels=`<text x="28" y="32" fill="#ffcd75" font-size="23" font-weight="bold">ANIMOL · T01_A · Brick and endcap restoration</text>`;
for (let c=0;c<versions.length;c++) {
  labels+=`<text x="${c*column+28}" y="70" fill="#f4f4f4" font-size="19">${versions[c].label}</text>`;
  let y=top;
  for (const row of rows) {
    labels+=`<text x="${c*column+28}" y="${y+26}" fill="#94b0c2" font-size="16">${row.label}</text>`;
    const input=path.join(versions[c].dir,'Samples/T01_A',`${row.id}.png`);
    const meta=await sharp(input).metadata();
    const buffer=await sharp(input).resize(meta.width*row.scale,meta.height*row.scale,{kernel:'nearest'}).png().toBuffer();
    composites.push({input:buffer,left:c*column+Math.floor((column-meta.width*row.scale)/2),top:y+48});
    y+=row.height;
  }
}
composites.push({input:Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><g font-family="DejaVu Sans, sans-serif">${labels}</g></svg>`),left:0,top:0});
await fs.mkdir(path.join(root,'Previews'),{recursive:true});
await sharp({create:{width,height,channels:4,background:'#121521'}}).composite(composites).png().toFile(path.join(root,'Previews/T01_A_v1_v2_v3_comparison.png'));
console.log(JSON.stringify({preview:'Previews/T01_A_v1_v2_v3_comparison.png',width,height,method:'Runtime fixture PNGs assembled with integer nearest scales; labels only, no sprite pixel repainting.'}));
