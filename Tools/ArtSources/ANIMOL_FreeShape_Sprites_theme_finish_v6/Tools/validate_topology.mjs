import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  BITS, CANONICAL_MASKS, RAW_TO_CANONICAL, RAW_TO_INDEX, QUARTER_MODULE_KEYS,
  canonicalize, getQuadrants, neighborMask, resolveCell, cellKey,
  occupancyFromRows, chunkCoordinate, partitionOccupancy,
} from './terrain_topology.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const catalog = JSON.parse(fs.readFileSync(path.join(root, 'Data/topology_catalog.json'), 'utf8'));
const fixtureFile = JSON.parse(fs.readFileSync(path.join(root, 'Data/logical_fixtures.json'), 'utf8'));
const started = performance.now();
const groups = [];
const record = (id, count, details) => groups.push({ id, passed: true, count, details });

// This reference defines the expected geometry independently from module names/order.
const offsets = [[0, 1, 1], [1, 1, 2], [1, 0, 4], [1, -1, 8], [0, -1, 16], [-1, -1, 32], [-1, 0, 64], [-1, 1, 128]];
const referenceRaw = (has, x, y) => offsets.reduce((m, [dx, dy, bit]) => m + (has(x + dx, y + dy) ? bit : 0), 0);
const walls = {
  N: { corners: ['NW', 'NE'], state: 'TOP' }, S: { corners: ['SW', 'SE'], state: 'BOTTOM' },
  W: { corners: ['NW', 'SW'], state: 'SIDE' }, E: { corners: ['NE', 'SE'], state: 'SIDE' },
};
const quarterExposes = (q, direction) => q.state === 'OUTER' || q.state === walls[direction].state;
function verifyBoundaries(quadrants, has, x, y) {
  const qmap = Object.fromEntries(quadrants.map(q => [q.corner, q]));
  for (const [direction, dx, dy] of [['N', 0, 1], ['S', 0, -1], ['W', -1, 0], ['E', 1, 0]]) {
    const expected = !has(x + dx, y + dy);
    for (const corner of walls[direction].corners) assert.equal(quarterExposes(qmap[corner], direction), expected);
  }
}

assert.equal(CANONICAL_MASKS.length, 47);
assert.equal(QUARTER_MODULE_KEYS.length, 20);
assert.equal(new Set(QUARTER_MODULE_KEYS).size, 20);
assert.deepEqual(catalog.bitOrder, BITS);
assert.deepEqual(catalog.canonicalMasks, CANONICAL_MASKS);
assert.deepEqual(catalog.rawToCanonical, RAW_TO_CANONICAL);
assert.deepEqual(catalog.rawToIndex, RAW_TO_INDEX);
assert.deepEqual(catalog.quarterModuleKeys, QUARTER_MODULE_KEYS);
assert.equal(catalog.styleIds.length, 20);
assert.equal(new Set(catalog.styleIds).size, 20);
assert.equal(catalog.expectedBaseSpriteCount, 940);
assert.deepEqual(catalog.cells.map(c => c.quadrants), CANONICAL_MASKS.map(getQuadrants));
record('catalog_contract', 940, '20 styles × 47 canonical base sprite slots; data-only count, no claim that images are present.');

const cornerTuples = new Set();
for (let raw = 0; raw < 256; raw++) {
  const canonical = canonicalize(raw), quadrants = getQuadrants(raw);
  assert(CANONICAL_MASKS.includes(canonical));
  assert.equal(canonicalize(canonical), canonical);
  assert.equal(CANONICAL_MASKS[RAW_TO_INDEX[raw]], canonical);
  assert.deepEqual(quadrants, getQuadrants(canonical));
  assert.equal(quadrants.length, 4);
  for (const q of quadrants) assert(QUARTER_MODULE_KEYS.includes(q.moduleKey));
  const has = (x, y) => {
    if (x === 0 && y === 0) return true;
    const n = offsets.find(([dx, dy]) => dx === x && dy === y);
    return n ? Boolean(raw & n[2]) : false;
  };
  verifyBoundaries(quadrants, has, 0, 0);
  const cm = Object.fromEntries(quadrants.map(q => [q.corner, q]));
  for (const [corner, dx, dy] of [['NW', -1, 1], ['NE', 1, 1], ['SW', -1, -1], ['SE', 1, -1]]) {
    const diagonalNotch = has(dx, 0) && has(0, dy) && !has(dx, dy);
    assert.equal(cm[corner].state === 'INNER', Boolean(diagonalNotch));
  }
  cornerTuples.add(quadrants.map(q => q.moduleKey).join('|'));
}
assert.equal(cornerTuples.size, 47);
assert.deepEqual(getQuadrants(170), getQuadrants(0)); // Four diagonals alone create no joined surfaces.
record('all_raw_neighborhoods', 256, 'Every raw neighbor mask resolves; all four exterior sides and concave-corner selections agree with occupancy.');

