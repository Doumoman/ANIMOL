import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { BITS, CANONICAL_MASKS, RAW_TO_CANONICAL, RAW_TO_INDEX, QUARTER_MODULE_KEYS, getQuadrants } from './terrain_topology.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const write = (name, value) => fs.writeFileSync(path.join(root, 'Data', name), `${JSON.stringify(value, null, 2)}\n`);

const themes = [
  { id: 'T01', name: '월궁' }, { id: 'T02', name: '구름고래목장' },
  { id: 'T03', name: '별가루도서관' }, { id: 'T04', name: '시간유리온실' }, { id: 'T05', name: '오로라수정광산' },
];
write('topology_catalog.json', {
  schemaVersion: 1,
  contractId: 'ANIMOL_FREE_SHAPE_BLOB47_V1',
  cellUnits: 1,
  cellPixels: 32,
  quarterPixels: 16,
  spritePPU: 32,
  topology: '8-neighbor lookup; diagonals require both adjacent orthogonal cells',
  occupancyConnectivity: 4,
  emptyCenterSprite: null,
  artDependsOnAlpha: false,
  bitOrder: BITS,
  canonicalMasks: CANONICAL_MASKS,
  rawToCanonical: RAW_TO_CANONICAL,
  rawToIndex: RAW_TO_INDEX,
  quarterModuleKeys: QUARTER_MODULE_KEYS,
  spritesPerStyle: 47,
  styleCount: 20,
  expectedBaseSpriteCount: 940,
  styleIds: themes.flatMap(theme => ['A', 'B', 'C', 'D'].map(style => `${theme.id}_${style}`)),
  themes,
  cells: CANONICAL_MASKS.map((mask, index) => ({
    index,
    canonicalMask: mask,
    fileSuffix: `mask${String(mask).padStart(3, '0')}.png`,
    quadrants: getQuadrants(mask),
  })),
  boundaryRules: {
    chunkSeams: 'Resolve neighbors in global occupancy, not within a render chunk.',
    diagonalTouch: 'Diagonal-only contact remains separate and has no connecting ornament.',
    styleSeams: 'Cross-style material transitions require a separately authored connector policy.',
    physics: 'Only canonical logical occupancy owns solid collision; art alpha never adds or removes collision.',
    existingV3: 'This contract does not reinterpret existing stamp ownership, saved fields, IDs, variants or physics owners.',
  },
});

const filled = (w, h) => Array.from({ length: h }, () => '#'.repeat(w));
const regular = [
  '......##', '....####', '..######', '########',
];
const fixture = (id, rows, components, holes = 0, purpose = '', origin = { x: 0, y: 0 }) => ({
  id, origin, rows, expected: { components, holes }, purpose,
});
write('logical_fixtures.json', {
  schemaVersion: 1,
  contractId: 'ANIMOL_FREE_SHAPE_BLOB47_V1',
  rowsOrder: 'north-first',
  originRule: 'integer bottom-left; localY = height - 1 - row',
  symbols: { '#': 'occupied solid cell', '.': 'empty cell, no foreground and no solid collision' },
  componentConnectivity: 4,
  holeDefinition: 'Empty four-connected components enclosed by occupied cells within a padded bounding box.',
  fixtures: [
    fixture('single_1x1', ['#'], 1, 0, 'All four exterior corners.'),
    fixture('thin_vertical_1x16', filled(1, 16), 1, 0, 'One-cell width; separate top and bottom caps.'),
    fixture('thin_horizontal_16x1', filled(16, 1), 1, 0, 'One-cell height; top and bottom remain distinct.'),
    fixture('rectangle_3x5', filled(3, 5), 1, 0, 'Tall body and side finish.'),
    fixture('rectangle_5x3', filled(5, 3), 1, 0, 'Wide body; no nonuniform stretching.'),
    fixture('maximum_rectangle_16x16', filled(16, 16), 1, 0, 'Maximum suggested art-bake region, not a map-size cap.'),
    fixture('asymmetric_stairs', ['........##', '.......###', '...#######', '..########', '##########'], 1, 0, 'Unequal tread and rise.'),
    fixture('regular_stairs_ascending_right', regular, 1, 0, 'Regular two-cell tread and one-cell rise.'),
    fixture('regular_stairs_ascending_left', regular.map(row => [...row].reverse().join('')), 1, 0, 'Opposite stair direction.'),
    fixture('open_u_pit', ['##......##', '##......##', '##......##', '###....###', '##########', '##########'], 1, 0, 'Open pit; concave floor corners; no art across empty center.'),
    fixture('closed_hole', ['##########', '##########', '##......##', '##......##', '##......##', '##########', '##########'], 1, 1, 'Enclosed void; ceiling, walls and floor all face inward.'),
    fixture('overhang', ['##########', '##########', '##........', '##........', '####......'], 1, 0, 'Underside ceiling and unsupported-looking overhang silhouette.'),
    fixture('branch_t', ['#########', '#########', '...###...', '...###...', '...###...', '...###...'], 1, 0, 'T branch with two inward corners.'),
    fixture('diagonal_separate', ['#.', '.#'], 2, 0, 'Touching only at a corner does not merge.'),
    fixture('multiple_components', ['###.....##', '###.....##', '..........', '....##....', '....##....'], 3, 0, 'Three independent terrain components.'),
    fixture('large_chunk_seam_34x6', filled(34, 6), 1, 0, 'Global solid boundary crosses -16,0,16 in x and -16 in y; no artificial walls.', { x: -17, y: -17 }),
  ],
});
console.log(JSON.stringify({ canonicalMasks: CANONICAL_MASKS.length, quarterModules: QUARTER_MODULE_KEYS.length, styles: 20, baseSprites: 940, fixtures: 16 }));
