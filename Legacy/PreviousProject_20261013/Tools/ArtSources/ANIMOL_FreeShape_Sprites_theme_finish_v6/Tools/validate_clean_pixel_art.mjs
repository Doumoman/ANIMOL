/**
 * Read-only pixel-quality candidate audit. This does not repair PNGs and does not
 * classify every pixel-art jaggy/double semantically. Use actual atlas sheets
 * and composed fixtures for visual review after the measurable checks pass.
 *
 * node Tools/validate_clean_pixel_art.mjs
 * node Tools/validate_clean_pixel_art.mjs --check-core --contact-sheets
 * node Tools/validate_clean_pixel_art.mjs --baseline ../ANIMOL_FreeShape_Sprites_v1
 */
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';
import { CANONICAL_MASKS, canonicalize, neighborMask, cellKey } from './terrain_topology.mjs';
import { resolveVariant } from './terrain_composition.mjs';

const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2), baselineIndex = args.indexOf('--baseline');
const BASELINE = path.resolve(ROOT, baselineIndex >= 0 ? args[baselineIndex + 1] : '../ANIMOL_FreeShape_Sprites_v1');
const RUN_CORE = args.includes('--check-core'), MAKE_SHEETS = args.includes('--contact-sheets');
const INK = 0x1a1c2c, CELL = 32, EXAMPLES = 8;
const DIRECTIONS = [{ name: 'N', bit: 1 }, { name: 'E', bit: 4 }, { name: 'S', bit: 16 }, { name: 'W', bit: 64 }];
const local = p => path.relative(ROOT, p).split(path.sep).join('/');
const rgbHex = color => `#${color.toString(16).padStart(6, '0')}`;
const addExample = (items, item) => { if (items.length < EXAMPLES) items.push(item); };
const json = async (root, name) => JSON.parse(await fs.readFile(path.join(root, name), 'utf8'));
const isInk = (pixels, x, y) => pixels[y * CELL + x] === INK;
function emptyNoiseSummary() {
  return { sprites: 0, filesWithIsolatedPixelCandidates: 0, isolatedPixelCandidates: 0,
    filesWithSmallClusterCandidates: 0, smallClusterCandidates: 0, smallClusterPixels: 0,
    clusterAreas: { '1': 0, '2': 0, '3': 0 }, colors: {}, borderClippedSmallClustersExcluded: 0 };
}
function emptyBorderSummary() {
  return { sprites: 0, exposedSides: 0, straightBoundarySamples: 0, exactOnePixelInkSamples: 0,
    missingBoundaryInkSamples: 0, thickerThanOnePixelInkSamples: 0,
    inkThicknessHistogram: {}, perimeterInk2x2Candidates: 0 };
}
function noiseCandidates(pixels, width = CELL, height = CELL) {
  const visited = new Uint8Array(width * height), summary = emptyNoiseSummary(), examples = [];
  for (let index = 0; index < pixels.length; index++) {
    if (visited[index]) continue;
    if (pixels[index] === 0xffffffff) { visited[index] = 1; continue; }
    const color = pixels[index], queue = [index]; visited[index] = 1;
    let touchesBorder = false;
    for (let cursor = 0; cursor < queue.length; cursor++) {
      const item = queue[cursor], x = item % width, y = Math.floor(item / width);
      if (x === 0 || x === width - 1 || y === 0 || y === height - 1) touchesBorder = true;
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
        if (!dx && !dy) continue;
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
        const ni = ny * width + nx;
        if (!visited[ni] && pixels[ni] === color) { visited[ni] = 1; queue.push(ni); }
      }
    }
    if (queue.length > 3) continue;
    if (touchesBorder) { summary.borderClippedSmallClustersExcluded++; continue; }
    summary.smallClusterCandidates++; summary.smallClusterPixels += queue.length;
    summary.clusterAreas[queue.length]++;
    if (queue.length === 1) summary.isolatedPixelCandidates++;
    const hex = rgbHex(color); summary.colors[hex] = (summary.colors[hex] ?? 0) + 1;
    addExample(examples, { color: hex, area: queue.length, pixels: queue.map(item => [item % width, Math.floor(item / width)]) });
  }
  return { summary, examples };
}
function boundaryCoordinate(direction, along, depth = 0) {
  return direction === 'N' ? [along, depth] : direction === 'S' ? [along, 31 - depth]
    : direction === 'W' ? [depth, along] : [31 - depth, along];
}
function borderCandidates(pixels, mask) {
  const summary = emptyBorderSummary(), examples = [], sides = [];
  for (const direction of DIRECTIONS) {
    if (mask & direction.bit) continue;
    summary.exposedSides++;
    const side = { direction: direction.name, samples: 16, exactOnePixelInk: 0, missingBoundaryInk: 0, thickerInk: 0 };
    // Exclude eight pixels at each corner. Corners/caps are not straight-run
    // thickness references, and a thick structural frame is not a double.
    for (let along = 8; along < 24; along++) {
      let thickness = 0;
      for (let depth = 0; depth < 8; depth++) {
        const [x, y] = boundaryCoordinate(direction.name, along, depth);
        if (!isInk(pixels, x, y)) break;
        thickness++;
      }
      summary.straightBoundarySamples++;
      summary.inkThicknessHistogram[thickness] = (summary.inkThicknessHistogram[thickness] ?? 0) + 1;
      if (thickness === 1) { summary.exactOnePixelInkSamples++; side.exactOnePixelInk++; }
      else if (thickness === 0) { summary.missingBoundaryInkSamples++; side.missingBoundaryInk++; }
      else { summary.thickerThanOnePixelInkSamples++; side.thickerInk++; }
      if (thickness !== 1) addExample(examples, { direction: direction.name, along, boundary: boundaryCoordinate(direction.name, along), consecutiveInkDepth: thickness });
    }
    sides.push(side);
  }
  for (let y = 0; y < 31; y++) for (let x = 0; x < 31; x++) {
    const nearExposedPerimeter = (!(mask & 1) && y < 4) || (!(mask & 16) && y > 26)
      || (!(mask & 64) && x < 4) || (!(mask & 4) && x > 26);
    if (!nearExposedPerimeter) continue;
    if (isInk(pixels, x, y) && isInk(pixels, x + 1, y) && isInk(pixels, x, y + 1) && isInk(pixels, x + 1, y + 1)) {
      summary.perimeterInk2x2Candidates++;
      addExample(examples, { kind: 'perimeter ink 2x2 candidate', x, y });
    }
  }
  return { summary, sides, examples };
}
function mergeNoise(target, source) {
  target.sprites++; target.filesWithIsolatedPixelCandidates += source.isolatedPixelCandidates > 0;
  target.filesWithSmallClusterCandidates += source.smallClusterCandidates > 0;
  for (const field of ['isolatedPixelCandidates', 'smallClusterCandidates', 'smallClusterPixels', 'borderClippedSmallClustersExcluded']) target[field] += source[field];
  for (const area of ['1', '2', '3']) target.clusterAreas[area] += source.clusterAreas[area];
  for (const [color, count] of Object.entries(source.colors)) target.colors[color] = (target.colors[color] ?? 0) + count;
}
function mergeBorder(target, source) {
  target.sprites++;
  for (const field of ['exposedSides', 'straightBoundarySamples', 'exactOnePixelInkSamples', 'missingBoundaryInkSamples', 'thickerThanOnePixelInkSamples', 'perimeterInk2x2Candidates']) target[field] += source[field];
  for (const [depth, count] of Object.entries(source.inkThicknessHistogram)) target.inkThicknessHistogram[depth] = (target.inkThicknessHistogram[depth] ?? 0) + count;
}
async function scan(root, keepRecords) {
  const lookup = await json(root, 'Data/sprite_lookup.json'), digest = crypto.createHash('sha256');
  assert.equal(lookup.cellPixels, CELL); assert.equal(lookup.styles.length, 20);
  assert.deepEqual(lookup.canonicalMasks, CANONICAL_MASKS);
  const totals = { noise: emptyNoiseSummary(), border: emptyBorderSummary() }, styles = [], records = [], sprites = new Map();
  for (const style of lookup.styles) {
    assert.equal(style.variants.length, 4);
    const stats = { styleId: style.styleId, noise: emptyNoiseSummary(), border: emptyBorderSummary() };
    for (const variant of style.variants) {
      assert.equal(variant.cells.length, 47);
      for (const cell of variant.cells) {
        const bytes = await fs.readFile(path.join(root, cell.file));
        const { data, info } = await sharp(bytes).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
        assert.equal(info.width, CELL, cell.file); assert.equal(info.height, CELL, cell.file);
        const pixels = new Uint32Array(CELL * CELL);
        for (let i = 0; i < pixels.length; i++) {
          assert.equal(data[i * 4 + 3], 255, `Opaque logical cell expected: ${cell.file}`);
          pixels[i] = (data[i * 4] << 16) | (data[i * 4 + 1] << 8) | data[i * 4 + 2];
        }
        digest.update(`${cell.file}\n`); digest.update(bytes);
        const noise = noiseCandidates(pixels), border = borderCandidates(pixels, cell.mask);
        mergeNoise(totals.noise, noise.summary); mergeNoise(stats.noise, noise.summary);
        mergeBorder(totals.border, border.summary); mergeBorder(stats.border, border.summary);
        if (keepRecords) {
          sprites.set(`${style.styleId}|${variant.id}|${cell.mask}`, pixels);
          records.push({ file: cell.file, styleId: style.styleId, variant: variant.id, mask: cell.mask,
            isolatedPixelCandidates: noise.summary.isolatedPixelCandidates, smallClusterCandidates: noise.summary.smallClusterCandidates,
            smallClusterPixels: noise.summary.smallClusterPixels, noiseExamples: noise.examples,
            border: { sides: border.sides, perimeterInk2x2Candidates: border.summary.perimeterInk2x2Candidates, examples: border.examples } });
        }
      }
    }
    styles.push(stats);
  }
  assert.equal(totals.noise.sprites, 3760);
  return { lookup, sprites, report: { rootLabel: keepRecords ? 'current package' : 'V1 baseline', pixelPngDigestSha256: digest.digest('hex'), totals, styles, ...(keepRecords ? { records } : {}) } };
}
function adjacencyContexts() {
  const contexts = [];
  for (const orientation of ['horizontal', 'vertical']) {
    const w = orientation === 'horizontal' ? 4 : 3, h = orientation === 'horizontal' ? 3 : 4;
    const first = [1, 1], second = orientation === 'horizontal' ? [2, 1] : [1, 2], free = [];
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++)
      if (!(x === first[0] && y === first[1]) && !(x === second[0] && y === second[1])) free.push([x, y]);
    for (let mask = 0; mask < 1024; mask++) {
      const set = new Set([cellKey(...first), cellKey(...second)]);
      free.forEach(([x, y], bit) => { if (mask & (1 << bit)) set.add(cellKey(x, y)); });
      contexts.push({ orientation, first, second, firstMask: canonicalize(neighborMask(set, ...first)), secondMask: canonicalize(neighborMask(set, ...second)) });
    }
  }
  return contexts;
}
function seamCandidates(current) {
  const contexts = adjacencyContexts(), result = { contextsPerStyleAndPhase: contexts.length,
    logicalContextsVisited: 0, uniquePixelPairs: 0, sharedPixelSamples: 0,
    doubleInkRunsAtLeastTwoPixelsCandidates: 0, outerStraightLineJoinCandidates: 0, styles: [], examples: [] };
  for (const style of current.lookup.styles) {
    const seen = new Set(), stats = { styleId: style.styleId, uniquePixelPairs: 0, doubleInkRunCandidates: 0, outerLineJoinCandidates: 0 };
    for (let py = 0; py < 2; py++) for (let px = 0; px < 2; px++) for (const context of contexts) {
      result.logicalContextsVisited++;
      const firstVariant = `v${resolveVariant(px, py, 0)}`;
      const [dx, dy] = context.orientation === 'horizontal' ? [1, 0] : [0, 1];
      const secondVariant = `v${resolveVariant(px + dx, py + dy, 0)}`;
      const key = `${context.orientation}|${firstVariant}|${context.firstMask}|${secondVariant}|${context.secondMask}`;
      if (seen.has(key)) continue; seen.add(key);
      const a = current.sprites.get(`${style.styleId}|${firstVariant}|${context.firstMask}`), b = current.sprites.get(`${style.styleId}|${secondVariant}|${context.secondMask}`);
      assert(a && b, key); result.uniquePixelPairs++; stats.uniquePixelPairs++;
      let run = 0, longest = 0;
      for (let along = 0; along < CELL; along++) {
        const va = context.orientation === 'horizontal' ? a[along * CELL + 31] : a[along];
        const vb = context.orientation === 'horizontal' ? b[along * CELL] : b[31 * CELL + along];
        result.sharedPixelSamples++;
        // The four pixels nearest either exterior corner/cap can legitimately
        // continue an outside rail; they are not internal double-line samples.
        if (along >= 4 && along < 28 && va === INK && vb === INK) { run++; longest = Math.max(longest, run); }
        else run = 0;
      }
      if (longest >= 2) {
        result.doubleInkRunsAtLeastTwoPixelsCandidates++; stats.doubleInkRunCandidates++;
        addExample(result.examples, { styleId: style.styleId, pair: key, longestPairedInkRun: longest });
      }
      const sharedExposed = context.orientation === 'horizontal' ? [{ bit: 1, pos: 0, label: 'N' }, { bit: 16, pos: 31, label: 'S' }]
        : [{ bit: 64, pos: 0, label: 'W' }, { bit: 4, pos: 31, label: 'E' }];
      for (const side of sharedExposed) {
        if ((context.firstMask & side.bit) || (context.secondMask & side.bit)) continue;
        const va = context.orientation === 'horizontal' ? a[side.pos * CELL + 31] : a[side.pos];
        const vb = context.orientation === 'horizontal' ? b[side.pos * CELL] : b[31 * CELL + side.pos];
        if (va !== INK || vb !== INK) {
          result.outerStraightLineJoinCandidates++; stats.outerLineJoinCandidates++;
          addExample(result.examples, { styleId: style.styleId, pair: key, outsideSide: side.label, first: rgbHex(va), second: rgbHex(vb) });
        }
      }
    }
    result.styles.push(stats);
  }
  return result;
}
async function contactSheets(lookup) {
  const destination = path.join(ROOT, 'QA/ContactSheets'); await fs.mkdir(destination, { recursive: true });
  const files = [];
  for (const style of lookup.styles) {
    const columns = [];
    for (const variant of style.variants) {
      const data = await sharp(path.join(ROOT, variant.atlas)).resize(1024, 768, { kernel: 'nearest' }).png().toBuffer();
      columns.push({ input: data, left: variant.phaseX * 1024, top: variant.phaseY * 768 });
    }
    const filename = path.join(destination, `${style.styleId}_all47_all4phases_4x.png`);
    await sharp({ create: { width: 2048, height: 1536, channels: 4, background: '#f4f4f4' } }).composite(columns).png().toFile(filename);
    files.push(local(filename));
  }
  return { generated: true, files, pixelsAltered: false, transform: 'Nearest-neighbor 4x enlargement and 2x2 atlas arrangement only; v0 top-left, v1 top-right, v2 bottom-left, v3 bottom-right.', humanVisualReview: 'NOT CLAIMED; these sheets are supplied for review.' };
}
async function samplingChecks(current) {
  const result = { status: 'PASS', pngs: 0, pixelsChecked: 0, bodyFields: 0, framePanels: 0, ringPanels: 0,
    bodyFieldPeriodPixels: [], bodyOnlyMask255Comparisons: 0, pixelPngDigestSha256: null,
    noiseTotals: emptyNoiseSummary(), files: [], scope: 'All PNGs in every Art/<style>/Sampling directory: actual palette, binary alpha, transparent RGB, metadata dimensions and candidate clusters. Body field periodicity and all four mask255 exports are checked exactly.' };
  const digest = crypto.createHash('sha256'), palette = new Set(current.lookup.palette.map(hex => parseInt(hex.slice(1), 16)));
  for (const style of current.lookup.styles) {
    const directory = path.join(ROOT, 'Art', style.styleId, 'Sampling');
    let entries;
    try { entries = await fs.readdir(directory); }
    catch (error) { if (error.code !== 'ENOENT') throw error; entries = []; }
    const names = entries.filter(name => /\.png$/iu.test(name)).sort();
    if (current.lookup.artVersion >= 2) assert(names.length > 0, `Missing Sampling directory: ${style.styleId}`);
    for (const name of names) {
      const absolute = path.join(directory, name), relative = local(absolute), bytes = await fs.readFile(absolute);
      const { data, info } = await sharp(bytes).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
      const pixels = new Uint32Array(info.width * info.height), usedColors = new Set();
      const body = relative === style.materialSampling?.repeatedField;
      const frame = relative === style.decorativeSampling?.frame, ring = relative === style.decorativeSampling?.ring;
      const expectedPixels = body ? 128 : frame ? style.decorativeSampling.framePixels : ring ? style.decorativeSampling.ringPixels : null;
      if (expectedPixels !== null) { assert.equal(info.width, expectedPixels, relative); assert.equal(info.height, expectedPixels, relative); }
      for (let i = 0; i < pixels.length; i++) {
        const alpha = data[i * 4 + 3], color = (data[i * 4] << 16) | (data[i * 4 + 1] << 8) | data[i * 4 + 2];
        assert(alpha === 0 || alpha === 255, `Sampling nonbinary alpha: ${relative} at pixel ${i}`);
        if (body) assert.equal(alpha, 255, `Sampling body field void: ${relative} at pixel ${i}`);
        if (alpha) { assert(palette.has(color), `Sampling color outside Sweetie16: ${relative} ${rgbHex(color)}`); pixels[i] = color; usedColors.add(rgbHex(color)); }
        else { assert.equal(color, 0, `Sampling colored transparent pixel: ${relative} at pixel ${i}`); pixels[i] = 0xffffffff; }
      }
      const noise = noiseCandidates(pixels, info.width, info.height); mergeNoise(result.noiseTotals, noise.summary);
      digest.update(`${relative}\n`); digest.update(bytes); result.pngs++; result.pixelsChecked += pixels.length;
      if (frame) result.framePanels++; if (ring) result.ringPanels++;
      if (body) {
        result.bodyFields++;
        const period = style.materialSampling.periodPixels;
        assert(Number.isInteger(period) && period > 0 && period <= info.width && period <= info.height, `Invalid body period: ${style.styleId}`);
        if (!result.bodyFieldPeriodPixels.includes(period)) result.bodyFieldPeriodPixels.push(period);
        for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
          assert.equal(pixels[y * info.width + x], pixels[(y % period) * info.width + x % period], `Body period mismatch: ${style.styleId} at ${x},${y}`);
        }
        for (const variant of style.variants) {
          const exported = current.sprites.get(`${style.styleId}|${variant.id}|255`), sx = 32 + variant.phaseX * CELL, sy = 32 + variant.phaseY * CELL;
          assert(exported, `Missing body-only mask255: ${style.styleId}/${variant.id}`);
          for (let y = 0; y < CELL; y++) for (let x = 0; x < CELL; x++)
            assert.equal(exported[y * CELL + x], pixels[(sy + y) * info.width + sx + x], `Body-only export differs from sampling field: ${style.styleId}/${variant.id} ${x},${y}`);
          result.bodyOnlyMask255Comparisons++;
        }
      }
      result.files.push({ file: relative, dimensions: [info.width, info.height], role: body ? 'body' : frame ? 'frame' : ring ? 'ring' : 'unregistered sampling PNG',
        colors: [...usedColors].sort(), isolatedPixelCandidates: noise.summary.isolatedPixelCandidates, smallClusterCandidates: noise.summary.smallClusterCandidates,
        smallClusterPixels: noise.summary.smallClusterPixels, examples: noise.examples });
    }
  }
  if (current.lookup.artVersion >= 2) { assert.equal(result.bodyFields, 20); assert.equal(result.bodyOnlyMask255Comparisons, 80); }
  result.pixelPngDigestSha256 = digest.digest('hex');
  return result;
}
function coreChecks() {
  const names = ['validate_topology.mjs', 'validate_sprite_files.mjs', 'validate_gallery.mjs'], checks = [];
  for (const name of names) {
    const result = spawnSync(process.execPath, [path.join(ROOT, 'Tools', name)], { cwd: ROOT, encoding: 'utf8', maxBuffer: 8 * 1024 * 1024 });
    checks.push({ script: `Tools/${name}`, passed: result.status === 0, exitCode: result.status,
      ...(result.status !== 0 ? { message: String(result.stderr || result.stdout || result.error).slice(-4000) } : {}) });
    if (result.status !== 0) break;
  }
  return checks;
}
function compare(before, after) {
  const fields = ['isolatedPixelCandidates', 'smallClusterCandidates', 'smallClusterPixels'];
  return Object.fromEntries(fields.map(field => [field, { v1: before[field], current: after[field],
    change: after[field] - before[field], reductionPercent: before[field] ? Number((100 * (before[field] - after[field]) / before[field]).toFixed(2)) : null }]));
}
const started = performance.now();
const current = await scan(ROOT, true), baseline = await scan(BASELINE, false);
const sampling = await samplingChecks(current);
const core = RUN_CORE ? coreChecks() : [];
const report = {
  schemaVersion: 1, artVersion: current.lookup.artVersion ?? 1, contractId: current.lookup.contractId, generatedAt: new Date().toISOString(), nodeVersion: process.version,
  measurementStatus: 'COMPLETE', coreChecksStatus: RUN_CORE ? core.every(c => c.passed) && core.length === 3 ? 'PASS' : 'FAIL' : 'NOT RUN IN THIS INVOCATION',
  artSemanticVerdict: 'NOT AUTOMATICALLY DETERMINED',
  definitions: {
    isolatedPixelCandidate: 'An opaque exact-color 8-connected component of area 1 entirely inside the tile; equivalently none of its eight neighbors has the same exact color.',
    smallClusterCandidate: 'An exact-color 8-connected component of area 1, 2 or 3 that does not touch the tile boundary. A clipped feature at a tile edge is excluded.',
    straightBoundaryInkCandidate: 'For cardinally exposed sides only, inspect the central 16 boundary pixels (8..23), count consecutive inward #1a1c2c pixels from depth0 up to8. A count other than1 is a review candidate, not proof of a double or a missing semantic outline.',
    perimeterInk2x2Candidate: 'A #1a1c2c 2x2 square within4px of a cardinally exposed tile side. This may be an intentional corner/frame/shadow, so it is a review candidate only.',
    sharedEdgeCandidate: 'Enumerate all2048 two-cell logical neighborhoods at all4 actual global phase parities for every20styles; deduplicate identical sprite pairs. Paired #1a1c2c runs≥2 away from outer4px end regions are possible internal double lines, not confirmed faults.',
    samplingClusterCandidate: 'Use the same exact-color8-connected area≤3 definition at native sampling-image dimensions, exclude components touching that image boundary and ignore transparent pixels. The counts are candidate statistics, not defect verdicts.',
  },
  limitations: [
    'These color-component and boundary metrics do not semantically distinguish intentional tiny details from noise.',
    'A 2x2 filled material or intentional frame is not intrinsically a pixel-art double.',
    'Jaggies require judging the intended curve and staircase rhythm. Arbitrary jaggies-free art is not proven by this audit.',
    'All3760 exported PNGs are measured; human review of every atlas is not claimed.',
    'No Unity rendering, gameplay, collider, editor latency or mobile device validation is performed by this script.',
  ],
  current: current.report, baseline: baseline.report,
  noiseComparison: { total: compare(baseline.report.totals.noise, current.report.totals.noise),
    styles: current.report.styles.map(style => ({ styleId: style.styleId, ...compare(baseline.report.styles.find(s => s.styleId === style.styleId).noise, style.noise) })) },
  actualSpriteAdjacencyAudit: seamCandidates(current),
  samplingChecks: sampling,
  existingPackageChecks: core,
  reviewSheets: MAKE_SHEETS ? await contactSheets(current.lookup) : { generated: false, humanVisualReview: 'NOT CLAIMED' },
  durationMs: Math.round(performance.now() - started),
};
await fs.writeFile(path.join(ROOT, 'Docs/clean_pixel_validation.json'), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ measurementStatus: report.measurementStatus, coreChecksStatus: report.coreChecksStatus,
  pngsMeasuredPerVersion: report.current.totals.noise.sprites, noiseComparison: report.noiseComparison.total,
  borderCandidates: report.current.totals.border, adjacentUniquePairs: report.actualSpriteAdjacencyAudit.uniquePixelPairs,
  samplingPngs: report.samplingChecks.pngs, samplingPixelsChecked: report.samplingChecks.pixelsChecked,
  reviewSheets: report.reviewSheets.files?.length ?? 0, report: 'Docs/clean_pixel_validation.json', durationMs: report.durationMs }, null, 2));
if (RUN_CORE && report.coreChecksStatus !== 'PASS') process.exitCode = 1;
