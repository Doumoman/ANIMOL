using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Puzzle
{
    // Structural occupancy comes solely from terrain. Separate decorative deck has no gameplay cells.
    public sealed class TwilightCoastRenderer : MonoBehaviour
    {
        public PuzzleMap map;
        public TwilightCoastArt art;
        public bool environmentEnabled = true;
        public Tilemap Floor { get; private set; }
        public Tilemap Underside { get; private set; }
        public Tilemap Deck { get; private set; }
        public Tilemap DeckUnderside { get; private set; }
        public Grid Grid { get; private set; }
        public int RefreshedCells { get; private set; }
        public TwilightCoastHorizon Horizon { get; private set; }
        Transform propsRoot;
        readonly HashSet<Vector2Int> deck = new HashSet<Vector2Int>();
        RectInt footprint;
        public bool Structural(Vector2Int p) => map.TryTerrain(p, out _);
        public void Bind(PuzzleMap data, TwilightCoastArt source)
        {
            Unbind(); map = data; art = source;
            if (!Floor) foreach (var layer in GetComponentsInChildren<Tilemap>())
            { if (layer.name == "Coast Sand") Floor = layer; if (layer.name == "Wet Sand Body") Underside = layer; if (layer.name == "Decorative Deck") Deck = layer; if (layer.name == "Deck Underside") DeckUnderside = layer; }
            if (!Floor)
            {
                Grid = new GameObject("Coast floor grid", typeof(Grid)).GetComponent<Grid>(); Grid.transform.SetParent(transform, false);
                Grid.transform.localPosition = new Vector3(-.5f, -.5f, 0); Grid.cellSize = Vector3.one;
                Underside = Layer("Wet Sand Body", -20, -.1f); DeckUnderside = Layer("Deck Underside", -20, -.1f);
                Floor = Layer("Coast Sand", 0, 0); Deck = Layer("Decorative Deck", 0, 0);
            }
            else Grid = Floor.layoutGrid as Grid;
            map.TerrainChanged += Changed; map.Resized += Resized; Populate(); UpdateEnvironment();
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
            Floor.SetTile(cell, pair?.sand); Underside.SetTile(cell, pair?.wetSandBody);
        }
        public void RefreshNeighborhood(Vector2Int p)
        { for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++) RefreshCell(p + new Vector2Int(x, y)); }
        public static RectInt Footprint(PuzzleMap data)
        {
            var cells = data.terrain.Where(c => data.Contains(c.position)).Select(c => c.position).ToArray();
            if (cells.Length == 0) return new RectInt(0, 0, data.Width, data.Height);
            return new RectInt(cells.Min(p => p.x), cells.Min(p => p.y), cells.Max(p => p.x) - cells.Min(p => p.x) + 1, cells.Max(p => p.y) - cells.Min(p => p.y) + 1);
        }
        void UpdateEnvironment()
        {
            var next = Footprint(map);
            if (!Horizon) Horizon = GetComponentInChildren<TwilightCoastHorizon>(true);
            if (!propsRoot) propsRoot = transform.Find("Border decorations on visual sand");
            if (Horizon && propsRoot && next == footprint && environmentEnabled) return;
            footprint = next; deck.Clear(); Deck.ClearAllTiles(); DeckUnderside.ClearAllTiles();
            if (!environmentEnabled) { if (Horizon) Horizon.gameObject.SetActive(false); if (propsRoot) propsRoot.gameObject.SetActive(false); return; }
            if (!Horizon) Horizon = Instantiate(art.horizonPrefab, transform).GetComponent<TwilightCoastHorizon>();
            Horizon.gameObject.SetActive(true);
            var repeats = Mathf.CeilToInt(Mathf.Max(0, footprint.width - 12) * .5f) + 2;
            if (Horizon.leftRepeats != repeats || Horizon.rightRepeats != repeats) Horizon.Assemble(repeats,repeats);
            var center = (footprint.xMin + footprint.xMax - 1) * .5f;
            Horizon.transform.localPosition = new Vector3(center - 6, footprint.yMax + 3.5f, 0);
            // Visual sand strips outside the actual terrain footprint. Main tile mask stays map-only.
            var width = Mathf.Max(8, footprint.width); var left = footprint.xMin + (footprint.width - width) / 2;
            for (var x = left; x < left + width; x++)
            {
                deck.Add(new Vector2Int(x, footprint.yMin - 3)); deck.Add(new Vector2Int(x, footprint.yMin - 2));
                // Keep the single sun and reflection visible between the rear decorative islets.
                if (x < left + 2 || x >= left + width - 2)
                { deck.Add(new Vector2Int(x, footprint.yMax + 1)); deck.Add(new Vector2Int(x, footprint.yMax + 2)); }
            }
            foreach (var p in deck)
            {
                var pair = art.Find(TheaterTopology.Resolve(deck.Contains,p)); var cell = new Vector3Int(p.x,p.y,0);
                Deck.SetTile(cell,pair.sand); DeckUnderside.SetTile(cell,pair.wetSandBody);
            }
            var elapsed = propsRoot ? propsRoot.GetComponentsInChildren<TwilightCoastPalm>().Select(p=>p.Elapsed).DefaultIfEmpty().Max() : 0;
            if (propsRoot) { propsRoot.gameObject.SetActive(false); if (Application.isPlaying) Destroy(propsRoot.gameObject); else DestroyImmediate(propsRoot.gameObject); }
            propsRoot = new GameObject("Border decorations on visual sand").transform; propsRoot.SetParent(transform,false);
            foreach (var y in new[] { footprint.yMin - 3.4f, footprint.yMax + .6f })
            {
                Place(art.palmPrefab,new Vector2(left+.5f,y),elapsed);
                if (y < footprint.yMin) Place(art.fencePrefab,new Vector2(left+width*.5f-.5f,y),0);
                Place(art.rocksPrefab,new Vector2(left+width-1.5f,y),0);
            }
        }
        void Place(GameObject prefab, Vector2 p, float elapsed)
        {
            var root = Instantiate(prefab,propsRoot); root.name = prefab.name; root.transform.localPosition=p; root.transform.localScale=Vector3.one;
            var palm=root.GetComponent<TwilightCoastPalm>(); if (palm) palm.Advance(elapsed);
        }
        void Changed(Vector2Int p) { RefreshNeighborhood(p); if (Footprint(map) != footprint) UpdateEnvironment(); }
        void Resized() { Populate(); UpdateEnvironment(); }
        void Unbind() { if (map) { map.TerrainChanged -= Changed; map.Resized -= Resized; } }
        void OnDestroy() => Unbind();
    }
}
