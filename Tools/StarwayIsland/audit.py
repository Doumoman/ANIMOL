"""Read-only source/import and actual Game View pixel audit. Run from repo root."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

SOURCE = Path('Docs/Inbox/StarwaySkyIsland/ANIMOL_STARWAY_SKYISLAND_V02')
OUTPUT = Path('Docs/Validation/StarwayIsland')

def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def audit():
    listed = 0
    for line in (SOURCE / 'SHA256SUMS.txt').read_text(encoding='utf-8').splitlines():
        expected, name = line.split(None, 1)
        assert digest(SOURCE / name.strip().lstrip('*')) == expected, name
        listed += 1
    manifest = json.loads((SOURCE / 'manifest.json').read_text(encoding='utf-8'))
    master = {tuple(bytes.fromhex(color[1:])) for color in manifest['palette']}
    island = {tuple(bytes.fromhex(color[1:])) for color in manifest['islandPalette']}
    imported = json.loads((OUTPUT / 'import_bytes.json').read_text(encoding='utf-8-sig'))
    for item in imported['files']:
        assert digest(item['source']) == digest(item['target']) == item['sha256'], item['target']
        colors = Image.open(item['target']).convert('RGBA').getcolors(65536)
        assert colors is not None, item['target']
        allowed = island if '/Tiles/Floor/' in item['target'].replace('\\', '/') else master
        for count, color in colors:
            assert color[3] in (0, 255), item['target']
            assert color[:3] in allowed if color[3] else color[:3] == (0, 0, 0), item['target']
    comparisons = []
    for capture, reference in [
        ('join_1x.png', 'inspection/island_join_1x.png'),
        ('join_4x.png', 'inspection/island_join_4x.png'),
        ('background_A.png', 'camera/A_origin_composite.png'),
        ('background_B.png', 'camera/B_right_down_composite.png'),
        ('background_C.png', 'camera/C_left_down_composite.png')
    ]:
        # Game View has an opaque black clear behind the source's transparent voids.
        actual = Image.open(OUTPUT / capture).convert('RGB')
        expected = Image.open(SOURCE / reference).convert('RGB')
        assert actual.size == expected.size, capture
        difference = ImageChops.difference(actual, expected)
        assert difference.getbbox() is None, capture
        comparisons.append(dict(capture=capture, reference=reference, size=actual.size,
                                differing_rgb_pixels=0, sha256=digest(OUTPUT / capture)))
    baseline = json.loads((OUTPUT / 'before_hashes.json').read_text(encoding='utf-8-sig'))
    changed = [name for name, value in baseline.items() if digest(name) != value]
    permitted = ['Assets/ANIMOL/Scripts/Runtime/' + name + '.cs' for name in ['PuzzleTheme', 'PuzzleMapView', 'PuzzleBackdrop']]
    assert sorted(changed) == sorted(permitted), changed
    report = dict(source_sha256_entries_verified=listed, native_imports_verified=len(imported['files']),
                  palette_binary_alpha_and_transparent_rgb_verified=True, comparisons=comparisons,
                  protected_files=len(baseline), changed_visual_scripts=changed,
                  maps_gameplay_swipe_ui_scenes_and_other_theme_assets_unchanged=True)
    (OUTPUT / 'pixel_and_scope_audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    audit()
