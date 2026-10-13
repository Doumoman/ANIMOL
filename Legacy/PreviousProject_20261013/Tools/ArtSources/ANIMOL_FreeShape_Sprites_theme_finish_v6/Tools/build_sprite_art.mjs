/**
 * Bake ANIMOL free-shape cells from clean, grid-aligned authored image panels.
 * All visible pixels come from source artwork crops. Geometry here only masks,
 * slices, palette-converts, and assembles those authored pixels.
 * Run: node Tools/build_sprite_art.mjs [Data/source_rects.json]
 */
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { CANONICAL_MASKS, RAW_TO_CANONICAL, RAW_TO_INDEX, getQuadrants,
  occupancyFromRows, resolveCell } from './terrain_topology.mjs';
import { resolveVariant, motifPlacements } from './terrain_composition.mjs';

const require = createRequire(import.meta.url);
const sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const manifestPath = path.resolve(process.argv[2] ?? path.join(ROOT, 'Data/source_rects.json'));
const manifest = JSON.parse(await fs.readFile(manifestPath, 'utf8'));
const ART_VERSION = manifest.artVersion ?? 2;
if (!Number.isInteger(ART_VERSION) || ART_VERSION < 1) throw new Error(`Invalid artVersion: ${ART_VERSION}`);
const CELL = 32, QUARTER = 16, PANEL = 128, PERIOD = 64;
const DECOR_FRAME = manifest.decorativeFramePixels ?? PANEL;
const DECOR_RING = manifest.decorativeRingPixels ?? PANEL;
if (![64, 128].includes(DECOR_FRAME) || ![64, 72, 128].includes(DECOR_RING)) throw new Error('Decorative source sizes must use the authored sampling profiles: frame 64/128 and ring 64/72/128.');
const TRIM = manifest.trimWidth ?? 16;
const INNER = manifest.innerExtent ?? 16;
const INNER_TRIM = manifest.innerTrimWidth ?? 16;
function styleSamplingSettings(definition) {
  if (definition.junctionOverlayProfile && (!['decoration-only-v4', 'decoration-roles-v5', 'native-quarter-roles-v6'].includes(definition.junctionOverlayProfile) || (definition.junctionOverlayProfile !== 'native-quarter-roles-v6' && definition.styleId !== 'T01_A'))) throw new Error(`Unsupported junction overlay profile for ${definition.styleId}: ${definition.junctionOverlayProfile}`);
  if (definition.junctionOverlayProfile === 'decoration-roles-v5' && (!definition.nativeFrameDecorationPath || !definition.nativeRingDecorationPath)) throw new Error('v5 requires both authoritative architectural decoration planes.');
  if ([definition.nativeFrameDecorationPath, definition.nativeRingDecorationPath].some(Boolean) && definition.junctionOverlayProfile !== 'decoration-roles-v5') throw new Error('Native decoration sources require the v5 provenance profile.');
  if ([definition.nativeMaterialPath, definition.nativeMaterialReferencePath, definition.nativeFramePath, definition.nativeRingPath].some(Boolean) && definition.styleId !== 'T01_A' && definition.junctionOverlayProfile !== 'native-quarter-roles-v6') throw new Error('Native junction source overrides require authoritative native role registration.');
  if (definition.junctionOverlayProfile === 'native-quarter-roles-v6' && (!definition.nativeMaterialPath || !definition.nativeQuarterRolePaths || Object.keys(definition.nativeQuarterRolePaths).length !== 20)) throw new Error('v6 requires a native 64px material and all 20 semantic quarter role planes.');
  const framePixels = definition.decorativeFramePixels ?? DECOR_FRAME;
  const ringPixels = definition.decorativeRingPixels ?? DECOR_RING;
  if (![64, 128].includes(framePixels) || ![64, 72, 128].includes(ringPixels)) throw new Error(`Invalid style source sampling sizes for ${definition.styleId}`);
  const settings = { framePixels, ringPixels, trimWidth: definition.trimWidth ?? TRIM, innerExtent: definition.innerExtent ?? INNER, innerTrimWidth: definition.innerTrimWidth ?? INNER_TRIM, horizontalPeriodPixels: definition.horizontalFramePeriodPixels ?? manifest.horizontalFramePeriodPixels ?? (framePixels === 64 ? 32 : PERIOD), sidePeriodPixels: definition.sideFramePeriodPixels ?? manifest.sideFramePeriodPixels ?? (framePixels === 64 ? 32 : PERIOD), completeOuterCornerPatches: definition.completeOuterCornerPatches ?? true, completeInnerCornerPatches: definition.completeInnerCornerPatches ?? true };
  if (definition.junctionOverlayProfile) settings.junctionOverlayProfile = definition.junctionOverlayProfile;
  if ((definition.nativeFramePath && framePixels !== PANEL) || (definition.nativeRingPath && ringPixels !== PANEL)) throw new Error('Native frame/ring overrides must retain their authored 128px sampling.');
  if ([settings.trimWidth, settings.innerExtent, settings.innerTrimWidth].some(value => !Number.isInteger(value) || value < 1 || value > QUARTER) || [settings.horizontalPeriodPixels, settings.sidePeriodPixels].some(value => !Number.isInteger(value) || value < QUARTER || value > framePixels || framePixels % value)) throw new Error(`Invalid style sampling profile: ${JSON.stringify(settings)}`);
  return settings;
}
const paletteHex = manifest.palette ?? [
  '#1a1c2c', '#5d275d', '#b13e53', '#ef7d57', '#ffcd75', '#a7f070',
  '#38b764', '#257179', '#29366f', '#3b5dc9', '#41a6f6', '#73eff7',
  '#f4f4f4', '#94b0c2', '#566c86', '#333c57',
];
if (paletteHex.length !== 16) throw new Error('Exactly 16 palette colors are required.');
const palette = paletteHex.map(hex => {
  if (!/^#[0-9a-f]{6}$/iu.test(hex)) throw new Error(`Invalid palette color: ${hex}`);
  return [parseInt(hex.slice(1, 3), 16), parseInt(hex.slice(3, 5), 16), parseInt(hex.slice(5, 7), 16)];
});
const DEFAULT_THEME_EXCLUSIONS = { T01: [5, 6], T02: [1, 5, 6], T03: [5, 6, 7, 9, 10, 11], T04: [], T05: [2, 3, 4, 5, 6] };
let activePaletteIndices = palette.map((_, index) => index);
let activePaletteKey = 'all';
const nearestCache = new Map();
const local = p => path.relative(ROOT, p).split(path.sep).join('/');
const absoluteSource = p => path.isAbsolute(p) ? p : path.resolve(ROOT, p);
const rgba = (width, height) => ({ width, height, data: Buffer.alloc(width * height * 4) });
const pixelIndex = (img, x, y) => (y * img.width + x) * 4;
const imageCache = new Map();
async function loadImage(filename) {
  const resolved = absoluteSource(filename);
  if (!imageCache.has(resolved)) imageCache.set(resolved, sharp(resolved).ensureAlpha().raw().toBuffer({ resolveWithObject: true }).then(({ data, info }) => ({ width: info.width, height: info.height, data })));
  return imageCache.get(resolved);
}
async function loadNativePixels(filename, pixels, solid = false) {
  const source = await loadImage(filename);
  if (source.width !== pixels || source.height !== pixels) throw new Error(`Native source must be ${pixels}x${pixels}: ${filename}, got ${source.width}x${source.height}`);
  const allowed = new Set(activePaletteIndices.map(index => palette[index].join(',')));
  for (let offset = 0; offset < source.data.length; offset += 4) {
    const alpha = source.data[offset + 3];
    if ((solid && alpha !== 255) || (alpha !== 0 && alpha !== 255)) throw new Error(`Native source has invalid alpha at pixel ${offset / 4}: ${filename}`);
    if (alpha && !allowed.has([...source.data.subarray(offset, offset + 3)].join(','))) throw new Error(`Native source color is outside the declared style palette at pixel ${offset / 4}: ${filename}`);
    if (!alpha && source.data.subarray(offset, offset + 3).some(Boolean)) throw new Error(`Native source has colored transparent pixels at pixel ${offset / 4}: ${filename}`);
  }
  return { ...source, data: Buffer.from(source.data), fit: { scale: 1, offsetX: 0, offsetY: 0, sourceWidth: pixels, sourceHeight: pixels, mode: 'native', resamplingKernel: 'nearest', spatialFilters: [], nativeSource: local(absoluteSource(filename)), nativePixelsCopiedExactly: true }, bounds: { x: 0, y: 0, right: pixels, bottom: pixels } };
}
function copyPixel(from, fx, fy, to, tx, ty) {
  const a = pixelIndex(from, fx, fy), b = pixelIndex(to, tx, ty);
  for (let c = 0; c < 4; c++) to.data[b + c] = from.data[a + c];
}
function crop(img, rect) {
  const r = { x: Math.round(rect.x), y: Math.round(rect.y), w: Math.round(rect.w ?? rect.width), h: Math.round(rect.h ?? rect.height) };
  if (r.w <= 0 || r.h <= 0 || r.x < 0 || r.y < 0 || r.x + r.w > img.width || r.y + r.h > img.height) throw new Error(`Crop outside source: ${JSON.stringify(r)} for ${img.width}x${img.height}`);
  const result = rgba(r.w, r.h);
  for (let y = 0; y < r.h; y++) for (let x = 0; x < r.w; x++) copyPixel(img, r.x + x, r.y + y, result, x, y);
  return result;
}
async function nearestResize(img, width, height) {
  const data = await sharp(img.data, { raw: { width: img.width, height: img.height, channels: 4 } }).resize(width, height, { kernel: 'nearest', fit: 'fill' }).raw().toBuffer();
  return { width, height, data };
}
function integerStepResize(img, width, height, phase = [0, 0]) {
  const stepX = img.width / width, stepY = img.height / height;
  if (!Number.isInteger(stepX) || !Number.isInteger(stepY) || phase.some((value, index) => !Number.isInteger(value) || value < 0 || value >= (index ? stepY : stepX))) throw new Error(`Invalid native nearest sampling phase: ${JSON.stringify(phase)}`);
  const result = rgba(width, height);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) copyPixel(img, x * stepX + phase[0], y * stepY + phase[1], result, x, y);
  return result;
}
function nearestColor(r, g, b) {
  const key = `${activePaletteKey}/${(r << 16) | (g << 8) | b}`;
  if (nearestCache.has(key)) return nearestCache.get(key);
  let best = 0, bestDistance = Infinity;
  for (const n of activePaletteIndices) {
    const p = palette[n], dr = r - p[0], dg = g - p[1], db = b - p[2];
    const distance = dr * dr + dg * dg + db * db;
    if (distance < bestDistance) { bestDistance = distance; best = n; }
  }
  nearestCache.set(key, best);
  return best;
}
/** Remove only matte pixels connected to the image boundary, retaining enclosed pale details. */
function removeBoundaryMatte(img, color = [255, 255, 255], tolerance = 36) {
  const visited = new Uint8Array(img.width * img.height), queue = [];
  const eligible = index => {
    const p = index * 4;
    return img.data[p + 3] === 0 || Math.max(...color.map((v, c) => Math.abs(img.data[p + c] - v))) <= tolerance;
  };
  function add(x, y) {
    const index = y * img.width + x;
    if (visited[index] || !eligible(index)) return;
    visited[index] = 1; queue.push(index);
  }
  for (let x = 0; x < img.width; x++) { add(x, 0); add(x, img.height - 1); }
  for (let y = 0; y < img.height; y++) { add(0, y); add(img.width - 1, y); }
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const index = queue[cursor], x = index % img.width, y = Math.floor(index / img.width);
    img.data[index * 4 + 3] = 0;
    if (x) add(x - 1, y); if (x + 1 < img.width) add(x + 1, y);
    if (y) add(x, y - 1); if (y + 1 < img.height) add(x, y + 1);
  }
}
function quantize(img, forceSolid = false) {
  for (let p = 0; p < img.data.length; p += 4) {
    const alpha = forceSolid || img.data[p + 3] >= (manifest.alphaThreshold ?? 128) ? 255 : 0;
    if (!alpha) { img.data.fill(0, p, p + 4); continue; }
    const color = palette[nearestColor(img.data[p], img.data[p + 1], img.data[p + 2])];
    img.data[p] = color[0]; img.data[p + 1] = color[1]; img.data[p + 2] = color[2]; img.data[p + 3] = 255;
  }
  return img;
}
function rolePaletteProfile(img, preserveInk = false, options = {}) {
  const minimumComponent = options.minimumComponentPixels ?? 32;
  const minimumSpan = options.minimumSpanPixels ?? 32;
  const minimumTotal = options.minimumTotalPixels ?? Infinity;
  const seen = new Uint8Array(img.width * img.height), colors = new Map();
  const colorAt = index => img.data.subarray(index * 4, index * 4 + 3).toString('hex');
  for (let index = 0; index < seen.length; index++) {
    if (!img.data[index * 4 + 3]) continue;
    const color = colorAt(index);
    if (!colors.has(color)) colors.set(color, { color: `#${color}`, pixelCount: 0, largestComponent: 0, maxSpanX: 0, maxSpanY: 0, components: 0 });
    colors.get(color).pixelCount++;
    if (seen[index]) continue;
    const queue = [index]; seen[index] = 1;
    let left = img.width, top = img.height, right = -1, bottom = -1;
    for (let cursor = 0; cursor < queue.length; cursor++) {
      const current = queue[cursor], x = current % img.width, y = Math.floor(current / img.width);
      left = Math.min(left, x); right = Math.max(right, x); top = Math.min(top, y); bottom = Math.max(bottom, y);
      for (const neighbor of [x ? current - 1 : -1, x + 1 < img.width ? current + 1 : -1, y ? current - img.width : -1, y + 1 < img.height ? current + img.width : -1]) {
        if (neighbor < 0 || seen[neighbor] || !img.data[neighbor * 4 + 3] || colorAt(neighbor) !== color) continue;
        seen[neighbor] = 1; queue.push(neighbor);
      }
    }
    const record = colors.get(color);
    record.components++; record.largestComponent = Math.max(record.largestComponent, queue.length);
    record.maxSpanX = Math.max(record.maxSpanX, right - left + 1); record.maxSpanY = Math.max(record.maxSpanY, bottom - top + 1);
  }
  const baseline = [...colors.values()].sort((a, b) => b.pixelCount - a.pixelCount);
  const selected = baseline.filter(record => record.largestComponent >= minimumComponent || record.maxSpanX >= minimumSpan || record.maxSpanY >= minimumSpan || record.pixelCount >= minimumTotal || (preserveInk && record.color === paletteHex[0])).map(record => record.color);
  if (!selected.length && baseline.length) selected.push(baseline[0].color);
  return { method: 'Palette role stabilization: retain colors used by authored continuous clusters or complete band color totals, without spatial filtering or pixel deletion.', minimumComponentPixels: minimumComponent, minimumSpanPixels: minimumSpan, minimumTotalPixels: Number.isFinite(minimumTotal) ? minimumTotal : null, baseline, selectedColors: selected, preservesAdditionalAuthoredColors: selected.length > 3 };
}
function quantizeWithPalette(img, colors, forceSolid = false) {
  const previousIndices = activePaletteIndices, previousKey = activePaletteKey;
  activePaletteIndices = colors.map(color => paletteHex.map(hex => hex.toLowerCase()).indexOf(color.toLowerCase()));
  if (!activePaletteIndices.length || activePaletteIndices.some(index => index < 0)) throw new Error(`Invalid role palette: ${JSON.stringify(colors)}`);
  activePaletteKey = activePaletteIndices.join(',');
  try { return quantize(img, forceSolid); }
  finally { activePaletteIndices = previousIndices; activePaletteKey = previousKey; }
}
/** Pixel-art export: one uniform nearest-neighbor scale, with integer bounds. */
async function fitPanel(img, mode = 'contain') {
  if (manifest.requireSquarePanels !== false && img.width !== img.height) throw new Error(`Clean art panels must be square; got ${img.width}x${img.height}. Locate the whole authored square instead of fitting a non-square crop.`);
  const scale = mode === 'cover' ? Math.max(PANEL / img.width, PANEL / img.height) : Math.min(PANEL / img.width, PANEL / img.height);
  const offsetX = Math.floor((PANEL - Math.round(img.width * scale)) / 2);
  const offsetY = Math.floor((PANEL - Math.round(img.height * scale)) / 2);
  const data = img.width === PANEL && img.height === PANEL ? Buffer.from(img.data) : await sharp(img.data, { raw: { width: img.width, height: img.height, channels: 4 } }).resize(PANEL, PANEL, { fit: mode, kernel: 'nearest', background: { r: 0, g: 0, b: 0, alpha: 0 } }).raw().toBuffer();
  const result = { width: PANEL, height: PANEL, data };
  result.fit = { scale, offsetX, offsetY, sourceWidth: img.width, sourceHeight: img.height, mode, resamplingKernel: 'nearest', spatialFilters: [] };
  result.bounds = {
    x: Math.max(0, offsetX), y: Math.max(0, offsetY),
    right: Math.min(PANEL, offsetX + Math.round(img.width * scale)), bottom: Math.min(PANEL, offsetY + Math.round(img.height * scale)),
  };
  return result;
}
async function panel(source, rect, solid, mode = 'contain') { return quantize(await fitPanel(crop(source, rect), mode), solid); }
function registerFrameBounds(frame, explicitBounds) {
  if (explicitBounds) {
    const bounds = { x: explicitBounds.x, y: explicitBounds.y, right: explicitBounds.x + explicitBounds.w, bottom: explicitBounds.y + explicitBounds.h };
    if (Object.values(bounds).some(value => !Number.isInteger(value)) || bounds.x < 0 || bounds.y < 0 || bounds.right > frame.width || bounds.bottom > frame.height || bounds.right - bounds.x < 32 || bounds.bottom - bounds.y < 32) throw new Error(`Invalid explicit frame content bounds: ${JSON.stringify(bounds)}`);
    frame.bounds = bounds;
    frame.fit.contentBounds = { ...bounds, registration: 'explicit source registration' };
    return frame;
  }
  // Image generation can leave a few opaque fringe pixels outside the authored
  // rectangular body. Locate the dense body rather than sampling those fringes
  // as a side. This changes only crop coordinates, never authored pixel colors.
  const thresholdX = Math.ceil(frame.width * (manifest.frameBodyCoverage ?? 0.8)), thresholdY = Math.ceil(frame.height * (manifest.frameBodyCoverage ?? 0.8));
  const columns = Array(frame.width).fill(0), rows = Array(frame.height).fill(0);
  for (let y = 0; y < frame.height; y++) for (let x = 0; x < frame.width; x++) if (frame.data[pixelIndex(frame, x, y) + 3]) { columns[x]++; rows[y]++; }
  const xs = columns.flatMap((count, index) => count >= thresholdY ? [index] : []), ys = rows.flatMap((count, index) => count >= thresholdX ? [index] : []);
  if (!xs.length || !ys.length) throw new Error('Cannot register dense rectangular frame body; provide explicit frameContentBounds.');
  const bounds = { x: xs[0], y: ys[0], right: xs.at(-1) + 1, bottom: ys.at(-1) + 1 };
  if (bounds.right - bounds.x < 32 || bounds.bottom - bounds.y < 32) throw new Error(`Registered frame body is too small: ${JSON.stringify(bounds)}`);
  frame.bounds = bounds;
  frame.fit.contentBounds = { ...bounds, registration: 'opaque coverage crop registration', minimumRowCoverage: thresholdX, minimumColumnCoverage: thresholdY };
  return frame;
}
function horizontalBandRoleProfile(provisional, rawCore) {
  const baseline = rolePaletteProfile(provisional);
  const core = rgba(rawCore.width, rawCore.height), bands = [];
  for (let y = 0; y < provisional.height; y++) {
    const counts = new Map();
    for (let x = 0; x < provisional.width; x++) {
      const p = pixelIndex(provisional, x, y), color = `#${provisional.data.subarray(p, p + 3).toString('hex')}`;
      counts.set(color, (counts.get(color) ?? 0) + 1);
    }
    const sourceColors = [...counts.entries()].map(([color, pixelCount]) => ({ color, pixelCount })).sort((a, b) => b.pixelCount - a.pixelCount || paletteHex.indexOf(a.color) - paletteHex.indexOf(b.color));
    const selectedColor = sourceColors[0].color;
    // This profile is only declared for authored horizontal plank/band planes.
    // Assign each unchanged source row its dominant role palette; no new pixels,
    // shapes, neighbors, or spatial smoothing are introduced.
    const sourceRow = crop(rawCore, { x: 0, y, w: rawCore.width, h: 1 });
    blit(quantizeWithPalette(sourceRow, [selectedColor], true), core, 0, y);
    bands.push({ y, widthPixels: rawCore.width, sourceColors, selectedColors: [selectedColor] });
  }
  return { core, profile: { ...baseline, profile: 'horizontal-bands', method: 'Explicit authored horizontal band role palette: each source-core row is color-converted to its dominant allowed color; source positions and opaque coverage are retained.', selectedColors: [...new Set(bands.map(band => band.selectedColors[0]))], horizontalBands: bands } };
}
async function materialField(authoredMaterial, definition) {
  if (definition.nativeMaterialPath) {
    const core = await loadNativePixels(definition.nativeMaterialPath, PERIOD, true), result = rgba(PANEL, PANEL);
    for (let y = 0; y < PANEL; y++) for (let x = 0; x < PANEL; x++) copyPixel(core, ((x - 32) % PERIOD + PERIOD) % PERIOD, ((y - 32) % PERIOD + PERIOD) % PERIOD, result, x, y);
    result.fit = { ...core.fit, fieldSource: 'authored native material repeat', sourceRegionNativePixels: [0, 0, PERIOD, PERIOD], materialOnlyMedian: 0, repeatCells: [2, 2], rolePalette: { profile: 'native-exact', method: 'Exact authored native pixels; palette validated without conversion or spatial filtering.', selectedColors: activePaletteIndices.map(index => paletteHex[index]) } };
    return result;
  }
  // The interior comes exclusively from its authored material panel. A frame's
  // decorative center must never become a repeated texture for the whole mass.
  const rawCore = crop(authoredMaterial, { x: 32, y: 32, w: PERIOD, h: PERIOD });
  const provisional = quantize({ ...rawCore, data: Buffer.from(rawCore.data) }, true);
  if (definition.materialRoleProfile && !['horizontal-bands', 'explicit-palette'].includes(definition.materialRoleProfile)) throw new Error(`Unknown material role profile: ${definition.materialRoleProfile}`);
  const bandRole = definition.materialRoleProfile === 'horizontal-bands' ? horizontalBandRoleProfile(provisional, rawCore) : null;
  const explicitColors = definition.materialRoleProfile === 'explicit-palette' ? (definition.materialAllowedPalette ?? activePaletteIndices.map(index => paletteHex[index])) : null;
  const roleProfile = bandRole?.profile ?? (explicitColors ? { profile: 'explicit-palette', method: 'Authored structural texture palette retained explicitly; no component/spot removal, band flattening or spatial smoothing.', selectedColors: explicitColors } : rolePaletteProfile(provisional));
  const core = bandRole?.core ?? (explicitColors ? quantizeWithPalette(rawCore, explicitColors, true) : (manifest.stabilizeMaterialPalette === false ? provisional : quantizeWithPalette(rawCore, roleProfile.selectedColors, true)));
  const result = rgba(PANEL, PANEL);
  for (let y = 0; y < PANEL; y++) for (let x = 0; x < PANEL; x++) copyPixel(core, ((x - 32) % PERIOD + PERIOD) % PERIOD, ((y - 32) % PERIOD + PERIOD) % PERIOD, result, x, y);
  result.fit = { ...authoredMaterial.fit, fieldSource: 'authored material core', sourceRegionNormalizedPixels: [32, 32, PERIOD, PERIOD], materialOnlyMedian: 0, repeatCells: [2, 2], rolePalette: manifest.stabilizeMaterialPalette === false ? null : roleProfile };
  return result;
}
function blit(from, to, tx, ty, mask = () => true) {
  for (let y = 0; y < from.height; y++) for (let x = 0; x < from.width; x++) {
    if (!mask(x, y) || tx + x < 0 || ty + y < 0 || tx + x >= to.width || ty + y >= to.height || !from.data[pixelIndex(from, x, y) + 3]) continue;
    copyPixel(from, x, y, to, tx + x, ty + y);
  }
}
async function savePng(img, filename) {
  await fs.mkdir(path.dirname(filename), { recursive: true });
  await sharp(img.data, { raw: { width: img.width, height: img.height, channels: 4 } }).png({ compressionLevel: 9 }).toFile(filename);
}
function cropSafe(img, x, y, w, h) {
  // Repeating a clamped source-edge pixel creates doubled outlines. A source
  // layout that cannot supply a complete corner is an import error instead.
  return crop(img, { x, y, w, h });
}
function applyRegisteredContourPalette(img, side, palettes) {
  const colors = palettes?.[side];
  if (!colors) return img;
  const rect = side === 'N' ? { x: 0, y: 0, w: img.width, h: 1 } : side === 'S' ? { x: 0, y: img.height - 1, w: img.width, h: 1 } : side === 'W' ? { x: 0, y: 0, w: 1, h: img.height } : { x: img.width - 1, y: 0, w: 1, h: img.height };
  // One already-authored outer contour line is assigned its role palette.
  // Its source-pixel positions and alpha remain unchanged.
  blit(quantizeWithPalette(crop(img, rect), colors), img, rect.x, rect.y);
  return img;
}
function frameEdge(frame, corner, axis, phaseX, phaseY) {
  const isWest = corner.endsWith('W'), isNorth = corner.startsWith('N');
  const horizontalPeriod = frame.samplingSettings.horizontalPeriodPixels;
  const sidePeriod = frame.samplingSettings.sidePeriodPixels;
  const sampleX = (frame.width - horizontalPeriod) / 2 + (phaseX * CELL + (isWest ? 0 : QUARTER)) % horizontalPeriod;
  const sampleY = (frame.height - sidePeriod) / 2 + (phaseY * CELL + (isNorth ? 0 : QUARTER)) % sidePeriod;
  // Sample the authored directional side, never rotate a top edge to make a side/bottom.
  if (axis === 'vertical') {
    const result = cropSafe(frame, sampleX, isNorth ? frame.bounds.y : frame.bounds.bottom - QUARTER, QUARTER, QUARTER);
    const colors = frame.horizontalRolePalettes?.[isNorth ? 'N' : 'S']?.selectedColors;
    if (colors) quantizeWithPalette(result, colors);
    // Explicit authored facet roles only recolor this source crop. They retain
    // source positions/alpha; complete corner patches and reference panels are
    // separate and never receive straight-strip palette overrides.
    for (const band of frame.horizontalTrimPaletteBands?.[isNorth ? 'N' : 'S'] ?? []) {
      const sourceBand = cropSafe(result, 0, band.startY, QUARTER, band.height);
      blit(quantizeWithPalette(sourceBand, band.allowedPalette), result, 0, band.startY);
    }
    return applyRegisteredContourPalette(result, isNorth ? 'N' : 'S', frame.registeredContourPalettes);
  }
  const result = cropSafe(frame, isWest ? frame.bounds.x : frame.bounds.right - QUARTER, sampleY, QUARTER, QUARTER);
  return applyRegisteredContourPalette(result, isWest ? 'W' : 'E', frame.registeredContourPalettes);
}
function prepareHorizontalRolePalettes(frame, definition, settings) {
  frame.samplingSettings = settings;
  frame.registeredContourPalettes = definition.registeredContourPalettes;
  for (const [side, colors] of Object.entries(frame.registeredContourPalettes ?? {})) if (!['N', 'S', 'W', 'E'].includes(side) || !colors.length || colors.some(color => !paletteHex.includes(color))) throw new Error(`Invalid registered contour role palette: ${side} ${JSON.stringify(colors)}`);
  frame.horizontalTrimPaletteBands = definition.horizontalTrimPaletteBands;
  for (const [side, bands] of Object.entries(frame.horizontalTrimPaletteBands ?? {})) {
    if (!['N', 'S'].includes(side)) throw new Error(`Invalid horizontal facet side: ${side}`);
    for (const band of bands) if (!Number.isInteger(band.startY) || !Number.isInteger(band.height) || band.startY < 0 || band.height <= 0 || band.startY + band.height > QUARTER || !band.allowedPalette?.length || band.allowedPalette.some(color => !paletteHex.includes(color))) throw new Error(`Invalid horizontal facet palette profile: ${JSON.stringify(band)}`);
  }
  if ((definition.stabilizeHorizontalTrims ?? manifest.stabilizeHorizontalTrims) === false) return frame;
  const period = settings.horizontalPeriodPixels, start = (frame.width - period) / 2;
  const options = { minimumComponentPixels: period, minimumSpanPixels: period, minimumTotalPixels: period };
  frame.horizontalRolePalettes = {
    N: rolePaletteProfile(cropSafe(frame, start, frame.bounds.y, period, QUARTER), true, options),
    S: rolePaletteProfile(cropSafe(frame, start, frame.bounds.bottom - QUARTER, period, QUARTER), true, options),
  };
  return frame;
}
function chooseNativeFramePhase(frame) {
  const scored = [];
  for (const phase of [[0, 0], [1, 0], [0, 1], [1, 1]]) {
    const sample = registerFrameBounds({ ...integerStepResize(frame, 64, 64, phase), fit: { ...frame.fit } });
    const ink = (x, y) => {
      const p = pixelIndex(sample, x, y), color = palette[0];
      return sample.data[p + 3] === 255 && color.every((value, channel) => sample.data[p + channel] === value);
    };
    const counts = { missing: 0, one: 0, two: 0, threePlus: 0 };
    for (const side of ['N', 'S', 'W', 'E']) for (let position = 16; position < 48; position++) {
      let depth = 0;
      for (; depth < 8; depth++) {
        const x = side === 'W' ? sample.bounds.x + depth : side === 'E' ? sample.bounds.right - 1 - depth : position;
        const y = side === 'N' ? sample.bounds.y + depth : side === 'S' ? sample.bounds.bottom - 1 - depth : position;
        if (!ink(x, y)) break;
      }
      counts[depth === 0 ? 'missing' : depth === 1 ? 'one' : depth === 2 ? 'two' : 'threePlus']++;
    }
    scored.push({ phase, counts, score: counts.missing * 4 + counts.two * 2 + counts.threePlus });
  }
  scored.sort((a, b) => a.score - b.score);
  return { phase: scored[0].phase, candidates: scored, method: 'Integer-step nearest source registration; retain sampled authored contour pixels, no outline drawing.' };
}
function outerPatch(frame, corner) {
  const result = cropSafe(frame, corner.endsWith('W') ? frame.bounds.x : frame.bounds.right - QUARTER, corner.startsWith('N') ? frame.bounds.y : frame.bounds.bottom - QUARTER, QUARTER, QUARTER);
  applyRegisteredContourPalette(result, corner.startsWith('N') ? 'N' : 'S', frame.registeredContourPalettes);
  return applyRegisteredContourPalette(result, corner.endsWith('W') ? 'W' : 'E', frame.registeredContourPalettes);
}
function innerPatch(ring, hole, corner) {
  // A filled cell's NW inner corner lies southeast of the hole, and vice versa.
  const west = corner.endsWith('W'), north = corner.startsWith('N');
  return cropSafe(ring, west ? hole.x + hole.w : hole.x - QUARTER, north ? hole.y + hole.h : hole.y - QUARTER, QUARTER, QUARTER);
}
function decorationOnlyOverlay(source, bounds, settings, hole = null) {
  const result = { ...source, data: Buffer.alloc(source.data.length) }, seeds = new Uint8Array(source.width * source.height);
  const rgbAt = (x, y) => {
    const offset = pixelIndex(source, x, y);
    return source.data[offset + 3] ? (source.data[offset] << 16) | (source.data[offset + 1] << 8) | source.data[offset + 2] : null;
  };
  const inOuterCap = (x, y) => x >= bounds.x && x < bounds.right && ((y >= bounds.y && y < bounds.y + settings.trimWidth) || (y >= bounds.bottom - settings.trimWidth && y < bounds.bottom));
  const inHoleMolding = (x, y) => hole && x >= hole.x - settings.trimWidth && x < hole.x + hole.w + settings.trimWidth && ((y >= hole.y - settings.trimWidth && y < hole.y) || (y >= hole.y + hole.h && y < hole.y + hole.h + settings.trimWidth));
  for (let y = 0; y < source.height; y++) for (let x = 0; x < source.width; x++) {
    const color = rgbAt(x, y);
    // White is a cap/molding role only in its registered horizontal boundary
    // bands. A scattered white body brick must not become an overlay seed.
    if (color === 0xef7d57 || color === 0x5d275d || (color === 0xf4f4f4 && (inOuterCap(x, y) || inHoleMolding(x, y)))) seeds[y * source.width + x] = 1;
  }
  const contourSides = (x, y) => {
    const sides = [];
    if (!hole) {
      if (x >= bounds.x && x < bounds.right && y === bounds.y) sides.push('N');
      if (x >= bounds.x && x < bounds.right && y === bounds.bottom - 1) sides.push('S');
      if (y >= bounds.y && y < bounds.bottom && x === bounds.x) sides.push('W');
      if (y >= bounds.y && y < bounds.bottom && x === bounds.right - 1) sides.push('E');
    }
    return sides;
  };
  for (let y = 0; y < source.height; y++) for (let x = 0; x < source.width; x++) {
    const color = rgbAt(x, y), sides = contourSides(x, y);
    let keep = Boolean(seeds[y * source.width + x]);
    // Retain only existing source ink within one native pixel of decoration.
    // No jade plate, mortar background, outline or neighboring color is drawn.
    if (!keep && color === 0x1a1c2c) {
      for (let dy = -1; dy <= 1 && !keep; dy++) for (let dx = -1; dx <= 1; dx++) {
        const nx = x + dx, ny = y + dy;
        if (nx >= 0 && nx < source.width && ny >= 0 && ny < source.height && seeds[ny * source.width + nx]) { keep = true; break; }
      }
    }
    if (color !== null && (keep || sides.length)) {
      copyPixel(source, x, y, result, x, y);
      // Registered contour recoloring preserves the sampled source coverage.
      // Applying it here keeps the full overlay free of foreign jade pixels.
      for (const side of sides) if (source.registeredContourPalettes?.[side]) {
        const pixel = crop(result, { x, y, w: 1, h: 1 });
        blit(quantizeWithPalette(pixel, source.registeredContourPalettes[side]), result, x, y);
      }
    }
  }
  result.junctionOverlay = { profile: settings.junctionOverlayProfile, decorationColors: ['#ef7d57', '#5d275d'], registeredWhiteCapWidth: settings.trimWidth, inkHaloPixels: 1, sourcePixelsOnly: true };
  return result;
}
function quarterModule(material, frame, ring, hole, corner, state, phaseX, phaseY, settings) {
  const west = corner.endsWith('W'), north = corner.startsWith('N');
  const result = crop(material, { x: 32 + phaseX * CELL + (west ? 0 : QUARTER), y: 32 + phaseY * CELL + (north ? 0 : QUARTER), w: QUARTER, h: QUARTER });
  const distanceX = x => west ? x : QUARTER - 1 - x;
  const distanceY = y => north ? y : QUARTER - 1 - y;
  if (settings.junctionOverlayProfile) result.junctionOverlayMask = new Uint8Array(QUARTER * QUARTER);
  const applyPatch = (patch, mask = () => true) => blit(patch, result, 0, 0, (x, y) => {
    const keep = mask(x, y);
    if (keep && patch.data[pixelIndex(patch, x, y) + 3] && result.junctionOverlayMask) result.junctionOverlayMask[y * QUARTER + x] = 1;
    return keep;
  });
  if (settings.junctionOverlayProfile === 'native-quarter-roles-v6') {
    const patch = frame.nativeQuarterRoleImages?.[`${corner}_${state}`];
    if (!patch) throw new Error(`Missing native quarter role plane: ${corner}_${state}`);
    applyPatch(patch);
    return result;
  }
  if (state === 'TOP' || state === 'BOTTOM' || state === 'OUTER') applyPatch(frameEdge(frame, corner, 'vertical', phaseX, phaseY), (x, y) => distanceY(y) < settings.trimWidth);
  if (state === 'SIDE' || state === 'OUTER') applyPatch(frameEdge(frame, corner, 'horizontal', phaseX, phaseY), (x, y) => distanceX(x) < settings.trimWidth);
  // Whole authored corners are the default. The explicit legacy outer profile
  // keeps its authored 8px edge union, leaving the central material visible in
  // a 1x1 cell without changing the source crop or its pixels.
  if (state === 'OUTER') applyPatch(outerPatch(frame, corner), settings.completeOuterCornerPatches ? () => true : (x, y) => distanceX(x) < settings.trimWidth || distanceY(y) < settings.trimWidth);
  if (state === 'INNER') applyPatch(innerPatch(ring, hole, corner), settings.completeInnerCornerPatches ? () => true : (x, y) => distanceX(x) < settings.innerExtent && distanceY(y) < settings.innerExtent && (distanceX(x) < settings.innerTrimWidth || distanceY(y) < settings.innerTrimWidth));
  return result;
}
function assemble(modules, mask) {
  const result = rgba(CELL, CELL);
  for (const q of getQuadrants(mask)) blit(modules[q.moduleKey], result, q.column * QUARTER, q.row * QUARTER);
  return result;
}
function renderFixture(fixture, spritesByVariant, motif, styleId, seed = 0) {
  const grid = occupancyFromRows(fixture.rows, fixture.origin), result = rgba(grid.width * CELL, grid.height * CELL);
  for (const key of grid.occupied) {
    const [x, y] = key.split(',').map(Number), topology = resolveCell(grid.occupied, x, y);
    blit(spritesByVariant[resolveVariant(x, y, seed)][topology.canonicalMask], result, (x - grid.origin.x) * CELL, (grid.height - 1 - (y - grid.origin.y)) * CELL);
  }
  const placements = motifPlacements(grid, styleId, seed);
  for (const placement of placements) blit(motif, result, (placement.x - grid.origin.x) * CELL, (grid.height - (placement.y - grid.origin.y) - placement.height) * CELL);
  return { image: result, placements };
}

