"""Offline technical reference composites from exact runtime sprites and typed snapshots."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import hashlib,json,math,io,os
W=Path(__file__).resolve().parent;ID='ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01'
P=W/'package_staging'/ID;O=W/'preview_exports';O.mkdir(exist_ok=True)
m=json.loads((P/'manifest.json').read_text());design=json.loads((W/'catalog_design.json').read_text())
assets={a['file']:a for a in m['assets']};groups={a['key']:a['file'] for a in m['groups']}
fonts='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def font(n):return ImageFont.truetype(fonts,n)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save_image(im,path):
    data=io.BytesIO();im.save(data,format='PNG');raw=data.getvalue()
    check=Image.open(io.BytesIO(raw));check.load();assert check.size==im.size
    temporary=path.with_suffix('.pending');temporary.write_bytes(raw);os.replace(temporary,path)
    check=Image.open(path);check.load()
def hsh(seed,layer,height,tag):
    h=2166136261
    for c in tag:h=((h^ord(c))*16777619)&0xffffffff
    h=((h^(seed&0xffffffff))*16777619)&0xffffffff
    h^=((layer&0xffffffff)*0xc2b2ae35)&0xffffffff;h^=((height&0xffffffff)*0x27d4eb2f)&0xffffffff
    h^=h>>16;h=(h*0x7feb352d)&0xffffffff;h^=h>>15;h=(h*0x846ca68b)&0xffffffff
    return (h^(h>>16))&0xffffffff
def selections(s):
    index={(r['x'],r['y'],r['logicalLayer'],r['heightKey']):r for r in s['networkCells']}
    def mask(r,x,y):
        out=0
        for dx,dy,b in [(0,1,1),(1,0,2),(0,-1,4),(-1,0,8)]:
            other=index.get((x+dx,y+dy,r['logicalLayer'],r['heightKey']))
            if other and other['family']==r['family'] and other['connectionGroup']==r['connectionGroup']:out|=b
        return out
    rows=[]
    for r in s['networkCells']:
        k=mask(r,r['x'],r['y']);f=f"decor/connected/{r['family']}/n{k:02d}.png"
        rows.append((r['sortingOrder'],r['x']+.5,r['y']+.5,f,'network'))
        post=f"decor/connected/{r['family']}/periodic_post.png"
        if k in (5,10) and post in assets:
            dx,dy=(1,0) if k==10 else (0,1)
            phase=hsh(s['seed'],r['logicalLayer'],r['heightKey'],r['connectionGroup'])%s['postSpacing']
            coord=r['x'] if k==10 else r['y']
            if (coord-phase)%s['postSpacing']==0 and mask(r,r['x']-dx,r['y']-dy)==k and mask(r,r['x']+dx,r['y']+dy)==k:
                rows.append((r['sortingOrder']+1,r['x']+.5,r['y']+.5,post,'optional-post'))
    for r in s['groupPlacements']:
        f=groups[r['groupKey']];w,h=assets[f]['footprintCells']
        rows.append((r['sortingOrder'],r['anchorX']+w/2,r['anchorY']+h/2,f,'whole-or-point'))
    return sorted(rows,key=lambda r:r[0])
def blit(out,f,x,y,ppu):
    a=assets[f];im=Image.open(P/f).convert('RGBA');scale=ppu/a['ppu']
    size=(round(im.width*scale),round(im.height*scale));im=im.resize(size,Image.Resampling.LANCZOS)
    left=round(x-a['pivot'][0]*size[0]);top=round(y-(1-a['pivot'][1])*size[1])
    out.alpha_composite(im,(left,top))
qa={};guide=json.loads((W/'examples_exports/Examples/inspection_layout_guide.json').read_text())
for key,title,bounds in [('all26','ALL 26 DESIGNS / SPATIAL REFERENCE',[24,40]),('stage','COAST HARBOR / 26 DESIGNS',[18,28])]:
    s=json.loads((W/f'examples_exports/Examples/{key}_snapshot.json').read_text());width,height=bounds
    ppu=60 if key=='all26' else 76;margin=80;top=180;bottom=100
    size=(width*ppu+margin*2,height*ppu+top+bottom)
    for grid in (False,True):
        out=Image.new('RGBA',size,(9,26,34,255));dr=ImageDraw.Draw(out)
        dr.rectangle((margin-8,top-8,margin+width*ppu+8,top+height*ppu+8),fill='#29484e',outline='#a68a53',width=2)
        dr.text((margin,35),'STAR SWELL COAST / CONNECTED DECOR V01',font=font(29),fill='#f6dfac')
        dr.text((margin,80),title,font=font(22),fill='#d3e4f4')
        dr.text((margin,120),'OFFLINE REFERENCE / UNITY IMPORT, COMPILE, PLAY: NOT_RUN',font=font(17),fill='#80cfdf')
        if grid:
            for x in range(width+1):dr.line((margin+x*ppu,top,margin+x*ppu,top+height*ppu),fill='#4b596c',width=1)
            for y in range(height+1):dr.line((margin,top+y*ppu,margin+width*ppu,top+y*ppu),fill='#4b596c',width=1)
        rows=selections(s)
        for order,x,y,f,kind in rows:blit(out,f,margin+x*ppu,top+(height-y)*ppu,ppu)
        dr=ImageDraw.Draw(out)
        dr.text((margin,size[1]-65),'6 connected families + 12 whole multi-cell props + 8 one-cell props',font=font(19),fill='#f4dfb8')
        name=key.upper()+('_BOARD_GRID.png'if grid else'_BOARD.png');save_image(out,O/name)
        qa[name]=dict(sha256=sha(O/name),snapshotSha256=sha(W/f'examples_exports/Examples/{key}_snapshot.json'),pixelSize=list(size),pixelsPerCell=ppu,
            spriteSelections=len(rows),optionalPostSelections=sum(r[4]=='optional-post'for r in rows),actualUnityScreenshot=False)

# Individual full silhouette catalog: exactly26 design panels, mask/helper variants not counted.
cw,ch=440,440;cols=4;rows=7;out=Image.new('RGBA',(cols*cw,rows*ch+170),(9,26,34,255));dr=ImageDraw.Draw(out)
dr.text((35,25),'STAR SWELL COAST / 26 DISTINCT DECORATION DESIGNS',font=font(31),fill='#f6dfac')
dr.text((35,78),'NEW NATIVE ART / FLAT ANGULAR CEL SHADING / OFFLINE REFERENCE',font=font(20),fill='#80cfdf')
items=[]
for a in design['networks']:items.append((a['nameEn'],a['key'],f"decor/connected/{a['key']}/n10.png",'CONNECTED / 16 MASKS'))
for a in design['groups']+design['points']:items.append((a['nameEn'],a['key'],groups[a['key']],f"GROUND {a['footprintCells'][0]} x {a['footprintCells'][1]}"))
for i,(title,key,f,kind)in enumerate(items):
    x=(i%cols)*cw;y=(i//cols)*ch+140;a=assets[f]
    dr=ImageDraw.Draw(out);dr.rectangle((x+12,y+10,x+cw-12,y+ch-10),fill='#142e3a',outline='#38616a',width=2)
    im=Image.open(P/f).convert('RGBA');bb=im.getbbox();crop=im.crop(bb);k=min((cw-66)/crop.width,285/crop.height)
    crop=crop.resize((round(crop.width*k),round(crop.height*k)),Image.Resampling.LANCZOS)
    out.alpha_composite(crop,(x+(cw-crop.width)//2,y+22+(285-crop.height)//2))
    dr=ImageDraw.Draw(out);dr.text((x+25,y+330),title,font=font(19),fill='#f6dfac');dr.text((x+25,y+368),kind,font=font(16),fill='#80cfdf')
save_image(out,O/'CATALOG_26_DESIGNS.png');qa['CATALOG_26_DESIGNS.png']=dict(sha256=sha(O/'CATALOG_26_DESIGNS.png'),semanticDesignPanels=len(items),actualUnityScreenshot=False,displayCropOnly=True)
(O/'preview_qa.json').write_text(json.dumps(dict(status='PASS_OFFLINE_REFERENCE_COMPOSITION',artManifestSha256=sha(P/'manifest.json'),outputs=qa,
    nativeArtRepainted=False,actualUnityImport='NOT_RUN',actualUnityPlay='NOT_RUN'),indent=2)+'\n')
print(json.dumps(qa,indent=2))
