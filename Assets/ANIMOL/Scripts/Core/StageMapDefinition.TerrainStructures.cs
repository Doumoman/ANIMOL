using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEngine;
using Engine = Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Core
{
    public sealed partial class StageMapDefinition
    {
        [SerializeField] private AnimolTerrainPlacementCollection terrainPlacements = new AnimolTerrainPlacementCollection();
        [SerializeField] private FreeShapeLayer freeShapeTerrain = new FreeShapeLayer();
        public FreeShapeLayer FreeShapeTerrain => freeShapeTerrain;
        public bool HasFreeShape => freeShapeTerrain != null && freeShapeTerrain.HasData;
        public AnimolTerrainPlacementCollection TerrainPlacements => terrainPlacements;
        public void EditorInvalidateTerrainDerivedCache() { collisionDataRevision = -1; }
        public bool HasTerrainStructures => HasFreeShape || terrainPlacements != null &&
            (terrainPlacements.schemaVersion != 3 || terrainPlacements.placements == null || terrainPlacements.placements.Count > 0);

        public AnimolTerrainSavedMap ReadTerrain(StageTerrainStructureRegistry registry)
        {
            if (terrainPlacements == null || terrainPlacements.schemaVersion != 3 || terrainPlacements.placements == null)
                throw new InvalidOperationException("Unsupported terrain placement schema; original serialized data is preserved.");
            var dto = new AnimolTerrainSavedMap { themeId = themeId, revision = authoringRevision,
                schemaVersion = terrainPlacements.schemaVersion, placements = terrainPlacements.placements,
                freeShape = freeShapeTerrain?.Copy() ?? new FreeShapeLayer() };
            foreach (var cell in cells.Where(c => c.Layer == StageMapLayer.Terrain))
                dto.baseCells.Add(new AnimolTerrainBaseCell { x = cell.X, y = cell.Y, themeId = themeId,
                    styleId = registry.Style(themeId, cell.TileId, cell.VariantId) });
            // This host has no serialized structure-owned cell cache. Any base/owner overlap is corruption.
            if (!Engine.Resolve(registry.Catalog, dto, out _, out var error)) throw new InvalidOperationException(error);
            if(dto.freeShape.HasData)FreeShapeArtRegistry.Load(dto.freeShape);
            return Engine.CloneMap(dto);
        }

        public AnimolTerrainResolveResult ResolveTerrain(StageTerrainStructureRegistry registry)
        {
            if (!Engine.Resolve(registry.Catalog, ReadTerrain(registry), out var result, out var error))
                throw new InvalidOperationException(error);
            foreach (var p in terrainPlacements.placements)
            {
                var e = result.entriesById[p.catalogId];
                if (!ContainsCell(p.x, p.y) || !ContainsCell(p.x + e.width - 1, p.y + e.height - 1))
                    throw new InvalidOperationException("Structure footprint is outside authored chunks: " + p.instanceId);
            }
            return result;
        }

#if UNITY_EDITOR
        public void EditorAddValidationTerrainCell(Vector2Int position)
        {
            if (UnityEditor.AssetDatabase.Contains(this)) throw new InvalidOperationException("Validation expansion is only permitted on a disposable map clone.");
            cells.Add(new StageMapCell(position.x, position.y, StageMapLayer.Terrain, "__VALIDATION_ONLY__", ""));
        }
#endif

        public void ValidateTerrainCandidate(StageTerrainStructureRegistry registry, AnimolTerrainSavedMap candidate)
        {
            if (candidate == null || candidate.themeId != themeId || candidate.revision != authoringRevision + 1)
                throw new InvalidOperationException("Stale candidate or changed map identity/revision.");
            if (!Engine.Resolve(registry.Catalog, candidate, out var resolved, out var error)) throw new InvalidOperationException(error);
            if(candidate.freeShape!=null && candidate.freeShape.HasData)
            {
                if(!Mathf.Approximately(worldUnitsPerCell,1))throw new InvalidOperationException("자유형 지형은 1셀=1unit 설정이 필요합니다. 맵 설정에서 먼저 지정하세요.");
                var art=FreeShapeArtRegistry.Load(candidate.freeShape);
                foreach(var style in candidate.freeShape.cells.Select(c=>c.styleId).Distinct())art.Style(style);
            }
            foreach (var p in candidate.placements)
            {
                var e = resolved.entriesById[p.catalogId];
                if (!ContainsCell(p.x, p.y) || !ContainsCell(p.x + e.width - 1, p.y + e.height - 1))
                    throw new InvalidOperationException("Structure art footprint is outside authored chunks: " + p.instanceId);
                if (!registry.Frame(e.frameId).ValidateForFootprint(e.width, e.height, out error)) throw new InvalidOperationException(error);
            }
            if (resolved.solids.Keys.Any(p => !ContainsCell(p.x, p.y))) throw new InvalidOperationException("Terrain is outside authored chunks.");
            var previous = ResolveTerrain(registry);
            var added = new HashSet<Vector2Int>(resolved.solids.Keys.Except(previous.solids.Keys));
            foreach (var item in objects)
            {
                if (EnumeratePlacementCells(item).Any(added.Contains))
                    throw new InvalidOperationException("Terrain overlaps object footprint/swept path: " + item.StableId);
                if (item.Kind == StageMapObjectKind.MoonPhaseStair)
                {
                    int side = Mathf.CeilToInt(Mathf.Max(item.Settings.FootprintCells.x, item.Settings.FootprintCells.y));
                    for (int x = 0; x < side; x++) for (int y = 0; y < side; y++)
                        if (added.Contains(new Vector2Int(item.X + x, item.Y + y)))
                            throw new InvalidOperationException("Terrain overlaps rotating object: " + item.StableId);
                }
            }
        }

        // Atomic authoring API: only the terrain diff and placement collection change.
        // Existing cell instances retain exact tileId/variant; objects and unknown type/settings data are untouched.
        public void EditorApplyTerrainCandidate(StageTerrainStructureRegistry registry, AnimolTerrainSavedMap candidate,
            StageMapCell explicitCell = null)
        {
            ValidateTerrainCandidate(registry, candidate);
            var existing = cells.Where(c => c.Layer == StageMapLayer.Terrain).ToDictionary(c => new Vector2Int(c.X, c.Y));
            var replacement = cells.Where(c => c.Layer != StageMapLayer.Terrain).ToList();
            foreach (var c in candidate.baseCells)
            {
                var pos = new Vector2Int(c.x, c.y);
                if (explicitCell != null && explicitCell.X == c.x && explicitCell.Y == c.y) replacement.Add(explicitCell);
                else if (existing.TryGetValue(pos, out var old) && registry.Style(themeId, old.TileId, old.VariantId) == c.styleId) replacement.Add(old);
                else
                {
                    var binding = registry.Preferred(themeId, c.styleId);
                    replacement.Add(new StageMapCell(c.x, c.y, StageMapLayer.Terrain, binding.tileId, binding.variantId));
                }
            }
            var copy = Engine.CloneMap(candidate);
            freeShapeTerrain = copy.freeShape;
            cells = replacement;
            terrainPlacements = new AnimolTerrainPlacementCollection { schemaVersion = copy.schemaVersion, placements = copy.placements };
            NotifyAuthoredChange();
            collisionDataRevision = -1;
        }

        private bool EditStructuredBaseCell(int x, int y, string tileId, string variantId, bool erase)
        {
            var registry = StageTerrainStructureRegistry.Load();
            var dto = ReadTerrain(registry);
            var current = FindCell(x, y, StageMapLayer.Terrain);
            var resolved = ResolveTerrain(registry);
            if (resolved.solids.TryGetValue(new Vector2Int(x, y), out var solid) && !string.IsNullOrEmpty(solid.ownerId))
                throw new InvalidOperationException("Select complete structure instance " + solid.ownerId + " to move/delete its owned # cells.");
            if (erase && current == null) return false;
            var style = erase ? null : registry.Style(themeId, tileId, variantId);
            AnimolTerrainSavedMap candidate;
            if (!erase && current != null && registry.Style(themeId, current.TileId, current.VariantId) == style)
            { candidate = Engine.CloneMap(dto); candidate.revision++; }
            else if (!Engine.TrySetBaseCell(registry.Catalog, dto, new Vector2Int(x, y), !erase, style, out candidate, out var error))
                throw new InvalidOperationException(error);
            EditorApplyTerrainCandidate(registry, candidate, erase ? null : new StageMapCell(x, y, StageMapLayer.Terrain, tileId, variantId));
            return true;
        }
    }
}