const fixtureDoc = JSON.parse(await fs.readFile(path.join(ROOT, 'Data/logical_fixtures.json'), 'utf8'));
const styleCatalog = JSON.parse(await fs.readFile(path.join(ROOT, 'Data/style_catalog.json'), 'utf8'));
const catalogByStyle = new Map(styleCatalog.styles.map(style => [style.styleId, style]));
const lookup = {
  schemaVersion: 1, contractId: 'ANIMOL_FREE_SHAPE_BLOB47_V1', artVersion: ART_VERSION, cellPixels: CELL, pixelsPerUnit: CELL,
  palette: paletteHex, transparentRGB: '#000000', alphaValues: [0, 255],
  cellArtAlpha: 'Every pixel of occupied cell sprites is opaque; empty cells have no sprite.',
  canonicalMasks: CANONICAL_MASKS, rawToCanonical: RAW_TO_CANONICAL, rawToIndex: RAW_TO_INDEX,
  phase: { periodCells: [2, 2], formula: 'v = floorMod(globalX + (seed & 1), 2) + 2 * floorMod(-globalY + ((seed >> 1) & 1), 2)', originIndependent: true, chunkIndependent: true },
  macroMotif: { footprintCells: [4, 4], requiredMarginCells: 1, candidatePeriodCells: [8, 8], candidateResidue: [2, 2], hash: 'FNV-1a UTF-16 joined with |; use complete global occupancy; skip hash % 4 == 0' },
  sourceManifest: local(manifestPath), styles: [],
};
const stats = { artVersion: ART_VERSION, resamplingKernel: 'nearest', spatialFilters: [], materialSource: 'authored material panel only', styles: 0, sourceSheets: 0, authoredPanels: 0, quarterModules: 0, cellSprites: 0, atlases: 0, sampleImages: 0, decoratedSamples: 0, samples: [] };
const copiedSources = new Set();
const galleryImages = new Map();