// Enumerate every 4×4 occupancy. This exercises all thin, touching, hole and branch
// arrangements without assuming a representative shape list proves arbitrary maps.
let occupiedCellContexts = 0, emptyCellContexts = 0, sharedEdges = 0;
for (let gridMask = 0; gridMask < 65536; gridMask++) {
  const has = (x, y) => x >= 0 && x < 4 && y >= 0 && y < 4 && Boolean(gridMask & (1 << (y * 4 + x)));
  for (let y = 0; y < 4; y++) for (let x = 0; x < 4; x++) {
    const actual = resolveCell(has, x, y);
    if (!has(x, y)) {
      assert.equal(actual, null);
      emptyCellContexts++;
      continue;
    }
    occupiedCellContexts++;
    assert.equal(actual.rawMask, referenceRaw(has, x, y));
    assert(actual.spriteIndex >= 0 && actual.spriteIndex < 47);
    verifyBoundaries(actual.quadrants, has, x, y);
    if (has(x + 1, y)) sharedEdges++;
    if (has(x, y + 1)) sharedEdges++;
  }
}
assert.equal(occupiedCellContexts, 524288);
assert.equal(emptyCellContexts, 524288);
assert.equal(sharedEdges, 393216);
record('all_4x4_occupancies', 65536, { occupiedCellContexts, emptyCellContexts, sharedEdges, result: 'No foreground in empty centers and no exterior side wall on a solid-solid shared edge.' });

// Every adjacent pair's combined 4×3 neighborhood has ten free cells: 1,024
// contexts per orientation. Occupied pair endpoints are fixed, not randomized.
for (const orientation of ['horizontal', 'vertical']) {
  const w = orientation === 'horizontal' ? 4 : 3, h = orientation === 'horizontal' ? 3 : 4;
  const first = { x: 1, y: 1 }, second = orientation === 'horizontal' ? { x: 2, y: 1 } : { x: 1, y: 2 };
  const free = [];
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (!(x === first.x && y === first.y) && !(x === second.x && y === second.y)) free.push([x, y]);
  assert.equal(free.length, 10);
  for (let mask = 0; mask < 1024; mask++) {
    const set = new Set([cellKey(first.x, first.y), cellKey(second.x, second.y)]);
    free.forEach(([x, y], bit) => { if (mask & (1 << bit)) set.add(cellKey(x, y)); });
    const has = (x, y) => set.has(cellKey(x, y));
    verifyBoundaries(getQuadrants(neighborMask(set, first.x, first.y)), has, first.x, first.y);
    verifyBoundaries(getQuadrants(neighborMask(set, second.x, second.y)), has, second.x, second.y);
  }
}
record('all_adjacent_pair_contexts', 2048, '1,024 combined neighborhoods each for horizontal and vertical solid-solid pairs.');

const step4 = [[1, 0], [-1, 0], [0, 1], [0, -1]];
function countComponents(keys) {
  const remaining = new Set(keys);
  let count = 0;
  while (remaining.size) {
    const first = remaining.values().next().value, queue = [first];
    remaining.delete(first); count++;
    for (let i = 0; i < queue.length; i++) {
      const [x, y] = queue[i].split(',').map(Number);
      for (const [dx, dy] of step4) {
        const next = cellKey(x + dx, y + dy);
        if (remaining.delete(next)) queue.push(next);
      }
    }
  }
  return count;
}
function countHoles(grid) {
  const empty = new Set();
  for (let y = grid.origin.y - 1; y <= grid.origin.y + grid.height; y++) {
    for (let x = grid.origin.x - 1; x <= grid.origin.x + grid.width; x++) {
      const key = cellKey(x, y);
      if (!grid.occupied.has(key)) empty.add(key);
    }
  }
  return countComponents(empty) - 1; // The padded exterior is exactly one component.
}
const fixtureStats = [];
for (const f of fixtureFile.fixtures) {
  const grid = occupancyFromRows(f.rows, f.origin);
  const components = countComponents(grid.occupied), holes = countHoles(grid);
  assert.equal(components, f.expected.components, `${f.id}: connected components`);
  assert.equal(holes, f.expected.holes, `${f.id}: holes`);
  for (let row = 0; row < grid.height; row++) for (let col = 0; col < grid.width; col++) {
    const x = f.origin.x + col, y = f.origin.y + grid.height - 1 - row;
    const value = resolveCell(grid.occupied, x, y);
    assert.equal(value !== null, f.rows[row][col] === '#');
    if (value) verifyBoundaries(value.quadrants, (cx, cy) => grid.occupied.has(cellKey(cx, cy)), x, y);
  }
  fixtureStats.push({ id: f.id, width: grid.width, height: grid.height, occupiedCells: grid.occupied.size, components, holes });
}
record('logical_shape_fixtures', fixtureFile.fixtures.length, fixtureStats);

