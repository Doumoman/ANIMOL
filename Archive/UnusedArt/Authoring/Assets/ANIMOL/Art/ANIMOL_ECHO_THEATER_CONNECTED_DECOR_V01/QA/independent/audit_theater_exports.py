"""Independent PNG/metadata/native-provenance checks. Does not import producer code.

This is an offline asset audit, not a Unity import, source compilation, or play test.
Run after producer exports are frozen. Missing producers are reported NOT_READY.
"""
from pathlib import Path
from PIL import Image
import argparse
import hashlib
import json
import re

HERE = Path(__file__).resolve().parent
WORK = HERE.parent
NETS = ['velvet_aisle_barrier', 'carved_stage_balustrade', 'prop_shelf_run',
        'velvet_curtain_screen', 'brass_footlight_strip', 'ivy_planter_strip']
GROUPS = {'grand_piano_3x2': [3, 2], 'audience_bench_3x1': [3, 1],
          'ticket_booth_2x2': [2, 2], 'costume_rack_3x1': [3, 1],
          'vanity_dressing_2x1': [2, 1], 'phonograph_table_2x1': [2, 1],
          'scenery_flat_3x1': [3, 1], 'stacked_props_2x2': [2, 2],
          'mask_display_2x1': [2, 1], 'puppet_storage_2x2': [2, 2],
          'backstage_spotlight_duo_2x1': [2, 1], 'stage_steps_2x1': [2, 1]}
POINTS = ['ticket_stand', 'folded_chair', 'music_stand', 'tall_candelabra',
          'mask_pedestal', 'script_pile', 'sandbag_stack', 'flower_urn']
EXPECTED = {'networks_a': NETS[:3], 'networks_b': NETS[3:],
            'groups_a': list(GROUPS)[:6], 'groups_b': list(GROUPS)[6:] + POINTS}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def read(path):
    return json.loads(path.read_text(encoding='utf-8'))

def native_refs(asset):
    refs = asset.get('sourceFiles', [])
    if isinstance(refs, str): refs = [refs]
    for key in ['sourceNative', 'source_file']:
        if isinstance(asset.get(key), str): refs.append(asset[key])
    out = []
    for ref in refs:
        if isinstance(ref, str): out.append(ref)
        elif isinstance(ref, dict) and isinstance(ref.get('file'), str): out.append(ref['file'])
    return list(dict.fromkeys(out))

