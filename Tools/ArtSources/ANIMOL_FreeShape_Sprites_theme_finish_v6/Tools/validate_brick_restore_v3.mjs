/** Brick-pattern survival and bounded-change regression. Reads art; never edits PNGs. */
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2), argument = (flag, fallback) => args.includes(flag) ? args[args.indexOf(flag) + 1] : fallback;
const BASELINE = path.resolve(ROOT, argument('--baseline-v2', '../ANIMOL_FreeShape_Sprites_clean_v2'));
const TARGET = 'T01_A';
const MORTAR = new Set(argument('--mortar-colors', '#1a1c2c').split(',').map(hex => parseInt(hex.replace('#', ''), 16)));
const THRESHOLDS = Object.freeze({ minimumSubstantialColors: 3, minimumColorFraction: .01, maximumDominantFraction: .85,
  minimumHorizontalMortarRuns: 2, horizontalRunLength: 12, minimumVerticalMortarRuns: 4, verticalRunLength: 6 });
const local = filename => path.relative(ROOT, filename).split(path.sep).join('/');
const json = async (root, filename) => JSON.parse(await fs.readFile(path.join(root, filename), 'utf8'));
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const started = performance.now(), lookup = await json(ROOT, 'Data/sprite_lookup.json');
assert.equal(lookup.artVersion, 3, 'Run this audit only after the artVersion3 package is built.');
assert.equal(lookup.contractId, 'ANIMOL_FREE_SHAPE_BLOB47_V1');
assert.equal(lookup.styles.length, 20); assert.equal(lookup.cellPixels, 32); assert.equal(lookup.pixelsPerUnit, 32);
const targetStyle = lookup.styles.find(style => style.styleId === TARGET); assert(targetStyle);
async function pngFiles(directory) {
  const files = [];
  for (const item of await fs.readdir(directory, { withFileTypes: true })) {
    const filename = path.join(directory, item.name);
    if (item.isDirectory()) files.push(...await pngFiles(filename));
    else if (/\.png$/iu.test(item.name)) files.push(filename);
  }
  return files;
}
const regression = { result: 'PASS', baselineLabel: 'unchanged final clean_v2 package', styles: [], filesCompared: 0, bytesCompared: 0, combinedBaselineDigest: null, combinedCurrentDigest: null };
const oldDigest = crypto.createHash('sha256'), newDigest = crypto.createHash('sha256');
async function styleFiles(root, styleId) {
  return [...await pngFiles(path.join(root, 'Art', styleId)), ...await pngFiles(path.join(root, 'Samples', styleId)),
    ...(await fs.readdir(path.join(root, 'Art/Atlases'))).filter(filename => filename.startsWith(`${styleId}_`) && filename.endsWith('.png')).map(filename => path.join(root, 'Art/Atlases', filename))];
}
for (const style of lookup.styles.filter(style => style.styleId !== TARGET)) {
  const files = await styleFiles(BASELINE, style.styleId), currentFiles = await styleFiles(ROOT, style.styleId);
  assert.deepEqual(currentFiles.map(filename => path.relative(ROOT, filename)).sort(), files.map(filename => path.relative(BASELINE, filename)).sort(), `Non-target style PNG filename set changed: ${style.styleId}`);
  const stats = { styleId: style.styleId, filesCompared: files.length, bytesCompared: 0 };
  for (const baselineFile of files.sort()) {
    const relative = path.relative(BASELINE, baselineFile).split(path.sep).join('/');
    const before = await fs.readFile(baselineFile), after = await fs.readFile(path.join(ROOT, relative));
    assert(before.equals(after), `Non-target style changed: ${relative}`);
    oldDigest.update(`${relative}\n`); oldDigest.update(before); newDigest.update(`${relative}\n`); newDigest.update(after);
    regression.filesCompared++; regression.bytesCompared += before.length; stats.bytesCompared += before.length;
  }
  regression.styles.push(stats);
}
assert.equal(regression.styles.length, 19); assert.equal(regression.filesCompared, 5605, 'Unexpected v2 style PNG file coverage.');
regression.combinedBaselineDigest = oldDigest.digest('hex'); regression.combinedCurrentDigest = newDigest.digest('hex');
async function readImage(filename) {
  const bytes = await fs.readFile(path.join(ROOT, filename));
  const { data, info } = await sharp(bytes).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { bytes, data, width: info.width, height: info.height };
}
const field = await readImage(targetStyle.materialSampling.repeatedField);
assert.equal(field.width, 128); assert.equal(field.height, 128);
const period = targetStyle.materialSampling.periodPixels; assert.equal(period, 64);
const colors = new Map(), mortar = new Uint8Array(period * period), palette = new Set(lookup.palette.map(hex => parseInt(hex.slice(1), 16)));
for (let y = 0; y < period; y++) for (let x = 0; x < period; x++) {
  const index = ((y + 32) * field.width + x + 32) * 4;
  assert.equal(field.data[index + 3], 255);
  const color = (field.data[index] << 16) | (field.data[index + 1] << 8) | field.data[index + 2];
  assert(palette.has(color)); colors.set(color, (colors.get(color) ?? 0) + 1); mortar[y * period + x] = MORTAR.has(color) ? 1 : 0;
}
const distribution = [...colors].map(([color, pixels]) => ({ color: `#${color.toString(16).padStart(6, '0')}`, pixels, fraction: pixels / (period * period) })).sort((a, b) => b.pixels - a.pixels);
function lineRuns(vertical, minimumLength) {
  const lines = [];
  for (let fixed = 0; fixed < period; fixed++) {
    let start = null;
    for (let moving = 0; moving <= period; moving++) {
      const filled = moving < period && mortar[(vertical ? moving : fixed) * period + (vertical ? fixed : moving)];
      if (filled && start === null) start = moving;
      if (!filled && start !== null) { if (moving - start >= minimumLength) lines.push({ fixed, start, length: moving - start }); start = null; }
    }
  }
  return lines;
}
const horizontal = lineRuns(false, THRESHOLDS.horizontalRunLength), vertical = lineRuns(true, THRESHOLDS.verticalRunLength);
const brick = { result: 'PASS', input: targetStyle.materialSampling.repeatedField, inputSha256: sha(field.bytes), analyzedCorePixels: [32, 32, 64, 64],
  colorDistribution: distribution, mortarColors: [...MORTAR].map(color => `#${color.toString(16).padStart(6, '0')}`), thresholds: THRESHOLDS,
  substantialColorCount: distribution.filter(color => color.fraction >= THRESHOLDS.minimumColorFraction).length,
  dominantColorFraction: distribution[0].fraction, longHorizontalMortarRuns: horizontal.length, longVerticalMortarRuns: vertical.length,
  horizontalExamples: horizontal.slice(0, 12), verticalExamples: vertical.slice(0, 12),
  scope: 'These minimal native64px field checks reject a flat fill and demonstrate surviving horizontal+vertical structural mortar. They do not rate artistic quality, curves, endcaps or every whole-map composition.' };
