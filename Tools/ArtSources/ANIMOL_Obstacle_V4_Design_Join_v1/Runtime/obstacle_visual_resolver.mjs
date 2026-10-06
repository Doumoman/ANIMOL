import { BITS, neighborMask, canonicalize, RAW_TO_INDEX } from './v4_terrain_topology.mjs';
import { resolveVariant } from './v4_terrain_composition.mjs';

export const FULL = new Set(['C02','C03','C08','C10','R03']);
export const TOP = new Set(['C01','C05','C06','C09','M02','R01']);
export const AIR = new Set(['C04','C07']);
export const POSES = ['idle','warn','active','inactive'];
export const CAP_PIXELS = 8;
const coords = (x,y) => {
  if (!Number.isSafeInteger(x) || !Number.isSafeInteger(y)) throw new RangeError('integer cell required');
};
const key = (x,y) => { coords(x,y); return `${x},${y}`; };
export function materialEqual(a,b) {
  return !!a && !!b && a.themeId === b.themeId && a.styleId === b.styleId &&
    a.artVersion === b.artVersion && (a.joinGroup || 'default') === (b.joinGroup || 'default');
}
export function isFull(c) { return !!c && (c.kind === 'terrain' || FULL.has(c.kind)); }
export function hasTop(c) { return isFull(c) || (!!c && TOP.has(c.kind) && c.surfaceEnabled === true); }
function pose(c) {
  if (c.pose === 'pending') return 'warn';
  if (!POSES.includes(c.pose || 'idle')) throw new Error('unknown visual pose');
  return c.pose || 'idle';
}
function validate(c) {
  if (!c || c.artVersion !== 4 || !/^T0[1-5]_[ABCD]$/.test(c.styleId) || c.themeId !== c.styleId.slice(0,3))
    throw new Error('matching v4 theme/style required; no fallback');
  if (c.kind !== 'terrain' && !FULL.has(c.kind) && !TOP.has(c.kind) && !AIR.has(c.kind)) throw new Error('unknown kind');
  if (TOP.has(c.kind) && typeof c.surfaceEnabled !== 'boolean') throw new Error('TOP requires authoritative surfaceEnabled');
  if (c.kind === 'M02' && c.themeId !== 'T01') throw new Error('M02 is a moon candidate');
  if (['R01','R03'].includes(c.kind) && c.themeId !== 'T05') throw new Error('R01/R03 are mine candidates');
  if (['C02','C08'].includes(c.kind) && !['LEFT','RIGHT'].includes(c.facing)) throw new Error('side facing required');
  if (c.kind === 'C07' && !['LEFT','RIGHT','UP'].includes(c.facing)) throw new Error('wind direction required');
}

/** Read an immutable post-physics snapshot. This function never writes occupancy or colliders. */
export function resolveVisual(read,x,y,seed=0) {
  coords(x,y); if (!Number.isSafeInteger(seed)) throw new RangeError('integer seed required');
  const c = read(x,y); if (!c) return null; validate(c);
  const bodyAt = (nx,ny) => { const n=read(nx,ny); return materialEqual(c,n) && isFull(n); };
  const topAt = (nx,ny) => { const n=read(nx,ny); return materialEqual(c,n) && hasTop(n); };
  const bodyRaw = isFull(c) ? neighborMask(bodyAt,x,y) : 0;
  let capRaw = bodyRaw;
  if (hasTop(c)) {
    if (topAt(x-1,y)) capRaw |= BITS.W;
    if (topAt(x+1,y)) capRaw |= BITS.E;
  }
  const bodyMask = canonicalize(bodyRaw), capMask = canonicalize(capRaw);
  const exposedTop = !bodyAt(x,y+1);
  const problems=[];
  if (TOP.has(c.kind) && !exposedTop || ['C03','C08','C10'].includes(c.kind) && !exposedTop) problems.push('functional top face blocked');
  if (c.kind === 'C02' && bodyAt(x+(c.facing==='LEFT'?-1:1),y)) problems.push('reflect face blocked');
  const facing = ['C02','C07','C08'].includes(c.kind) ? c.facing : 'UP';
  const state = c.kind==='terrain' ? 'idle' : pose(c);
  return Object.freeze({
    x,y,kind:c.kind,themeId:c.themeId,styleId:c.styleId,artVersion:4,
    bodyRaw,bodyMask,bodyIndex:RAW_TO_INDEX[bodyRaw],capRaw,capMask,capIndex:RAW_TO_INDEX[capRaw],
    variant:resolveVariant(x,y,seed),
    drawBody:isFull(c), drawCap:hasTop(c)&&exposedTop,
    capPatch:isFull(c)&&exposedTop&&capMask!==bodyMask,
    capOnly:TOP.has(c.kind)&&c.surfaceEnabled, capHeight:CAP_PIXELS,
    overlay:c.kind==='terrain'?null:`${c.themeId}/${c.kind}/${facing}/${state}`,
    pose:state,facing,problems,
    cacheKey:[4,c.styleId,c.joinGroup||'default',c.kind,bodyMask,capMask,resolveVariant(x,y,seed),facing,state,c.surfaceEnabled===true?1:0].join('|')
  });
}

/** A geometry/material change affects nine masks and 36 possible existing v4 motif anchors. */
export function affectedArtCells(x,y) {
  coords(x,y); const cells=[], motifAnchors=[];
  for(let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++) cells.push([x+dx,y+dy]);
  for(let dy=-4;dy<=1;dy++) for(let dx=-4;dx<=1;dx++) motifAnchors.push([x+dx,y+dy]);
  return { cells,motifAnchors };
}

/** Strict overlap policy for the preview/importer. AIR devices do not occupy an existing terrain cell. */
export function putVisual(map,x,y,cell) {
  coords(x,y); validate(cell);
  if (map.has(key(x,y))) throw new Error('cell already owned; remove/replace through an authoring transaction');
  map.set(key(x,y),Object.freeze({...cell}));
}
