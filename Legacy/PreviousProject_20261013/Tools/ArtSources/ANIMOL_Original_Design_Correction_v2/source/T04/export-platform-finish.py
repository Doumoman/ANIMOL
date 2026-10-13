from PIL import Image
from pathlib import Path
import numpy as np, json, shutil, hashlib
root=Path(__file__).parent
raw=Path('/workspace/scratch/01a31b8b978d/generated_images/exec-209a44d3-94f5-4b2c-9025-b018c7c2962f.png')
source=Path('/workspace/scratch/01a31b8b978d/work/art-correction02/recovered/batch01/ANIMOL_Art_Revision_Batch01_v1/source/main-background/T04-original-layer-atlas.png')
original=Image.open(source).convert('RGBA').crop((752,16,1104,720))
shutil.copy2(raw,root/'raw-platform-finish.png')
generated=Image.open(raw).convert('RGB').resize((352,184),Image.Resampling.NEAREST)
generated.save(root/'registered-platform-finish.png')
colors=['1a1c2c','5d275d','b13e53','ef7d57','ffcd75','a7f070','38b764','257179','29366f','3b5dc9','41a6f6','73eff7','f4f4f4','94b0c2','566c86','333c57']
palette=np.array([[int(c[j:j+2],16) for j in (0,2,4)] for c in colors],dtype=np.int32)
rgb=np.array(generated).astype(np.int32)
idx=((rgb[:,:,None,:]-palette[None,None,:,:])**2).sum(axis=3).argmin(axis=2)
alpha=(np.array(original.crop((0,520,352,704)))[:,:,3]>127)&~(rgb.max(axis=2)<=12)
rgba=np.zeros((184,352,4),dtype=np.uint8);rgba[:,:,:3]=palette[idx];rgba[:,:,3]=alpha.astype(np.uint8)*255
crop=Image.fromarray(rgba,'RGBA')
prior=Image.open(root/'T04-platform.png').convert('RGBA')
prior.save(root/'T04-platform-atlas-pass.png')
prior.paste(crop,(0,520))
# Technical registration: the original source slab starts at y527 while the
# shared runtime/rabbit walking contract is y526. Translate the whole finished
# layer by exactly one native pixel, without modifying any local artwork.
unaligned=prior
prior=Image.new('RGBA',unaligned.size)
prior.paste(unaligned.crop((0,1,352,704)),(0,0))
prior.save(root/'T04-platform.png')
prior.crop((0,520,352,704)).resize((1408,736),Image.Resampling.NEAREST).save(root/'T04-platform-review-4x.png')
composite=Image.new('RGBA',(352,704))
for n in ('far','mid','platform','near'):composite.alpha_composite(Image.open(root/f'T04-{n}.png').convert('RGBA'))
composite.save(root/'T04-composite.png');composite.resize((704,1408),Image.Resampling.NEAREST).save(root/'T04-composite-2x.png')
old=Image.open(root/'T04-original-composite.png').convert('RGBA')
pair=Image.new('RGBA',(704,704),(26,28,44,255));pair.alpha_composite(old,(0,0));pair.alpha_composite(composite,(352,0));pair.resize((1408,1408),Image.Resampling.NEAREST).save(root/'T04-original-new-2x.png')
meta=json.loads((root/'metadata.json').read_text())
meta['platform_finish']={'raw_size':list(Image.open(raw).size),'registered_crop_size':[352,184],'crop_native_y':520,'crop_placement':'exact technical paste into prior native layer, then whole-layer registration deltaY=-1','alpha_policy':'original binary alpha intersect generated black key (all RGB <= 12), then whole layer shifted up1px','purpose':'remove isolated slab/column gold pixels and unify simple leaf outline','method':'built-in imagegen plus nearest-neighbour palette export only','source_walk_row':527,'final_walk_row':526,'whole_layer_registration_deltaY':-1,'registration_scope':'platform only; far/mid/near unchanged; do not apply this shift again in Unity'}
for a in meta['assets']:
    if a['path']=='T04-platform.png':
        a['sha256']=hashlib.sha256((root/'T04-platform.png').read_bytes()).hexdigest()
        a['final_opaque_pixels']=int((np.array(prior)[:,:,3]>127).sum());a['removed_alpha_fringe_pixels']=a['original_opaque_pixels']-a['final_opaque_pixels']
(root/'metadata.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(meta['platform_finish']))
