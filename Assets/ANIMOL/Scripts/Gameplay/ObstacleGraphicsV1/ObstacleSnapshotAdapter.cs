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
        public static string Kind(StageMapObjectKind kind) => kind switch {
            StageMapObjectKind.DropPlatform => "C01",
            StageMapObjectKind.MoonLanternStep => "C05",
            _ => null
        };
        public static string Availability(StageMapObjectTypeDefinition type)
        {
            if (Kind(type.Kind) != null) return "V6 접합";
            if (type.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype) return "기능 미구현 · 기존 프리뷰";
            return "기존 동작·그래픽";
        }
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
            if (placement.Settings.FootprintCells != Vector2.one) throw new InvalidOperationException("V6 obstacle graphics require the verified 1-cell footprint: " + placement.StableId);
            bool surface = true; string pose = "idle";
            if (owner != null)
            {
                if (kind == "C01" && !(owner is DropPlatformObject) || kind == "C05" && !(owner is MoonLanternStepObject))
                    throw new InvalidOperationException("Obstacle runtime owner does not match graphics adapter.");
                var collider = owner.GetComponent<BoxCollider2D>();
                if (collider == null) throw new InvalidOperationException("Missing authoritative obstacle surface.");
                surface = collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger;
                if (owner is MoonLanternStepObject lantern)
                    pose = lantern.State == LanternStepState.Warning ? "warn" : surface ? "idle" : "inactive";
            }
            return new GraphicCell { Kind=kind, ThemeId=map.ThemeId, StyleId=Style(map,placement), ArtVersion=map.FreeShapeTerrain.artVersion,
                Facing="UP", Pose=pose, SurfaceEnabled=surface };
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
