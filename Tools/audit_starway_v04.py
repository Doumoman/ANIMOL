"""Read-only V04 source/GUID/reference audit; run after Unity generation."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path('Assets/ANIMOL/Art/StarwayPasture/V04')
OUT = Path('Docs/Validation/StarwayV04')
OUT.mkdir(parents=True, exist_ok=True)
guid_paths = {}
for meta in Path('Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(errors='replace'), re.M)
    if match:
        guid_paths.setdefault(match[1], []).append(str(meta)[:-5].replace('\\', '/'))
duplicates = {guid: paths for guid, paths in guid_paths.items() if len(paths) > 1}
for package_root in (Path('Packages'), Path('Library/PackageCache')):
    for meta in package_root.rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(errors='replace'), re.M)
        if match:
            guid_paths.setdefault(match[1], []).append(str(meta)[:-5].replace('\\', '/'))
missing = []
references = 0
for file in ROOT.rglob('*'):
    if file.suffix not in {'.unity', '.prefab', '.asset', '.anim', '.mat'}:
        continue
    for guid in re.findall(r'guid: ([0-9a-f]{32})', file.read_text(errors='replace')):
        references += 1
        if not guid.startswith('0000000000000000') and guid not in guid_paths:
            missing.append({'owner': str(file), 'guid': guid})
hashes = json.loads((OUT / 'source_hashes.json').read_text())
unchanged = all(hashlib.sha256(Path(row['path']).read_bytes()).hexdigest() == row['source_sha256'] for row in hashes)
baseline = Path('C:/Users/user/AppData/Local/Temp/animol-v04-journal/before.json')
changed = []
if baseline.exists():
    for path, digest in json.loads(baseline.read_text()).items():
        file = Path(path)
        if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest() != digest:
            changed.append(path)
report = dict(png_count=len(hashes), original_png_bytes_preserved=unchanged,
              duplicate_guids=duplicates, serialized_guid_references=references,
              missing_guid_references=missing, changed_preexisting_files=changed)
(OUT / 'reference_audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
assert unchanged and not duplicates and not missing
