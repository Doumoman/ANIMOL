using System;
using System.Collections.Generic;
using ANIMOL.Art.CoastConnectedDecorV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Converts the authoritative PuzzleMap's whole decoration list to visual-only DTOs.
    // The approved-cell query is pure; this class never reserves gameplay cells.
    public static class CoastConnectedDecorHost
    {
        public sealed class Mapping
        {
            public readonly string key;
            public readonly bool network;
            public readonly int width, height;
            public Mapping(string key, bool network, int width, int height)
            { this.key = key; this.network = network; this.width = width; this.height = height; }
        }

        public static bool IsCoast(PuzzleMap map) => map && map.theme && map.theme.starSwellCoastV01Catalog;

        public static bool IsDeclaredKey(string key) =>
            CoastConnectedDecorV01Planner.IsNetworkFamily(key) ||
            CoastConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out _);

        public static Mapping SourceMapping(PuzzleTheme theme, Sprite sprite)
        {
            if (!theme || !theme.starSwellCoastV01Catalog || !sprite ||
                !theme.coastConnectedDecorV01Catalog || !theme.coastConnectedDecorV01ManifestPin) return null;
            var catalog = theme.coastConnectedDecorV01Catalog;
            var pin = theme.coastConnectedDecorV01ManifestPin.text.Trim();
            foreach (var family in CoastConnectedDecorV01Planner.NetworkFamilies)
                if (catalog.TryGetExact(CoastConnectedDecorV01Planner.NetworkFile(family, 0), pin,
                    out var art, out _) && art.Sprite == sprite)
                    return new Mapping(family, true, 1, 1);
            foreach (var key in CoastConnectedDecorV01Planner.GroupKeys)
                if (catalog.TryGetGroup(key, pin, out var group, out var art, out _) && art.Sprite == sprite)
                    return new Mapping(key, false, group.WidthCells, group.HeightCells);
            return null;
        }

        public static bool IsLegacyCoastProp(PuzzleTheme theme, Sprite sprite)
        {
            if (!theme || !theme.starSwellCoastV01Catalog || !sprite) return false;
            var catalog = theme.starSwellCoastV01Catalog;
            for (var i = 0; i < catalog.SpriteCount; i++)
            {
                var record = catalog.GetSpriteRecord(i);
                if (record.File.StartsWith("props/coast/", StringComparison.Ordinal) && record.Sprite == sprite)
                    return true;
            }
            return false;
        }

        public static bool TryMap(PuzzleMap map, DecorationCell cell, out Mapping mapping)
        {
            mapping = IsCoast(map) ? SourceMapping(map.theme, cell.sprite) : null;
            return mapping != null && cell.tint == Color.white &&
                (string.IsNullOrEmpty(cell.visualFamily) || cell.visualFamily == mapping.key);
        }

        public static bool Occupies(PuzzleMap map, DecorationCell cell, Vector2Int position)
        {
            if (!TryMap(map, cell, out var mapping)) return cell.position == position;
            return position.x >= cell.position.x && position.x < cell.position.x + mapping.width &&
                position.y >= cell.position.y && position.y < cell.position.y + mapping.height;
        }

        public static bool AllowDecorCell(PuzzleMap map, IList<DecorationCell> decorations, Vector2Int position)
        {
            // Water in the scenario is a gameplay cell, never decoration land. The
            // distant OceanField is independent of these board coordinates.
            if (!map.Contains(position) || !map.TryTerrain(position, out var tile) ||
                (map.scenario && map.scenario.water.Contains(position))) return false;
            // The existing wood/stone bridge rim owns its rail and corner sprites.
            if (
                (tile.material == TerrainMaterial.Stone || tile.material == TerrainMaterial.Sand))
                foreach (var direction in PuzzleAutotile.Cardinal)
                    if (!map.TryTerrain(position + direction, out _)) return false;
            foreach (var cell in decorations)
                if (!TryMap(map, cell, out _) && Occupies(map, cell, position)) return false;
            return true;
        }

        public static bool TryCreateSnapshot(PuzzleMap map, IList<DecorationCell> decorations,
            Vector3 worldOrigin, float cellSize, long revision,
            out CoastConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null; error = null;
            if (!IsCoast(map) || !map.theme.coastConnectedDecorV01Catalog ||
                !map.theme.coastConnectedDecorV01ManifestPin)
            { error = "The Coast map needs its exact connected decoration catalog and art pin."; return false; }
            var pin = map.theme.coastConnectedDecorV01ManifestPin.text.Trim();
            if (!map.theme.coastConnectedDecorV01Catalog.ValidateIntegrity(pin, out error)) return false;
            var networks = new List<CoastDecorNetworkCell>();
            var groups = new List<CoastDecorGroupPlacement>();
            var planes = new HashSet<Vector2Int>();
            var occupiedDecorCells = new HashSet<Vector2Int>();
            for (var i = 0; i < decorations.Count; i++)
            {
                var cell = decorations[i];
                if (!map.Contains(cell.position))
                {
                    if (IsDeclaredKey(cell.visualFamily) || TryMap(map, cell, out _))
                    { error = "A connected Coast decoration anchor is outside the current board."; return false; }
                    continue;
                }
                if (!TryMap(map, cell, out var mapping))
                {
                    if (IsLegacyCoastProp(map.theme, cell.sprite))
                    { error = "A V01 Coast prop requires a reviewed connected mapping and full footprint before display."; return false; }
                    if (IsDeclaredKey(cell.visualFamily))
                    { error = "Current Coast decoration Sprite does not match its declared visual key: " + cell.visualFamily; return false; }
                    continue;
                }
                for (var dy = 0; dy < mapping.height; dy++) for (var dx = 0; dx < mapping.width; dx++)
                {
                    var claimed = cell.position + new Vector2Int(dx, dy);
                    if (!occupiedDecorCells.Add(claimed))
                    { error = "Coast decorations overlap across visual layers or heights."; return false; }
                }
                planes.Add(new Vector2Int(cell.logicalLayer, cell.heightKey));
                if (mapping.network)
                    networks.Add(new CoastDecorNetworkCell(cell.position.x, cell.position.y, mapping.key,
                        string.IsNullOrWhiteSpace(cell.connectionGroup) ? mapping.key : cell.connectionGroup,
                        cell.logicalLayer, cell.heightKey, 15));
                else
                    groups.Add(new CoastDecorGroupPlacement(
                        string.IsNullOrEmpty(cell.placementId) ? "existing:" + i : cell.placementId,
                        mapping.key, cell.position.x, cell.position.y, cell.logicalLayer, cell.heightKey, 15));
            }
            var allowed = new List<CoastDecorAllowedCell>();
            foreach (var plane in planes)
                for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
                    if (AllowDecorCell(map, decorations, new Vector2Int(x, y)))
                        allowed.Add(new CoastDecorAllowedCell(x, y, plane.x, plane.y));
            snapshot = new CoastConnectedDecorV01BoardSnapshot {
                packageId = CoastConnectedDecorV01Catalog.PackageId,
                sourceManifestSha256 = pin,
                boardId = string.IsNullOrWhiteSpace(map.name) ? "map-instance-" + map.GetInstanceID() : map.name,
                revision = revision, isWholeBoard = true, isExampleOnly = false,
                originX = worldOrigin.x, originY = worldOrigin.y, originZ = worldOrigin.z,
                cellSize = cellSize, seed = StableSeed(map.name), postSpacing = 4,
                networkCells = networks.ToArray(), groupPlacements = groups.ToArray(), allowedDecorCells = allowed.ToArray()
            };
            if (!snapshot.TryValidate(out error)) { snapshot = null; return false; }
            try
            {
                CoastConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                    key => {
                        if (!map.theme.coastConnectedDecorV01Catalog.TryGetGroup(key, pin,
                            out var group, out _, out var reason)) throw new InvalidOperationException(reason);
                        return new CoastDecorGroupSize(group.WidthCells, group.HeightCells);
                    }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing);
                return true;
            }
            catch (Exception exception)
            { error = "The complete Coast decoration placement is invalid: " + exception.Message; snapshot = null; return false; }
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
