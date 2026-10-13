"""Preserve legacy launch bodies separately while leaving shared campaign UI active."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'
name = 'Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs'
source = ROOT / name
original = source.read_bytes()
backup = JOURNAL / 'originals' / name
assert not backup.exists(), 'Mixed source backup already exists'
backup.parent.mkdir(parents=True, exist_ok=True)
backup.write_bytes(original)
text = original.decode('utf-8-sig')
pattern = r'#if ANIMOL_LEGACY_MAP_EDITOR([^#]*?)#else[^#]*?#endif'
legacy, count = re.subn(pattern, lambda m: m[1], text, flags=re.S)
assert count == 3, 'Expected exactly the three previously separated launch bodies'
legacy = legacy.replace('            // Archived scene entry: Legacy/Platformer_20261013/Assets/ANIMOL/Scenes/.\n', '')
archive = ROOT / 'Legacy/Platformer_20261013' / name
assert not archive.exists(), 'Do not overwrite an existing legacy snapshot'
archive.parent.mkdir(parents=True, exist_ok=True)
archive.write_text(legacy, encoding='utf-8')
archive.with_name(archive.name + '.meta').write_bytes(source.with_name(source.name + '.meta').read_bytes())
old = '// Archived scene entry: Legacy/Platformer_20261013/Assets/ANIMOL/Scenes/.'
new = '// LEGACY: platformer map editor, 2026-10-13\n            // Former scene launch body: Legacy/Platformer_20261013/' + name
assert text.count(old) == 3
source.write_text(text.replace(old, new), encoding='utf-8')
plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
plan['mixed_legacy_copy'] = name
(JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
