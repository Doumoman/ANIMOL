/** Independent role-plane and small-hole regression. Reads final PNGs; never edits artwork. */
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { CANONICAL_MASKS, RAW_TO_CANONICAL, RAW_TO_INDEX, QUARTER_MODULE_KEYS, getQuadrants, occupancyFromRows, resolveCell, partitionOccupancy } from './terrain_topology.mjs';
import { resolveVariant } from './terrain_composition.mjs';

const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2), option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const BASELINE = path.resolve(ROOT, option('--baseline-v4', '../ANIMOL_FreeShape_Sprites_joint_finish_v4'));
const REPORT = path.resolve(ROOT, option('--report', 'Docs/hole_finish_validation.json'));
const errors = [], checks = {}, images = new Map();
let failureCount = 0;
const started = performance.now(), equalJSON = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function verify(ok, label, detail) {
  if (ok) return;
  failureCount++;
  if (errors.length < 80) errors.push({ label, ...(detail === undefined ? {} : { detail }) });
}
const loadJSON = async (root, name) => JSON.parse(await fs.readFile(path.join(root, name), 'utf8'));
async function image(root, name) {
  const filename = path.resolve(root, name);
  if (!images.has(filename)) images.set(filename, (async () => {
    const bytes = await fs.readFile(filename), { data, info } = await sharp(bytes).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    return { bytes, data, width: info.width, height: info.height };
  })());
  return images.get(filename);
}
const blank = (width, height) => ({ width, height, data: Buffer.alloc(width * height * 4) });
const index = (img, x, y) => (y * img.width + x) * 4;
const rgba = color => Buffer.from([...color.slice(1).match(/../gu).map(value => parseInt(value, 16)), 255]);
function paint(img, x, y, color) {
  if (x >= 0 && y >= 0 && x < img.width && y < img.height) rgba(color).copy(img.data, index(img, x, y));
}
function compare(actual, expected, label) {
  let mismatches = 0;
  const examples = [];
  verify(actual.width === expected.width && actual.height === expected.height, `${label}: dimensions`, { actual: [actual.width, actual.height], expected: [expected.width, expected.height] });
  if (actual.data.length !== expected.data.length) return expected.width * expected.height;
  for (let p = 0; p < expected.data.length; p += 4) if (!actual.data.subarray(p, p + 4).equals(expected.data.subarray(p, p + 4))) {
    mismatches++;
    if (examples.length < 8) examples.push({ x: (p / 4) % expected.width, y: Math.floor(p / 4 / expected.width), actual: actual.data.subarray(p, p + 4).toString('hex'), expected: expected.data.subarray(p, p + 4).toString('hex') });
  }
  verify(mismatches === 0, `${label}: exact RGBA`, { mismatches, examples });
  return mismatches;
}
async function pngFiles(directory) {
  const result = [];
  for (const entry of await fs.readdir(directory, { withFileTypes: true })) {
    const name = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...await pngFiles(name));
    else if (entry.name.endsWith('.png')) result.push(name);
  }
  return result;
}
async function styleFiles(root, id) {
  return [...await pngFiles(path.join(root, 'Art', id)), ...await pngFiles(path.join(root, 'Samples', id)),
    ...(await fs.readdir(path.join(root, 'Art/Atlases'))).filter(name => name.startsWith(`${id}_`) && name.endsWith('.png')).map(name => path.join(root, 'Art/Atlases', name))]
    .map(name => path.relative(root, name).split(path.sep).join('/')).sort();
}

