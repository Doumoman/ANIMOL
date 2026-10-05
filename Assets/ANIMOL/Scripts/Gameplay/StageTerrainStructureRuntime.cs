using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using Animol.TerrainStructure;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Gameplay
{
    // Map-global lifetime. Chunk visibility never owns, splits or respawns the complete sprite.
    public sealed class StageTerrainStructureRuntime : MonoBehaviour
    {
        public readonly Dictionary<string, GameObject> Instances = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        public readonly Dictionary<Vector2Int, HashSet<string>> ChunkOwners = new Dictionary<Vector2Int, HashSet<string>>();
        public AnimolTerrainResolveResult LogicalGrid { get; private set; }
        public int BuiltRevision { get; private set; } = -1;
        private readonly HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();
        private readonly Dictionary<string, Vector2Int[]> coveredChunks = new Dictionary<string, Vector2Int[]>();
        private Tile ownedCellTile;
        private GameObject freeShapeArt;

        public void Build(StageMapDefinition map, Tilemap physicsOwner, StageTerrainStructureRegistry registry)
        {
            var resolved = map.ResolveTerrain(registry);
            foreach (var p in map.TerrainPlacements.placements)
            {
                var e = resolved.entriesById[p.catalogId];
                if (!registry.Frame(e.frameId).ValidateForFootprint(e.width, e.height, out var error)) throw new InvalidOperationException(error);
            }
            Clear();
            LogicalGrid = resolved;
            if (physicsOwner == null && (resolved.placements.Count > 0 || map.HasFreeShape)) throw new InvalidOperationException("Operational terrain Tilemap physics owner is missing.");
            if (physicsOwner != null)
            {
                if (physicsOwner.GetComponent<TilemapCollider2D>() == null) throw new InvalidOperationException("Operational terrain TilemapCollider2D is missing.");
                ownedCellTile = ScriptableObject.CreateInstance<Tile>();
                ownedCellTile.name = "StructureLogicalCell";
                ownedCellTile.hideFlags = HideFlags.HideAndDontSave;
                ownedCellTile.colliderType = Tile.ColliderType.Grid;
                foreach (var cell in resolved.solids.Values.Where(c => !string.IsNullOrEmpty(c.ownerId)))
                    physicsOwner.SetTile(new Vector3Int(cell.cell.x, cell.cell.y, 0), ownedCellTile);
                var renderer = physicsOwner.GetComponent<TilemapRenderer>();
                if (renderer != null) renderer.sortingOrder = 20;
            }
            foreach (var p in map.TerrainPlacements.placements)
            {
                var e = resolved.entriesById[p.catalogId];
                var instance = CreateArt(transform, p, e, registry.Frame(e.frameId), map.GetEditorPreviewUnitsPerCell(), e.kind == "Structure" ? 10 : 30);
                Instances.Add(p.instanceId, instance);
                var chunks = AnimolTerrainPlacementEngine.GetCoveredChunks(e, new Vector2Int(p.x, p.y));
                coveredChunks.Add(p.instanceId, chunks);
                foreach (var chunk in chunks)
                {
                    if (!ChunkOwners.TryGetValue(chunk, out var owners)) ChunkOwners.Add(chunk, owners = new HashSet<string>());
                    owners.Add(p.instanceId); visibleChunks.Add(chunk);
                }
            }
            if(map.HasFreeShape)
            {freeShapeArt=new GameObject("FreeShape artwork · no collider");freeShapeArt.transform.SetParent(transform,false);freeShapeArt.AddComponent<FreeShapeTerrainRenderer>().Rebuild(map.FreeShapeTerrain,map.GetEditorPreviewUnitsPerCell());}
            physicsOwner?.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();
            BuiltRevision = map.AuthoringRevision;
        }

        public void SetChunkVisible(Vector2Int chunk, bool visible)
        {
            if(freeShapeArt!=null)freeShapeArt.GetComponent<FreeShapeTerrainRenderer>().SetChunkVisible(chunk,visible);
            if (visible) visibleChunks.Add(chunk); else visibleChunks.Remove(chunk);
            if (!ChunkOwners.TryGetValue(chunk, out var owners)) return;
            foreach (var id in owners) Instances[id].SetActive(coveredChunks[id].Any(visibleChunks.Contains));
        }

        public static GameObject CreateArt(Transform parent, AnimolTerrainPlacement p, AnimolTerrainCatalogEntry e,
            AnimolTerrainArtFrameDefinition frame, float units, int order)
        {
            var root = new GameObject(p.instanceId + " · " + p.catalogId);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(p.x * units, p.y * units, 0);
            var art = new GameObject("SourceArt"); art.transform.SetParent(root.transform, false);
            art.transform.localPosition = frame.VisualOffset * units;
            art.transform.localScale = Vector3.one * (frame.UniformScale * units);
            var renderer = art.AddComponent<SpriteRenderer>(); renderer.sprite = frame.Sprite;
            renderer.sharedMaterial = frame.Material; renderer.sortingOrder = order;
            return root;
        }

        public void Clear()
        {
            if(freeShapeArt!=null){freeShapeArt.SetActive(false);DestroyOwned(freeShapeArt);freeShapeArt=null;}
            foreach (var instance in Instances.Values) if (instance != null) { instance.SetActive(false); DestroyOwned(instance); }
            Instances.Clear(); ChunkOwners.Clear(); coveredChunks.Clear(); visibleChunks.Clear();
            if (ownedCellTile != null) DestroyOwned(ownedCellTile);
            LogicalGrid = null; BuiltRevision = -1;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void OnDestroy() { if (ownedCellTile != null) DestroyOwned(ownedCellTile); }
    }
}
