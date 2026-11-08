"""Read-only comparison against the pre-application backup; never writes Unity YAML."""
import collections
import hashlib
import json
import pathlib
import re
import sys

root = pathlib.Path.cwd()
backup = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path(
    (root / 'Temp/maphud-backup-path.txt').read_text(encoding='utf-8-sig').strip())

def blocks(path):
    return {i: (int(t), s) for t, i, s in re.findall(
        r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=\n--- |\Z)', path.read_text(encoding='utf-8-sig'), re.S)}

def fields(text):
    return {k: re.sub(r'(?<=[: ,])-0(?=[},\s])', '0', v.strip()) for k, v in re.findall(
        r'^  ([\w]+):(.*?)(?=^  [\w]+:|\Z)', text, re.M | re.S)}

def ids(text):
    return re.findall(r'fileID: (-?\d+)', text)

reports = []
for asset, is_map in [('Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab', True),
                      ('Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab', False)]:
    old, new = blocks(backup / asset), blocks(root / asset)
    parsed = {i: fields(s) for i, (_, s) in old.items()}
    owner = next(f for f in parsed.values() if ('stageScreen' if is_map else 'costNumber') in f)
    transforms = {ids(f['m_GameObject'])[0]: i for i, f in parsed.items() if old[i][0] == 224}
    def beneath(go, ancestor):
        if go == ancestor:
            return True
        transform = transforms.get(go)
        parent = ids(parsed[transform]['m_Father'])[0] if transform else '0'
        return parent != '0' and beneath(ids(parsed[parent]['m_GameObject'])[0], ancestor)
    map_go = ids(owner['stageScreen'])[0] if is_map else None
    protected = [ids(owner[key])[0] for key in (
        ['mainScreen', 'settingsScreen', 'modal'] if is_map else ['pauseOverlay', 'clearOverlay', 'modalBackdrop'])]
    changes = collections.Counter()
    original_rectangles = 0
    for i, (kind, _) in old.items():
        assert i in new, ('Original object removed', asset, i)
        before, after = parsed[i], fields(new[i][1])
        changed = {k for k in before.keys() | after.keys() if before.get(k) != after.get(k)}
        if kind == 224:
            original_rectangles += 1
        if not changed:
            continue
        go = i if kind == 1 else ids(before['m_GameObject'])[0]
        assert not any(beneath(go, p) for p in protected), ('Protected screen changed', asset, i, changed)
        if is_map and not (kind == 1 and go == ids(owner['m_GameObject'])[0]):
            assert beneath(go, map_go), ('Map change escaped stageScreen', i, changed)
        if kind == 1:
            assert changed == {'m_Component'}
            old_ids, new_ids = ids(before['m_Component']), ids(after['m_Component'])
            assert [x for x in new_ids if x in old_ids] == old_ids
            assert all(x in new and x not in old for x in new_ids if x not in old_ids)
        elif kind == 224:
            assert changed == {'m_Children'}, ('Original geometry changed', i, changed)
            old_ids, new_ids = ids(before['m_Children']), ids(after['m_Children'])
            assert [x for x in new_ids if x in old_ids] == old_ids
            assert len(new_ids) == len(old_ids) + 1
        else:
            allowed = [{'m_Color'}, {'m_fontAsset', 'm_sharedMaterial'}] if is_map else [{'m_Color'}, {'m_Sprite'}, {'digits'}]
            assert changed in allowed, ('Unintended original property change', asset, i, changed)
            if changed == {'m_Color'}:
                assert before['m_Color'].split('a:')[0] == after['m_Color'].split('a:')[0]
                assert after['m_Color'].endswith('a: 0}')
        changes[str(kind) + ':' + ','.join(sorted(changed))] += 1
    reports.append({'prefab': asset, 'originalRecords': len(old), 'originalRectangles': original_rectangles,
                    'protectedScreensUnchanged': True, 'changes': dict(changes), 'unintendedChanges': 0})

preserved = []
for relative in ['Assets/ANIMOL/Scenes/Main/NotebookUI.unity', 'Assets/ANIMOL/Scenes/Main/NotebookUI.unity.meta',
                 'Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab.meta',
                 'Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab.meta',
                 'Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset', 'Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset.meta',
                 'ProjectSettings/EditorBuildSettings.asset']:
    original, current = (backup / relative).read_bytes(), (root / relative).read_bytes()
    assert original == current, ('Protected file bytes changed', relative)
    preserved.append({'path': relative, 'sha256': hashlib.sha256(current).hexdigest()})
report = {'prefabs': reports, 'unchangedFiles': preserved, 'signedZeroNormalizationIgnored': True}
output = root / 'Docs/Validation/MapHudCrayon/PrefabPreservation.json'
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'originalRectangles': sum(r['originalRectangles'] for r in reports),
                  'protectedFiles': len(preserved), 'unintendedChanges': 0}))
