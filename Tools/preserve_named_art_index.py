"""Preserve the dirty reference manifest and prepare an index-only HEAD transformation."""
import json
import subprocess
from prepare_topdown_cutover import ROOT, JOURNAL, LEGACY

name = 'ProjectSettings/ANIMOLNamedArtReferences.json'
path = ROOT / name
backup = JOURNAL / 'originals' / name
backup.parent.mkdir(parents=True, exist_ok=True)
assert not backup.exists(), 'Do not overwrite the original manifest backup'
backup.write_bytes(path.read_bytes())
archive = ROOT / LEGACY / name
archive.parent.mkdir(parents=True, exist_ok=True)
archive.write_bytes(path.read_bytes())
head = subprocess.check_output(['git', 'show', 'HEAD:' + name], cwd=ROOT)
(JOURNAL / 'namedart-head-transformed.json').write_bytes(head)
plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
plan['manifest_updates'] = [name]
(JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
