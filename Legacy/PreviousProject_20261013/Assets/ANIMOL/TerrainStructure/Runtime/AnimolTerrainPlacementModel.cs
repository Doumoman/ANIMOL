using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animol.TerrainStructure
{
    [Serializable]
    public sealed class AnimolTerrainCatalogData
    {
        public int version = 3;
        public float cellUnits = 1f;
        public AnimolTerrainCatalogEntry[] entries = new AnimolTerrainCatalogEntry[0];
    }

    [Serializable]
    public sealed class AnimolTerrainCatalogEntry
    {
        public string id;
        public string themeId;
        public string styleId;
        public string frameId;
        public string role;
        public string kind;
        public string maskHash;
        public int width;
        public int height;
        public int footprintCount, solidCount, voidCount, supportCount;
        // PNG/top row first. # owns a 1x1 solid cell; . owns no solid cell.
        public string[] solidRows = new string[0];
        // InteriorOverlay support only. Support is tested; it never creates cells.
        public string[] supportRows = new string[0];
    }

    [Serializable]
    public sealed class AnimolTerrainBaseCell
    {
        public int x;
        public int y;
        public string themeId;
        public string styleId;
    }

    [Serializable]
    public sealed class AnimolTerrainPlacement
    {
        public string instanceId;
        public string catalogId;
        public int catalogVersion;
        public string maskHash;
        public string themeId;
        public string styleId;
        public int x;
        public int y;
        public string orientation = "None";
    }

    /// <summary>
    /// Production integration persists this collection inside the existing
    /// StageMapDefinition. Its existing 1x1 terrain-cell fields remain authoritative.
    /// Do not persist a second copy of the complete cell map beside those fields.
    /// </summary>
    [Serializable]
    public sealed class AnimolTerrainPlacementCollection
    {
        public int schemaVersion = 3;
        public List<AnimolTerrainPlacement> placements = new List<AnimolTerrainPlacement>();
    }

    /// <summary>
    /// Transaction DTO: a host projects its ordinary, non-structure cells here and
    /// combines them with its persisted placement collection. Derived structure
    /// ownership/cell expansion is a cache, not another production storage format.
    /// The standalone example can serialize this complete DTO with JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class AnimolTerrainSavedMap
    {
        public FreeShapeLayer freeShape = new FreeShapeLayer();
        public int schemaVersion = 3;
        public int revision;
        public string themeId;
        public List<AnimolTerrainBaseCell> baseCells = new List<AnimolTerrainBaseCell>();
        public List<AnimolTerrainPlacement> placements = new List<AnimolTerrainPlacement>();
    }

    public sealed class AnimolTerrainCellResult
    {
        public Vector2Int cell;
        // Empty for ordinary/base cells. Otherwise the complete structure owner ID.
        public string ownerId;
        public string themeId;
        public string styleId;
    }

    /// <summary>Ephemeral expansion for collision, rendering and authoring checks.</summary>
    public sealed class AnimolTerrainResolveResult
    {
        public Dictionary<Vector2Int, AnimolTerrainCellResult> solids =
            new Dictionary<Vector2Int, AnimolTerrainCellResult>();
        public List<AnimolTerrainPlacement> placements = new List<AnimolTerrainPlacement>();
        public List<AnimolTerrainPlacement> overlays = new List<AnimolTerrainPlacement>();
        public Dictionary<string, AnimolTerrainCatalogEntry> entriesById =
            new Dictionary<string, AnimolTerrainCatalogEntry>(StringComparer.Ordinal);
    }
}
