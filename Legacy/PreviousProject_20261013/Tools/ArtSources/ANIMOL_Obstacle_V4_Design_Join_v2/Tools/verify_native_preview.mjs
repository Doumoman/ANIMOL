import fs from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),require=createRequire(import.meta.url);
let api;try{api=require('@napi-rs/canvas');}catch{api=require(path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES||'','@napi-rs/canvas'));}
const elements={},defaults={style:'',kind:'',facing:'UP',pose:'idle',fixture:'floor',zoom:'2',seed:'0',brush:'terrain'};
const values={facing:['UP','LEFT','RIGHT'],pose:['idle','warn','active','inactive'],fixture:['floor','wall','step','alone','boundary'],zoom:['1','2','4','8'],brush:['terrain','device','erase']};
class Option {constructor(text,value){this.text=text;this.value=value;this.disabled=false;}}
function element(id){
 if(elements[id])return elements[id];
 if(['scene','board'].includes(id)){
  const c=api.createCanvas(id==='scene'?512:864,id==='scene'?224:600);c.style={};c.getBoundingClientRect=()=>({left:0,top:0,width:512,height:224});elements[id]=c;return c;
 }
 const e={value:defaults[id]||'',options:(values[id]||[]).map(v=>new Option(v,v)),style:{},listeners:{},textContent:'',
  add(o){this.options.push(o);if(!this.value)this.value=o.value;},addEventListener(event,f){this.listeners[event]=f;}};
 return elements[id]=e;
}
const script=(await fs.readFile(path.join(root,'ANIMOL_Obstacle_V4_Preview.html'),'utf8')).match(/<script>([\s\S]*)<\/script>/)[1];
const sandbox={console,Option,Image:api.Image,ImageData:api.ImageData,setTimeout,clearTimeout,Uint8Array,Uint8ClampedArray,
 requestAnimationFrame(callback){sandbox.rafCallback=callback;},document:{getElementById:element,createElement(tag){if(tag==='canvas')return api.createCanvas(300,150);return {click(){}};}},window:null};
sandbox.window=sandbox;vm.createContext(sandbox);vm.runInContext(script,sandbox,{timeout:10000});
for(let n=0;n<300&&!sandbox.previewReady&&!sandbox.previewError;n++)await new Promise(r=>setTimeout(r,10));
if(!sandbox.previewReady)throw Error('preview did not load: '+sandbox.previewError);
const cases=[];
for(let t=1;t<=5;t++)for(const style of ['A','B','C','D'])for(let k=1;k<=10;k++)for(const fixture of ['floor','wall','step','alone','boundary']){
 if(k===9)continue;
 const kind='C'+String(k).padStart(2,'0'),plan=sandbox.setFixture(kind,`T0${t}_${style}`,fixture);
 cases.push({kind,style:`T0${t}_${style}`,fixture,body:plan.drawBody,capOnly:plan.capOnly,pose:plan.pose,issues:plan.problems});
}
const frames=JSON.parse(await fs.readFile(path.join(root,'Data/mechanism_catalog.json'),'utf8')).entries;
const keys=new Set(frames.map(e=>e.id));
for(const c of cases){const p=sandbox.setFixture(c.kind,c.style,c.fixture);if(!keys.has(p.overlay))throw Error('missing registered frame: '+p.overlay);}
for(const entry of frames){
 element('facing').value=entry.facing;sandbox.setFixture(entry.kind,entry.themeId+'_A','alone',entry.pose);
 sandbox.setAnimationTime(entry.frame*(entry.frameDurationMs||1));
 if(sandbox.lastPlan.overlay!==entry.id)throw Error('frame display key differs: '+entry.id+' got '+sandbox.lastPlan.overlay);
}
sandbox.setAnimationTime(0);
await fs.writeFile(path.join(root,'Art/Preview_45_Theme_Skins.png'),Buffer.from(sandbox.exportBoard().split(',')[1],'base64'));
await fs.mkdir(path.join(root,'Validation/animation_frames'),{recursive:true});
for(let f=0;f<64;f++){
 sandbox.setAnimationTime(f*30);
 await fs.writeFile(path.join(root,`Validation/animation_frames/board_${String(f).padStart(3,'0')}.png`),elements.board.toBuffer('image/png'));
}
sandbox.setAnimationTime(0);
for(const [kind,fixture,pose,name]of [['C01','floor','idle','top_only_connection'],['C02','wall','idle','wall_connection'],['C05','floor','inactive','inactive_restored_endcap'],['C03','step','idle','step_connection']]){
 sandbox.setFixture(kind,'T01_A',fixture,pose);await fs.writeFile(path.join(root,`Validation/${name}.png`),elements.scene.toBuffer('image/png'));
 const detail=api.createCanvas(1024,768),dc=detail.getContext('2d');dc.imageSmoothingEnabled=false;
 dc.drawImage(elements.scene,256,fixture==='wall'?64:128,128,96,0,0,1024,768);
 await fs.writeFile(path.join(root,`Validation/${name}_8x.png`),detail.toBuffer('image/png'));
}
// The actual HTML click callback must change the authored preview cell and resolve it again.
sandbox.setFixture('C03','T01_A','floor');element('brush').value='erase';elements.scene.onclick({clientX:272,clientY:176});
if(!elements.status.textContent.includes('칸'))throw Error('preview click handler failed');
sandbox.setFixture('C05','T01_A','floor');element('break').onclick();sandbox.setAnimationTime(160);
if(sandbox.lastPlan.capOnly)throw Error('broken platform kept its intact surface');
await fs.writeFile(path.join(root,'Validation/fracture_fall_160ms.png'),elements.scene.toBuffer('image/png'));
sandbox.setAnimationTime(0);
const report={cases:cases.length,animationFramesTested:frames.length,boardAnimationSamples:64,missingFrames:0,scriptExceptions:0,renderEngine:'@napi-rs/canvas, executing the complete embedded HTML script in Node VM',
 placementWarnings:cases.filter(c=>c.issues.length),fullBrowser:'Chromium executable unavailable; real browser layout and downloads not tested',
 scope:'Native Canvas rendering across 20 styles, 9 devices, 5 layouts; exact HTML callbacks executed. Not a Unity physics test or human approval of every contour.'};
await fs.writeFile(path.join(root,'Validation/native_preview_audit.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify({cases:cases.length,missingFrames:0,scriptExceptions:0,placementWarnings:report.placementWarnings.length,renderEngine:report.renderEngine}));