// This expected drawing is derived from declarations, not from generated role
// masks, output colors, builder functions, or source-panel color classification.
function rolePlane(layout, role) {
  const img = blank(128, 128), faces = role === 'frame' ? layout.frameFaces : layout.ringFaces;
  for (const r of faces) for (let y = r.y - 1; y <= r.y + r.h; y++) for (let x = r.x - 1; x <= r.x + r.w; x++) paint(img, x, y, layout.boundaryPalette);
  for (const r of faces) for (let y = 0; y < r.h; y++) for (let x = 0; x < r.w; x++) {
    const rounded = r.cornerRadiusPixels === 1 && (x === 0 || x === r.w - 1) && (y === 0 || y === r.h - 1);
    if (rounded) continue;
    const highlighted = r.highlightColor && y === 0 && x >= (r.highlightInsetPixels ?? 2) && x < r.w - (r.highlightInsetPixels ?? 2);
    paint(img, r.x + x, r.y + y, highlighted ? r.highlightColor : r.shadeColor && y === r.h - 1 ? r.shadeColor : r.c);
  }
  if (role === 'frame') {
    for (const x of layout.capJointColumns) for (const y of [5, 6, 7, 8, 9, 10, 117, 118, 119, 120]) paint(img, x, y, layout.boundaryPalette);
    const b = layout.frameBounds;
    for (let y = b.y; y < b.y + b.h; y++) { paint(img, b.x, y, layout.boundaryPalette); paint(img, b.x + b.w - 1, y, layout.boundaryPalette); }
    for (let x = b.x; x < b.x + b.w; x++) { paint(img, x, b.y, layout.boundaryPalette); paint(img, x, b.y + b.h - 1, layout.boundaryPalette); }
  } else {
    const h = layout.hole;
    for (let y = h.y; y < h.y + h.h; y++) for (let x = h.x; x < h.x + h.w; x++) img.data.fill(0, index(img, x, y), index(img, x, y) + 4);
  }
  return img;
}
function quarter(core, frame, ring, layout, sampling, corner, state, px, py) {
  const west = corner.endsWith('W'), north = corner.startsWith('N'), out = blank(16, 16), owners = new Uint8Array(256);
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const sx = px * 32 + (west ? 0 : 16) + x, sy = py * 32 + (north ? 0 : 16) + y;
    core.data.copy(out.data, index(out, x, y), index(core, sx, sy), index(core, sx, sy) + 4);
  }
  const b = layout.frameBounds, h = layout.hole, dx = x => west ? x : 15 - x, dy = y => north ? y : 15 - y;
  function overlay(src, sx, sy, accepts) {
    for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) if (accepts(x, y)) {
      const p = index(src, sx + x, sy + y);
      if (src.data[p + 3] === 255) { src.data.copy(out.data, index(out, x, y), p, p + 4); owners[y * 16 + x] = 1; }
    }
  }
  if (['TOP', 'BOTTOM', 'OUTER'].includes(state)) overlay(frame, (128 - sampling.horizontalPeriodPixels) / 2 + (px * 32 + (west ? 0 : 16)) % sampling.horizontalPeriodPixels, north ? b.y : b.y + b.h - 16, (x, y) => dy(y) < sampling.trimWidth);
  if (['SIDE', 'OUTER'].includes(state)) overlay(frame, west ? b.x : b.x + b.w - 16, (128 - sampling.sidePeriodPixels) / 2 + (py * 32 + (north ? 0 : 16)) % sampling.sidePeriodPixels, (x, y) => dx(x) < sampling.trimWidth);
  if (state === 'OUTER') overlay(frame, west ? b.x : b.x + b.w - 16, north ? b.y : b.y + b.h - 16, (x, y) => dx(x) < sampling.trimWidth || dy(y) < sampling.trimWidth);
  if (state === 'INNER') overlay(ring, west ? h.x + h.w : h.x - 16, north ? h.y + h.h : h.y - 16, () => true);
  return { ...out, owners };
}
function assembled(modules, quadrants) {
  const out = blank(32, 32);
  for (const q of quadrants) for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const src = modules[q.moduleKey], p = index(src, x, y);
    src.data.copy(out.data, index(out, q.column * 16 + x, q.row * 16 + y), p, p + 4);
  }
  return out;
}
const offsets = [[0, 1, 1], [1, 1, 2], [1, 0, 4], [1, -1, 8], [0, -1, 16], [-1, -1, 32], [-1, 0, 64], [-1, 1, 128]];
function independentTopology(occupied, x, y) {
  if (!occupied.has(`${x},${y}`)) return null;
  let raw = 0;
  for (const [dx, dy, bit] of offsets) if (occupied.has(`${x + dx},${y + dy}`)) raw |= bit;
  let mask = raw;
  for (const [vertical, horizontal, diagonal] of [[1, 64, 128], [1, 4, 2], [16, 64, 32], [16, 4, 8]]) if (!(raw & vertical) || !(raw & horizontal)) mask &= ~diagonal;
  return { raw, mask };
}

