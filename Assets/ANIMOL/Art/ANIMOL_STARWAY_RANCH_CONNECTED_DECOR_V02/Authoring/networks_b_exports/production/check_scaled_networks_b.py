from pathlib import Path
from PIL import Image
from scipy import ndimage
import numpy as np
import json

R=Path(__file__).resolve().parents[1]
families=['orchard_trellis','herb_planter_strip','ranch_lantern_strip']
fixtures={
    'H2':{(0,0),(1,0)},
    'H3':{(0,0),(1,0),(2,0)},
    'H4':{(0,0),(1,0),(2,0),(3,0)},
    'L':{(0,0),(1,0),(1,1),(1,2)},
    'T':{(0,0),(1,0),(2,0),(1,1),(1,2)},
    'cross':{(1,0),(0,1),(1,1),(2,1),(1,2)},
    'ring':{(x,y)for x in range(3)for y in range(3)if(x,y)!=(1,1)},
}
N=[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]
records=[]
for family in families:
    masks={m:Image.open(R/f'decor/connected/{family}/n{m:02d}.png').convert('RGBA')for m in range(16)}
    for key,cells in fixtures.items():
        width=(max(x for x,y in cells)+1)*256+64
        height=(max(y for x,y in cells)+1)*256+64
        native=Image.new('RGBA',(width,height))
        sprites=[]
        for x,y in sorted(cells,key=lambda q:(q[1],q[0])):
            mask=sum(b for dx,dy,b in N if(x+dx,y+dy)in cells)
            native.alpha_composite(masks[mask],(x*256,y*256));sprites.append((x,y,masks[mask]))
        for ppw in [48,64,80,96,128]:
            scale=ppw/256; size=(round(width*scale),round(height*scale))
            for filter_name,fil in [('BILINEAR',Image.Resampling.BILINEAR),('LANCZOS',Image.Resampling.LANCZOS)]:
                monolithic=native.resize(size,fil)
                independent=Image.new('RGBA',size)
                for x,y,spr in sprites:
                    independent.alpha_composite(spr.resize((round(spr.width*scale),round(spr.height*scale)),fil),(x*ppw,y*ppw))
                a=np.array(monolithic);b=np.array(independent)
                core=ndimage.binary_erosion(a[:,:,3]==255,iterations=2)
                assert core.any(),(family,key,ppw,filter_name,'no opaque core')
                alpha_loss=int(np.maximum(a[:,:,3].astype(int)-b[:,:,3],0)[core].max())
                rgba_delta=int(np.abs(a.astype(int)-b.astype(int))[core].max())
                pa=np.concatenate([a[:,:,:3].astype(float)*a[:,:,3:4]/255,a[:,:,3:4]],axis=2)
                pb=np.concatenate([b[:,:,:3].astype(float)*b[:,:,3:4]/255,b[:,:,3:4]],axis=2)
                premul_delta=float(np.abs(pa-pb).max())
                records.append({'family':family,'fixture':key,'pixelsPerCell':ppw,'filter':filter_name,'opaqueCoreAlphaLoss':alpha_loss,'opaqueCoreMaxRgbaDifference':rgba_delta,'wholeImageMaxRgbaDifference':int(np.abs(a.astype(int)-b.astype(int)).max()),'wholeImageMaxPremultipliedRgbaDifference':premul_delta,'wholeImageMaxAlphaDifference':int(np.abs(a[:,:,3].astype(int)-b[:,:,3].astype(int)).max())})
report={'status':'MEASURED_OFFLINE','cases':len(records),'opaqueCoreDefinition':'monolithic reference alpha255 eroded2screenpixels; antialiased outside silhouette excluded','maxOpaqueCoreAlphaLoss':max(r['opaqueCoreAlphaLoss']for r in records),'maxOpaqueCoreRgbaDifference':max(r['opaqueCoreMaxRgbaDifference']for r in records),'maxWholeImageDifference':max(r['wholeImageMaxRgbaDifference']for r in records),'maxWholeImagePremultipliedDifference':max(r['wholeImageMaxPremultipliedRgbaDifference']for r in records),'bilinearMaxOpaqueCoreAlphaLoss':max(r['opaqueCoreAlphaLoss']for r in records if r['filter']=='BILINEAR'),'claims':'CPU resize and composition only, not actual GPU filtering or Unity PlayMode. Antialiasing differences are measured and reported, not asserted zero. Raw RGB difference at alpha0 does not indicate visible color difference.','actualUnity':'NOT_RUN','records':records}
(R/'networks_b_scaled_qa.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items()if k!='records'},ensure_ascii=False,indent=2))
