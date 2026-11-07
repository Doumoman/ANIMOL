/* Starway Ranch v0.28. Dialogue text is read only from data/ranch_dialogue.js.
   Offline-safe: no fetch, no network fonts, no third-party runtime. */
(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const assetUrl = path => window.RANCH_ART[path] || path;
  const data = window.RANCH_DIALOGUE;
  if (!data || !Array.isArray(data.scenes) || !data.scenes.length) {
    $('placeholder').textContent = '대사 파일을 찾을 수 없습니다. 폴더 전체를 압축 해제한 뒤 다시 열어 주세요.';
    $('line').textContent = 'data/ranch_dialogue.js 파일이 필요합니다.';
    return;
  }
  const scenes = data.scenes;
  const els = Object.fromEntries(['stage','world','plate','stars','runner','friends','isolHand','boarRush','anticipation','curtain','placeholder','speaker','line','skip','dialogue','dialogueWrap','actionButton','completion','reduce'].map(id=>[id,$(id)]));
  const pulses = [...document.querySelectorAll('.pulse')];
  const eyes = [...document.querySelectorAll('.eye')];
  const pops = [$('popIsol'),$('popTori')];
  const ctx = els.stars.getContext('2d',{alpha:true});
  const clamp = (x,a=0,b=1)=>Math.max(a,Math.min(b,x));
  const lerp = (a,b,t)=>a+(b-a)*t;
  const ease = x=>1-Math.pow(1-clamp(x),3);
  const imageCache = new Map();
  let sceneIndex=0, lineIndex=0, epoch=0, completed=false, busyAction=false;
  let starProgress=0, starPhase=0, soundEnabled=false, audioContext=null;
  const frames=new Set(), voices=new Set();
  const media = window.matchMedia('(prefers-reduced-motion: reduce)');
  els.reduce.checked=media.matches;
  const current=()=>scenes[sceneIndex].lines[lineIndex];
  let lastChronicleResult=null,chronicleSkipRequested=false;
  const chronicle=new window.ChroniclePlayer({stage:els.stage,layer:$('chronicleLayer'),viewport:$('chronicleViewport'),column:$('chronicleColumn'),hint:$('chronicleHint'),continueButton:$('chronicleContinue'),data:window.RANCH_CHRONICLE,reducedMotion:()=>els.reduce.checked,onContinue:result=>{lastChronicleResult=result;completed=true;resetFx();refreshUI();}});
  const inChronicle=()=>current().action==='play_chronicle';

  function imageReady(path){
    if(!imageCache.has(path)) imageCache.set(path,new Promise(resolve=>{
      const im=new Image(); im.onload=()=>resolve(true);im.onerror=()=>resolve(false);im.src=assetUrl(path);
    }));
    return imageCache.get(path);
  }
  function stopAudio(){
    for(const source of voices){try{source.stop();source.disconnect();}catch(_){} }
    voices.clear();
  }
  function cancelTimeline(){
    epoch++; for(const id of frames)cancelAnimationFrame(id);frames.clear();stopAudio();
    return epoch;
  }
  function resetFx(){
    chronicle.cancel();chronicleSkipRequested=false;
    cancelTimeline();
    for(const el of [els.runner,els.friends,els.isolHand,els.boarRush,els.stars,...pops])el.hidden=true;
    els.world.style.transform='none';els.plate.style.transform='none';els.speaker.style.transform='none';
    els.runner.style.opacity='0';els.runner.style.transform='none';els.boarRush.style.opacity='0';els.boarRush.style.transform='none';
    els.curtain.style.opacity='0';els.anticipation.style.opacity='0';
    els.friends.style.transform='translateY(-6%) scale(.94)';els.friends.style.opacity='1';
    for(const el of [...pulses,...eyes,...pops]){el.style.opacity='0';if(!eyes.includes(el))el.style.transform='none';}
    if(ctx)ctx.clearRect(0,0,els.stars.width,els.stars.height);
    starProgress=0;starPhase=0;busyAction=false;
    return epoch;
  }
  function animate(ms,token,draw,done){
    if(token!==epoch)return;
    if(els.reduce.checked){draw(1,ms);if(done)done();return;}
    let start=null;
    function frame(now){
      if(token!==epoch)return;
      if(start===null)start=now;
      const p=clamp((now-start)/ms);draw(p,now-start);
      if(p<1){const id=requestAnimationFrame(t=>{frames.delete(id);frame(t);});frames.add(id);}
      else if(done)done();
    }
    const id=requestAnimationFrame(t=>{frames.delete(id);frame(t);});frames.add(id);
  }
  async function setPlate(shot,token,override){
    const requested=override||'art/'+shot+'.png';
    let path=requested,ok=await imageReady(path);
    if(token!==epoch)return false;
    if(!ok&&requested!=='art/'+scenes[sceneIndex].id+'.png'){
      path='art/'+scenes[sceneIndex].id+'.png';ok=await imageReady(path);
    }
    if(token!==epoch)return false;
    els.plate.hidden=!ok;els.placeholder.hidden=ok;
    if(ok)els.plate.src=assetUrl(path);
    else els.placeholder.textContent='장면 이미지가 없습니다: '+shot;
    return ok;
  }
  function activeGate(){return !completed&&!!current().action;}
  function refreshUI(){
    const scene=scenes[sceneIndex],line=current(),shot=line.shot||scene.id;
    els.skip.hidden=completed;els.skip.disabled=busyAction;
    els.speaker.textContent=line.speaker||'기록';els.line.textContent=line.text;
    $('title').textContent=scene.id+' · '+scene.title;$('description').textContent=scene.summary;
    els.dialogue.setAttribute('aria-label',line.action?'화면 위의 행동 버튼으로 진행':'다음 대사');
    els.dialogue.classList.toggle('await-action',!!line.action);
    els.dialogueWrap.hidden=inChronicle()||!$('showDialogue').checked;
    els.actionButton.hidden=!line.action||inChronicle();
    els.actionButton.textContent=line.action==='block_lamp'?'등불을 가리고 이솔을 부른다':line.action==='chapter_complete'?'이 밤의 기록을 마친다':'';
    els.actionButton.disabled=false;
    $('nextLine').disabled=!!line.action||completed;
    $('prevLine').disabled=sceneIndex===0&&lineIndex===0;
    $('prev').disabled=sceneIndex===0;$('next').disabled=sceneIndex===scenes.length-1;
    [...$('scenes').children].forEach((b,i)=>b.setAttribute('aria-pressed',String(i===sceneIndex)));
    $('shots').replaceChildren();
    const seen=new Set();scene.lines.forEach((l,i)=>{
      const key=l.shot||scene.id;if(seen.has(key))return;seen.add(key);
      const b=document.createElement('button');b.type='button';b.className='shot';b.textContent=key;
      b.setAttribute('aria-pressed',String(key===shot));b.onclick=()=>selectLine(i);$('shots').append(b);
    });
    els.completion.hidden=!completed;
  }
  function speakerEmphasis(token){
    if(els.reduce.checked||!['R01','R05','R06','R08','R10','R13','R17'].includes(scenes[sceneIndex].id))return;
    // Only a label moves: flattened CG art is never bounced to fake actor animation.
    animate(210,token,p=>{const lift=-2*Math.sin(p*Math.PI);els.speaker.style.transform='translateY('+lift+'px)';},()=>els.speaker.style.transform='none');
  }
  async function renderLine(){
    const token=resetFx();completed=false;refreshUI();
    const l=current(),shot=l.shot||scenes[sceneIndex].id;
    const previousShot=lineIndex?scenes[sceneIndex].lines[lineIndex-1].shot:'';
    const firstOfShot=shot!==previousShot;
    let override=l.cue==='boar_charge'?'art/R12.png':'';
    if(shot==='R08'&&(l.cue==='village_chaos'||firstOfShot))override='layers/R08_FrontPlate.png';
    if(shot==='R15'){
      const layered=await Promise.all(['layers/R15_BG.png','layers/R15_Friends.png','layers/R15_IsolHand.png'].map(imageReady));
      if(token!==epoch)return;
      if(layered.every(Boolean)){
        override='layers/R15_BG.png';els.friends.src=assetUrl('layers/R15_Friends.png');els.friends.hidden=false;
        els.isolHand.src=assetUrl('layers/R15_IsolHand.png');els.isolHand.hidden=false;
        els.stars.hidden=false;
        if((!firstOfShot||scenes[sceneIndex].id!=='R15')&&l.cue!=='spatial_recede')applyFriends(1);
      }
    }
    await setPlate(shot,token,override);
    if(token!==epoch)return;
    if(inChronicle()){lastChronicleResult=null;chronicle.start().catch(()=>{$('notice').textContent='기록문 데이터를 읽을 수 없습니다. 전체 폴더를 다시 확인해 주세요.';});if(chronicleSkipRequested)chronicle.skip();return;}
    // Combat starts and remains in blackout. Individual impact cues never replace dialogue.
    if(shot==='R12B'&&l.cue!=='boar_charge')els.curtain.style.opacity='1';
    if(shot==='R15'&&(!firstOfShot||scenes[sceneIndex].id!=='R15')&&l.cue!=='spatial_recede')drawStars(1,0);
    speakerEmphasis(token);
    const cue=l.cue||(shot==='R08'&&firstOfShot?'village_chaos':shot==='R08B'&&firstOfShot?'runner_reverse':shot==='R15'&&firstOfShot&&scenes[sceneIndex].id==='R15'?'spatial_recede':'');
    playCue(cue,token,shot);
  }
  function selectScene(index){sceneIndex=clamp(index,0,scenes.length-1);lineIndex=0;renderLine();}
  function selectLine(index){lineIndex=clamp(index,0,scenes[sceneIndex].lines.length-1);renderLine();}
  function advance(){
    if(activeGate()||busyAction||completed)return false;
    if(lineIndex<scenes[sceneIndex].lines.length-1)selectLine(lineIndex+1);
    else if(sceneIndex<scenes.length-1)selectScene(sceneIndex+1);
    else {completed=true;resetFx();refreshUI();}
    return true;
  }
  function skipCurrentScene(){
    if(completed||busyAction)return false;
    if(inChronicle()){if(chronicle.active)chronicle.skip();else chronicleSkipRequested=true;return false;}
    const lines=scenes[sceneIndex].lines;
    // Skip dialogue only; required input lines must still be read and acted on.
    for(let i=lineIndex;i<lines.length;i++){
      if(lines[i].action){if(i!==lineIndex)selectLine(i);return false;}
    }
    if(sceneIndex<scenes.length-1)selectScene(sceneIndex+1);
    else {completed=true;resetFx();refreshUI();}
    return true;
  }
  function previous(){
    if(lineIndex>0)selectLine(lineIndex-1);
    else if(sceneIndex>0){sceneIndex--;lineIndex=scenes[sceneIndex].lines.length-1;renderLine();}
  }
  function performAction(){
    if(busyAction||completed)return;
    const action=current().action;if(!action)return;
    if(action==='play_chronicle'){if(chronicle.isDone())$('chronicleContinue').onclick();return;}
    if(action==='chapter_complete'){resetFx();completed=true;refreshUI();return;}
    if(action==='block_lamp'){
      // Stop the previous timeline without dismantling the current R15 composition.
      // Friends, foreground hand and constellation layers stay visible until the R16 cut.
      const token=cancelTimeline();busyAction=true;els.actionButton.disabled=true;els.skip.disabled=true;
      // A deliberate action, separate from the dialogue surface. Short shutter of starlight.
      animate(420,token,p=>{els.anticipation.style.opacity=String(ease(p)*.72);},()=>{
        if(token!==epoch)return;busyAction=false;
        if(lineIndex<scenes[sceneIndex].lines.length-1)selectLine(lineIndex+1);
        else selectScene(sceneIndex+1);
      });
    }
  }
  function playCue(cue,token,shot){
    if(!cue)return;
    if(cue==='village_chaos'){
      if(els.reduce.checked)return;
      imageReady('layers/R08_IsolRun.png').then(ok=>{
        if(!ok||token!==epoch)return;els.runner.src=assetUrl('layers/R08_IsolRun.png');els.runner.hidden=false;
        sfx('run');animate(780,token,p=>{els.runner.style.opacity=String(Math.min(1,p*12,(1-p)*12));els.runner.style.transform='translateX('+lerp(-30,235,ease(p))+'%)';},()=>els.runner.hidden=true);
      });return;
    }
    if(cue==='runner_reverse'){/* Reverse angle is supplied by R08B, never mirrored. */return;}
    if(cue==='rustle'){
      sfx('rustle');animate(700,token,p=>{
        els.anticipation.style.opacity=String(.25+.42*Math.sin(p*Math.PI));
        if(!els.reduce.checked)els.plate.style.transform='translateX('+(Math.sin(p*24)*.3*Math.sin(p*Math.PI))+'%) scale(1.008)';
      },()=>{els.plate.style.transform='none';els.anticipation.style.opacity='.25';});return;
    }
    if(cue==='eyes_glint'){
      sfx('glint');
      if(shot==='R12A'||shot==='R12'){
        const positions=shot==='R12A'?[[70.271,33.914],[76.340,33.364]]:[[69.8,20.175],[79.15,18.985]];
        eyes.forEach((el,i)=>{el.style.left=(positions[i][0]-1.4)+'%';el.style.top=(positions[i][1]-.7)+'%';el.style.width='2.8%';el.style.height='1.4%';});
        animate(150,token,p=>eyes.forEach(el=>el.style.opacity=String(Math.sin(p*Math.PI))),()=>eyes.forEach(el=>el.style.opacity='0'));
      }
      else animate(150,token,p=>{els.anticipation.style.opacity=String(.42*(1-Math.sin(p*Math.PI)));},()=>els.anticipation.style.opacity='0');
      return;
    }
    if(cue==='boar_reveal'){
      pops.forEach(p=>p.hidden=false);sfx('reveal');
      animate(460,token,p=>pops.forEach((pop,i)=>{const v=clamp((p-i*.22)/.55);pop.style.opacity=String(v);pop.style.transform=els.reduce.checked?'none':'scale('+lerp(.82,1,ease(v))+')';}));return;
    }
    if(cue==='boar_charge'){
      if(els.reduce.checked){els.curtain.style.opacity='1';sfx('charge');return;}
      imageReady('layers/R12_BoarRush.png').then(ok=>{
        if(token!==epoch)return;
        if(ok){els.boarRush.src=assetUrl('layers/R12_BoarRush.png');els.boarRush.hidden=false;}
        sfx('charge');animate(350,token,p=>{
          els.boarRush.style.opacity=String(clamp(p*8));els.boarRush.style.transform='translate('+lerp(28,-20,p)+'%,'+lerp(-12,24,p)+'%) scale('+lerp(.55,1.8,ease(p))+')';
          els.curtain.style.opacity=String(clamp((p-.68)/.32));
          els.world.style.transform='translateY('+(-.6*Math.sin(p*20))+'%)';
        },()=>{els.boarRush.hidden=true;els.curtain.style.opacity='1';els.world.style.transform='none';});
      });return;
    }
    if(['impact_left','impact_right','impact_final'].includes(cue)){
      const i=['impact_left','impact_right','impact_final'].indexOf(cue);els.curtain.style.opacity='1';
      const pulse=pulses[i];sfx(cue);
      animate(i===2?430:330,token,(p,ms)=>{
        // The first 65ms hold the cutout impact at one pose; localized effect only.
        const q=clamp((ms-65)/(i===2?365:265));
        pulse.style.opacity=els.reduce.checked?'.78':String(1-q);
        pulse.style.transform=els.reduce.checked?'none':'rotate('+(i===1?-16:12)+'deg) scale('+lerp(.85,i===2?1.45:1.2,ease(q))+')';
        if(!els.reduce.checked)els.world.style.transform='translate('+(Math.sin(q*35)*.65*(1-q))+'%,'+(Math.cos(q*30)*.42*(1-q))+'%)';
      },()=>{els.world.style.transform='none';if(!els.reduce.checked)pulse.style.opacity='0';});return;
    }
    if(cue==='boar_settle'){
      // A quiet second of stillness; the player retains control of the line.
      animate(1000,token,p=>{els.anticipation.style.opacity=String(lerp(.22,0,p));});return;
    }
    if(cue==='spatial_recede'){
      animate(3600,token,p=>{applyFriends(ease(p));drawStars(ease(p),p*2.2);});return;
    }
    if(cue==='return_grasp'){
      // Static close-up is sufficient; no victory shake or cheerful pop during rescue.
      animate(500,token,p=>els.anticipation.style.opacity=String(.18*(1-p)));return;
    }
  }
  function applyFriends(p){
    if(!els.friends.hidden){els.friends.style.transform='translateY('+lerp(-6,-11,p)+'%) scale('+lerp(.94,.56,p)+')';els.friends.style.opacity='1';}
  }
  function seeded(i){const x=Math.sin((i+93)*87.211)*43758.5453;return x-Math.floor(x);}
  const planes=[{count:11,depth:.27,color:'#bcbefa'},{count:9,depth:.62,color:'#8edcff'},{count:7,depth:1,color:'#d9f8ff'}].map((plane,j)=>({...plane,points:Array.from({length:plane.count},(_,i)=>{
    const n=i+j*31;let x=.07+.86*seeded(n*4),y=.10+.66*seeded(n*4+1);if(x>.30&&x<.72&&y>.23&&y<.57)x=i%2?.23:.78;return{x,y,r:seeded(n*4+2),phase:seeded(n*4+3)};
  })}));
  const links=[[[0,1],[4,5],[6,8]],[[0,3],[3,4],[5,6]],[[0,1],[4,6]]];
  function drawStars(progress,phase){
    starProgress=progress;starPhase=phase;if(!ctx||els.stars.hidden)return;
    const w=els.stage.clientWidth,h=els.stage.clientHeight,ratio=Math.min(window.devicePixelRatio||1,2);if(!w||!h)return;
    if(els.stars.width!==Math.round(w*ratio)||els.stars.height!==Math.round(h*ratio)){els.stars.width=Math.round(w*ratio);els.stars.height=Math.round(h*ratio);}
    ctx.setTransform(ratio,0,0,ratio,0,0);ctx.clearRect(0,0,w,h);
    planes.forEach((plane,j)=>{
      const depth=plane.depth,drift=progress*depth,scale=1+drift*.29;
      const points=plane.points.map(p=>({x:(.50+(p.x-.50)*scale-.013*drift)*w,y:(.44+(p.y-.44)*scale+.026*drift)*h,r:p.r,phase:p.phase}));
      ctx.strokeStyle=plane.color;ctx.lineWidth=Math.max(.7,w*.002)*(.74+depth*.45);ctx.globalAlpha=lerp(.16,.42,progress)*(1-depth*.20);
      for(const [a,b] of links[j]){ctx.beginPath();ctx.moveTo(points[a].x,points[a].y);ctx.lineTo(points[b].x,points[b].y);ctx.stroke();}
      ctx.fillStyle=plane.color;for(const p of points){const r=Math.max(1,w*.006)*lerp(.52,1.4,p.r)*(.66+depth*.48);ctx.globalAlpha=(.41+.43*progress)*(.80+.17*Math.sin(phase*2+p.phase*7));ctx.beginPath();ctx.moveTo(p.x,p.y-r*1.6);ctx.lineTo(p.x+r*.7,p.y);ctx.lineTo(p.x,p.y+r*1.6);ctx.lineTo(p.x-r*.7,p.y);ctx.closePath();ctx.fill();}
    });ctx.globalAlpha=1;
  }
  function sfx(kind){
    if(!soundEnabled||!audioContext||audioContext.state!=='running')return;
    const now=audioContext.currentTime;
    const settings={rustle:[90,48,.30,.035,'sawtooth'],glint:[720,950,.12,.025,'sine'],reveal:[170,85,.23,.04,'triangle'],charge:[145,38,.32,.055,'sawtooth'],impact_left:[110,35,.12,.065,'triangle'],impact_right:[155,42,.13,.065,'triangle'],impact_final:[78,24,.24,.075,'triangle'],run:[70,45,.25,.025,'triangle']};
    const a=settings[kind];if(!a)return;
    const o=audioContext.createOscillator(),g=audioContext.createGain();o.type=a[4];o.frequency.setValueAtTime(a[0],now);o.frequency.exponentialRampToValueAtTime(a[1],now+a[2]);g.gain.setValueAtTime(.0001,now);g.gain.linearRampToValueAtTime(a[3],now+.008);g.gain.exponentialRampToValueAtTime(.0001,now+a[2]);o.connect(g);g.connect(audioContext.destination);voices.add(o);o.onended=()=>{voices.delete(o);o.disconnect();g.disconnect();};o.start(now);o.stop(now+a[2]+.02);
  }
  async function toggleSound(){
    soundEnabled=!soundEnabled;
    if(soundEnabled){
      const Audio=window.AudioContext||window.webkitAudioContext;
      if(!Audio){soundEnabled=false;$('notice').textContent='이 브라우저는 임시 효과음 재생을 지원하지 않습니다.';}
      else {audioContext=audioContext||new Audio();try{await audioContext.resume();}catch(_){soundEnabled=false;}}
    }
    if(!soundEnabled)stopAudio();$('sound').setAttribute('aria-pressed',String(soundEnabled));$('sound').textContent='임시 효과음: '+(soundEnabled?'켬':'끔');
  }
  scenes.forEach((scene,i)=>{const b=document.createElement('button');b.type='button';b.className='scene';b.textContent=scene.id;b.setAttribute('aria-label',scene.id+' '+scene.title);b.onclick=()=>selectScene(i);$('scenes').append(b);});
  $('prev').onclick=()=>selectScene(sceneIndex-1);$('next').onclick=()=>selectScene(sceneIndex+1);
  $('prevLine').onclick=previous;$('nextLine').onclick=advance;els.dialogue.onclick=advance;
  $('replay').onclick=()=>selectLine(0);$('restart').onclick=()=>selectScene(0);els.actionButton.onclick=performAction;
  $('sound').onclick=toggleSound;els.skip.onclick=skipCurrentScene;
  els.reduce.onchange=()=>renderLine();
  $('showDialogue').onchange=e=>{els.dialogueWrap.hidden=inChronicle()||!e.target.checked;};
  document.addEventListener('keydown',e=>{
    if(e.altKey||e.ctrlKey||e.metaKey||e.repeat||/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName))return;
    if(e.key==='ArrowLeft'){e.preventDefault();selectScene(sceneIndex-1);}
    if(e.key==='ArrowRight'){e.preventDefault();selectScene(sceneIndex+1);}
    if(e.key===' '&&!inChronicle()&&e.target.tagName!=='BUTTON'){e.preventDefault();advance();}
    if(e.key.toLowerCase()==='r'){e.preventDefault();selectLine(0);}
  });
  window.addEventListener('pagehide',()=>{resetFx();if(audioContext)audioContext.close();});
  // BFCache restores the DOM after pagehide cancelled its players. Restart only the current unfinished line.
  window.addEventListener('pageshow',event=>{if(!event.persisted)return;if(completed)refreshUI();else renderLine();});
  if(window.ResizeObserver)new window.ResizeObserver(()=>drawStars(starProgress,starPhase)).observe(els.stage);
  // Test/inspection hooks return state only; they also make editorial navigation reproducible.
  window.RANCH_PREVIEW={selectScene,selectLine,advance,previous,performAction,skipCurrentScene,chronicle,replay:()=>selectLine(0),snapshot:()=>({scene:scenes[sceneIndex].id,lineIndex,lineId:current().id,shot:current().shot,action:current().action||null,cue:current().cue||'',pendingFrames:frames.size,pendingVoices:voices.size,completed,busyAction,chronicle:chronicle.snapshot(),chronicleResult:lastChronicleResult})};
  selectScene(0);
})();
