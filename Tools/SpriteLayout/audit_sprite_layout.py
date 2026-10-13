from pathlib import Path
import json
import hashlib
import re
import subprocess

JOURNAL=Path('C:/Users/user/AppData/Local/Temp/animol-sprite-layout')
OUT=Path('Docs/Validation/SpriteLayout')
OUT.mkdir(parents=True,exist_ok=True)
operations=[line.split('\t') for line in (OUT/'move_operations.tsv').read_text().splitlines()]
def destination(path):
    meta=path.endswith('.meta')
    if meta:path=path[:-5]
    for source,target in operations:
        if path==source or path.startswith(source+'/'):path=target+path[len(source):]
    if path.startswith('Assets/ANIMOL/Art/') or path=='Assets/ANIMOL/Art':path='Legacy/PreviousProject_20261013/RetiredEmptyFolders/'+path
    return path+'.meta' if meta else path
tree={}
for entry in (JOURNAL/'head-tree.z').read_bytes().split(b'\0'):
    if not entry:continue
    info,path=entry.split(b'\t',1);mode,kind,blob=info.decode().split();tree[path.decode()]=(mode,blob)
before=json.loads((JOURNAL/'before.json').read_text())
rows=[]
guid_errors=[]
png_errors=[]
font_errors=[]
for source,digest in before.items():
    target=destination(source)
    if target==source:continue
    row=dict(source=source,destination=target,sha256_before=digest)
    if source.endswith('.png'):
        row['bytes_preserved']=Path(target).exists() and hashlib.sha256(Path(target).read_bytes()).hexdigest()==digest
        if not row['bytes_preserved']:png_errors.append(source)
    if '/Fonts/' in source and not source.endswith('.meta'):
        if not Path(target).exists() or hashlib.sha256(Path(target).read_bytes()).hexdigest()!=digest:font_errors.append(source)
    if source.endswith('.meta'):
        row['guid_before']=re.search(r'^guid: ([0-9a-f]{32})$',subprocess.check_output(['git','show',tree[source][1]]).decode(errors='replace'),re.M).group(1) if source in tree else None
        row['guid_after']=re.search(r'^guid: ([0-9a-f]{32})$',Path(target).read_text(errors='replace'),re.M).group(1) if Path(target).exists() else None
        if row['guid_before'] and row['guid_after']!=row['guid_before']:guid_errors.append(source)
    rows.append(row)
map={}
for meta in Path('Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
    if match:map.setdefault(match[1],[]).append(str(meta)[:-5])
duplicates={guid:paths for guid,paths in map.items() if len(paths)>1}
for root in ('Packages','Library/PackageCache'):
    for meta in Path(root).rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(errors='replace'),re.M)
        if match:map.setdefault(match[1],[]).append(str(meta)[:-5])
missing=[]
for file in Path('Assets').rglob('*'):
    if file.suffix not in ('.asset','.prefab','.unity','.anim','.mat'):continue
    for guid in re.findall(r'guid: ([0-9a-f]{32})',file.read_text(errors='replace')):
        if not guid.startswith('0000000000000000') and guid not in map:missing.append(dict(owner=str(file),guid=guid))
bad_names=[str(p) for p in Path('Assets/ANIMOL/Sprites').rglob('*') if p.is_file() and p.suffix in ('.png','.anim') and not re.fullmatch(r'(spr|anim)_[a-z0-9]+(?:_[a-z0-9]+)*\.(png|anim)',p.name)]
versioned=[str(p) for p in Path('Assets/ANIMOL').rglob('*') if re.search(r'V\d{2}|StarwayV04|StarwayPasture',p.name)]
report=dict(png_count=len(list(Path('Assets/ANIMOL/Sprites').rglob('*.png'))),clip_count=len(list(Path('Assets/ANIMOL/Sprites').rglob('*.anim'))),png_byte_errors=png_errors,font_byte_errors=font_errors,guid_errors=guid_errors,duplicates=duplicates,missing_references=missing,bad_names=bad_names,versioned_paths=versioned)
(OUT/'move_manifest.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
assert not any((png_errors,font_errors,guid_errors,duplicates,missing,bad_names,versioned))
