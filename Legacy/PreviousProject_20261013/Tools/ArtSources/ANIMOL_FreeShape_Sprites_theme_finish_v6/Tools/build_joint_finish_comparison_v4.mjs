import fs from 'node:fs/promises';import path from 'node:path';import{createRequire}from'node:module';import{fileURLToPath}from'node:url';
const require=createRequire(import.meta.url),sharp=require('sharp'),ROOT=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),OLD=path.resolve(ROOT,'../ANIMOL_FreeShape_Sprites_brick_restore_v3');
const layers=[],W=1376,H=1452;const text=(x,y,t,size=19)=>'<text x="'+x+'" y="'+y+'" font-family="DejaVu Sans" font-size="'+size+'" fill="#f4f4f4">'+t+'</text>';let svg='<svg width="'+W+'" height="'+H+'" xmlns="http://www.w3.org/2000/svg">';
svg+=text(24,34,'ANIMOL T01_A | actual exported PNG comparison',24);svg+=text(24,67,'v3: previous joints',22)+text(712,67,'v4: aligned joints / shading / rounded stone corners',22);
for(const[col,root]of [[0,OLD],[1,ROOT]]){const x=24+col*688;
 const add=async(file,scale,yy,xx=x)=>{const img=sharp(path.join(root,file)),m=await img.metadata();layers.push({input:await img.resize(m.width*scale,m.height*scale,{kernel:'nearest'}).png().toBuffer(),left:xx,top:yy});};
 svg+=text(x,100,'Single cell (4x) and material repeat (4x)');
 await add('Art/T01_A/Sprites/mask000.png',4,114);
 const body=col===0?await sharp(path.join(root,'Art/T01_A/Sampling/body_field128.png')).extract({left:32,top:32,width:64,height:64}).resize(256,256,{kernel:'nearest'}).png().toBuffer():await sharp(path.join(root,'SourceArt/Revised/T01_A_BrickRepeat64.png')).resize(256,256,{kernel:'nearest'}).png().toBuffer();
 layers.push({input:body,left:x+168,top:114});
 svg+=text(x,412,'5x3 solid terrain (2x)');await add('Samples/T01_A/rectangle_5x3.png',2,424);
 svg+=text(x,664,'Stepped terrain (2x)');await add('Samples/T01_A/regular_stairs_ascending_right.png',2,676);
 svg+=text(x,976,'Walls / ceiling / inner corners (2x)');await add('Samples/T01_A/closed_hole.png',2,988);
}
svg+='</svg>';layers.unshift({input:Buffer.from(svg),left:0,top:0});
await sharp({create:{width:W,height:H,channels:4,background:'#29313e'}}).composite(layers).png({compressionLevel:9}).toFile(path.join(ROOT,'Previews/T01_A_joint_finish_v4_comparison.png'));
for(const n of ['mask000','mask255','mask085','mask213']){try{await sharp(path.join(ROOT,'Art/T01_A/Sprites',n+'.png')).resize(320,320,{kernel:'nearest'}).png().toFile(path.join(ROOT,'Previews',n+'_junction_v4_10x.png'));}catch{}}
console.log('Actual sprite comparison rendered with integer nearest enlargement.');

