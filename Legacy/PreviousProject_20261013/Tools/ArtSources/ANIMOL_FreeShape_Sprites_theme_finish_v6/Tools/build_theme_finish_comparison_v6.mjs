/** Exact package pixels. Terrain comes from lookup-selected atlas rects, never redraws. */
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { occupancyFromRows } from './terrain_topology.mjs';
import { createRenderPlan } from './terrain_preview_model.mjs';

const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2), baselineOption = args.indexOf('--baseline-v5');
if (baselineOption >= 0 && !args[baselineOption + 1]) throw new Error('--baseline-v5 requires a directory.');
const BEFORE = path.resolve((baselineOption >= 0 ? args[baselineOption + 1] : args[0]) ?? path.join(ROOT, '../ANIMOL_FreeShape_Sprites_hole_finish_v5'));
const CELL = 32, background = '#29313e';
const xml = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const text = (x, y, value, size = 16, color = '#f4f4f4') => `<text x="${x}" y="${y}" font-family="DejaVu Sans" font-size="${size}" fill="${color}">${xml(value)}</text>`;
const themeNames = { T01: 'Moon Palace', T02: 'Cloud Whale Ranch', T03: 'Stardust Library', T04: 'Timeglass Greenhouse', T05: 'Aurora Crystal Mine' };
const lookupByRoot = new Map(), atlasCache = new Map(), records = [];
for (const [directory, version] of [[BEFORE, 5], [ROOT, 6]]) {
  const lookup = JSON.parse(await fs.readFile(path.join(directory, 'Data/sprite_lookup.json'), 'utf8'));
  if (lookup.artVersion !== version || lookup.styles.length !== 20) throw new Error(`Expected complete artVersion ${version} lookup in ${directory}`);
  lookupByRoot.set(directory, lookup);
}
async function atlases(root, style) {
  const key = root + '/' + style.styleId;
  if (!atlasCache.has(key)) atlasCache.set(key, await Promise.all(style.variants.map(v => sharp(path.join(root, v.atlas)).ensureAlpha().raw().toBuffer({ resolveWithObject: true }))));
  return atlasCache.get(key);
}
async function bake(root, style, rows, origin, seed = 0) {
  const plan = createRenderPlan(occupancyFromRows(rows, origin), style.styleId, seed, false);
  const sheets = await atlases(root, style), width = plan.width * CELL, height = plan.height * CELL, data = Buffer.alloc(width * height * 4);
  for (const cell of plan.cells) {
    const variant = style.variants[cell.variant], record = variant.cells[cell.spriteIndex], rect = record.rect, atlas = sheets[cell.variant];
    if (record.mask !== cell.canonicalMask || rect.width !== CELL || rect.height !== CELL) throw new Error('Lookup/topology mismatch');
    for (let dy = 0; dy < CELL; dy++) {
      const source = ((rect.y + dy) * atlas.info.width + rect.x) * 4;
      const destination = ((cell.row * CELL + dy) * width + cell.column * CELL) * 4;
      atlas.data.copy(data, destination, source, source + CELL * 4);
    }
  }
  return { data, width, height, cellCount: plan.cells.length };
}
async function terrainLayer(root, style, shape, scale, left, top) {
  const image = await bake(root, style, shape.rows, shape.origin, shape.seed ?? 0);
  const input = await sharp(image.data, { raw: { width: image.width, height: image.height, channels: 4 } }).resize(image.width * scale, image.height * scale, { kernel: 'nearest' }).png().toBuffer();
  records.push({ artVersion: lookupByRoot.get(root).artVersion, styleId: style.styleId, rows: shape.rows, origin: shape.origin, seed: shape.seed ?? 0, scale, occupiedCells: image.cellCount, atlasSources: style.variants.map(v => v.atlas) });
  return { input, left, top };
}
async function saveCanvas(file, width, height, svg, layers) {
  await sharp({ create: { width, height, channels: 4, background } }).composite([{ input: Buffer.from(svg + '</svg>'), left: 0, top: 0 }, ...layers]).png({ compressionLevel: 9 }).toFile(path.join(ROOT, file));
}
await fs.mkdir(path.join(ROOT, 'Previews'), { recursive: true });
const shapes = [
  { name: '1x1 enclosed hole', top: 314, rows: ['###', '#.#', '###'], origin: { x: -17, y: -1 } },
  { name: '2x2 enclosed hole', top: 454, rows: ['####', '#..#', '#..#', '####'], origin: { x: 1, y: 1 } },
  { name: 'Stair boundary / inner join', top: 626, rows: ['#...', '##..', '###.', '####'], origin: { x: 0, y: 0 } },
  { name: 'Open pit / side / ceiling', top: 804, rows: ['#..#', '#..#', '#..#', '####'], origin: { x: 0, y: 0 } },
];
for (const themeId of Object.keys(themeNames)) {
  const width = 1248, height = 1030, columnWidth = 312, layers = [];
  let svg = `<svg width="${width}" height="${height}" xmlns="http://www.w3.org/2000/svg">`;
  svg += text(24, 31, `${themeId} | ${themeNames[themeId]} | v5 / v6 actual exported art`, 22);
  svg += text(24, 58, 'Four approved style identities. Registered material detail and border roles; T01_A is locked to v5.', 16, '#94b0c2');
  const afterStyles = lookupByRoot.get(ROOT).styles.filter(s => s.themeId === themeId);
  for (const [column, after] of afterStyles.entries()) {
    const x = column * columnWidth;
    svg += `<rect x="${x + 2}" y="76" width="308" height="910" rx="0" fill="#1f2734"/>`;
    svg += text(x + 12, 102, `${after.styleId}${after.styleId === 'T01_A' ? ' | preserved' : ' | revised'}`, 18);
    for (const [side, root] of [[0, BEFORE], [1, ROOT]]) {
      const style = lookupByRoot.get(root).styles.find(s => s.styleId === after.styleId);
      const left = x + 12 + side * 156;
      svg += text(left, 127, side === 0 ? 'v5 before' : 'v6 current', 14, '#ffcd75');
      const input = await sharp(path.join(root, style.panels.material)).extract({ left: 32, top: 32, width: 64, height: 64 }).resize(128, 128, { kernel: 'nearest' }).png().toBuffer();
      layers.push({ input, left, top: 141 });
      svg += text(left, 288, '64px material, 2x', 12, '#94b0c2');
      for (const shape of shapes) layers.push(await terrainLayer(root, style, shape, 1, left + (128 - shape.rows[0].length * CELL) / 2, shape.top));
    }
    for (const shape of shapes) svg += text(x + 12, shape.top - 10, shape.name, 13, '#94b0c2');
  }
  svg += text(24, 1014, 'Terrain: exact 32px atlas rects, canonical masks, global phase. Motifs off; nearest scaling; no grid or painted correction.', 13, '#94b0c2');
  await saveCanvas(`Previews/${themeId}_theme_finish_v6_comparison.png`, width, height, svg, layers);
}

