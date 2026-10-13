"""Independent source-PNG composition for the V6 adaptation. No runtime art is generated."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
V6 = ROOT / "Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6"
MECH = ROOT / "Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v1"
OUT = ROOT / "Docs/Validation/ObstacleGraphicsV1/Expected"
OUT.mkdir(parents=True, exist_ok=True)
lookup = json.loads((V6 / "Data/sprite_lookup.json").read_text(encoding="utf-8"))
catalog = json.loads((MECH / "Data/mechanism_catalog.json").read_text(encoding="utf-8"))
mechanisms = {e["id"]: e for e in catalog["entries"]}
atlas = Image.open(MECH / catalog["atlas"]).convert("RGBA")
for style in lookup["styles"]:
    image = Image.new("RGBA", (480, 192))
    def tile(raw, x, y):
        phase = (x % 2) + 2 * ((-y) % 2)
        variant = style["variants"][phase]
        entry = variant["cells"][lookup["rawToIndex"][raw]]
        r = entry["rect"]
        return Image.open(V6 / variant["atlas"]).convert("RGBA").crop((r["x"], r["y"], r["x"]+32, r["y"]+32))
    for i in range(10):
        # Pairs straddle negative coordinates, including the former -16 boundary.
        x, y = -17+(i % 5)*3, -2+(i // 5)*3
        left_x, top_y = (x+17)*32, (5-(y+2))*32
        full = i+1 in (2,3,8,10)
        top = i+1 in (1,5,6,9)
        base = tile(4 if full else 0, x, y)
        if top:
            base.paste((0,0,0,0),(0,0,32,8))
            base.alpha_composite(tile(4,x,y).crop((0,0,32,8)))
        image.alpha_composite(base,(left_x,top_y))
        device_x = left_x+32
        if full: image.alpha_composite(tile(64,x+1,y),(device_x,top_y))
        if top: image.alpha_composite(tile(64,x+1,y).crop((0,0,32,8)),(device_x,top_y))
        kind = f"C{i+1:02}"
        facing = "RIGHT" if kind in ("C02","C08") else "UP"
        pose = "active" if kind == "C09" else "idle"
        entry = mechanisms[f'{style["themeId"]}/{kind}/{facing}/{pose}']
        r = entry["atlasRect"]
        image.alpha_composite(atlas.crop((r["x"],r["y"],r["x"]+32,r["y"]+32)),(device_x,top_y))
    image.save(OUT / f'{style["styleId"]}.png')
print("20 V6 source compositions created (200 graphic fixtures; no physics claim).")
