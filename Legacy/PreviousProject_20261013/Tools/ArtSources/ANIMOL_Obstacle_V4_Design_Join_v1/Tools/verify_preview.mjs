import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),require=createRequire(import.meta.url);
let playwright;try{playwright=require('playwright');}catch{playwright=require(path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES||'','playwright'));}
const browser=await playwright.chromium.launch({headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage({viewport:{width:1240,height:1060},deviceScaleFactor:1}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.join(root,'ANIMOL_Obstacle_V4_Preview.html')).href);
 await page.waitForFunction(()=>window.previewReady===true,{timeout:15000});
 const cases=await page.evaluate(()=>{
  const results=[];
  for(let t=1;t<=5;t++)for(const style of ['A','B','C','D'])for(let k=1;k<=10;k++)for(const fixture of ['floor','wall','step','alone','boundary']){
   const kind='C'+String(k).padStart(2,'0'),plan=window.setFixture(kind,`T0${t}_${style}`,fixture);
   results.push({kind,style:`T0${t}_${style}`,fixture,body:plan.drawBody,capOnly:plan.capOnly,pose:plan.pose,issues:plan.problems});
  }
  return results;
 });
 // Restore a legible default. Candidate warnings are recorded separately from technical errors.
 await page.evaluate(()=>window.setFixture('C03','T01_A','floor','idle'));
 const board=await page.evaluate(()=>window.exportBoard());
 await fs.writeFile(path.join(root,'Art/Preview_50_Theme_Skins.png'),Buffer.from(board.split(',')[1],'base64'));
 await page.screenshot({path:path.join(root,'Validation/preview_window.png'),fullPage:true});
 await page.evaluate(()=>window.setFixture('C01','T01_A','floor','idle'));
 await page.locator('#scene').screenshot({path:path.join(root,'Validation/top_only_connection.png')});
 await page.evaluate(()=>window.setFixture('C02','T01_A','wall','idle'));
 await page.locator('#scene').screenshot({path:path.join(root,'Validation/wall_connection.png')});
 await page.evaluate(()=>window.setFixture('C05','T01_A','floor','inactive'));
 await page.locator('#scene').screenshot({path:path.join(root,'Validation/inactive_restored_endcap.png')});
 const info={cases:cases.length,technicalErrors:errors,visualPlacementWarnings:cases.filter(c=>c.issues.length),
  scope:'Offline Chromium render across 20 styles, 10 devices and 5 layouts. This is graphics QA, not Unity physics or human approval of every contour.',
  browserVersion:browser.version()};
 await fs.writeFile(path.join(root,'Validation/preview_audit.json'),JSON.stringify(info,null,2));
 console.log(JSON.stringify({cases:info.cases,technicalErrors:errors.length,placementWarnings:info.visualPlacementWarnings.length}));
 if(errors.length)throw new Error(errors.join('; '));
}finally{await browser.close();}
