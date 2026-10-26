using System;
using System.Collections.Generic;
using ANIMOL.Art.RanchConnectedDecorV02;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // The authoritative PuzzleMap owns placements and gameplay occupancy. This
    // adapter publishes a whole-board, visual-only snapshot after committed edits.
    public static class RanchConnectedDecorV02Host
    {
        public sealed class Mapping
        {
            public readonly string sourceId, family, groupKey;
            public readonly int width, height, occupiedWidth, occupiedHeight;
            public bool IsGroup => groupKey != null;
            public bool IsVisual => family != null || groupKey != null;
            public string Key => family ?? groupKey;
            public Mapping(string sourceId, string family, string groupKey, int width, int height,
                int occupiedWidth = 0, int occupiedHeight = 0)
            {
                this.sourceId = sourceId; this.family = family; this.groupKey = groupKey;
                this.width = width; this.height = height;
                this.occupiedWidth = occupiedWidth > 0 ? occupiedWidth : width;
                this.occupiedHeight = occupiedHeight > 0 ? occupiedHeight : height;
            }
        }

        public const int ForegroundSortingOrder = 41;
        // Explicit V12 Sprite identities. The barn and windmill retain their
        // established 3x1 and 2x1 gameplay occupancy despite taller artwork.
        public static readonly Mapping[] LegacyV12 = {
            new Mapping("props/fence_horizontal.png", "cream_fence", null, 2, 1),
            new Mapping("props/fence_picket_horizontal.png", "cream_fence", null, 2, 1),
            new Mapping("props/fence_vertical.png", "cream_fence", null, 1, 2),
            new Mapping("props/stone_wall.png", "meadow_stone_wall", null, 2, 1),
            new Mapping("props/yellow_flowers.png", "raised_flower_meadow", null, 1, 1),
            new Mapping("props/clover_flowers.png", "raised_flower_meadow", null, 1, 1),
            new Mapping("props/hay_pair.png", null, "hay_store_2x1", 2, 1),
            new Mapping("props/clover_tree.png", null, "tree_flower_corner_2x2", 2, 2),
            new Mapping("props/ranch_barn.png", null, "barnyard_3x2", 3, 2, 3, 1),
            new Mapping("props/water_trough.png", null, "feeding_corner_3x1", 3, 1),
            new Mapping("props/milk_cans.png", null, "milk_can_pair", 1, 1),
            new Mapping("props/bench.png", null, null, 2, 1),
            new Mapping("props/ranch_gate.png", null, null, 2, 1),
            new Mapping("props/wheelbarrow.png", null, null, 1, 2),
            new Mapping("props/crate_pair.png", null, null, 2, 1),
            new Mapping("props/windmill_body.png", null, null, 2, 2, 2, 1)
        };

        public static bool IsRanch(PuzzleMap map) => map && map.theme && map.theme.starwayRanch;
        public static bool IsDeclaredKey(string key) =>
            RanchConnectedDecorV02Planner.IsNetworkFamily(key) ||
            RanchConnectedDecorV02Planner.TryGetDeclaredGroupSize(key, out _);

        static Mapping FromKey(string key)
        {
            if (RanchConnectedDecorV02Planner.IsNetworkFamily(key))
                return new Mapping("ranch:" + key, key, null, 1, 1);
            if (RanchConnectedDecorV02Planner.TryGetDeclaredGroupSize(key, out var size))
                return new Mapping("ranch:" + key, null, key, size.width, size.height,
                    key == "barnyard_3x2" ? 3 : 0, key == "barnyard_3x2" ? 1 : 0);
            return null;
        }

        public static Mapping SourceMapping(PuzzleMap map, Sprite sprite)
        {
            if (!IsRanch(map) || !sprite) return null;
            foreach (var legacy in LegacyV12)
                if (map.theme.starwayRanch.Find(legacy.sourceId) == sprite) return legacy;

            var catalog = map.theme.ranchConnectedDecorV02Catalog;
            var pinAsset = map.theme.ranchConnectedDecorV02ManifestPin;
            if (catalog && pinAsset)
            {
                var pin = pinAsset.text.Trim();
                foreach (var family in RanchConnectedDecorV02Planner.NetworkFamilies)
                    if (catalog.TryGetExact(RanchConnectedDecorV02Planner.NetworkFile(family, 0), pin,
                        out var art, out _) && art.Sprite == sprite)
                        return FromKey(family);
                foreach (var key in RanchConnectedDecorV02Planner.GroupKeys)
                    if (catalog.TryGetGroup(key, pin, out _, out var art, out _) && art.Sprite == sprite)
                        return FromKey(key);
            }

            // Old V01 Sprite references can remain in saved placements. They are
            // input identities only; the V02 catalog alone supplies rendered art.
            var oldCatalog = map.theme.ranchConnectedDecorV01Catalog;
            if (oldCatalog)
                for (var i = 0; i < oldCatalog.SpriteCount; i++)
                {
                    var row = oldCatalog.GetSpriteAt(i);
                    if (row.Sprite == sprite &&
                        (row.Kind == "connected_decoration" || row.Kind == "connected_meadow" ||
                         row.Kind == "multi_cell_group"))
                        return FromKey(row.Family);
                }
            return null;
        }

        public static Mapping SourceMapping(PuzzleMap map, DecorationCell cell)
        {
            if (!IsRanch(map)) return null;
            if (!string.IsNullOrEmpty(cell.visualFamily))
            {
                var declared = FromKey(cell.visualFamily);
                var identified = SourceMapping(map, cell.sprite);
                return declared != null && identified != null && identified.IsVisual &&
                    identified.Key == declared.Key ? identified : null;
            }
            return SourceMapping(map, cell.sprite);
        }

        public static bool TryMap(PuzzleMap map, DecorationCell cell, out Mapping mapping)
        {
            mapping = SourceMapping(map, cell);
            return mapping != null && mapping.IsVisual && cell.tint == Color.white;
        }

        public static bool Occupies(PuzzleMap map, DecorationCell cell, Vector2Int position)
        {
            var mapping = SourceMapping(map, cell);
            if (mapping == null) return cell.position == position;
            return position.x >= cell.position.x && position.x < cell.position.x + mapping.occupiedWidth &&
                position.y >= cell.position.y && position.y < cell.position.y + mapping.occupiedHeight;
        }

        // No mutation or reservations occur in this eligibility read.
        public static bool AllowDecorCell(PuzzleMap map, IList<DecorationCell> decorations, Vector2Int position)
        {
            if (!map.Contains(position)) return false;
            if (map.TryTerrain(position, out var tile) &&
                (tile.material == TerrainMaterial.Stone || tile.material == TerrainMaterial.Sand))
                foreach (var direction in PuzzleAutotile.Cardinal)
                    if (!map.TryTerrain(position + direction, out _)) return false;
            foreach (var cell in decorations)
                if (!TryMap(map, cell, out _) && Occupies(map, cell, position)) return false;
            return true;
        }

        public static bool TryCreateSnapshot(PuzzleMap map, IList<DecorationCell> decorations,
            Vector3 worldOrigin, float cellSize, long revision,
            out RanchConnectedDecorV02BoardSnapshot snapshot, out string error)
        {
            snapshot = null; error = null;
            if (!IsRanch(map) || !map.theme.ranchConnectedDecorV02Catalog ||
                !map.theme.ranchConnectedDecorV02ManifestPin)
            { error = "The Ranch map needs its exact V02 catalog and manifest pin."; return false; }
            var catalog = map.theme.ranchConnectedDecorV02Catalog;
            var pin = map.theme.ranchConnectedDecorV02ManifestPin.text.Trim();
            if (!catalog.ValidateIntegrity(pin, out error)) return false;
            var networks = new List<RanchDecorNetworkCell>();
            var groups = new List<RanchDecorGroupPlacement>();
            var planes = new HashSet<Vector2Int>();
            var claimed = new HashSet<Vector2Int>();
            for (var i = 0; i < decorations.Count; i++)
            {
                var cell = decorations[i];
                if (!map.Contains(cell.position))
                {
                    if (!string.IsNullOrEmpty(cell.visualFamily) || TryMap(map, cell, out _))
                    { error = "A connected Ranch decoration anchor is outside the board."; return false; }
                    continue;
                }
                if (!TryMap(map, cell, out var mapping))
                {
                    if (!string.IsNullOrEmpty(cell.visualFamily))
                    { error = "Declared Ranch visual key has an invalid tint or Sprite."; return false; }
                    continue;
                }
                for (var dy = 0; dy < mapping.height; dy++) for (var dx = 0; dx < mapping.width; dx++)
                    if (!claimed.Add(cell.position + new Vector2Int(dx, dy)))
                    { error = "Ranch decorations overlap across visual layers or heights."; return false; }
                planes.Add(new Vector2Int(cell.logicalLayer, cell.heightKey));
                if (mapping.IsGroup)
                    groups.Add(new RanchDecorGroupPlacement(
                        string.IsNullOrEmpty(cell.placementId) ? "existing:" + i : cell.placementId,
                        mapping.groupKey, cell.position.x, cell.position.y,
                        cell.logicalLayer, cell.heightKey,
                        mapping.groupKey == "barnyard_3x2" ? ForegroundSortingOrder : 15));
                else
                {
                    var connection = string.IsNullOrWhiteSpace(cell.connectionGroup)
                        ? mapping.family : cell.connectionGroup;
                    for (var dy = 0; dy < mapping.height; dy++) for (var dx = 0; dx < mapping.width; dx++)
                        networks.Add(new RanchDecorNetworkCell(cell.position.x + dx, cell.position.y + dy,
                            mapping.family, connection, cell.logicalLayer, cell.heightKey, 15));
                }
            }
            var allowed = new List<RanchDecorAllowedCell>();
            foreach (var plane in planes)
                for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
                    if (AllowDecorCell(map, decorations, new Vector2Int(x, y)))
                        allowed.Add(new RanchDecorAllowedCell(x, y, plane.x, plane.y));
            snapshot = new RanchConnectedDecorV02BoardSnapshot {
                packageId = RanchConnectedDecorV02Catalog.PackageId,
                sourceManifestSha256 = pin,
                boardId = string.IsNullOrWhiteSpace(map.name) ? "map-instance-" + map.GetInstanceID() : map.name,
                revision = revision, isWholeBoard = true, isExampleOnly = false,
                originX = worldOrigin.x, originY = worldOrigin.y, originZ = worldOrigin.z,
                cellSize = cellSize, seed = StableSeed(map.name), postSpacing = 4,
                networkCells = networks.ToArray(), groupPlacements = groups.ToArray(),
                allowedDecorCells = allowed.ToArray()
            };
            if (!snapshot.TryValidate(out error)) { snapshot = null; return false; }
            try
            {
                RanchConnectedDecorV02Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                    key => {
                        if (!catalog.TryGetGroup(key, pin, out var group, out _, out var reason))
                            throw new InvalidOperationException(reason);
                        return new RanchDecorGroupSize(group.WidthCells, group.HeightCells);
                    }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing);
                return true;
            }
            catch (Exception exception)
            { error = "The whole Ranch V02 decoration layout is invalid: " + exception.Message; snapshot = null; return false; }
        }

        static int StableSeed(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var ch in value ?? string.Empty) { hash ^= ch; hash *= 16777619u; }
                return (int)hash;
            }
        }
    }
}
