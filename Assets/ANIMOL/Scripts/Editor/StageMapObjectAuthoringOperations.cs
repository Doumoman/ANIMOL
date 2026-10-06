using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed class StageMapObjectPlacementValidation
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public static class StageMapObjectAuthoringOperations
    {
        public static Vector2 SnapWorldToLogical(Vector2 world, float unitsPerCell, StageMapObjectTypeDefinition type,
            HalfBlockPlacement halfPlacement = HalfBlockPlacement.Lower)
        {
            var units = Mathf.Max(.01f, unitsPerCell);
            var logical = world / units;
            logical.x = Mathf.Floor(logical.x);
            if (type != null && Mathf.Approximately(type.VerticalSnapCells, .5f))
                logical.y = Mathf.Floor(logical.y) + (halfPlacement == HalfBlockPlacement.Upper ? .5f : 0f);
            else logical.y = Mathf.Floor(logical.y);
            return logical;
        }

        public static StageMapObjectPlacementValidation ValidatePlacement(StageMapDefinition map, StageMapObjectPlacement candidate,
            string replacingStableId = null)
        {
            var result = new StageMapObjectPlacementValidation();
            if (map == null || candidate == null) { result.Errors.Add("Map and candidate are required."); return result; }
            if (string.IsNullOrWhiteSpace(candidate.StableId)) result.Errors.Add("Stable ID is required.");
            if (map.Objects.Any(item => item.StableId == candidate.StableId && item.StableId != replacingStableId))
                result.Errors.Add($"Stable ID {candidate.StableId} already exists.");
            var cells = StageMapDefinition.EnumeratePlacementCells(candidate).Distinct().ToArray();
            var outside = cells.Where(cell => !map.CanAuthorCell(cell.x, cell.y)).ToArray();
            if (outside.Length > 0)
            {
                var outsideCells = outside.Distinct().OrderBy(x => x.x).ThenBy(x => x.y);
                result.Errors.Add("Footprint/path crosses map bounds. Expand the map to include: " + string.Join(", ", outsideCells));
            }
            var occupiedByObjects = new HashSet<Vector2Int>(map.Objects.Where(item => item.StableId != replacingStableId)
                .SelectMany(StageMapDefinition.EnumeratePlacementCells));
            if (candidate.Kind != StageMapObjectKind.RiceSlow && cells.Any(occupiedByObjects.Contains))
                result.Errors.Add("Footprint/path overlaps another authored object.");
            var solidTerrain = new HashSet<Vector2Int>(map.Cells.Where(item => item.Layer == StageMapLayer.Terrain)
                .Select(item => new Vector2Int(item.X, item.Y)));
            if(map.HasTerrainStructures)
            {
                try { solidTerrain.UnionWith(map.ResolveTerrain(ANIMOL.Gameplay.StageTerrainStructureRegistry.Load()).solids.Keys); }
                catch(Exception ex){result.Errors.Add(ex.Message);return result;}
            }
            if (RequiresPath(candidate.Kind) && cells.Skip(1).Any(solidTerrain.Contains))
                result.Errors.Add("Moving path crosses authored terrain.");
            if (candidate.Kind == StageMapObjectKind.Pounder && candidate.Settings.PathCells.Count < 2)
                result.Errors.Add("Pounder requires authored upper/lower path cells.");
            if (candidate.Kind == StageMapObjectKind.RailPlatform && candidate.Settings.PathCells.Count < 2)
                result.Errors.Add("Rail platform requires at least start/end path nodes.");
            if (RequiresPath(candidate.Kind) && candidate.Settings.PathCells.Count < 2)
                result.Errors.Add($"{candidate.DataKey} requires at least start/end path nodes.");
            if (candidate.Settings.LinkedInstanceIds.Count < candidate.Settings.MinimumLinkCount)
                result.Errors.Add($"{candidate.DataKey} requires {candidate.Settings.MinimumLinkCount} stable-ID link(s).");
            if (candidate.Kind == StageMapObjectKind.RiceSlow && !Mathf.Approximately(candidate.Settings.FootprintCells.x, 2f))
                result.Errors.Add("Rice slow zone must remain two cells wide.");
            if (candidate.Kind == StageMapObjectKind.HalfBlock && !Mathf.Approximately(candidate.Settings.FootprintCells.y, .5f))
                result.Errors.Add("Half block height must remain 0.5 cell.");
            if (candidate.Kind == StageMapObjectKind.MoonLanternStep)
            {
                if (candidate.Settings.FootprintCells.x < 1f || candidate.Settings.FootprintCells.x > 2f)
                    result.Errors.Add("Moon lantern step width must remain one or two cells.");
                if (candidate.Settings.WarningSeconds <= 0f || candidate.Settings.RecoverSeconds <= 0f)
                    result.Errors.Add("Moon lantern step requires visible warning and recovery times.");
                if (!candidate.Settings.DeferWhileOccupied)
                    result.Errors.Add("Moon lantern step must defer disappearance while occupied.");
                if (!solidTerrain.Any(cell => cell.y < candidate.Y && Mathf.Abs(cell.x - candidate.X) <= 2))
                    result.Warnings.Add("Moon lantern step has no authored lower recovery floor nearby; play-check the fall route.");
            }
            if (candidate.Kind == StageMapObjectKind.MoonRabbitBowl && candidate.Settings.VerticalImpulse <= 0f)
                result.Errors.Add("Rabbit bowl requires a positive vertical-only next-jump assist.");
            if (candidate.Kind == StageMapObjectKind.MoonJadePendulum && candidate.Settings.ArcHeightCells <= 0f)
                result.Errors.Add("Jade pendulum requires a positive authored arc height.");
            if (candidate.Kind == StageMapObjectKind.MoonSlidingEave && candidate.Settings.PathCells.Count >= 2 &&
                candidate.Settings.PathCells[0].y != candidate.Settings.PathCells[1].y)
                result.Errors.Add("Sliding eave path must remain horizontal.");
            if (candidate.Kind == StageMapObjectKind.MoonPhaseStair)
            {
                if (!Mathf.Approximately(Mathf.Abs(candidate.Settings.RotationDegrees), 90f))
                    result.Errors.Add("Moon phase stair rotation must remain 90 degrees.");
                if (!candidate.Settings.DeferWhileOccupied)
                    result.Errors.Add("Moon phase stair must defer rotation while occupied.");
                var side = Mathf.CeilToInt(Mathf.Max(candidate.Settings.FootprintCells.x, candidate.Settings.FootprintCells.y));
                var rotationCells = Enumerable.Range(0, side).SelectMany(dx => Enumerable.Range(0, side)
                    .Select(dy => new Vector2Int(candidate.X + dx, candidate.Y + dy))).ToArray();
                var ownCells = new HashSet<Vector2Int>(cells);
                if (rotationCells.Where(cell => !ownCells.Contains(cell)).Any(occupiedByObjects.Contains))
                    result.Errors.Add("Moon phase stair rotated footprint overlaps another authored object.");
                if (rotationCells.Where(cell => !ownCells.Contains(cell)).Any(solidTerrain.Contains))
                    result.Errors.Add("Moon phase stair rotated footprint crosses authored terrain.");
                if (rotationCells.Any(cell => !map.CanAuthorCell(cell.x, cell.y)))
                    result.Errors.Add("Moon phase stair rotated footprint crosses map bounds.");
            }
            if (candidate.Kind == StageMapObjectKind.SideSpring)
            {
                var landingX = candidate.X + (candidate.Settings.Direction == StageMapObjectDirection.Left ? -3 : 3);
                if (!map.CanAuthorCell(landingX, candidate.Y + 2))
                    result.Warnings.Add("Side spring predicted landing leaves map bounds; expand/view the landing route before play.");
            }
            if (candidate.Kind == StageMapObjectKind.Pounder && candidate.Settings.WarningSeconds <= 0f)
                result.Errors.Add("Pounder requires a visible pre-drop warning time.");
            if (candidate.Kind == StageMapObjectKind.RiceSlow)
            {
                var alignedPounder = map.Objects.Any(item => item.Kind == StageMapObjectKind.Pounder && item.X == candidate.X &&
                    Mathf.Approximately(item.Settings.FootprintCells.x, candidate.Settings.FootprintCells.x));
                if (!alignedPounder) result.Warnings.Add("Rice is not horizontally aligned with a 2-cell pounder. Manual placement is allowed; play-check evasion space.");
            }
            return result;
        }

        public static bool Place(StageMapDefinition map, StageMapObjectTypeDefinition type, Vector2Int cell, string stableId,
            StageMapObjectSettings overrideSettings, out StageMapObjectPlacementValidation validation)
        {
            validation = new StageMapObjectPlacementValidation();
            if (map == null || type == null) { validation.Errors.Add("Map and registered type are required."); return false; }
            var settings = (overrideSettings ?? type.DefaultSettings).Clone();
            var candidate = new StageMapObjectPlacement(stableId, type.Kind, cell.x, cell.y, type.StableTypeId, type.Prefab, settings);
            validation = ValidatePlacement(map, candidate);
            if (!validation.IsValid) return false;
            CreateAutosaveBackup(map, "before-place");
            Undo.RecordObject(map, "Place ANIMOL Map Object");
            map.EditorPlaceObject(stableId, type.Kind, cell.x, cell.y, type.StableTypeId, type.Prefab, settings);
            Save(map);
            return true;
        }

        public static bool PlacePounderRiceSet(StageMapDefinition map, StageMapObjectTypeRegistry registry, Vector2Int pounderUpperCell,
            string stablePrefix, out string message)
        {
            message = string.Empty;
            var pounderType = registry?.Find(StageMapObjectKind.Pounder);
            var riceType = registry?.Find(StageMapObjectKind.RiceSlow);
            if (map == null || pounderType == null || riceType == null) { message = "Registry is missing pounder/rice types."; return false; }
            var pounderSettings = pounderType.DefaultSettings.Clone();
            pounderSettings.EditorConfigure(pounderSettings.Version, pounderSettings.Direction, new Vector2(2f, 3f), pounderSettings.HalfPlacement,
                pounderSettings.SpringContactPolicy, pounderSettings.HorizontalImpulse, pounderSettings.VerticalImpulse, pounderSettings.MovementSpeed,
                pounderSettings.UpperPauseSeconds, pounderSettings.LowerPauseSeconds, pounderSettings.ActivationRangeCells,
                pounderSettings.GroundSpeedMultiplier, pounderSettings.EndStopSeconds, pounderSettings.ReturnWhenEmpty,
                new[] { pounderUpperCell, pounderUpperCell + new Vector2Int(0, -3) });
            var riceCell = pounderUpperCell + new Vector2Int(0, -4);
            var prefix = string.IsNullOrWhiteSpace(stablePrefix) ? $"POUNDER-SET-{Guid.NewGuid():N}" : stablePrefix.Trim();
            var pounder = new StageMapObjectPlacement(prefix + "-POUNDER", pounderType.Kind, pounderUpperCell.x, pounderUpperCell.y,
                pounderType.StableTypeId, pounderType.Prefab, pounderSettings);
            var rice = new StageMapObjectPlacement(prefix + "-RICE", riceType.Kind, riceCell.x, riceCell.y,
                riceType.StableTypeId, riceType.Prefab, riceType.DefaultSettings);
            var first = ValidatePlacement(map, pounder);
            var second = ValidatePlacement(map, rice);
            if (!first.IsValid || !second.IsValid)
            {
                message = string.Join(" | ", first.Errors.Concat(second.Errors));
                return false;
            }
            CreateAutosaveBackup(map, "before-pounder-rice");
            Undo.RecordObject(map, "Place ANIMOL Pounder + Rice Set");
            map.EditorPlaceObject(pounder.StableId, pounder.Kind, pounder.X, pounder.Y, pounder.DataKey, pounder.Prefab, pounder.Settings);
            map.EditorPlaceObject(rice.StableId, rice.Kind, rice.X, rice.Y, rice.DataKey, rice.Prefab, rice.Settings);
            Save(map);
            message = $"Placed {pounder.StableId} and {rice.StableId}; adjust separately and play-check evasion space.";
            return true;
        }

        public static bool Flip(StageMapDefinition map, string stableId)
        {
            var placement = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            if (placement == null) return false;
            CreateAutosaveBackup(map, "before-flip");
            Undo.RecordObject(map, "Flip ANIMOL Map Object");
            placement.EditorFlip();
            map.EditorNotifyAuthoredPropertiesChanged();
            Save(map);
            return true;
        }

        public static bool Remove(StageMapDefinition map, string stableId)
        {
            if (map == null || map.Objects.All(item => item.StableId != stableId)) return false;
            CreateAutosaveBackup(map, "before-remove");
            Undo.RecordObject(map, "Remove ANIMOL Map Object");
            var removed = map.EditorRemoveObject(stableId);
            Save(map);
            return removed;
        }

        public static int RemoveWithMutualLinks(StageMapDefinition map, string stableId)
        {
            var target = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            if (target == null) return 0;
            var removeIds = new HashSet<string>(StringComparer.Ordinal) { stableId };
            foreach (var linkedId in target.Settings.LinkedInstanceIds)
            {
                var linked = map.Objects.FirstOrDefault(item => item.StableId == linkedId);
                if (linked != null && linked.Settings.LinkedInstanceIds.Contains(stableId)) removeIds.Add(linkedId);
            }
            CreateAutosaveBackup(map, "before-remove-set");
            Undo.RecordObject(map, "Remove ANIMOL Map Object Set");
            var removed = removeIds.Count(map.EditorRemoveObject);
            Save(map);
            return removed;
        }

        public static bool Duplicate(StageMapDefinition map, string stableId, Vector2Int offset, out string newStableId,
            out StageMapObjectPlacementValidation validation)
        {
            newStableId = string.Empty;
            validation = new StageMapObjectPlacementValidation();
            var source = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            if (source == null) { validation.Errors.Add("Source object was not found."); return false; }
            var settings = source.Settings.Clone();
            settings.EditorSetLinkedInstanceIds(Array.Empty<string>());
            newStableId = $"{source.DataKey}-{Guid.NewGuid():N}";
            var candidate = new StageMapObjectPlacement(newStableId, source.Kind, source.X + offset.x, source.Y + offset.y,
                source.DataKey, source.Prefab, settings);
            validation = ValidatePlacement(map, candidate);
            if (!validation.IsValid) return false;
            CreateAutosaveBackup(map, "before-duplicate");
            Undo.RecordObject(map, "Duplicate ANIMOL Map Object");
            map.EditorPlaceObject(candidate.StableId, candidate.Kind, candidate.X, candidate.Y, candidate.DataKey, candidate.Prefab, candidate.Settings);
            Save(map);
            validation.Warnings.Add("Linked IDs were cleared on duplicate; relink the copied set before Ready validation.");
            return true;
        }

        public static bool Move(StageMapDefinition map, string stableId, Vector2Int destination,
            out StageMapObjectPlacementValidation validation)
        {
            validation = new StageMapObjectPlacementValidation();
            var source = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            if (source == null) { validation.Errors.Add("Source object was not found."); return false; }
            var delta = destination - new Vector2Int(source.X, source.Y);
            if (delta == Vector2Int.zero) return true;
            var settings = source.Settings.Clone();
            settings.EditorSetPathCells(settings.PathCells.Select(node => node + delta));
            var candidate = new StageMapObjectPlacement(source.StableId, source.Kind, destination.x, destination.y,
                source.DataKey, source.Prefab, settings);
            validation = ValidatePlacement(map, candidate, stableId);
            if (!validation.IsValid) return false;
            CreateAutosaveBackup(map, "before-move");
            Undo.RecordObject(map, "Move ANIMOL Map Object");
            map.EditorPlaceObject(candidate.StableId, candidate.Kind, candidate.X, candidate.Y,
                candidate.DataKey, candidate.Prefab, candidate.Settings);
            Save(map);
            return true;
        }

        public static bool SetPath(StageMapDefinition map, string stableId, IEnumerable<Vector2Int> pathCells,
            out StageMapObjectPlacementValidation validation)
        {
            validation = new StageMapObjectPlacementValidation();
            var source = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            if (source == null) { validation.Errors.Add("Source object was not found."); return false; }
            var requestedPath = pathCells?.ToArray() ?? Array.Empty<Vector2Int>();
            if (requestedPath.Distinct().Count() != requestedPath.Length)
            {
                validation.Errors.Add("Path nodes must use unique cells.");
                return false;
            }
            var settings = source.Settings.Clone();
            settings.EditorSetPathCells(requestedPath);
            var candidate = new StageMapObjectPlacement(source.StableId, source.Kind, source.X, source.Y,
                source.DataKey, source.Prefab, settings);
            validation = ValidatePlacement(map, candidate, stableId);
            if (!validation.IsValid) return false;
            CreateAutosaveBackup(map, "before-path");
            Undo.RecordObject(map, "Edit ANIMOL Map Object Path");
            map.EditorPlaceObject(candidate.StableId, candidate.Kind, candidate.X, candidate.Y,
                candidate.DataKey, candidate.Prefab, candidate.Settings);
            Save(map);
            return true;
        }

        public static bool PlaceLinkedPair(StageMapDefinition map, StageMapObjectTypeDefinition type, Vector2Int firstCell,
            Vector2Int secondCell, out string message)
        {
            message = string.Empty;
            if (map == null || type == null || !type.RequiresLinkedPair) { message = "A linked-pair type is required."; return false; }
            var pairValidation = ValidateLinkedPairPlacement(map, type, firstCell, secondCell);
            if (!pairValidation.IsValid) { message = string.Join(" | ", pairValidation.Errors); return false; }
            var firstId = $"{type.StableTypeId}-A-{Guid.NewGuid():N}";
            var secondId = $"{type.StableTypeId}-B-{Guid.NewGuid():N}";
            var firstSettings = BuildPairedSettings(type, firstCell, secondId, StageMapObjectDirection.Right);
            var secondSettings = BuildPairedSettings(type, secondCell, firstId, StageMapObjectDirection.Left);
            var first = new StageMapObjectPlacement(firstId, type.Kind, firstCell.x, firstCell.y, type.StableTypeId, type.Prefab, firstSettings);
            var second = new StageMapObjectPlacement(secondId, type.Kind, secondCell.x, secondCell.y, type.StableTypeId, type.Prefab, secondSettings);
            var firstValidation = ValidatePlacement(map, first);
            var secondValidation = ValidatePlacement(map, second);
            if (!firstValidation.IsValid || !secondValidation.IsValid)
            {
                message = string.Join(" | ", firstValidation.Errors.Concat(secondValidation.Errors));
                return false;
            }
            CreateAutosaveBackup(map, "before-linked-pair");
            Undo.RecordObject(map, "Place ANIMOL Linked Pair");
            map.EditorPlaceObject(first.StableId, first.Kind, first.X, first.Y, first.DataKey, first.Prefab, first.Settings);
            map.EditorPlaceObject(second.StableId, second.Kind, second.X, second.Y, second.DataKey, second.Prefab, second.Settings);
            Save(map);
            message = $"Placed linked pair {firstId} <-> {secondId}.";
            return true;
        }

        public static StageMapObjectPlacementValidation ValidateLinkedPairPlacement(StageMapDefinition map,
            StageMapObjectTypeDefinition type, Vector2Int firstCell, Vector2Int secondCell)
        {
            var result = new StageMapObjectPlacementValidation();
            if (map == null || type == null || !type.RequiresLinkedPair)
            { result.Errors.Add("A linked-pair type is required."); return result; }
            var firstSettings = BuildPairedSettings(type, firstCell, "PAIR-PREVIEW-B", StageMapObjectDirection.Right);
            var secondSettings = BuildPairedSettings(type, secondCell, "PAIR-PREVIEW-A", StageMapObjectDirection.Left);
            var first = new StageMapObjectPlacement("PAIR-PREVIEW-A", type.Kind, firstCell.x, firstCell.y, type.StableTypeId, type.Prefab, firstSettings);
            var second = new StageMapObjectPlacement("PAIR-PREVIEW-B", type.Kind, secondCell.x, secondCell.y, type.StableTypeId, type.Prefab, secondSettings);
            var firstValidation = ValidatePlacement(map, first);
            var secondValidation = ValidatePlacement(map, second);
            result.Errors.AddRange(firstValidation.Errors); result.Errors.AddRange(secondValidation.Errors);
            result.Warnings.AddRange(firstValidation.Warnings); result.Warnings.AddRange(secondValidation.Warnings);
            if (StageMapDefinition.EnumeratePlacementCells(first).Intersect(StageMapDefinition.EnumeratePlacementCells(second)).Any())
                result.Errors.Add("Linked pair footprints/paths overlap each other.");
            return result;
        }

        public static void SaveEditedProperties(StageMapDefinition map, string undoName)
        {
            if (map == null) return;
            CreateAutosaveBackup(map, "before-properties");
            map.EditorNotifyAuthoredPropertiesChanged();
            Save(map);
        }

        // V4 settings editor supplies a detached copy. Validate before touching the original,
        // retain the placement's identity/prefab, and advance authoring exactly once.
        public static bool ApplySettings(StageMapDefinition map, string stableId, StageMapObjectSettings settings,
            out StageMapObjectPlacementValidation validation)
        {
            var source = map?.Objects.FirstOrDefault(item => item.StableId == stableId);
            validation = new StageMapObjectPlacementValidation();
            if (source == null) { validation.Errors.Add("Source object was not found."); return false; }
            var candidate = new StageMapObjectPlacement(source.StableId, source.Kind, source.X, source.Y, source.DataKey, source.Prefab, settings);
            validation = ValidatePlacement(map, candidate, stableId);
            if (!validation.IsValid) return false;
            CreateAutosaveBackup(map, "before-settings");
            Undo.RecordObject(map, "Edit ANIMOL Object Settings");
            map.EditorPlaceObject(candidate.StableId, candidate.Kind, candidate.X, candidate.Y, candidate.DataKey, candidate.Prefab, candidate.Settings);
            Save(map); return true;
        }

        public static StageMapObjectSettings PlacementSettings(StageMapObjectTypeDefinition type, Vector2Int cell)
        {
            var s = type.DefaultSettings.Clone();
            var path = s.PathCells.ToArray();
            switch (type.Kind)
            {
                case StageMapObjectKind.Pounder: case StageMapObjectKind.CloudBalloonTether: path = new[] {cell,cell+new Vector2Int(0,-3)}; break;
                case StageMapObjectKind.RailPlatform: path = new[] {cell,cell+new Vector2Int(4,0)}; break;
                case StageMapObjectKind.MoonJadeBalance: path = new[] {cell,cell+Vector2Int.down}; break;
                case StageMapObjectKind.LibIndexDrawer: path = new[] {cell,cell+new Vector2Int((int)s.Direction*2,0)}; break;
                case StageMapObjectKind.GreenSandRetrace: path = new[] {cell,cell+new Vector2Int(3,0)}; break;
                case StageMapObjectKind.MineMagnetPair: case StageMapObjectKind.MoonSlidingEave: path = new[] {cell,cell+new Vector2Int((int)s.Direction,0)}; break;
                case StageMapObjectKind.MoonJadePendulum: path = new[] {cell+new Vector2Int(-2,0),cell+new Vector2Int(2,0)}; break;
            }
            s.EditorSetPathCells(path); return s;
        }

        public static string GenerateStableId(StageMapObjectKind kind) => $"{kind.ToString().ToUpperInvariant()}-{Guid.NewGuid():N}";
        public static string GenerateStableId(string stableTypeId) => $"{stableTypeId}-{Guid.NewGuid():N}";

        private static StageMapObjectSettings BuildPairedSettings(StageMapObjectTypeDefinition type, Vector2Int cell,
            string linkedId, StageMapObjectDirection direction)
        {
            var settings = type.DefaultSettings.Clone();
            var path = type.Kind == StageMapObjectKind.MoonJadeBalance
                ? new[] { cell, cell + Vector2Int.down }
                : new[] { cell, cell + new Vector2Int((int)direction, 0) };
            settings.EditorConfigure(settings.Version, direction, type.FootprintCells, settings.HalfPlacement,
                settings.SpringContactPolicy, settings.HorizontalImpulse, settings.VerticalImpulse, settings.MovementSpeed,
                settings.UpperPauseSeconds, settings.LowerPauseSeconds, settings.ActivationRangeCells,
                settings.GroundSpeedMultiplier, settings.EndStopSeconds, settings.ReturnWhenEmpty, path);
            settings.EditorConfigureAuthoring(type.ImplementationLevel, new[] { linkedId }, settings.PhaseSeed,
                settings.ResetPolicy, settings.RouteRole, 1, settings.PrototypeNotice);
            return settings;
        }

        private static bool RequiresPath(StageMapObjectKind kind) => kind is StageMapObjectKind.Pounder or
            StageMapObjectKind.RailPlatform or StageMapObjectKind.MoonJadeBalance or StageMapObjectKind.CloudBalloonTether or
            StageMapObjectKind.LibIndexDrawer or StageMapObjectKind.GreenSandRetrace or StageMapObjectKind.MineMagnetPair or
            StageMapObjectKind.MoonJadePendulum or StageMapObjectKind.MoonSlidingEave;

        private static void CreateAutosaveBackup(StageMapDefinition map, string label) =>
            StageMapBackupService.CreateBackup(map, $"autosave-{label}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}");

        private static void Save(StageMapDefinition map)
        {
            map.EditorMarkCollisionDataSynchronized();
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            StageMapAuthoringWorkspace.ScheduleRefresh(map);
            SceneView.RepaintAll();
        }
    }
}
