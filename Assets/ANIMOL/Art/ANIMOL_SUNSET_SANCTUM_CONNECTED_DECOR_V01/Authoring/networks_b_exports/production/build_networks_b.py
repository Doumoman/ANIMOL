from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage
import hashlib, json
import numpy as np

R = Path(__file__).resolve().parents[1]
for p in ['production/components', 'decor/connected', 'inspection']:
    (R / p).mkdir(parents=True, exist_ok=True)

SPECS = {
    'star_banner_partition': {'h': 116, 'v': 72, 'joint': [110, 132], 'bottom': 286},
    'votive_light_channel': {'h': 64, 'v': 56, 'joint': [78, 82], 'bottom': 265},
    'blue_crystal_planter': {'h': 100, 'v': 92, 'joint': [112, 130], 'bottom': 290},
}
BOXES = {'h': [0, 0, 1340, 430], 'v': [1340, 0, 1536, 1024], 'joint': [0, 500, 600, 1024]}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(im, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)
    Image.open(path).load()
def write(path, data): path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')

def component(source, box):
    # Native cutout registration: largest alpha128 connected subject and a 2px native AA
    # fringe. No RGB painting, recoloring, procedural shape replacement or quantization.
    a = np.array(Image.open(source).convert('RGBA').crop(box))
    labels, count = ndimage.label(a[:, :, 3] >= 128, np.ones((3, 3), bool))
    sizes = np.bincount(labels.ravel()); sizes[0] = 0
    assert count > 0
    body = labels == int(np.argmax(sizes))
    fringe = ndimage.binary_dilation(body, iterations=2)
    a[:, :, 3] = np.where(fringe, a[:, :, 3], 0)
    # The imagegen alpha core is254. Normalize only near-opaque alpha, preserving
    # all native RGB and ordinary antialiasing; erase alpha below8 only.
    a[:, :, 3] = np.where(a[:, :, 3] >= 245, 255, a[:, :, 3])
    a[a[:, :, 3] < 8, 3] = 0
    out = Image.fromarray(a, 'RGBA')
    return out.crop(out.getbbox())

def carrier(part, axis, thickness, family):
    if axis == 'h':
        im = part.resize((round(part.width * thickness / part.height), thickness), Image.Resampling.LANCZOS)
        assert im.width >= 256
        x = (im.width - 256) // 2
        im = im.crop((x, 0, x + 256, thickness))
        # Shared native cross-section at both ports. Only a native column is
        # extruded along the connection axis, without synthesizing RGB.
        profile_x = {'star_banner_partition':59,'blue_crystal_planter':109,'votive_light_channel':59}[family]
        profile = im.crop((profile_x, 0, profile_x+1, thickness)).resize((12, thickness), Image.Resampling.NEAREST)
        im.paste(profile, (0, 0)); im.paste(profile, (244, 0))
    else:
        im = part.resize((thickness, round(part.height * thickness / part.width)), Image.Resampling.LANCZOS)
        assert im.height >= 256
        y = (im.height - 256) // 2
        im = im.crop((0, y, thickness, y + 256))
        profile_y = {'star_banner_partition':82,'blue_crystal_planter':96,'votive_light_channel':207}[family]
        profile = im.crop((0, profile_y, thickness, profile_y+1)).resize((thickness, 12), Image.Resampling.NEAREST)
        im.paste(profile, (0, 0)); im.paste(profile, (0, 244))
    a = np.array(im)
    # Re-establish near-opaque source core after uniform resizing.
    a[:, :, 3] = np.where(a[:, :, 3] >= 245, 255, a[:, :, 3])
    a[a[:, :, 3] < 8, 3] = 0
    return Image.fromarray(a, 'RGBA')

