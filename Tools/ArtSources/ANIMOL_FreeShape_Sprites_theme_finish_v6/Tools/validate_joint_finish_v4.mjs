/** Strict native mortar geometry and bounded-change audit. Reads PNGs; never edits art. */
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { getQuadrants, QUARTER_MODULE_KEYS } from './terrain_topology.mjs';

const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2);
const arg = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const BASELINE = path.resolve(ROOT, arg('--baseline-v3', '../ANIMOL_FreeShape_Sprites_brick_restore_v3'));
const NATIVE = arg('--native-repeat', 'SourceArt/Revised/T01_A_BrickRepeat64.png');
const LAYOUT = arg('--layout', 'Data/joint_native_layout_v4.json');
const REPORT = path.resolve(ROOT, arg('--report', 'Docs/joint_finish_validation.json'));
const TARGET = 'T01_A', PERIOD = 64, CELL = 32, INK = 0x1a1c2c;
const errors = [], checks = {}, imageCache = new Map();
let failureCount = 0;
const started = performance.now();
const local = filename => path.relative(ROOT, filename).split(path.sep).join('/');
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function requireCheck(ok, label, detail = null) {
  if (ok) return;
  failureCount++;
  if (errors.length < 80) errors.push({ label, ...(detail === null ? {} : { detail }) });
}
const equalJSON = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const json = async (root, filename) => JSON.parse(await fs.readFile(path.join(root, filename), 'utf8'));
async function readImage(root, filename) {
  const absolute = path.resolve(root, filename);
  if (!imageCache.has(absolute)) imageCache.set(absolute, (async () => {
    const bytes = await fs.readFile(absolute);
    const { data, info } = await sharp(bytes).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    return { bytes, data, width: info.width, height: info.height };
  })());
  return imageCache.get(absolute);
}
const offset = (image, x, y) => (y * image.width + x) * 4;
function rgb(image, x, y) {
  const i = offset(image, x, y);
  return (image.data[i] << 16) | (image.data[i + 1] << 8) | image.data[i + 2];
}
const hex = color => `#${color.toString(16).padStart(6, '0')}`;
const pixelEqual = (a, ai, b, bi) => a.data.subarray(ai, ai + 4).equals(b.data.subarray(bi, bi + 4));
async function pngFiles(directory) {
  const files = [];
  for (const entry of await fs.readdir(directory, { withFileTypes: true })) {
    const filename = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...await pngFiles(filename));
    else if (/\.png$/iu.test(entry.name)) files.push(filename);
  }
  return files;
}
async function styleFiles(root, id) {
  return [
    ...await pngFiles(path.join(root, 'Art', id)),
    ...await pngFiles(path.join(root, 'Samples', id)),
    ...(await fs.readdir(path.join(root, 'Art/Atlases')))
      .filter(name => name.startsWith(`${id}_`) && name.endsWith('.png'))
      .map(name => path.join(root, 'Art/Atlases', name)),
  ].map(filename => path.relative(root, filename).split(path.sep).join('/')).sort();
}
function cyclicSpan(values) {
  const sorted = [...new Set(values)].sort((a, b) => a - b);
  let largestGap = -1, origin = sorted[0];
  for (let i = 0; i < sorted.length; i++) {
    const next = sorted[(i + 1) % sorted.length];
    const distance = ((next - sorted[i] + PERIOD) % PERIOD) || PERIOD;
    if (distance - 1 > largestGap) { largestGap = distance - 1; origin = next; }
  }
  return { origin, length: PERIOD - largestGap };
}
function declaredNativeGeometry(layout) {
  requireCheck(layout.styleId === TARGET && layout.logicalPixels === PERIOD, 'Native layout identity or dimensions changed', LAYOUT);
  const colors = new Uint32Array(PERIOD * PERIOD); colors.fill(INK);
  const owners = new Int16Array(PERIOD * PERIOD); owners.fill(-1);
  const faces = [], shades = [], highlights = [], expectedRegions = new Map();
  const shadeFor = { '#f4f4f4': '#ef7d57', '#ef7d57': '#5d275d' };
  const courses = layout.courses ?? [];
  requireCheck(equalJSON(courses.map(row => row.y), [0, 16, 32, 48]), 'Native course positions changed', courses.map(row => row.y));
  for (const row of courses) {
    requireCheck(row.height === 16 && Array.isArray(row.joints) && row.joints[0] === 0 && row.joints.every((x, i) => Number.isInteger(x) && x >= 0 && x < 64 && (!i || x > row.joints[i - 1])), 'Invalid native course rectangle registration', row);
    requireCheck(Array.isArray(row.colors) && row.colors.length === row.joints.length, 'Native face colors do not match joint intervals', row);
    requireCheck(Array.isArray(row.shadeColors) && row.shadeColors.length === row.colors.length, 'Native facet bands must be explicitly declared for every face', row.y);
    requireCheck(Array.isArray(row.highlightColors) && row.highlightColors.length === row.colors.length, 'Native highlight bands must be explicitly declared for every face', row.y);
    for (let j = 0; j < row.joints.length; j++) {
      const x = row.joints[j] + 1, right = row.joints[j + 1] ?? PERIOD, y = row.y + 1;
      const w = right - x, h = row.height - 1, color = row.colors[j], shade = row.shadeColors?.[j] ?? null;
      const highlight = row.highlightColors?.[j] ?? null, highlightInset = row.highlightInsetPixels ?? layout.highlightInsetPixels ?? 2;
      const radius = row.cornerRadiusPixels ?? layout.cornerRadiusPixels ?? layout.bodyCornerRadiusPixels ?? 0;
      requireCheck([0, 1].includes(radius) && w > radius * 2 && h > radius * 2, 'Only registered radius0/radius1 native corners are supported', { y: row.y, j, w, h, radius });
      requireCheck(shade === (shadeFor[color] ?? null), 'Approved color relationship lost its declared bottom facet', { course: row.y, face: j, color, shade, expected: shadeFor[color] ?? null });
      requireCheck(highlight === (color === '#257179' ? '#f4f4f4' : null), 'Approved teal top highlight was lost or changed', { course: row.y, face: j, color, highlight });
      requireCheck(Number.isInteger(highlightInset) && highlightInset === 2 && (!highlight || w > highlightInset * 2), 'Native highlight inset differs from registered two-pixel inset', { course: row.y, face: j, w, highlightInset });
      const id = faces.length;
      const face = { id, course: row.y, face: j, origin: [x, y], dimensions: [w, h], color, shadeColor: shade, highlightColor: highlight, highlightInsetPixels: highlightInset, cornerRadiusPixels: radius };
      faces.push(face);
      let shadePixels = 0, highlightPixels = 0;
      for (let yy = y; yy < y + h; yy++) for (let xx = x; xx < x + w; xx++) {
        const corner = radius === 1 && (xx === x || xx === x + w - 1) && (yy === y || yy === y + h - 1);
        if (corner) continue;
        const isHighlight = highlight && yy === y && xx >= x + highlightInset && xx < x + w - highlightInset;
        const c = isHighlight ? highlight : shade && yy === y + h - 1 ? shade : color, index = yy * PERIOD + xx;
        owners[index] = id; colors[index] = parseInt(c.slice(1), 16);
        const key = `${id}/${colors[index]}`;
        if (!expectedRegions.has(key)) expectedRegions.set(key, []);
        expectedRegions.get(key).push(index);
        if (shade && yy === y + h - 1) shadePixels++;
        if (isHighlight) highlightPixels++;
      }
      if (shade) shades.push({ brickId: id, color: shade, y: y + h - 1, pixels: shadePixels });
      if (highlight) highlights.push({ brickId: id, color: highlight, y, inset: highlightInset, pixels: highlightPixels });
    }
  }
  return { colors, owners, faces, shades, highlights, expectedRegions, courses };
}
function auditNativeGeometry(image, expected) {
  const result = {
    dimensions: [image.width, image.height], inkColor: '#1a1c2c',
    layout: LAYOUT, registeredFaces: expected.faces, horizontalCourses: [],
    toroidalInk2x2: 0, declaredRoundedInk2x2: 0, unregisteredInk2x2: 0, missingDeclaredInk2x2: 0, ink2x2Examples: [],
    directDifferentNonInkContacts: 0, contactExamples: [],
    allowedSameBrickFacetContacts: 0, coloredComponents: [], rectangularColoredComponents: 0, nonRectangularColoredComponents: 0,
    registeredFaceOrFacetComponents: 0, unregisteredColoredComponents: 0,
    nativeProfilePixelMismatches: 0, nativeProfileMismatchExamples: [], shadeBands: expected.shades, expectedShadePixels: expected.shades.reduce((sum, band) => sum + band.pixels, 0), shadePixelMismatches: 0,
    highlightBands: expected.highlights, expectedHighlightPixels: expected.highlights.reduce((sum, band) => sum + band.pixels, 0), highlightPixelMismatches: 0,
    nativePixels: PERIOD * PERIOD,
  };
  if (image.width !== PERIOD || image.height !== PERIOD) return result;
  const colors = Array.from({ length: PERIOD * PERIOD }, (_, i) => rgb(image, i % PERIOD, Math.floor(i / PERIOD)));
  const at = (x, y) => colors[((y + PERIOD) % PERIOD) * PERIOD + ((x + PERIOD) % PERIOD)];
  const expAt = (x, y) => expected.colors[((y + PERIOD) % PERIOD) * PERIOD + ((x + PERIOD) % PERIOD)];
  const ownerAt = (x, y) => expected.owners[((y + PERIOD) % PERIOD) * PERIOD + ((x + PERIOD) % PERIOD)];
  for (let y = 0; y < PERIOD; y++) for (let x = 0; x < PERIOD; x++) {
    const actual2x2 = [at(x, y), at(x + 1, y), at(x, y + 1), at(x + 1, y + 1)].every(color => color === INK);
    const expected2x2 = [expAt(x, y), expAt(x + 1, y), expAt(x, y + 1), expAt(x + 1, y + 1)].every(color => color === INK);
    if (expected2x2) result.declaredRoundedInk2x2++;
    if (expected2x2 && !actual2x2) result.missingDeclaredInk2x2++;
    if (actual2x2) {
      result.toroidalInk2x2++;
      if (!expected2x2) {
        result.unregisteredInk2x2++;
        if (result.ink2x2Examples.length < 20) result.ink2x2Examples.push([x, y]);
      }
    }
    if (at(x, y) !== expAt(x, y)) {
      result.nativeProfilePixelMismatches++;
      if (result.nativeProfileMismatchExamples.length < 20) result.nativeProfileMismatchExamples.push({ pixel: [x, y], actual: hex(at(x, y)), expected: hex(expAt(x, y)) });
      const owner = ownerAt(x, y), face = expected.faces[owner];
      if (face?.shadeColor && y === face.origin[1] + face.dimensions[1] - 1) result.shadePixelMismatches++;
      if (face?.highlightColor && y === face.origin[1] && x >= face.origin[0] + face.highlightInsetPixels && x < face.origin[0] + face.dimensions[0] - face.highlightInsetPixels) result.highlightPixelMismatches++;
    }
    for (const [dx, dy] of [[1, 0], [0, 1]]) {
      const a = at(x, y), b = at(x + dx, y + dy);
      if (a !== INK && b !== INK && a !== b) {
        if (ownerAt(x, y) >= 0 && ownerAt(x, y) === ownerAt(x + dx, y + dy) && a === expAt(x, y) && b === expAt(x + dx, y + dy)) {
          result.allowedSameBrickFacetContacts++; continue;
        }
        result.directDifferentNonInkContacts++;
        if (result.contactExamples.length < 20) result.contactExamples.push({ from: [x, y], to: [(x + dx) % PERIOD, (y + dy) % PERIOD], colors: [hex(a), hex(b)] });
      }
    }
  }
  requireCheck(result.unregisteredInk2x2 === 0 && result.missingDeclaredInk2x2 === 0, 'Mortar 2x2 positions differ from declared radius1 corners, including repeat seams', { unregistered: result.unregisteredInk2x2, missing: result.missingDeclaredInk2x2, examples: result.ink2x2Examples });
  requireCheck(result.directDifferentNonInkContacts === 0, 'Different flat brick faces touch without mortar, including repeat seams', result.contactExamples);
  requireCheck(result.nativeProfilePixelMismatches === 0, 'Native brick colors, facet bands or rounded geometry differ from exact profile', { pixels: result.nativeProfilePixelMismatches, examples: result.nativeProfileMismatchExamples });
  requireCheck(result.expectedShadePixels > 0 && result.shadePixelMismatches === 0, 'Existing bottom facet shading was lost or altered', { expectedShadePixels: result.expectedShadePixels, mismatches: result.shadePixelMismatches });
  requireCheck(result.expectedHighlightPixels > 0 && result.highlightPixelMismatches === 0, 'Existing straight teal highlights were lost or altered', { expectedHighlightPixels: result.expectedHighlightPixels, mismatches: result.highlightPixelMismatches });
  for (const y of [0, 16, 32, 48]) {
    const missingInk = [];
    for (let x = 0; x < PERIOD; x++) if (at(x, y) !== INK) missingInk.push(x);
    const verticalPorts = [], irregularColumns = [], roundedCornerColumns = [];
    for (let x = 0; x < PERIOD; x++) {
      const inkRows = [];
      for (let dy = 1; dy < 16; dy++) if (at(x, y + dy) === INK) inkRows.push(y + dy);
      if (inkRows.length === 15) verticalPorts.push(x);
      else if (inkRows.length) {
        const expectedInkRows = [];
        for (let dy = 1; dy < 16; dy++) if (expAt(x, y + dy) === INK) expectedInkRows.push(y + dy);
        if (equalJSON(inkRows, expectedInkRows)) roundedCornerColumns.push({ x, inkRows });
        else irregularColumns.push({ x, inkRows, expectedInkRows });
      }
    }
    result.horizontalCourses.push({ y, missingInk, verticalPorts, roundedCornerColumns, irregularColumns });
    requireCheck(missingInk.length === 0, `Horizontal mortar course y=${y} is not continuous`, missingInk);
    requireCheck(equalJSON(verticalPorts, expected.courses.find(row => row.y === y)?.joints), `Course y=${y} vertical joints differ from registration`, verticalPorts);
    requireCheck(irregularColumns.length === 0, `Course y=${y} contains unregistered partial or stepped joints`, irregularColumns.slice(0, 12));
  }
  const seen = new Uint8Array(colors.length);
  for (let seed = 0; seed < colors.length; seed++) {
    if (seen[seed] || colors[seed] === INK) continue;
    const color = colors[seed], queue = [seed]; seen[seed] = 1;
    for (let i = 0; i < queue.length; i++) {
      const index = queue[i], x = index % PERIOD, y = Math.floor(index / PERIOD);
      for (const [nx, ny] of [[(x + 1) % PERIOD, y], [(x + PERIOD - 1) % PERIOD, y], [x, (y + 1) % PERIOD], [x, (y + PERIOD - 1) % PERIOD]]) {
        const neighbor = ny * PERIOD + nx;
        if (!seen[neighbor] && colors[neighbor] === color) { seen[neighbor] = 1; queue.push(neighbor); }
      }
    }
    const xs = cyclicSpan(queue.map(index => index % PERIOD));
    const ys = cyclicSpan(queue.map(index => Math.floor(index / PERIOD)));
    const rectangular = queue.length === xs.length * ys.length;
    const owners = [...new Set(queue.map(index => expected.owners[index]))];
    const expectedRegion = owners.length === 1 && owners[0] >= 0 ? expected.expectedRegions.get(`${owners[0]}/${color}`) : null;
    const matchesRegisteredFaceOrFacet = Boolean(expectedRegion && expectedRegion.length === queue.length && queue.every(index => expected.colors[index] === color));
    const component = { color: hex(color), origin: [xs.origin, ys.origin], dimensions: [xs.length, ys.length], pixels: queue.length, rectangular, brickOwners: owners, matchesRegisteredFaceOrFacet };
    result.coloredComponents.push(component);
    if (rectangular) result.rectangularColoredComponents++;
    else result.nonRectangularColoredComponents++;
    if (matchesRegisteredFaceOrFacet) result.registeredFaceOrFacetComponents++; else result.unregisteredColoredComponents++;
    requireCheck(matchesRegisteredFaceOrFacet, 'Colored component differs from its registered rounded face or facet band', component);
  }
  return result;
}
async function auditNativeDecorativeFaces(target, layout) {
  const result = { sources: [], facePixelsCompared: 0, facePixelMismatches: 0, bottomFacetPixels: 0, topHighlightPixels: 0, roundedCornerPixels: 0, paletteFailures: 0, alphaFailures: 0, mismatchExamples: [] };
  const allowed = new Set(target.allowedPalette.map(color => parseInt(color.slice(1), 16)));
  const shadeFor = { '#f4f4f4': '#ef7d57', '#ef7d57': '#5d275d' };
  for (const role of ['frame', 'ring']) {
    const filename = target.sourceTransforms?.[role]?.nativeSource ?? target.nativeSourceProfile?.[`${role}128`];
    requireCheck(typeof filename === 'string', `Native ${role} source is not declared`);
    if (!filename) continue;
    const image = await readImage(ROOT, filename);
    result.sources.push({ role, file: filename, pngSha256: sha(image.bytes), dimensions: [image.width, image.height] });
    requireCheck(image.width === 128 && image.height === 128, `Native ${role} source dimensions changed`, [image.width, image.height]);
    for (let y = 0; y < image.height; y++) for (let x = 0; x < image.width; x++) {
      const alpha = image.data[offset(image, x, y) + 3];
      if (![0, 255].includes(alpha)) result.alphaFailures++;
      if (alpha && !allowed.has(rgb(image, x, y))) result.paletteFailures++;
    }
    const rectangles = role === 'frame' ? layout.frameFaces : [...layout.frameFaces, ...layout.ringFaces];
    for (const r of rectangles) {
      const radius = r.cornerRadiusPixels ?? 0, inset = r.highlightInsetPixels ?? 2;
      requireCheck([0, 1].includes(radius), `Native ${role} face has an unregistered corner radius`, r);
      requireCheck((r.shadeColor ?? null) === (shadeFor[r.c] ?? null), `Native ${role} colored face lost its bottom facet`, r);
      if (r.highlightColor) requireCheck(r.c === '#5d275d' && r.w > 2 && r.highlightColor === '#ef7d57' && Number.isInteger(inset) && inset >= radius && r.w > 2 * inset, `Native ${role} knob highlight is unregistered`, r);
      for (let yy = 0; yy < r.h; yy++) for (let xx = 0; xx < r.w; xx++) {
        const x = r.x + xx, y = r.y + yy;
        const corner = radius === 1 && (xx === 0 || xx === r.w - 1) && (yy === 0 || yy === r.h - 1);
        const capJoint = (layout.capJointColumns ?? []).includes(x) && ((y >= 5 && y <= 10) || (y >= 117 && y <= 120));
        const highlighted = r.highlightColor && yy === 0 && xx >= inset && xx < r.w - inset;
        const shaded = r.shadeColor && yy === r.h - 1;
        const color = corner || capJoint ? '#1a1c2c' : highlighted ? r.highlightColor : shaded ? r.shadeColor : r.c;
        const expected = parseInt(color.slice(1), 16);
        result.facePixelsCompared++;
        if (!corner && !capJoint && highlighted) result.topHighlightPixels++;
        if (!corner && !capJoint && shaded) result.bottomFacetPixels++;
        if (corner) result.roundedCornerPixels++;
        if (rgb(image, x, y) !== expected || image.data[offset(image, x, y) + 3] !== 255) {
          result.facePixelMismatches++;
          if (result.mismatchExamples.length < 20) result.mismatchExamples.push({ role, pixel: [x, y], expected: color, actual: hex(rgb(image, x, y)) });
        }
      }
    }
  }
  requireCheck(result.sources.length === 2 && result.facePixelsCompared > 0, 'Native decorative source audit lacks frame/ring coverage');
  requireCheck(result.facePixelMismatches === 0, 'Native caps, braces, facets or rounded face corners differ from registration', result.mismatchExamples);
  requireCheck(result.paletteFailures === 0 && result.alphaFailures === 0, 'Native decorative source palette/alpha contract failed', { paletteFailures: result.paletteFailures, alphaFailures: result.alphaFailures });
  requireCheck(result.bottomFacetPixels > 0 && result.topHighlightPixels > 0, 'Native decorative shadows or knob highlights are missing', { bottomFacetPixels: result.bottomFacetPixels, topHighlightPixels: result.topHighlightPixels });
  return result;
}