def audit_owner(owner):
    root = WORK / (owner + '_exports')
    manifest = root / 'manifest.json'
    if not manifest.exists():
        candidates = list(root.glob('*manifest.json'))
        if len(candidates) == 1: manifest = candidates[0]
    if not manifest.exists(): return {'status': 'NOT_READY', 'owner': owner, 'errors': ['Manifest not yet frozen.']}
    j = read(manifest)
    assets = {}
    images = {}
    errors, warnings, shape_rows, sources = [], [], [], set()
    for a in j.get('assets', []):
        name = a.get('file')
        if not isinstance(name, str) or name.startswith('/') or '..' in Path(name).parts:
            errors.append('Unsafe file path ' + str(name)); continue
        if name in assets:
            errors.append('Duplicate runtime path ' + name); continue
        assets[name] = a
        p = root / name
        if not p.is_file(): errors.append('Missing runtime PNG ' + name); continue
        try:
            im = Image.open(p); im.load()
            if im.mode != 'RGBA': errors.append('PNG must retain explicit RGBA mode ' + name)
            im = im.convert('RGBA'); images[name] = im
        except Exception as ex:
            errors.append('PNG decode ' + name + ': ' + type(ex).__name__); continue
        if list(im.size) != [a.get('width'), a.get('height')]: errors.append('Canvas dimensions ' + name)
        if sha(p) != a.get('sha256'): errors.append('File SHA256 ' + name)
        if a.get('ppu') != 256: errors.append('PPU must be actual JSON ppu:256 ' + name)
        lr, fp, pv = a.get('logicalRect'), a.get('footprintCells'), a.get('pivot')
        if not (isinstance(lr, list) and len(lr) == 4 and isinstance(fp, list) and len(fp) == 2 and isinstance(pv, list) and len(pv) == 2):
            errors.append('Ground metadata ' + name); continue
        if lr[2:] != [fp[0] * 256, fp[1] * 256]: errors.append('Ground dimensions vs footprint ' + name)
        if lr[0] < 0 or lr[1] < 0 or lr[0] + lr[2] > im.width or lr[1] + lr[3] > im.height:
            errors.append('Logical rectangle outside canvas ' + name)
        expect_pivot = [(lr[0] + lr[2] / 2) / im.width, 1 - (lr[1] + lr[3] / 2) / im.height]
        if max(abs(pv[i] - expect_pivot[i]) for i in [0, 1]) > 1e-6: errors.append('Pivot not ground-rectangle center ' + name)
        alpha = im.getchannel('A')
        bbox = alpha.getbbox()
        if bbox is None: errors.append('Empty sprite ' + name); continue
        if alpha.getextrema()[0] != 0: errors.append('No actual transparency ' + name)
        if alpha.getextrema()[1] != 255: warnings.append('No fully opaque interior ' + name)
        family = a.get('family')
        if family in NETS and a.get('kind') == 'connected_tile':
            if im.size != (320, 384) or lr != [32, 96, 256, 256] or fp != [1, 1]: errors.append('Network canonical geometry ' + name)
        if family in GROUPS:
            if fp != GROUPS[family]: errors.append('Group declared footprint ' + name)
            width_fill = (bbox[2] - bbox[0]) / lr[2]
            height_fill = (bbox[3] - bbox[1]) / lr[3]
            if width_fill < .9: errors.append('Group visible width below 90% logical width ' + name)
            if family == 'stacked_props_2x2' and height_fill < .9:
                errors.append('ART01: 2x2 whole-pile visible height below 90% logical height ' + name)
            if family == 'stacked_props_2x2':
                ground = alpha.crop((lr[0], lr[1], lr[0] + lr[2], lr[1] + lr[3])).point(lambda n: 255 if n >= 128 else 0)
                ground_bbox = ground.getbbox()
                if ground_bbox is None or ground_bbox[3] - ground_bbox[1] < lr[3] * .85:
                    errors.append('ART01: opaque 2x2 pile coverage leaves nearly empty depth row ' + name)
                for y0 in [0, 256]:
                    row = ground.crop((0, y0, 512, y0 + 256))
                    density = sum(row.getdata()) / (255 * 512 * 256)
                    if density < .35:
                        errors.append('ART01: 2x2 pile row opaque density below35% ' + str(y0) + ' ' + name)
        else: width_fill = height_fill = None
        if family in POINTS and fp != [1, 1]: errors.append('Point declared footprint ' + name)
        refs = native_refs(a)
        for ref in refs:
            if not (root / ref).is_file(): errors.append('Missing native source ' + ref + ' <- ' + name)
            else: sources.add(ref)
        if not refs: warnings.append('No per-asset sourceFiles; requires explicit root source provenance review ' + name)
        shape_rows.append({'file': name, 'family': family, 'kind': a.get('kind'), 'alphaBBox': list(bbox),
                           'logicalRect': lr, 'footprintCells': fp, 'visibleWidthFill': width_fill, 'visibleHeightFill': height_fill,
                           'rgbaPixelSha256': hashlib.sha256(im.tobytes()).hexdigest()})
    actual_families = {a.get('family') for a in assets.values()}
    missing_families = sorted(set(EXPECTED[owner]) - actual_families)
    if missing_families: errors.append('Missing semantic families ' + ', '.join(missing_families))
    for family in set(EXPECTED[owner]) & set(NETS):
        expected = {f'decor/connected/{family}/n{i:02d}.png' for i in range(16)}
        if not expected <= set(assets): errors.append('Missing 16-mask set ' + family)
        pixels = {hashlib.sha256(images[f].tobytes()).hexdigest() for f in expected if f in images}
        if len(pixels) != 16: errors.append('Not 16 distinct decoded mask images ' + family)
    return {'status': 'PASS_OFFLINE' if not errors else 'FAIL_OFFLINE', 'owner': owner,
            'manifest': str(manifest.relative_to(WORK)), 'manifestSha256': sha(manifest),
            'errors': errors, 'warnings': warnings, 'runtimeAssetCount': len(assets),
            'semanticFamilies': sorted(actual_families), 'sourceFilesReferenced': sorted(sources),
            'shapeRows': shape_rows, '_root': root, '_assets': assets, '_images': images}

