using System;
using System.Collections.Generic;
using System.Linq;
using Animol.TerrainStructure;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [Serializable]
    public sealed class StageTerrainStyleBinding
    {
        public string themeId, tileId, variantId, styleId;
        public bool preferred;
    }

    // References the canonical JSON and immutable source-frame assets; no duplicate map storage.
    public sealed class StageTerrainStructureRegistry : ScriptableObject
    {
        public TextAsset catalogJson;
        public AnimolTerrainArtFrameDefinition[] frames = Array.Empty<AnimolTerrainArtFrameDefinition>();
        public List<StageTerrainStyleBinding> bindings = new List<StageTerrainStyleBinding>();
        [NonSerialized] private AnimolTerrainCatalogData cachedCatalog;
        [NonSerialized] private string cachedJson;
        public AnimolTerrainCatalogData Catalog
        {
            get
            {
                if (catalogJson == null) throw new InvalidOperationException("Terrain catalog JSON is missing.");
                var json = catalogJson.text;
                if (cachedCatalog == null || cachedJson != json)
                { cachedCatalog = JsonUtility.FromJson<AnimolTerrainCatalogData>(json); cachedJson = json; }
                return cachedCatalog;
            }
        }
        private void OnEnable() { cachedCatalog = null; cachedJson = null; }
        private void OnValidate() { cachedCatalog = null; cachedJson = null; }
        public static StageTerrainStructureRegistry Load() => Resources.Load<StageTerrainStructureRegistry>("ANIMOL_TerrainStructureRegistry")
            ?? throw new InvalidOperationException("Initialize the ANIMOL terrain structure registry first.");
        public AnimolTerrainArtFrameDefinition Frame(string id) => frames.FirstOrDefault(f => f != null && f.name == id)
            ?? throw new InvalidOperationException("Missing source frame: " + id);
        public string Style(string theme, string tile, string variant)
        {
            var matches = bindings.Where(b => b.themeId == theme && b.tileId == tile && b.variantId == (variant ?? "")).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Unmapped/ambiguous terrain cell: {theme}/{tile}/{variant}. Register its exact tileId/variant; no fallback is permitted.");
            return matches[0].styleId;
        }
        public StageTerrainStyleBinding Preferred(string theme, string style) => bindings.Single(b => b.themeId == theme && b.styleId == style && b.preferred);
    }
}
