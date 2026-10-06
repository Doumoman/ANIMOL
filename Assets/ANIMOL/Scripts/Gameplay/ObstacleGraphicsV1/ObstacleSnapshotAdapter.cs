using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using Animol.TerrainStructure;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    // Maps proven operational owners only. Similar names are not proof of equivalent behavior.
    public static class ObstacleSnapshotAdapter
    {
        public static string Kind(StageMapObjectKind kind) => CommonObstacleCatalog.Id(kind);
        public static string Availability(StageMapObjectTypeDefinition type) => Kind(type.Kind) != null ? "V6 ?? ???" : "??? ???";
        public static string Style(StageMapDefinition map, StageMapObjectPlacement placement)
        {
            var explicitStyle = placement.Settings.ObstacleStyleId;
            if (!string.IsNullOrEmpty(explicitStyle))
            {
                if (!explicitStyle.StartsWith(map.ThemeId+"_",StringComparison.Ordinal)) throw new InvalidOperationException("Obstacle style belongs to another theme.");
                FreeShapeArtRegistry.Load(map.FreeShapeTerrain).Style(explicitStyle);
                return explicitStyle;
            }
            var styles = (map.FreeShapeTerrain?.cells ?? new List<FreeShapeCell>()).Select(c=>c.styleId).Distinct().ToArray();
            if (styles.Length != 1) throw new InvalidOperationException("장애물 재료를 A/B/C/D 중 명시적으로 선택하세요: " + placement.StableId);
            return styles[0];
        }
        public static GraphicCell Read(StageMapDefinition map, StageMapObjectPlacement placement, StageMapRuntimeObject owner = null)
        {
            var kind = Kind(placement.Kind); if (kind == null) return null;
            CommonObstacleCatalog.ValidateSettings(placement);
            if (placement.Settings.FootprintCells != Vector2.one) throw new InvalidOperationException("V6 obstacle graphics require the verified 1-cell footprint: " + placement.StableId);
            bool surface = kind != "C09"; string pose = kind == "C09" ? "inactive" : "idle";
            if (owner != null)
            {
                if (!(owner is CommonObstacleObject device) || device.Kind != placement.Kind)
                    throw new InvalidOperationException("Obstacle runtime owner does not match the current contract.");
                surface = device.SurfaceEnabled; pose = device.Pose;
            }
            return new GraphicCell { Kind=kind, ThemeId=map.ThemeId, StyleId=Style(map,placement), ArtVersion=map.FreeShapeTerrain.artVersion,
                Facing=CommonObstacleCatalog.Facing(placement), Pose=pose, SurfaceEnabled=surface };
        }
        public static Dictionary<Vector2Int,GraphicCell> Preview(StageMapDefinition map, IEnumerable<StageMapObjectPlacement> placements = null)
        {
            var result = new Dictionary<Vector2Int,GraphicCell>();
            foreach (var p in placements ?? map.Objects)
            {
                var cell = Read(map,p); if (cell == null) continue;
                if (!result.TryAdd(new Vector2Int(p.X,p.Y),cell)) throw new InvalidOperationException("Duplicate graphic owner: " + p.StableId);
            }
            return result;
        }
        public static Dictionary<Vector2Int,GraphicCell> Merge(FreeShapeLayer layer, IReadOnlyDictionary<Vector2Int,GraphicCell> devices)
        {
            FreeShapeArtRegistry.Load(layer);
            var result = FreeShapeTopology.Index(layer).ToDictionary(e=>e.Key,e=>new GraphicCell {
                Kind="terrain",StyleId=e.Value.styleId,ThemeId=e.Value.styleId.Substring(0,3),ArtVersion=layer.artVersion });
            if (devices != null) foreach(var pair in devices)
                if (!result.TryAdd(pair.Key,pair.Value)) throw new InvalidOperationException("Terrain/device duplicate owner at " + pair.Key);
            return result;
        }
        public static void Validate(FreeShapeLayer layer, IReadOnlyDictionary<Vector2Int,GraphicCell> devices)
        {
            var cells=Merge(layer,devices);
            foreach(var pair in devices)
            {
                var plan=ObstacleVisualResolver.Resolve((x,y)=>cells.GetValueOrDefault(new Vector2Int(x,y)),pair.Key.x,pair.Key.y,layer.seed);
                ObstacleArtRegistry.Load().Sprite(plan.OverlayKey);
                if(plan.Problems.Count>0) throw new InvalidOperationException(pair.Key+": "+string.Join("; ",plan.Problems));
            }
        }
    }
}
