"""Add canonical UI sprite moves and temporarily exclude archive code for batch execution."""
import json
import re
from pathlib import Path
from prepare_topdown_cutover import ROOT, JOURNAL, HEADER

def remember(path):
    target = JOURNAL / 'originals' / path.relative_to(ROOT)
    if not target.exists():
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(path.read_bytes())

def main():
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    for name in plan['archive']:
        if name.endswith('.cs'):
            path = ROOT / name
            remember(path)
            path.write_bytes(HEADER.encode() + path.read_bytes() + b'\n#endif // ANIMOL_LEGACY_MAP_EDITOR\n')
    sprites = []
    for name in sorted(plan['sprites_existing']):
        if not name.endswith('.png'):
            continue
        path = Path(name)
        lower = name.lower()
        category = ('Settings' if '/settings/' in lower else 'Store' if '/store/' in lower
                    else 'StageSelect' if 'stage' in lower else 'InGame' if 'pause' in lower or 'hud' in lower
                    else 'MainMenu' if 'FiveThemeMenuMaps' in name else 'Common')
        package = path.parts[3]
        def snake(value):
            value = re.sub(r'([a-z0-9])([A-Z])', r'\1_\2', value)
            return re.sub(r'[^a-z0-9]+', '_', value.lower()).strip('_')
        filename = 'spr_ui_' + snake(category) + '_' + snake(package) + '_' + snake(path.stem) + '.png'
        before = json.loads((JOURNAL / 'before.json').read_text(encoding='utf-8'))
        guid = next(g for g, source in before['guids'].items() if source == name)
        sprites.append(dict(source=name, destination='Assets/ANIMOL/Sprites/UI/' + category + '/' + filename, guid=guid))
    plan['sprites'] = sprites
    (JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
    destination = ROOT / 'Assets/ANIMOL/Sprites/sprite_path_manifest.json'
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({'sprites': sprites}, ensure_ascii=False, indent=2), encoding='utf-8')
    for folder in ['UI/MainMenu', 'UI/InGame', 'UI/Settings', 'UI/Common', 'UI/StageSelect', 'UI/Store',
                   'Player', 'Tiles/Floor', 'Tiles/Wall', 'Tiles/Decoration', 'Obstacles', 'Background']:
        (destination.parent / folder).mkdir(parents=True, exist_ok=True)
    # Editor builders retain their source package keys, resolving the canonical asset path.
    changed = []
    for path in (ROOT / 'Assets/ANIMOL').rglob('*.cs'):
        name = path.relative_to(ROOT).as_posix()
        if name in plan['archive'] or name in plan['guarded'] or 'SpritePathEditor' in name:
            continue
        text = path.read_text(encoding='utf-8-sig')
        if '/Editor/' not in name:
            continue
        original = text
        # Balance parentheses to wrap both literal and computed AssetDatabase paths.
        pattern = re.compile(r'AssetDatabase\.LoadAssetAtPath<(?:Sprite|Texture2D)>\(')
        matches = list(pattern.finditer(text))
        for match in reversed(matches):
            start, pos, depth, quoted = match.end(), match.end(), 1, False
            while pos < len(text) and depth:
                ch = text[pos]
                if ch == '"' and (pos == 0 or text[pos - 1] != '\\'):
                    quoted = not quoted
                if not quoted:
                    if ch == '(':
                        depth += 1
                    elif ch == ')':
                        depth -= 1
                pos += 1
            arg = text[start:pos - 1]
            text = text[:start] + 'ANIMOL.SpritePathEditor.CanonicalSpritePaths.Resolve(' + arg + ')' + text[pos - 1:]
        if 'MissingUiV1Builder.cs' in name:
            text = text.replace('var path = Root + "/Sprites/" + relative;', 'var path = ANIMOL.SpritePathEditor.CanonicalSpritePaths.Resolve(Root + "/Sprites/" + relative);')
            text = text.replace('Path.GetFileNameWithoutExtension(name) + " t:Sprite", new[] { Root + "/Sprites" }',
                                'Path.GetFileNameWithoutExtension(ANIMOL.SpritePathEditor.CanonicalSpritePaths.Resolve(Root + "/Sprites/" + name)) + " t:Sprite", new[] { "Assets/ANIMOL/Sprites/UI" }')
        for move in sprites:
            text = text.replace('"' + move['source'] + '"', '"' + move['destination'] + '"')
        if text != original:
            remember(path)
            path.write_text(text, encoding='utf-8')
            changed.append(name)
            # The containing explicit assembly must reference the independent path resolver.
            parent = path.parent
            while parent != ROOT:
                asmdefs = list(parent.glob('*.asmdef'))
                if asmdefs:
                    asm = asmdefs[0]
                    config = json.loads(asm.read_text(encoding='utf-8-sig'))
                    if 'ANIMOL.SpritePathEditor' not in config.get('references', []):
                        remember(asm)
                        config.setdefault('references', []).append('ANIMOL.SpritePathEditor')
                        asm.write_text(json.dumps(config, indent=2) + '\n', encoding='utf-8')
                        changed.append(asm.relative_to(ROOT).as_posix())
                    break
                parent = parent.parent
    plan['path_updates'] = sorted(set(changed))
    (JOURNAL / 'plan.json').write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'sprites': len(sprites), 'path_updates': len(set(changed))}))

if __name__ == '__main__':
    main()