const lookup = await loadJSON(ROOT, 'Data/sprite_lookup.json'), baselineLookup = await loadJSON(BASELINE, 'Data/sprite_lookup.json');
const layout = await loadJSON(ROOT, 'Data/hole_native_layout_v5.json'), historicalLayout = await loadJSON(ROOT, 'Data/joint_native_layout_v4.json'), baselineLayout = await loadJSON(BASELINE, 'Data/joint_native_layout_v4.json');
verify(equalJSON(historicalLayout, baselineLayout), 'Historical v4 native layout changed');
for (const key of ['styleId', 'logicalPixels', 'palette', 'sourceFaces', 'courses', 'frameBounds', 'hole', 'frameFaces', 'boundaryPalette', 'capJointColumns']) verify(equalJSON(layout[key], baselineLayout[key]), `Approved native ${key} registration changed`);
verify(layout.artVersion === 5, 'Active hole layout art revision missing');
const registeredRingRectangles = [
  ['#5d275d', 27, 39, 2, 51, 0, null], ['#5d275d', 97, 39, 2, 51, 0, null],
  ['#f4f4f4', 40, 36, 46, 3, 1, '#ef7d57'], ['#f4f4f4', 40, 90, 46, 5, 1, '#ef7d57'],
  ['#ef7d57', 24, 36, 7, 4, 1, '#5d275d'], ['#ef7d57', 95, 36, 7, 4, 1, '#5d275d'],
  ['#ef7d57', 24, 90, 7, 6, 1, '#5d275d'], ['#ef7d57', 95, 90, 7, 6, 1, '#5d275d'],
];
verify(equalJSON(layout.ringFaces.map(r => [r.c, r.x, r.y, r.w, r.h, r.cornerRadiusPixels ?? 0, r.shadeColor ?? null]), registeredRingRectangles), 'Hole collar/rail geometry differs from registered v5 correction');
verify(lookup.artVersion === 5 && lookup.schemaVersion === 1 && lookup.contractId === 'ANIMOL_FREE_SHAPE_BLOB47_V1' && lookup.cellPixels === 32 && lookup.pixelsPerUnit === 32, 'Art revision or 1x1 occupancy contract changed');
for (const key of ['palette', 'transparentRGB', 'alphaValues', 'cellArtAlpha', 'phase', 'macroMotif']) verify(equalJSON(lookup[key], baselineLookup[key]), `Lookup ${key} contract changed`);
verify(equalJSON(lookup.canonicalMasks, CANONICAL_MASKS) && equalJSON(lookup.rawToCanonical, RAW_TO_CANONICAL) && equalJSON(lookup.rawToIndex, RAW_TO_INDEX), '47-mask topology lookup changed');
verify(lookup.styles.length === 20 && lookup.styles.every((style, i) => style.styleId === baselineLookup.styles[i].styleId), 'Five-theme 20-style catalog changed');
for (const file of ['Data/logical_fixtures.json', 'Data/style_catalog.json']) verify((await fs.readFile(path.join(ROOT, file))).equals(await fs.readFile(path.join(BASELINE, file))), `Existing fixture/catalog file changed: ${file}`);
checks.contract = { artVersion: lookup.artVersion, contractId: lookup.contractId, schemaVersion: lookup.schemaVersion, cellPixels: lookup.cellPixels, pixelsPerUnit: lookup.pixelsPerUnit, styles: lookup.styles.length, canonicalMasks: lookup.canonicalMasks.length, fixtureSchemaUnchanged: true };

