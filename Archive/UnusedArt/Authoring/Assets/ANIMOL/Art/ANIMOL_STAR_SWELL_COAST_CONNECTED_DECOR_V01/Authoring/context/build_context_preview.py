"""Native-sprite-only offline Coast art context. Never a Unity capture or gameplay map."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse, hashlib, io, json, math, os, random

O=Path(__file__).resolve().parent
W=O.parent
BASE=W/'reference/ANIMOL_STAR_SWELL_COAST_CARTOON_V01'
PACKAGE='ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01'
P=W/'package_staging'/PACKAGE
SNAP=W/'examples_exports/Examples/stage_snapshot.json'
WIDTH,HEIGHT,CELL=1600,3000,64
ORIGIN=(224,2500)
BG_OFFSET=(9,24.0625)
NEIGHBORS=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]

def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def font(size):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',size)
def save(im,p):
    data=io.BytesIO();im.save(data,format='PNG');raw=data.getvalue()
    q=Image.open(io.BytesIO(raw));q.load();assert q.size==im.size
    pending=p.with_suffix('.pending');pending.write_bytes(raw);os.replace(pending,p)
    q=Image.open(p);q.load()

def stable_hash(seed,layer,height,group):
    h=2166136261
    for c in group:h=((h^ord(c))*16777619)&0xffffffff
    h=((h^(seed&0xffffffff))*16777619)&0xffffffff
    h^=((layer&0xffffffff)*0xc2b2ae35)&0xffffffff
    h^=((height&0xffffffff)*0x27d4eb2f)&0xffffffff
    h^=h>>16;h=(h*0x7feb352d)&0xffffffff
    h^=h>>15;h=(h*0x846ca68b)&0xffffffff
    return (h^(h>>16))&0xffffffff

def ground_cells():
    cells={(x,y)for x in range(18)for y in range(28)}
    for y in [6,7,8,9,10,11,12,16,17]:cells.discard((0,y))
    for y in [6,7,8,9,10,11,12,13,14,15]:cells.discard((17,y))
    for x in [0,1]:cells.discard((x,0))
    cells.discard((0,1))
    for x in range(18):
        if not 1<=x<=16:cells.discard((x,25))
        if not 4<=x<=14:cells.discard((x,26))
        if not 6<=x<=11:cells.discard((x,27))
    holes={(x,y)for x in [8,9]for y in [18,19]}|{(x,y)for x in[8,9]for y in[4,5]}
    assert holes<=cells
    return cells-holes,holes

def selections(snapshot,new_assets):
    index={(r['x'],r['y'],r['logicalLayer'],r['heightKey']):r for r in snapshot['networkCells']}
    def mask(r,x,y):
        result=0
        for dx,dy,b in [(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]:
            t=index.get((x+dx,y+dy,r['logicalLayer'],r['heightKey']))
            if t and t['family']==r['family'] and t['connectionGroup']==r['connectionGroup']:result|=b
        return result
    rows=[]
    for r in snapshot['networkCells']:
        n=mask(r,r['x'],r['y']);file=f"decor/connected/{r['family']}/n{n:02d}.png"
        rows.append(dict(file=file,x=r['x']+.5,y=r['y']+.5,sortingOrder=r['sortingOrder'],role='new_connected_decor',mask=n))
        post=f"decor/connected/{r['family']}/periodic_post.png"
        if n not in(5,10)or post not in new_assets:continue
        dx,dy=(1,0)if n==10 else(0,1)
        coord=r['x']if n==10 else r['y']
        phase=stable_hash(snapshot['seed'],r['logicalLayer'],r['heightKey'],r['connectionGroup'])%snapshot['postSpacing']
        if (coord-phase)%snapshot['postSpacing']==0 and mask(r,r['x']-dx,r['y']-dy)==n and mask(r,r['x']+dx,r['y']+dy)==n:
            rows.append(dict(file=post,x=r['x']+.5,y=r['y']+.5,sortingOrder=r['sortingOrder']+1,role='new_optional_periodic_post'))
    group_files={g['key']:g['file']for g in read(P/'manifest.json')['groups']}
    for r in snapshot['groupPlacements']:
        file=group_files[r['groupKey']];fw,fh=new_assets[file]['footprintCells']
        rows.append(dict(file=file,x=r['anchorX']+fw/2,y=r['anchorY']+fh/2,sortingOrder=r['sortingOrder'],role='new_whole_or_point_decor',placementId=r['placementId']))
    return sorted(rows,key=lambda r:r['sortingOrder'])

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--terrain-only',action='store_true');args=ap.parse_args()
    bmanifest=read(BASE/'manifest.json');ba={r['file']:r for r in bmanifest['assets']}
    bg=read(BASE/'background_scene_layout.json');snap=read(SNAP)
    design=read(W/'catalog_design.json');sizes={a['key']:a['footprintCells']for a in design['groups']+design['points']}
    used_designs={r['family']for r in snap['networkCells']}|{r['groupKey']for r in snap['groupPlacements']}
    expected_designs={r['key']for r in design['networks']+design['groups']+design['points']}
    assert used_designs==expected_designs and len(used_designs)==26
    land,holes=ground_cells();claims=set()
    for r in snap['networkCells']:claims.add((r['x'],r['y']))
    for r in snap['groupPlacements']:
        fw,fh=sizes[r['groupKey']]
        for dy in range(fh):
            for dx in range(fw):claims.add((r['anchorX']+dx,r['anchorY']+dy))
    assert claims<=land,sorted(claims-land)
    assert len(claims)==104
    # This inspection island is not exported as an authoritative game map.
    # Only the typed decoration positions are exact; ground is native style context.
    out=Image.new('RGBA',(WIDTH,HEIGHT),(13,43,54,255))
    cache={};used_baseline={};used_new={};layout=[]
    def blit(file,x,y,sx=1.0,opacity=1.0,new=False,role='background',order=0):
        catalog=na if new else ba;root=P if new else BASE;a=catalog[file]
        p=root/file;expected=a['sha256']
        if new:used_new[file]=expected
        else:used_baseline[file]=expected
        key=(new,file,sx,opacity)
        if key not in cache:
            im=Image.open(p).convert('RGBA');im.load()
            size=(max(1,round(im.width/a['ppu']*CELL*sx)),max(1,round(im.height/a['ppu']*CELL*sx)))
            im=im.resize(size,Image.Resampling.LANCZOS)
            if opacity!=1:im.putalpha(im.getchannel('A').point(lambda n:round(n*opacity)))
            cache[key]=im
        im=cache[key];px=ORIGIN[0]+x*CELL;py=ORIGIN[1]-y*CELL
        out.alpha_composite(im,(round(px-a['pivot'][0]*im.width),round(py-(1-a['pivot'][1])*im.height)))
        layout.append(dict(file=file,x=x,y=y,scale=sx,opacity=opacity,role=role,sortingOrder=order,source='new_addon'if new else'baseline_context_only'))
    sky='backgrounds/coast/sky_base.png';a=ba[sky]
    cover=max((WIDTH/CELL)/(a['width']/a['ppu']),(HEIGHT/CELL)/(a['height']/a['ppu']))*1.02
    blit(sky,(WIDTH/2-ORIGIN[0])/CELL,(ORIGIN[1]-HEIGHT/2)/CELL,cover,role='sky_cover',order=-1100)
    # Place the source world layout relative to this inspection board; all native pixels unchanged.
    background=[]
    for r in bg['layers']:
        if r['id']=='frame_palm_west':continue
        x=r['positionWorld'][0]+BG_OFFSET[0];y=r['positionWorld'][1]+BG_OFFSET[1];scale=r['localScale'][0]
        if r['id']=='far_cloud_west':x,y=2.5,33.2;scale=1.6
        if r['id']=='far_cloud_east':x,y=17.9,31.5;scale=1.5
        if r['id']=='near_cloud_west':x,y=-3,8.6
        if r['id']=='near_cloud_east':x,y=21.2,1.6
        if r['id']=='frame_cliff_east':x,y=22.5,9.5
        if r['role']=='horizon':scale=(WIDTH/CELL)/(ba[r['file']]['width']/ba[r['file']]['ppu'])
        background.append((r['sortingOrder'],r['file'],x,y,scale,r.get('color',[1,1,1,1])[3],r['role']))
    # Global OceanField repeats only the fullcanvas carrier below exactly one fixed horizon.
    horizon=6.25+BG_OFFSET[1]
    for order,file,x,y,scale,opacity,role in sorted(background):
        if order>=-880:continue
        blit(file,x,y,scale,opacity,role=role,order=order)
    carrier='tiles/water/carrier.png';assert ba[carrier]['width']==ba[carrier]['height']==256
    for row in range(math.ceil(horizon+8)+1):
        for col in range(-4,23):blit(carrier,col+.5,horizon-row-.5,role='global_ocean_underlay',order=-880)
    for order,file,x,y,scale,opacity,role in sorted(background):
        if order>=-880:blit(file,x,y,scale,opacity,role=role,order=order)
    # Flood fill to distinguish the two internal holes from exterior gaps.
    void={(x,y)for x in range(-1,19)for y in range(-1,29)if(x,y)not in land};outside=set();todo=[(-1,-1)]
    while todo:
        q=todo.pop()
        if q in outside or q not in void:continue
        outside.add(q);todo.extend((q[0]+dx,q[1]+dy)for dx,dy in[(1,0),(-1,0),(0,1),(0,-1)])
    enclosed=void-outside;assert enclosed==holes
    supports=[]
    for y in range(28):
        exposed=sorted(x for x,yy in land if yy==y and(x,y-1)not in land);i=0
        while i<len(exposed):
            start=exposed[i];j=i+1
            while j<len(exposed)and exposed[j]==exposed[j-1]+1 and((exposed[j],y-1)in holes)==((start,y-1)in holes):j+=1
            count=j-i;remaining=count;offset=0;rng=random.Random(17041+start*97+y*131)
            while remaining:
                choices=[n for n in[2,3,4]if n<=remaining and(remaining-n==0 or remaining-n>=2)]
                n=remaining if remaining<=4 else rng.choice(choices)
                cap=(start,y-1)in holes;file=f'undersides/sandy_rock/{"cap"if cap else"span"}_{n:02d}.png'
                blit(file,start+offset+n/2,y+.5,role='native_shallow_hole_cap'if cap else'native_fused_exterior_support',order=-20)
                supports.append(dict(x=start+offset,y=y,spanCells=n,internalHole=cap,file=file,parentRunCells=count))
                remaining-=n;offset+=n
            i=j
    terrain=[]
    for x,y in sorted(land,key=lambda q:(-q[1],q[0])):
        mask=sum(b for dx,dy,b in NEIGHBORS if(x+dx,y+dy)in land)
        file=f'tiles/coast_sand/full256/n{mask:03d}.png'
        blit(file,x+.5,y+.5,role='native_coast_sand_context',order=0)
        terrain.append(dict(x=x,y=y,material='A',neighborMask=mask,file=file))
    new_rows=[];pin=None
    if not args.terrain_only:
        manifest=read(P/'manifest.json');na={a['file']:a for a in manifest['assets']};pin=sha(P/'manifest.json')
        assert pin==snap['sourceManifestSha256']==(P/'manifest_sha256.txt').read_text().strip()
        assert len(na)==118 and manifest['packageId']==PACKAGE
        new_rows=selections(snap,na)
        for r in new_rows:blit(r['file'],r['x'],r['y'],new=True,role=r['role'],order=r['sortingOrder'])
        assert len(new_rows)==79
        assert sum(r['role']=='new_whole_or_point_decor'for r in new_rows)==20
        assert sum(r['role']=='new_optional_periodic_post'for r in new_rows)==4
    # UI is a reference caption only; no new bitmap artwork is invented by this renderer.
    dr=ImageDraw.Draw(out)
    dr.rectangle((30,28,1570,160),fill=(8,25,37,245))
    title='STAR SWELL COAST / TERRAIN CONTEXT DRAFT'if args.terrain_only else'STAR SWELL COAST / CONNECTED DECOR V01'
    dr.text((58,48),title,font=font(39),fill='#ffe5b4')
    dr.text((58,108),'OFFLINE ART CONTEXT / UNITY IMPORT, COMPILE, PLAY: NOT_RUN',font=font(24),fill='#80e1dc')
    dr.rectangle((30,2840,1570,2970),fill=(8,25,37,240))
    footer='TERRAIN-ONLY WORKING DRAFT / NEW DECORATION NOT RENDERED YET'if args.terrain_only else'26 new designs / 6 connected families + 12 multi-cell + 8 single-cell'
    dr.text((58,2860),footer,font=font(24),fill='#ffe5b4')
    dr.text((58,2907),'Existing sky, ocean, sand and underside are reference context only.',font=font(22),fill='#98d4d4')
    name='terrain_context_draft.png'if args.terrain_only else'COAST_SCENE_CONTEXT.png';save(out,O/name)
    if args.terrain_only:
        print(json.dumps(dict(output=str(O/name),groundCells=len(land),decorClaimCells=len(claims),waterHoleCells=len(holes))));return
    for f,dg in used_baseline.items():assert sha(BASE/f)==dg,f
    for f,dg in used_new.items():assert sha(P/f)==dg,f
    sun=[r for r in layout if r['role']=='setting_sun'];horizons=[r for r in layout if r['role']=='horizon']
    assert len(sun)==len(horizons)==1
    stars=[r for r in layout if r['role']=='star'];clouds=[r for r in layout if r['role']=='cloud']
    assert stars and clouds and max(r['sortingOrder']for r in stars)<min(r['sortingOrder']for r in clouds)
    assert not any(r['file'].startswith(('props/','animation/','devices/'))for r in layout if r['source']=='baseline_context_only')
    source_baseline=[dict(file=f,sha256=dg,role='read_only_context_not_runtime_addon')for f,dg in sorted(used_baseline.items())]
    source_new=[dict(file=f,sha256=dg)for f,dg in sorted(used_new.items())]
    write(O/'context_layout.json',dict(schema='offline_native_sprite_context_layout',packageId=PACKAGE,artManifestSha256=pin,
        typedSnapshotSha256=sha(SNAP),pixelSize=[WIDTH,HEIGHT],pixelsPerCell=CELL,worldOriginPixels=list(ORIGIN),
        backgroundWorldOffset=list(BG_OFFSET),groundContextCells=terrain,decorClaimsAllOnLand=True,
        internalWaterHoleCells=sorted(map(list,holes)),supportChunks=supports,newSelections=new_rows,
        renderLayers=layout,notProductionMap=True,actualUnityCapture=False))
    write(O/'context_provenance.json',dict(packageId=PACKAGE,artManifestSha256=pin,
        baselinePackage='ANIMOL_STAR_SWELL_COAST_CARTOON_V01',baselineManifestSha256=sha(BASE/'manifest.json'),
        baselineArchiveSha256=sha(W/'reference_download/ANIMOL/ANIMOL_STAR_SWELL_COAST_CARTOON_V01.zip'),
        baselineAssets=source_baseline,newAddonAssets=source_new,nativePixelsRepainted=False,
        method='Mechanical native RGBA composition, uniform PPU scaling and pivot placement. Technical caption only.',
        baselinePngCopiedIntoAddonRuntime=False,actualUnityCapture=False))
    write(O/'context_preview_qa.json',dict(status='PASS_OFFLINE_CONTEXT_COMPOSITION',packageId=PACKAGE,artManifestSha256=pin,
        output=name,outputSha256=sha(O/name),pixelSize=[WIDTH,HEIGHT],runtimeNewSelections=len(new_rows),
        semanticDesigns=len(used_designs),networkCells=len(snap['networkCells']),wholeOrPointPlacements=len(snap['groupPlacements']),
        optionalPeriodicPosts=sum(r['role']=='new_optional_periodic_post'for r in new_rows),decorClaimCells=len(claims),
        nativeContextGroundCells=len(land),internalWaterHoleCells=len(holes),allDecorClaimsOnNativeLand=True,
        nativeContextSupportChunks=len(supports),baselineOnlyContextAssets=len(used_baseline),newAssetsUsed=len(used_new),
        allUsedFileSha256Verified=True,uniqueSunRenderers=len(sun),uniqueHorizonRenderers=len(horizons),
        starOrdersBehindCloudOrders=True,backgroundPlacementIsInspectionOnly=True,
        sourceRuntimePngMutated=False,actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualUnityPlay='NOT_RUN',actualUnityCapture=False))
    print(json.dumps(dict(output=str(O/name),sha256=sha(O/name),groundCells=len(land),claims=len(claims),newSelections=len(new_rows)),indent=2))

if __name__=='__main__':main()
