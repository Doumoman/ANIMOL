"""Prepare measured supplementary cutover work; Unity moves the already imported sprites."""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'
LEGACY = 'Legacy/Platformer_20261013/'
MARKER = '// LEGACY: platformer map editor, 2026-10-13'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def strip_wrapper(content):
    opening = b'#if ANIMOL_LEGACY_MAP_EDITOR\n'
    ending = b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n'
    normalized = content.replace(b'\r\n', b'\n')
    assert opening in normalized and normalized.endswith(ending)
    # Preserve the exact original body including its original newline style.
    match = re.search(rb'#if ANIMOL_LEGACY_MAP_EDITOR\r?\n', content)
    tail = re.search(rb'\r?\n#endif // ANIMOL_LEGACY_MAP_EDITOR\r?\n$', content)
    return content[match.end():tail.start()]

def named_sprite(entry):
    source = Path(entry['source'])
    package = source.parts[3]
    original = source.stem
    value = re.sub(r'([a-z0-9])([A-Z])', r'\1_\2', original).lower()
    value = re.sub(r'[^a-z0-9]+', '_', value).strip('_')
    value = re.sub(r'_(?:\d+x\d+|\d+)$', '', value)
    value = re.sub(r'^ui_', '', value)
    value = re.sub(r'^common_', '', value)
    folder = Path(entry['destination']).parent.name
    if package == 'PortraitArtV1':
        folder = 'MainMenu' if value.startswith(('button_', 'capsule_', 'card_')) else 'Common'
        value = 'portrait_' + value
    elif package == 'FiveThemeMenuMaps':
        value = 'theme_menu_' + value
    screen = {'MainMenu': 'main', 'InGame': 'ingame', 'Settings': 'settings', 'Common': 'common',
              'StageSelect': 'stage_select', 'Store': 'store'}[folder]
    states = {'idle', 'pressed', 'focused', 'error', 'selected', 'unselected', 'on', 'off', 'empty', 'preview', 'active', 'blocked', 'reset'}
    if value.endswith('_default'):
        value = value[:-8] + '_idle'
    elif value.split('_')[-1] not in states:
        value += '_idle'
    return 'Assets/ANIMOL/Sprites/UI/' + folder + '/spr_ui_' + screen + '_' + value + '.png'

def main():
    assert not JOURNAL.exists(), 'Inspect the existing supplement journal before restarting'
    JOURNAL.mkdir(parents=True)
    status = git('status', '--porcelain=v1', '-z', '--untracked-files=all').decode('utf-8').split('\0')
    files = [p for prefix in ['Assets', 'ProjectSettings', 'Legacy/Platformer_20261013'] for p in (ROOT / prefix).rglob('*') if p.is_file()]
    snapshot = {'root': str(ROOT), 'base_commit': git('rev-parse', 'HEAD').decode().strip(),
                'status': [s for s in status if s], 'index': git('ls-files', '--stage', '-z').decode().split('\0'),
                'sha256': {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}}
    (JOURNAL / 'before.json').write_text(json.dumps(snapshot, ensure_ascii=False, indent=2), encoding='utf-8')
    original_manifest = json.loads((ROOT / 'Assets/ANIMOL/Sprites/sprite_path_manifest.json').read_text(encoding='utf-8'))
    moves = [{'source': e['destination'], 'destination': named_sprite(e), 'guid': e['guid'], 'original_source': e['source']} for e in original_manifest['sprites']]
    assert len({m['destination'] for m in moves}) == len(moves), 'Canonical names collide'
    assert all(m['source'].startswith('Assets/ANIMOL/') for m in moves)
    guarded = json.loads((ROOT / 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json').read_text(encoding='utf-8'))['guarded']
    for name in guarded:
        path = ROOT / name
        original = path.read_bytes()
        backup = JOURNAL / 'originals' / name
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(original)
        body = strip_wrapper(original)
        archive = ROOT / LEGACY / name
        assert not archive.exists(), 'An existing Legacy original must not be overwritten: ' + name
        archive.parent.mkdir(parents=True, exist_ok=True)
        archive.write_bytes(body)
        archive.with_name(archive.name + '.meta').write_bytes(path.with_name(path.name + '.meta').read_bytes())
        header = MARKER + '\n// Original and inactive generator dependencies: ' + LEGACY + name + '\n#if ANIMOL_LEGACY_MAP_EDITOR\n'
        path.write_bytes(header.encode() + body + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
    named_index = ROOT / 'ProjectSettings/ANIMOLNamedArtReferences.json'
    backup = JOURNAL / 'originals/ProjectSettings/ANIMOLNamedArtReferences.json'
    backup.parent.mkdir(parents=True, exist_ok=True)
    backup.write_bytes(named_index.read_bytes())
    (JOURNAL / 'namedart-head.json').write_bytes(git('show', 'HEAD:ProjectSettings/ANIMOLNamedArtReferences.json'))
    plan = {'sprites': moves, 'guarded': guarded, 'legacy_copies': [LEGACY + p for p in guarded],
            'phase1_exists': (ROOT / 'Assets/ANIMOL/Phase1').is_dir(),
            'p01_scenes': sorted(p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets').rglob('*P01*.unity'))}
    (JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'sprites_to_rename': len(moves), 'legacy_generator_originals': len(guarded),
                      'phase1_exists': plan['phase1_exists'], 'p01_scenes': plan['p01_scenes'], 'baseline_changes': len(snapshot['status'])}))

if __name__ == '__main__':
    main()
