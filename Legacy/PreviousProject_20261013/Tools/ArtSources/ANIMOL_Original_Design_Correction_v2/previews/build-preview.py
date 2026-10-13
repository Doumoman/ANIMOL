#!/usr/bin/env python3
"""Embed original/v1/corrected native PNGs; no art painting or runtime changes."""
from pathlib import Path
import base64, hashlib, json
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
BASE = ROOT/'recovered/batch01/ANIMOL_Art_Revision_Batch01_v1'
LAYERS = ['far','mid','platform','near']
records = []

def encoded(path, expected):
    blob = path.read_bytes()
    with Image.open(path) as image:
        size = image.size
    assert size == expected, (path, size, expected)
    records.append({'path':str(path.relative_to(ROOT)), 'size':list(size),
                    'bytes':len(blob), 'sha256':hashlib.sha256(blob).hexdigest()})
    return 'data:image/png;base64,' + base64.b64encode(blob).decode('ascii')

data = {'backgrounds':{}, 'rabbit':{}, 'campaign':None}
for variant in ['original','previous','corrected']:
    group = {}
    for n in range(1,6):
        theme = f'T0{n}'
        for layer in LAYERS:
            name = f'{theme}-{layer}.png'
            path = ROOT/'background/original'/name if variant=='original' else \
                   BASE/'runtime/main-background'/name if variant=='previous' else \
                   ROOT/'background'/theme/name
            group[name[:-4]] = encoded(path,(352,704))
    data['backgrounds'][variant] = group
for direction in ['Right','Left']:
    data['rabbit']['rabbit'+direction] = encoded(BASE/'runtime/main-background'/f'rabbit-run-{direction.lower()}.png',(512,96))
data['campaign'] = encoded(ROOT/'theme-ui/previews/Campaign_Corrected_352x704.png',(352,704))
data['inputHashes'] = records

motion = (BASE/'previews/background-animation.js').read_text()
(HERE/'background-animation.js').write_text(motion)
output = (HERE/'preview-template.html').read_text()
substitutions = {'/*STYLE*/':(HERE/'preview.css').read_text(),
                 '/*DATA*/':'window.PREVIEW_DATA='+json.dumps(data,ensure_ascii=False,separators=(',',':'))+';',
                 '/*MOTION*/':motion, '/*APP*/':(HERE/'preview-app.js').read_text()}
for marker,content in substitutions.items():
    assert output.count(marker)==1, marker
    output = output.replace(marker,content)
out = HERE/'ANIMOL_Original_Design_Correction_v2_Preview.html'
out.write_text(output)
(HERE/'preview-input-hashes.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'preview':str(out), 'bytes':out.stat().st_size, 'embeddedImages':len(records),
                  'runtimeChanges':'20 background PNGs; SC02 presentation settings only'},ensure_ascii=False))
