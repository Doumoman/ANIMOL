/** Exact code-native functional inserts. The v4 architectural shell is sampled separately, never recolored. */
export const THEMES = {
 T01:{name:'월궁',light:'#f4f4f4',material:'#257179',accent:'#ef7d57',shade:'#5d275d'},
 T02:{name:'구름고래 목장',light:'#f4f4f4',material:'#94b0c2',accent:'#73eff7',shade:'#257179'},
 T03:{name:'별가루 도서관',light:'#f4f4f4',material:'#ffcd75',accent:'#ef7d57',shade:'#5d275d'},
 T04:{name:'시간유리 온실',light:'#73eff7',material:'#41a6f6',accent:'#a7f070',shade:'#257179'},
 T05:{name:'오로라 수정광산',light:'#94b0c2',material:'#566c86',accent:'#41a6f6',shade:'#333c57'}
};
export const COMMON_NAMES = {
 C01:'하향 발판',C02:'좌우 반전 스프링',C03:'상향 스프링',C04:'점멸 위험칸',C05:'이탈 소멸판',
 C06:'착지 반동 받침',C07:'방향 바람칸',C08:'방향 벨트',C09:'접근 생성판',C10:'저마찰 바닥',
 M02:'달그릇 (지역 후보)',R01:'공명 수정판 (지역 후보)',R03:'대전 수정판 (지역 후보)'
};
export const DIRECTIONS = kind => ['C02','C08'].includes(kind)?['LEFT','RIGHT']:kind==='C07'?['UP','LEFT','RIGHT']:['UP'];
export const POSES_BY_KIND = {
 C01:['idle','active'],C02:['idle','active'],C03:['idle','active'],C04:['idle','warn','active'],
 C05:['idle','warn','inactive'],C06:['idle','active'],C07:['idle','active'],C08:['idle','active'],
 C09:['inactive','warn','active'],C10:['idle','active'],M02:['idle','active'],R01:['active','warn','inactive'],R03:['idle','warn','active']
};
const INK='#1a1c2c', RED='#b13e53', WARN='#ffcd75';
const rgb=s=>[parseInt(s.slice(1,3),16),parseInt(s.slice(3,5),16),parseInt(s.slice(5,7),16)];

