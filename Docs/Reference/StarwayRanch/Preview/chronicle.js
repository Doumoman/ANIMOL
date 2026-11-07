/* Dual-record credits. Pure timeline + DOM player; no image alteration or saved game state. */
(() => {
 'use strict';
 const clamp=(n,a=0,b=1)=>Math.max(a,Math.min(b,n));
 class ChronicleTimeline {
  constructor(data,viewportHeight,contentHeight,rowCenters){
   this.data=data;this.viewportHeight=viewportHeight;this.contentHeight=contentHeight;this.rowCenters=rowCenters;
   this.phase='intro';this.progress=0;this.y=viewportHeight;this.held=false;this.skipped=false;this.finished=false;
  }
  get distance(){return this.viewportHeight+this.contentHeight;}
  get speed(){return this.held?(this.data.holdMultiplier||3):1;}
  duration(){return ({intro:this.data.introDuration||.6,asteran:this.distance/(this.data.scrollSpeed||84),pause:this.data.afterAsteranPause||1,rewind:this.data.rewindDuration||4.2,rewritePause:this.data.afterRewritePause||.65,higher:this.distance/(this.data.scrollSpeed||84)})[this.phase]||Infinity;}
  setMetrics(viewportHeight,contentHeight,rowCenters){this.viewportHeight=viewportHeight;this.contentHeight=contentHeight;this.rowCenters=rowCenters;this.layout();}
  layout(){
   if(this.phase==='asteran'||this.phase==='higher')this.y=this.viewportHeight-this.distance*this.progress;
   else if(this.phase==='pause'||this.phase==='done')this.y=-this.contentHeight;
   else if(this.phase==='rewind')this.y=-this.contentHeight+this.distance*this.progress;
   else this.y=this.viewportHeight;
  }
  update(seconds){
   if(this.phase==='done')return;
   let left=Math.max(0,seconds)*this.speed;
   for(let guard=0;left>0&&guard<8;guard++){
    const duration=this.duration(),remaining=(1-this.progress)*duration;
    if(left<remaining){this.progress+=left/duration;left=0;}
    else {left-=remaining;this.phase=({intro:'asteran',asteran:'pause',pause:'rewind',rewind:'rewritePause',rewritePause:'higher',higher:'done'})[this.phase];this.progress=0;if(this.phase==='done'){this.finished=true;this.held=false;break;}}
   }
   this.layout();
  }
  changed(index){if(['rewritePause','higher','done'].includes(this.phase))return true;if(this.phase!=='rewind')return false;return this.y+this.rowCenters[index]>=this.viewportHeight*.5;}
  noise(index){if(this.phase!=='rewind')return false;return Math.abs(this.y+this.rowCenters[index]-this.viewportHeight*.5)<this.viewportHeight*.09;}
  skip(){this.skipped=true;this.finished=false;this.held=false;this.phase='done';this.progress=0;this.layout();}
  snapshot(){return {phase:this.phase,progress:this.progress,y:this.y,held:this.held,speed:this.speed,skipped:this.skipped,finished:this.finished,changed:this.rowCenters.map((_,i)=>this.changed(i))};}
 }
 class ChroniclePlayer {
  constructor(options){
   Object.assign(this,options);this.focused=typeof document.hasFocus==='function'?document.hasFocus():true;this.active=false;this.serial=0;this.frame=0;this.lastTime=null;this.pointerId=null;this.pressTime=0;this.staticPage=0;this.staticSide='asteran';this.rows=[];this.elapsedVisible=0;
   this.layer.onpointerdown=e=>this.pointerDown(e);
   this.layer.onpointerup=e=>this.pointerUp(e);
   this.layer.onpointercancel=()=>this.release();this.layer.onlostpointercapture=()=>this.release();this.layer.onpointerleave=()=>this.release();
   this.continueButton.onclick=e=>{if(e)e.stopPropagation();if(this.active&&this.isDone()&&this.pointerId===null&&!this.keyHeld)this.onContinue(this.result());};
   window.addEventListener('blur',()=>{this.focused=false;this.release();this.lastTime=null;});
   window.addEventListener('focus',()=>{this.focused=true;this.lastTime=null;if(this.active&&!this.reduced)this.schedule();});
   window.addEventListener('pagehide',()=>{this.focused=false;this.release();this.lastTime=null;});
   document.addEventListener('visibilitychange',()=>{this.release();this.lastTime=null;if(!document.hidden&&this.active&&!this.reduced)this.schedule();});
   document.addEventListener('keydown',e=>{
    if(!this.active||e.code!=='Space'||e.repeat||e.target===this.continueButton||/^(INPUT|TEXTAREA|SELECT|BUTTON)$/.test(e.target.tagName))return;
    e.preventDefault();this.keyHeld=true;this.pressTime=performance.now();if(this.timeline&&!this.reduced)this.timeline.held=true;
   });
   document.addEventListener('keyup',e=>{
    if(!this.active||e.code!=='Space'||e.target===this.continueButton||/^(INPUT|TEXTAREA|SELECT|BUTTON)$/.test(e.target.tagName))return;
    e.preventDefault();if(this.keyHeld&&this.reduced&&performance.now()-this.pressTime<350)this.nextStaticPage();this.release();
   });
   if(window.ResizeObserver)this.observer=new window.ResizeObserver(()=>{if(this.active)this.measure();});
   if(this.observer)this.observer.observe(this.stage);
  }
  async start(){
   this.cancel();const ticket=++this.serial;this.active=true;this.reduced=!!this.reducedMotion();this.layer.hidden=false;this.layer.style.opacity='0';this.continueButton.hidden=true;this.elapsedVisible=0;this.staticPage=0;this.staticSide='asteran';this.pendingSkip=false;
   if(!this.data||!Array.isArray(this.data.records)||!this.data.records.length){this.layer.hidden=true;this.active=false;throw new Error('Chronicle data missing');}
   this.build();
   if(document.fonts){try{await document.fonts.load('16px Galmuri14');await document.fonts.ready;}catch(_){}}
   if(ticket!==this.serial||!this.active)return;
   this.measure();this.timeline=new ChronicleTimeline(this.data,this.viewportLogicalHeight,this.contentLogicalHeight,this.rowCenters);
   this.layer.classList.toggle('static-record',this.reduced);
   this.hint.textContent=this.reduced?'화면을 누르면 다음 문단':'길게 누르면 빠르게';this.hint.hidden=false;this.hint.style.opacity='1';
   if(this.reduced){this.layer.style.opacity='1';this.renderStatic();}
   else {this.lastTime=null;this.render();this.schedule();}
   if(this.pendingSkip)this.skip();
  }
  build(){
   this.column.replaceChildren();this.rows=[];
   const entries=[{id:'heading',asteran:this.data.titles.asteran,higher:this.data.titles.higher},...this.data.records];
   entries.forEach((record,index)=>{
    const row=document.createElement('div');row.className='record-row'+(index===0?' record-heading':'');
    const a=document.createElement('p'),h=document.createElement('p');a.className='record-source';h.className='record-alt';a.textContent=record.asteran;h.textContent=record.higher;h.setAttribute('aria-hidden','true');
    row.append(a,h);this.column.append(row);this.rows.push({row,a,h,record});
   });
  }
  measure(){
   if(!this.rows.length)return;
   const factor=(this.data.referenceHeight||1920)/Math.max(1,this.stage.clientHeight);
   this.viewportLogicalHeight=this.viewport.clientHeight*factor;
   const font=this.stage.clientWidth*(45/1080),line=font*1.6,gap=this.stage.clientWidth*.085;
   let offset=0;this.rowCenters=[];
   for(let i=0;i<this.rows.length;i++){
    const {row,a,h,record}=this.rows[i];row.style.height='auto';
    // offsetHeight measures both font faces even when one has opacity zero.
    const fallback=Math.max(record.asteran.split('\n').length,record.higher.split('\n').length)*line;
    const height=Math.max(a.offsetHeight||0,h.offsetHeight||0,a.scrollHeight||0,h.scrollHeight||0,fallback)+(i===0?line*.7:0);
    row.style.height=height+'px';row.style.marginBottom=gap+'px';this.rowCenters.push((offset+height*.5)*factor);offset+=height+gap;
   }
   this.contentLogicalHeight=Math.max(this.column.scrollHeight||0,offset)*factor;
   if(this.timeline){this.timeline.setMetrics(this.viewportLogicalHeight,this.contentLogicalHeight,this.rowCenters);if(this.reduced)this.renderStatic();else this.render();}
  }
  schedule(){if(!this.active||this.reduced||document.hidden||!this.focused||this.frame||this.isDone())return;this.frame=requestAnimationFrame(now=>{this.frame=0;this.tick(now);});}
  tick(now){
   if(!this.active||this.reduced||document.hidden||!this.focused)return;
   const dt=this.lastTime===null?0:Math.min(.05,Math.max(0,(now-this.lastTime)/1000));this.lastTime=now;this.elapsedVisible+=dt;
   this.timeline.update(dt);this.render();this.schedule();
  }
  render(){
   if(!this.timeline)return;const s=this.timeline,scale=this.stage.clientHeight/(this.data.referenceHeight||1920);
   this.layer.style.opacity=s.phase==='intro'?String(clamp(s.progress)):'1';this.column.style.transform='translateY('+(s.y*scale)+'px)';
   this.hint.style.opacity=this.elapsedVisible>2.4?'0':'1';
   this.rows.forEach(({row,a,h},i)=>{
    const changed=s.changed(i),noise=s.noise(i);a.style.opacity=changed?'0':'1';h.style.opacity=changed?'1':'0';a.setAttribute('aria-hidden',String(changed));h.setAttribute('aria-hidden',String(!changed));
    row.classList.toggle('record-rewritten',changed);row.classList.toggle('record-glitch',noise);
    row.style.transform=noise?'translateX('+((Math.floor(s.progress*75+i*3)%3)-1)*3+'px)':'none';
   });
   this.continueButton.hidden=!this.isDone();if(this.isDone()){this.hint.hidden=true;this.timeline.held=false;}
   this.continueButton.disabled=this.pointerId!==null||this.keyHeld;
  }
  renderStatic(){
   this.column.style.transform='none';this.rows.forEach(({row,a,h},i)=>{
    row.hidden=i!==0&&i!==this.staticPage+1;row.classList.toggle('record-glitch',false);row.style.transform='none';const higher=this.staticSide==='higher';a.style.opacity=higher?'0':'1';h.style.opacity=higher?'1':'0';a.setAttribute('aria-hidden',String(higher));h.setAttribute('aria-hidden',String(!higher));
   });this.hint.hidden=this.isDone();this.continueButton.hidden=!this.isDone();if(this.isDone())this.rows.forEach(({row})=>row.hidden=true);
  }
  nextStaticPage(){
   if(!this.active||!this.reduced||this.isDone())return;
   if(this.staticPage+1<this.data.records.length)this.staticPage++;
   else if(this.staticSide==='asteran'){this.staticSide='higher';this.staticPage=0;}
   else {this.timeline.phase='done';this.timeline.finished=true;}
   this.renderStatic();
  }
  pointerDown(e){
   if(!this.active||this.isDone()||e.target===this.continueButton||(e.button!==undefined&&e.button!==0)||this.pointerId!==null)return;
   e.preventDefault();this.pointerId=e.pointerId;this.pressTime=performance.now();this.pressXY=[e.clientX,e.clientY];
   try{this.layer.setPointerCapture(e.pointerId);}catch(_){}
   if(this.timeline&&!this.reduced)this.timeline.held=true;
  }
  pointerUp(e){
   if(e.pointerId!==this.pointerId)return;
   const short=performance.now()-this.pressTime<350,still=Math.hypot(e.clientX-this.pressXY[0],e.clientY-this.pressXY[1])<12;
   this.release();if(this.reduced&&short&&still)this.nextStaticPage();
  }
  release(){
   const id=this.pointerId;this.pointerId=null;this.keyHeld=false;if(this.timeline)this.timeline.held=false;
   this.continueButton.disabled=false;
   if(id!==null){try{if(this.layer.hasPointerCapture(id))this.layer.releasePointerCapture(id);}catch(_){}}
  }
  isDone(){return !!this.timeline&&this.timeline.phase==='done';}
  skip(){if(!this.active)return;if(!this.timeline){this.pendingSkip=true;return;}this.release();this.timeline.skip();if(this.frame)cancelAnimationFrame(this.frame);this.frame=0;if(this.reduced)this.renderStatic();else this.render();}
  result(){return {finished:!!this.timeline&&this.timeline.finished,skipped:!!this.timeline&&this.timeline.skipped};}
  cancel(){this.serial++;this.release();if(this.frame)cancelAnimationFrame(this.frame);this.frame=0;this.lastTime=null;this.active=false;this.timeline=null;this.layer.hidden=true;this.continueButton.hidden=true;}
  snapshot(){return {active:this.active,reduced:this.reduced,staticPage:this.staticPage,staticSide:this.staticSide,...(this.timeline?this.timeline.snapshot():{}),pendingFrame:!!this.frame};}
 }
 window.ChronicleTimeline=ChronicleTimeline;window.ChroniclePlayer=ChroniclePlayer;
})();
