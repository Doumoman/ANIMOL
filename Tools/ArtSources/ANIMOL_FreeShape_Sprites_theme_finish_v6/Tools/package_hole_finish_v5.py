"""Build a complete, verified ZIP atomically; never expose a partial archive."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
args = parser.parse_args()
output = Path(args.output).resolve()
manifest_path = root / 'PACKAGE_MANIFEST.json'
lookup = json.loads((root / 'Data/sprite_lookup.json').read_text())
assert lookup['artVersion'] == 5
paths = sorted(p for p in root.rglob('*') if p.is_file()
               and p != manifest_path and 'node_modules' not in p.parts
               and not any(part.startswith('.') for part in p.relative_to(root).parts))
cells = sorted({c['file'] for s in lookup['styles'] for v in s['variants'] for c in v['cells']})
digest = hashlib.sha256()
for filename in cells:
    digest.update((filename + '\n').encode())
    digest.update((root / filename).read_bytes())
manifest = {
    'package': root.name, 'assetStatus': 'revised-output-for-review',
    'artVersion': 5, 'contractId': lookup['contractId'], 'schemaVersion': 1,
    'changedStyles': ['T01_A'], 'preservedStylesCount': 19,
    'unityIntegration': 'NOT RUN',
    'pixelQualityVerdict': 'Declared architectural role planes, small-hole regressions and collar ports PASS; universal semantic jaggies approval not implied.',
    'cellPngDigestMethod': 'SHA256(sorted relative cell path + LF + PNG bytes)',
    'cellPngDigestSha256': digest.hexdigest(),
    'files': [{'path': p.relative_to(root).as_posix(), 'bytes': p.stat().st_size,
               'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in paths],
}
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
temporary = output.with_suffix(output.suffix + '.building')
output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for p in sorted(paths + [manifest_path]):
        archive.write(p, root.name + '/' + p.relative_to(root).as_posix())
with zipfile.ZipFile(temporary) as archive:
    assert archive.testzip() is None
    assert len(archive.infolist()) == len(manifest['files']) + 1
    for item in manifest['files']:
        content = archive.read(root.name + '/' + item['path'])
        assert len(content) == item['bytes']
        assert hashlib.sha256(content).hexdigest() == item['sha256']
os.replace(temporary, output)
print(json.dumps({'result': 'PASS', 'archive': str(output),
                  'files': len(paths) + 1, 'bytes': output.stat().st_size,
                  'sha256': hashlib.sha256(output.read_bytes()).hexdigest()}))