export function mechanism(theme,kind,facing='UP',pose='idle') {
 const t=THEMES[theme]; if(!t||!COMMON_NAMES[kind]||!DIRECTIONS(kind).includes(facing)) throw new Error('unsupported insert');
 if(!['idle','warn','active','inactive'].includes(pose)) throw new Error('unsupported pose');
 const data=new Uint8Array(32*32*4), roles=new Uint8Array(32*32);
 let role=2;
 const p=(x,y,col) => {
  if(!Number.isInteger(x)||!Number.isInteger(y)||x<0||x>31||y<0||y>31) throw new RangeError('pixel outside one cell');
  const i=4*(y*32+x); data.set([...rgb(col),255],i); roles[y*32+x]=role;
 };
 const rect=(x,y,w,h,c)=>{for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)p(xx,yy,c);};
 const box=(x,y,w,h,fill=t.material)=>{
  role=1;rect(x+1,y,w-2,1,INK);rect(x+1,y+h-1,w-2,1,INK);rect(x,y+1,1,h-2,INK);rect(x+w-1,y+1,1,h-2,INK);
  role=2;if(w>2&&h>2)rect(x+1,y+1,w-2,h-2,fill);
 };
 const hline=(x,y,w,c)=>rect(x,y,w,1,c);
 const diamond=(x,y,c)=>{rect(x+1,y,2,1,c);rect(x,y+1,4,2,c);rect(x+1,y+3,2,1,c);};
 const chevron=(x,y,dir,c)=>{
  if(dir==='UP'||dir==='DOWN'){
   for(let i=0;i<4;i++){const yy=dir==='UP'?y+i:y+3-i;rect(x+3-i,yy,2,1,c);rect(x+3+i,yy,2,1,c);}
  } else for(let i=0;i<4;i++){const xx=dir==='RIGHT'?x+3-i:x+i;rect(xx,y+3-i,1,2,c);rect(xx,y+3+i,1,2,c);}
 };
 const state=pose==='warn'?WARN:pose==='active'?t.light:t.accent;
 if(pose==='inactive'&&['C01','C05','C06','C09','M02','R01'].includes(kind)){
  diamond(14,13,t.shade);return {data,roles};
 }
 switch(kind){
 case 'C01':
  box(6,8,20,10,t.shade);hline(8,9,16,t.light);chevron(12,11,'DOWN',t.light);
  break;
 case 'C02': {
  const left=facing==='LEFT',compressed=pose==='active';
  box(3,7,26,19,t.shade);rect(left?0:30,9,2,15,state);
  const foldX=left?11:14,span=compressed?3:7;
  for(let row=0;row<3;row++){hline(foldX,11+row*4,span,t.light);rect(foldX+(row%2?0:span-1),11+row*4,1,4,t.light);}
  chevron(left?19:5,13,left?'LEFT':'RIGHT',t.light);
  break;
 }
 case 'C03':
  box(7,8,18,15,t.shade);hline(8,9,16,state);chevron(12,11,'UP',t.light);
  hline(10,18,12,t.accent);hline(11,20,10,t.accent);
  break;
 case 'C04': {
  if(pose!=='active'){
   role=1;hline(5,6,22,pose==='warn'?WARN:t.shade);hline(5,26,22,pose==='warn'?WARN:t.shade);
   rect(5,6,1,21,pose==='warn'?WARN:t.shade);rect(26,6,1,21,pose==='warn'?WARN:t.shade);role=2;
   box(14,11,4,8,pose==='warn'?WARN:t.shade);break;
  }
  const c=RED;
  for(let tooth=0;tooth<3;tooth++)for(let row=0;row<8;row++)rect(6+tooth*7+3-Math.floor(row/2),13+row,1+2*Math.floor(row/2),1,c);
  hline(5,21,22,t.accent);hline(5,22,22,t.shade);
  break;
 }
 case 'C05':
  box(7,8,18,9,t.shade);hline(8,9,16,t.light);diamond(14,11,state);rect(15,8,1,3,t.accent);
  break;
 case 'C06': {
  const compressed=pose==='active',yy=compressed?12:8;
  const cushion=theme==='T01'||theme==='T02'?t.light:theme==='T04'?t.accent:t.material;
  box(7,yy,18,10,t.shade);rect(9,yy+1,14,3,t.light);rect(8,yy+2,16,5,cushion);
  hline(9,yy+1,14,t.light);hline(10,yy+7,12,t.accent);
  if(theme==='T02'){hline(10,yy+4,12,t.light);}
  if(theme==='T03'){rect(9,yy+4,2,4,t.shade);hline(12,yy+4,11,t.light);}
  if(theme==='T04'){rect(15,yy+3,2,4,t.accent);}
  if(theme==='T05'){hline(11,yy+4,10,t.accent);}
  break;
 }
 case 'C07': {
  // Planned monotone staircase ribbons. Every run is 2,2,3,3; no isolated sparkle pixels.
  const ribbon=(sx,sy,vertical)=>{
   const path=[[0,0],[0,1],[1,2],[1,3],[2,4],[2,5],[2,6],[3,7],[3,8],[3,9]];
   for(const [a,b] of path) rect(vertical?sx+a:sx+b,vertical?sy+b:sy+a,vertical?2:1,vertical?1:2,t.light);
  };
  if(facing==='UP'){ribbon(7,14,true);ribbon(21,13,true);chevron(12,6,'UP',state);}
  else {ribbon(5,8,false);ribbon(6,22,false);chevron(facing==='LEFT'?17:19,13,facing,state);}
  break;
 }
 case 'C08':
  box(4,8,24,9,t.shade);hline(6,9,20,state);box(6,11,4,4,t.accent);box(22,11,4,4,t.accent);
  chevron(10,10,facing,t.light);chevron(16,10,facing,t.light);
  break;
 case 'C09':
  box(8,8,16,10,t.shade);hline(9,9,14,state);chevron(12,11,'UP',t.light);
  if(theme==='T03'){rect(15,9,2,8,t.accent);}
  if(theme==='T04'){rect(15,11,2,6,t.accent);hline(11,13,5,t.accent);hline(16,15,5,t.accent);}
  break;
 case 'C10':
  box(6,8,20,7,t.shade);hline(8,9,16,t.light);hline(9,10,14,t.material);hline(10,12,12,t.accent);
  break;
 case 'M02':
  box(7,8,18,12,t.shade);rect(9,9,14,5,t.light);rect(11,12,10,4,t.material);diamond(14,14,state);
  break;
 case 'R01':
  box(8,8,16,11,t.shade);diamond(14,10,state);hline(10,16,12,t.accent);
  break;
 case 'R03':
  box(8,8,16,15,t.shade);rect(14,10,4,9,pose==='active'?RED:state);chevron(12,16,'UP',pose==='active'?RED:t.light);
  break;
 }
 return {data,roles};
}