try {
  const lookup = await json(ROOT, 'Data/sprite_lookup.json');
  const beforeLookup = await json(BASELINE, 'Data/sprite_lookup.json');
  requireCheck(lookup.artVersion === 4, 'Expected freshly baked artVersion4 lookup', lookup.artVersion);
  requireCheck(lookup.contractId === 'ANIMOL_FREE_SHAPE_BLOB47_V1', 'Topology contract changed', lookup.contractId);
  requireCheck(lookup.styles.length === 20 && lookup.cellPixels === CELL && lookup.pixelsPerUnit === CELL, 'Style/cell/PPU contract changed');
  requireCheck(equalJSON(lookup.styles.map(style => style.styleId), beforeLookup.styles.map(style => style.styleId)), 'Style identity/order contract changed');
  for (const key of ['canonicalMasks', 'rawToCanonical', 'rawToIndex', 'phase']) requireCheck(equalJSON(lookup[key], beforeLookup[key]), `Topology/phase contract changed: ${key}`);
  requireCheck(lookup.canonicalMasks.length === 47, 'Expected 47 canonical masks');
  const target = lookup.styles.find(style => style.styleId === TARGET);
  if (!target) throw new Error('T01_A missing from lookup');
  const palette = new Set(lookup.palette.map(value => parseInt(value.slice(1), 16)));
  const targetPalette = new Set(target.allowedPalette.map(value => parseInt(value.slice(1), 16)));
  const native = await readImage(ROOT, NATIVE);
  requireCheck(target.materialSampling.nativeMaterialSource === NATIVE, 'Lookup must declare the exact audited native material source', target.materialSampling.nativeMaterialSource);
  const layout = await json(ROOT, LAYOUT), declaredNative = declaredNativeGeometry(layout);
  checks.nativeRepeat = { file: NATIVE, pngSha256: sha(native.bytes), ...auditNativeGeometry(native, declaredNative) };
  checks.nativeDecorativeFaces = await auditNativeDecorativeFaces(target, layout);
  requireCheck(native.width === PERIOD && native.height === PERIOD, 'Native brick master must be 64x64', checks.nativeRepeat.dimensions);
  if (native.width !== PERIOD || native.height !== PERIOD) throw new Error('Cannot compare phase pixels without native64 master');
  let nativePaletteFailures = 0, nativeAlphaFailures = 0;
  for (let y = 0; y < PERIOD; y++) for (let x = 0; x < PERIOD; x++) {
    if (!targetPalette.has(rgb(native, x, y))) nativePaletteFailures++;
    if (native.data[offset(native, x, y) + 3] !== 255) nativeAlphaFailures++;
  }
  checks.nativeRepeat.paletteFailures = nativePaletteFailures;
  checks.nativeRepeat.alphaFailures = nativeAlphaFailures;
  requireCheck(nativePaletteFailures === 0 && nativeAlphaFailures === 0, 'Native repeat has non-palette or non-opaque pixels', { nativePaletteFailures, nativeAlphaFailures });

  const field = await readImage(ROOT, target.materialSampling.repeatedField);
  let fieldMismatches = 0;
  requireCheck(field.width === 128 && field.height === 128, 'Repeated material field must remain 128x128', [field.width, field.height]);
  for (let y = 0; y < field.height; y++) for (let x = 0; x < field.width; x++) {
    if (!pixelEqual(field, offset(field, x, y), native, offset(native, (x + 32) % PERIOD, (y + 32) % PERIOD))) fieldMismatches++;
  }
  checks.repeatedField = { file: target.materialSampling.repeatedField, dimensions: [field.width, field.height], pixelsCompared: field.width * field.height, mismatches: fieldMismatches };
  requireCheck(fieldMismatches === 0, '128px field is not an exact phase-registered repetition of native64', fieldMismatches);

  const preserved = { styles: [], pngsCompared: 0, bytesCompared: 0, changedFiles: [], filenameSetDifferences: [] };
  const oldDigest = crypto.createHash('sha256'), newDigest = crypto.createHash('sha256');
  for (const style of lookup.styles.filter(style => style.styleId !== TARGET)) {
    const before = await styleFiles(BASELINE, style.styleId), after = await styleFiles(ROOT, style.styleId);
    if (!equalJSON(before, after)) preserved.filenameSetDifferences.push(style.styleId);
    requireCheck(equalJSON(before, after), `Non-target style PNG file set changed: ${style.styleId}`);
    let bytesCompared = 0;
    for (const filename of before) {
      const a = await fs.readFile(path.join(BASELINE, filename)), b = await fs.readFile(path.join(ROOT, filename));
      oldDigest.update(`${filename}\n`); oldDigest.update(a); newDigest.update(`${filename}\n`); newDigest.update(b);
      preserved.pngsCompared++; preserved.bytesCompared += a.length; bytesCompared += a.length;
      if (!a.equals(b)) preserved.changedFiles.push(filename);
    }
    preserved.styles.push({ styleId: style.styleId, pngsCompared: before.length, bytesCompared });
  }
  preserved.baselineDigest = oldDigest.digest('hex'); preserved.currentDigest = newDigest.digest('hex');
  checks.preservedOtherStyles = preserved;
  requireCheck(preserved.styles.length === 19 && preserved.pngsCompared === 5605, 'Expected all 5605 PNGs from the other 19 styles', { styles: preserved.styles.length, pngs: preserved.pngsCompared });
  requireCheck(preserved.changedFiles.length === 0, 'Non-target style PNG bytes changed', preserved.changedFiles.slice(0, 20));

  const spriteAudit = { sprites: 0, targetSprites: 0, targetChanged: 0, targetUnchanged: [], targetHashes: [],
    paletteFailures: 0, alphaFailures: 0, nonOpaqueCellPixels: 0, atlasPixelMismatches: 0,
    bodyCellsCompared: 0, bodyCellPixelsCompared: 0, bodyCellMismatches: 0, outerContourFailures: 0 };
  const targetImages = new Map();
  for (const style of lookup.styles) {
    const beforeStyle = beforeLookup.styles.find(candidate => candidate.styleId === style.styleId);
    requireCheck(style.variants.length === 4, `Expected four art phases for ${style.styleId}`, style.variants.length);
    requireCheck(equalJSON(style.variants.map(({ id, phaseX, phaseY }) => ({ id, phaseX, phaseY })), beforeStyle?.variants.map(({ id, phaseX, phaseY }) => ({ id, phaseX, phaseY }))), `Variant phase registrations changed: ${style.styleId}`);
    for (const variant of style.variants) {
      const beforeVariant = beforeStyle?.variants.find(candidate => candidate.id === variant.id);
      requireCheck(equalJSON(variant.cells, beforeVariant?.cells), `Cell asset/index/atlas-rect contract changed: ${style.styleId}/${variant.id}`);
      requireCheck(equalJSON(variant.cells.map(cell => cell.mask), lookup.canonicalMasks), `Canonical cell list changed: ${style.styleId}/${variant.id}`);
      requireCheck([0, 1].includes(variant.phaseX) && [0, 1].includes(variant.phaseY), `Invalid phase: ${style.styleId}/${variant.id}`);
      const atlas = await readImage(ROOT, variant.atlas);
      requireCheck(atlas.width === 256 && atlas.height === 192 && equalJSON(variant.atlasPixels, [256, 192]), `Atlas dimensions changed: ${style.styleId}/${variant.id}`, [atlas.width, atlas.height]);
      for (const cell of variant.cells) {
        const image = await readImage(ROOT, cell.file); spriteAudit.sprites++;
        requireCheck(image.width === CELL && image.height === CELL, `Cell dimensions changed: ${cell.file}`, [image.width, image.height]);
        for (let y = 0; y < image.height; y++) for (let x = 0; x < image.width; x++) {
          const index = offset(image, x, y), alpha = image.data[index + 3];
          if (![0, 255].includes(alpha)) spriteAudit.alphaFailures++;
          if (alpha !== 255) spriteAudit.nonOpaqueCellPixels++;
          if (alpha && !palette.has(rgb(image, x, y))) spriteAudit.paletteFailures++;
          if (cell.rect.x + x >= atlas.width || cell.rect.y + y >= atlas.height || !pixelEqual(image, index, atlas, offset(atlas, cell.rect.x + x, cell.rect.y + y))) spriteAudit.atlasPixelMismatches++;
          if (style.styleId !== TARGET) continue;
          if ((!(cell.mask & 1) && y === 0) || (!(cell.mask & 16) && y === 31) || (!(cell.mask & 64) && x === 0) || (!(cell.mask & 4) && x === 31)) {
            if (rgb(image, x, y) !== INK || alpha !== 255) spriteAudit.outerContourFailures++;
          }
          if (cell.mask === 255) {
            spriteAudit.bodyCellPixelsCompared++;
            if (!pixelEqual(image, index, native, offset(native, (variant.phaseX * CELL + x) % PERIOD, (variant.phaseY * CELL + y) % PERIOD))) spriteAudit.bodyCellMismatches++;
          }
        }
        if (style.styleId !== TARGET) continue;
        targetImages.set(cell.file, { image, variant, cell }); spriteAudit.targetSprites++;
        if (cell.mask === 255) spriteAudit.bodyCellsCompared++;
        const before = await fs.readFile(path.join(BASELINE, cell.file)), changed = !before.equals(image.bytes);
        spriteAudit.targetHashes.push({ file: cell.file, beforeSha256: sha(before), afterSha256: sha(image.bytes), changed });
        if (changed) spriteAudit.targetChanged++; else spriteAudit.targetUnchanged.push(cell.file);
        if (cell.mask === 255) requireCheck(changed, `Plain body phase was not rebaked: ${cell.file}`);
      }
    }
  }
  checks.spriteContract = spriteAudit;
  requireCheck(spriteAudit.sprites === 3760 && spriteAudit.targetSprites === 188, 'Sprite audit coverage incomplete', { all: spriteAudit.sprites, target: spriteAudit.targetSprites });
  requireCheck(spriteAudit.paletteFailures === 0 && spriteAudit.alphaFailures === 0 && spriteAudit.nonOpaqueCellPixels === 0, 'Cell palette or opaque alpha contract failed', { palette: spriteAudit.paletteFailures, alpha: spriteAudit.alphaFailures, nonOpaque: spriteAudit.nonOpaqueCellPixels });
  requireCheck(spriteAudit.atlasPixelMismatches === 0, 'Atlases differ from exported cell pixels', spriteAudit.atlasPixelMismatches);
  requireCheck(spriteAudit.outerContourFailures === 0, 'Exposed T01_A outer contour lost ink', spriteAudit.outerContourFailures);
  requireCheck(spriteAudit.bodyCellsCompared === 4 && spriteAudit.bodyCellMismatches === 0, 'The four body phases differ from native repeat', { phases: spriteAudit.bodyCellsCompared, mismatches: spriteAudit.bodyCellMismatches });

  const overlay = target.junctionOverlaySampling;
  requireCheck(Boolean(overlay), 'Target lookup lacks semantic junction overlay policy');
  requireCheck(equalJSON(overlay?.backgroundOriginNormalizedPixels, [32, 32]) && equalJSON(overlay?.backgroundPeriodPixels, [64, 64]), 'Junction overlay uses a different body registration', overlay);
  const maskAudit = { input: 'Data/sprite_lookup.json styles[T01_A].variants[].junctionOverlayMasks',
    quarterMasksCompared: 0, cellsCompared: 0, bodyPixelsCompared: 0, overlayPixels: 0,
    pixelMismatches: 0, mismatchExamples: [], invalidMasks: [], interiorOverlayPixels: 0 };
  for (const variant of target.variants) {
    const records = variant.junctionOverlayMasks ?? {};
    requireCheck(equalJSON(Object.keys(records).sort(), [...QUARTER_MODULE_KEYS].sort()), `Semantic overlay masks incomplete: ${variant.id}`, Object.keys(records));
    for (const key of QUARTER_MODULE_KEYS) {
      const rows = records[key];
      const valid = Array.isArray(rows) && rows.length === 16 && rows.every(row => typeof row === 'string' && /^[01]{16}$/u.test(row));
      if (!valid) { maskAudit.invalidMasks.push(`${variant.id}/${key}`); continue; }
      maskAudit.quarterMasksCompared++;
      if (key.endsWith('_IN')) maskAudit.interiorOverlayPixels += rows.join('').split('1').length - 1;
    }
  }
  for (const [filename, { image, variant, cell }] of targetImages) {
    const quadrants = getQuadrants(cell.mask), records = variant.junctionOverlayMasks ?? {};
    if (quadrants.some(q => maskAudit.invalidMasks.includes(`${variant.id}/${q.moduleKey}`) || !records[q.moduleKey])) continue;
    maskAudit.cellsCompared++;
    for (const q of quadrants) for (let qy = 0; qy < 16; qy++) for (let qx = 0; qx < 16; qx++) {
      if (records[q.moduleKey][qy][qx] === '1') { maskAudit.overlayPixels++; continue; }
      const x = q.column * 16 + qx, y = q.row * 16 + qy;
      maskAudit.bodyPixelsCompared++;
      if (!pixelEqual(image, offset(image, x, y), native, offset(native, (variant.phaseX * CELL + x) % PERIOD, (variant.phaseY * CELL + y) % PERIOD))) {
        maskAudit.pixelMismatches++;
        if (maskAudit.mismatchExamples.length < 20) maskAudit.mismatchExamples.push({ file: filename, pixel: [x, y], module: q.moduleKey });
      }
    }
  }
  checks.sharedBodyOutsideAuthoredOverlay = maskAudit;
  requireCheck(maskAudit.cellsCompared === 188 && maskAudit.quarterMasksCompared === 80, 'Shared-body semantic audit lacks complete target coverage', { cells: maskAudit.cellsCompared, quarterMasks: maskAudit.quarterMasksCompared, invalid: maskAudit.invalidMasks });
  requireCheck(maskAudit.bodyPixelsCompared >= 4096, 'Semantic body masks have insufficient non-overlay coverage', maskAudit.bodyPixelsCompared);
  requireCheck(maskAudit.interiorOverlayPixels === 0, 'Plain IN modules contain decoration overlay', maskAudit.interiorOverlayPixels);
  requireCheck(maskAudit.pixelMismatches === 0, 'Body outside authored overlay differs from native phase', { mismatches: maskAudit.pixelMismatches, examples: maskAudit.mismatchExamples });
} catch (error) {
  requireCheck(false, 'Audit could not complete', String(error?.stack ?? error));
}

