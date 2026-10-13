"""Leave common campaign UI usable while archived scene launch code is disabled."""
import json
from prepare_topdown_cutover import ROOT, JOURNAL

name = 'Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs'
path = ROOT / name
original = path.read_bytes()
backup = JOURNAL / 'originals' / name
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists():
    backup.write_bytes(original)
text = original.decode('utf-8-sig')
for method in ['LaunchDevelopmentRun', 'LaunchMapDevelopmentRun', 'LaunchSelectedStage']:
    start = text.index('        private void ' + method + '()')
    opening = text.index('{', start)
    pos, depth = opening + 1, 1
    while depth:
        if text[pos] == '{':
            depth += 1
        elif text[pos] == '}':
            depth -= 1
        pos += 1
    body = text[opening + 1:pos - 1]
    text = text[:opening + 1] + '\n            // Archived scene entry: Legacy/Platformer_20261013/Assets/ANIMOL/Scenes/.\n#if ANIMOL_LEGACY_MAP_EDITOR' + body + '\n#else\n            ShowToast("탑뷰 퍼즐 준비 중입니다.");\n#endif\n        ' + text[pos - 1:]
path.write_text(text, encoding='utf-8')
plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
plan['path_updates'].append(name)
(JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
