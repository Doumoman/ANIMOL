using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Core
{
    public static class StageMapMarkerValidation
    {
        public static StageMapValidationReport ValidateLocalPlay(StageMapDefinition map)
        {
            var report = StageMapValidator.ValidateStructure(map);
            if (map == null) return report;
            if (map.Objects.Count(o => o.Kind == StageMapObjectKind.PlayerStart) != 1) report.Errors.Add("Local Play requires exactly one PlayerStart.");
            if (map.Objects.Count(o => o.Kind == StageMapObjectKind.Exit) != 1) report.Errors.Add("Local Play requires exactly one Exit.");
            if (map.Objects.Count(o => o.Kind == StageMapObjectKind.BubbleCandidate) < 3) report.Errors.Add("Local Play requires at least three unique BubbleCandidate IDs.");
            foreach (var marker in map.Objects.Where(o => o.Kind is StageMapObjectKind.PlayerStart or StageMapObjectKind.Checkpoint))
                report.Errors.AddRange(ValidateSafeFeet(map, marker));
            return report;
        }

        public static IReadOnlyList<string> ValidateSafeFeet(StageMapDefinition map, StageMapObjectPlacement marker,
            IEnumerable<StageMapObjectPlacement> proposedObjects = null)
        {
            var errors = new List<string>();
            if (marker.Kind is not (StageMapObjectKind.PlayerStart or StageMapObjectKind.Checkpoint)) return errors;
            var units=map.GetEditorPreviewUnitsPerCell();
            var offset = marker.Settings.CheckpointRespawnOffsetCells;
            if (!float.IsFinite(offset.x) || !float.IsFinite(offset.y)) { errors.Add(marker.StableId + ": invalid safe feet offset."); return errors; }
            var feet = new Vector2(marker.X + .5f, marker.Y) + offset;
            var body = Gameplay.CampaignMapMarkerGeometry.PlayerBodyRectAtFeetInCells(feet, units);
            // Exclude the supporting floor contact, without shrinking the head clearance.
            body.yMin += .001f;
            var solid = new HashSet<Vector2Int>(map.Cells.Where(c => c.Layer == StageMapLayer.Terrain).Select(c => new Vector2Int(c.X, c.Y)));
            solid.UnionWith(map.FreeShapeTerrain.cells.Select(c => new Vector2Int(c.x, c.y)));
            if (map.HasTerrainStructures) solid.UnionWith(map.ResolveTerrain(Gameplay.StageTerrainStructureRegistry.Load()).solids.Keys);
            if (solid.Any(c => body.Overlaps(new Rect(c.x, c.y, 1, 1)))) errors.Add(marker.StableId + ": respawn body overlaps solid terrain.");
            if (!solid.Contains(new Vector2Int(Mathf.FloorToInt(feet.x), Mathf.FloorToInt(feet.y - .01f))))
                errors.Add(marker.StableId + ": safe feet require a supporting floor directly below.");
            var objects = proposedObjects ?? map.Objects;
            if (objects.Where(o => o.StableId != marker.StableId && !CommonObstacleCatalog.IsMarker(o.Kind))
                .SelectMany(StageMapDefinition.EnumeratePlacementCells).Any(c => body.Overlaps(new Rect(c.x, c.y, 1, 1))))
                errors.Add(marker.StableId + ": respawn body overlaps a hazard or device.");
            if (!map.CellBounds.Contains(new Vector2Int(Mathf.FloorToInt(body.xMin), Mathf.FloorToInt(body.yMin))) ||
                !map.CellBounds.Contains(new Vector2Int(Mathf.FloorToInt(body.xMax), Mathf.FloorToInt(body.yMax))))
                errors.Add(marker.StableId + ": respawn body is outside map bounds.");
            return errors;
        }
    }
}
