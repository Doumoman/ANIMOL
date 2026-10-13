"""Restore only a known build-generated font change to its verified clean starting bytes."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'
before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
restored = []
for name in ['Assets/ANIMOL/Typography/NotoSansSymbols2-Regular_Fallback.asset', 'ProjectSettings/UnityConnectSettings.asset']:
    assert not any(s[3:] == name for s in before['status']), 'Pre-existing change detected; do not restore: ' + name
    blob = subprocess.check_output(['git', 'show', before['base_commit'] + ':' + name], cwd=ROOT)
    variants = [blob, blob.replace(b'\n', b'\r\n')]
    original = next((b for b in variants if hashlib.sha256(b).hexdigest() == before['sha256'][name]), None)
    assert original is not None, 'Could not reconstruct the exact baseline bytes: ' + name
    path = ROOT / name
    if path.read_bytes() != original:
        backup = JOURNAL / 'build-generated' / name
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(path.read_bytes())
        path.write_bytes(original)
    assert hashlib.sha256(path.read_bytes()).hexdigest() == before['sha256'][name]
    restored.append(name)
print(json.dumps({'verified_original_bytes_restored': restored}))
