from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,os
ROOT=Path(__file__).resolve().parents[2]
W=Path(__file__).resolve().parent
B=ROOT/'work/ranch_devices_v13/reference/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'
G=W/'guides';G.mkdir(parents=True,exist_ok=True)
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
cat=json.loads((B/'decoration_catalog.json').read_text())
styles={'hay_store_2x1':96,'feeding_corner_3x1':64,'tree_flower_corner_2x2':256,'barnyard_3x2':256,'orchard_bench_3x1':160,'seedling_rack_3x1':64,'harvest_cart_2x2':160,'chicken_coop_2x2':192,'apiary_corner_2x2':160,'picnic_arbor_3x2':256,'produce_stall_3x2':256,'garden_workbench_2x1':128}
def rect(d,xy,fill=None,width=3):d.rectangle(xy,fill=fill,outline='#283848',width=width)
def plane(d,x,y,w,h,z=0):
 rect(d,(x,y-z,x+w,y+h-z),'#eee1be')
 for px,py in [(x,y),(x+w,y),(x,y+h),(x+w,y+h)]:
  if z:d.line((px,py-z,px,py),fill='#547c71',width=9)
def guide(key,w,h,e):
 T=256;im=Image.new('RGB',(w*T+64,h*T+64+e),'white');d=ImageDraw.Draw(im)
 x,y=32,32+e;ww,hh=w*T,h*T
 # Exact ground projection and per-cell measurements; blueprint marks are NOT final art.
 for i in range(w+1):d.line((x+i*T,y,x+i*T,y+hh),fill='#b6c4cc',width=1)
 for j in range(h+1):d.line((x,y+j*T,x+ww,y+j*T),fill='#b6c4cc',width=1)
 d.rectangle((x,y,x+ww,y+hh),outline='#d23e58',width=4)
 if key in ['hay_store_2x1','feeding_corner_3x1','seedling_rack_3x1','garden_workbench_2x1']:
  z=min(e,96);plane(d,x+14,y+14,ww-28,hh-28,z)
  if key=='seedling_rack_3x1':
   for i in [1,2]:d.line((x+i*T,y+14-z,x+i*T,y+hh-14-z),fill='#283848',width=5)
 elif key=='orchard_bench_3x1':
  plane(d,x+16,y+12,ww-32,hh-24,96)
  rect(d,(x+16,y-160,x+ww-16,y-96),'#d6e4cf')
 elif key=='harvest_cart_2x2':
  plane(d,x+35,y+20,ww-70,hh-40,96)
  for px in [x+12,x+ww-35]:
   for py in [y+72,y+hh-95]:rect(d,(px,py,px+24,py+58),'#96a1b0')
 elif key=='tree_flower_corner_2x2':
  # Full square flower bed footprint, with elevated tree in northern half.
  rect(d,(x+10,y+10,x+ww-10,y+hh-10),None,8)
  rect(d,(x+ww*.45,y-70,x+ww*.55,y+hh*.48),'#ae8967')
  d.polygon([(x+ww*.15,y-15),(x+ww*.3,y-e+12),(x+ww*.7,y-e+12),(x+ww*.85,y-15),(x+ww*.5,y+hh*.2)],fill='#b4d39a',outline='#283848',width=3)
 elif key in ['barnyard_3x2','chicken_coop_2x2']:
  bw=ww*.64 if w==3 else ww*.72
  # Roof and facade form one large building, yard remains the same square-grid scale.
  rect(d,(x+20,y-e+20,x+20+bw,y+160),'#d6e4cf')
  rect(d,(x+28,y-20,x+12+bw,y+230),'#eee1be')
  rect(d,(x+40+bw*.2,y+20,x+40+bw*.2+150,y+220),None,4)
  if w==3:rect(d,(x+bw+35,y-20,x+ww-10,y+240),'#e5d3a2')
  for px in [x+8,x+ww-8]:d.line((px,y,px,y+hh),fill='#547c71',width=9)
  d.line((x,y+hh,x+ww,y+hh),fill='#547c71',width=9)
 elif key=='apiary_corner_2x2':
  for i in range(2):plane(d,x+i*T+18,y+14,T-36,T-28,128)
  plane(d,x+18,y+T+20,ww-36,T-36,48)
 elif key in ['picnic_arbor_3x2','produce_stall_3x2']:
  for px in [x+12,x+ww-12]:
   for py in [y+12,y+hh-12]:d.line((px,py-e,px,py),fill='#547c71',width=11)
  if key=='produce_stall_3x2':
   plane(d,x+12,y+12,ww-24,T-24,e)
   plane(d,x+18,y+T+16,ww-36,T-28,80)
   for i in [1,2]:d.line((x+i*T,y+T-64,x+i*T,y+hh-92),fill='#283848',width=4)
  else:
   # Open canopy perimeter, so the picnic table remains visible.
   rect(d,(x+12,y-e+12,x+ww-12,y-e+72),'#e6ce8d')
   plane(d,x+70,y+130,ww-140,hh-210,96)
 d.text((12,im.height-22),'GROUND: '+str(w)+' x '+str(h)+'   HEIGHT: separate',fill='#b32946',font=F)
 p=G/(key+'.png');tmp=p.with_name(p.name+'.tmp');im.save(tmp,format='PNG');os.replace(tmp,p)
 return str(p)
records=[]
for s in cat['groups']:
 k=s['key'];w,h=s['footprintCells'];records.append(dict(key=k,w=w,h=h,extra=styles[k],guide=guide(k,w,h,styles[k]),target=str(B/'decor/groups'/f'{k}.png')))
(W/'guide_manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2))
# Evidence of the exact Library baseline already held locally.
archive=ROOT/'work/ranch_devices_v13/reference_download/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02.zip'
print(json.dumps({'guides':len(records),'baselineZipBytes':archive.stat().st_size}))
