/** Integer pixel mechanisms. Architectural body sprites remain byte-identical to v4. */
export const THEMES = {
 T01:{name:'월궁',light:'#f4f4f4',material:'#257179',accent:'#ef7d57',shade:'#5d275d',mochi:'#f4f4f4'},
 T02:{name:'구름고래 목장',light:'#f4f4f4',material:'#94b0c2',accent:'#73eff7',shade:'#257179',mochi:'#f4f4f4'},
 T03:{name:'별가루 도서관',light:'#f4f4f4',material:'#ffcd75',accent:'#ef7d57',shade:'#5d275d',mochi:'#ffcd75'},
 T04:{name:'시간유리 온실',light:'#73eff7',material:'#41a6f6',accent:'#a7f070',shade:'#257179',mochi:'#a7f070'},
 T05:{name:'오로라 수정광산',light:'#94b0c2',material:'#566c86',accent:'#41a6f6',shade:'#333c57',mochi:'#73eff7'}
};
export const COMMON_NAMES = {
 C01:'하향 발판',C02:'좌우 반사 스프링',C03:'상향 스프링',C04:'상시 위험칸',C05:'이탈 소멸판',
 C06:'떡 반동 발판',C07:'방향 바람칸',C08:'컨베이어',C10:'얼음 바닥',
 M02:'달그릇 (지역 후보)',R01:'공명 수정판 (지역 후보)',R03:'대전 수정판 (지역 후보)'
};
export const COMMON_KINDS = Object.keys(COMMON_NAMES).filter(k=>k.startsWith('C'));
export const DIRECTIONS = kind => ['C02','C08'].includes(kind)?['LEFT','RIGHT']:kind==='C07'?['UP','LEFT','RIGHT']:['UP'];
export const POSES_BY_KIND = {
 C01:['idle'],C02:['idle','active'],C03:['idle','active'],C04:['active'],
 C05:['idle','active','warn','inactive'],C06:['idle','active'],C07:['idle','active'],C08:['idle','active'],
 C10:['idle'],M02:['idle','active'],R01:['active','warn','inactive'],R03:['idle','warn','active']
};
export const FRAME_COUNTS = {C02:{active:7},C03:{active:7},C06:{active:7},C08:{idle:8,active:8}};
export const FRAME_MS = {C02:30,C03:30,C06:40,C08:80};
export const REPLACE_CAP_KINDS = new Set(['C03','C05','C06','C08','C10']);
export const frameCount = (kind,pose)=>FRAME_COUNTS[kind]?.[pose]||1;
export const animationFrame = (kind,pose,elapsedMs)=>{
 if(!Number.isFinite(elapsedMs)||elapsedMs<0)throw new Error('nonnegative animation time required');
 const count=frameCount(kind,pose),index=Math.floor(elapsedMs/(FRAME_MS[kind]||100));
 return kind==='C08'?index%count:Math.min(count-1,index);
};
export const FRAGMENTS = [
 {id:0,rect:{x:0,y:0,width:8,height:8},velocityX:-0.10,velocityY:-1.2},
 {id:1,rect:{x:8,y:0,width:10,height:8},velocityX:-0.03,velocityY:-1.6},
 {id:2,rect:{x:18,y:0,width:8,height:8},velocityX:0.04,velocityY:-1.35},
 {id:3,rect:{x:26,y:0,width:6,height:8},velocityX:0.10,velocityY:-1.8}
];
const INK='#1a1c2c', RED='#b13e53', WARN='#ffcd75', WHITE='#f4f4f4', CYAN='#73eff7', BLUE='#41a6f6';
const rgb=s=>[parseInt(s.slice(1,3),16),parseInt(s.slice(3,5),16),parseInt(s.slice(5,7),16)];

