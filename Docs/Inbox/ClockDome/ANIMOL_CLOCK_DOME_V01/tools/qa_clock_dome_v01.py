"""Read-only checks of the delivered pixels; no Unity runtime claim."""
from pathlib import Path
from PIL import Image
from scipy.ndimage import label,binary_fill_holes
import numpy as np,json
ROOT=Path('output/ANIMOL_CLOCK_DOME_V01')
CORNERS=[(1,9,-1,-1),(2,3,1,-1),(4,6,1,1),(8,12,-1,1)]
def topology(occ,x,y):
    o=sum(b for dx,dy,b in [(0,-1,1),(1,0,2),(0,1,4),(-1,0,8)] if (x+dx,y+dy) not in occ)
    i=sum(b for b,s,dx,dy in CORNERS if not o&s and (x+dx,y+dy) not in occ)
    return o,i
def run():
    m=json.loads((ROOT/'manifest.json').read_text());pal={tuple(bytes.fromhex(h[1:])) for h in m['palette']};cache={};errors=[]
    report={'scope':'Exported clock-dome PNGs; Unity scene and device logic not connected','pairCases':0,'pairFailures':0,'nativeTileFiles':0,'materials':[],'errors':errors}
    for p in ROOT.rglob('*.png'):
        a=np.array(Image.open(p).convert('RGBA'));cache[p.relative_to(ROOT).as_posix()]=a
        if set(np.unique(a[:,:,3]))-{0,255}:errors.append([str(p),'non-binary alpha'])
        if np.any(a[:,:,:3][a[:,:,3]==0]):errors.append([str(p),'hidden RGB'])
        if {tuple(c) for c in a[:,:,:3][a[:,:,3]>0]}-pal:errors.append([str(p),'off palette'])
    report['pngFiles']=len(cache)
    for mat in m['materials']:
        rec={'layer':mat['key'],'outer16':len(mat['outer']),'inner4':len(mat['innerPatches']),'ready47':len(mat['blob47']),'pairFailures':0,'outline2x2Blocks':0}
        for f in [e['file'] for k in ['outer','innerPatches','blob47'] for e in mat[k]]:
            a=cache[f];report['nativeTileFiles']+=1
            if a.shape!=(80,80,4):errors.append([f,'native dimension'])
            if '/inner4/' not in f:
                mask=a[:,:,3]>0
                if label(mask)[1]!=1:errors.append([f,'detached components'])
                if np.any(binary_fill_holes(mask)&~mask):errors.append([f,'unexpected hole'])
                ink=np.all(a[:,:,:3]==(26,28,44),axis=2)&mask
                rec['outline2x2Blocks']+=int((ink[:-1,:-1]&ink[1:,:-1]&ink[:-1,1:]&ink[1:,1:]).sum())
                pad=np.pad(mask,1);inside=pad[:-2,1:-1]&pad[2:,1:-1]&pad[1:-1,:-2]&pad[1:-1,2:]
                if np.any(ink&inside):errors.append([f,'contour exceeds native1px'])
        ready={(e['outer'],e['inner']):cache[e['file']] for e in mat['blob47']}
        for axis in (0,1):
            other=(int(axis==0),int(axis==1))
            cells=[(x,y) for y in range(-1,2+(axis==1)) for x in range(-1,2+(axis==0)) if (x,y) not in ((0,0),other)]
            for bits in range(1<<len(cells)):
                occ={(0,0),other}|{c for k,c in enumerate(cells) if bits>>k&1};a=ready[topology(occ,0,0)];b=ready[topology(occ,*other)];report['pairCases']+=1
                if not np.array_equal(a[:,-1] if axis==0 else a[-1],b[:,0] if axis==0 else b[0]):rec['pairFailures']+=1
        outer={e['mask']:cache[e['file']] for e in mat['outer']}
        for (o,i),expected in ready.items():
            a=outer[o].copy()
            for p in mat['innerPatches']:
                if i&p['bit']:
                    x,y,w,h=p['replaceRectTopLeft'];a[y:y+h,x:x+w]=cache[p['file']][y:y+h,x:x+w]
            if not np.array_equal(a,expected):errors.append([mat['key'],o,i,'inner patch mismatch'])
        report['pairFailures']+=rec['pairFailures'];report['materials'].append(rec)
    if report['pairFailures']:errors.append(['floor','seams'])
    cams=json.loads((ROOT/'camera/camera_positions.json').read_text());report['backgrounds']=[]
    for bg in m['backgrounds']:
        a=cache[bg['file']]
        if a.shape!=(960,960,4):errors.append([bg['file'],'size'])
        if not np.array_equal(a[:,0],a[:,-1]) or not np.array_equal(a[0],a[-1]):errors.append([bg['file'],'repeat seam'])
        if bg['layer']=='far' and not np.all(a[:,:,3]==255):errors.append([bg['file'],'coverage gap'])
        for c in cams['positions']:
            ox=int(np.floor(c['x']*bg['parallax']+.5));oy=int(np.floor(c['y']*bg['parallax']+.5));expected=a[(np.arange(560)+oy)%960][:,(np.arange(400)+ox)%960]
            if not np.array_equal(cache[f"camera/{c['name']}_{bg['layer']}.png"],expected):errors.append([bg['file'],c['name'],'camera wrap'])
        report['backgrounds'].append({'layer':bg['layer'],'repeatXY':True,'size':[960,960]})
    for c in cams['positions']:
        if not np.all(cache[f"camera/{c['name']}_composite.png"][:,:,3]==255):errors.append([c['name'],'camera gap'])
    one=cache['inspection/floor_join_1x.png'];four=cache['inspection/floor_join_4x.png']
    if not np.array_equal(four,one.repeat(4,0).repeat(4,1)):errors.append(['inspection','4x interpolation'])
    parts={p['name']:p for p in m['parts']};report['parts']=[]
    for name,p in parts.items():
        a=cache[p['file']];mask=a[:,:,3]>0
        if a.shape[:2]!=(p['size'][1],p['size'][0]) or any(v%80 for v in p['size']):errors.append([name,'80px multiple'])
        lab,n=label(binary_fill_holes(mask)&~mask);sizes=np.bincount(lab.ravel())[1:];small=int(np.sum(sizes<=4))
        if small:errors.append([name,'tiny alpha hole',small])
        # Internal dark faces use #333C57, reserving #1A1C2C for native contours.
        ink=np.all(a[:,:,:3]==(26,28,44),axis=2)&mask;pad=np.pad(mask,1);inside=pad[:-2,1:-1]&pad[2:,1:-1]&pad[1:-1,:-2]&pad[1:-1,2:]
        if np.any(ink&inside):errors.append([name,'interior ink contour'])
        report['parts'].append({'name':name,'size':p['size'],'alphaComponents':label(mask)[1],'intentionalNegativeSpacePixels':sizes.tolist(),'microHoles':small})
    get=lambda n:cache[parts[n]['file']]
    cap=get('dome_cap_960x400')
    assembled=np.concatenate([get('dome_cap_segment_'+n+'_320x400') for n in ['left','center','right']],axis=1)
    if not np.array_equal(cap,assembled):errors.append(['dome','slices do not reassemble'])
    for side,x in [('left',0),('right',800)]:
        col=get('column_'+side+'_repeat_160x80');foot=get('column_'+side+'_foot_160x80')
        if not np.array_equal(cap[-1,x:x+160],col[0]) or not np.array_equal(col[0],col[-1]) or not np.array_equal(col[-1],foot[0]):errors.append(['architecture',side,'column join'])
    report['architectureJoins']='cap -> column -> column -> foot; fixed cap slices'
    report['animationFrames']=0
    for clip in m['animations']:
        base=cache[clip['frames'][0]['file']]
        for f in clip['frames']:
            a=cache[f['file']];report['animationFrames']+=1
            if a.shape!=base.shape:errors.append([clip['name'],'canvas drift'])
            if not np.array_equal(base[:clip['fixedTopRows']],a[:clip['fixedTopRows']]):errors.append([clip['name'],'anchor moved'])
            if np.any(a[:,0,3]) or np.any(a[:,-1,3]):errors.append([clip['name'],'clipped animation'])
    layout=json.loads((ROOT/'examples/map_layout.json').read_text());ground=layout['structuralGround'];walk=layout['walkableExample'];overlap=0;unsupported=[]
    for p in layout['propPlacements']:
        a=get(p['asset']);ys,xs=np.where(a[:,:,3]>0);px,py=p['topLeftBoardPixels']
        for x,y in zip(xs,ys):
            cx=(int(x)+px)//80;cy=(int(y)+py)//80
            if 0<=cy<len(walk) and 0<=cx<len(walk[cy]) and walk[cy][cx]=='1':overlap+=1
        feet=xs[ys==ys.max()];gy=(py+int(ys.max()))//80
        if not any(0<=gy<len(ground) and 0<=(px+int(x))//80<len(ground[0]) and ground[gy][(px+int(x))//80]=='1' for x in feet):unsupported.append(p['asset'])
    report['propPixelsOverWalkableCells']=overlap;report['unsupportedProps']=unsupported
    if overlap:errors.append(['layout','props over playable cells',overlap])
    if unsupported:errors.append(['layout','unsupported props',unsupported])
    report['cameraPositions']=3;report['status']='PASS' if not errors else 'FAIL'
    (ROOT/'qa_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2));return report
if __name__=='__main__':
    import sys
    sys.exit(bool(run()['errors']))
