"""Portable offline package checks; does not run or compile Unity."""
from pathlib import Path
from collections import Counter
import argparse
import hashlib
import json
import math
import re
from PIL import Image

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def verify(root):
    root = Path(root).resolve()
    m = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
    assert m['schemaVersion'] == 1
    assert m['packageId'] == 'ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01'
    assert (root / 'manifest_sha256.txt').read_text().strip() == sha(root / 'manifest.json')
    design = json.loads((root / 'decoration_catalog.json').read_text(encoding='utf-8'))
    nets = {a['key'] for a in design['networks']}
    groups = {a['key']: a['footprintCells'] for a in design['groups'] + design['points']}
    point_keys = {a['key'] for a in design['points']}
    assert (len(nets), len(groups), len(point_keys)) == (6, 20, 8)
    assets = m['assets']
    assert 116 <= len(assets) <= 118
    paths = [a['file'] for a in assets]
    assert len(paths) == len(set(paths)) == len({p.casefold() for p in paths})
    actual = {p.relative_to(root).as_posix() for p in (root / 'decor').rglob('*.png')}
    assert actual == set(paths)
    kinds = Counter()
    by_file = {}
    source_files = set()
    for a in assets:
        rel = a['file']
        assert rel.startswith('decor/') and not Path(rel).is_absolute() and '..' not in Path(rel).parts
        p = root / rel
        assert re.fullmatch(r'[0-9a-f]{64}', a['sha256']) and sha(p) == a['sha256'], rel
        with Image.open(p) as im:
            im.load()
            assert im.format == 'PNG' and im.mode == 'RGBA'
            assert im.size == (a['width'], a['height'])
            alpha = im.getchannel('A')
            assert alpha.getextrema()[0] == 0 and alpha.getextrema()[1] > 0, rel
        assert a['ppu'] == 256
        x, y, w, h = a['logicalRect']
        cx, cy = a['footprintCells']
        assert all(type(n) is int for n in [x, y, w, h, cx, cy])
        assert w == cx * 256 and h == cy * 256 and min(cx, cy) >= 1
        assert 0 <= x and 0 <= y and x + w <= a['width'] and y + h <= a['height']
        px, py = a['pivot']
        assert all(math.isfinite(v) and 0 <= v <= 1 for v in [px, py])
        assert abs(px * a['width'] - (x + w / 2)) < 0.01, rel
        assert abs((1 - py) * a['height'] - (y + h / 2)) < 0.01, rel
        if a['kind'] in ['connected_tile', 'periodic_post']:
            assert a['family'] in nets and [a['width'], a['height']] == [320, 384]
            assert a['logicalRect'] == [32, 96, 256, 256] and [cx, cy] == [1, 1]
        else:
            assert a['family'] in groups and [cx, cy] == groups[a['family']]
            assert a['kind'] == ('point' if a['family'] in point_keys else 'group')
        if a['family'] == 'stacked_props_2x2':
            with Image.open(p) as im:
                ground_alpha = im.getchannel('A').crop((x, y, x + w, y + h)).point(lambda n: 255 if n >= 128 else 0)
                bounds = ground_alpha.getbbox()
                assert bounds and (bounds[2] - bounds[0]) >= w * 0.9
                assert (bounds[3] - bounds[1]) >= h * 0.85, '2x2 prop pile leaves an almost empty row'
                for row_top in [0, 256]:
                    row = ground_alpha.crop((0, row_top, w, row_top + 256))
                    density = sum(value * count for value, count in enumerate(row.histogram())) / (255 * w * 256)
                    assert density >= 0.35, ('2x2 prop pile row too empty', row_top, density)
        refs = a.get('sourceFiles', [])
        assert refs, ('missing native provenance', rel)
        for source in refs:
            assert source.startswith('sources/') and (root / source).is_file(), source
            source_files.add(source)
        kinds[a['kind']] += 1
        by_file[rel] = a
    assert kinds['connected_tile'] == 96 and kinds['group'] == 12 and kinds['point'] == 8
    assert {a['family'] for a in assets} == nets | set(groups)
    for family in nets:
        masks = [by_file[f'decor/connected/{family}/n{n:02}.png'] for n in range(16)]
        assert all(a['family'] == family and a['kind'] == 'connected_tile' for a in masks)
        assert len({a['sha256'] for a in masks}) == 16
    group_table = {g['key']: g for g in m['groups']}
    assert len(m['groups']) == len(group_table) == 20 and set(group_table) == set(groups)
    for key, size in groups.items():
        g = group_table[key]
        a = by_file[g['file']]
        assert g['footprintCells'] == size and a['family'] == key and a['footprintCells'] == size
    for a in assets:
        if a['kind'] == 'periodic_post':
            assert a['family'] in ['velvet_aisle_barrier', 'carved_stage_balustrade']
    source_manifest = json.loads((root / 'native_sources_manifest.json').read_text())
    assert source_files <= {a['file'] for a in source_manifest['sources']}
    for a in source_manifest['sources']:
        assert sha(root / a['file']) == a['sha256'], a['file']
        with Image.open(root / a['file']) as im:
            im.load()
            assert im.size == (a['width'], a['height'])
    checksum_count = 0
    sums = root / 'package_checksums_sha256.txt'
    if sums.exists():
        for line in sums.read_text().splitlines():
            checksum, rel = line.split('  ', 1)
            assert sha(root / rel) == checksum, rel
            checksum_count += 1
    return {'status': 'PASS_OFFLINE', 'runtimePngCount': len(assets), 'decorationFamilyCount': 26,
            'networkFamilies': 6, 'wholeMultiCellGroups': 12, 'pointProps': 8,
            'kindCounts': dict(kinds), 'nativeSourceFiles': len(source_manifest['sources']),
            'checksumEntriesVerified': checksum_count,
            'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN',
            'actualNUnit': 'NOT_RUN', 'actualHostBindings': 'NOT_RUN', 'actualUnityPlay': 'NOT_RUN'}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--package', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = verify(args.package)
    payload = json.dumps(result, ensure_ascii=False, indent=2) + '\n'
    if args.output:
        args.output.write_text(payload, encoding='utf-8')
    print(payload)
