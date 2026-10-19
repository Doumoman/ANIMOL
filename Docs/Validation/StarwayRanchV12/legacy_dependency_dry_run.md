# V12 legacy removal dry run

- V10 sprites with GUID/fileID inventory: 1816
- Serialized references to those GUIDs in all Assets YAML: 0
- Matching V12 relative PNG paths: 1816
- Package PNG bytes were verified against manifest SHA256 before rebind.
- Active Starway art asset and nine Starway scenarios now refer to V12 sprites and themed prefabs.
- Shared generic device prefabs and other themes are excluded from removal.

## Candidate old sprite directories

- Assets/ANIMOL/Sprites/StarwayRanch/backgrounds/
- Assets/ANIMOL/Sprites/StarwayRanch/bridges/
- Assets/ANIMOL/Sprites/StarwayRanch/bridge_decor/
- Assets/ANIMOL/Sprites/StarwayRanch/decor/
- Assets/ANIMOL/Sprites/StarwayRanch/devices/
- Assets/ANIMOL/Sprites/StarwayRanch/props/
- Assets/ANIMOL/Sprites/StarwayRanch/skirts/
- Assets/ANIMOL/Sprites/StarwayRanch/surfaces/
- Assets/ANIMOL/Sprites/StarwayRanch/tiles/

## Hits

None.

## Other retired assets

- V10 runtime scripts: 5 classes, serialized component references 0. V10 builder/importer have no callers after V12 migration.
- V10 manifest/layout/import JSON TextAssets: 6, serialized references 0.
