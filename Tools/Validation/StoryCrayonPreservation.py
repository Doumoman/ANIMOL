"""Read-only validation of the story skin against its pre-application backup."""
import collections
import hashlib
import json
import pathlib
import re
import sys

root = pathlib.Path.cwd()
backup = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path(
    (root / 'Temp/story-crayon-backup-path.txt').read_text(encoding='utf-8-sig').strip())
output = root / 'Docs/Validation/StoryCrayon'

def blocks(path):
    return {i: (int(kind), text) for kind, i, text in re.findall(
        r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=\n--- |\Z)', path.read_text(encoding='utf-8-sig'), re.S)}

def fields(text):
    return {k: v.strip() for k, v in re.findall(r'^  ([\w]+):(.*?)(?=^  [\w]+:|\Z)', text, re.M | re.S)}

def ids(text):
    return re.findall(r'fileID: (-?\d+)', text)

def overrides(text):
    return {(reference, path): (value, obj) for reference, path, value, obj in re.findall(
        r'- target: (.*?)\n\s+propertyPath: (.*?)\n\s+value:(.*?)\n\s+objectReference: (.*?)\n', text)}

assets = ['Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab',
          'Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab',
          'Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity']
reports = []
for asset in assets:
    before, after = blocks(backup / asset), blocks(root / asset)
    changes = collections.Counter()
    rectangles = 0
    for identity, (kind, text) in before.items():
        assert identity in after, ('Original record removed', asset, identity)
        a, b = fields(text), fields(after[identity][1])
        changed = {key for key in a.keys() | b.keys() if a.get(key) != b.get(key)}
        if kind == 224:
            rectangles += 1
        if not changed:
            continue
        if kind in (1, 224):
            key = 'm_Component' if kind == 1 else 'm_Children'
            assert changed == {key}, ('Original geometry/object changed', asset, identity, changed)
            old_ids, new_ids = ids(a[key]), ids(b[key])
            assert [i for i in new_ids if i in old_ids] == old_ids
            assert all(i in after and i not in before for i in new_ids if i not in old_ids)
        elif kind == 114:
            assert changed <= {'m_Enabled', 'crayonSkin', 'm_Color', 'm_TextStyleHashCode', 'm_fontColor32'}, (asset, identity, changed)
            if 'm_Enabled' in changed:
                assert a['m_Enabled'] == '1' and b['m_Enabled'] == '0'
            if 'm_Color' in changed:
                assert a['m_Color'].split('a:')[0] == b['m_Color'].split('a:')[0] and b['m_Color'].endswith('a: 0}')
            if 'm_TextStyleHashCode' in changed:
                assert a['m_TextStyleHashCode'] == '0' and b['m_TextStyleHashCode'] == '-1183493901'
            # m_fontColor32 is TMP's derived cache. The actual float color, font,
            # material, text, size, style, and every other serialized field stay equal.
        elif kind == 223:
            assert changed == {'m_AdditionalShaderChannelsFlag'}
            assert a['m_AdditionalShaderChannelsFlag'] == '0' and b['m_AdditionalShaderChannelsFlag'] == '25'
        elif kind == 1001:
            assert changed == {'m_Modification'}
            old_mods, new_mods = overrides(text), overrides(after[identity][1])
            assert all(new_mods.get(key) == value for key, value in old_mods.items()), ('Existing prefab override changed', asset)
            assert all(key[1].strip("'").startswith('externalFrames.Array.') for key in new_mods.keys() - old_mods.keys())
            # Other prefab modification lists must remain untouched.
            for key in ['m_TransformParent', 'm_RemovedComponents', 'm_RemovedGameObjects', 'm_AddedGameObjects', 'm_AddedComponents']:
                old_line = re.search(r'^    ' + key + r':(.*?)(?=^    \w+:|\Z)', text, re.M | re.S)
                new_line = re.search(r'^    ' + key + r':(.*?)(?=^    \w+:|\Z)', after[identity][1], re.M | re.S)
                assert (old_line.group(1) if old_line else '') == (new_line.group(1) if new_line else ''), (asset, key)
        else:
            raise AssertionError((asset, identity, kind, changed))
        changes[str(kind) + ':' + ','.join(sorted(changed))] += 1
    assert (backup / (asset + '.meta')).read_bytes() == (root / (asset + '.meta')).read_bytes()
    reports.append({'path': asset, 'originalRecords': len(before), 'originalRectangles': rectangles,
                    'allowedChanges': dict(changes), 'unintendedChanges': 0, 'metaUnchanged': True})

protected = json.loads((root / 'Temp/story-crayon-protected-hashes.json').read_text(encoding='utf-8'))
changed = [name for name, digest in protected.items() if hashlib.sha256((root / name).read_bytes()).hexdigest() != digest]
assert not changed, ('Protected map/HUD/font/data files changed', changed)
for name in ['Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset', 'Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset.meta',
             'Assets/ANIMOL/Scripts/Packages/WilliamParadoxStoryV14/Runtime/StoryChronicleView.cs']:
    assert (backup / name).read_bytes() == (root / name).read_bytes(), ('Existing font/chronicle changed', name)
result = {'assets': reports, 'protectedFiles': len(protected), 'protectedChanges': changed,
          'existingGalmuri14AndChronicleSourceUnchanged': True}
(output / 'SourcePreservation.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'originalRecords': sum(r['originalRecords'] for r in reports),
                  'originalRectangles': sum(r['originalRectangles'] for r in reports), 'protectedFiles': len(protected), 'unintendedChanges': 0}))
