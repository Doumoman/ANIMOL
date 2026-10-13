"""Read-only GUID/reference inventory for the 2026-10-13 Unity cutover."""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEXT_EXT = {'.cs', '.unity', '.prefab', '.asset', '.mat', '.anim', '.controller',
            '.overrideController', '.asmdef', '.asmref', '.json', '.spriteatlas', '.meta'}

def inventory():
    files = sorted(p for p in (ROOT / 'Assets').rglob('*') if p.is_file())
    paths = [p.relative_to(ROOT).as_posix() for p in files]
    texts = {}
    for p, name in zip(files, paths):
        if p.suffix in TEXT_EXT:
            texts[name] = p.read_text(encoding='utf-8-sig', errors='replace')
    guids, duplicates = {}, {}
    for name, text in texts.items():
        if name.endswith('.meta'):
            match = re.search(r'^guid: ([0-9a-f]{32})$', text, re.M)
            if match:
                guid = match[1]
                if guid in guids:
                    duplicates.setdefault(guid, [guids[guid]]).append(name[:-5])
                else:
                    guids[guid] = name[:-5]
    refs = {}
    for name, text in texts.items():
        if not name.endswith('.meta'):
            refs[name] = sorted(set(guids[g] for g in re.findall(r'\b(?:guid: |GUID:)([0-9a-f]{32})', text) if g in guids))
    asmdefs = {name: json.loads(text) for name, text in texts.items() if name.endswith('.asmdef')}
    status = subprocess.check_output(['git', 'status', '--porcelain=v1', '-z', '--untracked-files=all'], cwd=ROOT).decode('utf-8').split('\0')
    hashes = {name: hashlib.sha256(p.read_bytes()).hexdigest() for p, name in zip(files, paths)}
    return dict(paths=paths, texts=texts, guids=guids, duplicate_guids=duplicates,
                references=refs, asmdefs=asmdefs, status=[s for s in status if s], sha256=hashes)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = inventory()
    result.pop('texts')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'files': len(result['paths']), 'guids': len(result['guids']),
                      'duplicate_guids': len(result['duplicate_guids']), 'asmdefs': len(result['asmdefs']),
                      'existing_changes': len(result['status'])}))