const target = lookup.styles.find(style => style.styleId === 'T01_A'), sampling = target.decorativeSampling;
verify(sampling.junctionOverlayProfile === 'decoration-roles-v5', 'T01_A role-plane profile missing');
verify(sampling.outerCornerPolicy?.includes('8px') && sampling.innerCornerPolicy?.includes('complete'), 'Approved corner sampling policy changed');
checks.unchangedNative = { files: [], pixelMismatches: 0 };
for (const file of ['SourceArt/Revised/T01_A_BrickRepeat64.png', 'SourceArt/Revised/T01_A_BrickRepeat128.png', 'SourceArt/Revised/T01_A_Frame_Joint128.png', 'Art/T01_A/material.png', 'Art/T01_A/frame.png', 'Art/T01_A/motif.png', 'Art/T01_A/Sampling/body_field128.png']) {
  const current = await image(ROOT, file), previous = await image(BASELINE, file), mismatches = compare(current, previous, `Unchanged source/panel ${file}`);
  verify(current.bytes.equals(previous.bytes), `Unchanged source/panel PNG bytes: ${file}`);
  checks.unchangedNative.pixelMismatches += mismatches;
  checks.unchangedNative.files.push({ file, pngSha256: hash(current.bytes), pixels: current.width * current.height });
}
const expectedFrame = rolePlane(layout, 'frame'), expectedRing = rolePlane(layout, 'ring');
const oldFrame = await image(BASELINE, 'SourceArt/Revised/T01_A_Frame_Joint128.png'), expectedFullRing = { ...oldFrame, data: Buffer.from(oldFrame.data) };
for (let p = 0; p < expectedRing.data.length; p += 4) if (expectedRing.data[p + 3]) expectedRing.data.copy(expectedFullRing.data, p, p, p + 4);
for (let y = layout.hole.y; y < layout.hole.y + layout.hole.h; y++) for (let x = layout.hole.x; x < layout.hole.x + layout.hole.w; x++) expectedFullRing.data.fill(0, index(expectedFullRing, x, y), index(expectedFullRing, x, y) + 4);
checks.revisedNativeRing = { method: 'v4 native frame plus v5 declared ring faces and halo, clipped rectangular hole; all unrelated pixels preserved', files: [], rgbaMismatches: 0 };
for (const file of ['SourceArt/Revised/T01_A_Ring_Joint128.png', 'Art/T01_A/ring.png', 'Art/T01_A/Sampling/ring128.png']) {
  const actual = await image(ROOT, file), mismatches = compare(actual, expectedFullRing, `Registered revised ring ${file}`);
  checks.revisedNativeRing.rgbaMismatches += mismatches;
  checks.revisedNativeRing.files.push({ file, pngSha256: hash(actual.bytes) });
}
checks.declaredRolePlanes = { files: [], rgbaMismatches: 0, unassociatedBodyOverlayPixels: 0, inheritedOrangeFinPixels: 0, ringBraceShadePixels: 0, ringBraceRoundedCorners: 0 };
for (const [role, expected] of [['frame', expectedFrame], ['ring', expectedRing]]) {
  const file = `SourceArt/Revised/T01_A_${role === 'frame' ? 'Frame' : 'Ring'}_Decoration128.png`, actual = await image(ROOT, file);
  const mismatches = compare(actual, expected, `Declared ${role} decoration plane`);
  checks.declaredRolePlanes.rgbaMismatches += mismatches;
  let opaquePixels = 0, unassociated = 0;
  for (let p = 0; p < actual.data.length; p += 4) { if (actual.data[p + 3]) opaquePixels++; if (actual.data[p + 3] && !expected.data[p + 3]) unassociated++; }
  checks.declaredRolePlanes.unassociatedBodyOverlayPixels += unassociated;
  checks.declaredRolePlanes.files.push({ role, file, opaquePixels, pngSha256: hash(actual.bytes) });
}
for (const [startY, endY] of [[25, 30], [89, 94]]) for (let y = startY; y <= endY; y++) for (let x = 14; x <= 15; x++) {
  const actual = await image(ROOT, 'SourceArt/Revised/T01_A_Ring_Decoration128.png');
  if (actual.data[index(actual, x, y) + 3]) checks.declaredRolePlanes.inheritedOrangeFinPixels++;
}
verify(checks.declaredRolePlanes.inheritedOrangeFinPixels === 0, 'Screenshot regression: inherited left orange fin is still an overlay', checks.declaredRolePlanes.inheritedOrangeFinPixels);
for (const r of layout.ringFaces.filter(face => face.c === '#ef7d57')) {
  checks.declaredRolePlanes.ringBraceShadePixels += r.w - 2;
  checks.declaredRolePlanes.ringBraceRoundedCorners += 4;
  verify(r.shadeColor === '#5d275d' && r.cornerRadiusPixels === 1, 'Ring brace shadow or rounded corner declaration lost', r);
}

