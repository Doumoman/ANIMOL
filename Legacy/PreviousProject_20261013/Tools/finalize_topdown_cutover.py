"""Verify preserved bytes and write the cutover inventory after Unity moves complete."""
import hashlib
import json
import shutil
from pathlib import Path
from prepare_topdown_cutover import ROOT, JOURNAL, LEGACY, HEADER

def main():
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
    assert (JOURNAL / 'executed.txt').exists(), 'Unity moves have not completed'
    errors = []
    moved = {p: LEGACY + p for p in plan['archive']}
    moved.update({m['source']: m['destination'] for m in plan['sprites']})
    for source, target in moved.items():
        for suffix in ['', '.meta']:
            path = ROOT / (target + suffix)
            expected = before['sha256'].get(source + suffix)
            actual = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
            if expected != actual:
                errors.append(source + suffix)
        # Folder metas accompany the archived original relative path too.
        if target.startswith(LEGACY):
            for parent in Path(source).parents:
                meta = ROOT / (parent.as_posix() + '.meta')
                if meta.is_file():
                    dest = ROOT / LEGACY / (parent.as_posix() + '.meta')
                    dest.parent.mkdir(parents=True, exist_ok=True)
                    if not dest.exists():
                        shutil.copyfile(meta, dest)
    # Preserve the original user-owned scene list next to the platformer archive.
    settings = JOURNAL / 'originals/ProjectSettings/EditorBuildSettings.asset'
    dest = ROOT / LEGACY / 'ProjectSettings/EditorBuildSettings.asset'
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(settings, dest)
    untouched_errors = []
    changed = set(plan['guarded']) | set(plan['path_updates']) | set(moved)
    for name, expected in before['sha256'].items():
        base = name[:-5] if name.endswith('.meta') else name
        if base in changed:
            continue
        path = ROOT / name
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            untouched_errors.append(name)
    # Guarded old pipeline content must remain exactly recoverable inside the wrapper.
    guard_errors = []
    for name in plan['guarded']:
        original = (JOURNAL / 'originals' / name).read_bytes()
        if (ROOT / name).read_bytes() != HEADER.encode() + original + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n':
            guard_errors.append(name)
    dangling = []
    archived = set(plan['archive'])
    for source, references in before['references'].items():
        if source in archived or source.endswith('.cs'):
            continue
        for target in references:
            if target in archived:
                dangling.append({'source': source, 'target': target})
    report = {'archive_root': LEGACY, 'moves': [{'source': p, 'destination': t,
              'guid': next((g for g, a in before['guids'].items() if a == p), None)} for p, t in sorted(moved.items())],
              'guarded': plan['guarded'], 'path_updates': plan['path_updates'],
              'manifest_updates': plan.get('manifest_updates', []), 'asmdefs': before['asmdefs'],
              'retained_dependencies': plan['retained_dependencies'], 'p01_scenes': plan['p01_scenes'],
              'topdown_files': plan['topdown_files'], 'phase1': plan['phase1'],
              'initial': {'files': len(before['paths']), 'guids': len(before['guids']),
                          'asmdefs': len(before['asmdefs']), 'existing_changes': len([s for s in before['status'] if not s[3:].startswith('Tools/')]),
                          'duplicate_guids': before['duplicate_guids']},
              'verification': {'moved_bytes_and_meta_mismatches': errors,
                               'untouched_asset_changes': untouched_errors, 'guard_content_mismatches': guard_errors,
                               'active_asset_references_to_archived_guids': dangling}}
    (ROOT / 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report['verification'], ensure_ascii=False))
    assert not errors and not untouched_errors and not guard_errors and not dangling

if __name__ == '__main__':
    main()
