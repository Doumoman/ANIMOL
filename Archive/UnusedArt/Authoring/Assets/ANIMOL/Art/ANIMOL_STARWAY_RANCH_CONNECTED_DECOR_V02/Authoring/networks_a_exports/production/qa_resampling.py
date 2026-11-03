from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,numpy as np
W=Path(__file__).resolve().parents[1]
BITS=[(0,-1,1),(1,-1,2),(1,0,4),(1,1,8),(0,1,16),(-1,1,32),(-1,0,64),(-1,-1,128)]
def mask(o,x,y):return sum(b for dx,dy,b in BITS if(x+dx,y+dy)in o)
def render(o,ppw):
 size=round(320/256*ppw);out=Image.new('RGBA',((max(x for x,y in o)+1)*ppw+round(64/256*ppw),(max(y for x,y in o)+1)*ppw+round(64/256*ppw)),(170,208,153,255))
 for x,y in sorted(o):
  im=Image.open(W/f'decor/connected/raised_flower_meadow/n{mask(o,x,y):03d}.png').convert('RGBA').resize((size,size),Image.Resampling.LANCZOS);out.alpha_composite(im,(x*ppw,y*ppw))
 return out
q=[]
for ppw in [64,80,128,256]:
 o={(x,y)for x in range(3)for y in range(3)};im=render(o,ppw);a=np.array(im);pad=round(32/256*ppw)
 center=a[pad+ppw:pad+2*ppw,pad+ppw:pad+2*ppw,:3]
 diff=int((center.max((0,1)).astype(int)-center.min((0,1)).astype(int)).max());assert diff==0,(ppw,diff)
 q.append({'logicalPixelsPerCell':ppw,'resampler':'Pillow LANCZOS on each individual RGBA sprite BEFORE composition','centralCellInteriorRGBRange':diff,'noVisibleInternalAlphaCutGrid':True})
o={(x,y)for x in range(5)for y in range(3)if(x,y)!=(2,1)};im=render(o,128)
for i,(x,y)in enumerate([(0,0),(3,0),(1,2),(4,1)]):
 s=Image.open(W/f'decor/accents/meadow/flowers_{i+1:02d}.png').resize((160,160),Image.Resampling.LANCZOS);im.alpha_composite(s,(x*128,y*128))
im.convert('RGB').save(W/'inspection/meadow_large_region_preview.png')
old=W/'inspection/meadow_draft_preview.png'
if old.exists():old.unlink()
p=W/'meadow_qa.json';j=json.loads(p.read_text());j['bilinearRiskResamplingEvidence']=q;p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n')
(W/'meadow_resampling_qa.json').write_text(json.dumps({'status':'PASS_OFFLINE','checks':q,'actualUnityBilinearCapture':'NOT_RUN','scope':'Offline CPU resampling evidence; actual GPU/Unity imports require project verification.'},indent=2)+'\n')
print(json.dumps({'status':'PASS_OFFLINE_RESAMPLING','sizes':[x['logicalPixelsPerCell']for x in q],'maxInteriorRGBRange':max(x['centralCellInteriorRGBRange']for x in q)}))
