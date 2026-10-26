using System;
using System.Collections.Generic;
using ANIMOL.Art.RanchConnectedDecorV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Maps confirmed V12 prop Sprite identities to V01 visual families and footprints.
    // Visual claims remain separate from the map's gameplay occupancy query.
    public static class RanchConnectedDecorHost
    {
        public sealed class Mapping
        {
            public readonly string sourceId, family, groupKey;
            public readonly int width, height;
            public bool IsGroup => groupKey != null;
            public Mapping(string sourceId, string family, string groupKey, int width, int height)
            { this.sourceId = sourceId; this.family = family; this.groupKey = groupKey; this.width = width; this.height = height; }
        }

        public static readonly Mapping[] Mappings = {
            new Mapping("props/fence_horizontal.png", RanchConnectedDecorV01Planner.CreamFence, null, 2, 1),
            new Mapping("props/fence_picket_horizontal.png", RanchConnectedDecorV01Planner.CreamFence, null, 2, 1),
            new Mapping("props/fence_vertical.png", RanchConnectedDecorV01Planner.CreamFence, null, 1, 2),
            new Mapping("props/stone_wall.png", RanchConnectedDecorV01Planner.StoneWall, null, 2, 1),
            new Mapping("props/yellow_flowers.png", RanchConnectedDecorV01Planner.Meadow, null, 1, 1),
            new Mapping("props/clover_flowers.png", RanchConnectedDecorV01Planner.Meadow, null, 1, 1),
            new Mapping("props/hay_pair.png", null, "hay_store_2x1", 2, 1),
            new Mapping("props/clover_tree.png", null, "tree_flower_corner_2x2", 2, 2),
            new Mapping("props/ranch_barn.png", null, "barnyard_3x2", 3, 2),
            new Mapping("props/water_trough.png", null, "feeding_corner_3x1", 3, 1)
        };
        public static readonly Mapping[] DirectCells = {
            new Mapping("ranch:cream_fence", RanchConnectedDecorV01Planner.CreamFence, null, 1, 1),
            new Mapping("ranch:meadow_stone_wall", RanchConnectedDecorV01Planner.StoneWall, null, 1, 1),
            new Mapping("ranch:raised_flower_meadow", RanchConnectedDecorV01Planner.Meadow, null, 1, 1)
        };
        static readonly Mapping[] RetainedV12Footprints = {
            new Mapping("props/bench.png", null, null, 2, 1),
            new Mapping("props/ranch_gate.png", null, null, 2, 1),
            new Mapping("props/wheelbarrow.png", null, null, 1, 2),
            new Mapping("props/crate_pair.png", null, null, 2, 1),
            new Mapping("props/windmill_body.png", null, null, 2, 2)
        };

        public static Mapping SourceMapping(PuzzleMap map, Sprite sprite)
        {
            if (!map || !map.theme || !map.theme.starwayRanch || !sprite) return null;
            var art = map.theme.starwayRanch;
            foreach (var candidate in Mappings)
                if (art.Find(candidate.sourceId) == sprite) return candidate;
            foreach (var candidate in RetainedV12Footprints)
                if (art.Find(candidate.sourceId) == sprite) return candidate;
            var catalog = map.theme.ranchConnectedDecorV01Catalog;
            if (catalog)
                foreach (var candidate in DirectCells)
                {
                    var file = RanchConnectedDecorV01Planner.NetworkFile(candidate.family, 0);
                    for (var i = 0; i < catalog.SpriteCount; i++)
                    {
                        var record = catalog.GetSpriteAt(i);
                        if (record.File == file && record.Sprite == sprite) return candidate;
                    }
                }
            return null;
        }

        public static Mapping SourceMapping(PuzzleMap map, DecorationCell cell)
        {
            if (!string.IsNullOrEmpty(cell.visualFamily))
            {
                foreach (var candidate in DirectCells)
                    if (candidate.family == cell.visualFamily) return candidate;
                return null;
            }
            return SourceMapping(map, cell.sprite);
        }

        public static bool TryMap(PuzzleMap map, DecorationCell cell, out Mapping mapping)
        {
            mapping = null;
            if (cell.tint != Color.white && string.IsNullOrEmpty(cell.visualFamily)) return false;
            var source = SourceMapping(map, cell);
            if (source == null || (source.family == null && source.groupKey == null)) return false;
            mapping = source;
            return true;
        }

        public static bool Occupies(PuzzleMap map, DecorationCell cell, Vector2Int position)
        {
            var mapping = SourceMapping(map, cell);
            if (mapping == null) return cell.position == position;
            return position.x >= cell.position.x && position.x < cell.position.x + mapping.width &&
                position.y >= cell.position.y && position.y < cell.position.y + mapping.height;
        }

        public static bool AllowClaim(PuzzleMap map, IList<DecorationCell> decorations, RanchDecorClaim claim)
        {
            var p = new Vector2Int(claim.cell.x, claim.cell.y);
            if (!map.Contains(p)) return false;
            // The old decoration brush allowed every in-bounds cell. Only the
            // exposed Stone/Sand edges owned by V12 bridge_decor are reserved here.
            if (map.TryTerrain(p, out var terrain) &&
                (terrain.material == TerrainMaterial.Stone || terrain.material == TerrainMaterial.Sand))
                foreach (var direction in PuzzleAutotile.Cardinal)
                    if (!map.TryTerrain(p + direction, out _)) return false;
            foreach (var cell in decorations)
                if (!TryMap(map, cell, out _) && Occupies(map, cell, p)) return false;
            return true;
        }

        public static bool TryCreatePlan(PuzzleMap map, RanchConnectedDecorV01Catalog catalog, string pin,
            IList<DecorationCell> decorations, out RanchDecorPlan plan, out List<RanchDecorNetworkCell> networks,
            out List<RanchDecorGroupPlacement> groups, out string error)
        {
            plan = null; networks = new List<RanchDecorNetworkCell>(); groups = new List<RanchDecorGroupPlacement>(); error = null;
            if (!map || !map.theme || !map.theme.starwayRanch || !catalog)
            { error = "Missing Starway Ranch map or latest connected decoration catalog."; return false; }
            for (var index = 0; index < decorations.Count; index++)
            {
                var cell = decorations[index];
                if (!map.Contains(cell.position)) continue; // Cropped records stay serialized, as before.
                if (!string.IsNullOrEmpty(cell.visualFamily) && SourceMapping(map, cell) == null)
                { error = "Unknown explicit ranch decoration visual family: " + cell.visualFamily; return false; }
                if (!TryMap(map, cell, out var mapping)) continue;
                if (mapping.IsGroup)
                {
                    var id = string.IsNullOrEmpty(cell.placementId) ? "legacy:" + index : cell.placementId;
                    groups.Add(new RanchDecorGroupPlacement(id, mapping.groupKey, cell.position.x, cell.position.y,
                        cell.logicalLayer, cell.heightKey, 15));
                }
                else
                {
                    var connection = string.IsNullOrEmpty(cell.connectionGroup) ? mapping.family : cell.connectionGroup;
                    for (var y = 0; y < mapping.height; y++) for (var x = 0; x < mapping.width; x++)
                        networks.Add(new RanchDecorNetworkCell(cell.position.x + x, cell.position.y + y,
                            mapping.family, connection, cell.logicalLayer, cell.heightKey, 15));
                }
            }
            try
            {
                plan = RanchConnectedDecorV01Planner.CreatePlan(networks, groups, key =>
                {
                    if (!catalog.TryGetGroup(key, pin, out var group, out _, out var diagnostic))
                        throw new InvalidOperationException(diagnostic);
                    return new RanchDecorGroupSize(group.WidthCells, group.HeightCells);
                }, claim => AllowClaim(map, decorations, claim));
                return true;
            }
            catch (Exception exception)
            { error = exception.Message; plan = null; return false; }
        }
    }
}
