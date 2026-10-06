from pathlib import Path
from PIL import Image
import numpy as np
import json,hashlib,shutil

out=Path(__file__).resolve().parent
base=out.parents[1]/'recovered/batch01/ANIMOL_Art_Revision_Batch01_v1'
original=Image.open(base/'source/main-background/T02-original-layer-atlas.png').convert('RGBA')
rawpath=Path('/workspace/scratch/01a31b8b978d/generated_images/exec-9391b5ce-85d7-4d34-ab5a-3c5a402eef8c.png')
shutil.copyfile(rawpath,out/'generated-atlas.png')
raw=Image.open(rawpath).convert('RGBA')
registered=raw.resize(original.size,Image.Resampling.NEAREST)
bridgepath=Path('/workspace/scratch/01a31b8b978d/generated_images/exec-eb8bf618-60bb-4ef2-8058-18e365870eda.png')
shutil.copyfile(bridgepath,out/'generated-bridge.png')
bridge=Image.open(bridgepath).convert('RGBA')
palette=np.array([tuple(bytes.fromhex(v)) for v in ['1a1c2c','5d275d','b13e53','ef7d57','ffcd75','a7f070','38b764','257179','29366f','3b5dc9','41a6f6','73eff7','f4f4f4','94b0c2','566c86','333c57']],dtype=np.int16)
metadata={'artisticEditsInScript':False,'technicalOperations':['uniform registration','native nearest sampling','fixed palette nearest mapping without dithering','original binary alpha preservation for far/mid/near; T02 platform upper disconnected sky artifact uses prior cleaned alpha only above y448; below y448 intersect original alpha with edited bridge alpha threshold128 to avoid transparent RGB fringes'], 'assets':[]}
composite=Image.new('RGBA',(352,704))
oldcomp=Image.new('RGBA',(352,704))
for i,name in enumerate(['far','mid','platform','near']):
    box=(16+368*i,16,368+368*i,720)
    src=np.array(registered.crop(box))
    if name=='platform':
        src[448:]=np.array(bridge.resize((352,256),Image.Resampling.NEAREST))
    orig=np.array(original.crop(box))
    alpha=orig[:,:,3].copy()
    if name=='platform':
        alpha[:448]=np.array(Image.open(base/'runtime/main-background/T02-platform.png').convert('RGBA'))[:448,:,3]
        alpha[448:]=((alpha[448:]>0)&(src[448:,:,3]>=128)).astype(np.uint8)*255
    rgb=src[:,:,:3].astype(np.int16)
    dif=rgb[:,:,None,:]-palette[None,None,:,:]
    dist=(dif.astype(np.int32)**2).sum(axis=3)
    native=np.zeros((704,352,4),dtype=np.uint8)
    native[:,:,:3]=palette[dist.argmin(axis=2)].astype(np.uint8)
    native[:,:,3]=alpha
    native[alpha==0,:3]=0
    im=Image.fromarray(native,'RGBA');path=out/f'T02-{name}.png';im.save(path)
    composite.alpha_composite(im);oldcomp.alpha_composite(Image.fromarray(orig,'RGBA'))
    metadata['assets'].append({'id':f'T02-{name}','sourceSize':list(raw.size),'crop':list(box),'size':[352,704],'originalAlphaIdentical':bool(np.array_equal(alpha,orig[:,:,3])),'originalAlphaBelowY448Identical':bool(np.array_equal(alpha[448:],orig[448:,:,3])),'belowY448RemovedAlphaPixels':int(((orig[448:,:,3]>0)&(alpha[448:]==0)).sum()),'floor526ContinuousX70To281':bool((alpha[526,70:282]==255).all()) if name=='platform' else None,'bridgeTargetSourceSize':list(bridge.size) if name=='platform' else None,'bridgeTargetDestinationRect':[0,448,352,704] if name=='platform' else None,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
composite.save(out/'T02-composite.png')
composite.resize((704,1408),Image.Resampling.NEAREST).save(out/'T02-composite-2x.png')
im=Image.open(out/'T02-platform.png');im.crop((0,448,352,704)).resize((1408,1024),Image.Resampling.NEAREST).save(out/'T02-bridge-4x.png')
comparison=Image.new('RGBA',(704,704));comparison.alpha_composite(oldcomp,(0,0));comparison.alpha_composite(composite,(352,0));comparison.resize((1408,1408),Image.Resampling.NEAREST).save(out/'T02-original-revised-comparison-2x.png')
(out/'export-metadata.json').write_text(json.dumps(metadata,indent=2)+'\n')
print(json.dumps(metadata))