const report = {
  schemaVersion: 1, artVersion: 4, generatedAt: new Date().toISOString(),
  result: failureCount ? 'FAIL' : 'PASS', failureCount, errors, checks,
  baseline: local(BASELINE), durationMs: Math.round(performance.now() - started),
  scope: 'Exact native64 registered rounded faces, retained facet shading, straight mortar outside declared corner zones, and phase/body agreement; palette/alpha/atlas/topology contract; other19 PNG byte preservation. Native mortar checks apply to brick material, not decorative beams or endcaps.',
  limitations: [
    'Semantic body coverage is checked against the builder-declared overlay masks; the artistic appropriateness of those masks requires visual review.',
    'A passing native material check does not certify every decorative curve, bracket or whole-map joint as one pixel wide.',
    'No Unity, map-editor transaction, collider or device rendering verification is implied.',
  ],
};
await fs.mkdir(path.dirname(REPORT), { recursive: true });
await fs.writeFile(REPORT, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ result: report.result, failureCount, errors, native: checks.nativeRepeat && {
  ink2x2: checks.nativeRepeat.toroidalInk2x2, directNonInkContacts: checks.nativeRepeat.directDifferentNonInkContacts,
  declaredRoundedInk2x2: checks.nativeRepeat.declaredRoundedInk2x2, unregisteredInk2x2: checks.nativeRepeat.unregisteredInk2x2,
  expectedShadePixels: checks.nativeRepeat.expectedShadePixels, shadePixelMismatches: checks.nativeRepeat.shadePixelMismatches,
  expectedHighlightPixels: checks.nativeRepeat.expectedHighlightPixels, highlightPixelMismatches: checks.nativeRepeat.highlightPixelMismatches,
  rectangularComponents: checks.nativeRepeat.rectangularColoredComponents, nonRectangularComponents: checks.nativeRepeat.nonRectangularColoredComponents,
  registeredFaceOrFacetComponents: checks.nativeRepeat.registeredFaceOrFacetComponents,
  courses: checks.nativeRepeat.horizontalCourses.map(({ y, verticalPorts }) => ({ y, verticalPorts })),
}, preservedOtherStylePngs: checks.preservedOtherStyles?.pngsCompared, changedTargetSprites: checks.spriteContract?.targetChanged,
  checkedSprites: checks.spriteContract?.sprites, bodyPixelsCompared: checks.sharedBodyOutsideAuthoredOverlay?.bodyPixelsCompared,
  report: local(REPORT), durationMs: report.durationMs }, null, 2));
if (failureCount) process.exitCode = 1;