// Dense contact view: every style has a solid mass and an actual empty internal cell.
const width = 1664, height = 1280, layers = [], solid = { rows: ['###', '###', '###'], origin: { x: 0, y: 0 } }, hole = { rows: ['###', '#.#', '###'], origin: { x: -17, y: -1 } };
let svg = `<svg width="${width}" height="${height}" xmlns="http://www.w3.org/2000/svg">`;
svg += text(20, 30, 'ANIMOL theme_finish_v6 | 5 themes x 4 styles | actual atlas assembly, 2x nearest pixels', 23);
svg += text(20, 55, 'Each pair: solid 3x3 terrain / center 1x1 hole. T01_A remains v5; the other 19 styles are revised.', 17, '#94b0c2');
for (const [index, style] of lookupByRoot.get(ROOT).styles.entries()) {
  const column = index % 4, row = Math.floor(index / 4), x = column * 416, y = 76 + row * 238;
  svg += `<rect x="${x + 3}" y="${y}" width="409" height="230" fill="#1f2734"/>`;
  svg += text(x + 12, y + 23, `${style.styleId} | ${themeNames[style.themeId]}${style.styleId === 'T01_A' ? ' (v5)' : ''}`, 15);
  layers.push(await terrainLayer(ROOT, style, solid, 2, x + 12, y + 34));
  layers.push(await terrainLayer(ROOT, style, hole, 2, x + 212, y + 34));
}
await saveCanvas('Previews/ANIMOL_theme_finish_v6_overview.png', width, height, svg, layers);
await fs.writeFile(path.join(ROOT, 'Docs/theme_finish_comparison_build.json'), JSON.stringify({ artVersion: 6, result: 'PASS', input: 'Data/sprite_lookup.json atlas rects; material crop is actual Art material panel x32/y32/w64/h64', baseline: path.basename(BEFORE), output: 'five theme comparisons plus one 20-style overview', motifIncluded: false, spatialFilter: 'none; integer nearest enlargement only', handPaintedPixels: false, records }, null, 2) + '\n');
console.log(JSON.stringify({ artVersion: 6, previews: 6, renderedTerrainCases: records.length, actualAtlasCells: records.reduce((sum, r) => sum + r.occupiedCells, 0), baseline: path.basename(BEFORE), result: 'PASS' }));
