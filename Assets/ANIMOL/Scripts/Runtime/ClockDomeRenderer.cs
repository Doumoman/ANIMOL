using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Puzzle
{
    // Structural occupancy comes solely from terrain. Separate decorative deck has no gameplay cells.
    public sealed class ClockDomeRenderer : MonoBehaviour
    {
        public PuzzleMap map;
        public ClockDomeArt art;
        public bool architectureEnabled = true;
        public Tilemap Floor { get; private set; }
        public Tilemap Underside { get; private set; }
        public Tilemap Deck { get; private set; }
        public Tilemap DeckUnderside { get; private set; }
        public Grid Grid { get; private set; }
        public int RefreshedCells { get; private set; }
        public ClockDomeRoom Room { get; private set; }
        public SpriteRenderer Telescope { get; private set; }
        readonly HashSet<Vector2Int> deck = new HashSet<Vector2Int>();
        RectInt footprint;
        public bool Structural(Vector2Int p) => map.TryTerrain(p, out _);
        public void Bind(PuzzleMap data, ClockDomeArt source)
        {
            Unbind(); map = data; art = source;
            if (!Floor) foreach (var layer in GetComponentsInChildren<Tilemap>())
            { if (layer.name == "Dome Floor") Floor = layer; if (layer.name == "Dome Underside") Underside = layer; if (layer.name == "Decorative Deck") Deck = layer; if (layer.name == "Deck Underside") DeckUnderside = layer; }
            if (!Floor)
            {
                Grid = new GameObject("Clock floor grid", typeof(Grid)).GetComponent<Grid>(); Grid.transform.SetParent(transform, false);
                Grid.transform.localPosition = new Vector3(-.5f, -.5f, 0); Grid.cellSize = Vector3.one;
                Underside = Layer("Dome Underside", -20, -.15f); DeckUnderside = Layer("Deck Underside", -20, -.15f);
                Floor = Layer("Dome Floor", 0, 0); Deck = Layer("Decorative Deck", 0, 0);
            }
            else Grid = Floor.layoutGrid as Grid;
            map.TerrainChanged += Changed; map.Resized += Resized; Populate(); UpdateArchitecture();
        }
        Tilemap Layer(string name, int order, float y)
        {
            var root = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); root.transform.SetParent(Grid.transform, false);
            root.transform.localPosition = new Vector3(0, y, 0); var tilemap = root.GetComponent<Tilemap>(); tilemap.tileAnchor = new Vector3(.5f, .5f, 0);
            var renderer = root.GetComponent<TilemapRenderer>(); renderer.sortingOrder = order; renderer.mode = TilemapRenderer.Mode.Chunk; return tilemap;
        }
        public void Populate()
        { Floor.ClearAllTiles(); Underside.ClearAllTiles(); foreach (var c in map.terrain) if (map.Contains(c.position)) RefreshCell(c.position); }
        public void RefreshCell(Vector2Int p)
        {
            RefreshedCells++; var cell = new Vector3Int(p.x, p.y, 0); var pair = Structural(p) ? art.Find(TheaterTopology.Resolve(Structural, p)) : null;
            Floor.SetTile(cell, pair?.floor); Underside.SetTile(cell, pair?.underside);
        }
        public void RefreshNeighborhood(Vector2Int p)
        { for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++) RefreshCell(p + new Vector2Int(x, y)); }
        public static RectInt Footprint(PuzzleMap data)
        {
            var cells = data.terrain.Where(c => data.Contains(c.position)).Select(c => c.position).ToArray();
            if (cells.Length == 0) return new RectInt(0, 0, data.Width, data.Height);
            return new RectInt(cells.Min(p => p.x), cells.Min(p => p.y), cells.Max(p => p.x) - cells.Min(p => p.x) + 1, cells.Max(p => p.y) - cells.Min(p => p.y) + 1);
        }
        void UpdateArchitecture()
        {
            footprint = Footprint(map); deck.Clear(); Deck.ClearAllTiles(); DeckUnderside.ClearAllTiles();
            if (!Room) Room = GetComponentInChildren<ClockDomeRoom>();
            if (!Telescope) Telescope = GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Telescope on nonwalkable deck");
            if (!architectureEnabled) { if (Room) Room.gameObject.SetActive(false); if (Telescope) Telescope.gameObject.SetActive(false); return; }
            if (!Room) Room = Instantiate(art.roomPrefab, transform).GetComponent<ClockDomeRoom>();
            Room.gameObject.SetActive(true); Room.art = art; var rows = footprint.height + 1;
            if (Room.columnRows != rows) Room.Assemble(rows);
            var center = (footprint.xMin + footprint.xMax - 1) * .5f;
            Room.transform.localPosition = new Vector3(center - 6, footprint.yMax + 5.5f, 0);
            if (!Telescope) { var root = Instantiate(art.telescopePrefab, transform); root.name = "Telescope on nonwalkable deck"; Telescope = root.GetComponent<SpriteRenderer>(); }
            Telescope.gameObject.SetActive(true);
            // Four visual cells outside the occupied footprint; never written into PuzzleMap.
            var left = footprint.xMin + Mathf.Max(0, (footprint.width - 2) / 2);
            for (var x = left; x < left + 2; x++) for (var y = footprint.yMax + 1; y <= footprint.yMax + 2; y++) deck.Add(new Vector2Int(x, y));
            foreach (var p in deck)
            {
                var pair = art.Find(TheaterTopology.Resolve(deck.Contains, p)); var cell = new Vector3Int(p.x, p.y, 0);
                Deck.SetTile(cell, pair.floor); DeckUnderside.SetTile(cell, pair.underside);
            }
            Telescope.transform.localPosition = new Vector3(left + .5f, footprint.yMax + .6f, 0);
        }
        void Changed(Vector2Int p) { RefreshNeighborhood(p); if (Footprint(map) != footprint) UpdateArchitecture(); }
        void Resized() { Populate(); UpdateArchitecture(); }
        void Unbind() { if (map) { map.TerrainChanged -= Changed; map.Resized -= Resized; } }
        void OnDestroy() => Unbind();
    }
}
