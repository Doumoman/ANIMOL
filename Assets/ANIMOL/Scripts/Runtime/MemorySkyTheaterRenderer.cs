using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Puzzle
{
    public sealed class MemorySkyTheaterRenderer : MonoBehaviour
    {
        public PuzzleMap map;
        public MemorySkyTheaterArt art;
        public MemorySkyTheaterLayout layout;
        public Tilemap Wood { get; private set; }
        public Tilemap Underside { get; private set; }
        public Tilemap Velvet { get; private set; }
        public Grid Grid { get; private set; }
        public int RefreshedCells { get; private set; }
        readonly HashSet<Vector2Int> deck = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> carpet = new HashSet<Vector2Int>();
        Transform propsRoot;
        RectInt boundFootprint;
        bool initialized;
        public bool Structural(Vector2Int p) => map.TryTerrain(p, out _) || deck.Contains(p);
        public bool Carpet(Vector2Int p) => carpet.Contains(p) && Structural(p);
        public void Bind(PuzzleMap data, MemorySkyTheaterArt source, MemorySkyTheaterLayout appearance = null)
        {
            if (!propsRoot) propsRoot = transform.Find("Theater border props (visual only)");
            var chosen = appearance ? appearance : source.defaultLayout;
            var preserveProps = propsRoot && map == data && art == source && layout == chosen;
            var footprint = Footprint(data);
            var rebuildBorder = preserveProps && chosen && chosen.fitBorderToMap && footprint != boundFootprint;
            var preserveCarpet = initialized && map == data && art == source && layout == chosen;
            Unbind(); map = data; art = source; layout = chosen;
            deck.Clear(); if (!preserveCarpet) carpet.Clear();
            if (layout) { deck.UnionWith(layout.visualDeck); if (!preserveCarpet) carpet.UnionWith(layout.velvet); }
            var borderWidth = footprint.width < 3 ? footprint.width + 2 : footprint.width;
            var left = footprint.xMin + (footprint.width - borderWidth) / 2;
            if (layout && layout.fitBorderToMap)
            {
                deck.Clear();
                for (var x = left; x < left + borderWidth; x++)
                    for (var y = footprint.yMin - 3; y <= footprint.yMax + 2; y++) if (y < footprint.yMin || y >= footprint.yMax) deck.Add(new Vector2Int(x, y));
            }
            if (!Wood) foreach (var layer in GetComponentsInChildren<Tilemap>())
            { if (layer.name == "Wood") Wood = layer; if (layer.name == "Velvet") Velvet = layer; if (layer.name == "Theater Underside") Underside = layer; }
            if (!Wood)
            {
                Grid = new GameObject("Theater Grid", typeof(Grid)).GetComponent<Grid>(); Grid.transform.SetParent(transform, false);
                Grid.transform.localPosition = new Vector3(-.5f, -.5f, 0); Grid.cellSize = Vector3.one;
                Underside = Layer("Theater Underside", -20, -.175f); Wood = Layer("Wood", 0, 0); Velvet = Layer("Velvet", 2, 0);
            }
            else Grid = Wood.layoutGrid as Grid;
            map.TerrainChanged += Changed; map.Resized += Resized;
            Populate();
            boundFootprint = footprint; initialized = true;
            if (preserveProps && !rebuildBorder) return;
            // Resizing a visual border preserves elapsed animation; it is not another entrance.
            var clocks = propsRoot ? propsRoot.GetComponentsInChildren<MemorySkyTheaterAnimation>().GroupBy(a => a.sequence).ToDictionary(g => g.Key, g => g.First().Elapsed) : new Dictionary<ANIMOL.Presentation.SpriteFrameSequence, float>();
            if (propsRoot) { propsRoot.gameObject.SetActive(false); if (Application.isPlaying) Destroy(propsRoot.gameObject); else DestroyImmediate(propsRoot.gameObject); }
            propsRoot = new GameObject("Theater border props (visual only)").transform; propsRoot.SetParent(transform, false);
            var placements = layout ? layout.props : new List<TheaterPlacement>();
            if (layout && layout.fitBorderToMap)
            {
                var frame = placements.FirstOrDefault(p => p.prefab && p.prefab.GetComponent<MemorySkyTheaterFrame>());
                var organ = placements.FirstOrDefault(p => p.prefab && p.prefab.name == "organ_160x160");
                var seats = placements.FirstOrDefault(p => p.prefab && p.prefab.name == "seat_pair_160x80");
                var light = placements.FirstOrDefault(p => p.prefab && p.prefab.name == "footlights_80x80");
                placements = new List<TheaterPlacement>();
                if (frame != null) placements.Add(new TheaterPlacement { prefab = frame.prefab, position = new Vector2(left - .5f, footprint.yMax + 2.5f) });
                if (organ != null && borderWidth >= 4) placements.Add(new TheaterPlacement { prefab = organ.prefab, position = new Vector2((footprint.xMin + footprint.xMax - 1) * .5f, footprint.yMax + .5f) });
                if (seats != null)
                {
                    placements.Add(new TheaterPlacement { prefab = seats.prefab, position = new Vector2(borderWidth >= 4 ? left + .5f : (footprint.xMin + footprint.xMax - 1) * .5f, footprint.yMin - 2.45f) });
                    if (borderWidth >= 4) placements.Add(new TheaterPlacement { prefab = seats.prefab, position = new Vector2(left + borderWidth - 1.5f, footprint.yMin - 2.45f) });
                }
                if (light != null) for (var x = left; x < left + borderWidth; x++) placements.Add(new TheaterPlacement { prefab = light.prefab, position = new Vector2(x, footprint.yMin - 1.25f) });
            }
            foreach (var prop in placements)
            {
                if (!prop.prefab) continue;
                var instance = Instantiate(prop.prefab, propsRoot); instance.name = prop.prefab.name;
                instance.transform.localPosition = prop.position; instance.transform.localScale = Vector3.one;
                var frame = instance.GetComponent<MemorySkyTheaterFrame>(); if (frame && layout.fitBorderToMap) frame.SetWidth(borderWidth);
                foreach (var animation in instance.GetComponentsInChildren<MemorySkyTheaterAnimation>()) if (clocks.TryGetValue(animation.sequence, out var elapsed)) animation.Advance(elapsed);
            }
        }
        Tilemap Layer(string name, int order, float y)
        {
            var root = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); root.transform.SetParent(Grid.transform, false);
            root.transform.localPosition = new Vector3(0, y, 0); var tilemap = root.GetComponent<Tilemap>(); tilemap.tileAnchor = new Vector3(.5f, .5f, 0);
            var renderer = root.GetComponent<TilemapRenderer>(); renderer.sortingOrder = order; renderer.mode = TilemapRenderer.Mode.Chunk; return tilemap;
        }
        public void Populate()
        {
            Underside.ClearAllTiles(); Wood.ClearAllTiles(); Velvet.ClearAllTiles();
            foreach (var cell in map.terrain) if (map.Contains(cell.position)) RefreshCell(cell.position);
            foreach (var p in deck) if (!map.TryTerrain(p, out _)) RefreshCell(p);
        }
        public void RefreshCell(Vector2Int p)
        {
            RefreshedCells++; var cell = new Vector3Int(p.x, p.y, 0);
            var pair = Structural(p) ? art.Find(TheaterTopology.Resolve(Structural, p)) : null;
            Wood.SetTile(cell, pair?.wood); Underside.SetTile(cell, pair?.underside);
            Velvet.SetTile(cell, Carpet(p) ? art.Find(TheaterTopology.Resolve(Carpet, p)).velvet : null);
        }
        public void RefreshNeighborhood(Vector2Int p)
        { for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++) RefreshCell(p + new Vector2Int(x, y)); }
        public void SetCarpet(Vector2Int p, bool present)
        { if (present) carpet.Add(p); else carpet.Remove(p); RefreshNeighborhood(p); }
        static RectInt Footprint(PuzzleMap data)
        {
            var cells = data.terrain.Where(c => data.Contains(c.position)).Select(c => c.position).ToArray();
            if (cells.Length == 0) return new RectInt(0, 0, data.Width, data.Height);
            var min = new Vector2Int(cells.Min(p => p.x), cells.Min(p => p.y)); var max = new Vector2Int(cells.Max(p => p.x) + 1, cells.Max(p => p.y) + 1);
            return new RectInt(min, max - min);
        }
        void Changed(Vector2Int p)
        { if (layout && layout.fitBorderToMap && Footprint(map) != boundFootprint) Bind(map, art, layout); else RefreshNeighborhood(p); }
        void Resized() => Bind(map, art, layout);
        void Unbind() { if (map) { map.TerrainChanged -= Changed; map.Resized -= Resized; } }
        void OnDestroy() => Unbind();
    }
}
