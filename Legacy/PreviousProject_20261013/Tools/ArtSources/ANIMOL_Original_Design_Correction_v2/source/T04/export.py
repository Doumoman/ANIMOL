from PIL import Image
from pathlib import Path
import numpy as np
import shutil, json, hashlib

root=Path(__file__).parent
source=Path('/workspace/scratch/01a31b8b978d/work/art-correction02/recovered/batch01/ANIMOL_Art_Revision_Batch01_v1/source/main-background/T04-original-layer-atlas.png')
raw=Path('/workspace/scratch/01a31b8b978d/generated_images/exec-52f7b067-9c4b-4015-9d2e-f052b1acbaf4.png')
shutil.copy2(raw,root/'raw-atlas.png')
original=Image.open(source).convert('RGBA')
generated=Image.open(raw).convert('RGB').resize(original.size,Image.Resampling.NEAREST)
generated.save(root/'registered-atlas.png')
colors=['1a1c2c','5d275d','b13e53','ef7d57','ffcd75','a7f070','38b764','257179','29366f','3b5dc9','41a6f6','73eff7','f4f4f4','94b0c2','566c86','333c57']
palette=np.array([[int(c[j:j+2],16) for j in (0,2,4)] for c in colors],dtype=np.int32)
meta={'theme':'T04','operation':'original-identity-preserving pixel-art surface cleanup','tool':'built-in imagegen','original_atlas':str(source),'raw_atlas_size':list(Image.open(raw).size),'registered_atlas_size':list(original.size),'registration':'nearest-neighbour only','alpha_policy':'original binary alpha intersect generated black-space key (all raw RGB channels <= 12); no silhouette painting','palette':'Sweetie16 nearest Euclidean RGB, no dithering','assets':[]}
finals=[];before=[]
for n,name in enumerate(['far','mid','platform','near']):
    box=(16+368*n,16,368+368*n,720)
    src=original.crop(box);rgb=np.array(generated.crop(box)).astype(np.int32)
    idx=((rgb[:,:,None,:]-palette[None,None,:,:])**2).sum(axis=3).argmin(axis=2)
    alpha=(np.array(src)[:,:,3]>127)&~(rgb.max(axis=2)<=12)
    rgba=np.zeros((704,352,4),dtype=np.uint8);rgba[:,:,:3]=palette[idx];rgba[:,:,3]=alpha.astype(np.uint8)*255
    im=Image.fromarray(rgba,'RGBA');im.save(root/f'T04-{name}.png');finals.append(im);before.append(src)
    meta['assets'].append({'path':f'T04-{name}.png','size':list(im.size),'sha256':hashlib.sha256((root/f'T04-{name}.png').read_bytes()).hexdigest(),'original_opaque_pixels':int((np.array(src)[:,:,3]>127).sum()),'final_opaque_pixels':int(alpha.sum()),'removed_alpha_fringe_pixels':int(((np.array(src)[:,:,3]>127)&~alpha).sum())})
composite=Image.new('RGBA',(352,704));origcomp=Image.new('RGBA',(352,704))
for im in finals:composite.alpha_composite(im)
for im in before:origcomp.alpha_composite(im)
composite.save(root/'T04-composite.png');origcomp.save(root/'T04-original-composite.png')
composite.resize((704,1408),Image.Resampling.NEAREST).save(root/'T04-composite-2x.png')
finals[2].crop((0,520,352,704)).resize((1408,736),Image.Resampling.NEAREST).save(root/'T04-platform-review-4x.png')
panel=Image.new('RGBA',(704,704),(26,28,44,255));panel.alpha_composite(origcomp,(0,0));panel.alpha_composite(composite,(352,0));panel.resize((1408,1408),Image.Resampling.NEAREST).save(root/'T04-original-new-2x.png')
(root/'metadata.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(meta,ensure_ascii=False))