assert(brick.substantialColorCount >= THRESHOLDS.minimumSubstantialColors, 'Brick field lost its meaningful material/mortar/shadow color populations.');
assert(brick.dominantColorFraction <= THRESHOLDS.maximumDominantFraction, 'Brick field collapsed toward a flat dominant fill.');
assert(horizontal.length >= THRESHOLDS.minimumHorizontalMortarRuns, 'Brick field lost long horizontal mortar lines.');
assert(vertical.length >= THRESHOLDS.minimumVerticalMortarRuns, 'Brick field lost vertical mortar joints.');
const contour = { result: 'PASS', inkColor: '#1a1c2c', registeredContourPalettes: targetStyle.decorativeSampling.registeredContourPalettes,
  exportedSprites: 0, exposedCellSides: 0, exposedBoundaryPixels: 0, fixtureBoundaries: [], singleSpriteCenterComparisons: 0,
  singleCenterPixelsCompared: 0, scope: 'Exact ink color on every cardinally exposed outer boundary pixel, plus single-tile body-center preservation. This does not imply that all adjacent interior structural ink is one pixel thick or that all curves are jaggies-free.' };
assert.deepEqual(contour.registeredContourPalettes, { N: ['#1a1c2c'], S: ['#1a1c2c'], W: ['#1a1c2c'], E: ['#1a1c2c'] }, 'The four registered contour roles must all explicitly use ink.');
function requireInk(image, x, y, label) {
  const offset = (y * image.width + x) * 4;
  assert.equal(image.data[offset], 0x1a, label); assert.equal(image.data[offset + 1], 0x1c, label);
  assert.equal(image.data[offset + 2], 0x2c, label); assert.equal(image.data[offset + 3], 255, label);
}
const cardinalSides = [{ name: 'N', bit: 1 }, { name: 'S', bit: 16 }, { name: 'W', bit: 64 }, { name: 'E', bit: 4 }];
for (const variant of targetStyle.variants) {
  const bodyCell = await readImage(variant.cells.find(cell => cell.mask === 255).file);
  for (const cell of variant.cells) {
    const image = await readImage(cell.file); contour.exportedSprites++;
    for (const side of cardinalSides) {
      if (cell.mask & side.bit) continue;
      contour.exposedCellSides++;
      for (let along = 0; along < 32; along++) {
        const x = side.name === 'W' ? 0 : side.name === 'E' ? 31 : along;
        const y = side.name === 'N' ? 0 : side.name === 'S' ? 31 : along;
        requireInk(image, x, y, `Exposed contour lost ink: ${cell.file}/${side.name} at ${x},${y}`); contour.exposedBoundaryPixels++;
      }
    }
    if (cell.mask === 0) {
      for (let y = 8; y < 24; y++) for (let x = 8; x < 24; x++) {
        const offset = (y * 32 + x) * 4;
        assert(image.data.subarray(offset, offset + 4).equals(bodyCell.data.subarray(offset, offset + 4)), `Single tile body center overwritten: ${cell.file} at ${x},${y}`);
        contour.singleCenterPixelsCompared++;
      }
      contour.singleSpriteCenterComparisons++;
    }
  }
}
assert.equal(contour.exportedSprites, 188); assert.equal(contour.singleSpriteCenterComparisons, 4);
for (const [fixture, extent] of [['single_1x1', 32], ['maximum_rectangle_16x16', 512]]) {
  const filename = `Samples/${TARGET}/${fixture}.png`, image = await readImage(filename); assert.equal(image.width, extent); assert.equal(image.height, extent);
  const counts = { N: 0, S: 0, W: 0, E: 0 };
  for (let along = 0; along < extent; along++) {
    for (const [side, x, y] of [['N', along, 0], ['S', along, extent - 1], ['W', 0, along], ['E', extent - 1, along]]) {
      requireInk(image, x, y, `Fixture contour lost ink: ${filename}/${side} at ${x},${y}`); counts[side]++;
    }
  }
  contour.fixtureBoundaries.push({ file: filename, pixelsPerSide: counts, result: 'PASS' });
}
// Reproduce reference panels from the declared raw crop and nearest fit.
// Include the motif when no separate matte-removal stage is requested.
// Sampling overlays are separately checked by validate_clean_pixel_art.
const manifest = await json(ROOT, lookup.sourceManifest), theme = manifest.themes.find(theme => theme.themeId === targetStyle.themeId);
const definition = theme.styles.find(style => style.styleId === TARGET || `${theme.themeId}_${style.styleId}` === TARGET); assert(definition);
const sourcePath = targetStyle.sourceOverride ?? definition.sourcePath ?? theme.sourcePath;
assert(!path.isAbsolute(sourcePath) && path.resolve(ROOT, sourcePath).startsWith(`${ROOT}${path.sep}`), 'Raw-source reference must be portable and inside this package.');
const source = await readImage(sourcePath), allowed = targetStyle.allowedPalette.map(hex => [parseInt(hex.slice(1, 3), 16), parseInt(hex.slice(3, 5), 16), parseInt(hex.slice(5, 7), 16)]);
const sourceCrop = { result: 'PASS', source: sourcePath, sourceSha256: sha(source.bytes), sourceDimensions: [source.width, source.height], panels: [],
  motifScope: definition.motifMatte === false ? 'Exact source-crop reproduction included; no matte removal requested.' : 'Not reproduced here: motif matte processing has separate semantics.' };
