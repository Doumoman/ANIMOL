"""Prepare dependency-conservative cutover; Unity performs the actual asset moves.

Keep current worktree bytes in an external journal. Index updates are handled separately,
so pre-existing changes are never incorporated into the cutover commit.
"""
import importlib.util
import json
import re
from pathlib import Path

spec = importlib.util.spec_from_file_location('audit', Path(__file__).with_name('topdown_cutover_inventory.py'))
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
ROOT = audit.ROOT
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-journal'
LEGACY = 'Legacy/Platformer_20261013/'
DEFINE = 'ANIMOL_LEGACY_MAP_EDITOR'
HEADER = ('// Platformer archive: Legacy/Platformer_20261013/ (original Assets-relative paths).\n'
          '// Disabled by default during the top-view swipe puzzle cutover.\n'
          '#if ANIMOL_LEGACY_MAP_EDITOR\n')

def main():
    if JOURNAL.exists():
        raise RuntimeError('Journal already exists; inspect it before preparing another cutover.')
    data = audit.inventory()
    texts = data.pop('texts')
    cs = {p: t for p, t in texts.items() if p.endswith('.cs') and p.startswith('Assets/ANIMOL/')}
    def code_only(text):
        return re.sub(r'//[^\n]*|/\*.*?\*/|@?"(?:\\.|""|[^"\\])*"', ' ', text, flags=re.S)
    clean = {p: code_only(t) for p, t in cs.items()}
    def top_types(text):
        found = set()
        for match in re.finditer(r'\b(?:class|struct|enum|interface)\s+(\w+)', text):
            prefix = text[:match.start()]
            if prefix.count('{') - prefix.count('}') <= 1:
                found.add(match[1])
        return found
    declarations = {p: top_types(t) for p, t in clean.items()}
    tokens = {p: set(re.findall(r'\b\w+\b', t)) for p, t in clean.items()}
    guards = {p for p in cs if '/Scripts/Editor/TerrainEditorV4/' in p or
              Path(p).stem.startswith(('StageMapAuthoring', 'StageMapObjectAuthoring', 'StageMapScenePalette',
                                      'StageTerrainStructureIntegration', 'CampaignMapEditorWindow',
                                      'T01S01ManualMapWorkflow', 'T01S01HumanReviewWindow'))}
    # Propagate removed types through callers and all parts of partial classes.
    while True:
        names = set().union(*(declarations[p] for p in guards))
        added = {p for p in cs if tokens[p] & names and ('/Editor/' in p or '/Tests/' in p)} - guards
        if not added:
            break
        guards |= added
    files = {p for p in data['paths'] if not p.endswith('.meta')}
    scene_candidates = {p for p in files if p.startswith('Assets/ANIMOL/Scenes/') and p.endswith('.unity')
                        and Path(p).stem not in {'Bootstrap', 'Lobby', 'Results'}}
    prefixes = ('Assets/ANIMOL/RunPrototype/', 'Assets/ANIMOL/Art/MapObjects/',
                'Assets/ANIMOL/Prefabs/MapObjects/', 'Assets/ANIMOL/Prefabs/Gameplay/',
                'Assets/ANIMOL/Data/Development/', 'Assets/ANIMOL/Data/Campaign/Maps/',
                'Assets/ANIMOL/ObstacleGraphicsV1/', 'Assets/ANIMOL/TerrainFreeShape/',
                'Assets/ANIMOL/TerrainStructure/', 'Assets/ANIMOL/Atlases/',
                'Assets/ANIMOL/Generated/', 'Assets/ANIMOL/GraphicsQA/', 'Assets/ANIMOL/MapBackups/')
    candidates = {p for p in files if p.startswith(prefixes)} | scene_candidates
    candidates |= {p for p in cs if '/Scripts/Gameplay/' in p and Path(p).stem != 'GameplayFeedbackDirector'}
    candidates |= {p for p in cs if Path(p).stem.startswith(('RunPrototype', 'RunRoutePlan'))}
    # Dedicated editor/test code can be archived; mixed common UI builders stay guarded.
    candidates |= {p for p in guards if not Path(p).name.startswith('UiBuildPipeline')
                   and ('/Scripts/Editor/' in p or '/Tests/' in p)}
    # Retain assembly definitions; moving individual files keeps surviving assemblies intact.
    candidates = {p for p in candidates if not p.endswith(('.asmdef', '.asmref'))}
    retained = {}
    while True:
        blocked = {}
        for source in files - candidates:
            if source in guards:
                continue
            for target in data['references'].get(source, []):
                if target in candidates:
                    blocked.setdefault(target, source + ' (GUID)')
            if source in cs:
                for target in candidates & cs.keys():
                    if declarations[target] & tokens[source]:
                        blocked.setdefault(target, source + ' (C# type)')
            # Exact serialized/editor paths are also dependencies.
            if source in texts:
                for target in re.findall(r'Assets/[\w./ -]+\.(?:unity|asset|prefab|png|json|anim)', texts[source]):
                    if target in candidates:
                        blocked.setdefault(target, source + ' (path)')
        if not blocked:
            break
        candidates -= blocked.keys()
        retained.update(blocked)
    # Run prototype tests/builders must follow the removed RunPrototype types.
    for p, t in cs.items():
        if 'RunPrototype' in Path(p).stem and p not in candidates:
            guards.add(p)
    JOURNAL.mkdir(parents=True)
    (JOURNAL / 'before.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    changed = guards - candidates
    for p in changed:
        path = ROOT / p
        original = path.read_bytes()
        backup = JOURNAL / 'originals' / p
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(original)
        path.write_bytes(HEADER.encode() + original + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
    plan = {'archive': sorted(candidates), 'guarded': sorted(changed), 'retained_dependencies': retained,
            'legacy_root': LEGACY, 'sha256': {p: data['sha256'][p] for p in candidates},
            'sprites_existing': [p for p in files if p.startswith('Assets/ANIMOL/') and '/Sprites/' in p],
            'p01_scenes': [p for p in files if p.endswith('.unity') and 'p01' in p.lower()],
            'topdown_files': [p for p in files if re.search(r'top.?down|swipe', p, re.I)],
            'phase1': [p for p in files if 'Phase1' in p]}
    (JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: len(v) for k, v in plan.items() if isinstance(v, (list, dict))}))

if __name__ == '__main__':
    main()
