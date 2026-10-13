// Expected images use the delivered atlas bytes and the package's global topology/phase contract.
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {occupancyFromRows,resolveCell,RAW_TO_CANONICAL} from '../ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6/Tools/terrain_topology.mjs';
import {resolveVariant,motifPlacements,hashArtPlacement} from '../ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6/Tools/terrain_composition.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const pack=path.join(root,'Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6');
const sharp=createRequire(path.join(pack,'package.json'))('sharp');
const read=async n=>JSON.parse(await fs.readFile(path.join(pack,n),'utf8'));
const lookup=await read('Data/sprite_lookup.json');
const fixtures=(await read('Data/logical_fixtures.json')).fixtures.map(f=>({...f,seed:0,scale8:false}));
for(const [x,y] of [[0,0],[1,0],[0,1],[1,1],[-17,-17],[-16,-16]])
 fixtures.push({id:`hole_1x1_${x}_${y}`,origin:{x,y},seed:x<0?-17:0,rows:['###','#.#','###'],scale8:x===0&&y===0});
fixtures.push({id:'hole_2x2',origin:{x:-17,y:-17},seed:7,rows:['####','#..#','#..#','####'],scale8:false});
const out=path.join(root,'Docs/Validation/ThemeFinishV6/Expected');await fs.mkdir(out,{recursive:true});
for(const s of lookup.styles){
 const atlases=await Promise.all(s.variants.map(async v=>({v,...await sharp(path.join(pack,v.atlas)).ensureAlpha().raw().toBuffer({resolveWithObject:true})})));
 const motif=await sharp(path.join(pack,s.panels.motif)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(const f of fixtures){
  const grid=occupancyFromRows(f.rows,f.origin),w=grid.width*32,h=grid.height*32,data=Buffer.alloc(w*h*4);
  for(const key of grid.occupied){const [x,y]=key.split(',').map(Number),a=atlases[resolveVariant(x,y,f.seed)],cell=a.v.cells.find(c=>c.mask===resolveCell(grid.occupied,x,y).canonicalMask);
   for(let row=0;row<32;row++)a.data.copy(data,((h-(y-f.origin.y+1)*32+row)*w+(x-f.origin.x)*32)*4,((cell.rect.y+row)*a.info.width+cell.rect.x)*4,((cell.rect.y+row)*a.info.width+cell.rect.x+32)*4);
  }
  for(const m of motifPlacements(grid,s.styleId,f.seed))for(let y=0;y<128;y++)for(let x=0;x<128;x++){
   const a=(y*128+x)*4,b=((h-(m.y-f.origin.y+4)*32+y)*w+(m.x-f.origin.x)*32+x)*4;
   if(motif.data[a+3])motif.data.copy(data,b,a,a+4);
  }
  await sharp(data,{raw:{width:w,height:h,channels:4}}).png().toFile(path.join(out,s.styleId+'-'+f.id+'.png'));
 }
}
await fs.writeFile(path.join(out,'Fixtures.json'),JSON.stringify({fixtures},null,2)+'\n');
console.log(JSON.stringify({styles:lookup.styles.length,fixtures:fixtures.length,images:lookup.styles.length*fixtures.length}));

const vectors=[];
for(const style of ['T01_A','T03_C','T05_D'])for(const x of [-17,-16,-1,0,1,16,17])for(const y of [-17,-16,-1,0,1,16,17])for(const seed of [0,1,2,3,-17,-2147483648,2147483647])vectors.push({style,x,y,seed,variant:resolveVariant(x,y,seed),hash:hashArtPlacement(style,x,y,seed)});
await fs.writeFile(path.join(out,'ReferenceVectors.json'),JSON.stringify({raw:RAW_TO_CANONICAL,vectors})+'\n');
