using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public enum StageMapPaletteCategory { Select, Terrain, Markers, LogicObjects }

    public enum TerrainTileTopology
    {
        Center, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight
    }

    public static class StageMapScenePalette
    {
        private const string RegistryPath = "Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset";
        public const string VariantSeparator = "@";

        public static readonly TerrainTileTopology[] NineSliceOrder =
        {
            TerrainTileTopology.TopLeft, TerrainTileTopology.Top, TerrainTileTopology.TopRight,
            TerrainTileTopology.Left, TerrainTileTopology.Center, TerrainTileTopology.Right,
            TerrainTileTopology.BottomLeft, TerrainTileTopology.Bottom, TerrainTileTopology.BottomRight
        };

        public static readonly StageMapObjectKind[] MarkerKinds =
        {
            StageMapObjectKind.PlayerStart, StageMapObjectKind.BubbleCandidate, StageMapObjectKind.Checkpoint,
            StageMapObjectKind.Exit, StageMapObjectKind.Hole, StageMapObjectKind.Spike
        };

        public static string ComposeVariantId(string designId, TerrainTileTopology topology)
        {
            var style = string.IsNullOrWhiteSpace(designId) ? "BASE" : designId.Trim().ToUpperInvariant();
            return $"{style}{VariantSeparator}{topology.ToString().ToUpperInvariant()}";
        }

        public static string TopologyGlyph(TerrainTileTopology topology) => topology switch
        {
            TerrainTileTopology.TopLeft => "↖", TerrainTileTopology.Top => "↑", TerrainTileTopology.TopRight => "↗",
            TerrainTileTopology.Left => "←", TerrainTileTopology.Center => "■", TerrainTileTopology.Right => "→",
            TerrainTileTopology.BottomLeft => "↙", TerrainTileTopology.Bottom => "↓", TerrainTileTopology.BottomRight => "↘",
            _ => "■"
        };

        public static string MarkerLabel(StageMapObjectKind kind) => kind switch
        {
            StageMapObjectKind.PlayerStart => "START",
            StageMapObjectKind.BubbleCandidate => "BUBBLE",
            StageMapObjectKind.Checkpoint => "CHECK",
            StageMapObjectKind.Exit => "EXIT",
            StageMapObjectKind.Hole => "HOLE",
            StageMapObjectKind.Spike => "SPIKE",
            _ => kind.ToString().ToUpperInvariant()
        };

        public static Color ObjectColor(StageMapObjectKind kind) => kind switch
        {
            StageMapObjectKind.PlayerStart => new Color(.2f, 1f, .35f, .9f),
            StageMapObjectKind.BubbleCandidate => new Color(.15f, .85f, 1f, .9f),
            StageMapObjectKind.Checkpoint => new Color(1f, .78f, .12f, .9f),
            StageMapObjectKind.Exit => new Color(.25f, .55f, 1f, .9f),
            StageMapObjectKind.Hole or StageMapObjectKind.Spike => new Color(1f, .22f, .2f, .9f),
            _ => new Color(1f, .2f, .82f, .9f)
        };

        public static StageMapObjectPlacement FindObjectAtCell(StageMapDefinition map, Vector2Int cell)
        {
            if (map == null) return null;
            return map.Objects.LastOrDefault(item => StageMapDefinition.EnumeratePlacementCells(item).Contains(cell))
                   ?? map.Objects.LastOrDefault(item => item.X == cell.x && item.Y == cell.y);
        }

        public static StageMapObjectTypeDefinition FindType(string stableTypeId)
        {
            if (string.IsNullOrWhiteSpace(stableTypeId)) return null;
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>(RegistryPath);
            return registry?.Find(stableTypeId);
        }

        public static Sprite ResolvePreviewSprite(StageMapObjectTypeDefinition type)
        {
            if (type == null) return null;
            if (type.Prefab != null)
            {
                var renderers = type.Prefab.GetComponentsInChildren<SpriteRenderer>(true);
                var operational = renderers.FirstOrDefault(item => item.sprite != null && HasAncestor(item.transform, "ANIMOL_32px_Art"));
                if (operational != null) return operational.sprite;
                var visible = renderers.FirstOrDefault(item => item.sprite != null && item.gameObject.activeInHierarchy);
                if (visible != null) return visible.sprite;
            }
            return type.GhostSprite != null ? type.GhostSprite : type.Icon;
        }

        public static Sprite ResolvePreviewSprite(StageMapObjectPlacement placement) =>
            placement == null ? null : ResolvePreviewSprite(FindType(placement.DataKey));

        public static Vector3 CalculateAspectFitScale(Sprite sprite, Vector2 footprint, float unitsPerCell)
        {
            if (sprite == null) return new Vector3(Mathf.Max(.2f, footprint.x) * unitsPerCell,
                Mathf.Max(.2f, footprint.y) * unitsPerCell, 1f);
            var bounds = sprite.bounds.size;
            var targetWidth = Mathf.Max(.2f, footprint.x) * unitsPerCell;
            var targetHeight = Mathf.Max(.2f, footprint.y) * unitsPerCell;
            var uniform = Mathf.Min(targetWidth / Mathf.Max(.001f, bounds.x), targetHeight / Mathf.Max(.001f, bounds.y));
            return new Vector3(uniform, uniform, 1f);
        }

        public static string BriefDescription(StageMapObjectTypeDefinition type, int maxLength = 180)
        {
            if (type == null) return string.Empty;
            var text = !string.IsNullOrWhiteSpace(type.EffectDescription) ? type.EffectDescription :
                !string.IsNullOrWhiteSpace(type.Description) ? type.Description :
                type.BehaviorComponentIds.Count > 0 ? "동작: " + string.Join(", ", type.BehaviorComponentIds) : "정적 맵 요소";
            text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            maxLength = Mathf.Max(8, maxLength);
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 1).TrimEnd() + "…";
        }

        private static bool HasAncestor(Transform transform, string name)
        {
            for (var current = transform; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }
    }
}