for (const theme of manifest.themes) {
  const excluded = DEFAULT_THEME_EXCLUSIONS[theme.themeId] ?? [];
  activePaletteIndices = theme.allowedPalette ? theme.allowedPalette.map(color => paletteHex.map(c => c.toLowerCase()).indexOf(color.toLowerCase())) : palette.map((_, index) => index).filter(index => !excluded.includes(index));
  if (!activePaletteIndices.length || activePaletteIndices.some(index => index < 0)) throw new Error(`Invalid allowedPalette for ${theme.themeId}`);
  activePaletteKey = activePaletteIndices.join(',');
  const sourcePath = absoluteSource(theme.sourcePath);
  const themeSource = await loadImage(theme.sourcePath);
  if (!copiedSources.has(sourcePath)) {
    const destination = path.join(ROOT, 'SourceArt', 'Raw', `${theme.themeId}_Source.png`);
    await fs.mkdir(path.dirname(destination), { recursive: true }); if (sourcePath !== destination) await fs.copyFile(sourcePath, destination);
    copiedSources.add(sourcePath); stats.sourceSheets++;
  }
  for (const definition of theme.styles) {
    if (definition.allowedPalette) {
      activePaletteIndices = definition.allowedPalette.map(color => paletteHex.map(c => c.toLowerCase()).indexOf(color.toLowerCase()));
      if (!activePaletteIndices.length || activePaletteIndices.some(index => index < 0)) throw new Error(`Invalid allowedPalette for ${definition.styleId}`);
      activePaletteKey = activePaletteIndices.join(',');
    } else {
      activePaletteIndices = theme.allowedPalette ? theme.allowedPalette.map(color => paletteHex.map(c => c.toLowerCase()).indexOf(color.toLowerCase())) : palette.map((_, index) => index).filter(index => !excluded.includes(index));
      activePaletteKey = activePaletteIndices.join(',');
    }
    const styleId = definition.styleId.startsWith(`${theme.themeId}_`) ? definition.styleId : `${theme.themeId}_${definition.styleId}`;
    const sampling = styleSamplingSettings(definition);
    const source = definition.sourcePath ? await loadImage(definition.sourcePath) : themeSource;
    if (definition.sourcePath) {
      const overrideSourcePath = absoluteSource(definition.sourcePath);
      if (!copiedSources.has(overrideSourcePath)) {
        const destination = path.join(ROOT, 'SourceArt', 'Raw', `${styleId}_Source.png`);
        await fs.mkdir(path.dirname(destination), { recursive: true });
        if (overrideSourcePath !== destination) await fs.copyFile(overrideSourcePath, destination);
        copiedSources.add(overrideSourcePath); stats.sourceSheets++;
      }
    }
    const artRoot = path.join(ROOT, 'Art', styleId);
    const fallbackMaterial = await fitPanel(crop(source, definition.material), 'cover'), rawFrame = await fitPanel(crop(source, definition.frame));
    const materialReference = definition.nativeMaterialReferencePath ? await loadNativePixels(definition.nativeMaterialReferencePath, PANEL, true) : await panel(source, definition.material, false);
    const material = await materialField(fallbackMaterial, definition);
    const frame = registerFrameBounds(definition.nativeFramePath ? await loadNativePixels(definition.nativeFramePath, PANEL) : quantize({ ...rawFrame, data: Buffer.from(rawFrame.data) }, false), definition.nativeFrameContentBounds ?? definition.frameContentBounds);
    const sourceRing = await panel(source, definition.ring, false), ring = definition.nativeRingPath ? await loadNativePixels(definition.nativeRingPath, PANEL) : sourceRing;
    const framePhaseProfile = sampling.framePixels === 64 && !definition.frameSamplePhase ? chooseNativeFramePhase(frame) : null;
    const frameSamplePhase = definition.frameSamplePhase ?? framePhaseProfile?.phase ?? [0, 0];
    const decorativeFrame = prepareHorizontalRolePalettes(sampling.framePixels === PANEL ? frame : registerFrameBounds({ ...integerStepResize(frame, sampling.framePixels, sampling.framePixels, frameSamplePhase), fit: { ...frame.fit, decorativeUniformScale: sampling.framePixels / PANEL, referencePanelPixels: PANEL, samplePanelPixels: sampling.framePixels, nativeSamplePhase: frameSamplePhase } }), definition, sampling);
    const decorativeRing = sampling.ringPixels === PANEL ? ring : { ...(await nearestResize(ring, sampling.ringPixels, sampling.ringPixels)), fit: { ...ring.fit, decorativeUniformScale: sampling.ringPixels / PANEL, referencePanelPixels: PANEL, samplePanelPixels: sampling.ringPixels } };
    let motifRaw = crop(source, definition.motif);
    let clearPixels = 0; for (let p = 3; p < motifRaw.data.length; p += 4) if (motifRaw.data[p] < 128) clearPixels++;
    if (definition.motifMatte !== false && (definition.motifMatte || clearPixels < motifRaw.width * motifRaw.height * .01)) removeBoundaryMatte(motifRaw, definition.motifMatte?.color ?? [255, 255, 255], definition.motifMatte?.tolerance ?? 36);
    const fittedMotif = await fitPanel(motifRaw);
    const motif = definition.nativeMotifPath ? await loadNativePixels(definition.nativeMotifPath, PANEL) : definition.motifAllowedPalette ? quantizeWithPalette(fittedMotif, definition.motifAllowedPalette, false) : quantize(fittedMotif, false);
    const holeInput = definition.innerHole;
    if (!holeInput) throw new Error(`innerHole bbox required for ${styleId}`);
    const ringRect = definition.ring;
    // Native ring corrections preserve the original registered hole by default;
    // a newly authored native ring can provide its own pixel-space hole box.
    const holeFit = definition.nativeRingPath ? sourceRing.fit : ring.fit;
    const hole = definition.nativeRingPath && definition.nativeInnerHolePixels ? { ...definition.nativeInnerHolePixels } : {
      x: ((definition.innerHoleRelative ? holeInput.x : holeInput.x - ringRect.x) * holeFit.scale) + holeFit.offsetX,
      y: ((definition.innerHoleRelative ? holeInput.y : holeInput.y - ringRect.y) * holeFit.scale) + holeFit.offsetY,
      w: holeInput.w * holeFit.scale, h: holeInput.h * holeFit.scale,
    };
    if (['x', 'y', 'w', 'h'].some(key => !Number.isFinite(hole[key])) || hole.w <= 0 || hole.h <= 0) throw new Error(`Invalid registered ring hole for ${styleId}: ${JSON.stringify(hole)}`);
    for (const key of Object.keys(hole)) hole[key] = Math.round(hole[key]);
    if (hole.x < 0 || hole.y < 0 || hole.x + hole.w > PANEL || hole.y + hole.h > PANEL) throw new Error(`Hole outside normalized ring for ${styleId}: ${JSON.stringify(hole)}`);
    const decorativeHole = Object.fromEntries(Object.entries(hole).map(([key, value]) => [key, Math.round(value * sampling.ringPixels / PANEL)]));
    if (decorativeHole.x < QUARTER || decorativeHole.y < QUARTER || sampling.ringPixels - decorativeHole.x - decorativeHole.w < QUARTER || sampling.ringPixels - decorativeHole.y - decorativeHole.h < QUARTER) throw new Error(`Authored ring cannot supply complete ${QUARTER}px inner corners at ${sampling.ringPixels}px sampling: ${styleId} ${JSON.stringify(decorativeHole)}`);
    const roleSources = sampling.junctionOverlayProfile === 'decoration-roles-v5';
    const directQuarterSources = sampling.junctionOverlayProfile === 'native-quarter-roles-v6';
    const junctionFrame = roleSources ? { ...decorativeFrame, data: (await loadNativePixels(definition.nativeFrameDecorationPath, PANEL)).data } : sampling.junctionOverlayProfile && !directQuarterSources ? decorationOnlyOverlay(decorativeFrame, decorativeFrame.bounds, sampling) : decorativeFrame;
    const junctionRing = roleSources ? { ...decorativeRing, data: (await loadNativePixels(definition.nativeRingDecorationPath, PANEL)).data } : sampling.junctionOverlayProfile && !directQuarterSources ? decorationOnlyOverlay(decorativeRing, { x: 0, y: 0, right: decorativeRing.width, bottom: decorativeRing.height }, sampling, decorativeHole) : decorativeRing;
    if (directQuarterSources) {
      junctionFrame.nativeQuarterRoleImages = {};
      for (const corner of ['NW', 'NE', 'SW', 'SE']) for (const state of ['IN', corner.startsWith('N') ? 'TOP' : 'BOTTOM', 'SIDE', 'OUTER', 'INNER']) {
        const key = `${corner}_${state}`, rolePath = definition.nativeQuarterRolePaths[key];
        if (!rolePath) throw new Error(`Missing native quarter role path: ${styleId}/${key}`);
        junctionFrame.nativeQuarterRoleImages[key] = await loadNativePixels(rolePath, QUARTER);
      }
      junctionFrame.junctionOverlay = { profile: sampling.junctionOverlayProfile, sourcePixelsOnly: true, roleSelection: 'declared native quarter geometry; directional attachment ports registered before assembly', quarterRoleSources: definition.nativeQuarterRolePaths, bodyColorsExcludedByProvenance: true };
    }
    if (roleSources) junctionFrame.junctionOverlay = { profile: sampling.junctionOverlayProfile, sourcePixelsOnly: true, roleSelection: 'declared architectural face provenance and its exact ink halo; never inferred from color', frameDecorationSource: definition.nativeFrameDecorationPath, ringDecorationSource: definition.nativeRingDecorationPath, bodyColorsExcludedByProvenance: true };
    await Promise.all([savePng(materialReference, path.join(artRoot, 'material.png')), savePng(material, path.join(artRoot, 'Sampling/body_field128.png')), savePng(frame, path.join(artRoot, 'frame.png')), savePng(ring, path.join(artRoot, 'ring.png')), savePng(motif, path.join(artRoot, 'motif.png'))]); stats.authoredPanels += 4;
    if (definition.nativeMotifPath) await fs.copyFile(absoluteSource(definition.nativeMotifPath), path.join(artRoot, 'motif.png'));
    const catalogEntry = catalogByStyle.get(styleId);
    if (!catalogEntry || catalogEntry.themeId !== theme.themeId) throw new Error(`Style is outside the existing ANIMOL catalog: ${styleId}`);
    const styleRecord = { styleId, themeId: theme.themeId, themeName: catalogEntry.themeName, displayName: catalogEntry.displayName, allowedPalette: activePaletteIndices.map(index => paletteHex[index]), panels: { material: local(path.join(artRoot, 'material.png')), frame: local(path.join(artRoot, 'frame.png')), ring: local(path.join(artRoot, 'ring.png')), motif: local(path.join(artRoot, 'motif.png')) }, sourceTransforms: { material: materialReference.fit, frame: frame.fit, ring: ring.fit, motif: motif.fit }, holeNormalizedPixels: hole, variants: [] };
    if (definition.sourcePath) styleRecord.sourceOverride = local(absoluteSource(definition.sourcePath));
    if (definition.motifAllowedPalette) styleRecord.motifAllowedPalette = definition.motifAllowedPalette;
    styleRecord.materialSampling = { referencePanel: styleRecord.panels.material, repeatedField: local(path.join(artRoot, 'Sampling/body_field128.png')), periodPixels: PERIOD, referencePreserved: true, rolePalette: material.fit.rolePalette };
    if (definition.nativeMaterialPath) styleRecord.materialSampling.nativeMaterialSource = local(absoluteSource(definition.nativeMaterialPath));
    if (definition.nativeMaterialReferencePath) styleRecord.materialSampling.nativeMaterialReferenceSource = local(absoluteSource(definition.nativeMaterialReferencePath));
    if ([definition.nativeMaterialPath, definition.nativeMaterialReferencePath, definition.nativeFramePath, definition.nativeRingPath].some(Boolean)) {
      styleRecord.nativeSourceProfile = { method: 'Authoritative palette-validated native final art; source pixels copied exactly, with the legacy generated strip retained as history.', materialRepeat64: definition.nativeMaterialPath ? local(absoluteSource(definition.nativeMaterialPath)) : undefined, materialReference128: definition.nativeMaterialReferencePath ? local(absoluteSource(definition.nativeMaterialReferencePath)) : undefined, frame128: definition.nativeFramePath ? local(absoluteSource(definition.nativeFramePath)) : undefined, ring128: definition.nativeRingPath ? local(absoluteSource(definition.nativeRingPath)) : undefined, legacyHistorySource: definition.sourcePath ? local(absoluteSource(definition.sourcePath)) : undefined };
    }
    if (sampling.junctionOverlayProfile) styleRecord.junctionOverlaySampling = { ...junctionFrame.junctionOverlay, backgroundOriginNormalizedPixels: [32, 32], backgroundPeriodPixels: [PERIOD, PERIOD], whiteRoles: 'registered horizontal outer cap bands and inner-hole molding bands only', excludedSourceRoles: ['jade plate backgrounds', 'unassociated mortar ink', 'interior white bricks'], frameSource: definition.nativeFramePath ? local(absoluteSource(definition.nativeFramePath)) : styleRecord.panels.frame, ringSource: definition.nativeRingPath ? local(absoluteSource(definition.nativeRingPath)) : styleRecord.panels.ring, ringHolePixels: decorativeHole, masks: 'variants[].junctionOverlayMasks[moduleKey]:16 north-first binary strings;1 means an opaque authored decoration/contour pixel was overlaid' };
    if (roleSources) {
      Object.assign(styleRecord.junctionOverlaySampling, { whiteRoles: 'declared architectural faces only; body face colors and shades never select overlay pixels', excludedSourceRoles: ['all body brick faces and their facets', 'body mortar outside architectural face halos'], frameSource: definition.nativeFrameDecorationPath, ringSource: definition.nativeRingDecorationPath, nativeLayout: 'Data/hole_native_layout_v5.json' });
      Object.assign(styleRecord.nativeSourceProfile, { frameDecoration128: definition.nativeFrameDecorationPath, ringDecoration128: definition.nativeRingDecorationPath, nativeLayout: 'Data/hole_native_layout_v5.json' });
    }
    if (directQuarterSources) {
      Object.assign(styleRecord.junctionOverlaySampling, { whiteRoles: 'declared native architectural faces only', excludedSourceRoles: ['all material pixels; color never determines architectural ownership'], frameSource: definition.nativeFramePath, ringSource: definition.nativeRingPath, nativeLayout: definition.nativeStyleProfile, quarterRoleSources: definition.nativeQuarterRolePaths, holeVertexPolicy: 'incoming horizontal and vertical band intersection; outer union is prohibited at internal hole vertices' });
      Object.assign(styleRecord.nativeSourceProfile, { quarterRoleSources: definition.nativeQuarterRolePaths, nativeLayout: definition.nativeStyleProfile, motif128: definition.nativeMotifPath });
    }
    {
      const framePath = path.join(artRoot, 'Sampling', `frame${sampling.framePixels}.png`), ringPath = path.join(artRoot, 'Sampling', `ring${sampling.ringPixels}.png`);
      await Promise.all([savePng(decorativeFrame, framePath), savePng(decorativeRing, ringPath)]);
      styleRecord.decorativeSampling = { sourceMethod: 'Uniform nearest resize, role palette conversion and source-pixel cropping only', framePixels: sampling.framePixels, ringPixels: sampling.ringPixels, frame: local(framePath), ring: local(ringPath), holePixels: decorativeHole, frameBounds: decorativeFrame.bounds, frameSamplePhase, framePhaseProfile, horizontalTrimRolePalettes: decorativeFrame.horizontalRolePalettes, horizontalTrimPaletteBands: decorativeFrame.horizontalTrimPaletteBands, registeredContourPalettes: decorativeFrame.registeredContourPalettes, registeredContourRole: decorativeFrame.registeredContourPalettes ? 'first source-registered row/column in directional strips and outer patches; colors only, no alpha or geometry edits' : undefined, horizontalPeriodPixels: sampling.horizontalPeriodPixels, sidePeriodPixels: sampling.sidePeriodPixels, trimWidth: sampling.trimWidth, innerExtent: sampling.innerExtent, innerTrimWidth: sampling.innerTrimWidth, outerCornerPolicy: sampling.completeOuterCornerPatches ? 'complete authored 16x16 patch' : `legacy authored ${sampling.trimWidth}px outer edge union; central material retained`, innerCornerPolicy: sampling.completeInnerCornerPatches ? 'complete authored 16x16 patch; legacy inner trim mask is not applied' : 'legacy inner extent and trim mask', materialPeriodPixels: PERIOD, originalReferencePanelsRetained: true };
      if (sampling.junctionOverlayProfile) {
        styleRecord.decorativeSampling.junctionOverlayProfile = sampling.junctionOverlayProfile;
        styleRecord.decorativeSampling.sourceMethod = 'Registered authored decoration pixels and existing one-pixel ink halos over the shared globally phased body; native panel overrides are copied exactly without resampling.';
        styleRecord.decorativeSampling.outerCornerPolicy = sampling.completeOuterCornerPatches ? 'complete 16x16 transparent authored decoration overlay' : `authored ${sampling.trimWidth}px outer edge union; jade and unrelated mortar excluded from the overlay`;
        styleRecord.decorativeSampling.innerCornerPolicy = sampling.completeInnerCornerPatches ? 'complete 16x16 transparent authored decoration overlay; body remains visible outside decoration' : 'legacy inner extent and trim applied to transparent authored decoration overlay';
      }
      if (directQuarterSources) Object.assign(styleRecord.decorativeSampling, { sourceMethod: 'Exact 16px native semantic quarter overlays plus exact globally phased native 64px body; reference frame/ring panels are assembled previews and never sampled for junctions.', outerCornerPolicy: 'native directional band union; outgoing profiles match their straight strips', innerCornerPolicy: 'native incoming band intersection; every port matches its straight trim and body resumes outside registered depths', junctionOverlayProfile: sampling.junctionOverlayProfile });
    }
    const spritesByVariant = [];
    for (let variant = 0; variant < 4; variant++) {
      const phaseX = variant % 2, phaseY = Math.floor(variant / 2), modules = {}, sprites = {};
      const variantDir = variant === 0 ? artRoot : path.join(artRoot, `variants/v${variant}`);
      const moduleRecords = {}, junctionMaskRecords = {};
      for (const corner of ['NW', 'NE', 'SW', 'SE']) for (const state of ['IN', corner.startsWith('N') ? 'TOP' : 'BOTTOM', 'SIDE', 'OUTER', 'INNER']) {
        const key = `${corner}_${state}`, image = quarterModule(material, junctionFrame, junctionRing, decorativeHole, corner, state, phaseX, phaseY, sampling);
        if (image.junctionOverlayMask) junctionMaskRecords[key] = Array.from({ length: QUARTER }, (_, row) => [...image.junctionOverlayMask.subarray(row * QUARTER, (row + 1) * QUARTER)].join(''));
        modules[key] = image; const filename = path.join(variantDir, 'modules', `${key}.png`); await savePng(image, filename); moduleRecords[key] = local(filename); stats.quarterModules++;
      }
      const atlas = rgba(8 * CELL, 6 * CELL), spriteRecords = [];
      for (const [index, mask] of CANONICAL_MASKS.entries()) {
        const image = assemble(modules, mask), filename = path.join(variantDir, 'Sprites', `mask${String(mask).padStart(3, '0')}.png`);
        sprites[mask] = image; await savePng(image, filename); blit(image, atlas, index % 8 * CELL, Math.floor(index / 8) * CELL);
        spriteRecords.push({ mask, index, file: local(filename), rect: { x: index % 8 * CELL, y: Math.floor(index / 8) * CELL, width: CELL, height: CELL } }); stats.cellSprites++;
      }
      const atlasPath = path.join(ROOT, 'Art/Atlases', `${styleId}_v${variant}.png`); await savePng(atlas, atlasPath); stats.atlases++;
      const variantRecord = { id: `v${variant}`, phaseX, phaseY, atlas: local(atlasPath), atlasPixels: [8 * CELL, 6 * CELL], modules: moduleRecords, cells: spriteRecords };
      if (sampling.junctionOverlayProfile) variantRecord.junctionOverlayMasks = junctionMaskRecords;
      styleRecord.variants.push(variantRecord); spritesByVariant.push(sprites);
    }
    for (const fixture of fixtureDoc.fixtures) {
      const rendered = renderFixture(fixture, spritesByVariant, motif, styleId, manifest.sampleSeed ?? 0);
      const filename = path.join(ROOT, 'Samples', styleId, `${fixture.id}.png`); await savePng(rendered.image, filename);
      const sample = { styleId, fixtureId: fixture.id, file: local(filename), widthPixels: rendered.image.width, heightPixels: rendered.image.height, motifPlacements: rendered.placements };
      stats.samples.push(sample); stats.sampleImages++; if (rendered.placements.length) stats.decoratedSamples++;
      if (['open_u_pit', 'closed_hole', 'asymmetric_stairs'].includes(fixture.id)) galleryImages.set(`${styleId}/${fixture.id}`, rendered.image);
    }
    lookup.styles.push(styleRecord); stats.styles++;
    process.stdout.write(`${styleId}: 188 cells, 80 quarters, 4 atlases, ${fixtureDoc.fixtures.length} samples\n`);
  }
}