def overlap_checks(report, family):
    imgs = report['_images']
    files = [f'decor/connected/{family}/n{i:02d}.png' for i in range(16)]
    if not all(f in imgs for f in files): return {'status': 'NOT_READY'}
    unsafe, identical, shared, pairs = 0, 0, 0, 0
    examples = []
    for axis in ['horizontal', 'vertical']:
        for a in range(16):
            for b in range(16):
                compatible = bool((a & 2) and (b & 8)) if axis == 'horizontal' else bool((a & 1) and (b & 4))
                if not compatible: continue
                pairs += 1
                aa = imgs[files[a]].crop((256, 0, 320, 384) if axis == 'horizontal' else (0, 0, 320, 128))
                bb = imgs[files[b]].crop((0, 0, 64, 384) if axis == 'horizontal' else (0, 256, 320, 384))
                bad = 0
                for p, q in zip(aa.getdata(), bb.getdata()):
                    if p[3] > 0 and q[3] > 0:
                        shared += 1
                        if p[3] == q[3] == 255 and p[:3] == q[:3]: identical += 1
                        else: unsafe += 1; bad += 1
                if bad and len(examples) < 12: examples.append({'axis': axis, 'maskA': a, 'maskB': b, 'unsafeOverlapPixels': bad})
    return {'status': 'PASS_SAFE_OVERLAP' if not unsafe else 'JOIN_COMPOSITION_REVIEW_REQUIRED',
            'compatiblePairs': pairs, 'expectedPairs': 128, 'sharedVisiblePixels': shared,
            'safeIdenticalOpaquePixels': identical, 'unsafeOverlapPixels': unsafe, 'examples': examples,
            'scope': 'Full padded PNG overlap at 256px cell spacing. Partial-alpha or different-RGB overlap requires review; alpha255 same-RGB overlap is harmless.'}

def boundary_profile_checks(report, family):
    imgs = report['_images']
    files = [f'decor/connected/{family}/n{i:02d}.png' for i in range(16)]
    if not all(f in imgs for f in files): return {'status': 'NOT_READY'}
    strips = {}
    missing_open, closed_visible, mismatched = [], [], []
    for n, name in enumerate(files):
        im = imgs[name]
        strips[n] = {'N': im.crop((0, 96, 320, 97)), 'E': im.crop((287, 0, 288, 384)),
                     'S': im.crop((0, 351, 320, 352)), 'W': im.crop((32, 0, 33, 384))}
        for side, bit in [('N', 1), ('E', 2), ('S', 4), ('W', 8)]:
            visible = strips[n][side].getchannel('A').getbbox() is not None
            if n & bit and not visible: missing_open.append({'mask': n, 'side': side})
            if not n & bit and visible: closed_visible.append({'mask': n, 'side': side})
    pairs = 0
    for a in range(16):
        for b in range(16):
            for sa, ba, sb, bb in [('E', 2, 'W', 8), ('N', 1, 'S', 4)]:
                if a & ba and b & bb:
                    pairs += 1
                    if strips[a][sa].tobytes() != strips[b][sb].tobytes():
                        mismatched.append({'maskA': a, 'sideA': sa, 'maskB': b, 'sideB': sb})
    return {'status': 'PASS_NATIVE_PROFILES' if not missing_open and not mismatched else 'NATIVE_PROFILE_REVIEW_REQUIRED',
            'connectedOpposedPairs': pairs, 'expectedPairs': 128, 'missingOpenBoundary': missing_open,
            'differentOpposedBoundaryProfiles': mismatched, 'closedSideVisibleBoundary': closed_visible,
            'note': 'Exact RGBA boundary matching. Closed-side alpha can be a legal upper overhang and is reported rather than automatically failed.'}

