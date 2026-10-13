"""Stage only cutover changes while keeping pre-existing worktree changes uncommitted."""
import difflib
import json
import subprocess
from pathlib import Path
from prepare_topdown_cutover import ROOT, JOURNAL, LEGACY, HEADER

def git(*args, data=None, check=True):
    return subprocess.run(['git', *args], cwd=ROOT, input=data, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, check=check)

def main():
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
    assert all(s[:1] in {' ', '?'} for s in before['status']), 'Existing staged work must be preserved separately'
    assert not git('diff', '--cached', '--name-only').stdout.strip(), 'Index must still match baseline'
    tracked = {}
    for entry in git('ls-files', '--stage', '-z').stdout.decode().split('\0'):
        if entry:
            info, path = entry.split('\t', 1)
            mode, blob, stage = info.split()
            assert stage == '0'
            tracked[path] = (mode, blob)
    moves = {p: LEGACY + p for p in plan['archive']}
    moves.update({m['source']: m['destination'] for m in plan['sprites']})
    for source, dest in moves.items():
        for suffix in ['', '.meta']:
            old, new = source + suffix, dest + suffix
            if old in tracked:
                mode, blob = tracked[old]
                git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + new)
                git('update-index', '--force-remove', old)
    def stage_bytes(name, content):
        blob = git('hash-object', '-w', '--stdin', data=content).stdout.decode().strip()
        mode = tracked.get(name, ('100644', None))[0]
        git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + name)
    for name in plan['guarded']:
        if name in tracked:
            original = git('show', ':' + name).stdout
            stage_bytes(name, HEADER.encode() + original + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
    # Apply the worktree-original -> current delta to the original index, not all dirty content.
    failed = []
    for name in plan['path_updates']:
        if name not in tracked:
            continue
        original = (JOURNAL / 'originals' / name).read_text(encoding='utf-8-sig').splitlines(keepends=True)
        current = (ROOT / name).read_text(encoding='utf-8-sig').splitlines(keepends=True)
        patch = ''.join(difflib.unified_diff(original, current, fromfile='a/' + name, tofile='b/' + name, n=3)).encode('utf-8')
        if patch:
            result = git('apply', '--cached', '--ignore-space-change', '-', data=patch, check=False)
            if result.returncode:
                failed.append({'path': name, 'reason': result.stderr.decode(errors='replace')})
    settings = 'ProjectSettings/EditorBuildSettings.asset'
    source = git('show', 'HEAD:' + settings).stdout.decode('utf-8')
    # Preserve baseline scene settings in the archive without staging the user's added entries.
    stage_bytes(LEGACY + settings, source.encode('utf-8'))
    import re
    source = re.sub(r'  - enabled: [01]\n    path: ([^\n]+)\n    guid: [^\n]+\n',
                    lambda m: '' if m[1] in moves or 'ThemeRuntime_' in m[1] or 'MapAuthoringWorkspace' in m[1] else m[0], source)
    stage_bytes(settings, source.encode('utf-8'))
    named_art = 'ProjectSettings/ANIMOLNamedArtReferences.json'
    stage_bytes(LEGACY + named_art, git('show', 'HEAD:' + named_art).stdout)
    stage_bytes(named_art, (JOURNAL / 'namedart-head-transformed.json').read_bytes().replace(b'\r\n', b'\n'))
    # Copy tracked folder metas into the archive; keep their working copies unchanged.
    folder_metas = set()
    for old in moves:
        if not moves[old].startswith(LEGACY):
            continue
        for parent in Path(old).parents:
            name = parent.as_posix() + '.meta'
            if name in tracked and (ROOT / LEGACY / name).exists():
                folder_metas.add(name)
    for name in sorted(folder_metas):
        mode, blob = tracked[name]
        git('update-index', '--add', '--cacheinfo', mode + ',' + blob + ',' + LEGACY + name)
    new_files = ['Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md', 'Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md',
                 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json', 'MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md',
                 'Assets/Editor/AnimolTopdownCutoverBatch.cs', 'Assets/Editor/AnimolTopdownCutoverBatch.cs.meta',
                 'Assets/ANIMOL/SpritePathEditor.meta', 'Assets/ANIMOL/SpritePathEditor/ANIMOL.SpritePathEditor.asmdef',
                 'Assets/ANIMOL/SpritePathEditor/ANIMOL.SpritePathEditor.asmdef.meta',
                 'Assets/ANIMOL/SpritePathEditor/CanonicalSpritePaths.cs', 'Assets/ANIMOL/SpritePathEditor/CanonicalSpritePaths.cs.meta',
                 'Assets/ANIMOL/Sprites.meta', 'Assets/ANIMOL/Sprites/sprite_path_manifest.json',
                 'Assets/ANIMOL/Sprites/sprite_path_manifest.json.meta']
    new_files += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets/ANIMOL/Sprites').rglob('*.meta')
                  if p.relative_to(ROOT).as_posix()[:-5] not in moves.values()]
    new_files += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets/ANIMOL/Sprites').rglob('.gitkeep')]
    if 'Assets/Editor.meta' not in before['sha256']:
        new_files.append('Assets/Editor.meta')
    new_files += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Tools').glob('*topdown*cutover*.py')]
    new_files += ['Tools/guard_platformer_launches.py']
    new_files += ['Tools/preserve_named_art_index.py', 'Tools/archive_platformer_tests.py']
    new_files += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Docs/Validation/TopdownCutover').rglob('*') if p.is_file()]
    new_files += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets/ANIMOL/SpritePathEditor').rglob('*') if p.is_file()]
    for name in sorted(set(new_files)):
        if (ROOT / name).exists():
            git('add', '--', name)
    (JOURNAL / 'staging-failures.json').write_text(json.dumps(failed, indent=2), encoding='utf-8')
    print(json.dumps({'staged_paths': len(git('diff', '--cached', '--name-only').stdout.decode().splitlines()), 'patch_failures': failed}, ensure_ascii=False))
    if failed:
        raise RuntimeError('Do not commit: some cutover-only patches could not be staged safely')

if __name__ == '__main__':
    main()
