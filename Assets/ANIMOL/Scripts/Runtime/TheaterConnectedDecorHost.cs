using System;
using System.Collections.Generic;
using ANIMOL.Art.TheaterConnectedDecorV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Adapts the existing PuzzleMap decoration records to the theater's visual-only snapshot.
    // The authoritative map and gameplay occupancy are never written by this adapter.
    public static class TheaterConnectedDecorHost
    {
        public sealed class Mapping
        {
            public readonly string key;
            public readonly bool network;
            public readonly int width, height;
            public Mapping(string key, bool network, int width, int height)
            { this.key = key; this.network = network; this.width = width; this.height = height; }
        }

        public static bool IsTheater(PuzzleMap map) => map && map.theme &&
            map.theme.memorySkyTheater && map.theme.memorySkyTheater.v02;

        public static bool IsDeclaredKey(string key)
        {
            if (TheaterConnectedDecorV01Planner.IsNetworkFamily(key)) return true;
            return TheaterConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out _);
        }

        public static Mapping SourceMapping(PuzzleTheme theme, Sprite sprite)
        {
            if (!theme || !theme.memorySkyTheater || !theme.memorySkyTheater.v02 || !sprite ||
                !theme.theaterConnectedDecorV01Catalog || !theme.theaterConnectedDecorV01ManifestPin) return null;
            var catalog = theme.theaterConnectedDecorV01Catalog;
            var pin = theme.theaterConnectedDecorV01ManifestPin.text.Trim();
            foreach (var family in TheaterConnectedDecorV01Planner.NetworkFamilies)
                if (catalog.TryGetExact(TheaterConnectedDecorV01Planner.NetworkFile(family, 0), pin,
                    out var art, out _) && art.Sprite == sprite)
                    return new Mapping(family, true, 1, 1);
            foreach (var key in TheaterConnectedDecorV01Planner.GroupKeys)
                if (catalog.TryGetGroup(key, pin, out var group, out var art, out _) && art.Sprite == sprite)
                    return new Mapping(key, false, group.WidthCells, group.HeightCells);
            return null;
        }

        public static bool TryMap(PuzzleMap map, DecorationCell cell, out Mapping mapping)
        {
            mapping = IsTheater(map) ? SourceMapping(map.theme, cell.sprite) : null;
            return mapping != null && cell.tint == Color.white &&
                (string.IsNullOrEmpty(cell.visualFamily) || cell.visualFamily == mapping.key);
        }

        public static bool Occupies(PuzzleMap map, DecorationCell cell, Vector2Int position)
        {
            if (!TryMap(map, cell, out var mapping)) return cell.position == position;
            return position.x >= cell.position.x && position.x < cell.position.x + mapping.width &&
                position.y >= cell.position.y && position.y < cell.position.y + mapping.height;
        }

        // The current editor's decoration brush permits in-bounds cells and rejects
        // overlap with another decoration. V03's authored catwalk owns its rail art.
        public static bool AllowDecorCell(PuzzleMap map, IList<DecorationCell> decorations, Vector2Int position)
        {
            if (!map.Contains(position)) return false;
            var layout = map.theme.memorySkyTheater.defaultLayout;
            if (layout && layout.visualDeck.Contains(position)) return false;
            foreach (var cell in decorations)
                if (!TryMap(map, cell, out _) && Occupies(map, cell, position)) return false;
            return true;
        }

        public static bool TryCreateSnapshot(PuzzleMap map, IList<DecorationCell> decorations,
            Vector3 worldOrigin, float cellSize, long revision,
            out TheaterConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null; error = null;
            if (!IsTheater(map) || !map.theme.theaterConnectedDecorV01Catalog ||
                !map.theme.theaterConnectedDecorV01ManifestPin)
            { error = "The theater map needs its exact connected decoration catalog and art pin."; return false; }
            var pin = map.theme.theaterConnectedDecorV01ManifestPin.text.Trim();
            if (!map.theme.theaterConnectedDecorV01Catalog.ValidateIntegrity(pin, out error)) return false;
            var networks = new List<TheaterDecorNetworkCell>();
            var groups = new List<TheaterDecorGroupPlacement>();
            var planes = new HashSet<Vector2Int>();
            for (var i = 0; i < decorations.Count; i++)
            {
                var cell = decorations[i];
                if (!map.Contains(cell.position))
                {
                    if (IsDeclaredKey(cell.visualFamily) || TryMap(map, cell, out _))
                    { error = "A connected theater decoration anchor is outside the current board."; return false; }
                    continue; // Legacy cropped records remain serialized for the host's existing validation.
                }
                if (!TryMap(map, cell, out var mapping))
                {
                    if (IsDeclaredKey(cell.visualFamily))
                    { error = "Current theater decoration Sprite does not match its declared visual key: " + cell.visualFamily; return false; }
                    continue;
                }
                planes.Add(new Vector2Int(cell.logicalLayer, cell.heightKey));
                if (mapping.network)
                    networks.Add(new TheaterDecorNetworkCell(cell.position.x, cell.position.y, mapping.key,
                        string.IsNullOrWhiteSpace(cell.connectionGroup) ? mapping.key : cell.connectionGroup,
                        cell.logicalLayer, cell.heightKey, 15));
                else
                    groups.Add(new TheaterDecorGroupPlacement(
                        string.IsNullOrEmpty(cell.placementId) ? "existing:" + i : cell.placementId,
                        mapping.key, cell.position.x, cell.position.y, cell.logicalLayer, cell.heightKey, 15));
            }
            var allowed = new List<TheaterDecorAllowedCell>();
            foreach (var plane in planes)
                for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
                    if (AllowDecorCell(map, decorations, new Vector2Int(x, y)))
                        allowed.Add(new TheaterDecorAllowedCell(x, y, plane.x, plane.y));
            snapshot = new TheaterConnectedDecorV01BoardSnapshot {
                packageId = TheaterConnectedDecorV01Catalog.PackageId,
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
                TheaterConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                    key => {
                        if (!map.theme.theaterConnectedDecorV01Catalog.TryGetGroup(key, pin,
                            out var group, out _, out var reason)) throw new InvalidOperationException(reason);
                        return new TheaterDecorGroupSize(group.WidthCells, group.HeightCells);
                    }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing,
                    TheaterConnectedDecorV01Planner.SupportsPeriodicPost);
                return true;
            }
            catch (Exception exception)
            { error = "The complete theater decoration placement is invalid: " + exception.Message; snapshot = null; return false; }
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
