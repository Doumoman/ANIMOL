/* Runtime-state/DOM harness, not a rendered browser or Unity test. */
'use strict';
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../../Docs/Reference/StarwayRanch/Preview');
const dataRoot=path.resolve(__dirname,'../../Assets/ANIMOL/Data/StarwayRanch/Story');
const html=fs.readFileSync(path.join(root,'index.html'),'utf8');
const css=fs.readFileSync(path.join(root,'preview.css'),'utf8');
const js=fs.readFileSync(path.join(root,'preview.js'),'utf8');
const data=JSON.parse(fs.readFileSync(path.join(dataRoot,'ranch_dialogue.json'),'utf8'));
class Element{
  constructor(tag='DIV'){this.tagName=tag;this.children=[];this.attributes={};this.style={};this.hidden=false;this.checked=false;this.disabled=false;this.textContent='';this.clientWidth=405;this.clientHeight=720;this.width=405;this.height=720;this.classes=new Set();this.classList={toggle:(key,on)=>on?this.classes.add(key):this.classes.delete(key)};}
  append(...children){this.children.push(...children);}
  get offsetHeight(){if(this.style.height&&this.style.height!=='auto')return parseFloat(this.style.height);return this.textContent?this.textContent.split('\n').length*27:0;}
  get scrollHeight(){return this.children.length?this.children.reduce((n,c)=>n+c.offsetHeight+(parseFloat(c.style.marginBottom)||0),0):this.offsetHeight;}
  setPointerCapture(id){this.captured=id;}
  hasPointerCapture(id){return this.captured===id;}
  releasePointerCapture(){this.captured=null;}
  replaceChildren(...children){this.children=children;}
  setAttribute(k,v){this.attributes[k]=v;}
  getAttribute(k){return this.attributes[k];}
  set src(value){this.attributes.src=value;}
  get src(){return this.attributes.src||'';}
  getContext(){return drawing;}
}
const drawing=new Proxy({},{get:(target,key)=>key in target?target[key]:()=>{}});
const els=Object.fromEntries([...html.matchAll(/id="([^\"]+)"/g)].map(m=>[m[1],new Element()]));
els.showDialogue.checked=true;els.plate.hidden=true;els.chronicleViewport.clientHeight=576;
const pulses=Array.from({length:3},()=>new Element()),eyes=Array.from({length:2},()=>new Element());
const frames=new Map(),loadedPaths=new Set();let time=0,frameId=0;
let audioConstructed=0;
const parameter=()=>({setValueAtTime(){},linearRampToValueAtTime(){},exponentialRampToValueAtTime(){}});
class FakeAudio{
  constructor(){audioConstructed++;this.state='suspended';this.currentTime=0;this.destination={};}
  async resume(){this.state='running';}
  close(){this.state='closed';}
  createGain(){return{gain:parameter(),connect(){},disconnect(){}};}
  createOscillator(){return{frequency:parameter(),connect(){},disconnect(){},start(){},stop(time){if(time===undefined)this.onended?.();}};}
}
const windowEvents={},documentEvents={};
const documentStub={hidden:false,getElementById:id=>els[id],querySelectorAll:s=>s==='.pulse'?pulses:s==='.eye'?eyes:[],createElement:tag=>new Element(tag.toUpperCase()),addEventListener:(name,fn)=>(documentEvents[name]??=[]).push(fn)};
const context=vm.createContext({console,performance:{now:()=>time},Image:class{set src(p){loadedPaths.add(p);queueMicrotask(()=>fs.existsSync(path.join(root,p))?this.onload?.():this.onerror?.());}},requestAnimationFrame:fn=>{frames.set(++frameId,fn);return frameId;},cancelAnimationFrame:id=>frames.delete(id),document:documentStub,window:{RANCH_DIALOGUE:data,devicePixelRatio:1,AudioContext:FakeAudio,matchMedia:()=>({matches:false}),addEventListener:(name,fn)=>(windowEvents[name]??=[]).push(fn)}});
for(const file of ['../../../../Assets/ANIMOL/Data/StarwayRanch/Story/ranch_chronicle.js','asset-paths.js','chronicle.js','preview.js'])vm.runInContext(fs.readFileSync(path.join(root,file),'utf8'),context,{filename:'ranch_v028/'+file});
function fire(list,name,event={}){for(const fn of list[name]||[])fn(event);}
function pointer(id=1){return {pointerId:id,button:0,target:els.chronicleLayer,clientX:200,clientY:300,preventDefault(){}};}
const api=context.window.RANCH_PREVIEW;
async function settle(){for(let i=0;i<12;i++)await Promise.resolve();}
async function tick(ms){await settle();for(let elapsed=0;elapsed<ms;elapsed+=1000/60){time+=1000/60;const batch=[...frames.values()];frames.clear();batch.forEach(fn=>fn(time));await settle();}}
function position(sceneId,lineId){const i=data.scenes.findIndex(s=>s.id===sceneId);api.selectScene(i);api.selectLine(data.scenes[i].lines.findIndex(l=>l.id===lineId));}
function cuePosition(cue){for(const s of data.scenes){const l=s.lines.find(l=>l.cue===cue);if(l){position(s.id,l.id);return l;}}throw Error('cue missing: '+cue);}
(async()=>{
 const checks=[];
 await settle();assert.equal(els.scenes.children.length,17);assert(html.indexOf('../../../../Assets/ANIMOL/Data/StarwayRanch/Story/ranch_dialogue.js')<html.indexOf('src="preview.js"'));assert(!/fetch\(/.test(js));
 for(let s=0;s<data.scenes.length;s++){
   api.selectScene(s);await settle();
   const scene=data.scenes[s];
   for(let l=0;l<scene.lines.length;l++){
     api.selectLine(l);await settle();assert.equal(els.line.textContent,scene.lines[l].text);assert.equal(els.speaker.textContent,scene.lines[l].speaker||'기록');assert.equal(api.snapshot().lineId,scene.lines[l].id);assert.equal(els.plate.hidden,false,'plate exists: '+scene.lines[l].shot);
   }
 }
 checks.push('All '+data.scenes.reduce((n,s)=>n+s.lines.length,0)+' exact source lines and all17 parent scenes reachable, including every insert shot.');
 // Playback reaches each line in data order; no effects automatically advance a line.
 api.selectScene(0);await settle();let walked=0;
 for(const s of data.scenes)for(const line of s.lines){
   assert.equal(api.snapshot().lineId,line.id);assert.equal(els.line.textContent,line.text);walked++;
   await tick(30);assert.equal(api.snapshot().lineId,line.id);
   if(line.action){assert.equal(api.advance(),false);if(line.action==='play_chronicle')api.chronicle.skip();api.performAction();await tick(600);}
   else {assert.equal(api.advance(),true);await settle();}
 }
 assert.equal(api.snapshot().completed,true);checks.push('Sequential traversal presents all '+walked+' lines without automatic skipping and explicitly gates both actions.');
 // New top-right skip: skips one scene, cancels effects, preserves mandatory inputs.
 api.selectScene(0);await settle();els.skip.onclick();await settle();assert.equal(api.snapshot().scene,'R02');
 cuePosition('boar_charge');await tick(90);els.skip.onclick();await tick(800);assert.equal(api.snapshot().scene,'R13');assert.equal(els.boarRush.hidden,true);assert.equal(els.curtain.style.opacity,'0');assert.equal(api.snapshot().pendingVoices,0);
 api.selectScene(15);await settle();els.skip.onclick();await settle();assert.equal(api.snapshot().action,'block_lamp');const skipGateId=api.snapshot().lineId;els.skip.onclick();await settle();assert.equal(api.snapshot().lineId,skipGateId);
 api.selectScene(16);await settle();els.skip.onclick();await settle();assert.equal(api.snapshot().action,'play_chronicle');assert.equal(api.snapshot().completed,false);els.skip.onclick();await settle();assert.equal(api.snapshot().chronicle.skipped,true);assert.equal(api.snapshot().completed,false);api.performAction();await settle();assert.equal(api.snapshot().completed,true);assert.equal(els.skip.hidden,true);
 assert(html.includes('id="skip"'));assert(html.includes('&gt;&gt;'));assert(!html.includes('id="badge"'));assert(css.includes('font-family:Galmuri14'));assert(css.includes('../../../../Assets/ANIMOL/Fonts/Notebook/Galmuri14.woff2'));assert(fs.existsSync(path.join(root,'../../../../Assets/ANIMOL/Fonts/Notebook/Galmuri14.woff2')));assert(css.includes('min-width:44px'));assert(css.includes('font-synthesis:none'));
 checks.push('Top-right >> skips one scene; active combat effects cancel; both mandatory actions remain gated; scene badge removed and local Galmuri14 supplied.');
 // R16 cannot be advanced by the dialogue button or helper, only the separate action.
 const block=data.scenes.flatMap(s=>s.lines).find(l=>l.action==='block_lamp');position('R16',block.id);await settle();
 const gatedId=api.snapshot().lineId;assert.equal(els.actionButton.hidden,false);assert.equal(els.nextLine.disabled,true);els.dialogue.onclick();await tick(700);assert.equal(api.snapshot().lineId,gatedId);
 const preActionComposition={friends:els.friends.style.transform,hand:JSON.stringify(els.isolHand.style)};
 assert.equal(els.friends.hidden,false);assert.equal(els.isolHand.hidden,false);assert.equal(els.stars.hidden,false);
 api.performAction();assert.equal(api.snapshot().busyAction,true);assert.equal(api.advance(),false);
 await tick(180);assert.equal(api.snapshot().lineId,gatedId);assert.equal(els.friends.hidden,false);assert.equal(els.isolHand.hidden,false);assert.equal(els.stars.hidden,false);assert.equal(els.friends.style.transform,preActionComposition.friends);assert.equal(JSON.stringify(els.isolHand.style),preActionComposition.hand);
 await tick(600);assert.notEqual(api.snapshot().lineId,gatedId);checks.push('block_lamp uses a separate action, preserves friends/hand/stars through the420ms lamp fade, and cannot be bypassed through normal dialogue/Space.');
 // R08 pass and separate reverse angle; next shot only follows source-line navigation.
 const run=cuePosition('village_chaos');await settle();await tick(300);assert.equal(els.runner.hidden,false);assert.equal(api.snapshot().lineId,run.id);await tick(800);assert.equal(els.runner.hidden,true);assert.equal(api.snapshot().lineId,run.id);
 cuePosition('runner_reverse');await settle();assert.equal(els.plate.src,context.window.RANCH_ART['art/R08B.png']);assert.equal(els.runner.hidden,true);checks.push('R08 runner crosses in its cue; reverse shot is authored R08B, without auto-skipping dialogue.');
 // Boar: buildup, fitted eyes, distinct popups, charge blackout, three localized hits, recovery.
 cuePosition('rustle');await tick(900);assert.equal(els.plate.src,context.window.RANCH_ART['art/R12A.png']);
 cuePosition('eyes_glint');await tick(80);assert(eyes.some(e=>Number(e.style.opacity)>0));await tick(300);assert(eyes.every(e=>e.style.opacity==='0'));
 cuePosition('boar_reveal');await tick(650);assert.equal(els.popIsol.hidden,false);assert.equal(els.popTori.hidden,false);assert.equal(els.popIsol.style.opacity,'1');
 cuePosition('boar_charge');await tick(200);assert.equal(els.boarRush.hidden,false);assert.equal(els.plate.src,context.window.RANCH_ART['art/R12.png']);await tick(400);assert.equal(els.boarRush.hidden,true);assert.equal(els.curtain.style.opacity,'1');
 for(const [i,cue]of ['impact_left','impact_right','impact_final'].entries()){cuePosition(cue);await tick(90);assert(Number(pulses[i].style.opacity)>0);assert(pulses.filter((_,j)=>i!==j).every(el=>el.style.opacity==='0'));assert.equal(els.curtain.style.opacity,'1');await tick(600);assert.equal(pulses[i].style.opacity,'0');assert.equal(els.world.style.transform,'none');}
 cuePosition('boar_settle');await tick(1200);assert.equal(els.plate.src,context.window.RANCH_ART['art/R12C.png']);assert.equal(els.curtain.style.opacity,'0');assert.equal(els.anticipation.style.opacity,'0');checks.push('Boar buildup → glints → two popups → lunge/blackout → three independent hits → red-tag recovery all run with complete resets.');
 // The hand never moves, friends recede in three star planes and remain fully colored.
 cuePosition('spatial_recede');await settle();const handStyle=JSON.stringify(els.isolHand.style);assert.equal(els.isolHand.hidden,false);await tick(3900);assert.equal(JSON.stringify(els.isolHand.style),handStyle);assert.equal(els.friends.style.opacity,'1');assert(els.friends.style.transform.includes('0.56'));assert.equal(els.stars.hidden,false);checks.push('R15 holds the foreground hand fixed, shrinks friends to0.56 and maintains their opacity1.');
 // Cancellation while a cue is loading/animating must not leak into another scene.
 for(const cue of ['rustle','boar_reveal','boar_charge','spatial_recede','village_chaos']){
   cuePosition(cue);await tick(90);api.selectScene(3);await tick(4500);assert.equal(api.snapshot().scene,'R04');assert.equal(els.plate.src,context.window.RANCH_ART['art/R04.png']);assert.equal(els.curtain.style.opacity,'0');assert.equal(els.runner.hidden,true);assert.equal(els.boarRush.hidden,true);assert.equal(els.stars.hidden,true);assert.equal(els.popIsol.hidden,true);assert.equal(api.snapshot().pendingFrames,0);assert.equal(api.snapshot().pendingVoices,0);assert.equal(els.world.style.transform,'none');
 }
 cuePosition('boar_charge');api.selectScene(0);api.selectScene(6);await tick(1000);assert.equal(api.snapshot().scene,'R07');assert.equal(els.plate.src,context.window.RANCH_ART['art/R07.png']);assert.equal(els.boarRush.hidden,true);checks.push('Rapid navigation/replay cancels RAF, asynchronous image continuations and temporary voices; no effects survive scene changes.');
 els.reduce.checked=true;els.reduce.onchange();await settle();
 for(const cue of ['rustle','eyes_glint','boar_reveal','boar_charge','impact_left','impact_right','impact_final','boar_settle','spatial_recede']){
   cuePosition(cue);await settle();assert.equal(api.snapshot().pendingFrames,0);assert.equal(els.world.style.transform,'none');assert.equal(els.speaker.style.transform,'none');
 }
 checks.push('Reduced-motion mode applies static outcomes with zero RAF and zero world/speaker movement.');
 assert.equal(audioConstructed,0,'audio remains silent before explicit user toggle');await els.sound.onclick();assert.equal(audioConstructed,1);cuePosition('boar_charge');await settle();assert(api.snapshot().pendingVoices>0);api.selectScene(0);await settle();assert.equal(api.snapshot().pendingVoices,0);await els.sound.onclick();cuePosition('impact_final');await settle();assert.equal(api.snapshot().pendingVoices,0);checks.push('Temporary audio context is created only on explicit toggle; scene changes stop voices and mute prevents new voices (stubbed node lifecycle).');
 const repairScene=data.scenes.find(s=>s.id==='R13'),repairIndex=repairScene.lines.findIndex(l=>l.shot==='R13B');assert.equal(repairIndex,6);api.selectScene(12);api.selectLine(repairIndex);await settle();assert.equal(els.plate.src,context.window.RANCH_ART['art/R13B.png']);assert(!html.includes('shotCaption'));assert(!html.includes('currentMeta'));assert(!html.includes('lineProgress'));checks.push('R13 keeps narration and five scolding lines before repair. In-game shot captions and dialogue counts are absent.');
 assert(css.includes('--ui-edge:#eceee7'));assert(css.includes('--ui-stroke:1.25px'));assert(css.includes('font-size:4.3cqw'));assert(!css.includes('text-shadow'));assert(!css.includes('text-stroke'));assert(html.indexOf('id="storyUI"')>html.indexOf('id="world"'));assert(!js.includes('els.storyUI.style.transform'));assert(!html.includes('No cameras rendering'));checks.push('Source confirms thin neutral neon tokens, plain speaker,4.3cqw text, no text stroke, fixed UI separate from world effects, no Unity warning text.');
 // Independent dual-record timeline: forward, downward rewrite, full higher account and explicit ending.
 els.reduce.checked=false;els.reduce.onchange();await settle();cuePosition('dual_record');await settle();
 assert.equal(els.plate.src,context.window.RANCH_ART['art/R17B.png']);assert.equal(els.dialogueWrap.hidden,true);assert.equal(els.actionButton.hidden,true);assert.equal(els.chronicleLayer.hidden,false);assert.equal(api.chronicle.rows.length,7);
 await tick(800);assert.equal(api.snapshot().chronicle.phase,'asteran');const earlyY=api.snapshot().chronicle.y;
 await tick(1000);assert(api.snapshot().chronicle.y<earlyY,'first account travels upward');
 // A held pointer changes elapsed timeline speed to exactly three times base speed.
 const model=api.chronicle.timeline,previous=model.progress;els.chronicleLayer.onpointerdown(pointer());assert.equal(model.speed,3);
 const duration=model.duration();model.update(.2);assert(Math.abs(model.progress-previous-.6/duration)<1e-8);els.chronicleLayer.onpointerup(pointer());assert.equal(model.speed,1);assert.equal(api.snapshot().completed,false);
 els.chronicleLayer.onpointerdown(pointer(2));els.chronicleLayer.onpointercancel();assert.equal(model.held,false);
 els.chronicleLayer.onpointerdown(pointer(3));fire(windowEvents,'blur');assert.equal(model.held,false);fire(windowEvents,'focus');
 // Hidden document cannot burn through the scroll, including a long tab switch.
 documentStub.hidden=true;fire(documentEvents,'visibilitychange');const paused=model.progress;await tick(2000);assert.equal(model.progress,paused);
 documentStub.hidden=false;fire(documentEvents,'visibilitychange');await tick(30);assert(model.progress-paused<.002);
 // Real measured dimensions can be replaced without resetting phase progress.
 const progress=model.progress;model.setMetrics(1536,4000,[100,650,1200,1750,2300,2850,3400]);assert.equal(model.progress,progress);assert.equal(model.phase,'asteran');
 model.update((1-model.progress)*model.duration()+.001);api.chronicle.render();assert.equal(model.phase,'pause');assert.equal(model.y,-model.contentHeight);
 model.update(model.duration()+.001);assert.equal(model.phase,'rewind');const top=model.y;model.update(.9);api.chronicle.render();assert(model.y>top,'same column rewinds downward');
 model.progress=(model.contentHeight+model.viewportHeight*.5-model.rowCenters[3])/model.distance+.000001;model.layout();api.chronicle.render();assert(model.changed(3));assert(model.noise(3));assert.equal(api.chronicle.rows[3].h.style.opacity,'1');assert(api.chronicle.rows[3].row.classes.has('record-glitch'));
 model.update((1-model.progress)*model.duration()+.001);api.chronicle.render();assert.equal(model.phase,'rewritePause');assert(model.snapshot().changed.every(Boolean));
 model.update(model.duration()+.001);api.chronicle.render();assert.equal(model.phase,'higher');const higherY=model.y;model.update(1);api.chronicle.render();assert(model.y<higherY);assert(api.chronicle.rows.every(r=>!r.row.classes.has('record-glitch')));
 model.update((1-model.progress)*model.duration()+.001);api.chronicle.render();assert.equal(model.phase,'done');assert.equal(model.finished,true);assert.equal(model.skipped,false);assert.equal(els.chronicleContinue.hidden,false);assert.equal(api.snapshot().completed,false);assert.equal(api.advance(),false);
 els.chronicleLayer.onpointerup(pointer());assert.equal(api.snapshot().completed,false);api.performAction();assert.equal(api.snapshot().completed,true);assert.equal(api.snapshot().chronicleResult.finished,true);assert.equal(api.snapshot().chronicleResult.skipped,false);
 checks.push('Dual records: measured upward Asteran roll fully leaves viewport; same column reverses downward with localized row rewrite; complete higher roll is readable; Continue alone completes. Hold is 3x; release/cancel/blur reset it; hidden windows pause.');
 // Skip never counts as reading or completion. Re-entry resets the sequence and cancellation stops its RAF.
 cuePosition('dual_record');await settle();els.skip.onclick();assert.equal(api.snapshot().chronicle.finished,false);assert.equal(api.snapshot().chronicle.skipped,true);assert.equal(api.snapshot().completed,false);assert.equal(api.snapshot().chronicle.pendingFrame,false);api.performAction();assert.equal(api.snapshot().chronicleResult.skipped,true);
 cuePosition('dual_record');await tick(1000);assert.equal(api.snapshot().chronicle.skipped,false);api.selectScene(0);await tick(1000);assert.equal(els.chronicleLayer.hidden,true);assert.equal(api.snapshot().chronicle.pendingFrame,false);assert.equal(api.snapshot().chronicle.active,false);
 // Reduced motion: twelve deliberate page taps, no hold progression, no scroll or glitch.
 els.reduce.checked=true;els.reduce.onchange();cuePosition('dual_record');await settle();assert.equal(api.snapshot().chronicle.reduced,true);assert.equal(api.snapshot().chronicle.pendingFrame,false);els.chronicleLayer.onpointerdown(pointer());await tick(1000);els.chronicleLayer.onpointerup(pointer());assert.equal(api.snapshot().chronicle.staticPage,0);
 for(let i=0;i<6;i++)api.chronicle.nextStaticPage();assert.equal(api.snapshot().chronicle.staticSide,'higher');assert.equal(api.snapshot().chronicle.staticPage,0);
 for(let i=0;i<6;i++)api.chronicle.nextStaticPage();assert.equal(api.snapshot().chronicle.finished,true);assert.equal(api.snapshot().completed,false);assert(api.chronicle.rows.every(r=>!r.row.classes.has('record-glitch')));api.performAction();assert.equal(api.snapshot().completed,true);
 checks.push('Credits skip retains skipped=true/finished=false and waits for Continue; navigation cancels credits; re-entry resets. Reduced motion presents six plus six manual pages with no RAF, glitch, or hold-driven advancement.');
 // BFCache restoration restarts an unfinished current cue, while completed previews stay complete.
 fire(windowEvents,'pagehide');fire(windowEvents,'pageshow',{persisted:true});fire(windowEvents,'focus');await settle();assert.equal(api.snapshot().completed,true);assert.equal(els.completion.hidden,false);assert.equal(api.snapshot().chronicle.active,false);
 els.reduce.checked=false;cuePosition('dual_record');await tick(1000);assert.equal(api.snapshot().chronicle.phase,'asteran');const cachedLine=api.snapshot().lineId;
 fire(windowEvents,'pagehide');assert.equal(api.snapshot().chronicle.active,false);assert.equal(els.chronicleLayer.hidden,true);fire(windowEvents,'pageshow',{persisted:true});fire(windowEvents,'focus');await settle();assert.equal(api.snapshot().lineId,cachedLine);assert.equal(api.snapshot().completed,false);assert.equal(api.snapshot().chronicle.active,true);assert.equal(api.snapshot().chronicle.phase,'intro');assert.equal(els.chronicleLayer.hidden,false);assert.equal(els.dialogueWrap.hidden,true);
 api.selectScene(3);await settle();fire(windowEvents,'pagehide');fire(windowEvents,'pageshow',{persisted:true});fire(windowEvents,'focus');await settle();assert.equal(api.snapshot().scene,'R04');assert.equal(els.dialogueWrap.hidden,false);assert.equal(els.chronicleLayer.hidden,true);
 checks.push('BFCache pageshow(persisted) safely restarts the current unfinished line/credits after pagehide cancellation; completed previews remain complete.');
 assert(css.includes('bottom:max(1.5%, env(safe-area-inset-bottom, 0px))'));assert(css.includes('font-size:4.1667cqw'));assert(html.indexOf('../../../../Assets/ANIMOL/Data/StarwayRanch/Story/ranch_chronicle.js')<html.indexOf('src="chronicle.js"'));assert(html.indexOf('src="chronicle.js"')<html.indexOf('src="preview.js"'));
 for(const asset of loadedPaths)assert(fs.existsSync(path.join(root,asset)),asset);
 for(const [file,key] of [['ranch_dialogue','RANCH_DIALOGUE'],['ranch_chronicle','RANCH_CHRONICLE']]){
  const c=vm.createContext({window:{}});vm.runInContext(fs.readFileSync(path.join(dataRoot,file+'.js'),'utf8'),c);
  assert.equal(JSON.stringify(c.window[key]),JSON.stringify(JSON.parse(fs.readFileSync(path.join(dataRoot,file+'.json'),'utf8'))));
 }
 checks.push('Both offline JS wrappers match their complete JSON exactly.');
 const report={version:'0.28',method:'Node VM with DOM/canvas/image-loading stubs; actual runtime JS and actual JSON, filesystem-backed image existence. Not browser layout or Unity execution.',passed:true,checks,assetPathsChecked:[...loadedPaths],limitations:['Native browser font wrapping and pointer behavior are not rendered by this check.','This report covers offline HTML only; Unity results are recorded separately in unity_tests.json.','Audible playback requires manual browser check.']};
 fs.mkdirSync(path.resolve(__dirname,'../../Docs/Validation/StarwayRanchChronicle'),{recursive:true});fs.writeFileSync(path.resolve(__dirname,'../../Docs/Validation/StarwayRanchChronicle/preview_runtime_checks.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:checks.length,lines:walked,assets:loadedPaths.size,report:'Docs/Validation/StarwayRanchChronicle/preview_runtime_checks.json'},null,2));
})().catch(err=>{console.error(err);process.exitCode=1;});
