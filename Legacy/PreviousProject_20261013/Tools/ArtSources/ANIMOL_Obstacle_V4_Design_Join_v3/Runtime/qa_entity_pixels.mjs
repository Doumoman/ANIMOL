/** Small QA entities in the same 16-color palette. These are temporary stand-ins, not final character art. */
export function qaEntity(kind,state='idle',facing=1){
 const width=kind==='goal'?64:32,height=width,data=new Uint8Array(width*height*4);
 const put=(x,y,color)=>{const rgb=color.match(/\w\w/g).map(s=>parseInt(s,16));data.set([...rgb,255],4*(y*width+x));};
 const rect=(x,y,w,h,c)=>{for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)put(xx,yy,c);};
 if(kind==='player'){
  rect(9,5,5,14,'1a1c2c');rect(18,5,5,14,'1a1c2c');rect(10,6,3,12,'f4f4f4');rect(19,6,3,12,'f4f4f4');
  for(let y=16;y<31;y++){const inset=y===16||y===30?2:y===17||y===29?1:0;
   rect(7+inset,y,18-2*inset,1,'1a1c2c');if(y>16&&y<30)rect(8+inset,y,16-2*inset,1,'f4f4f4');}
  rect(facing<0?10:21,20,1,2,'1a1c2c');rect(9,30,5,2,'1a1c2c');rect(18,30,5,2,'1a1c2c');
 }else if(kind==='bell'){
  rect(14,3,4,4,'ffcd75');
  const half=[3,5,6,7,8,8,8,8,8,8,8,8,8,7,6,5,3];
  for(let y=0;y<17;y++){const r=half[y];rect(16-r,7+y,2*r,1,'1a1c2c');if(y>0&&y<16)rect(17-r,7+y,2*r-2,1,'ffcd75');}
  rect(11,10,8,2,'f4f4f4');rect(15,24,3,3,'ef7d57');
 }else if(kind==='start'){
  rect(8,7,2,25,'f4f4f4');for(let y=0;y<10;y++)rect(10,7+y,13-Math.floor(y/2),1,'a7f070');rect(6,30,6,2,'257179');
 }else if(kind==='goal'){
  rect(17,18,30,46,'333c57');rect(19,20,26,44,state==='open'?'38b764':'566c86');
  rect(22,23,20,41,'1a1c2c');rect(19,20,26,2,state==='open'?'a7f070':'94b0c2');
  if(state!=='open'){rect(28,38,8,10,'94b0c2');rect(29,35,6,4,'94b0c2');rect(30,36,4,3,'1a1c2c');}
 }
 return {width,height,data};
}
