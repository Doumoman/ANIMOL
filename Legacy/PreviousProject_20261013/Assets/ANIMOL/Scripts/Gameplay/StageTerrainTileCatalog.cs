using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Gameplay
{
    [Serializable]
    public sealed class StageTerrainTileEntry
    {
        [SerializeField] private string tileId = string.Empty;
        [SerializeField] private string themeId = string.Empty;
        [SerializeField] private TileBase tile;
        public string TileId => tileId;
        public string ThemeId => themeId;
        public TileBase Tile => tile;
        public void EditorConfigure(string id, string theme, TileBase value) { tileId = id; themeId = theme; tile = value; }
    }

    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Terrain Tile Catalog", fileName = "ANIMOL_TerrainTileCatalog")]
    public sealed class StageTerrainTileCatalog : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private List<StageTerrainTileEntry> entries = new List<StageTerrainTileEntry>();
        public int Version => version;
        public IReadOnlyList<StageTerrainTileEntry> Entries => entries;
        public TileBase Find(string tileId) => entries.FirstOrDefault(item => item != null && string.Equals(item.TileId, tileId, StringComparison.Ordinal))?.Tile;
        public TileBase FindThemeDefault(string themeId)
        {
            var folder = themeId switch
            {
                "T01" => "moon", "T02" => "cloud", "T03" => "library", "T04" => "greenhouse", "T05" => "mine", _ => string.Empty
            };
            return string.IsNullOrEmpty(folder) ? null : Find($"{folder}_C");
        }
        public void EditorConfigure(IEnumerable<StageTerrainTileEntry> values)
        { version = 1; entries = values?.Where(item => item != null).ToList() ?? new List<StageTerrainTileEntry>(); }
    }
}
