import pathlib,json,hashlib,re,collections
from PIL import Image
root=pathlib.Path.cwd();out=root/'Docs/Validation/LetterObservatory/Applied';before=json.loads((out/'baseline.json').read_text());mapping=json.loads((out/'source_path_map.json').read_text())
m=json.loads((root/'Assets/ANIMOL/Data/letter_observatory_import_manifest.json').read_text())
for row in m['animations']:
    elapsed=0
    for frame in row['frames']:
        assert frame['startMilliseconds']==elapsed
        elapsed+=frame['durationMilliseconds']
    assert elapsed==row['totalMilliseconds']
changed=[s for s,h in before['files'].items() if not (root/s).exists() or hashlib.sha256((root/s).read_bytes()).hexdigest()!=h]
allowed={'Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs'}
colors=set();errors=[];edge=[]
palette={(int(s[:2],16),int(s[2:4],16),int(s[4:],16)) for s in ['1A1C2C','257179','29366F','333C57','38B764','3B5DC9','41A6F6','566C86','5D275D','73EFF7','94B0C2','A7F070','B13E53','EF7D57','F4F4F4','FFCD75']}
for row in mapping:
    p=root/row['target'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256'];assert re.fullmatch(r'[a-z0-9_]+\.png',p.name)
    image=Image.open(p).convert('RGBA');pixels=list(image.get_flattened_data());alpha=image.getchannel('A');bounds=alpha.getbbox()
    if any(a not in (0,255) or (a==0 and (r,g,b)!=(0,0,0)) for r,g,b,a in pixels):errors.append(row['target'])
    colors.update((r,g,b) for r,g,b,a in pixels if a)
    if bounds and (bounds[0]==0 or bounds[1]==0 or bounds[2]==image.width or bounds[3]==image.height) and 'backdrop' not in p.name:edge.append(row['target'])
    if row['source'].split('/')[-1] in ['floor_navy.png','floor_slate.png']:
        assert bounds==(8,8,88,88)
        assert [image.getpixel((8+i,8)) for i in range(80)]==[image.getpixel((8+i,87)) for i in range(80)]
        assert [image.getpixel((8,8+i)) for i in range(80)]==[image.getpixel((87,8+i)) for i in range(80)]
assert colors<=palette and len(colors)==13 and not errors and not edge
guids=collections.defaultdict(list);allguids=set();missing=[]
for base in ['Assets','Packages','Library/PackageCache']:
    for p in (root/base).rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(errors='replace'),re.M)
        if match:
            allguids.add(match[1])
            if base=='Assets':guids[match[1]].append(p.relative_to(root).as_posix())
for p in (root/'Assets/ANIMOL').rglob('*'):
    if p.is_file() and p.suffix in ['.asset','.unity','.prefab','.anim','.mat','.spriteatlas']:
        for g in set(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(errors='replace'))):
            if g not in allguids and not g.startswith('0000000000000000'):missing.append((p.relative_to(root).as_posix(),g))
duplicate={g:p for g,p in guids.items() if len(p)>1}
result={'baselineHead':before['head'],'existingFilesChanged':changed,'existingArtAndSettingsBytesPreserved':set(changed)<=allowed,'existingMetaGuidsPreserved':not any(p.endswith('.meta') for p in changed),'sourceHashesPreserved':True,'pngCount':len(mapping),'paletteColors':len(colors),'binaryAlphaAndTransparentBlack':not errors,'unexpectedCanvasEdgeClipping':edge,'floorSeamsAnd80pxBounds':True,'duplicateAssetGuids':duplicate,'unresolvedSerializedGuids':missing}
(out/'audit.json').write_text(json.dumps(result,indent=2))
assert result['existingArtAndSettingsBytesPreserved'] and not duplicate and not missing,result
print(result)