def baseline():
    base = WORK / 'reference/ANIMOL_ECHO_THEATER_CARTOON_V03'
    if not base.exists(): return {'status': 'NOT_READY'}
    props = read(base / 'props_manifest.json')['assets']
    terrain = read(base / 'theater_terrain_manifest.json')
    bridge = read(base / 'bridge_decor_manifest.json')
    out = {'status': 'READ_OFFLINE', 'propsManifestSha256': sha(base / 'props_manifest.json'),
           'propAssetCount': len(props), 'multiCellPropAssetCount': sum(a.get('footprintCells', [1, 1]) != [1, 1] for a in props),
           'stateVariantWarning': 'footlights_on/dim/off are three state assets of one semantic prop, not three decoration kinds.',
           'props': [{'id': a['id'], 'file': a['file'], 'footprintCells': a.get('footprintCells'), 'sha256': a['sha256']} for a in props],
           'materialCodes': terrain['materialCodes'], 'existingBridgeDecorationRules': bridge['rules'],
           'notes': ['Old V03 already has six multi-cell prop assets; do not claim all old art was one tile.',
                     'New add-on is decorative. Actual host generation fit-to-cell/pivot/adjacency problems need project inspection.',
                     'Existing bridge rail/post selection owns catwalk boundaries; do not duplicate new decoration rails on its placements.',
                     'Theater S is velvet_carpet, not Ranch stone_bridge. Preserve S-to-G support normalization from V03.']}
    (HERE / 'baseline_decor_inventory.json').write_text(json.dumps(out, ensure_ascii=False, indent=2) + '\n')
    return out

def main():
    reports = {owner: audit_owner(owner) for owner in EXPECTED}
    for report in reports.values():
        if '_assets' in report:
            report['joinChecks'] = {f: overlap_checks(report, f) for f in NETS if f in report['semanticFamilies']}
            report['nativeBoundaryChecks'] = {f: boundary_profile_checks(report, f) for f in NETS if f in report['semanticFamilies']}
    public = {k: {a: b for a, b in v.items() if not a.startswith('_')} for k, v in reports.items()}
    families = set().union(*(set(r.get('semanticFamilies', [])) for r in reports.values()))
    union = {'networks': sorted(families & set(NETS)), 'wholeGroups': sorted(families & set(GROUPS)),
             'points': sorted(families & set(POINTS)), 'semanticCount': len(families & (set(NETS) | set(GROUPS) | set(POINTS))),
             'expectedSemanticCount': 26, 'maskVariantsCountAsNewKinds': False}
    all_metadata = all(r['status'] == 'PASS_OFFLINE' for r in reports.values()) and union['semanticCount'] == 26
    join_rows = [c for r in reports.values() for c in r.get('joinChecks', {}).values()]
    all_joins = len(join_rows) == 6 and all(c['status'] == 'PASS_SAFE_OVERLAP' for c in join_rows)
    status = 'PASS_OFFLINE' if all_metadata and all_joins else 'PENDING_OR_FAILED'
    result = {'status': status, 'scope': 'Independent producer file/metadata/alpha/semantic-family/native-source and padded-overlap audit. Source review and visual review separate.',
              'owners': public, 'familyInventory': union, 'metadataComplete': all_metadata, 'safeOverlapComplete': all_joins,
              'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN', 'actualNUnit': 'NOT_RUN',
              'actualPrefabBindings': 'NOT_RUN', 'actualPlayMode': 'NOT_RUN'}
    (HERE / 'independent_exports_qa.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    baseline()
    print(json.dumps({'status': status, 'semanticCount': union['semanticCount'],
                      'owners': {k: {'status': v['status'], 'errors': len(v['errors'])} for k, v in reports.items()}}, ensure_ascii=False))

if __name__ == '__main__': main()
