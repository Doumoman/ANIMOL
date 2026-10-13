import pathlib,json,hashlib,re,collections
from PIL import Image
root=pathlib.Path.cwd();out=root/'Docs/Validation/DeviceArt';baseline=json.loads((out/'baseline.json').read_text())
changed=[s for s,h in baseline['files'].items() if not (root/s).exists() or hashlib.sha256((root/s).read_bytes()).hexdigest()!=h]
mapping=json.loads((out/'source_path_map.json').read_text());colors=set();errors=[];edge=[]
for row in mapping:
    p=root/row['target'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256'],p
    assert re.fullmatch(r'[a-z0-9_]+\.png',p.name),p
    image=Image.open(p).convert('RGBA');pixels=list(image.getdata())
    if set(a for r,g,b,a in pixels)-{0,255}:errors.append(str(p))
    colors.update((r,g,b) for r,g,b,a in pixels if a)
    bounds=image.getchannel('A').getbbox()
    if bounds and (bounds[0]==0 or bounds[1]==0 or bounds[2]==image.width or bounds[3]==image.height):edge.append(row['target'])
guids=collections.defaultdict(list);allguids=set()
for base in ['Assets','Packages','Library/PackageCache']:
    for p in (root/base).rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(errors='replace'),re.M)
        if match:
            allguids.add(match[1])
            if base=='Assets':guids[match[1]].append(str(p.relative_to(root)))
duplicates={g:p for g,p in guids.items() if len(p)>1};missing=[]
for p in (root/'Assets/ANIMOL').rglob('*'):
    if p.is_file() and p.suffix in ['.unity','.prefab','.asset','.anim','.mat']:
        for g in set(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(errors='replace'))):
            if g not in allguids and not g.startswith('0000000000000000'):missing.append({'path':str(p.relative_to(root)),'guid':g})
result={'baselineHead':baseline['head'],'existingFilesChanged':changed,'existingArtBytesAndMetaGuidsPreserved':all(p=='Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs' for p in changed),'pngCount':len(mapping),'sourcePngHashesPreserved':True,'opaqueColors':len(colors),'alphaErrors':errors,'opaquePixelsAtFrameEdge':edge,'duplicateAssetGuids':duplicates,'unresolvedSerializedGuids':missing,'clips':len(list((root/'Assets/ANIMOL/Sprites/Obstacles').rglob('*.anim'))),'prefabs':len(list((root/'Assets/ANIMOL/Prefabs/Devices').glob('*.prefab')))}
(out/'audit.json').write_text(json.dumps(result,indent=2))
assert not errors and not duplicates and not missing and result['existingArtBytesAndMetaGuidsPreserved'],result
print({k:v for k,v in result.items() if k not in ['opaquePixelsAtFrameEdge']})
