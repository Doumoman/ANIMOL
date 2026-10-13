#!/usr/bin/env python3
"""Package hygiene/static checks; this does not compile Unity/C# or run device tests."""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


def delimiters(source):
    stack, index = [], 0
    while index < len(source):
        if source.startswith('//', index):
            end = source.find('\n', index)
            index = len(source) if end < 0 else end + 1
            continue
        if source.startswith('/*', index):
            end = source.find('*/', index + 2)
            assert end >= 0, 'Unclosed comment'
            index = end + 2
            continue
        verbatim = source.startswith('@"', index) or source.startswith('$@"', index) or source.startswith('@$"', index)
        if verbatim or source[index] in ('"', "'") or source.startswith('$"', index):
            if verbatim:
                index += 3 if source[index] == '$' or source.startswith('@$', index) else 2
                quote = '"'
            else:
                index += 1 if source[index] == '$' else 0
                quote = source[index]
                index += 1
            closed = False
            while index < len(source):
                if not verbatim and source[index] == '\\':
                    index += 2
                elif source[index] == quote:
                    if verbatim and source.startswith('""', index):
                        index += 2
                        continue
                    index += 1
                    closed = True
                    break
                else:
                    index += 1
            assert closed, 'Unclosed literal'
            continue
        character = source[index]
        if character in '({[':
            stack.append(character)
        elif character in ')}]':
            assert stack and stack.pop() == {')':'(', '}':'{', ']':'['}[character], 'Delimiter mismatch'
        index += 1
    assert not stack, 'Unclosed delimiter'


def main():
    root = Path(__file__).resolve().parent.parent
    # Assets are merged into the Unity project; support files stay in Tools/ANIMOLNet02.
    asset_root = root / 'Assets/ANIMOL/NetUiDev'
    if not asset_root.is_dir():
        asset_root = root.parent.parent / 'Assets/ANIMOL/NetUiDev'
    assert asset_root.is_dir(), 'NET02 Unity source directory missing'
    assemblies = {}
    for path in asset_root.rglob('*.asmdef'):
        data = json.loads(path.read_text(encoding='utf-8'))
        assert data['name'] not in assemblies, 'Duplicate assembly'
        assemblies[data['name']] = data
    runtime = assemblies['ANIMOL.NetUiDev']
    assert 'ANIMOL_NET_UI_DEV' in runtime['defineConstraints']
    assert assemblies['ANIMOL.NetUiDev.Editor']['includePlatforms'] == ['Editor']
    assert not assemblies['ANIMOL.NetUiDev.Editor'].get('defineConstraints'), 'DEV menu must work with symbol off'
    external = {'UnityEngine.TestRunner', 'UnityEditor.TestRunner', 'UnityEngine.UI', 'Unity.TextMeshPro',
                'ANIMOL.Runtime', 'ANIMOL.AnimalUiV2', 'ANIMOL.AnimalMultiplayerPhase3',
                'ANIMOL.MissingUiV1.Project', 'ANIMOL.MissingUiV1.Multiplayer'}
    for assembly in assemblies.values():
        for reference in assembly.get('references', []):
            assert reference in assemblies or reference in external, 'Unknown assembly reference: '+reference
    sources = sorted(asset_root.rglob('*.cs'))
    for path in sources:
        delimiters(path.read_text(encoding='utf-8'))
    for path in root.rglob('*.json'):
        if path.name != 'MANIFEST.json':
            json.loads(path.read_text(encoding='utf-8'))
    ET.parse(asset_root/'Runtime/link.xml')
    assert not list(root.rglob('*.sqlite3')) and not list(root.rglob('*.db')), 'Live database must not ship'
    all_csharp = '\n'.join(p.read_text(encoding='utf-8') for p in sources)
    assert 'OnGUI(' not in all_csharp and 'new GameObject("Canvas' not in all_csharp
    assert 'new EventSystem' not in all_csharp
    launch = (root/'Tools/Cloud/run-server.sh').read_text(encoding='utf-8')
    assert 'Server/server.py' in launch and '--database' in launch and '--bind' in launch
    source_handoff = root/'Docs/ANIMOL_UI_IMPLEMENTATION_AND_SERVER_HANDOFF.md'
    assert '기준일: **2026-10-05**' in source_handoff.read_text(encoding='utf-8')
    fixtures = json.loads((root/'ServerTests/fixture_config.json').read_text(encoding='utf-8'))
    for row in fixtures['Animals']:
        assert row['AnimalId'].startswith('TEST_') and row['StableArtId'].startswith('TEST_')
    empty = json.loads((root/'Server/config.empty.json').read_text(encoding='utf-8'))
    assert empty['Animals'] == [] and empty['Modes'] == [] and empty['Accounts'] == []
    print(json.dumps({'status':'PASS', 'csharp_files':len(sources),'asmdefs':len(assemblies),
        'handoff_sha256':hashlib.sha256(source_handoff.read_bytes()).hexdigest(),
        'note':'Static package checks only; Unity compilation and NUnit/device execution are unverified.'},indent=2))


if __name__ == '__main__':
    main()
