"""Stage the supplement delta only; dirty original bodies stay dirty in both source and backup."""
import json
import re
import subprocess
import sys
from pathlib import Path

sys.dont_write_bytecode = True
from supplement_topdown_cutover import strip_wrapper, MARKER

ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'
LEGACY = 'Legacy/Platformer_20261013/'

def git(*args, data=None):
    return subprocess.run(['git', *args], cwd=ROOT, input=data, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout

def main():
    before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    cached = set(git('diff', '--cached', '--name-only').decode().splitlines())
    move_paths = {m[k] + suffix for m in plan['sprites'] for k in ['source', 'destination'] for suffix in ['', '.meta']}
    assert not cached or ('--resume' in sys.argv and cached <= move_paths), 'Preserve unrelated staged changes'
    assert all(s[0] in {' ', '?'} for s in before['status'])
    tracked = {}
    for entry in before['index']:
        if entry:
            info, path = entry.split('\t', 1)
            mode, blob, stage = info.split()
            assert stage == '0'
            tracked[path] = (mode, blob)
    def stage_bytes(name, data):
        blob = git('hash-object', '-w', '--stdin', data=data).decode().strip()
        git('update-index', '--add', '--cacheinfo', tracked.get(name, ('100644', ''))[0] + ',' + blob + ',' + name)
    for move in plan['sprites']:
        for suffix in ['', '.meta']:
            source, destination = move['source'] + suffix, move['destination'] + suffix
            mode, blob = tracked[source]
            git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + destination)
            git('update-index', '--force-remove', source)
    for name in plan['guarded']:
        if name not in tracked:
            continue  # Preserve pre-existing untracked originals and their new recovery copies as untracked.
        body = strip_wrapper(git('show', before['base_commit'] + ':' + name))
        stage_bytes(LEGACY + name, body)
        header = MARKER + '\n// Original and inactive generator dependencies: ' + LEGACY + name + '\n#if ANIMOL_LEGACY_MAP_EDITOR\n'
        stage_bytes(name, header.encode() + body + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
        mode, blob = tracked[name + '.meta']
        git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + LEGACY + name + '.meta')
    name = plan['mixed_legacy_copy']
    head = git('show', before['base_commit'] + ':' + name).decode('utf-8')
    old = '// Archived scene entry: Legacy/Platformer_20261013/Assets/ANIMOL/Scenes/.'
    new = MARKER + '\n            // Former scene launch body: ' + LEGACY + name
    assert head.count(old) == 1, 'User-added launch methods remain outside this commit'
    stage_bytes(name, head.replace(old, new).encode('utf-8'))
    legacy = re.sub(r'#if ANIMOL_LEGACY_MAP_EDITOR([^#]*?)#else[^#]*?#endif', lambda m: m[1], head, flags=re.S)
    legacy = legacy.replace('            ' + old + '\n', '')
    stage_bytes(LEGACY + name, legacy.encode('utf-8'))
    mode, blob = tracked[name + '.meta']
    git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + LEGACY + name + '.meta')
    # The new UI recovery snapshot needs its original folder meta too.
    folder_meta = 'Assets/ANIMOL/Scripts/UI.meta'
    if not (ROOT / LEGACY / folder_meta).exists():
        target = ROOT / LEGACY / folder_meta
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / folder_meta).read_bytes())
    mode, blob = tracked[folder_meta]
    git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + LEGACY + folder_meta)
    stage_bytes('ProjectSettings/ANIMOLNamedArtReferences.json', (JOURNAL / 'namedart-head.json').read_bytes().replace(b'\r\n', b'\n'))
    paths = ['Assets/Editor/AnimolCutoverSupplementBatch.cs', 'Assets/Editor/AnimolCutoverSupplementBatch.cs.meta',
             'Assets/ANIMOL/SpritePathEditor/CanonicalSpritePaths.cs', 'Assets/ANIMOL/SpritePathEditor/Tests/Editor/CanonicalSpritePathTests.cs',
             'Assets/ANIMOL/Sprites/sprite_path_manifest.json', 'Assets/ANIMOL/UI/FiveThemeMenuMaps/Tests/PlayMode/CutoverPortraitUiTests.cs',
             'Assets/ANIMOL/UI/FiveThemeMenuMaps/Tests/PlayMode/CutoverPortraitUiTests.cs.meta', 'Legacy/Platformer_20261013/README.md',
             'Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md', 'Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md', 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json',
             'Docs/ANIMOL_CUTOVER_CLASSIFICATION.json', 'MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md']
    paths += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Screenshots/TopdownCutover_20261013').glob('*') if p.is_file()]
    paths += ['Docs/Validation/TopdownCutover/' + n for n in ['supplement-import-audit.json', 'supplement-reference-audit.json',
              'supplement-editmode.xml', 'supplement-playmode.xml', 'namedart-build-preflight.json', 'mobile-build.json', 'supplement-scene-validation.json']]
    paths += ['Tools/' + n for n in ['supplement_topdown_cutover.py', 'audit_topdown_cutover.py', 'write_cutover_documents.py',
              'restore_cutover_build_side_effect.py', 'preserve_mixed_legacy_calls.py', 'report_cutover_supplement.py', 'stage_cutover_supplement.py']]
    for name in sorted(set(paths)):
        assert (ROOT / name).is_file(), name
        assert not any(s[3:] == name and not name.startswith(('Docs/', 'MCP_INBOX/')) and name != 'Tools/supplement_topdown_cutover.py' for s in before['status']), 'Unexpected existing dirty file: ' + name
        git('add', '--', name)
    changed = git('diff', '--cached', '--name-status', '-M').decode('utf-8').splitlines()
    evidence = 'Docs/Validation/TopdownCutover/supplement-changed-files.json'
    (ROOT / evidence).write_text(json.dumps({'base_commit': before['base_commit'], 'paths': changed}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    git('add', '--', evidence)
    print(json.dumps({'staged_change_records': len(changed), 'existing_dirty_bodies_included': False}))

if __name__ == '__main__':
    main()