const core = await image(ROOT, 'SourceArt/Revised/T01_A_BrickRepeat64.png'), expectedModules = [], actualCells = [];
checks.targetAssembly = { variants: 0, quarterModules: 0, quarterPixelsCompared: 0, quarterPixelMismatches: 0, overlayMaskMismatches: 0, overlayPixels: 0, bodyPixels: 0, cellSprites: 0, cellPixelMismatches: 0, atlasPixelMismatches: 0, opaqueCellAlphaFailures: 0 };
checks.innerJunctionPorts = { comparisons: 0, rgbaMismatches: 0, ports: [] };
for (const variant of target.variants) {
  verify(variant.id === `v${variant.phaseX + variant.phaseY * 2}` && [0, 1].includes(variant.phaseX) && [0, 1].includes(variant.phaseY), 'Global phase variant registration changed', variant.id);
  const modules = {};
  for (const key of QUARTER_MODULE_KEYS) {
    const [corner, state] = key.split('_'), expected = quarter(core, expectedFrame, expectedRing, layout, sampling, corner, state, variant.phaseX, variant.phaseY), actual = await image(ROOT, variant.modules[key]);
    modules[key] = expected;
    checks.targetAssembly.quarterPixelMismatches += compare(actual, expected, `${variant.id}/${key}`);
    const rows = variant.junctionOverlayMasks?.[key];
    verify(Array.isArray(rows) && rows.length === 16 && rows.every(row => /^[01]{16}$/u.test(row)), `Invalid overlay role mask: ${variant.id}/${key}`);
    let mismatches = 0;
    for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
      const expectedOwner = expected.owners[y * 16 + x];
      if (String(expectedOwner) !== rows?.[y]?.[x]) mismatches++;
      if (expectedOwner) checks.targetAssembly.overlayPixels++; else checks.targetAssembly.bodyPixels++;
    }
    verify(mismatches === 0, `Overlay role mask differs from declared geometry: ${variant.id}/${key}`, mismatches);
    checks.targetAssembly.overlayMaskMismatches += mismatches;
    if (state === 'INNER') {
      // Outgoing horizontal collar and vertical rail profiles are asserted as
      // explicit port vectors, independently of the declaration-derived plane.
      const west = corner.endsWith('W'), north = corner.startsWith('N');
      const edgeX = west ? 0 : 15, edgeY = north ? 0 : 15;
      const horizontal = north ? [[0, '#1a1c2c'], ...[1, 2, 3, 4, 5].map(y => [y, '#ef7d57']), [6, '#5d275d'], [7, '#1a1c2c']]
        : [[10, '#1a1c2c'], ...[11, 12, 13].map(y => [y, '#ef7d57']), [14, '#5d275d'], [15, '#1a1c2c']];
      const vertical = ['#1a1c2c', '#5d275d', '#5d275d', '#1a1c2c'].map((color, position) => [(west ? 0 : 12) + position, color]);
      let portMismatches = 0;
      for (const [y, color] of horizontal) if (!actual.data.subarray(index(actual, edgeX, y), index(actual, edgeX, y) + 4).equals(rgba(color))) portMismatches++;
      for (const [x, color] of vertical) if (!actual.data.subarray(index(actual, x, edgeY), index(actual, x, edgeY) + 4).equals(rgba(color))) portMismatches++;
      verify(portMismatches === 0, `Inner collar/rail port is not flush: ${variant.id}/${key}`, portMismatches);
      checks.innerJunctionPorts.comparisons += horizontal.length + vertical.length;
      checks.innerJunctionPorts.rgbaMismatches += portMismatches;
      checks.innerJunctionPorts.ports.push({ variant: variant.id, corner, horizontalEdgeX: edgeX, horizontal, verticalEdgeY: edgeY, vertical, rgbaMismatches: portMismatches });
    }
    checks.targetAssembly.quarterModules++; checks.targetAssembly.quarterPixelsCompared += 256;
  }
  expectedModules[variant.phaseX + variant.phaseY * 2] = modules;
  const atlas = await image(ROOT, variant.atlas), cells = new Map();
  verify(atlas.width === 256 && atlas.height === 192 && equalJSON(variant.atlasPixels, [256, 192]), `Atlas dimensions changed: ${variant.id}`);
  let blankSlotFailures = 0;
  for (let y = 160; y < 192; y++) for (let x = 224; x < 256; x++) if (!atlas.data.subarray(index(atlas, x, y), index(atlas, x, y) + 4).equals(Buffer.from([0, 0, 0, 0]))) blankSlotFailures++;
  verify(blankSlotFailures === 0, `Unused atlas slot is not transparent black: ${variant.id}`, blankSlotFailures);
  verify(variant.cells.length === 47 && equalJSON(variant.cells.map(cell => cell.mask), CANONICAL_MASKS), `47-cell list changed: ${variant.id}`);
  for (const cell of variant.cells) {
    const expected = assembled(modules, getQuadrants(cell.mask)), actual = await image(ROOT, cell.file);
    cells.set(cell.mask, actual);
    checks.targetAssembly.cellPixelMismatches += compare(actual, expected, `${variant.id}/mask${cell.mask}`);
    verify(cell.index === CANONICAL_MASKS.indexOf(cell.mask) && cell.rect.width === 32 && cell.rect.height === 32 && cell.rect.x === (cell.index % 8) * 32 && cell.rect.y === Math.floor(cell.index / 8) * 32, 'Atlas cell registration changed', cell);
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
      const p = index(actual, x, y), ap = index(atlas, cell.rect.x + x, cell.rect.y + y);
      if (actual.data[p + 3] !== 255) checks.targetAssembly.opaqueCellAlphaFailures++;
      if (!actual.data.subarray(p, p + 4).equals(atlas.data.subarray(ap, ap + 4))) checks.targetAssembly.atlasPixelMismatches++;
    }
    checks.targetAssembly.cellSprites++;
  }
  actualCells[variant.phaseX + variant.phaseY * 2] = cells;
  checks.targetAssembly.variants++;
}
verify(checks.targetAssembly.variants === 4 && checks.targetAssembly.quarterModules === 80 && checks.targetAssembly.cellSprites === 188, 'Target sprite count changed', checks.targetAssembly);
verify(checks.targetAssembly.opaqueCellAlphaFailures === 0 && checks.targetAssembly.atlasPixelMismatches === 0, 'Occupied-cell alpha or atlas contents changed', checks.targetAssembly);

