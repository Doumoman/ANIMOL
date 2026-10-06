/* Pure rendering module. No network requests or external dependencies. */
(function (global) {
  'use strict';
  const W = 352, H = 704, PERIOD = 5, TRANSITION = 0.36;
  function seed(n) {
    let x = (n + 117) | 0;
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 4294967296;
  }
  // Independent repeatable random choices for the camera and the runner.
  function direction(slot, salt) { return seed(Math.imul(slot + 11, 92821) ^ Math.imul(salt, 0x9e3779b1)) < 0.5 ? -1 : 1; }
  function canvas() {
    const el = document.createElement('canvas'); el.width = W; el.height = H;
    const ctx = el.getContext('2d', { alpha: false }); ctx.imageSmoothingEnabled = false;
    return [el, ctx];
  }
  function create(images, meta, output) {
    const ctx = output.getContext('2d', { alpha: false }); ctx.imageSmoothingEnabled = false;
    const [current, cc] = canvas(), [previous, pc] = canvas();
    function imagePlane(c, img, scale, dx, dy) {
      const dw = Math.round(W * scale), dh = Math.round(H * scale);
      c.drawImage(img, Math.round((W-dw)/2+dx), Math.round((H-dh)/2+dy), dw, dh);
    }
    function scene(c, theme, slot, p, t, options) {
      const layers = images[theme];
      const camera = direction(slot, 91);
      const phase = p - 0.5;
      c.fillStyle = '#1a1c2c'; c.fillRect(0,0,W,H);
      imagePlane(c,layers.far,1.16,camera*88*phase*0.30,72*phase*0.30);
      if (!options.hideMid) imagePlane(c,layers.mid,1.36,camera*88*phase*0.62,72*phase*0.62);
      // The building and floor are drawn with no camera movement whatsoever.
      c.drawImage(layers.platform,0,0);
      if (!options.hideRabbit) {
        const run = direction(slot, 314);
        const frame = Math.floor(t*14)%8;
        const fw=meta.rabbit.frameWidth, fh=meta.rabbit.frameHeight;
        const x = run===1 ? -fw + p*(W+fw) : W-p*(W+fw);
        const sprite = images[run===1?'rabbitRight':'rabbitLeft'];
        c.drawImage(sprite,frame*fw,0,fw,fh,Math.round(x),meta.floorY-meta.rabbit.footY,fw,fh);
      }
      if (!options.hideNear) imagePlane(c,layers.near,1.60,camera*88*phase*1.60,72*phase*1.60);
      return { theme, slot, cameraDirection:camera, rabbitDirection:direction(slot,314), floorY:meta.floorY };
    }
    function render(t, selection=-1, options={}) {
      const slot=Math.floor(t/PERIOD), local=t-slot*PERIOD, p=local/PERIOD;
      const theme=selection<0?slot%5:selection;
      const state=scene(cc,theme,slot,p,t,options);
      ctx.drawImage(current,0,0);
      // A brief stepped diagonal wipe preserves the strict sixteen-color palette.
      if (slot>0 && local<TRANSITION && !options.noTransition) {
        scene(pc,selection<0?(slot-1)%5:selection,slot-1,0.999,t,options);
        ctx.drawImage(previous,0,0);
        const threshold = local / TRANSITION * 1.22 - 0.11;
        for(let y=0;y<H;y+=8) for(let x=0;x<W;x+=8) {
          const d=0.5*x/W+0.5*y/H;
          if(d < threshold) ctx.drawImage(current,x,y,8,8,x,y,8,8);
        }
      }
      return state;
    }
    return {render,meta};
  }
  async function loadEmbedded(data) {
    const map={};
    await Promise.all(Object.entries(data).map(([key,url])=>new Promise((resolve,reject)=>{
      const im=new Image(); im.onload=()=>{map[key]=im;resolve();};
      im.onerror=()=>reject(new Error('이미지를 불러오지 못했습니다: '+key)); im.src=url;
    })));
    const images={rabbitRight:map.rabbitRight,rabbitLeft:map.rabbitLeft};
    for(let i=0;i<5;i++) images[i]={far:map['T0'+(i+1)+'-far'],mid:map['T0'+(i+1)+'-mid'],platform:map['T0'+(i+1)+'-platform'],near:map['T0'+(i+1)+'-near']};
    return images;
  }
  global.AnimolBackgrounds={create,loadEmbedded,direction,width:W,height:H,period:PERIOD};
})(window);
