"""Register six new native imagegen whole props; no procedural RGB artwork."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import label, binary_dilation
import numpy as np
import json
import hashlib

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = 'ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01'
SPECS = [
    ('harbor_market_stall_3x2', 3, 2, 0),
    ('fisher_workbench_3x1', 3, 1, 64),
    ('driftwood_bench_3x1', 3, 1, 0),
    ('net_drying_rack_3x1', 3, 1, 128),
    ('cargo_pallet_2x2', 2, 2, 0),
    ('shell_display_2x1', 2, 1, 64),
]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def font(size):
    return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', size)

assets, groups, native_sources, checks, proofs = [], [], [], [], []
for key, columns, rows, overhang in SPECS:
    source = f'sources/{key}_native.png'
    source_path = ROOT / source
    im = Image.open(source_path).convert('RGBA')
    original = np.asarray(im)
    rgba = original.copy()
    alpha = rgba[:, :, 3].copy()
    # Alpha-only technical speck cleanup: connected native component RGB is unchanged.
    lab, _ = label(alpha > 180, structure=np.ones((3, 3), dtype='uint8'))
    sizes = np.bincount(lab.ravel())
    retained = np.flatnonzero(sizes >= 16)
    retained = retained[retained != 0]
    body = np.isin(lab, retained)
    allowed = binary_dilation(body, iterations=1)
    alpha[~allowed] = 0
    rgba[:, :, 3] = alpha
    assert np.array_equal(rgba[:, :, :3], original[:, :, :3])
    clean = Image.fromarray(rgba, 'RGBA')
    bounds = clean.getbbox()
    assert bounds is not None
    cropped = clean.crop(bounds)
    ground_width, ground_height = columns * 256, rows * 256
    canvas_width, canvas_height = ground_width + 64, ground_height + 64 + overhang
    logical = [32, 32 + overhang, ground_width, ground_height]
    scale = min(ground_width * .97 / cropped.width,
                (ground_height + overhang - 16) / cropped.height)
    target_size = (round(cropped.width * scale), round(cropped.height * scale))
    art = cropped.resize(target_size, Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (canvas_width, canvas_height), (0, 0, 0, 0))
    left = logical[0] + (ground_width - art.width) // 2
    bottom = logical[1] + ground_height - 8
    top = bottom - art.height
    assert min(left, top) >= 0
    canvas.alpha_composite(art, (left, top))
    file = f'decor/groups/{key}.png'
    output = ROOT / file
    canvas.save(output)
    visible = Image.fromarray((np.asarray(canvas)[:, :, 3] >= 128).astype('uint8') * 255).getbbox()
    fill = (visible[2] - visible[0]) / ground_width
    assert fill >= .90, (key, fill)
    ground_alpha = np.asarray(canvas)[logical[1]:logical[1]+ground_height,logical[0]:logical[0]+ground_width,3] >= 128
    yy, xx = np.where(ground_alpha)
    ground_extent_width = (int(xx.max())-int(xx.min())+1) / ground_width
    ground_extent_height = (int(yy.max())-int(yy.min())+1) / ground_height
    row_densities = [float(ground_alpha[row*256:(row+1)*256].mean()) for row in range(rows)]
    assert ground_extent_width >= .90, (key, 'groundwidth', ground_extent_width)
    if rows > 1:
        assert ground_extent_height >= .85, (key, 'groundheight', ground_extent_height)
        assert min(row_densities) >= .35, (key, 'rowdensity', row_densities)
    pivot = [(logical[0] + ground_width / 2) / canvas_width,
             1 - (logical[1] + ground_height / 2) / canvas_height]
    asset = {
        'id': key, 'key': key, 'family': key, 'kind': 'group', 'file': file,
        'width': canvas_width, 'height': canvas_height,
        'ppu': 256, 'pixelsPerUnit': 256, 'pivot': pivot,
        'logicalRect': logical, 'footprintCells': [columns, rows],
        'sha256': sha(output), 'sourceFiles': [source],
        'sourceSha256': sha(source_path),
        'nativeWholeGroup': True, 'oneRendererPerPlacement': True,
        'topOverhangPixels': overhang, 'visualAlphaBounds': list(visible),
        'horizontalGroundFill': round(fill, 6),
        'groundAlphaExtentWidth': ground_extent_width, 'groundAlphaExtentHeight': ground_extent_height,
        'groundRowOpaqueDensity': row_densities,
        'actualGameplayFootprintVerified': False, 'deriveColliderFromAlpha': False,
        'generationTool': 'built-in image_gen',
        'registration': {
            'nativeCropLTRB': list(bounds), 'nativeCropSize': list(cropped.size),
            'uniformScale': scale, 'resizedSize': list(target_size),
            'pasteTopLeft': [left, top], 'contactBottomPixel': bottom,
            'alphaOnlyTechnicalCleanup': True,
            'alphaCleanupPolicy':'Keep native alpha only within1px dilation of connected native opaque cores alpha>180 of area>=16; remove faint external halos/specks; no RGB modification.', 'rgbRepaint': False,
            'sourceRGBUnchangedBeforeUniformResize': True,
        },
    }
    assets.append(asset)
    groups.append({'key': key, 'file': file, 'footprintCells': [columns, rows]})
    native_sources.append({'file': source, 'sha256': sha(source_path),
                           'generatedBy': 'built-in image_gen', 'runtimeSelectable': False})
    checks.append({'key': key, 'decoded': True, 'widthFill': round(fill, 6),
                   'sourceSha256': sha(source_path), 'runtimeSha256': sha(output),
                   'exactPivotFromGroundCenter': True,
                   'nativeAlphaZeroPixels': int((original[:, :, 3] == 0).sum()),
                   'wholeSpriteOneRenderer': True,
                   'registrationRGBRepaint': False, 'groundAlphaExtentHeight': ground_extent_height, 'groundRowOpaqueDensity': row_densities})
    proof = Image.new('RGBA', canvas.size, (12, 26, 37, 255))
    draw = ImageDraw.Draw(proof)
    for y in range(rows):
        for x in range(columns):
            x0, y0 = logical[0] + x * 256, logical[1] + y * 256
            draw.rectangle((x0, y0, x0 + 256, y0 + 256),
                           fill=(57, 78, 82, 255) if (x + y) % 2 else (42, 63, 70, 255))
    proof.alpha_composite(canvas)
    draw = ImageDraw.Draw(proof)
    for x in range(columns + 1):
        xx = logical[0] + x * 256
        draw.line((xx, logical[1], xx, logical[1] + ground_height), fill=(98, 216, 207, 220), width=2)
    for y in range(rows + 1):
        yy = logical[1] + y * 256
        draw.line((logical[0], yy, logical[0] + ground_width, yy), fill=(98, 216, 207, 220), width=2)
    proof.save(ROOT / f'Inspection/ground_{key}.png')
    proofs.append(proof)

for draft_key in ['harbor_market_stall_3x2','fisher_workbench_3x1']:
    draft = ROOT / ('sources/'+draft_key+'_draft_native.png')
    native_sources.append({'file':str(draft.relative_to(ROOT)), 'sha256':sha(draft),
        'generatedBy':'built-in image_gen','runtimeSelectable':False,
        'selectionStatus':'SUPERSEDED_DRAFT','reason':'Replaced by native redraw removing busy rounded fish/spiral details or circular rope coil; not used by runtime.'})

manifest = {
    'schemaVersion': 1, 'packageId': PACKAGE,
    'themeDisplayName': '별너울 해안', 'owner': 'groups_a',
    'coordinateConvention': 'Logical rectangles top-left; Unity normalized pivots bottom-left; world +Y North.',
    'assets': assets, 'groups': groups, 'sources': native_sources,
    'generationPromptFiles': ['Authoring/generation_prompts.json', 'Authoring/angular_revision_prompts.json'],
    'sourcePolicy': 'Six whole new native imagegen compositions, mechanical crop/alpha speck cleanup/uniform resample/registration only; no painted RGB or old art reuse.',
    'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN',
    'actualPlayMode': 'NOT_RUN', 'actualHostDecorBinding': 'NOT_RUN',
}
write(ROOT / 'manifest.json', manifest)

contact = Image.new('RGB', (1800, 1900), (12, 26, 37))
draw = ImageDraw.Draw(contact)
draw.text((28, 20), 'STAR SWELL COAST / WHOLE MULTI-CELL GROUPS A', font=font(33), fill=(255, 226, 160))
draw.text((28, 68), '6 different props / native bitmap art / PPU256 / actual Unity NOT_RUN', font=font(21), fill=(106, 220, 207))
for index, (asset, proof) in enumerate(zip(assets, proofs)):
    x, y = 24 + index % 2 * 890, 120 + index // 2 * 590
    draw.rectangle((x, y, x + 864, y + 568), outline=(80, 99, 111), width=2)
    draw.text((x + 16, y + 14), asset['key'], font=font(25), fill=(255, 226, 160))
    w, h = asset['footprintCells']
    draw.text((x + 16, y + 52), f'{w} x {h} floor cells / ONE renderer / {asset["horizontalGroundFill"]:.1%} width',
              font=font(19), fill=(106, 220, 207))
    ratio = min(808 / proof.width, 444 / proof.height)
    small = proof.resize((round(proof.width * ratio), round(proof.height * ratio)), Image.Resampling.LANCZOS)
    contact.paste(small.convert('RGB'), (x + 24 + (808 - small.width) // 2, y + 92 + (444 - small.height) // 2))
    draw.text((x + 16, y + 538), 'Grid is review-only; runtime PNG has a transparent background.', font=font(16), fill=(182, 171, 195))
contact.save(ROOT / 'Inspection/groups_a_contact_sheet.png')

qa = {'status': 'PASS_OFFLINE', 'assets': len(assets), 'nativeFinalSources': len(assets), 'nativeDraftSources':2,
      'checks': checks, 'allWidthFillAtLeast90Percent': True,
      'nativeUniqueSourceHashes': len({s['sha256'] for s in native_sources}) == 8,
      'manifestSha256': sha(ROOT / 'manifest.json'),
      'actualUnityImport': 'NOT_RUN', 'actualCSharpCompile': 'NOT_RUN',
      'actualPlayMode': 'NOT_RUN', 'actualHostDecorBinding': 'NOT_RUN'}
write(ROOT / 'QA/groups_a_qa.json', qa)
print(json.dumps({'status': qa['status'], 'assets': len(assets), 'manifestSha256': qa['manifestSha256'],
                  'fills': {a['key']: a['horizontalGroundFill'] for a in assets}}, indent=2))
