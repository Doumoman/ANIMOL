"""Summarize real Game View captures and verify protected working files."""
from pathlib import Path
import csv
import hashlib
import json
import subprocess
import tempfile
import imageio_ffmpeg
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/MainUiV7'
CAP = OUT / 'Captures'
summary = {}
for height in [1920,2400]:
    size = f'1080x{height}'
    assert (CAP / f'{size}.complete').exists()
    trace = list(csv.DictReader((CAP / f'{size}_Live_26s.csv').open()))
    assert float(trace[-1]['real'])-float(trace[0]['real']) > 25
    assert {int(r['theme']) for r in trace} == set(range(5))
    assert all(int(r['theme']) == int(float(r['local'])//5)%5 for r in trace)
    assert all(r['rabbitY']=='436' and r['footY']=='526' for r in trace)
    audit = list(csv.DictReader((CAP / f'{size}_rabbit-audit.csv').open()))
    assert len(audit) == 800
    assert all(int(r['changed']) == int(r['upperChanged']) == int(r['viewportClipped']) == 0 for r in audit)
    checks = (CAP / f'{size}_checks.txt').read_text().splitlines()
    assert all(r.startswith('PASS ') for r in checks)
    shots = sorted(CAP.glob(f'Live_*_{size}.png'))
    times = [float(trace[int(p.name.split('_')[1])-2]['real']) for p in shots]
    lines = []
    for i,path in enumerate(shots):
        end = times[i+1] if i+1 < len(times) else 26.0
        lines += [f"file '{path.as_posix()}'", f'duration {end-times[i]:.6f}']
    lines += [f"file '{shots[-1].as_posix()}'"]
    with tempfile.TemporaryDirectory(prefix='animol-v7-') as tmp:
        listing = Path(tmp)/'concat.txt'
        listing.write_text('\n'.join(lines),encoding='utf-8')
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-loglevel','error','-f','concat','-safe','0','-i',str(listing),'-t','26','-r','30','-c:v','libx264','-crf','18','-pix_fmt','yuv420p',str(OUT/f'Unity-live-{size}.mp4')],check=True)
    sheet = Image.new('RGB',(8*192,2*220),(26,28,44))
    draw = ImageDraw.Draw(sheet)
    theme,cam = (3,-1) if height==1920 else (5,1)
    for row,run in enumerate([-1,1]):
        for frame in range(8):
            im=Image.open(CAP/f'Rabbit_T{theme}_cam{cam}_run{run}_f{frame}_{size}.png')
            top=(height-2160)/2+436*1080/352
            crop=im.crop((440,int(top),640,int(top+96*1080/352)))
            crop.thumbnail((192,190),Image.Resampling.NEAREST)
            sheet.paste(crop,(frame*192,row*220+25))
            draw.text((frame*192+5,row*220+5),f'dir {run} frame {frame}',fill='white')
    sheet.save(CAP/f'rabbit-frames-{size}.png')
    if height==2400:
        assert Image.open(CAP/f'Lobby_T01_{size}.png').getpixel((0,0))[:3] == (26,28,44)
    summary[size]=dict(trace_samples=len(trace),real_seconds=float(trace[-1]['real']),local_delta=float(trace[-1]['local'])-float(trace[0]['local']),live_screenshots=len(shots),gpu_cases=len(audit),route_and_visual_assertions=len(checks),near_lost_pixels=0,viewport_lost_pixels=0)

for name in ['protected','preexisting-dirty']:
    before=json.loads((OUT/f'{name}-before.json').read_text(encoding='utf-8'))
    after={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in before if (ROOT/p).exists()}
    assert before==after, name
    if name=='protected':
        paths=set()
        for folder in ['Data/Campaign','Maps','MapBackups','Scenes/Campaign']:
            paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/ANIMOL'/folder).rglob('*') if p.is_file())
        paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/ANIMOL/Scenes').glob('*.unity') if p.stem not in ('Bootstrap','Lobby'))
        assert paths==set(before), (len(paths),len(before))
    (OUT/f'{name}-after.json').write_text(json.dumps(after,indent=2),encoding='utf-8')
    summary[name]=dict(files=len(before),changed=0,missing=0)
(OUT/'verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary,indent=2))
