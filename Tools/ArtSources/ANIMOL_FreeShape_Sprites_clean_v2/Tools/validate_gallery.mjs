import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { makeEmptyGrid, applyBrush, rowsFromGrid, createRenderPlan } from './terrain_preview_model.mjs';
import { occupancyFromRows, resolveCell, cellKey } from './terrain_topology.mjs';
import { resolveVariant, motifPlacements } from './terrain_composition.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = name => JSON.parse(fs.readFileSync(path.join(root, name), 'utf8'));
const lookup = read('Data/sprite_lookup.json'), fixtures = read('Data/logical_fixtures.json'), stats = read('Docs/art_build_stats.json');
const groups = [];
const pass = (id, count, details) => groups.push({ id, passed: true, count, details });

const editor = makeEmptyGrid(16, 16, { x: -17, y: -1 });
assert.equal(editor.occupied.size, 0);
assert(applyBrush(editor, 0, 0));
assert(editor.occupied.has('-17,14'));
assert.equal(resolveCell(editor.occupied, -17, 14).canonicalMask, 0);
assert(applyBrush(editor, 1, 0));
assert.equal(resolveCell(editor.occupied, -17, 14).canonicalMask, 4);
assert.equal(resolveCell(editor.occupied, -16, 14).canonicalMask, 64);
assert(!applyBrush(editor, 1, 0));
assert(!applyBrush(editor, -1, 0));
assert(!applyBrush(editor, 16, 0));
const savedRows = rowsFromGrid(editor), savedOrigin = { ...editor.origin };
assert(applyBrush(editor, 1, 0, 'erase'));
assert.equal(resolveCell(editor.occupied, -17, 14).canonicalMask, 0);
assert(!applyBrush(editor, 1, 0, 'erase'));
const restored = occupancyFromRows(savedRows, savedOrigin);
assert.deepEqual(rowsFromGrid(restored), savedRows);
assert.equal(restored.occupied.size, 2);
assert.throws(() => applyBrush(editor, 0, 0, 'invalid'), TypeError);
pass('paint_erase_snapshot', 14, 'Painting and erasing immediately update neighboring topology; integer bottom-left origin survives snapshot restoration.');

const hole = occupancyFromRows(['###', '###', '###'], { x: 0, y: 0 });
assert(applyBrush(hole, 1, 1, 'erase'));
const holePlan = createRenderPlan(hole, lookup.styles[0].styleId, 0, true);
assert.equal(holePlan.cells.length, 8);
assert(!holePlan.cells.some(c => c.x === 1 && c.y === 1));
assert.equal(holePlan.motifs.length, 0);
for (const corner of [[0, 0], [0, 2], [2, 0], [2, 2]]) {
  assert(resolveCell(hole.occupied, ...corner).quadrants.some(q => q.state === 'INNER'));
}
pass('editable_enclosed_void', 8, 'Erasing an interior cell produces a real empty center and four concave-corner cells.');

