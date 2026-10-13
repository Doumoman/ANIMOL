import test from 'node:test';
import assert from 'node:assert/strict';
import {createQAStage,createQAGame,stepQAGame,resetQAGame,createQAReplayController,qaGeometry,QA_STEP} from '../Runtime/temporary_player.mjs';
const fixture=cells=>({id:'fixture',width:22,height:14,spawn:{x:1.5,y:1},bells:[],goal:{x:19,y:10,width:.8,height:1.4},cells});
test('spawn, exactly three unique bells and an initially locked goal are present',()=>{
 const g=createQAGame();assert.equal(g.stage.bells.length,3);assert.equal(new Set(g.stage.bells.map(b=>b.id)).size,3);
 assert.deepEqual([g.player.x,g.player.y],[2.5,1]);assert.equal(g.collected,0);assert.equal(g.status,'playing');
});
test('a player stands on the actual floor, jumps, falls and lands without sinking',()=>{
 const g=createQAGame(fixture(Array.from({length:10},(_,x)=>({x,y:0,kind:'terrain'}))));
 for(let i=0;i<20;i++)stepQAGame(g);assert.equal(g.player.y,1);assert.equal(g.player.grounded,true);
 stepQAGame(g,{jumpPressed:true});assert.ok(g.player.vy>0);assert.equal(g.player.grounded,false);
 for(let i=0;i<200;i++)stepQAGame(g);assert.equal(g.player.y,1);assert.equal(g.player.grounded,true);
});
test('standing spring launches from its visible top plate, not the ground below',()=>{
 const g=createQAGame(fixture([{x:2,y:1,kind:'C03'}]));g.player.x=2.5;g.player.y=2.03;g.player.vy=-2;g.player.grounded=false;
 for(let i=0;i<5&&!g.events.some(e=>e.type==='upSpring');i++)stepQAGame(g);
 assert.equal(g.player.y,2);assert.equal(g.player.vy,13);assert.equal(qaGeometry(g.cells[0]).type,'top');
});
test('side spring preserves incoming horizontal speed and has no top/body collider',()=>{
 const g=createQAGame(fixture([{x:2,y:1,kind:'C02',facing:'LEFT'}]));g.player.x=1.65;g.player.y=1.2;g.player.vx=6;g.player.grounded=false;
 for(let i=0;i<4&&!g.events.some(e=>e.type==='sideSpring');i++)stepQAGame(g);
 const e=g.events.find(e=>e.type==='sideSpring');assert.ok(e);assert.equal(e.outgoing,-e.incoming);
 assert.equal(qaGeometry(g.cells[0]).type,'side');assert.equal(g.player.grounded,false);
});
test('one-way platform permits rising through it and supports descending feet; down drops through',()=>{
 const g=createQAGame(fixture([{x:1,y:1,kind:'C01'}]));g.player.grounded=false;g.player.vy=8.5;
 for(let i=0;i<30;i++)stepQAGame(g);assert.ok(g.player.y>2);assert.ok(g.player.vy>0);
 for(let i=0;i<150;i++)stepQAGame(g);assert.equal(g.player.y,2);assert.equal(g.player.grounded,true);
 stepQAGame(g,{down:true});assert.ok(g.player.y<2);assert.equal(g.player.grounded,false);
});
test('crumble supports the occupant and becomes inactive only after leaving',()=>{
 const g=createQAGame(fixture([{x:1,y:0,kind:'C05'}]));
 for(let i=0;i<12;i++)stepQAGame(g);assert.equal(g.cells[0].surfaceEnabled,true);
 stepQAGame(g,{jumpPressed:true});assert.equal(g.cells[0].surfaceEnabled,false);
 assert.equal(g.events.filter(e=>e.type==='crumble').length,1);
});
test('bells count once and goal stays locked until three bells; reset restores all entities',()=>{
 const s=createQAStage(),g=createQAGame(s);g.player.x=19.4;g.player.y=10;
 stepQAGame(g);assert.equal(g.status,'playing');assert.match(g.message,/잠겨/);
 for(const bell of g.bells){g.player.x=bell.x;g.player.y=bell.y-.5;g.player.vx=g.player.vy=0;stepQAGame(g);}
 assert.equal(g.collected,3);stepQAGame(g);assert.equal(g.collected,3);
 g.player.x=19.4;g.player.y=10;stepQAGame(g);assert.equal(g.status,'cleared');
 resetQAGame(g);assert.equal(g.collected,0);assert.equal(g.status,'playing');assert.ok(g.bells.every(b=>!b.broken));
});
test('constant hazard and falling respawn at the start and reset progress',()=>{
 const g=createQAGame();g.player.x=18.5;g.player.y=1.1;stepQAGame(g);
 assert.equal(g.deaths,1);assert.deepEqual([g.player.x,g.player.y],[2.5,1]);
 g.player.y=-4;stepQAGame(g);assert.equal(g.deaths,2);assert.equal(g.collected,0);
});
test('the complete start → three bells → goal route clears using input only',()=>{
 const g=createQAGame(),control=createQAReplayController();
 for(let i=0;i<120*35&&g.status!=='cleared';i++)stepQAGame(g,control(g),QA_STEP);
 assert.equal(g.status,'cleared',JSON.stringify({x:g.player.x,y:g.player.y,bells:g.collected,events:g.events.slice(-12)}));
 assert.equal(g.collected,3);assert.equal(g.deaths,0);assert.ok(g.events.filter(e=>e.type==='upSpring').length>=3);
 assert.equal(g.events.filter(e=>e.type==='bell').length,3);assert.equal(g.events.filter(e=>e.type==='clear').length,1);
});
