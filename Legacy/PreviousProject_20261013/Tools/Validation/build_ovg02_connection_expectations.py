"""Independent V6 source-PNG composition: walls, steps, separated cells."""
import json
from functools import lru_cache
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
V6=ROOT/"Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6"
SRC=ROOT/"Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v2"
OUT=ROOT/"Docs/Validation/OVG02/Expected"
lookup=json.loads((V6/"Data/sprite_lookup.json").read_text(encoding="utf8"))
catalog=json.loads((SRC/"Data/mechanism_catalog.json").read_text(encoding="utf8"))
entries={e["id"]:e for e in catalog["entries"]}
mech=Image.open(SRC/catalog["atlas"]).convert("RGBA")
@lru_cache(None)
def png(path):return Image.open(path).convert("RGBA")
full=lambda c:c in ("terrain","C02","C03","C08","C10")
top=lambda c:full(c) or c in ("C01","C05","C06")
dirs=[(0,1),(1,1),(1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1)]
for style in lookup["styles"]:
 for geometry in (1,2,3):
  cells={}
  for i,n in enumerate((1,2,3,4,5,6,7,8,10)):
   x,y=-17+i%5*4,-2+i//5*4
   cells[(x,y)]="terrain"
   if geometry in (1,2):
    cells[(x,y-1)]=cells[(x+1,y-1)]="terrain"
   if geometry==1:cells[(x,y+1)]="terrain"
   cells[(x+(2 if geometry==3 else 1),y)]=f"C{n:02}"
  image=Image.new("RGBA",(640,256))
  def tile(raw,x,y):
   variant=style["variants"][x%2+2*((-y)%2)]
   r=variant["cells"][lookup["rawToIndex"][raw]]["rect"]
   return png(V6/variant["atlas"]).crop((r["x"],r["y"],r["x"]+32,r["y"]+32))
  for (x,y),kind in cells.items():
   raw=sum(1<<i for i,(dx,dy) in enumerate(dirs) if full(kind) and full(cells.get((x+dx,y+dy))))
   cap=raw
   if top(kind):
    if top(cells.get((x-1,y))):cap|=64
    if top(cells.get((x+1,y))):cap|=4
   patch=full(kind) and not full(cells.get((x,y+1))) and lookup["rawToIndex"][raw]!=lookup["rawToIndex"][cap]
   replace=kind in ("C05","C06","C08","C10")
   art=Image.new("RGBA",(32,32))
   if full(kind):
    body=tile(raw,x,y)
    if patch or replace:body.paste((0,0,0,0),(0,0,32,8))
    art.alpha_composite(body)
   if (kind in ("C01","C05","C06") or patch) and not replace:art.alpha_composite(tile(cap,x,y).crop((0,0,32,8)))
   if kind!="terrain":
    facing="RIGHT" if kind in ("C02","C08") else "UP"
    pose="active" if kind=="C04" else "idle"
    r=entries[f'{style["themeId"]}/{kind}/{facing}/{pose}/f0']["atlasRect"]
    art.alpha_composite(mech.crop((r["x"],r["y"],r["x"]+32,r["y"]+32)))
   image.alpha_composite(art,((x+17)*32,(4-y)*32))
  image.save(OUT/f'{style["styleId"]}-connection{geometry}.png')
print("60 independent V6 wall/step/separation PNG expectations created")