checks.smallHoleRegression = { cases: [], cellsCompared: 0, cellPixelMismatches: 0, innerCornersCompared: 0, variantsUsed: [], emptyCells: 0, topologyMismatches: 0, chunkResolutionMismatches: 0 };
const variantsUsed = new Set();
for (const size of [1, 2]) for (const origin of [{ x: 0, y: 0 }, { x: -1, y: -1 }, { x: -17, y: -18 }, { x: 17, y: 18 }]) for (const seed of [0, 1, 2, 3]) {
  const width = size + 4, rows = Array.from({ length: width }, (_, y) => Array.from({ length: width }, (_, x) => x >= 2 && x < size + 2 && y >= 2 && y < size + 2 ? '.' : '#').join(''));
  const grid = occupancyFromRows(rows, origin), chunks = partitionOccupancy(grid.occupied), before = checks.smallHoleRegression.innerCornersCompared;
  for (let ly = 0; ly < width; ly++) for (let lx = 0; lx < width; lx++) {
    const x = origin.x + lx, y = origin.y + ly, topology = resolveCell(grid.occupied, x, y), independent = independentTopology(grid.occupied, x, y);
    if (!independent) { verify(topology === null, 'Empty hole was given a sprite', { size, x, y }); checks.smallHoleRegression.emptyCells++; continue; }
    if (topology.rawMask !== independent.raw || topology.canonicalMask !== independent.mask) checks.smallHoleRegression.topologyMismatches++;
    const expectedVariant = ((x + (seed & 1)) % 2 + 2) % 2 + 2 * (((-y + ((seed >> 1) & 1)) % 2 + 2) % 2), actualVariant = resolveVariant(x, y, seed);
    verify(actualVariant === expectedVariant, 'Global negative-coordinate phase mismatch', { x, y, seed, actualVariant, expectedVariant });
    variantsUsed.add(actualVariant);
    const expected = assembled(expectedModules[expectedVariant], getQuadrants(independent.mask)), actual = actualCells[actualVariant].get(independent.mask);
    checks.smallHoleRegression.cellPixelMismatches += compare(actual, expected, `Small ${size}x${size} hole origin${origin.x},${origin.y} seed${seed} cell${x},${y}`);
    checks.smallHoleRegression.cellsCompared++;
    for (const q of topology.quadrants) if (q.state === 'INNER') checks.smallHoleRegression.innerCornersCompared++;
    const hasAcrossChunks = (gx, gy) => [...chunks.values()].some(chunk => chunk.has(`${gx},${gy}`));
    if (!equalJSON(resolveCell(hasAcrossChunks, x, y), topology)) checks.smallHoleRegression.chunkResolutionMismatches++;
  }
  verify(checks.smallHoleRegression.innerCornersCompared - before === 4, 'Small hole must have exactly four internal corner patches', { size, origin, seed, innerCorners: checks.smallHoleRegression.innerCornersCompared - before });
  checks.smallHoleRegression.cases.push({ holeCells: [size, size], rows, origin, seed, chunks: chunks.size, innerCorners: checks.smallHoleRegression.innerCornersCompared - before });
}
checks.smallHoleRegression.variantsUsed = [...variantsUsed].sort();
verify(checks.smallHoleRegression.cases.length === 32 && variantsUsed.size === 4 && checks.smallHoleRegression.topologyMismatches === 0 && checks.smallHoleRegression.chunkResolutionMismatches === 0, 'Small-hole topology/global-phase/chunk regression failed', checks.smallHoleRegression);

