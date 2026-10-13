import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const require=createRequire(import.meta.url),sharp=require('sharp');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const json=async p=>JSON.parse(await fs.readFile(path.join(root,p),'utf8'));
const lookup=await json('Data/sprite_lookup.json');
const fixtures=(await json('Data/logical_fixtures.json')).fixtures;
const old=await json('Docs/old_v3_preserved_sha256.json');
const palette=new Set(lookup.palette.map(c=>parseInt(c.slice(1),16)));
const result={status:'PASS',styles:lookup.styles.length,cellPngs:0,quarterPngs:0,atlases:0,panelPngs:0,samples:0,pixelsChecked:0,oldFilesPreserved:0,groups:[]};
assert.equal(lookup.styles.length,20);
assert.equal(lookup.canonicalMasks.length,47);
assert.equal(lookup.rawToCanonical.length,256);
for(let n=0;n<256;n++)assert.equal(lookup.canonicalMasks[lookup.rawToIndex[n]],lookup.rawToCanonical[n]);
async function image(filename, dimensions, full=false){
 const absolute=path.join(root,filename);
 const {data,info}=await sharp(absolute).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 if(dimensions){assert.equal(info.width,dimensions[0],filename);assert.equal(info.height,dimensions[1],filename);}
 for(let p=0;p<data.length;p+=4){
  const a=data[p+3];assert(a===0||a===255,`Nonbinary alpha: ${filename}`);
  if(full)assert.equal(a,255,`Hole inside occupied cell: ${filename}`);
  if(a)assert(palette.has((data[p]<<16)|(data[p+1]<<8)|data[p+2]),`Outside Sweetie16: ${filename}`);
  else assert.equal(data[p]|data[p+1]|data[p+2],0,`Colored transparent pixel: ${filename}`);
 }
 result.pixelsChecked+=info.width*info.height;
 return {data,width:info.width,height:info.height};
}
for(const style of lookup.styles){
 assert.equal(style.variants.length,4);
 for(const filename of Object.values(style.panels)){await image(filename,[128,128]);result.panelPngs++;}
 for(const variant of style.variants){
  assert.equal(variant.cells.length,47);assert.equal(Object.keys(variant.modules).length,20);
  const atlas=await image(variant.atlas,[256,192]);result.atlases++;
  for(const filename of Object.values(variant.modules)){await image(filename,[16,16],true);result.quarterPngs++;}
  for(const [index,cell] of variant.cells.entries()){
   assert.equal(cell.mask,lookup.canonicalMasks[index]);assert.equal(cell.index,index);
   const sprite=await image(cell.file,[32,32],true);result.cellPngs++;
   for(let y=0;y<32;y++){
    const atlasOffset=((cell.rect.y+y)*atlas.width+cell.rect.x)*4;
    assert(sprite.data.subarray(y*128,(y+1)*128).equals(atlas.data.subarray(atlasOffset,atlasOffset+128)),`Atlas/file mismatch: ${cell.file}`);
   }
  }
 }
 for(const fixture of fixtures){
  const w=fixture.rows[0].length,h=fixture.rows.length;
  const sample=await image(`Samples/${style.styleId}/${fixture.id}.png`,[w*32,h*32]);result.samples++;
  for(let y=0;y<h*32;y++)for(let x=0;x<w*32;x++){
   const solid=fixture.rows[Math.floor(y/32)][Math.floor(x/32)]==='#';
   assert.equal(sample.data[(y*sample.width+x)*4+3],solid?255:0,`Occupancy/alpha mismatch: ${style.styleId}/${fixture.id} at ${x},${y}`);
  }
 }
}
for(const input of old.files){
 const absolute=path.resolve(root,'../ANIMOL_Tilemap_Editor_v3/Assets/ANIMOL/TerrainStructure',input.file);
 try{assert.equal(crypto.createHash('sha256').update(await fs.readFile(absolute)).digest('hex'),input.sha256);result.oldFilesPreserved++;}
 catch(error){if(error.code==='ENOENT'){result.oldFilesCheck='SKIPPED (previous bundle not present)';}else throw error;}
}
assert.equal(result.cellPngs,3760);assert.equal(result.quarterPngs,1600);assert.equal(result.atlases,80);assert.equal(result.samples,320);
result.groups=['256-to-47 catalogue coverage','all actual PNG dimensions/palette/binary alpha','occupied-cell opacity','all atlas slots equal exported PNGs','all sample pixels equal logical masks','old source SHA256 preservation'];
result.limits=['No Unity compilation/runtime/collision or signed map save tested.','No browser execution in this validation.','Local shape coverage does not guarantee mixed-style transition art or every global composition quality.'];
await fs.writeFile(path.join(root,'Docs/sprite_file_validation.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
