"""Mechanical slicing/registration of native imagegen sprites; never draws sprite RGB."""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[1]
PACKAGE='ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

SPECS=[
 ('scenery_flat_3x1','group',[3,1],'scenery_storage_native.png',[0,0,1254,600],None),
 ('stacked_props_2x2','group',[2,2],'stacked_props_2x2_full_footprint_native.png',[0,0,1254,1254],None),
 ('mask_display_2x1','group',[2,1],'masks_puppets_native.png',[0,0,1086,620],None),
 ('puppet_storage_2x2','group',[2,2],'masks_puppets_native.png',[0,620,1086,1448],None),
 ('backstage_spotlight_duo_2x1','group',[2,1],'spotlights_steps_native.png',[0,0,1536,520],None),
 ('stage_steps_2x1','group',[2,1],'spotlights_steps_native.png',[0,520,1536,1024],None),
 ('ticket_stand','point',[1,1],'point_props_native.png',[0,0,430,455],[216,300]),
 ('folded_chair','point',[1,1],'folded_chair_native.png',[0,0,1024,1536],[192,320]),
 ('music_stand','point',[1,1],'point_props_native.png',[900,0,1340,455],[224,292]),
 ('tall_candelabra','point',[1,1],'point_props_native.png',[1340,0,1774,455],[216,320]),
 ('mask_pedestal','point',[1,1],'point_props_native.png',[0,455,430,887],[224,300]),
 ('script_pile','point',[1,1],'point_props_native.png',[430,455,900,887],[240,224]),
 ('sandbag_stack','point',[1,1],'point_props_native.png',[900,455,1340,887],[240,224]),
 ('flower_urn','point',[1,1],'point_props_native.png',[1340,455,1774,887],[224,288]),
]

def native_cut(source,box):
    im=Image.open(source).convert('RGBA').crop(box)
    a=np.array(im)
    # Keep native RGB/alpha on the principal connected component and its 2px AA fringe.
    # Native alpha<32 isolated resampling specks are not sprite details.
    labels,num=ndimage.label(a[:,:,3]>=32)
    sizes=np.bincount(labels.ravel());sizes[0]=0
    main=labels==sizes.argmax()
    keep=ndimage.binary_dilation(main,iterations=2)
    removed=int(np.count_nonzero((a[:,:,3]>0)&~keep))
    a[~keep]=0
    a[a[:,:,3]==0,:3]=0
    im=Image.fromarray(a)
    tight=im.getchannel('A').getbbox()
    if not tight:raise RuntimeError(source)
    return im.crop(tight),[box[0]+tight[0],box[1]+tight[1],box[0]+tight[2],box[1]+tight[3]],removed

