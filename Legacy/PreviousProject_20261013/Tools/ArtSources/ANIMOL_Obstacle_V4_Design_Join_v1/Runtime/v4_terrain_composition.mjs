/** Shared deterministic art phase and supported-motif policy for baking and preview. */
export function floorMod(value, divisor) { return ((value % divisor) + divisor) % divisor; }

export function resolveVariant(x, y, seed = 0) {
  return floorMod(x + (seed & 1), 2) + 2 * floorMod(-y + ((seed >> 1) & 1), 2);
}

export function hashArtPlacement(...values) {
  let h = 2166136261;
  for (const value of values.join('|')) { h ^= value.codePointAt(0); h = Math.imul(h, 16777619); }
  return h >>> 0;
}

/** Full 4×4 occupied support plus one-cell occupied margin in all directions. */
export function motifPlacements(grid, styleId, seed = 0) {
  const positions = [];
  const minX = grid.origin.x, minY = grid.origin.y;
  for (let y = minY; y <= minY + grid.height - 4; y++) for (let x = minX; x <= minX + grid.width - 4; x++) {
    if (floorMod(x, 8) !== 2 || floorMod(y, 8) !== 2 || hashArtPlacement(styleId, x, y, seed) % 4 === 0) continue;
    let valid = true;
    for (let dy = -1; dy <= 4 && valid; dy++) for (let dx = -1; dx <= 4; dx++) {
      if (!grid.occupied.has(`${x + dx},${y + dy}`)) { valid = false; break; }
    }
    if (valid) positions.push({ x, y, width: 4, height: 4 });
  }
  return positions;
}
