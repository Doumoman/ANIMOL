"""Independent sprite-first filtering comparison. CPU model, never actual Unity/GPU."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import hashlib
import json

OUT = Path(__file__).resolve().parent
WORK = OUT.parent
FAMILIES = {'networks_a': ['velvet_aisle_barrier', 'carved_stage_balustrade', 'prop_shelf_run'],
            'networks_b': ['velvet_curtain_screen', 'brass_footlight_strip', 'ivy_planter_strip']}
FIXTURES = {'straight_H7': ['XXXXXXX'], 'straight_V7': ['X'] * 7,
            'ring_3x3': ['XXX', 'X.X', 'XXX']}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def sample(im, size, filt):
    if filt == 'TWO_TAP_BILINEAR_CPU_MODEL':
        return im.transform(size, Image.Transform.AFFINE, (im.width / size[0], 0, 0, 0, im.height / size[1], 0), Image.Resampling.BILINEAR)
    return im.resize(size, Image.Resampling.LANCZOS)
def mask(cells, x, y):
    return sum(bit for dx, dy, bit in [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)] if (x + dx, y + dy) in cells)

def render(root, family, rows, px, filt):
    cells = {(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == 'X'}
    w, h = max(map(len, rows)) * 256 + 64, len(rows) * 256 + 128
    native = Image.new('RGBA', (w, h))
    individual = Image.new('RGBA', (round(w * px / 256), round(h * px / 256)))
    pins = {}
    for x, y in sorted(cells, key=lambda p: (p[1], p[0])):
        file = f'decor/connected/{family}/n{mask(cells, x, y):02d}.png'
        p = root / file
        im = Image.open(p).convert('RGBA'); pins[file] = sha(p)
        native.alpha_composite(im, (x * 256, y * 256))
        spr = sample(im, (round(im.width * px / 256), round(im.height * px / 256)), filt)
        individual.alpha_composite(spr, (x * px, y * px))
    whole = sample(native, individual.size, filt)
    low, high = whole.filter(ImageFilter.MinFilter(5)), whole.filter(ImageFilter.MaxFilter(5))
    flat_pixels = flat_bad = flat_max = opaque_pixels = opaque_loss_over1 = max_alpha_loss = 0
    for a, b, mn, mx in zip(whole.getdata(), individual.getdata(), low.getdata(), high.getdata()):
        if a[3] == 255:
            opaque_pixels += 1; loss = max(0, a[3] - b[3]); max_alpha_loss = max(max_alpha_loss, loss)
            if loss > 1: opaque_loss_over1 += 1
        if mn == mx and mn[3] == 255:
            flat_pixels += 1; diff = max(abs(a[i] - b[i]) for i in range(4)); flat_max = max(flat_max, diff)
            if diff > 1: flat_bad += 1
    passed = flat_bad == 0 and (filt != 'TWO_TAP_BILINEAR_CPU_MODEL' or opaque_loss_over1 == 0)
    return {'status': 'PASS_OFFLINE' if passed else 'FAIL_OFFLINE', 'flatOpaque5x5Pixels': flat_pixels,
            'flatOpaque5x5BadPixels': flat_bad, 'flatOpaque5x5MaxRGBA': flat_max,
            'referenceOpaquePixels': opaque_pixels, 'opaqueReferenceAlphaLossOver1Pixels': opaque_loss_over1,
            'maxAlphaLossIncludingAntialiasOvershoot': max_alpha_loss, 'sourcePNGSha256': pins}, whole, individual

checks, panels, manifest_pins = [], [], {}
font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 18)
for owner, families in FAMILIES.items():
    root = WORK / (owner + '_exports')
    manifest = root / 'manifest.json'
    if not manifest.exists():
        candidates = list(root.glob('*manifest.json'))
        if len(candidates) == 1: manifest = candidates[0]
    if not manifest.exists(): raise SystemExit('Producer not ready: ' + owner)
    manifest_pins[owner] = {'file': manifest.name, 'sha256': sha(manifest)}
    for family in families:
        for fixture, rows in FIXTURES.items():
            for filt in ['TWO_TAP_BILINEAR_CPU_MODEL', 'LANCZOS']:
                for px in [64, 100, 256]:
                    result, whole, individual = render(root, family, rows, px, filt)
                    result.update(family=family, fixture=fixture, filter=filt, cellPixels=px); checks.append(result)
                    if px == 64 and filt == 'LANCZOS':
                        cw = max(whole.width + 32, 500)
                        page = Image.new('RGBA', (cw + individual.width + 24, max(whole.height, 90) + 75), (20, 13, 41, 255))
                        d = ImageDraw.Draw(page);d.text((12, 6), family + ' / ' + fixture, fill='#f4dfb1', font=font)
                        d.text((12, 31), 'WHOLE FIELD FILTERED', fill='#81c8bd', font=font)
                        d.text((cw, 31), 'SPRITES FILTERED FIRST', fill='#81c8bd', font=font)
                        page.alpha_composite(whole, (12, 64)); page.alpha_composite(individual, (cw, 64));panels.append(page.convert('RGB'))
for owner, pin in manifest_pins.items():
    assert sha(WORK / (owner + '_exports') / pin['file']) == pin['sha256'], 'Producer changed during audit.'
failed = [c for c in checks if c['status'] != 'PASS_OFFLINE']
report = {'status': 'PASS_OFFLINE' if not failed else 'FAIL_OFFLINE', 'fixtureCount': 18, 'checkCount': len(checks),
          'manifestPins': manifest_pins, 'checks': checks,
          'note': 'Native assembled field filtered once versus each PNG independently filtered before placing at world-spaced cells. TWO_TAP_BILINEAR_CPU_MODEL is affine 2x2 sampling, not actual GPU capture. Flat eroded5x5 opaque plateaus must retain RGBA within1; raw antialias/resize filter overshoot differences remain reported. Actual imported texture/atlas/GPU handling NOT_RUN.',
          'actualUnityImport': 'NOT_RUN', 'actualGPUFilter': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN'}
(OUT / 'independent_scaled_connections.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
sheet = Image.new('RGB', (max(p.width for p in panels), sum(p.height + 12 for p in panels)), (15, 9, 30))
y = 0
for p in panels: sheet.paste(p, (0, y)); y += p.height + 12
sheet.save(OUT / 'independent_scaled_connections.png')
print(json.dumps({'status': report['status'], 'checkCount': len(checks), 'failures': [{k: c[k] for k in ['family', 'fixture', 'filter', 'cellPixels', 'flatOpaque5x5BadPixels', 'opaqueReferenceAlphaLossOver1Pixels', 'maxAlphaLossIncludingAntialiasOvershoot']} for c in failed]}))
