"""Portable bytes/data validation for installation revision2; never a Unity execution claim."""
from pathlib import Path
import argparse
import hashlib
import json
import re

PACKAGE='ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
PIN='8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069'
ROOT_FIELDS={'schemaVersion','packageId','sourceManifestSha256','boardId','revision','isWholeBoard','isExampleOnly','originX','originY','originZ','cellSize','seed','postSpacing','networkCells','groupPlacements','allowedDecorCells'}
NET={'x','y','logicalLayer','heightKey','sortingOrder','worldZ','family','connectionGroup'}
GROUP={'placementId','groupKey','anchorX','anchorY','logicalLayer','heightKey','sortingOrder','worldZ'}
ALLOW={'x','y','logicalLayer','heightKey'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8'))

def board_check(d,manifest):
    assert set(d)==ROOT_FIELDS and d['schemaVersion']==1 and d['packageId']==PACKAGE
    assert d['sourceManifestSha256']==PIN and d['isWholeBoard'] is True and d['isExampleOnly'] is True
    assert type(d['revision']) is int and d['revision']>=0
    assert d['cellSize']>0 and d['postSpacing'] in [3,4]
    sizes={g['key']:g['footprintCells'] for g in manifest['groups']}
    families={a['family'] for a in manifest['assets'] if a['kind']=='connected_tile'}
    allowed=set()
    for row in d['allowedDecorCells']:
        assert set(row)==ALLOW and all(type(row[k]) is int for k in ALLOW)
        key=tuple(row[k] for k in ['x','y','logicalLayer','heightKey'])
        assert key not in allowed;allowed.add(key)
    claims=set();ids=set();seen=set()
    for row in d['networkCells']:
        assert set(row)==NET and row['family'] in families and row['connectionGroup']
        key=tuple(row[k] for k in ['x','y','logicalLayer','heightKey'])
        assert key in allowed and key not in claims;claims.add(key);seen.add(row['family'])
    for row in d['groupPlacements']:
        assert set(row)==GROUP and row['groupKey'] in sizes and row['placementId'] not in ids
        ids.add(row['placementId']);seen.add(row['groupKey'])
        w,h=sizes[row['groupKey']]
        for dx in range(w):
            for dy in range(h):
                key=(row['anchorX']+dx,row['anchorY']+dy,row['logicalLayer'],row['heightKey'])
                assert key in allowed and key not in claims;claims.add(key)
    return {'networkCells':len(d['networkCells']),'groupPlacements':len(d['groupPlacements']),
            'claims':len(claims),'allowedCells':len(allowed),'distinctDesigns':len(seen)}

def verify(root):
    root=Path(root).resolve()
    m=read(root/'manifest.json')
    assert sha(root/'manifest.json')==PIN and (root/'manifest_sha256.txt').read_text().strip()==PIN
    rev=read(root/'installation_revision.json')
    assert rev['installationRevision']==2 and rev['artRevision']=='V01' and rev['runtimePngCount']==118
    assert len(m['assets'])==118 and len(m['groups'])==20
    for a in m['assets']:assert sha(root/a['file'])==a['sha256'],a['file']
    source=read(root/'completion_source_checksums.json')
    for category in ['unchangedOriginalCore','newCompletionSources']:
        for row in source[category]:assert sha(root/row['file'])==row['sha256'],row['file']
    assert len(source['unchangedOriginalCore'])==8
    binding=root/'Runtime/TheaterConnectedDecorV01Binding.cs'
    s=binding.read_text()
    assert re.search(r'controller\.Configure\s*\(',s) and re.search(r'controller\.SetLayout\s*\(',s)
    assert 'source.TryCaptureSnapshot' in s and '.Changed +=' in s and '.Changed -=' in s
    assert not re.search(r'\b(?:void|IEnumerator)\s+(?:Update|Start)\s*\(',s)
    for p in (root/'Runtime').glob('*.cs'):assert 'using UnityEditor' not in p.read_text(),p.name
    data={name:board_check(read(root/'Examples'/name),m) for name in ['stage_snapshot.json','all26_snapshot.json']}
    assert data['stage_snapshot.json']['networkCells']==48 and data['stage_snapshot.json']['groupPlacements']==16
    assert data['all26_snapshot.json']['distinctDesigns']==26 and data['all26_snapshot.json']['groupPlacements']==20
    old=read(root/'example_layout.json');stage=read(root/'Examples/stage_snapshot.json')
    assert stage['networkCells']==[{k:r[k] for k in NET} for r in old['networkCells']]
    assert stage['groupPlacements']==[{k:r[k] for k in GROUP} for r in old['groupPlacements']]
    checksums=0
    for line in (root/'package_checksums_sha256.txt').read_text().splitlines():
        digest,rel=line.split('  ',1)
        assert sha(root/rel)==digest,rel
        checksums+=1
    return {'status':'PASS_OFFLINE_BYTES_AND_DATA','installationRevision':2,'runtimePngCount':118,
            'artManifestUnchanged':True,'originalCoreSourceFilesUnchanged':8,
            'newCompletionSourceFiles':len(source['newCompletionSources']),
            'concreteControllerCallSitesPresent':True,'typedExamples':data,'packageChecksumEntries':checksums,
            'actualUnityImport':'NOT_RUN','actualCSharpCompile':'NOT_RUN','actualTestRunner':'NOT_RUN',
            'actualGeneratedPrefabOrScene':'NOT_RUN','actualHostBindings':'NOT_RUN','actualUnityPlay':'NOT_RUN'}

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--package',type=Path,default=Path(__file__).resolve().parents[2]);args=p.parse_args()
    print(json.dumps(verify(args.package),ensure_ascii=False,indent=2))
