from pathlib import Path
import hashlib
import json
import re

ARCHIVE = Path('Legacy/PreviousProject_20261013')
JOURNAL = Path('C:/Users/user/AppData/Local/Temp/animol-active-slice')
OUT = Path('Docs/Validation/ActiveSlice')
before = json.loads((JOURNAL/'before.json').read_text())
roots = (OUT/'moved_roots.txt').read_text().splitlines()
fonts = dict(line.split('\t') for line in (OUT/'font_moves.tsv').read_text().splitlines())
def archive_target(path):
    if path in fonts or (path.endswith('.meta') and path[:-5] in fonts):
        return ARCHIVE/'SharedDependenciesSnapshot'/path
    if any(path == root or path.startswith(root+'/') for root in roots):
        return ARCHIVE/path
    return None
rows = []
errors = []
for source, digest in before.items():
    target = archive_target(source)
    if target:
        actual = hashlib.sha256(target.read_bytes()).hexdigest() if target.exists() else None
        rows.append(dict(source=source,archive=str(target).replace('\\','/'),sha256=digest,preserved=actual==digest))
        if actual != digest: errors.append(source)
guid_paths = {}
for meta in Path('Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
    if match:guid_paths.setdefault(match[1],[]).append(str(meta)[:-5])
duplicates = {guid: paths for guid,paths in guid_paths.items() if len(paths)>1}
for base in ('Packages','Library/PackageCache'):
    for meta in Path(base).rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
        if match:guid_paths.setdefault(match[1],[]).append(str(meta)[:-5])
missing=[]
references=0
for file in Path('Assets').rglob('*'):
    if file.suffix not in ('.asset','.unity','.prefab','.anim','.mat','.controller'):continue
    for guid in re.findall(r'guid: ([0-9a-f]{32})',file.read_text(errors='replace')):
        references+=1
        if not guid.startswith('0000000000000000') and guid not in guid_paths:missing.append(dict(owner=str(file),guid=guid))
active_scenes=[str(p).replace('\\','/') for p in Path('Assets').rglob('*.unity')]
active_code=[str(p).replace('\\','/') for p in Path('Assets').rglob('*.cs')]
unexpected=[p for p in active_code if not p.startswith('Assets/ANIMOL/StarwayV04/')]
report=dict(archive_files=len(rows),archive_hash_errors=errors,font_assets_moved=len(fonts),duplicate_guids=duplicates,missing_references=missing,references=references,active_scenes=active_scenes,active_code=active_code,unexpected_old_code=unexpected)
(OUT/'archive_inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
assert not errors and not duplicates and not missing and not unexpected