// Gallery thumbnails are resampled previews, never runtime source assets.
for (const theme of manifest.themes) {
  const canvas = rgba(4 * 320, 3 * 256);
  for (const [column, definition] of theme.styles.entries()) {
    const styleId = definition.styleId.startsWith(`${theme.themeId}_`) ? definition.styleId : `${theme.themeId}_${definition.styleId}`;
    for (const [row, fixtureId] of ['open_u_pit', 'closed_hole', 'asymmetric_stairs'].entries()) {
      const original = galleryImages.get(`${styleId}/${fixtureId}`);
      const scale = Math.min(304 / original.width, 240 / original.height);
      const preview = await nearestResize(original, Math.max(1, Math.floor(original.width * scale)), Math.max(1, Math.floor(original.height * scale)));
      blit(preview, canvas, column * 320 + Math.floor((320 - preview.width) / 2), row * 256 + Math.floor((256 - preview.height) / 2));
    }
  }
  await savePng(canvas, path.join(ROOT, 'Previews', `${theme.themeId}_pit_hole_stairs.png`));
}
await fs.mkdir(path.join(ROOT, 'Data'), { recursive: true });
await fs.mkdir(path.join(ROOT, 'Docs'), { recursive: true });
await fs.writeFile(path.join(ROOT, 'Data/sprite_lookup.json'), `${JSON.stringify(lookup, null, 2)}\n`);
await fs.writeFile(path.join(ROOT, 'Data/palette.json'), `${JSON.stringify({ name: 'Sweetie-16', colors: paletteHex }, null, 2)}\n`);
await fs.writeFile(path.join(ROOT, 'Docs/art_build_stats.json'), `${JSON.stringify(stats, null, 2)}\n`);
process.stdout.write(`${JSON.stringify({ ...stats, samples: undefined })}\n`);
