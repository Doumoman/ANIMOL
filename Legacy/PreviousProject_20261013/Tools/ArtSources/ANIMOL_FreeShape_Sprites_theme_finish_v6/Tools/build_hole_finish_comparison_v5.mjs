/** Compare exact exported atlases. No motifs, smoothing, or hand-drawn repairs. */
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { occupancyFromRows } from './terrain_topology.mjs';
import { createRenderPlan } from './terrain_preview_model.mjs';

const require = createRequire(import.meta.url), sharp = require('sharp');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const BEFORE = path.resolve(ROOT, '../ANIMOL_FreeShape_Sprites_joint_finish_v4');
const W = 1200, H = 1130, CELL = 32;
const layers = [];
const xml = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const label = (x, y, value, size = 17) => `<text x="${x}" y="${y}" font-family="DejaVu Sans" font-size="${size}" fill="#f4f4f4">${xml(value)}</text>`;
let svg = `<svg width="${W}" height="${H}" xmlns="http://www.w3.org/2000/svg">`;
svg += label(24, 31, 'ANIMOL T01_A | internal-hole joints | exact exported atlas pixels', 22);
svg += label(24, 61, 'v4: before', 21) + label(624, 61, 'v5: repaired border roles / inner joints', 21);

async function bake(root, style, rows, origin, seed = 0) {
  const plan = createRenderPlan(occupancyFromRows(rows, origin), style.styleId, seed, false);
  const atlases = await Promise.all(style.variants.map(v => sharp(path.join(root, v.atlas)).ensureAlpha().raw().toBuffer({ resolveWithObject: true })));
  const width = plan.width * CELL, height = plan.height * CELL, data = Buffer.alloc(width * height * 4);
  for (const cell of plan.cells) {
    const variant = style.variants[cell.variant], rect = variant.cells[cell.spriteIndex].rect, atlas = atlases[cell.variant];
    if (variant.cells[cell.spriteIndex].mask !== cell.canonicalMask || rect.width !== CELL || rect.height !== CELL) throw new Error('Lookup/topology mismatch');
    for (let dy = 0; dy < CELL; dy++) {
      const start = ((rect.y + dy) * atlas.info.width + rect.x) * 4;
      const destination = ((cell.row * CELL + dy) * width + cell.column * CELL) * 4;
      atlas.data.copy(data, destination, start, start + CELL * 4);
    }
  }
  return { data, width, height };
}

const closed = JSON.parse(await fs.readFile(path.join(ROOT, 'Data/logical_fixtures.json'), 'utf8')).fixtures.find(f => f.id === 'closed_hole');
if (!closed) throw new Error('closed_hole fixture missing');
for (const [column, root] of [[0, BEFORE], [1, ROOT]]) {
  const lookup = JSON.parse(await fs.readFile(path.join(root, 'Data/sprite_lookup.json'), 'utf8'));
  if (lookup.artVersion !== (column === 0 ? 4 : 5)) throw new Error('Expected v4/v5 art versions');
  const style = lookup.styles.find(s => s.styleId === 'T01_A');
  const left = 24 + column * 600;
  const add = async (rows, origin, scale, x, y) => {
    const image = await bake(root, style, rows, origin);
    const input = await sharp(image.data, { raw: { width: image.width, height: image.height, channels: 4 } }).resize(image.width * scale, image.height * scale, { kernel: 'nearest' }).png().toBuffer();
    layers.push({ input, left: x, top: y });
  };
  svg += label(left, 91, '3x3 terrain, center 1x1 hole | all global phases | 2x');
  for (const [index, origin] of [{ x: 0, y: 0 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: 1, y: 1 }].entries()) {
    const x = left + (index % 2) * 258, y = 118 + Math.floor(index / 2) * 222;
    svg += label(x, y - 7, `origin (${origin.x},${origin.y}), seed 0`, 14);
    await add(['###', '#.#', '###'], origin, 2, x, y);
  }
  svg += label(left, 562, '4x4, center 2x2 hole | 2x');
  await add(['####', '#..#', '#..#', '####'], { x: 0, y: 0 }, 2, left, 576);
  svg += label(left + 278, 562, '5x3 solid | native 1x');
  await add(['#####', '#####', '#####'], { x: 0, y: 0 }, 1, left + 278, 576);
  svg += label(left, 859, '10x7 terrain, center 6x3 hole | native 1x');
  await add(closed.rows, closed.origin, 1, left, 872);
}
svg += label(24, 1116, 'No motifs or editor grid. Canonical masks and global phase select the package atlas rects. Integer nearest scaling only.', 14);
svg += '</svg>';
layers.unshift({ input: Buffer.from(svg), left: 0, top: 0 });
await fs.mkdir(path.join(ROOT, 'Previews'), { recursive: true });
await sharp({ create: { width: W, height: H, channels: 4, background: '#29313e' } }).composite(layers).png({ compressionLevel: 9 }).toFile(path.join(ROOT, 'Previews/T01_A_hole_finish_v5_comparison.png'));
console.log('T01_A_hole_finish_v5_comparison.png: actual v4/v5 atlas pixels, 4 global phases, motifs disabled.');
