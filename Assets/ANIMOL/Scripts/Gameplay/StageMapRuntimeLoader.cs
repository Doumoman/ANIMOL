using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Gameplay
{
    [Serializable]
    public sealed class StableTileReference
    {
        [SerializeField] private string tileId = string.Empty;
        [SerializeField] private TileBase tile;
        [SerializeField] private bool developmentPlaceholder;
        public string TileId => tileId;
        public TileBase Tile => tile;
        public bool DevelopmentPlaceholder => developmentPlaceholder;
    }

    public sealed partial class StageMapRuntimeLoader : MonoBehaviour
    {
        [SerializeField] private StageMapDefinition map;
        [SerializeField] private Tilemap background;
        [SerializeField] private Tilemap terrain;
        [SerializeField] private Tilemap decoration;
        [SerializeField] private Tilemap objectTiles;
        [SerializeField] private StableTileReference[] palette = Array.Empty<StableTileReference>();
        [SerializeField] private StageTerrainTileCatalog terrainTileCatalog;
        [SerializeField] private StageMapObjectTypeRegistry objectTypeRegistry;
        [SerializeField] private bool developmentPreview;
        private Transform runtimeObjectRoot;

        public StageMapDefinition Map => map;
        public bool DevelopmentPreview => developmentPreview;

        private void Start()
        {
            if (map != null) Load(map);
        }

        public void Load(StageMapDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var structural = StageMapValidator.ValidateStructure(definition);
            if (!structural.IsValid) throw new InvalidOperationException(string.Join(" | ", structural.Errors));
            // Resolve pinned masks before touching the existing preview/physics cache.
            var structureRegistry = definition.HasTerrainStructures ? StageTerrainStructureRegistry.Load() : null;
            if (structureRegistry != null) definition.ResolveTerrain(structureRegistry);
            PrepareTerrainPhysicsOwner(definition, structureRegistry != null);
            map = definition;
            ConfigureGridScale(definition.WorldUnitsPerCell);
            if (background != null) background.ClearAllTiles();
            if (terrain != null) terrain.ClearAllTiles();
            if (decoration != null) decoration.ClearAllTiles();
            if (objectTiles != null) objectTiles.ClearAllTiles();
            var lookup = new Dictionary<string, TileBase>(StringComparer.Ordinal);
            foreach (var entry in palette)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.TileId) && entry.Tile != null) lookup[entry.TileId] = entry.Tile;
            var catalog = terrainTileCatalog != null ? terrainTileCatalog : Resources.Load<StageTerrainTileCatalog>("ANIMOL_TerrainTileCatalog");
            if (catalog != null)
                foreach (var entry in catalog.Entries)
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.TileId) && entry.Tile != null) lookup[entry.TileId] = entry.Tile;
            foreach (var cell in definition.Cells)
            {
                if (!lookup.TryGetValue(cell.TileId, out var tile))
                {
                    if (cell.Layer != StageMapLayer.Terrain || catalog == null) continue;
                    tile = catalog.FindThemeDefault(definition.ThemeId);
                    if (tile == null) continue;
                }
                var tilemap = ResolveLayer(cell.Layer);
                if (generatedTerrainOwner != null && cell.Layer == StageMapLayer.Terrain &&
                    cell.TileId == "M9_MOON_ONE_WAY_16PX_PLACEHOLDER") tilemap = generatedOneWay;
                if (tilemap != null) tilemap.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile);
            }
            var structures = GetComponent<StageTerrainStructureRuntime>();
            if (structureRegistry != null)
            {
                if (structures == null) structures = gameObject.AddComponent<StageTerrainStructureRuntime>();
                structures.Build(definition, terrain, structureRegistry);
            }
            else if (structures != null) structures.Clear();
            SpawnAuthoredObjects(definition);
            if (terrain != null) terrain.CompressBounds();
            Physics2D.SyncTransforms();
            var worldBounds = definition.GetWorldBounds();
            var boundary = GetComponent<StageMapWorldBoundary>();
            if (boundary == null) boundary = gameObject.AddComponent<StageMapWorldBoundary>();
            boundary.Configure(worldBounds, definition.WorldUnitsPerCell);
            foreach (var follow in FindObjectsByType<DirectionalCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                follow.ConfigureWorldBounds(worldBounds);
        }

        public void ConfigureDefinition(StageMapDefinition definition, StageMapObjectTypeRegistry registry, bool isDevelopmentPreview)
        {
            map = definition;
            objectTypeRegistry = registry;
            developmentPreview = isDevelopmentPreview;
        }

        public void ResetRuntimeObjects()
        {
            if (runtimeObjectRoot == null) return;
            foreach (var resettable in runtimeObjectRoot.GetComponentsInChildren<MonoBehaviour>(true).OfType<IStageMapRuntimeResettable>())
                resettable.ResetRuntimeState();
        }

        private void SpawnAuthoredObjects(StageMapDefinition definition)
        {
            if (runtimeObjectRoot == null)
            {
                var existing = transform.Find("M9_RuntimeObjects");
                runtimeObjectRoot = existing != null ? existing : new GameObject("M9_RuntimeObjects").transform;
                runtimeObjectRoot.SetParent(transform, false);
                if (!Application.isPlaying) runtimeObjectRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
            for (var i = runtimeObjectRoot.childCount - 1; i >= 0; i--)
            {
                var child = runtimeObjectRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            foreach (var placement in definition.Objects)
            {
                var type = objectTypeRegistry?.Find(placement);
                if (type == null && !string.IsNullOrWhiteSpace(placement.DataKey))
                {
                    Debug.LogWarning($"[ANIMOL][M9B] Unknown map object type is preserved but unsupported: {placement.DataKey}/{placement.StableId}.", this);
                    if (!developmentPreview) continue;
                }
                if (placement.Settings.Version > 3)
                {
                    Debug.LogWarning($"[ANIMOL][M9B] Newer map object settings v{placement.Settings.Version} are preserved but unsupported: {placement.DataKey}/{placement.StableId}.", this);
                    if (!developmentPreview) continue;
                }
                if (placement.Settings.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype && !developmentPreview)
                {
                    Debug.LogError($"[ANIMOL][M9B] Blocked prototype runtime object {placement.DataKey}/{placement.StableId} outside DEV preview.", this);
                    continue;
                }
                var prefab = placement.Prefab != null ? placement.Prefab : type?.Prefab;
                if (prefab == null) continue;
                var instance = Instantiate(prefab, runtimeObjectRoot);
                StageMapRuntimeFactory.Configure(instance, placement, definition.WorldUnitsPerCell);
            }
            Physics2D.SyncTransforms();
        }

        public Tilemap ResolveLayer(StageMapLayer layer)
        {
            return layer switch
            {
                StageMapLayer.Background => background,
                StageMapLayer.Terrain => terrain,
                StageMapLayer.Decoration => decoration,
                StageMapLayer.Object => objectTiles,
                _ => null
            };
        }

        private void ConfigureGridScale(float unitsPerCell)
        {
            var grids = new HashSet<Grid>();
            foreach (var tilemap in new[] { background, terrain, decoration, objectTiles })
            {
                var grid = tilemap == null ? null : tilemap.GetComponentInParent<Grid>();
                if (grid != null) grids.Add(grid);
            }
            foreach (var grid in grids) grid.cellSize = new Vector3(unitsPerCell, unitsPerCell, 1f);
        }
    }

    public sealed class StageMapWorldBoundary : MonoBehaviour
    {
        private const string BoundaryRootName = "M8_WorldBoundary";
        public Rect WorldBounds { get; private set; }

        public void Configure(Rect worldBounds, float cellSize)
        {
            WorldBounds = worldBounds;
            var root = transform.Find(BoundaryRootName);
            if (root == null)
            {
                root = new GameObject(BoundaryRootName).transform;
                root.SetParent(transform, false);
            }
            while (root.childCount < 4)
            {
                var child = new GameObject($"Boundary-{root.childCount}");
                child.transform.SetParent(root, false);
                child.AddComponent<BoxCollider2D>();
            }
            var thickness = Mathf.Max(.1f, cellSize);
            ConfigureWall(root.GetChild(0), new Vector2(worldBounds.xMin - thickness * .5f, worldBounds.center.y), new Vector2(thickness, worldBounds.height + thickness * 2f));
            ConfigureWall(root.GetChild(1), new Vector2(worldBounds.xMax + thickness * .5f, worldBounds.center.y), new Vector2(thickness, worldBounds.height + thickness * 2f));
            ConfigureWall(root.GetChild(2), new Vector2(worldBounds.center.x, worldBounds.yMin - thickness * .5f), new Vector2(worldBounds.width, thickness));
            ConfigureWall(root.GetChild(3), new Vector2(worldBounds.center.x, worldBounds.yMax + thickness * .5f), new Vector2(worldBounds.width, thickness));
        }

        private static void ConfigureWall(Transform wall, Vector2 position, Vector2 size)
        {
            wall.localPosition = position;
            wall.localRotation = Quaternion.identity;
            var collider = wall.GetComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = false;
        }
    }
}