def compose(mask, h, v, joint, bottom):
    out = Image.new('RGBA', (320, 384))
    hy = 224 - h.height // 2
    vx = 160 - v.width // 2
    if mask & 8: out.alpha_composite(h.crop((0, 0, 128, h.height)), (32, hy))
    if mask & 2: out.alpha_composite(h.crop((128, 0, 256, h.height)), (160, hy))
    if mask & 1: out.alpha_composite(v.crop((0, 0, v.width, 128)), (vx, 96))
    if mask & 4: out.alpha_composite(v.crop((0, 128, v.width, 256)), (vx, 224))
    if mask not in (5, 10):
        out.alpha_composite(joint, (160 - joint.width // 2, bottom - joint.height))
    a = np.array(out)
    for bit, axis, source, destinations in [(8, 'x', 32, range(28, 32)), (2, 'x', 287, range(288, 292)), (1, 'y', 96, range(92, 96)), (4, 'y', 351, range(352, 356))]:
        if not mask & bit: continue
        profile = a[:, source, :].copy() if axis == 'x' else a[source, :, :].copy()
        # Only identical fully opaque core may share a4px bleed. Partial-alpha
        # contours retain half-open logical ownership, preventing double blend.
        profile[profile[:, 3] != 255] = 0
        for k in destinations:
            if axis == 'x': a[:, k, :] = profile
            else: a[k, :, :] = profile
    return Image.fromarray(a, 'RGBA')

assets = []; sources = []; images = {}; checks = []
for family, spec in SPECS.items():
    source_rel = f'sources/{family}_native.png'
    source = R / source_rel
    sources.append({'file': source_rel, 'sha256': sha(source), 'generatedBy': 'built-in image_gen', 'runtimeSelectable': False})
    parts = {name: component(source, box) for name, box in BOXES.items()}
    for name, im in parts.items(): save(im, R / f'production/components/{family}_{name}_native.png')
    h = carrier(parts['h'], 'h', spec['h'], family)
    v = carrier(parts['v'], 'v', spec['v'], family)
    joint = parts['joint']; mw, mh = spec['joint']; scale = min(mw / joint.width, mh / joint.height)
    joint = joint.resize((round(joint.width * scale), round(joint.height * scale)), Image.Resampling.LANCZOS)
    for name, im in [('h_carrier', h), ('v_carrier', v), ('joint_registered', joint)]: save(im, R / f'production/components/{family}_{name}.png')
    for mask in range(16):
        im = compose(mask, h, v, joint, spec['bottom'])
        file = f'decor/connected/{family}/n{mask:02d}.png'
        save(im, R / file); images[(family, mask)] = im
        assets.append({'file': file, 'width': 320, 'height': 384, 'ppu': 256,
                       'pivot': [0.5, 160/384], 'logicalRect': [32,96,256,256], 'footprintCells':[1,1],
                       'family': family, 'kind': 'connected_tile', 'neighborMask': mask,
                       'sha256': sha(R/file), 'sourceFiles':[source_rel], 'nativeComponentRectsXYXY':BOXES, 'registeredPortCorePixels':12,
                       'neighborBits': {'N':1,'E':2,'S':4,'W':8},
                       'nativeRgbRepainted':False,'deriveColliderFromAlpha':False,
                       'straightHasCenterJoint':False})

    arrays = {m: np.array(images[(family,m)]) for m in range(16)}
    pairs = 0; safe_overlap_pixels = 0
    for m,a in arrays.items():
        outside = a[:,:,3].copy(); outside[96:352,32:288]=0
        assert not np.any((outside>0)&(outside<255)), (family,m,'partial alpha padding')
        guard=outside.copy();guard[92:356,28:292]=0;assert not guard.any(),(family,m,'bleed exceeds4')
        for bit,probe in [(1,a[96,:,:]),(2,a[:,287,:]),(4,a[351,:,:]),(8,a[:,32,:])]:
            assert bool(probe[:,3].max()) == bool(m&bit), (family,m,bit,'wrong port presence')
    for m,a in arrays.items():
        for n,b in arrays.items():
            for axis,connected in [('h',bool(m&2 and n&8)),('v',bool(m&4 and n&1))]:
                if not connected:continue
                if axis=='h':
                    assert np.array_equal(a[:,287,:], b[:,32,:]),(family,m,n,'EW crosssection')
                    aa,bb=a[:,256:320,:],b[:,0:64,:]
                else:
                    assert np.array_equal(a[351,:,:], b[96,:,:]),(family,m,n,'SN crosssection')
                    aa,bb=a[256:384,:,:],b[0:128,:,:]
                overlap=(aa[:,:,3]>0)&(bb[:,:,3]>0)
                unsafe=overlap & ((aa[:,:,3]!=255)|(bb[:,:,3]!=255)|np.any(aa[:,:,:3]!=bb[:,:,:3],axis=2))
                assert not unsafe.any(),(family,m,n,axis,'unsafe overlap')
                safe_overlap_pixels+=int(overlap.sum());pairs+=1
    checks.append({'family':family,'maskCount':16,'nativePortRgbaExact':True,'compatiblePortPairs':pairs,'unsafeOverlapPixels':0,'safeOpaqueOverlapPixelObservations':safe_overlap_pixels,'actualUnity':'NOT_RUN'})

manifest={'schemaVersion':1,'packageId':'ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01','themeDisplayName':'낙일 성소',
          'assets':assets,'groups':[],'sources':sources,'generationPrompts':'generation_prompts.json',
          'maskBits':{'N':1,'E':2,'S':4,'W':8},'pixelsPerUnit':256,
          'portPolicy':{'logicalRect':[32,96,256,256],'opaquePortBleedPixels':4,'partialAlphaPaddingPixels':0,'identicalOpaqueOverlapAllowed':True},
          'sourcePolicy':'New built-in imagegen native RGBA. Mechanical component cutout, near-opaque alpha normalization, uniform rescale, native cross-section extrusion and kit placement only. No native RGB repaint.',
          'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
write(R/'manifest.json',manifest)

font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def font(n):return ImageFont.truetype(font_path,n)
sheet=Image.new('RGBA',(1800,1300),(29,22,48,255));d=ImageDraw.Draw(sheet)
d.text((25,18),'SUNSET SANCTUM / CONNECTED DECOR / 16 MASKS EACH',font=font(27),fill=(255,227,164))
d.text((25,58),'New native parts / N1 E2 S4 W8 / no per-cell straight joint / OFFLINE COMPOSITE',font=font(17),fill=(172,211,207))
for family_index,family in enumerate(SPECS):
    x0=family_index*600
    for mask in range(16):
        x=x0+10+(mask%4)*146;y=124+(mask//4)*270
        d.rectangle((x,y,x+138,y+228),fill=(53,36,67),outline=(92,67,100))
        im=images[(family,mask)].resize((140,168),Image.Resampling.LANCZOS)
        sheet.alpha_composite(im,(x,y+24))
        d.text((x+8,y+236),f'{mask:02d}',font=font(15),fill=(255,227,164))
    d.text((x0+16,1240),family,font=font(19),fill=(172,211,207))
save(sheet.convert('RGB'),R/'inspection/networks_b_16mask_contact.png')

cells={(x,1)for x in range(1,12)}|{(3,y)for y in range(1,7)}|{(x,6)for x in range(3,9)}|{(8,y)for y in range(3,7)}|{(x,3)for x in range(8,12)}|{(11,y)for y in range(1,4)}
neighbors=[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)]
example=Image.new('RGBA',(1800,2220),(29,22,48,255));d=ImageDraw.Draw(example)
d.text((26,18),'CONTINUOUS SANCTUM DECORATION / SAME NATIVE ART ACROSS TILE BORDERS',font=font(24),fill=(255,227,164))
layout=[]
for fi,family in enumerate(SPECS):
    ox,oy=90,115+fi*680;scale=.3125;cell=80
    d.text((ox,oy-45),family,font=font(23),fill=(172,211,207))
    for x,y in sorted(cells,key=lambda q:(q[1],q[0])):
        m=sum(bit for dx,dy,bit in neighbors if (x+dx,y+dy)in cells)
        spr=images[(family,m)].resize((100,120),Image.Resampling.LANCZOS)
        example.alpha_composite(spr,(ox+x*cell-10,oy+y*cell-30))
        layout.append({'family':family,'column':x,'rowPositiveDown':y,'mask':m,'file':f'decor/connected/{family}/n{m:02d}.png'})
    d.text((1120,oy+120),'Ends, L, T and cross share one joint.',font=font(21),fill=(255,227,164))
    d.text((1120,oy+160),'Straight runs share continuous carriers.',font=font(20),fill=(172,211,207))
    d.text((1120,oy+200),'Decor only; no gameplay collision edits.',font=font(19),fill=(200,179,205))
save(example.convert('RGB'),R/'inspection/networks_b_connected_layout.png')
write(R/'inspection/networks_b_connected_layout.json',{'cellPixels':80,'compositedOffline':True,'placements':layout})
qa={'status':'PASS_OFFLINE','runtimePngFiles':len(assets),'nativeSources':len(sources),'nativeSourceRgbaRepainted':False,'allPngDecoded':True,'compatiblePairCount':sum(x['compatiblePortPairs']for x in checks),'checks':checks,'manifestSha256':sha(R/'manifest.json'),'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualUnityPlay':'NOT_RUN'}
write(R/'networks_b_qa.json',qa)
print(json.dumps(qa,ensure_ascii=False,indent=2))
