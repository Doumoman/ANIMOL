from pathlib import Path
import json,re
root=Path.cwd();p=root/'Assets/ANIMOL/Data/twilight_sea_import_manifest.json';m=json.loads(p.read_text());moves=[]
def update(path):
    folder,name=path.rsplit('/',1);new=folder+'/'+re.sub(r'_(n|e|s|w)_(rear|front)(?=_f\d\d\.png|\.anim$)',r'_\2_\1',name)
    if path!=new:
        old='Assets/ANIMOL/'+path;dest='Assets/ANIMOL/'+new
        assert not (root/dest).exists() and old not in json.loads((root/'Docs/Validation/TwilightSea/baseline.json').read_text())['files']
        meta=(root/(old+'.meta')).read_text();guid=re.search(r'^guid: (\w+)',meta,re.M)[1]
        moves.append({'from':old,'to':dest,'guid':guid})
    return new
for row in m['animations']:
    row['clipPath']=update(row['clipPath'])
    for frame in row['frames']:frame['path']=update(frame['path'])
assert moves
p.write_text(json.dumps(m,indent=2),encoding='utf8')
mapping=root/'Docs/Validation/TwilightSea/source_path_map.json';rows=json.loads(mapping.read_text());targets={r['from']:r['to'] for r in moves}
for row in rows:row['target']=targets.get(row['target'],row['target'])
mapping.write_text(json.dumps(rows,indent=2),encoding='utf8')
(root/'Docs/Validation/TwilightSea/layer_name_moves.json').write_text(json.dumps({'moves':moves},indent=2),encoding='utf8')
(root/'Docs/Validation/TwilightSea/layer_name_moves.tsv').write_text('\n'.join('\t'.join((r['from'],r['to'],r['guid'])) for r in moves),encoding='utf8')
print('Prepared GUID-preserving moves:',len(moves))