assets=[];groups=[];qa=[]
for key,kind,footprint,source_name,box,maxsize in SPECS:
    source=ROOT/'sources'/source_name
    native,crop,removed=native_cut(source,box)
    logical_w,logical_h=[x*256 for x in footprint]
    if kind=='group':
        # Square pile uses a native two-depth-row arrangement. Its 4px outer
        # cap outlines overhang the logical width; the base body fills it.
        target_width=520 if key=='stacked_props_2x2' else round(logical_w*.965)
        scale=target_width/native.width
    else:
        scale=min(maxsize[0]/native.width,maxsize[1]/native.height)
    width=max(1,round(native.width*scale));height=max(1,round(native.height*scale))
    art=native.resize((width,height),Image.Resampling.LANCZOS)
    overhang=max(0,math.ceil((height-logical_h)/32)*32)
    top=32+overhang
    canvas=Image.new('RGBA',(logical_w+64,logical_h+top+32))
    dest=((canvas.width-width)//2,top+logical_h-height)
    canvas.alpha_composite(art,dest)
    rel=f'decor/{"groups" if kind=="group" else "points"}/{key}.png'
    path=ROOT/rel;path.parent.mkdir(parents=True,exist_ok=True);canvas.save(path)
    rect=[32,top,logical_w,logical_h]
    pivot=[.5,(32+logical_h/2)/canvas.height]
    item={'file':rel,'family':key,'kind':kind,'width':canvas.width,'height':canvas.height,'ppu':256,'pivot':pivot,'logicalRect':rect,'footprintCells':footprint,'sha256':sha(path),'sourceFiles':[f'sources/{source_name}'],'sourceCropBox':crop,'sourceSheetRegion':box,'uniformScale':scale,'sourceAlphaCleanup':'largest alpha>=32 component plus 2px native AA fringe; zero detached specks and RGB of transparent pixels only'}
    assets.append(item);groups.append({'key':key,'file':rel,'footprintCells':footprint})
    alpha=canvas.getchannel('A');bbox=alpha.getbbox()
    a=np.array(canvas)[:,:,3]
    ground=a[top:top+logical_h,32:32+logical_w]
    ground_mask=ground>=128
    ground_bbox=Image.fromarray((ground_mask*255).astype('uint8')).getbbox()
    row_occupancy=[float(np.count_nonzero(ground_mask[i*256:(i+1)*256]))/(logical_w*256) for i in range(footprint[1])]
    row={'key':key,'decodedMode':canvas.mode,'canvas':[canvas.width,canvas.height],'alphaBBox':list(bbox),'declaredFootprint':footprint,'logicalRect':rect,'nativeCropBox':crop,'runtimeArtSize':[width,height],'widthFill':width/logical_w,'groundOpaqueBBox':list(ground_bbox),'groundOpaqueWidthFill':(ground_bbox[2]-ground_bbox[0])/logical_w,'groundOpaqueHeightFill':(ground_bbox[3]-ground_bbox[1])/logical_h,'groundRowAlpha128Occupancy':row_occupancy,'removedDetachedAlphaSpecks':removed,'transparentBorder':all(v==0 for v in [alpha.getextrema()[0]])}
    assert bbox[0]>0 and bbox[1]>0 and bbox[2]<canvas.width and bbox[3]<canvas.height
    if kind=='group':assert width/logical_w>=.9
    if key=='stacked_props_2x2':
        assert row['groundOpaqueWidthFill']>=.9 and row['groundOpaqueHeightFill']>=.9
        assert all(v>=.35 for v in row_occupancy)
    assert 0<pivot[1]<1
    qa.append(row)

manifest={'schemaVersion':1,'packageId':PACKAGE,'producer':'groups_b_points','assets':assets,'groups':groups}
(ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(ROOT/'manifest_sha256.txt').write_text(sha(ROOT/'manifest.json')+'\n')
sources=[{'file':str(p.relative_to(ROOT)),'sha256':sha(p),'width':Image.open(p).width,'height':Image.open(p).height,'role':'discarded_native_draft' if 'draft' in p.name else 'native_source'} for p in sorted((ROOT/'sources').glob('*.png'))]
(ROOT/'native_sources_manifest.json').write_text(json.dumps({'sources':sources},indent=2)+'\n')
old_path=ROOT/'Authoring/pre_footprint_fix_shas.json'
unchanged=[]
if old_path.exists():
    old=json.loads(old_path.read_text())
    for path,digest in old.items():
        if path!='decor/groups/stacked_props_2x2.png':
            assert sha(ROOT/path)==digest,path
            unchanged.append(path)
report={'status':'PASS_OFFLINE_ART_EXPORT','assetCount':len(assets),'wholeGroupCount':6,'pointCount':8,'distinctDecorationFamilies':14,'artGeneration':'built-in imagegen; original native sources retained','postprocess':'mechanical crop/alpha-speck cleanup/uniform resample/registration only, no RGB recolor or procedural drawing','manifestSha256':sha(ROOT/'manifest.json'),'footprintFix':'stacked_props_2x2 native redrawn as two depth rows; other13 PNG hashes unchanged','unchangedOtherPngCount':len(unchanged),'checks':qa,'unityImport':'NOT_RUN','unityCompile':'NOT_RUN','unityPlayMode':'NOT_RUN'}
(ROOT/'groups_b_qa.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')

# The contact sheet is a review composition, never runtime sprite art.
sheet=Image.new('RGB',(1680,1540),'#211c37');draw=ImageDraw.Draw(sheet)
try:font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
except OSError:font=ImageFont.load_default()
for i,item in enumerate(assets):
    col=i%4;row=i//4;x=col*420;y=row*380
    draw.rounded_rectangle((x+10,y+10,x+410,y+370),12,fill='#302744')
    im=Image.open(ROOT/item['file']).convert('RGBA');im.thumbnail((390,305),Image.Resampling.LANCZOS)
    sheet.paste(im,(x+(420-im.width)//2,y+20+(305-im.height)//2),im)
    draw.text((x+20,y+332),item['family'],font=font,fill='#f6ddb1')
    draw.text((x+20,y+355),f"{item['footprintCells'][0]} x {item['footprintCells'][1]} cells",font=font,fill='#a5a5be')
sheet.save(ROOT/'Inspection/groups_b_contact_sheet.png')
print(json.dumps({'assets':len(assets),'manifestSha256':sha(ROOT/'manifest.json'),'nativeSources':len(sources),'contact':'Inspection/groups_b_contact_sheet.png'}))
