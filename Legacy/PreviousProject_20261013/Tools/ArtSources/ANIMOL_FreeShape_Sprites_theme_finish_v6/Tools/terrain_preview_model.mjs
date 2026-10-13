import { occupancyFromRows, cellKey, resolveCell } from './terrain_topology.mjs';
import { resolveVariant, motifPlacements } from './terrain_composition.mjs';

/** Canvas-independent editor model; occupancy remains the authoritative source. */
export function makeEmptyGrid(width, height, origin = { x: 0, y: 0 }) {
  if (!Number.isSafeInteger(width) || !Number.isSafeInteger(height) || width < 1 || height < 1) throw new RangeError('Preview dimensions must be positive integers');
  return occupancyFromRows(Array.from({ length: height }, () => '.'.repeat(width)), origin);
}

export function applyBrush(grid, column, row, mode = 'paint') {
  if (!Number.isInteger(column) || !Number.isInteger(row) || column < 0 || row < 0 || column >= grid.width || row >= grid.height) return false;
  if (mode !== 'paint' && mode !== 'erase') throw new TypeError('Brush mode must be paint or erase');
  const key = cellKey(grid.origin.x + column, grid.origin.y + grid.height - 1 - row);
  if (mode === 'erase') return grid.occupied.delete(key);
  if (grid.occupied.has(key)) return false;
  grid.occupied.add(key);
  return true;
}

export function rowsFromGrid(grid) {
  return Array.from({ length: grid.height }, (_, row) => Array.from({ length: grid.width }, (_, column) =>
    grid.occupied.has(cellKey(grid.origin.x + column, grid.origin.y + grid.height - 1 - row)) ? '#' : '.').join(''));
}

/** Identical global art phase and motif support rules to the PNG fixture baker. */
export function createRenderPlan(grid, styleId, seed = 0, includeMotifs = true) {
  const cells = [...grid.occupied].map(key => {
    const [x, y] = key.split(',').map(Number), topology = resolveCell(grid.occupied, x, y);
    return {
      x, y,
      column: x - grid.origin.x,
      row: grid.height - 1 - (y - grid.origin.y),
      canonicalMask: topology.canonicalMask,
      spriteIndex: topology.spriteIndex,
      variant: resolveVariant(x, y, seed),
    };
  }).sort((a, b) => a.row - b.row || a.column - b.column);
  const motifs = includeMotifs ? motifPlacements(grid, styleId, seed).map(placement => ({
    ...placement,
    column: placement.x - grid.origin.x,
    row: grid.height - (placement.y - grid.origin.y) - placement.height,
  })) : [];
  return { width: grid.width, height: grid.height, cellPixels: 32, cells, motifs };
}
