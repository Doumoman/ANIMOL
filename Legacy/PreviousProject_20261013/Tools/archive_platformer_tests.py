"""Include remaining physical platformer tests, while preserving common service tests."""
import json
import re
from prepare_topdown_cutover import ROOT, JOURNAL, HEADER

plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
added = []
for path in (ROOT / 'Assets/ANIMOL/Tests').rglob('*.cs'):
    if path.stem in {'CommercialPolishPolicyTests', 'CampaignCoreTests'}:
        continue
    text = path.read_text(encoding='utf-8-sig')
    if not (re.search(r'"(?:Gameplay|MapDevTest|ObjectLab|MoonObjectLab|ThemePlatformLab)"|ThemeRuntime_|DevPlayerController|DevTestSession', text)
            or path.stem.startswith(('M7', 'M8', 'M9B', 'M9C', 'M9Map', 'M9T01', 'AnimolMovement', 'AnimolStateful',
                                     'AnimolOperational', 'AnimolAtlas', 'CellBounds', 'Obstacle', 'CommonObstacle',
                                     'FreeShape', 'PlayerBody', 'TerrainStructure', 'ThemeFinish', 'MoonGraphics', 'Ovg04'))):
        continue
    name = path.relative_to(ROOT).as_posix()
    backup = JOURNAL / 'originals' / name
    backup.parent.mkdir(parents=True, exist_ok=True)
    backup.write_bytes(path.read_bytes())
    path.write_bytes(HEADER.encode() + path.read_bytes() + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
    plan['archive'].append(name)
    plan['sha256'][name] = before['sha256'][name]
    added.append(name)
(JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'additional_platformer_tests': len(added), 'total_archive': len(plan['archive'])}))