for (const role of ['material', 'frame', 'ring', ...(definition.motifMatte === false ? ['motif'] : [])]) {
  const rect = definition[role], fit = targetStyle.sourceTransforms[role]; assert(rect && fit);
  const left = Math.round(rect.x), top = Math.round(rect.y), width = Math.round(rect.w ?? rect.width), height = Math.round(rect.h ?? rect.height);
  assert(left >= 0 && top >= 0 && width > 0 && height > 0 && left + width <= source.width && top + height <= source.height, `Raw crop outside source: ${role}`);
  assert.equal(fit.resamplingKernel, 'nearest', `Unexpected filtering on ${role}`);
  const expected = await sharp(source.data, { raw: { width: source.width, height: source.height, channels: 4 } }).extract({ left, top, width, height }).resize(128, 128, { fit: fit.mode, kernel: 'nearest', background: { r: 0, g: 0, b: 0, alpha: 0 } }).raw().toBuffer();
  for (let index = 0; index < expected.length; index += 4) {
    if (expected[index + 3] < (manifest.alphaThreshold ?? 128)) { expected.fill(0, index, index + 4); continue; }
    let nearest = null, distance = Infinity;
    for (const color of allowed) {
      const error = (expected[index] - color[0]) ** 2 + (expected[index + 1] - color[1]) ** 2 + (expected[index + 2] - color[2]) ** 2;
      if (error < distance) { nearest = color; distance = error; }
    }
    expected[index] = nearest[0]; expected[index + 1] = nearest[1]; expected[index + 2] = nearest[2]; expected[index + 3] = 255;
  }
  const actual = await readImage(targetStyle.panels[role]); assert.equal(actual.width, 128); assert.equal(actual.height, 128);
  assert(expected.equals(actual.data), `Normalized ${role} reference differs from declared source crop, nearest fit and role palette.`);
  sourceCrop.panels.push({ role, output: targetStyle.panels[role], rect: [left, top, width, height], mode: fit.mode, bytesCompared: expected.length, outputSha256: sha(actual.bytes) });
}
const clean = await json(ROOT, 'Docs/clean_pixel_validation.json');
assert.equal(clean.artVersion, 3, 'Run fresh v3 clean QA before this regression.'); assert.equal(clean.coreChecksStatus, 'PASS');
assert.equal(clean.current.totals.noise.sprites, 3760); assert.equal(clean.samplingChecks.status, 'PASS');
const freshPixelDigest = crypto.createHash('sha256');
for (const style of lookup.styles) for (const variant of style.variants) for (const cell of variant.cells) {
  freshPixelDigest.update(`${cell.file}\n`); freshPixelDigest.update(await fs.readFile(path.join(ROOT, cell.file)));
}
assert.equal(freshPixelDigest.digest('hex'), clean.current.pixelPngDigestSha256, 'Clean candidate statistics are stale relative to the final exported PNGs.');
const reports = ['Docs/topology_validation.json', 'Docs/sprite_file_validation.json', 'Docs/gallery_validation.json'], coreReports = [];
for (const filename of reports) {
  const report = await json(ROOT, filename); assert(report.result === 'PASS' || report.status === 'PASS', filename);
  coreReports.push({ file: filename, result: report.result ?? report.status });
}
const result = { schemaVersion: 1, artVersion: 3, contractId: lookup.contractId, generatedAt: new Date().toISOString(), result: 'PASS',
  preservedNonTargetStylePngs: regression, targetBrickPattern: brick, targetRegisteredContours: contour, declaredSourceCropReproduction: sourceCrop,
  currentFullArtAudit: { file: 'Docs/clean_pixel_validation.json', artVersion: clean.artVersion, pixelPngDigestSha256: clean.current.pixelPngDigestSha256, coreChecksStatus: clean.coreChecksStatus,
    pngs: clean.current.totals.noise.sprites, isolatedPixelCandidates: clean.current.totals.noise.isolatedPixelCandidates, samplingPngs: clean.samplingChecks.pngs },
  coreReports, durationMs: Math.round(performance.now() - started),
  limitations: ['No complete semantic jaggies/doubles or endcap quality verdict is inferred from line/color counts.',
    'Tiny intentional details and colored structural corners may remain in noise or double-pixel candidate counts.',
    'No Unity import, renderer, collider, editor transaction or mobile-device verification was run here.',
    'Visual approval of every atlas, every endshape or every global terrain composition is not claimed.'] };
await fs.writeFile(path.join(ROOT, 'Docs/brick_restore_validation.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify({ result: result.result, preservedOtherStylePngs: regression.filesCompared, targetBrick: { colors: distribution.length,
  substantialColors: brick.substantialColorCount, dominantFraction: brick.dominantColorFraction, horizontalMortarRuns: horizontal.length, verticalMortarRuns: vertical.length },
  contourBoundaryPixels: contour.exposedBoundaryPixels, contourFixtures: contour.fixtureBoundaries, singleCenterPixelsCompared: contour.singleCenterPixelsCompared,
  sourceCropPanelsReproduced: sourceCrop.panels.length, artVersion: result.artVersion, finalPngDigest: clean.current.pixelPngDigestSha256, durationMs: result.durationMs }, null, 2));
