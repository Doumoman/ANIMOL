"""Independent CPU composition of supplied PNGs vs actual Unity native output."""
import json,math,re
from pathlib import Path
import numpy as np
from PIL import Image

out=Path('Docs/Validation/OriginalCorrectionV2')
source=Path('Tools/ArtSources/ANIMOL_Original_Design_Correction_v2/runtime/main-background')
layers={p.stem:np.array(Image.open(p).convert('RGBA')) for p in source.glob('*.png')}
rabbits={d:np.array(Image.open(f'Assets/ANIMOL/UI/MainUiV7/Textures/rabbit-run-{side}.png').convert('RGBA')) for d,side in [(-1,'left'),(1,'right')]}
y,x=np.mgrid[0:704,0:352];results=[]
def rnd(v):return math.floor(v+.5)
def over(result, layer, px, py, width, height):
    sx=np.floor((x+.5-px)*layer.shape[1]/width+.00001).astype(int)
    sy=np.floor((y+.5-py)*layer.shape[0]/height+.00001).astype(int)
    valid=(sx>=0)&(sx<layer.shape[1])&(sy>=0)&(sy<layer.shape[0])
    sampled=layer[sy.clip(0,layer.shape[0]-1),sx.clip(0,layer.shape[1]-1)]
    mask=valid&(sampled[:,:,3]>127);result[mask]=sampled[mask]
for path in sorted(out.glob('Native_T??_p*_c*_r*.png')):
    theme,p,cam,run=re.match(r'Native_(T\d\d)_p([.\d]+)_c(-?1)_r(-?1)',path.stem).groups();p=float(p);cam=int(cam);run=int(run)
    result=np.full((704,352,4),[26,28,44,255],dtype=np.uint8)
    for name,scale,speed in [('far',1.16,.3),('mid',1.36,.62),('platform',1,0),('rabbit',1,0),('near',1.6,1.6)]:
        if name=='rabbit':
            px=rnd(-64+p*416 if run==1 else 352-p*416);over(result,rabbits[run][:,:64],px,436,64,96)
        else:
            w=rnd(352*scale);h=rnd(704*scale);px=rnd((352-w)/2+cam*88*(p-.5)*speed);py=rnd((704-h)/2+72*(p-.5)*speed)
            over(result,layers[theme+'-'+name],px,py,w,h)
    actual=np.array(Image.open(path).convert('RGBA'));difference=int(np.any(result!=actual,axis=2).sum())
    results.append(dict(file=path.name,differentPixels=difference,pixels=352*704))
assert len(results)==60,len(results)
(out/'RenderParity.json').write_text(json.dumps(dict(comparisons=results,totalDifferentPixels=sum(r['differentPixels'] for r in results)),indent=2))
print('Compared',len(results),'frames;',sum(r['differentPixels'] for r in results),'different pixels')
assert not any(r['differentPixels'] for r in results)
