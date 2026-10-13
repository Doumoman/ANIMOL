/** ANIMOL free-shape art topology. Pure occupancy math; no image/physics inference. */
export const BITS = Object.freeze({ N: 1, NE: 2, E: 4, SE: 8, S: 16, SW: 32, W: 64, NW: 128 });
export const NEIGHBORS = Object.freeze([
  ['N', 1, 0, 1], ['NE', 2, 1, 1], ['E', 4, 1, 0], ['SE', 8, 1, -1],
  ['S', 16, 0, -1], ['SW', 32, -1, -1], ['W', 64, -1, 0], ['NW', 128, -1, 1],
].map(([name, bit, dx, dy]) => Object.freeze({ name, bit, dx, dy })));

const CORNERS = Object.freeze([
  { corner: 'NW', vertical: 1, horizontal: 64, diagonal: 128, column: 0, row: 0, cap: 'TOP' },
  { corner: 'NE', vertical: 1, horizontal: 4, diagonal: 2, column: 1, row: 0, cap: 'TOP' },
  { corner: 'SW', vertical: 16, horizontal: 64, diagonal: 32, column: 0, row: 1, cap: 'BOTTOM' },
  { corner: 'SE', vertical: 16, horizontal: 4, diagonal: 8, column: 1, row: 1, cap: 'BOTTOM' },
].map(Object.freeze));

function requireByte(raw) {
  if (!Number.isInteger(raw) || raw < 0 || raw > 255) throw new RangeError('rawMask must be an integer in [0,255]');
}
function requireCellCoordinate(value, name) {
  if (!Number.isSafeInteger(value)) throw new RangeError(`${name} must be a safe integer`);
}
export function cellKey(x, y) {
  requireCellCoordinate(x, 'x'); requireCellCoordinate(y, 'y');
  return `${x},${y}`;
}

/** Diagonal contact alone never joins artwork or components. */
export function canonicalize(raw) {
  requireByte(raw);
  let mask = raw;
  for (const c of CORNERS) {
    if (!(mask & c.vertical) || !(mask & c.horizontal)) mask &= ~c.diagonal;
  }
  return mask;
}

export const CANONICAL_MASKS = Object.freeze([...new Set(Array.from({ length: 256 }, (_, raw) => canonicalize(raw)))].sort((a, b) => a - b));
export const RAW_TO_CANONICAL = Object.freeze(Array.from({ length: 256 }, (_, raw) => canonicalize(raw)));
export const RAW_TO_INDEX = Object.freeze(RAW_TO_CANONICAL.map(mask => CANONICAL_MASKS.indexOf(mask)));
export const QUARTER_MODULE_KEYS = Object.freeze(CORNERS.flatMap(c => ['IN', c.cap, 'SIDE', 'OUTER', 'INNER'].map(state => `${c.corner}_${state}`)));

/**
 * Ordered NW,NE,SW,SE records. column/row are north-first 2x2 image offsets.
 * SIDE is the west/east edge appropriate to the named corner, never a rotated cap.
 */
export function getQuadrants(raw) {
  const mask = canonicalize(raw);
  return CORNERS.map(c => {
    const vertical = Boolean(mask & c.vertical), horizontal = Boolean(mask & c.horizontal);
    const state = !vertical && !horizontal ? 'OUTER'
      : !vertical ? c.cap : !horizontal ? 'SIDE'
      : mask & c.diagonal ? 'IN' : 'INNER';
    return { corner: c.corner, state, moduleKey: `${c.corner}_${state}`, column: c.column, row: c.row };
  });
}

/** Set contains "x,y" strings. A callback receives global integer (x,y). */
export function neighborMask(occupied, x, y) {
  requireCellCoordinate(x, 'x'); requireCellCoordinate(y, 'y');
  const has = typeof occupied === 'function' ? occupied
    : occupied instanceof Set ? (cx, cy) => occupied.has(cellKey(cx, cy)) : null;
  if (!has) throw new TypeError('occupied must be a Set of cell keys or a (x,y) callback');
  let mask = 0;
  for (const n of NEIGHBORS) if (has(x + n.dx, y + n.dy)) mask |= n.bit;
  return mask;
}

/** Empty center has no foreground sprite, even when its neighbors are occupied. */
export function resolveCell(occupied, x, y) {
  requireCellCoordinate(x, 'x'); requireCellCoordinate(y, 'y');
  const has = typeof occupied === 'function' ? occupied(x, y) : occupied.has(cellKey(x, y));
  if (!has) return null;
  const rawMask = neighborMask(occupied, x, y);
  const canonicalMask = canonicalize(rawMask);
  return { rawMask, canonicalMask, spriteIndex: RAW_TO_INDEX[rawMask], quadrants: getQuadrants(rawMask) };
}

/** Logical rows use # occupied and . empty. Origin is integer bottom-left. */
export function occupancyFromRows(rows, origin = { x: 0, y: 0 }) {
  if (!Array.isArray(rows) || rows.length === 0 || typeof rows[0] !== 'string' || rows[0].length === 0) throw new TypeError('rows must be nonempty strings');
  requireCellCoordinate(origin.x, 'origin.x'); requireCellCoordinate(origin.y, 'origin.y');
  const width = rows[0].length, height = rows.length, occupied = new Set();
  rows.forEach((row, r) => {
    if (typeof row !== 'string' || row.length !== width || /[^#.]/u.test(row)) throw new TypeError('rows must have equal width and contain only # or .');
    for (let c = 0; c < width; c++) if (row[c] === '#') occupied.add(cellKey(origin.x + c, origin.y + height - 1 - r));
  });
  return { width, height, origin: { ...origin }, occupied };
}

/** Rendering chunks do not alter art neighbors. Floor division is required below zero. */
export function chunkCoordinate(globalCoordinate, chunkSize = 16) {
  requireCellCoordinate(globalCoordinate, 'globalCoordinate');
  if (!Number.isSafeInteger(chunkSize) || chunkSize <= 0) throw new RangeError('chunkSize must be a positive integer');
  return Math.floor(globalCoordinate / chunkSize);
}

export function partitionOccupancy(occupied, chunkSize = 16) {
  if (!(occupied instanceof Set)) throw new TypeError('occupied must be a Set of cell keys');
  const chunks = new Map();
  for (const key of occupied) {
    const [x, y] = key.split(',').map(Number);
    const owner = cellKey(chunkCoordinate(x, chunkSize), chunkCoordinate(y, chunkSize));
    if (!chunks.has(owner)) chunks.set(owner, new Set());
    chunks.get(owner).add(key);
  }
  return chunks;
}
