using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Puzzle
{
    // Two globally ordered visual layers. Neither Tilemap participates in occupancy/physics.
    public sealed class SkyIslandMapRenderer : MonoBehaviour
    {
        public PuzzleMap map;
        public SkyIslandArt art;
        public Tilemap Grass { get; private set; }
        public Tilemap Underside { get; private set; }
        public void Bind(PuzzleMap data, SkyIslandArt source)
        {
            map = data; art = source;
            if (!Grass) foreach (var layer in GetComponentsInChildren<Tilemap>())
            { if (layer.name == "Grass") Grass = layer; if (layer.name.StartsWith("Underside")) Underside = layer; }
            if (!Grass)
            {
                var grid = new GameObject("Island Grid", typeof(Grid)); grid.transform.SetParent(transform, false);
                grid.transform.localPosition = new Vector3(-.5f, -.5f, 0); grid.GetComponent<Grid>().cellSize = Vector3.one;
                Underside = Layer(grid.transform, "Underside (visual only)", -20, SkyIslandArt.UndersideY);
                Grass = Layer(grid.transform, "Grass", 0, 0);
            }
            Clear();
        }
        Tilemap Layer(Transform parent, string name, int order, float offset)
        {
            var root = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0, offset, 0);
            var tiles = root.GetComponent<Tilemap>(); tiles.tileAnchor = new Vector3(.5f, .5f, 0);
            var renderer = root.GetComponent<TilemapRenderer>(); renderer.mode = TilemapRenderer.Mode.Chunk; renderer.sortingOrder = order;
            return tiles;
        }
        public void Clear() { if (Grass) Grass.ClearAllTiles(); if (Underside) Underside.ClearAllTiles(); }
        public void ClearCell(Vector2Int p)
        { var cell = new Vector3Int(p.x, p.y, 0); Grass.SetTile(cell, null); Underside.SetTile(cell, null); }
        public void RefreshCell(Vector2Int p)
        {
            if (!SkyIslandAutotile.UsesArt(map, p)) { ClearCell(p); return; }
            var shape = SkyIslandAutotile.Resolve(map, p); var pair = art.Find(shape.exposure, shape.innerCorners);
            if (pair == null) throw new System.InvalidOperationException("Missing island blob o" + shape.exposure + "_i" + shape.innerCorners);
            var cell = new Vector3Int(p.x, p.y, 0); Underside.SetTile(cell, pair.undersideTile); Grass.SetTile(cell, pair.grassTile);
        }
        public void PopulateAll()
        { Clear(); foreach (var cell in map.terrain) if (map.Contains(cell.position)) RefreshCell(cell.position); }
    }
}