export function mechanism(theme,kind,facing='UP',pose='idle',frame=0) {
 const t=THEMES[theme];
 if(!t||!COMMON_NAMES[kind]||!DIRECTIONS(kind).includes(facing))throw new Error('unsupported insert');
 if(!POSES_BY_KIND[kind].includes(pose))throw new Error('unsupported pose for kind');
 if(!Number.isInteger(frame)||frame<0||frame>=frameCount(kind,pose))throw new Error('unsupported animation frame');
 const data=new Uint8Array(32*32*4),roles=new Uint8Array(32*32);let role=2;
 const p=(x,y,col)=>{
  if(!Number.isInteger(x)||!Number.isInteger(y)||x<0||x>31||y<0||y>31)throw new RangeError('pixel outside one cell');
  const i=4*(y*32+x);data.set([...rgb(col),255],i);roles[y*32+x]=role;
 };
 const rect=(x,y,w,h,c)=>{for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)p(xx,yy,c);};
 const clear=(x,y,w,h)=>{for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++){data.fill(0,4*(yy*32+xx),4*(yy*32+xx)+4);roles[yy*32+xx]=0;}};
 const hline=(x,y,w,c)=>rect(x,y,w,1,c);
 const line=(x0,y0,x1,y1,c)=>{
  let dx=Math.abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;
  for(;;){p(x0,y0,c);if(x0===x1&&y0===y1)break;const e2=2*err;if(e2>=dy){err+=dy;x0+=sx;}if(e2<=dx){err+=dx;y0+=sy;}}
 };
 const box=(x,y,w,h,fill=t.material)=>{
  role=1;rect(x+1,y,w-2,1,INK);rect(x+1,y+h-1,w-2,1,INK);rect(x,y+1,1,h-2,INK);rect(x+w-1,y+1,1,h-2,INK);
  role=2;if(w>2&&h>2)rect(x+1,y+1,w-2,h-2,fill);
 };
 const diamond=(x,y,c)=>{rect(x+1,y,2,1,c);rect(x,y+1,4,2,c);rect(x+1,y+3,2,1,c);};
 const chevron=(x,y,dir,c)=>{
  if(dir==='UP'||dir==='DOWN')for(let i=0;i<4;i++){const yy=dir==='UP'?y+i:y+3-i;rect(x+3-i,yy,2,1,c);rect(x+3+i,yy,2,1,c);}
  else for(let i=0;i<4;i++){const xx=dir==='RIGHT'?x+3-i:x+i;rect(xx,y+3-i,1,2,c);rect(xx,y+3+i,1,2,c);}
 };
 const state=pose==='warn'?WARN:pose==='active'?t.light:t.accent;
 if(pose==='inactive')return {data,roles};
 switch(kind){
 case 'C01':break; // The unmarked thin v4 cap is the entire design.
 case 'C02':{
  const compression=pose==='active'?[0,2,4,6,4,2,0][frame]:0;
  const face=compression,mirror=facing==='RIGHT';
  const q=(x,y,c)=>p(mirror?31-x:x,y,c);
  // Only the rear mounting bar, contact plate and exposed coil. No roof or filled box.
  for(let y=3;y<32;y++)for(let x=28;x<32;x++)q(x,y,t.material);
  for(let y=3;y<32;y++)q(28,y,t.light);
  const nodes=[];for(let i=0;i<=6;i++)nodes.push([face+5+Math.round((27-face-5)*i/6),i%2?25:7]);
  for(let i=1;i<nodes.length;i++){
   let [x0,y0]=nodes[i-1],[x1,y1]=nodes[i];if(mirror){x0=31-x0;x1=31-x1;}line(x0,y0,x1,y1,WHITE);
  }
  for(let y=3;y<29;y++)for(let x=face;x<face+4;x++)q(x,y,t.material);
  for(let y=3;y<29;y++)q(face,y,WHITE);
  for(let x=face;x<face+4;x++){q(x,3,WHITE);q(x,28,t.accent);}
  break;
 }
 case 'C03':{
  const compression=pose==='active'?[0,2,4,5,4,2,0][frame]:0;
  // Standing on the support: a wide top plate, tall exposed coil and a small foot.
  const top=compression;
  rect(2,top,28,4,t.material);hline(2,top,28,WHITE);hline(2,top+3,28,t.shade);
  const nodes=[];for(let i=0;i<=4;i++)nodes.push([i%2?24:7,top+5+Math.round((27-top-5)*i/4)]);
  for(let i=1;i<nodes.length;i++)line(...nodes[i-1],...nodes[i],WHITE);
  rect(7,top+4,2,2,WHITE);rect(7,27,2,3,WHITE);
  rect(4,29,24,3,t.material);hline(4,29,24,t.light);hline(4,31,24,t.shade);break;
 }
 case 'C04':{
  for(let tooth=0;tooth<3;tooth++)for(let row=0;row<12;row++){
   const half=Math.floor(row/3),x=6+tooth*9-half,y=17+row;
   rect(x,y,1+2*half,1,WHITE);if(row>=3)rect(x,y,1,1,BLUE);
  }
  rect(1,29,30,2,RED);hline(1,31,30,t.shade);break;
 }
 case 'C05':{
  for(const f of FRAGMENTS){const {x,width:w}=f.rect;
   rect(x,0,w,8,t.material);hline(x,0,w,t.light);hline(x,7,w,t.shade);
  }
  for(const [start,ys]of [[7,[0,0,-1,-1,0,0,0,0]],[17,[0,-1,-1,0,0,1,1,1]],[25,[0,0,1,1,0,0,-1,-1]]]){
   for(let y=0;y<8;y++){const x=start+ys[y];clear(x,y,2,1);}
  }
  if(pose==='active'||pose==='warn'){
   clear(11,3,1,3);clear(11,3,4,1);clear(21,1,1,4);
  }
  break;
 }
 case 'C06':{
  const squash=pose==='active'?[0,2,4,5,4,1,0][frame]:0;
  const top=squash,h=12-squash,width=squash?32:28,left=(32-width)/2;
  const spans=[];
  for(let y=0;y<h;y++){
   const inset=y===0?4:y===1?2:y===h-1?2:y===2?1:0;
   spans.push([left+inset,left+width-1-inset]);
  }
  for(let y=0;y<h;y++)for(let x=spans[y][0];x<=spans[y][1];x++){
   const edge=y===0||y===h-1||x===spans[y][0]||x===spans[y][1]||
    (y>0&&(x<spans[y-1][0]||x>spans[y-1][1]));
   role=edge?1:2;p(x,top+y,edge?INK:t.mochi);
  }
  role=2;break;
 }
 case 'C07':{
  const ribbon=(sx,sy,vertical)=>{const points=[[0,0],[0,1],[1,2],[1,3],[2,4],[2,5],[2,6],[3,7],[3,8],[3,9]];
   for(const[a,b]of points)rect(vertical?sx+a:sx+b,vertical?sy+b:sy+a,vertical?2:1,vertical?1:2,t.light);};
  if(facing==='UP'){ribbon(7,14,true);ribbon(21,13,true);chevron(12,6,'UP',state);}
  else{ribbon(5,8,false);ribbon(6,22,false);chevron(facing==='LEFT'?17:19,13,facing,state);}
  break;
 }
 case 'C08':{
  rect(0,0,32,4,'#333c57');hline(0,0,32,INK);hline(0,3,32,t.shade);
  const sign=facing==='RIGHT'?1:-1;
  for(let x=0;x<32;x++)if(((x-sign*frame)%8+8)%8<3)rect(x,1,1,2,t.light);
  rect(0,4,32,8,t.material);hline(0,11,32,t.shade);
  for(const cx of [5,26]){
   box(cx-4,4,8,7,t.shade);rect(cx-2,5,4,4,t.light);
   const rotations=[[0,-1],[1,0],[0,1],[-1,0]],a=rotations[((frame*sign)%4+4)%4];
   rect(cx+a[0]-1,7+a[1],2,1,t.accent);
  }
  hline(10,6,12,t.shade);hline(10,9,12,t.light);break;
 }
 case 'C10':{
  rect(0,0,32,9,CYAN);hline(0,0,32,WHITE);hline(0,1,32,WHITE);hline(0,8,32,BLUE);
  for(let y=2;y<8;y++)for(let dx=0;dx<3;dx++)p(5+y+dx,y,WHITE);
  for(const[x,h]of [[3,3],[18,2],[28,3]]){rect(x,9,3,h-1,BLUE);rect(x+1,9+h-1,1,1,BLUE);}
  break;
 }
 case 'M02':box(7,8,18,12,t.shade);rect(9,9,14,5,t.light);rect(11,12,10,4,t.material);diamond(14,14,state);break;
 case 'R01':box(8,8,16,11,t.shade);diamond(14,10,state);hline(10,16,12,t.accent);break;
 case 'R03':box(8,8,16,15,t.shade);rect(14,10,4,9,pose==='active'?RED:state);chevron(12,16,'UP',pose==='active'?RED:t.light);break;
 }
 return {data,roles};
}
