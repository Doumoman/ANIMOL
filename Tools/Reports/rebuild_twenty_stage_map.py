"""Redistribute 20 playable stars per theme on the existing constellation wires."""
import copy
import itertools
import json
import math
from collections import Counter
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Assets/ANIMOL/Data/UI/Notebook/catalog.json"
TEXTURES = ROOT / "Assets/ANIMOL/Art/UI/ConstellationMap/Textures"


def stroke_style(path):
    with Image.open(path) as image:
        pixels = list(image.convert("RGBA").getdata())
    alpha = max(p[3] for p in pixels)
    rgb = Counter(p[:3] for p in pixels if p[3] >= alpha * .95).most_common(1)[0][0]
    return (*rgb, alpha)


def render_link(edge, state, color):
    scale = 3
    size = (edge["width"] * scale, edge["height"] * scale)
    points = [((p["x"] - edge["x"]) * scale, (p["y"] - edge["y"]) * scale) for p in edge["points"]]
    core = Image.new("RGBA", size)
    ImageDraw.Draw(core).line(points, fill=color, width=3 * scale, joint="curve")
    glow = core.filter(ImageFilter.GaussianBlur(4 * scale))
    image = Image.alpha_composite(glow, core).resize((edge["width"], edge["height"]), Image.Resampling.LANCZOS)
    path = TEXTURES / (edge["sprite"] + "_" + state + ".png")
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def main():
    data = json.loads(CATALOG.read_text(encoding="utf-8-sig"))
    old = copy.deepcopy(data)
    old_stages = {s["id"]: s for s in old["stages"]}
    exact_edges = {json.dumps(e["points"], sort_keys=True): e for e in old["edges"]}
    transitions = {(old_stages[e["from"]]["artworkId"], old_stages[e["to"]]["artworkId"]): e
                   for e in old["edges"] if e["kind"] == "transition"}
    colors = {state: stroke_style(TEXTURES / (old["edges"][0]["sprite"] + "_" + state + ".png"))
              for state in ["Future", "Cleared"]}
    stages, edges, generated = [], [], []
    for theme in data["themes"]:
        available = sorted((s for s in old["stages"] if s["themeId"] == theme["id"]), key=lambda s: s["order"])
        artworks = [a for a in data["artworks"] if a["themeId"] == theme["id"]]
        assert len(artworks) == 8 and len(available) >= 20
        theme_stages = []
        for index, art in enumerate(artworks):
            count = 3 if index < 4 else 2
            old_indices = art["stageRouteIndices"]
            if len(old_indices) == count:
                selected = old_indices
            else:
                path = [art["wireVertices"][v] for v in art["wirePath"]]
                lengths = [0.0]
                for a, b in zip(path, path[1:]):
                    lengths.append(lengths[-1] + math.hypot(a["x"] - b["x"], a["y"] - b["y"]))
                candidates = itertools.combinations(old_indices[1:-1], count - 2)
                selected = min(([old_indices[0], *middle, old_indices[-1]] for middle in candidates),
                               key=lambda seq: sum(abs(lengths[p] - lengths[-1] * i / (count - 1)) for i, p in enumerate(seq)))
            art["stageRouteIndices"] = selected
            art["stageIds"] = []
            for vertex in art["wireVertices"]:
                vertex["stageId"] = ""
            for route_index in selected:
                stage = copy.deepcopy(available[len(theme_stages)])
                vertex_index = art["wirePath"][route_index]
                vertex = art["wireVertices"][vertex_index]
                stage.update(artworkId=art["id"], wireVertexIndex=vertex_index, x=vertex["x"], y=vertex["y"])
                art["stageIds"].append(stage["id"]); vertex["stageId"] = stage["id"]
                theme_stages.append(stage)
        assert len(theme_stages) == 20
        stages.extend(theme_stages)
        by_art = {a["id"]: a for a in artworks}
        for index, (a, b) in enumerate(zip(theme_stages, theme_stages[1:])):
            if a["artworkId"] == b["artworkId"]:
                art = by_art[a["artworkId"]]
                start, end = (art["stageRouteIndices"][art["stageIds"].index(s["id"])] for s in [a, b])
                points = [{"x": art["wireVertices"][v]["x"], "y": art["wireVertices"][v]["y"]}
                          for v in art["wirePath"][start:end + 1]]
                previous = exact_edges.get(json.dumps(points, sort_keys=True))
                edge = copy.deepcopy(previous) if previous else {
                    "kind": "constellation", "artworkId": art["id"], "points": points,
                    "sprite": f"Map/StageLinks/{theme['id']}-{index}",
                    "x": math.floor(min(p["x"] for p in points)) - 12,
                    "y": math.floor(min(p["y"] for p in points)) - 12,
                    "width": math.ceil(max(p["x"] for p in points)) - math.floor(min(p["x"] for p in points)) + 24,
                    "height": math.ceil(max(p["y"] for p in points)) - math.floor(min(p["y"] for p in points)) + 24,
                }
                if not previous:
                    for state, color in colors.items(): render_link(edge, state, color)
                    generated.append(edge["sprite"])
            else:
                edge = copy.deepcopy(transitions[a["artworkId"], b["artworkId"]])
            edge.update({"from": a["id"], "to": b["id"]})
            edges.append(edge)
    data["stages"], data["edges"] = stages, edges
    CATALOG.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    print(json.dumps({"themes": len(data["themes"]), "stages": len(stages), "edges": len(edges), "generatedLinkPairs": len(generated)}))


if __name__ == "__main__": main()
