"""Read the current project and classify assets from live reference roots, retaining unknowns."""
import collections
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'
spec = importlib.util.spec_from_file_location('inventory', ROOT / 'Tools/topdown_cutover_inventory.py')
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)

def stripped_code(text):
    return re.sub(r'//[^\n]*|/\*.*?\*/|@?"(?:\\.|""|[^"\\])*"', ' ', text, flags=re.S)

def top_types(text):
    found = set()
    for match in re.finditer(r'\b(?:class|struct|enum|interface)\s+(\w+)', text):
        prefix = text[:match.start()]
        if prefix.count('{') - prefix.count('}') <= 1:
            found.add(match[1])
    return found

def main():
    data = inventory.inventory()
    before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    old = json.loads((ROOT / 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json').read_text(encoding='utf-8'))
    texts = data.pop('texts')
    active = sorted(p for p in data['paths'] if p.startswith('Assets/ANIMOL/') and not p.endswith('.meta'))
    files = set(active)
    references = {p: set(t for t in data['references'].get(p, []) if t in files) for p in active}
    declarations = collections.defaultdict(set)
    tokens = {}
    for p in active:
        if p.endswith('.cs') and p not in plan['guarded']:
            code = stripped_code(texts[p])
            tokens[p] = set(re.findall(r'\b\w+\b', code))
            for name in top_types(code):
                declarations[name].add(p)
    for p, names in tokens.items():
        for name in names:
            references[p].update(declarations[name] - {p})
    for p in active:
        if p in plan['guarded'] or p not in texts:
            continue
        for target in re.findall(r'Assets/[\w./ -]+\.(?:unity|asset|prefab|png|json|anim|spriteatlas)', texts[p]):
            if target in files and target != p:
                references[p].add(target)
        for key in re.findall(r'Resources\.Load(?:<[^>]+>)?\("([^"]+)"\)', texts[p]):
            references[p].update(t for t in files if '/Resources/' in t and t.split('/Resources/', 1)[1].rsplit('.', 1)[0] == key)
    incoming = collections.defaultdict(set)
    for p, targets in references.items():
        for t in targets:
            incoming[t].add(p)
    shared_prefixes = ('Assets/ANIMOL/UI/', 'Assets/ANIMOL/AnimalUiV2/', 'Assets/ANIMOL/NetUiDev/',
                       'Assets/ANIMOL/Typography/', 'Assets/ANIMOL/Fonts/', 'Assets/ANIMOL/Prefabs/UI/',
                       'Assets/ANIMOL/Sprites/UI/', 'Assets/ANIMOL/SpritePathEditor/', 'Assets/ANIMOL/NamedArt/')
    core_common = {'BubbleObjectiveService.cs', 'GrowthEconomyContracts.cs', 'CampaignProgressionService.cs',
                   'MultiplayerMatchContracts.cs', 'RunSessionController.cs', 'CampaignStageDefinition.cs',
                   'ThemeDefinition.cs', 'CampaignCatalog.cs'}
    roots = {p for p in active if p.startswith(shared_prefixes) or '/Scripts/UI/' in p
             or Path(p).name in core_common or p in {'Assets/ANIMOL/Scenes/Bootstrap.unity',
                                                    'Assets/ANIMOL/Scenes/Lobby.unity', 'Assets/ANIMOL/Scenes/Results.unity'}}
    roots -= set(plan['guarded'])
    reachable = set(roots)
    queue = list(sorted(roots))
    predecessor = {p: None for p in roots}
    while queue:
        p = queue.pop()
        for target in references[p] - reachable:
            reachable.add(target)
            predecessor[target] = p
            queue.append(target)
    archive_sources = {m['source'] for m in old['moves'] if m['destination'].startswith('Legacy/')}
    records = []
    for p in active:
        if p.startswith('Assets/ANIMOL/Phase1/'):
            category, reason = 'topdown-puzzle', 'Exact Phase1 root exists; preserve prototype until source/PPU/P01 validation'
        elif p in reachable:
            category = 'shared-or-still-referenced'
            reason = 'Shared UI/service/boot root' if predecessor[p] is None else 'Reachable from shared root through ' + predecessor[p]
        elif p in plan['guarded']:
            category, reason = 'platformer-only', 'Inactive original generator partial; no active external C# caller; original copied to Legacy'
        elif p.startswith(('Assets/ANIMOL/Scripts/Gameplay/', 'Assets/ANIMOL/Scripts/Development/TerrainEditorV4/',
                           'Assets/ANIMOL/TerrainStructure/', 'Assets/ANIMOL/ObstacleGraphicsV1/')):
            category = 'shared-or-still-referenced' if incoming[p] else 'unknown'
            reason = 'Existing typed/serialized consumers require retaining this file' if incoming[p] else 'Platformer provenance; no proven safe replacement; retain for compilation'
        else:
            category, reason = 'unknown', 'Not proven dedicated or reachable from a shared root; retained without editing'
        guid = next((g for g, target in data['guids'].items() if target == p), None)
        records.append({'path': p, 'guid': guid, 'category': category, 'referrers': sorted(incoming[p]),
                        'reason': reason, 'action': 'disabled-with-legacy-copy' if p in plan['guarded'] else 'retain'})
    for move in old['moves']:
        if move['source'] in archive_sources:
            records.append({'path': move['destination'], 'original_path': move['source'], 'guid': move['guid'],
                            'category': 'platformer-only', 'referrers': [], 'reason': 'Previously isolated dedicated asset; byte/meta verification retained', 'action': 'archived'})
    for p in plan['guarded']:
        guid = next(g for g, target in data['guids'].items() if target == p)
        records.append({'path': 'Legacy/Platformer_20261013/' + p, 'original_path': p, 'guid': guid,
                        'category': 'platformer-only', 'referrers': [], 'reason': 'Unwrapped original of inactive legacy bulk-generation partial', 'action': 'copied-original'})
    if plan.get('mixed_legacy_copy'):
        p = plan['mixed_legacy_copy']
        records.append({'path': 'Legacy/Platformer_20261013/' + p, 'original_path': p,
                        'guid': next(g for g, target in data['guids'].items() if target == p),
                        'category': 'shared-or-still-referenced', 'referrers': [p],
                        'reason': 'Recovery snapshot of former launch bodies within the shared campaign UI; only callers remain gated',
                        'action': 'copied-legacy-parts'})
    moves = {m['source']: m['destination'] for m in plan['sprites']}
    changed = set(plan['guarded']) | set(moves) | {'Assets/ANIMOL/Sprites/sprite_path_manifest.json',
              'Assets/ANIMOL/SpritePathEditor/CanonicalSpritePaths.cs', 'Assets/ANIMOL/SpritePathEditor/Tests/Editor/CanonicalSpritePathTests.cs',
              'ProjectSettings/ANIMOLNamedArtReferences.json'}
    if plan.get('mixed_legacy_copy'):
        changed.add(plan['mixed_legacy_copy'])
    failures, unrelated, originals = [], [], []
    for source, destination in moves.items():
        for suffix in ['', '.meta']:
            if hashlib.sha256((ROOT / (destination + suffix)).read_bytes()).hexdigest() != before['sha256'][source + suffix]:
                failures.append(source + suffix)
    for p, expected in before['sha256'].items():
        if (p[:-5] if p.endswith('.meta') else p) in changed:
            continue
        path = ROOT / p
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            unrelated.append(p)
    for p in plan['guarded']:
        path = ROOT / p
        archive = ROOT / 'Legacy/Platformer_20261013' / p
        body = path.read_bytes().split(b'#if ANIMOL_LEGACY_MAP_EDITOR\n', 1)[1].rsplit(b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n', 1)[0]
        if body != archive.read_bytes() or archive.with_name(archive.name + '.meta').read_bytes() != path.with_name(path.name + '.meta').read_bytes():
            originals.append(p)
    stages = sorted(p for p in active if '/Data/Campaign/Stages/' in p and p.endswith('.asset'))
    stage_counts = dict(collections.Counter(Path(p).stem.split('-')[0] for p in stages))
    result = {'root': str(ROOT), 'base_commit': before['base_commit'], 'unity_version': (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0],
              'phase1_exists': plan['phase1_exists'], 'p01_scenes': plan['p01_scenes'], 'build_settings': (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text(),
              'start_scene': 'Assets/ANIMOL/Scenes/Bootstrap.unity', 'asmdefs': data['asmdefs'], 'duplicate_guids': data['duplicate_guids'],
              'category_counts': dict(collections.Counter(r['category'] for r in records)), 'campaign_stage_slots': stage_counts,
              'verification': {'moved_byte_or_meta_mismatches': failures, 'unrelated_existing_file_changes': unrelated,
                               'legacy_original_mismatches': originals}, 'records': records, 'supplement_moves': plan['sprites']}
    dest = ROOT / 'Docs/ANIMOL_CUTOVER_CLASSIFICATION.json'
    dest.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'category_counts': result['category_counts'], 'campaign_stage_slots': stage_counts, 'verification': result['verification']}, ensure_ascii=False))
    assert not failures and not unrelated and not originals and not data['duplicate_guids']

if __name__ == '__main__':
    main()
