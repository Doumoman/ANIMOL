/** Deterministic QA-only player. Permanent ANIMOL input, stamina, animals and networking are not replaced. */
export const QA_STEP=1/120;
export const QA_CONFIG={width:.55,height:.88,walkSpeed:4.2,acceleration:32,airAcceleration:24,friction:30,iceFriction:2.5,gravity:22,jumpSpeed:8.5,springSpeed:13,mochiSpeed:6,coyoteSeconds:.1,jumpBufferSeconds:.1};
export function createQAStage(){
 const cells=[];const put=(x,y,kind='terrain',facing='UP')=>{
  const old=cells.findIndex(c=>c.x===x&&c.y===y);if(old>=0)cells.splice(old,1);
  cells.push({x,y,kind,facing});
 };
 for(let x=0;x<22;x++)put(x,0);
 for(const [left,right,y]of [[5,9,3],[10,14,6],[15,19,9]])for(let x=left;x<=right;x++)put(x,y);
 for(const [x,y]of [[4,1],[9,4],[14,7]])put(x,y,'C03');
 put(20,2,'C02','LEFT');for(let y=1;y<=4;y++)put(21,y);
 put(8,3,'C05');put(12,6,'C06');put(1,0,'C08','RIGHT');put(18,9,'C10');
 put(7,5,'C01');put(11,4,'C07','UP');put(18,1,'C04');
 return {id:'ANIMOL_TEMP_PLAYER_QA_V1',width:22,height:14,
  spawn:{x:2.5,y:1},bells:[{id:1,x:6.5,y:4.65},{id:2,x:11.5,y:7.65},{id:3,x:16.5,y:10.65}],
  goal:{x:19.0,y:10,width:.8,height:1.4},cells};
}
export function createQAGame(stage=createQAStage()){
 const game={stage,events:[],deaths:0,time:0,tick:0,status:'playing',message:'방울 3개를 모아 목적지로 가세요.'};
 resetQAGame(game,'start');return game;
}
export function resetQAGame(g,reason='manual'){
 g.player={x:g.stage.spawn.x,y:g.stage.spawn.y,vx:0,vy:0,grounded:true,facing:1,coyote:.1,jumpBuffer:0,dropUntil:-1};
 g.bells=g.stage.bells.map(b=>({...b,broken:false}));g.collected=0;g.status='playing';g.standing=null;
 g.cells=g.stage.cells.map(c=>({...c,surfaceEnabled:true,pose:c.kind==='C04'?'active':'idle',eventTime:-100,collapseTime:null}));
 g.message=reason==='start'?'방울 3개를 모아 목적지로 가세요.':reason==='manual'?'처음부터 다시 시작했어요.':'시작 지점으로 돌아왔어요. 방울도 초기화됐어요.';
 g.events.push({type:'reset',reason,tick:g.tick,time:g.time});
}
const overlap=(a,b)=>a.x+a.w>b.x+1e-7&&a.x<b.x+b.w-1e-7&&a.y+a.h>b.y+1e-7&&a.y<b.y+b.h-1e-7;
const approach=(value,target,amount)=>Math.abs(target-value)<=amount?target:value+Math.sign(target-value)*amount;
export const playerRect=g=>({x:g.player.x-QA_CONFIG.width/2,y:g.player.y,w:QA_CONFIG.width,h:QA_CONFIG.height});
export function qaGeometry(c){
 if(['terrain','C08','C10'].includes(c.kind))return {type:'solid',x:c.x,y:c.y,w:1,h:1,cell:c};
 if(c.kind==='C02')return {type:'side',x:c.x+(c.facing==='LEFT'?0:28/32),y:c.y+3/32,w:4/32,h:26/32,cell:c};
 if(c.kind==='C03'&&c.surfaceEnabled)return {type:'top',x:c.x+2/32,y:c.y+1,w:28/32,h:0,cell:c};
 if(['C01','C05','C06'].includes(c.kind)&&c.surfaceEnabled)return {type:'top',x:c.x,y:c.y+1,w:1,h:0,cell:c};
 if(c.kind==='C04')return {type:'hazard',x:c.x+1/32,y:c.y,w:30/32,h:15/32,cell:c};
 if(c.kind==='C07')return {type:'wind',x:c.x,y:c.y,w:1,h:1,cell:c};
 return null;
}
function emit(g,type,data={}){g.events.push({type,tick:g.tick,time:g.time,...data});}
export function stepQAGame(g,input={},dt=QA_STEP){
 if(!Number.isFinite(dt)||dt<=0||dt>1/30)throw new Error('QA step must be positive and at most 1/30');
 if(input.reset){resetQAGame(g);return g;}if(g.status==='cleared')return g;
 g.tick++;g.time+=dt;const p=g.player,C=QA_CONFIG,oldStanding=g.standing,oldCell=g.cells.find(c=>`${c.x},${c.y}`===oldStanding);
 const axis=Math.max(-1,Math.min(1,Number(input.axis)||0));
 if(axis){p.vx=approach(p.vx,axis*C.walkSpeed,(p.grounded?C.acceleration:C.airAcceleration)*dt);p.facing=Math.sign(axis);}
 else p.vx=approach(p.vx,0,(p.grounded?(oldCell?.kind==='C10'?C.iceFriction:C.friction):1.5)*dt);
 const belt=p.grounded&&oldCell?.kind==='C08'?(oldCell.facing==='LEFT'?-1.2:1.2):0;
 p.coyote=p.grounded?C.coyoteSeconds:Math.max(0,p.coyote-dt);
 p.jumpBuffer=input.jumpPressed?C.jumpBufferSeconds:Math.max(0,p.jumpBuffer-dt);
 if(input.down&&p.grounded&&oldCell&&['C01','C05','C06'].includes(oldCell.kind)){
  p.dropUntil=g.time+.25;p.y-=.02;p.grounded=false;p.coyote=0;g.standing=null;
 }
 if(p.jumpBuffer>0&&p.coyote>0){p.vy=C.jumpSpeed;p.grounded=false;p.coyote=0;p.jumpBuffer=0;emit(g,'jump');}
 p.vy=Math.max(-18,p.vy-C.gravity*dt);
 const geometry=g.cells.map(qaGeometry).filter(Boolean),body=playerRect(g);
 for(const r of geometry)if(r.type==='wind'&&overlap(body,r)){
  if(r.cell.facing==='UP')p.vy+=8*dt;else p.vx+=(r.cell.facing==='LEFT'?-6:6)*dt;
 }
 // Swept horizontal contact, choosing the nearest face to prevent tunnelling through a side spring.
 const dx=(p.vx+belt)*dt,oldX=p.x,newX=oldX+dx,hits=[];
 for(const r of geometry){
  if(!['solid','side'].includes(r.type)||p.y+C.height<=r.y+1e-7||p.y>=r.y+r.h-1e-7)continue;
  if(dx>0&&oldX+C.width/2<=r.x+1e-6&&newX+C.width/2>=r.x)
   hits.push({r,x:r.x-C.width/2,normal:-1});
  if(dx<0&&oldX-C.width/2>=r.x+r.w-1e-6&&newX-C.width/2<=r.x+r.w)
   hits.push({r,x:r.x+r.w+C.width/2,normal:1});
 }
 hits.sort((a,b)=>dx>0?a.x-b.x:b.x-a.x);p.x=hits.length?hits[0].x:newX;
 if(hits.length){const h=hits[0],front=h.r.type==='side'&&(h.r.cell.facing==='LEFT'?h.normal===-1:h.normal===1);
  if(front){const incoming=p.vx;p.vx=-incoming;h.r.cell.eventTime=g.time;h.r.cell.pose='active';emit(g,'sideSpring',{incoming,outgoing:p.vx,x:h.r.cell.x,y:h.r.cell.y});}
  else p.vx=0;
 }
 p.x=Math.max(C.width/2,Math.min(g.stage.width-C.width/2,p.x));
 const previousY=p.y,desiredY=previousY+p.vy*dt;let landing=null,ceiling=null;
 for(const r of geometry){
  if(p.x+C.width/2<=r.x+1e-7||p.x-C.width/2>=r.x+r.w-1e-7)continue;
  if(p.vy<=0&&(r.type==='solid'||r.type==='top'&&g.time>=p.dropUntil)){
   const top=r.type==='top'?r.y:r.y+r.h;
   if(previousY>=top-1e-5&&desiredY<=top+1e-6&&(!landing||top>landing.top))landing={r,top};
  }
  if(p.vy>0&&r.type==='solid'&&previousY+C.height<=r.y+1e-5&&desiredY+C.height>=r.y)
   if(!ceiling||r.y<ceiling.y)ceiling=r;
 }
 p.grounded=false;g.standing=null;p.y=desiredY;
 if(ceiling){p.y=ceiling.y-C.height;p.vy=0;}
 if(landing){const incomingVy=p.vy,c=landing.r.cell;p.y=landing.top;p.vy=0;p.grounded=true;g.standing=`${c.x},${c.y}`;
  if(c.kind==='C03'||c.kind==='C06'&&incomingVy<-.5){p.vy=c.kind==='C03'?C.springSpeed:C.mochiSpeed;p.grounded=false;p.coyote=0;g.standing=null;
   c.eventTime=g.time;c.pose='active';emit(g,c.kind==='C03'?'upSpring':'mochi',{x:c.x,y:c.y});}
  if(c.kind==='C05')c.pose='active';
 }
 if(oldCell?.kind==='C05'&&oldStanding!==g.standing&&oldCell.surfaceEnabled){
  oldCell.surfaceEnabled=false;oldCell.pose='inactive';oldCell.collapseTime=g.time;emit(g,'crumble',{x:oldCell.x,y:oldCell.y});
 }
 for(const c of g.cells)if(['C02','C03','C06'].includes(c.kind)&&g.time-c.eventTime>.28)c.pose='idle';
 const now=playerRect(g);
 for(const r of geometry)if(r.type==='hazard'&&overlap(now,r)){
  g.deaths++;emit(g,'death',{reason:'hazard'});resetQAGame(g,'hazard');return g;
 }
 if(p.y< -3){g.deaths++;emit(g,'death',{reason:'fall'});resetQAGame(g,'fall');return g;}
 for(const bell of g.bells)if(!bell.broken){
  const nearX=Math.max(now.x,Math.min(bell.x,now.x+now.w)),nearY=Math.max(now.y,Math.min(bell.y,now.y+now.h));
  if((nearX-bell.x)**2+(nearY-bell.y)**2<=.28**2){bell.broken=true;g.collected++;emit(g,'bell',{id:bell.id});g.message=g.collected===3?'방울 3개 완료! 열린 목적지로 가세요.':`방울 ${g.collected}/3`;}
 }
 const goal={x:g.stage.goal.x,y:g.stage.goal.y,w:g.stage.goal.width,h:g.stage.goal.height};
 if(overlap(now,goal)){
  if(g.collected===3){g.status='cleared';p.vx=p.vy=0;g.message='클리어! 시작 → 방울 3개 → 목적지 완료';emit(g,'clear');}
  else g.message=`목적지는 잠겨 있어요. 방울 ${g.collected}/3`;
 }
 return g;
}

/** Reproducible input-only route for QA evidence; it never changes positions or bell state. */
export function createQAReplayController(){
 let phase=0,launched=false,eventIndex=0;const springXs=[4.5,9.5,14.5],bellXs=[6.5,11.5,16.5];
 return g=>{
  if(g.status==='cleared')return {};
  for(;eventIndex<g.events.length;eventIndex++){const e=g.events[eventIndex];if(e.type==='reset'){phase=0;launched=false;}if(e.type==='upSpring')launched=true;}
  if(phase<3&&g.bells[phase].broken){phase++;launched=false;}
  const target=phase===3?19.4:launched?bellXs[phase]:springXs[phase],dx=target-g.player.x;
  let axis=Math.abs(dx)>.18?Math.sign(dx):Math.abs(g.player.vx)>.45?-Math.sign(g.player.vx):0;
  const jumpPressed=phase<3&&!launched&&g.player.grounded&&Math.abs(dx)<.35;
  return {axis,jumpPressed};
 };
}
