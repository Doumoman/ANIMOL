using System;
using System.Collections.Generic;
using ANIMOL.Art.SanctumConnectedDecorV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Converts the authoritative PuzzleMap's whole decoration list to visual-only DTOs.
    // The approved-cell query is pure; this class never reserves gameplay cells.
    public static class SanctumConnectedDecorHost
    {
        public sealed class Mapping
        {
            public readonly string key;
            public readonly bool network;
            public readonly int width, height;
            public Mapping(string key, bool network, int width, int height)
            { this.key = key; this.network = network; this.width = width; this.height = height; }
        }

        public static bool IsSanctum(PuzzleMap map) => map && map.theme && map.theme.sunsetSanctumV02Catalog;

        public static bool IsDeclaredKey(string key) =>
            SanctumConnectedDecorV01Planner.IsNetworkFamily(key) ||
            SanctumConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out _);

        public static Mapping SourceMapping(PuzzleTheme theme, Sprite sprite)
        {
            if (!theme || !theme.sunsetSanctumV02Catalog || !sprite ||
                !theme.sanctumConnectedDecorV01Catalog || !theme.sanctumConnectedDecorV01ManifestPin) return null;
            var catalog = theme.sanctumConnectedDecorV01Catalog;
            var pin = theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
            foreach (var family in SanctumConnectedDecorV01Planner.NetworkFamilies)
                if (catalog.TryGetExact(SanctumConnectedDecorV01Planner.NetworkFile(family, 0), pin,
                    out var art, out _) && art.Sprite == sprite)
                    return new Mapping(family, true, 1, 1);
            foreach (var key in SanctumConnectedDecorV01Planner.GroupKeys)
                if (catalog.TryGetGroup(key, pin, out var group, out var art, out _) && art.Sprite == sprite)
                    return new Mapping(key, false, group.WidthCells, group.HeightCells);
            return null;
        }

        public static bool IsLegacySanctumProp(PuzzleTheme theme, Sprite sprite)
        {
            if (!theme || !theme.sunsetSanctumV02Catalog || !sprite) return false;
            var catalog = theme.sunsetSanctumV02Catalog;
            for (var i = 0; i < catalog.SpriteCount; i++)
            {
                var record = catalog.GetSpriteRecord(i);
                if (record.File.StartsWith("props/sanctum/", StringComparison.Ordinal) && record.Sprite == sprite)
                    return true;
            }
            return false;
        }

        public static bool TryMap(PuzzleMap map, DecorationCell cell, out Mapping mapping)
        {
            mapping = IsSanctum(map) ? SourceMapping(map.theme, cell.sprite) : null;
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
            if (!map.Contains(position)) return false;
            // V02's stone/wood bridge edge sprites own their rail and shared corner art.
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
            out SanctumConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null; error = null;
            if (!IsSanctum(map) || !map.theme.sanctumConnectedDecorV01Catalog ||
                !map.theme.sanctumConnectedDecorV01ManifestPin)
            { error = "The Sanctum map needs its exact connected decoration catalog and art pin."; return false; }
            var pin = map.theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
            if (!map.theme.sanctumConnectedDecorV01Catalog.ValidateIntegrity(pin, out error)) return false;
            var networks = new List<SanctumDecorNetworkCell>();
            var groups = new List<SanctumDecorGroupPlacement>();
            var planes = new HashSet<Vector2Int>();
            for (var i = 0; i < decorations.Count; i++)
            {
                var cell = decorations[i];
                if (!map.Contains(cell.position))
                {
                    if (IsDeclaredKey(cell.visualFamily) || TryMap(map, cell, out _))
                    { error = "A connected Sanctum decoration anchor is outside the current board."; return false; }
                    continue;
                }
                if (!TryMap(map, cell, out var mapping))
                {
                    if (IsLegacySanctumProp(map.theme, cell.sprite))
                    { error = "A V02 Sanctum prop requires a reviewed V01 mapping and full footprint before display."; return false; }
                    if (IsDeclaredKey(cell.visualFamily))
                    { error = "Current Sanctum decoration Sprite does not match its declared visual key: " + cell.visualFamily; return false; }
                    continue;
                }
                planes.Add(new Vector2Int(cell.logicalLayer, cell.heightKey));
                if (mapping.network)
                    networks.Add(new SanctumDecorNetworkCell(cell.position.x, cell.position.y, mapping.key,
                        string.IsNullOrWhiteSpace(cell.connectionGroup) ? mapping.key : cell.connectionGroup,
                        cell.logicalLayer, cell.heightKey, 15));
                else
                    groups.Add(new SanctumDecorGroupPlacement(
                        string.IsNullOrEmpty(cell.placementId) ? "existing:" + i : cell.placementId,
                        mapping.key, cell.position.x, cell.position.y, cell.logicalLayer, cell.heightKey, 15));
            }
            var allowed = new List<SanctumDecorAllowedCell>();
            foreach (var plane in planes)
                for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
                    if (AllowDecorCell(map, decorations, new Vector2Int(x, y)))
                        allowed.Add(new SanctumDecorAllowedCell(x, y, plane.x, plane.y));
            snapshot = new SanctumConnectedDecorV01BoardSnapshot {
                packageId = SanctumConnectedDecorV01Catalog.PackageId,
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
                SanctumConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                    key => {
                        if (!map.theme.sanctumConnectedDecorV01Catalog.TryGetGroup(key, pin,
                            out var group, out _, out var reason)) throw new InvalidOperationException(reason);
                        return new SanctumDecorGroupSize(group.WidthCells, group.HeightCells);
                    }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing);
                return true;
            }
            catch (Exception exception)
            { error = "The complete Sanctum decoration placement is invalid: " + exception.Message; snapshot = null; return false; }
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
