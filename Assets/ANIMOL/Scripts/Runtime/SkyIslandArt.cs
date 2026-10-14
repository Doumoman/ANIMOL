using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Puzzle
{
    [Serializable] public class SkyIslandTilePair
    {
        public int outer, inner;
        public Sprite grass, underside;
        public Tile grassTile, undersideTile;
    }
    [CreateAssetMenu(menuName = "ANIMOL/Starway Island Art")]
    public sealed class SkyIslandArt : ScriptableObject
    {
        public SkyIslandTilePair[] blob47;
        public Sprite far, mid, near;
        public Material compositorMaterial;
        public const int PixelsPerUnit = 80;
        public const int RepeatPixels = 960;
        public const float UndersideY = -14f / 80;
        Dictionary<int, SkyIslandTilePair> index;
        public SkyIslandTilePair Find(int outer, int inner)
        {
            if (index == null) { index = new Dictionary<int, SkyIslandTilePair>(); foreach (var pair in blob47) index.Add(pair.outer | pair.inner << 4, pair); }
            return index.TryGetValue(outer | inner << 4, out var result) ? result : null;
        }
        void OnValidate() { index = null; }
    }
    public static class SkyIslandAutotile
    {
        // The source uses exposed NESW and inset NW/NE/SE/SW, not connected masks.
        public static TerrainShape Resolve(PuzzleMap map, Vector2Int p)
        {
            bool Occupied(Vector2Int q) => map.TryTerrain(q, out _);
            if (!Occupied(p)) return default;
            var n = Occupied(p + Vector2Int.up); var e = Occupied(p + Vector2Int.right);
            var s = Occupied(p + Vector2Int.down); var w = Occupied(p + Vector2Int.left);
            var outer = (n ? 0 : 1) | (e ? 0 : 2) | (s ? 0 : 4) | (w ? 0 : 8);
            var inner = n && w && !Occupied(p + new Vector2Int(-1, 1)) ? 1 : 0;
            if (n && e && !Occupied(p + new Vector2Int(1, 1))) inner |= 2;
            if (s && e && !Occupied(p + new Vector2Int(1, -1))) inner |= 4;
            if (s && w && !Occupied(p + new Vector2Int(-1, -1))) inner |= 8;
            return new TerrainShape(outer, inner);
        }
        public static bool UsesArt(PuzzleMap map, Vector2Int p) => map.TryTerrain(p, out var cell) && cell.material == TerrainMaterial.Grass && !cell.blocked;
    }
}
