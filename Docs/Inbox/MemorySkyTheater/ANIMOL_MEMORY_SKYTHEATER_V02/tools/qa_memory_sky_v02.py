"""Read-only checks of the exported V02 sprite pixels."""
from pathlib import Path
from PIL import Image
from scipy.ndimage import label,binary_fill_holes
import numpy as np,json
ROOT=Path('output/ANIMOL_MEMORY_SKYTHEATER_V02')
CORNERS=[(1,9,-1,-1),(2,3,1,-1),(4,6,1,1),(8,12,-1,1)]
def topology(occ,x,y):
    o=sum(b for dx,dy,b in [(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)] if (x+dx,y+dy) not in occ)
    i=sum(b for b,s,dx,dy in CORNERS if not o&s and (x+dx,y+dy) not in occ)
    return o,i
def run():
    m=json.loads((ROOT/'manifest.json').read_text());pal={tuple(bytes.fromhex(h[1:])) for h in m['palette']};cache={};errors=[]
    report={'scope':'Memory Sky Theater V02 PNG exports; no Unity project/runtime test','pairCases':0,'pairFailures':0,'nativeTileFiles':0,'materials':[],'errors':errors}
    for p in ROOT.rglob('*.png'):
        a=np.array(Image.open(p).convert('RGBA'));cache[p.relative_to(ROOT).as_posix()]=a
        if set(np.unique(a[:,:,3]))-{0,255}:errors.append([str(p),'non-binary alpha'])
        if np.any(a[:,:,:3][a[:,:,3]==0]):errors.append([str(p),'hidden RGB under alpha0'])
        if {tuple(c) for c in a[:,:,:3][a[:,:,3]>0]}-pal:errors.append([str(p),'off palette'])
    for mat in m['materials']:
        rec={'layer':mat['key'],'outer16':len(mat['outer']),'inner4':len(mat['innerPatches']),'ready47':len(mat['blob47']),'pairFailures':0,'outline2x2Blocks':0}
        files=[e['file'] for key in ['outer','innerPatches','blob47'] for e in mat[key]]
        for f in files:
            a=cache[f];report['nativeTileFiles']+=1
            if a.shape!=(80,80,4):errors.append([f,'wrong native dimension'])
            if '/inner4/' not in f:
                mask=a[:,:,3]>0
                if label(mask)[1]!=1:errors.append([f,'detached opaque components'])
                if np.any(binary_fill_holes(mask)&~mask):errors.append([f,'unwanted enclosed alpha hole'])
                line=np.all(a[:,:,:3]==(26,28,44),axis=2)&mask
                rec['outline2x2Blocks']+=int((line[:-1,:-1]&line[1:,:-1]&line[:-1,1:]&line[1:,1:]).sum())
                p=np.pad(mask,1,constant_values=False);inside=p[:-2,1:-1]&p[2:,1:-1]&p[1:-1,:-2]&p[1:-1,2:]
                if np.any(line&inside):errors.append([f,'dark contour thicker than 1 pixel'])
        ready={(e['outer'],e['inner']):cache[e['file']] for e in mat['blob47']}
        for axis in (0,1):
            other=(int(axis==0),int(axis==1))
            cells=[(x,y) for y in range(-1,2+(axis==1)) for x in range(-1,2+(axis==0)) if (x,y) not in ((0,0),other)]
            for bits in range(1<<len(cells)):
                occ={(0,0),other}|{c for k,c in enumerate(cells) if bits>>k&1};a=ready[topology(occ,0,0)];b=ready[topology(occ,*other)]
                report['pairCases']+=1
                if not np.array_equal(a[:,-1] if axis==0 else a[-1],b[:,0] if axis==0 else b[0]):rec['pairFailures']+=1
        outer={e['mask']:cache[e['file']] for e in mat['outer']}
        for (o,i),expected in ready.items():
            a=outer[o].copy()
            for p in mat['innerPatches']:
                if i&p['bit']:
                    x,y,w,h=p['replaceRectTopLeft'];a[y:y+h,x:x+w]=cache[p['file']][y:y+h,x:x+w]
            if not np.array_equal(a,expected):errors.append([mat['key'],o,i,'inner patch mismatch'])
        report['pairFailures']+=rec['pairFailures'];report['materials'].append(rec)
    if report['pairFailures']:errors.append(['tiles','seam mismatch'])
    report['backgrounds']=[]
    cams=json.loads((ROOT/'camera/camera_positions.json').read_text())
    for bg in m['backgrounds']:
        a=cache[bg['file']]
        if a.shape!=(960,960,4):errors.append([bg['file'],'size'])
        if not np.array_equal(a[:,0],a[:,-1]) or not np.array_equal(a[0],a[-1]):errors.append([bg['file'],'repeat seam'])
        if bg['layer']=='far' and not np.all(a[:,:,3]==255):errors.append([bg['file'],'Far coverage hole'])
        for c in cams['positions']:
            ox=int(np.floor(c['x']*bg['parallax']+.5));oy=int(np.floor(c['y']*bg['parallax']+.5))
            expected=a[(np.arange(560)+oy)%960][:,(np.arange(400)+ox)%960]
            if not np.array_equal(cache[f"camera/{c['name']}_{bg['layer']}.png"],expected):errors.append([bg['file'],c['name'],'camera wrap mismatch'])
        report['backgrounds'].append({'layer':bg['layer'],'size':[960,960],'repeatXY':True})
    for c in cams['positions']:
        if not np.all(cache[f"camera/{c['name']}_composite.png"][:,:,3]==255):errors.append([c['name'],'camera gap'])
    for stem in ['stage','velvet']:
        one=cache[f'inspection/{stem}_join_1x.png'];four=cache[f'inspection/{stem}_join_4x.png']
        if not np.array_equal(four,one.repeat(4,0).repeat(4,1)):errors.append(['inspection',stem,'not exact nearest4x'])
    props={p['name']:p for p in m['props']};report['props']=[]
    for name,p in props.items():
        a=cache[p['file']];mask=a[:,:,3]>0
        if a.shape[:2]!=(p['size'][1],p['size'][0]) or any(v%80 for v in p['size']):errors.append([name,'not an 80px multiple'])
        holes=binary_fill_holes(mask)&~mask;lab,num=label(holes);small=sum(1 for k in range(1,num+1) if np.sum(lab==k)<=3)
        if small:errors.append([name,'tiny alpha hole',small])
        ink=np.all(a[:,:,:3]==(26,28,44),axis=2)&mask
        pad=np.pad(mask,1,constant_values=False);inside=pad[:-2,1:-1]&pad[2:,1:-1]&pad[1:-1,:-2]&pad[1:-1,2:]
        if np.any(ink&inside):errors.append([name,'thick external ink contour'])
        report['props'].append({'name':name,'size':p['size'],'intentionalOpenings':num,'tinyAlphaHoles':small})
    l=cache[props['curtain_wing_left_80x160']['file']];r=cache[props['curtain_wing_right_80x160']['file']];h=cache[props['lintel_repeat_80']['file']]
    if not np.array_equal(l[:80,-1],h[:,0]) or not np.array_equal(h[:,-1],r[:80,0]) or not np.array_equal(h[:,0],h[:,-1]):errors.append(['proscenium','header seam mismatch'])
    report['headerJoins']='checked left, repeat, right'
    report['animationFrames']=0
    for c in m['animations']:
        base=cache[c['frames'][0]['file']]
        for frame in c['frames']:
            a=cache[frame['file']];report['animationFrames']+=1
            if a.shape!=base.shape:errors.append([c['name'],'changing canvas'])
            if 'fixedOutsideRect' in c:
                x,y,w,h=c['fixedOutsideRect'];outside=np.ones(base.shape[:2],dtype=bool);outside[y:y+h,x:x+w]=False
                if not np.array_equal(a[outside],base[outside]):errors.append([c['name'],'moving column or header'])
            else:
                if not np.array_equal(a[:,:,3],base[:,:,3]):errors.append([c['name'],'light alpha changes'])
            mask=a[:,:,3]>0;holes=binary_fill_holes(mask)&~mask;lab,num=label(holes)
            small=sum(1 for k in range(1,num+1) if np.sum(lab==k)<=3)
            if small:errors.append([frame['file'],'animation micro-hole',small])
    layout=json.loads((ROOT/'examples/map_layout.json').read_text());ground=layout['structuralGround'];walk=layout['walkableExample'];overlap=0;unsupported=[]
    for p in layout['propPlacements']:
        a=cache[props[p['asset']]['file']];ys,xs=np.where(a[:,:,3]>0);px,py=p['topLeftImagePixels']
        for x,y in zip(xs,ys):
            cx=(int(x)+px)//80;cy=(int(y)+py)//80
            if 0<=cy<len(walk) and 0<=cx<len(walk[cy]) and walk[cy][cx]=='1':overlap+=1
        bottom=int(ys.max());feet=xs[ys==bottom]
        if not any(0<=(py+bottom)//80<len(ground) and 0<=(px+int(x))//80<len(ground[0]) and ground[(py+bottom)//80][(px+int(x))//80]=='1' for x in feet):unsupported.append(p['asset'])
    report['propPixelsOverWalkableCells']=overlap;report['unsupportedProps']=unsupported
    if overlap:errors.append(['layout','props cover walkable cells',overlap])
    if unsupported:errors.append(['layout','unsupported props',unsupported])
    report['cameraPositions']=3;report['status']='PASS' if not errors else 'FAIL'
    (ROOT/'qa_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2));return report
if __name__=='__main__':
    import sys
    sys.exit(bool(run()['errors']))