let cellChecks = 0, motifChecks = 0;
for (const style of lookup.styles) {
  for (const fixture of fixtures.fixtures) {
    const grid = occupancyFromRows(fixture.rows, fixture.origin);
    const plan = createRenderPlan(grid, style.styleId, 0, true);
    assert.equal(plan.cells.length, grid.occupied.size);
    for (const cell of plan.cells) {
      assert(grid.occupied.has(cellKey(cell.x, cell.y)));
      assert.equal(cell.column, cell.x - grid.origin.x);
      assert.equal(cell.row, grid.height - 1 - (cell.y - grid.origin.y));
      assert.equal(cell.variant, resolveVariant(cell.x, cell.y, 0));
      const record = style.variants[cell.variant].cells[cell.spriteIndex];
      assert.equal(record.mask, cell.canonicalMask);
      assert.equal(record.rect.width, 32);
      assert.equal(record.rect.height, 32);
      assert.equal(record.rect.x, cell.spriteIndex % 8 * 32);
      assert.equal(record.rect.y, Math.floor(cell.spriteIndex / 8) * 32);
      cellChecks++;
    }
    const sample = stats.samples.find(s => s.styleId === style.styleId && s.fixtureId === fixture.id);
    assert(sample, `${style.styleId}/${fixture.id}: actual art baker sample metadata is required`);
    const basePlacements = plan.motifs.map(({ x, y, width, height }) => ({ x, y, width, height }));
    assert.deepEqual(basePlacements, sample.motifPlacements, `${style.styleId}/${fixture.id}: preview and actual art baker motif policy must agree`);
    assert.deepEqual(basePlacements, motifPlacements(grid, style.styleId, 0));
    for (const m of plan.motifs) {
      assert.equal(m.row, grid.height - (m.y - grid.origin.y) - 4);
      for (let dy = -1; dy <= 4; dy++) for (let dx = -1; dx <= 4; dx++) assert(grid.occupied.has(cellKey(m.x + dx, m.y + dy)));
      motifChecks++;
    }
    assert.equal(createRenderPlan(grid, style.styleId, 0, false).motifs.length, 0);
  }
}
pass('actual_atlas_render_plans', cellChecks, { styles: lookup.styles.length, fixtureCount: fixtures.fixtures.length, motifChecks, result: 'Every cell references an actual atlas slot; motif placements exactly match PNG baker metadata and complete occupied support.' });

let phaseChecks = 0;
for (const seed of [-7, 0, 1, 2, 3, 42]) for (const x of [-17, -16, -1, 0, 15, 16]) for (const y of [-17, -16, -1, 0, 15, 16]) {
  const a = occupancyFromRows(['#'], { x, y });
  const padded = occupancyFromRows(['...', '.#.', '...'], { x: x - 1, y: y - 1 });
  const first = createRenderPlan(a, lookup.styles[0].styleId, seed, false).cells[0];
  const second = createRenderPlan(padded, lookup.styles[0].styleId, seed, false).cells[0];
  assert.equal(first.variant, second.variant);
  assert.equal(first.canonicalMask, second.canonicalMask);
  assert(first.variant >= 0 && first.variant < 4);
  phaseChecks++;
}
pass('global_phase_independent_of_canvas_bounds', phaseChecks, 'Changing the preview crop/origin does not change art phase at the same global cell.');

const html = fs.readFileSync(path.join(root, 'DESIGN_GALLERY.html'), 'utf8');
const script = html.match(/<script type="module">([\s\S]*?)<\/script>/u)?.[1];
assert(script, 'Inline browser module must exist.');
const syntax = spawnSync(process.execPath, ['--input-type=module', '--check'], { input: script, encoding: 'utf8' });
assert.equal(syntax.status, 0, syntax.stderr);
assert(!/\b(?:src|href)\s*=\s*["'](?:https?:)?\/\//iu.test(html), 'External resource URL is not allowed.');
assert(!/\bfetch\s*\(/u.test(script), 'Offline gallery must not fetch local or network resources.');
assert(!/^import\s/gmu.test(script), 'Offline module must not import companion files.');
assert(html.includes('data:image/png;base64,'));
assert(html.includes('toBlob'));
pass('offline_html_syntax', 6, 'Inline module parses with Node; actual PNG data are embedded; no fetch/import/external asset dependency. No browser execution was performed.');

const report = { result: 'PASS', groups, styles: lookup.styles.length, scope: 'Canvas-independent paint, neighbor resolution, actual atlas render plans, shared motif policy and offline inline JavaScript syntax. Browser event execution and PNG canvas encoding were not tested.', browserTested: false };
fs.writeFileSync(path.join(root, 'Docs/gallery_validation.json'), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ result: report.result, groups: groups.length, styles: lookup.styles.length, cellChecks, motifChecks, phaseChecks, browserTested: false }));