assert.equal(chunkCoordinate(-1), -1);
assert.equal(chunkCoordinate(-16), -1);
assert.equal(chunkCoordinate(-17), -2);
assert.equal(chunkCoordinate(15), 0);
assert.equal(chunkCoordinate(16), 1);
let translatedContexts = 0;
for (const coordinate of [-17, -16, -1, 0, 15, 16]) {
  for (let raw = 0; raw < 256; raw++) {
    const grid = new Set([cellKey(coordinate, coordinate)]);
    offsets.forEach(([dx, dy, bit]) => { if (raw & bit) grid.add(cellKey(coordinate + dx, coordinate + dy)); });
    const chunks = partitionOccupancy(grid);
    const globalHasFromChunks = (x, y) => chunks.get(cellKey(chunkCoordinate(x), chunkCoordinate(y)))?.has(cellKey(x, y)) ?? false;
    assert.equal(neighborMask(grid, coordinate, coordinate), raw);
    assert.equal(neighborMask(globalHasFromChunks, coordinate, coordinate), raw);
    assert.deepEqual(resolveCell(grid, coordinate, coordinate), resolveCell(globalHasFromChunks, coordinate, coordinate));
    translatedContexts++;
  }
}
const large = fixtureFile.fixtures.find(f => f.id === 'large_chunk_seam_34x6');
const largeGrid = occupancyFromRows(large.rows, large.origin), largeChunks = partitionOccupancy(largeGrid.occupied);
assert.equal(largeChunks.size, 8);
const largeLookup = (x, y) => largeChunks.get(cellKey(chunkCoordinate(x), chunkCoordinate(y)))?.has(cellKey(x, y)) ?? false;
let negativeControlSeams = 0;
for (const key of largeGrid.occupied) {
  const [x, y] = key.split(',').map(Number);
  assert.deepEqual(resolveCell(largeGrid.occupied, x, y), resolveCell(largeLookup, x, y));
  const localChunk = largeChunks.get(cellKey(chunkCoordinate(x), chunkCoordinate(y)));
  if (neighborMask(localChunk, x, y) !== neighborMask(largeGrid.occupied, x, y)) negativeControlSeams++;
}
assert(negativeControlSeams > 0, 'Seam fixture must detect the wrong chunk-local neighbor implementation.');
record('global_negative_chunk_boundaries', translatedContexts + largeGrid.occupied.size, { translatedContexts, largeOccupiedCells: largeGrid.occupied.size, chunkCount: largeChunks.size, negativeControlCellsThatWouldShowWrongLocalBoundaries: negativeControlSeams });

// Common invalid inputs must fail rather than silently changing an authored mask.
for (const invalid of [-1, 256, 1.5, NaN, '1', null]) assert.throws(() => canonicalize(invalid), RangeError);
for (const invalid of [[], [''], ['#', '##'], ['x']]) assert.throws(() => occupancyFromRows(invalid), TypeError);
assert.throws(() => occupancyFromRows(['#'], { x: 0.5, y: 0 }), RangeError);
assert.throws(() => chunkCoordinate(-1, 0), RangeError);
assert.throws(() => resolveCell(() => false, 0.5, 0), RangeError);
record('invalid_inputs_rejected', 13, 'No invalid masks, fractional origins/coordinates, ragged rows, unknown symbols or zero-size chunks are accepted.');

const report = {
  contractId: catalog.contractId,
  result: 'PASS',
  scope: 'Node occupancy topology, lookup data and logical fixtures only. Art pixels, Unity runtime collision, editor integration and saving are not tested here.',
  durationMs: Math.round(performance.now() - started),
  groups,
};
fs.writeFileSync(path.join(root, 'Docs/topology_validation.json'), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ result: report.result, groups: groups.length, durationMs: report.durationMs, all4x4Masks: 65536, allRawMasks: 256, occupiedCellContexts, sharedEdges, fixtures: fixtureFile.fixtures.length }));
