"""Offline Ranch context: native sprites placed mechanically; never a Unity capture."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse, hashlib, io, json, os

O=Path(__file__).resolve().parent
W=O.parent
BASE=W/'reference/ANIMOL_STARWAY_RANCH_CARTOON_V12'
PACKAGE='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'
P=W/'package_staging'/PACKAGE
SNAP=W/'examples_exports/Examples/stage_snapshot.json'
WIDTH,HEIGHT,CELL=1600,3000,64
ORIGIN=(224,2500)
N4=[(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]
N8=[(0,1,1),(1,1,2),(1,0,4),(1,-1,8),(0,-1,16),(-1,-1,32),(-1,0,64),(-1,1,128)]
MEADOW='raised_flower_meadow'

def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def font(size):return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',size)
def save(im,p):
    data=io.BytesIO();im.save(data,format='PNG');raw=data.getvalue()
    check=Image.open(io.BytesIO(raw));check.load();assert check.size==im.size
    pending=p.with_suffix('.pending');pending.write_bytes(raw);os.replace(pending,p)
    check=Image.open(p);check.load()

def u32(x):return x&0xffffffff
def stable_hash(seed,x,y,layer,height,group):
    h=2166136261
    for c in group:h=u32((h^ord(c))*16777619)
    h=u32((h^u32(seed))*16777619)
    h^=u32(u32(x)*0x9e3779b9);h^=u32(u32(y)*0x85ebca6b)
    h^=u32(u32(layer)*0xc2b2ae35);h^=u32(u32(height)*0x27d4eb2f)
    h^=h>>16;h=u32(h*0x7feb352d);h^=h>>15;h=u32(h*0x846ca68b)
    return u32(h^(h>>16))

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
    # The first opening coincides with the two empty meadow cells; all rendered
    # decorations stay on land. These are inspection-only void cells, no game map.
    holes={(5,17),(6,17)}|{(x,y)for x in[8,9]for y in[4,5]}
    assert holes<=cells
    return cells-holes,holes

def selections(s,new_assets,group_files):
    index={(r['x'],r['y'],r['logicalLayer'],r['heightKey']):r for r in s['networkCells']}
    def mask(r,x,y):
        result=0
        for dx,dy,b in N8 if r['family']==MEADOW else N4:
            t=index.get((x+dx,y+dy,r['logicalLayer'],r['heightKey']))
            if t and t['family']==r['family'] and t['connectionGroup']==r['connectionGroup']:result|=b
        return result
    rows=[]
    for r in s['networkCells']:
        n=mask(r,r['x'],r['y']);meadow=r['family']==MEADOW
        file=f"decor/connected/{r['family']}/n{n:03d}.png" if meadow else f"decor/connected/{r['family']}/n{n:02d}.png"
        rows.append(dict(file=file,x=r['x']+.5,y=r['y']+.5,sortingOrder=r['sortingOrder'],role='new_connected_decor',neighborMask=n,usesEightNeighbors=meadow))
        if meadow:
            h=stable_hash(s['seed'],r['x'],r['y'],r['logicalLayer'],r['heightKey'],r['connectionGroup'])
            if h%5==0:
                variant=(h//5)%4+1
                rows.append(dict(file=f'decor/accents/meadow/flowers_{variant:02d}.png',x=r['x']+.5,y=r['y']+.5,sortingOrder=r['sortingOrder']+1,role='new_seeded_meadow_flower',accentHash=h,extraOccupancyClaim=False))
            continue
        post=f"decor/connected/{r['family']}/periodic_post.png"
        if n not in(5,10)or post not in new_assets:continue
        dx,dy=(1,0)if n==10 else(0,1)
        coord=r['x']if n==10 else r['y']
        phase=stable_hash(s['seed'],0,0,r['logicalLayer'],r['heightKey'],r['connectionGroup'])%s['postSpacing']
        if (coord-phase)%s['postSpacing']==0 and mask(r,r['x']-dx,r['y']-dy)==n and mask(r,r['x']+dx,r['y']+dy)==n:
            rows.append(dict(file=post,x=r['x']+.5,y=r['y']+.5,sortingOrder=r['sortingOrder']+1,role='new_optional_periodic_post',extraOccupancyClaim=False))
    for r in s['groupPlacements']:
        file=group_files[r['groupKey']];fw,fh=new_assets[file]['footprintCells']
        rows.append(dict(file=file,x=r['anchorX']+fw/2,y=r['anchorY']+fh/2,sortingOrder=r['sortingOrder'],role='new_whole_or_point_decor',placementId=r['placementId']))
    return sorted(rows,key=lambda r:r['sortingOrder'])

def edge_hash(x,row,seed):
    h=u32(seed)^u32(u32(x)*0x9e3779b9)^u32(u32(row)*0x85ebca6b)
    h^=h>>16;h=u32(h*0x7feb352d);h^=h>>15;h=u32(h*0x846ca68b)
    return u32(h^(h>>16))
def support_plan(land,holes,seed):
    """Mechanical Python port of baseline EdgeSelector grass branch (row-down y)."""
    supports=[];collars=[]
    for y in range(28):
        exposed=sorted(x for x,yy in land if yy==y and(x,y-1)not in land);i=0
        while i<len(exposed):
            start=exposed[i];j=i+1;hole=(start,y-1)in holes
            while j<len(exposed)and exposed[j]==exposed[j-1]+1 and((exposed[j],y-1)in holes)==hole:j+=1
            count=j-i
            def side(x,d):
                if(x+d,y)not in land:return 'end'
                return 'concave'if(x+d,y-1)in land else 'continue'
            left,right=side(start,-1),side(start+count-1,1)
            row=27-y
            state=edge_hash(start,row,u32(seed^u32(count*0x27d4eb2d)^ord('G')))
            def rand(maximum):
                nonlocal state
                assert maximum>0
                state=u32(state+0x9e3779b9);z=state;z^=z>>16;z=u32(z*0x7feb352d);z^=z>>15;z=u32(z*0x846ca68b)
                return u32(z^(z>>16))%maximum
            def add(offset,length,kind):
                lm=left if offset==0 else 'continue';rm=right if offset+length==count else 'continue'
                variant=rand(2);depth='medium'if rand(2)==0 else'deep'
                file=f'skirts/spans/grass/exterior/l{length}_L{lm}_R{rm}_v{variant}.png'
                supports.append(dict(file=file,x=start+offset+length/2,y=y+.5,spanCells=length,internalHole=False,parentRunCells=count,kind=kind,depthClass=depth,leftMode=lm,rightMode=rm,variant=variant))
            if not hole:
                if count<=4:add(0,count,'fused')
                else:
                    position=rand(min(2,count-3));made=0
                    while position<count:
                        remaining=count-position
                        maxlen=min(3,remaining-2 if made==0 else remaining)
                        length=1+rand(max(1,maxlen));add(position,length,'hanging_cluster');made+=1;position+=length
                        after=count-position
                        if after<2:break
                        position+=1+rand(min(2,after-1))
            for x in range(start,start+count):
                mask=sum(b for dx,dy,b in N8 if(x+dx,y+dy)in land)
                collars.append(dict(file=f'skirts/collars/grass/n{mask:03d}.png',x=x+.5,y=y+.5,spanCells=1,internalHole=hole,parentRunCells=count,kind='continuous_shallow_collar',neighborMask=mask))
            i=j
    return supports,collars

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--terrain-only',action='store_true');args=ap.parse_args()
    ba={r['file']:r for r in read(BASE/'manifest.json')['assets']}
    for name in['background_manifest.json','span_manifest.json']:
        for r in read(BASE/name)['assets']:
            a=dict(r)
            if 'sha256'not in a:a['sha256']=sha(BASE/a['file'])
            ba[a['file']]=a
    bg=read(BASE/'background_scene_layout.json');s=read(SNAP);design=read(W/'catalog_design.json')
    sizes={a['key']:a['footprintCells']for a in design['groups']+design['points']}
    used_designs={r['family']for r in s['networkCells']}|{r['groupKey']for r in s['groupPlacements']}
    assert used_designs=={r['key']for r in design['networks']+design['groups']+design['points']}and len(used_designs)==26
    land,holes=ground_cells();claims=set()
    for r in s['networkCells']:claims.add((r['x'],r['y']))
    for r in s['groupPlacements']:
        fw,fh=sizes[r['groupKey']]
        for dy in range(fh):
            for dx in range(fw):claims.add((r['anchorX']+dx,r['anchorY']+dy))
    assert len(claims)==120 and claims<=land,sorted(claims-land)
    void={(x,y)for x in range(-1,19)for y in range(-1,29)if(x,y)not in land};outside=set();todo=[(-1,-1)]
    while todo:
        q=todo.pop()
        if q in outside or q not in void:continue
        outside.add(q);todo.extend((q[0]+dx,q[1]+dy)for dx,dy in[(1,0),(-1,0),(0,1),(0,-1)])
    assert void-outside==holes
    out=Image.new('RGBA',(WIDTH,HEIGHT),(17,32,51,255));cache={};used_baseline={};used_new={};layout=[]
    na={}
    def blit(file,x,y,scale=1.0,opacity=1.0,new=False,role='background',order=0):
        catalog=na if new else ba;root=P if new else BASE;a=catalog[file]
        expected=a['sha256'];(used_new if new else used_baseline)[file]=expected
        key=(new,file,scale,opacity)
        if key not in cache:
            im=Image.open(root/file).convert('RGBA');im.load();ppu=a.get('ppu',a.get('pixelsPerUnit',256))
            size=(max(1,round(im.width/ppu*CELL*scale)),max(1,round(im.height/ppu*CELL*scale)))
            im=im.resize(size,Image.Resampling.LANCZOS)
            if opacity!=1:im.putalpha(im.getchannel('A').point(lambda n:round(n*opacity)))
            cache[key]=im
        im=cache[key];px=ORIGIN[0]+x*CELL;py=ORIGIN[1]-y*CELL
        out.alpha_composite(im,(round(px-a['pivot'][0]*im.width),round(py-(1-a['pivot'][1])*im.height)))
        layout.append(dict(file=file,x=x,y=y,scale=scale,opacity=opacity,role=role,sortingOrder=order,source='new_addon'if new else'baseline_context_only'))
    sky=bg['skyBase']['file'];a=ba[sky];ppu=a.get('ppu',a.get('pixelsPerUnit',256))
    cover=max((WIDTH/CELL)/(a['width']/ppu),(HEIGHT/CELL)/(a['height']/ppu))*1.02
    blit(sky,(WIDTH/2-ORIGIN[0])/CELL,(ORIGIN[1]-HEIGHT/2)/CELL,cover,role='sky_cover',order=-100)
    overrides={
        'far_cloud_left':(1.3,33.2,.84),'far_cloud_right':(19.5,30.8,.8),
        'mid_cloud_lower':(-2.7,13.6,.9),'mid_cloud_upper':(21.5,18.7,.85),
        'near_cloud_left':(-2.2,2.1,1.1),'near_cloud_right':(20.7,-1.8,1.15),
        'far_ranch_barn':(16.1,34.1,.66),'far_ranch_windmill':(.5,-2.3,.58)}
    for r in sorted(bg['layers'],key=lambda r:r['sortingOrder']):
        file=r['file'];isstar='/stars/'in file
        if r['id']in overrides:x,y,scale=overrides[r['id']]
        else:x,y=r['positionWorld'][0]*1.9+9,r['positionWorld'][1]*1.6+19;scale=r['localScale'][0]*1.2
        role='star'if isstar else'cloud'if'cloud'in r['id']else'far_ranch_islet'
        blit(file,x,y,scale,r.get('color',[1,1,1,1])[3],role=role,order=r['sortingOrder'])
    supports,collars=support_plan(land,holes,s['seed'])
    for r in supports:blit(r['file'],r['x'],r['y'],role='native_fused_or_sparse_underside',order=-10)
    for r in collars:blit(r['file'],r['x'],r['y'],role='native_shallow_collar',order=-5)
    terrain=[]
    for x,y in sorted(land,key=lambda q:(-q[1],q[0])):
        n=sum(b for dx,dy,b in N8 if(x+dx,y+dy)in land);file=f'tiles/grass/organic256/n{n:03d}.png'
        blit(file,x+.5,y+.5,role='native_grass_context',order=0);terrain.append(dict(x=x,y=y,material='G',neighborMask=n,file=file))
    new_rows=[];pin=None
    if not args.terrain_only:
        manifest=read(P/'manifest.json');na={a['file']:a for a in manifest['assets']};pin=sha(P/'manifest.json')
        assert pin==s['sourceManifestSha256']==(P/'manifest_sha256.txt').read_text().strip()
        assert len(na)==362 and manifest['packageId']==PACKAGE
        new_rows=selections(s,na,{g['key']:g['file']for g in manifest['groups']})
        for r in new_rows:blit(r['file'],r['x'],r['y'],new=True,role=r['role'],order=r['sortingOrder'])
        assert sum(r['role']=='new_connected_decor'for r in new_rows)==65
        assert sum(r['role']=='new_whole_or_point_decor'for r in new_rows)==20
        assert all(r['file']in na for r in new_rows)
    dr=ImageDraw.Draw(out);dr.rectangle((30,28,1570,160),fill=(8,25,37,245))
    title='STARWAY RANCH / TERRAIN CONTEXT DRAFT'if args.terrain_only else'STARWAY RANCH / CONNECTED DECOR V02'
    dr.text((58,48),title,font=font(39),fill='#ffe5b4')
    dr.text((58,108),'OFFLINE ART CONTEXT / UNITY IMPORT, COMPILE, PLAY: NOT_RUN',font=font(24),fill='#8ce0c1')
    dr.rectangle((30,2840,1570,2970),fill=(8,25,37,240))
    footer='TERRAIN-ONLY WORKING DRAFT / NEW DECORATION NOT RENDERED YET'if args.terrain_only else'26 designs / 5 cardinal families + raw256 meadow + 20 whole props'
    dr.text((58,2860),footer,font=font(24),fill='#ffe5b4')
    dr.text((58,2907),'Existing starry sky, grassland and underside are read-only context.',font=font(22),fill='#a3d6c2')
    name='terrain_context_draft.png'if args.terrain_only else'RANCH_SCENE_CONTEXT.png';save(out,O/name)
    if args.terrain_only:
        print(json.dumps(dict(output=str(O/name),groundCells=len(land),decorClaimCells=len(claims),internalVoidCells=len(holes))));return
    for f,dg in used_baseline.items():assert sha(BASE/f)==dg,f
    for f,dg in used_new.items():assert sha(P/f)==dg,f
    stars=[r for r in layout if r['role']=='star'];clouds=[r for r in layout if r['role']=='cloud']
    assert stars and clouds and max(r['sortingOrder']for r in stars)<min(r['sortingOrder']for r in clouds)
    assert not any(any(word in r['file'].lower()for word in['moon','planet','sun'])for r in layout)
    assert not any(r['file'].startswith(('props/','animation/','devices/'))for r in layout if r['source']=='baseline_context_only')
    write(O/'context_layout.json',dict(schema='offline_native_sprite_context_layout',packageId=PACKAGE,artManifestSha256=pin,
        typedSnapshotSha256=sha(SNAP),pixelSize=[WIDTH,HEIGHT],pixelsPerCell=CELL,worldOriginPixels=list(ORIGIN),
        groundContextCells=terrain,decorClaimsAllOnLand=True,internalVoidCells=sorted(map(list,holes)),
        undersideBodyChunks=supports,continuousShallowCollars=collars,newSelections=new_rows,renderLayers=layout,
        inspectionOnlyGround=True,notProductionMap=True,actualUnityCapture=False))
    write(O/'context_provenance.json',dict(packageId=PACKAGE,artManifestSha256=pin,
        baselinePackage='ANIMOL_STARWAY_RANCH_CARTOON_V12',baselineManifestSha256=sha(BASE/'manifest.json'),
        baselineArchiveSha256=sha(W/'reference_download/ANIMOL_STARWAY_RANCH_CARTOON_V12.zip'),
        baselineAssets=[dict(file=f,sha256=dg,role='read_only_context_not_runtime_addon')for f,dg in sorted(used_baseline.items())],
        newAddonAssets=[dict(file=f,sha256=dg)for f,dg in sorted(used_new.items())],nativePixelsRepainted=False,
        method='Mechanical native RGBA composition, uniform PPU scaling and pivot placement; technical caption only.',
        undersidePlannerReference='Runtime/StarwayRanchV12EdgeSelector.cs + MaterialSelector.cs: row-down seed, fused1-4, sparse longgrass bodies, continuous collars and holecollars only.',
        baselinePngCopiedIntoAddonRuntime=False,actualUnityCapture=False))
    write(O/'context_preview_qa.json',dict(status='PASS_OFFLINE_CONTEXT_COMPOSITION',packageId=PACKAGE,artManifestSha256=pin,
        output=name,outputSha256=sha(O/name),pixelSize=[WIDTH,HEIGHT],runtimeNewSelections=len(new_rows),semanticDesigns=26,
        networkCells=65,wholeOrPointPlacements=20,optionalPeriodicPosts=sum(r['role']=='new_optional_periodic_post'for r in new_rows),
        seededMeadowFlowerOverlays=sum(r['role']=='new_seeded_meadow_flower'for r in new_rows),flowerOverlayExtraClaims=0,
        decorClaimCells=len(claims),nativeContextGroundCells=len(land),internalVoidCells=len(holes),allDecorClaimsOnNativeLand=True,
        nativeUndersideBodyChunks=len(supports),nativeContinuousCollars=len(collars),enclosedGrassHolesUseCollarOnly=True,
        baselineOnlyContextAssets=len(used_baseline),newAssetsUsed=len(used_new),allUsedFileSha256Verified=True,
        uniqueSunRenderers=0,uniqueMoonRenderers=0,uniquePlanetRenderers=0,starOrdersBehindCloudOrders=True,
        backgroundPlacementIsInspectionOnly=True,sourceRuntimePngMutated=False,
        actualUnityImport='NOT_RUN',actualCSharpCompile='NOT_RUN',actualUnityPlay='NOT_RUN',actualUnityCapture=False))
    print(json.dumps(dict(output=str(O/name),sha256=sha(O/name),groundCells=len(land),claims=len(claims),newSelections=len(new_rows)),indent=2))

if __name__=='__main__':main()
