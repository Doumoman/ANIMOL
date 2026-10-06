'use strict';
/* Offline native Canvas + small DOM adapter; not a browser or Unity run. */
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert'),crypto=require('crypto');
const {createCanvas,Image}=require(require.resolve('@napi-rs/canvas',{paths:[process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES||__dirname]}));
const DIR=__dirname,ROOT=path.resolve(DIR,'..');
const html=fs.readFileSync(path.join(DIR,'ANIMOL_Original_Design_Correction_v2_Preview.html'),'utf8');
const scripts=[...html.matchAll(/<script>([\s\S]*?)<\/script>/g)].map(x=>x[1]);
let checks=0; const check=(value,label)=>{assert(value,label);checks++;};
check(scripts.length===3,'three embedded scripts');
check(!/(?:src|href)\s*=\s*["']https?:\/\//i.test(html),'no external resource tags');
check(!/\b(?:fetch|XMLHttpRequest|localStorage|sessionStorage)\s*\(/.test(scripts.join('\n')),'no requests or storage API');
scripts.forEach((s,i)=>new vm.Script(s,{filename:'embedded-'+i+'.js'}));
let time=0;const queue=[],elements={};
function element(id){if(elements[id])return elements[id];const el=id==='view'?createCanvas(352,704):{};
 el.style={};el.value=id==='variant'?'corrected':id==='theme'?'-1':id==='zoom'?'1':'';
 el.checked=['near','rabbit'].includes(id);el.disabled=false;el.hidden=false;
 el.attrs={};el.setAttribute=(k,v)=>{el.attrs[k]=v;};elements[id]=el;return el;}
const sandbox={document:{getElementById:element,createElement(tag){assert(tag==='canvas');return createCanvas(352,704);}},performance:{now:()=>time},Math,Promise,Image,requestAnimationFrame:cb=>{queue.push(cb);}};
const window=sandbox;sandbox.window=window;
vm.createContext(sandbox);for(const script of scripts)vm.runInContext(script,sandbox);
const A=window.AnimolBackgrounds,P=window.AnimolCorrectionPreview,data=window.PREVIEW_DATA;
function tick(){const fn=queue.shift();check(typeof fn==='function','animation frame queued');fn();}
function rgba(){return elements.view.getContext('2d').getImageData(0,0,elements.view.width,elements.view.height).data;}
function sum(bytes){return crypto.createHash('sha256').update(Buffer.from(bytes)).digest('hex');}
async function load(url){return new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{resolve(im);};im.onerror=reject;im.src=url;});}
(async()=>{
 for(let n=0;n<200&&!P.ready;n++)await new Promise(r=>setTimeout(r,5));
 check(P.ready,'all embedded images initialized');
 check(data.inputHashes.length===63,'60 planes + 2 rabbit sheets + 1 campaign PNG');
 for(const row of data.inputHashes){check(sum(fs.readFileSync(path.join(ROOT,row.path)))===row.sha256,'embedded input still current: '+row.path);}
 check(P.meta.floorY===526&&P.meta.rabbit.frameWidth===64&&P.meta.rabbit.frameHeight===96&&P.meta.rabbit.footY===90,'unchanged source anchor contract');
 check(fs.readFileSync(path.join(DIR,'background-animation.js'),'utf8')===fs.readFileSync(path.join(ROOT,'recovered/batch01/ANIMOL_Art_Revision_Batch01_v1/previews/background-animation.js'),'utf8'),'exact original motion source reused');
 const out=path.join(DIR,'native-canvas');fs.mkdirSync(out,{recursive:true});
 const renders=[],palette=new Set(['1a1c2c','5d275d','b13e53','ef7d57','ffcd75','a7f070','38b764','257179','29366f','3b5dc9','41a6f6','73eff7','f4f4f4','94b0c2','566c86','333c57']);
 time=2500;
 for(const variant of ['original','previous','corrected']){
  elements.variant.value=variant;elements.variant.onchange();
  for(let theme=0;theme<5;theme++){
   elements.theme.value=String(theme);tick();const pixels=rgba();
   check(elements.view.width===352&&elements.view.height===704,'native motion viewport');
   const colors=new Set();let opaque=true;for(let p=0;p<pixels.length;p+=4){opaque=opaque&&pixels[p+3]===255;colors.add(Buffer.from(pixels.slice(p,p+3)).toString('hex'));}check(opaque,'opaque output');
   // Original source contains the original palette treatment; only the new
   // delivered raster set is required to stay in Sweetie16 here.
   if(variant==='corrected')check([...colors].every(c=>palette.has(c)),'corrected palette remains Sweetie16 under NN motion');
   const file='T0'+(theme+1)+'-'+variant+'.png';fs.writeFileSync(path.join(out,file),elements.view.toBuffer('image/png'));
   renders.push({variant,theme:'T0'+(theme+1),file:'native-canvas/'+file,rgbaSha256:sum(pixels),colors:colors.size});
  }
 }
 elements.variant.value='corrected';elements.variant.onchange();elements.theme.value='0';tick();
 elements.pause.onclick();check(P.clock()===2.5,'pause stores current time');const before=sum(rgba());time=9000;tick();check(P.clock()===2.5&&sum(rgba())===before,'paused pixels stay fixed');
 elements.zoom.value='3';elements.zoom.onchange();check(elements.view.style.width==='1056px'&&elements.view.style.height==='2112px','integer 3x zoom');
 elements.detail.checked=true;elements.detail.onchange();tick();check(elements.view.height===250&&elements.view.style.height==='750px','native bridge crop and integer zoom');
 const bridge=await load(data.backgrounds.corrected['T01-platform']),expected=createCanvas(352,250),ec=expected.getContext('2d');ec.imageSmoothingEnabled=false;ec.fillStyle='#41a6f6';ec.fillRect(0,0,352,250);ec.drawImage(bridge,0,454,352,250,0,0,352,250);
 check(sum(rgba())===sum(ec.getImageData(0,0,352,250).data),'bridge crop exactly uses native source pixels');
 elements['campaign-tab'].onclick();tick();check(elements.view.height===704&&elements.variant.disabled&&elements.pause.disabled,'campaign disables background-only controls');
 const campaign=await load(data.campaign),cc=createCanvas(352,704),cctx=cc.getContext('2d');cctx.imageSmoothingEnabled=false;cctx.drawImage(campaign,0,0);
 check(sum(rgba())===sum(cctx.getImageData(0,0,352,704).data),'campaign image pixels unchanged');
 check(elements.state.textContent.includes('디자인 참고')&&elements.state.textContent.includes('미연결'),'campaign data status honest');
 elements.back.onclick();check(!elements.variant.disabled&&elements.back.hidden,'back restores background controls');
 for(const t of [0,4.999,5,9.999,10,24.999,25]){
  const loaded=await A.loadEmbedded(Object.assign({},data.backgrounds.corrected,data.rabbit));
  const canvas=createCanvas(352,704),renderer=A.create(loaded,P.meta,canvas);
  check(renderer.render(t,-1,{noTransition:true}).theme===Math.floor(t/5)%5,'five-second auto theme boundary '+t);
 }
 const board=createCanvas(1856,748),bc=board.getContext('2d');bc.imageSmoothingEnabled=false;
 bc.fillStyle='#1a1c2c';bc.fillRect(0,0,1856,748);bc.font='11px monospace';
 for(let n=0;n<5;n++){
  const theme='T0'+(n+1),png=fs.readFileSync(path.join(out,theme+'-corrected.png'));
  const im=await load('data:image/png;base64,'+png.toString('base64'));
  bc.drawImage(im,16+n*368,28);bc.fillStyle='#f4f4f4';bc.fillText(theme+' corrected / motion 2.5s',16+n*368,18);
 }
 fs.writeFileSync(path.join(out,'Corrected_FiveTheme_Motion_2_5s.png'),board.toBuffer('image/png'));
 const report={status:'passed',checks,method:'HTML embedded JS executed with native Skia Canvas and DOM/control adapter; actual raster renders; no browser/Unity',embeddedImages:63,externalRequests:0,browserExecuted:false,UnityExecuted:false,liveDataConnected:false,nativeDisplay:[352,704],bridgeCrop:[0,454,352,250],integerZooms:[1,2,3,4],unchangedRabbit:true,motionSourceExact:true,campaignRuntimePNGReplacements:0,inputHashes:data.inputHashes,renders};
 fs.writeFileSync(path.join(DIR,'preview-verification.json'),JSON.stringify(report,null,2)+'\n');
 process.stdout.write('PASS offline native preview: '+renders.length+' actual Canvas renders, controls, embedding hashes. No browser/Unity execution.\n');
})().catch(error=>{process.stderr.write(error.stack+'\n');process.exitCode=1;});