checks.otherStylesUnchanged = { styles: [], files: 0, bytes: 0, byteMismatches: 0, combinedSha256: null };
const combined = crypto.createHash('sha256');
for (const style of lookup.styles.filter(entry => entry.styleId !== 'T01_A')) {
  const files = await styleFiles(ROOT, style.styleId), oldFiles = await styleFiles(BASELINE, style.styleId), stats = { styleId: style.styleId, pngFiles: files.length, bytes: 0, byteMismatches: 0 };
  verify(equalJSON(files, oldFiles), `Other style PNG inventory changed: ${style.styleId}`);
  verify(equalJSON(style, baselineLookup.styles.find(entry => entry.styleId === style.styleId)), `Other style metadata changed: ${style.styleId}`);
  for (const file of files) {
    const current = await fs.readFile(path.join(ROOT, file)), previous = await fs.readFile(path.join(BASELINE, file));
    stats.bytes += current.length;
    if (!current.equals(previous)) stats.byteMismatches++;
    combined.update(file).update('\0').update(hash(current)).update('\n');
  }
  verify(stats.byteMismatches === 0, `Other style art changed: ${style.styleId}`, stats);
  checks.otherStylesUnchanged.styles.push(stats); checks.otherStylesUnchanged.files += stats.pngFiles; checks.otherStylesUnchanged.bytes += stats.bytes; checks.otherStylesUnchanged.byteMismatches += stats.byteMismatches;
}
checks.otherStylesUnchanged.combinedSha256 = combined.digest('hex');
const report = { pass: failureCount === 0, artVersion: lookup.artVersion, baseline: path.basename(BASELINE), method: 'Independent declaration-derived transparent role planes; exact RGBA mapping; global negative-coordinate small-hole resolver; bounded PNG-byte comparison. No image mutation.', elapsedMilliseconds: Math.round(performance.now() - started), checks, failureCount, errors };
await fs.mkdir(path.dirname(REPORT), { recursive: true });
await fs.writeFile(REPORT, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ pass: report.pass, failureCount, targetAssembly: checks.targetAssembly, smallHoleCases: checks.smallHoleRegression.cases.length, inheritedOrangeFinPixels: checks.declaredRolePlanes.inheritedOrangeFinPixels, otherStylePngFiles: checks.otherStylesUnchanged.files, otherStyleByteMismatches: checks.otherStylesUnchanged.byteMismatches, report: path.relative(ROOT, REPORT) }, null, 2));
if (failureCount) process.exitCode = 1;
