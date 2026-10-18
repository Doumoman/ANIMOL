using System.Collections.Generic;
using ANIMOL.Art.V10;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Visual-only foreground; all topology is computed from the full map.
    public sealed class StarwayRanchMapRenderer : MonoBehaviour
    {
        struct Placement
        {
            public string file;
            public Vector2 position;
            public int order;
        }

        public PuzzleMap map;
        public StarwayRanchArt art;
        readonly List<Placement> placements = new List<Placement>();
        readonly Dictionary<int, GameObject> visible = new Dictionary<int, GameObject>();
        RectInt lastViewport;

        public void Bind(PuzzleMap source, StarwayRanchArt catalog)
        { map = source; art = catalog; Replan(); }

        public void Replan()
        {
            foreach (var go in visible.Values) Remove(go);
            visible.Clear(); placements.Clear(); lastViewport = default;
            if (!map || !art) return;
            var rows = new string[map.Height];
            for (var y = 0; y < map.Height; y++)
            {
                var characters = new char[map.Width];
                for (var x = 0; x < map.Width; x++)
                {
                    if (map.TryTerrain(new Vector2Int(x, map.Height - 1 - y), out var cell) && !cell.blocked)
                        characters[x] = cell.material == TerrainMaterial.Grass ? 'G' :
                            cell.material == TerrainMaterial.Stone ? 'S' : 'W';
                    else characters[x] = '0';
                }
                rows[y] = new string(characters);
            }
            foreach (var body in StarwayRanchV10MaterialSelector.GetBottomFinishes(rows))
                Add(body.File, new Vector2(body.CenterCellX, map.Height - 1 - body.CenterCellY), body.IsCollar ? -10 : -20);
            for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
            {
                var file = StarwayRanchV10MaterialSelector.GetTopSpritePath(rows, x, y);
                if (file == null) continue;
                var world = new Vector2(x, map.Height - 1 - y);
                Add(file, world, 0);
                // Sparse, deterministic grass detail. No map or gameplay RNG is consumed.
                if (rows[y][x] == 'G' && (x * 17 + y * 31) % 19 == 0)
                    Add("decor/turf/tuft_0" + (1 + (x * 7 + y) % 6) + ".png", world + new Vector2(.24f, -.27f), 8);
            }
            foreach (var piece in StarwayRanchV10BridgeDecorSelector.Select(rows))
                Add(piece.File, new Vector2(-.5f + piece.XPixel / 256f,
                    map.Height - .5f - piece.YPixel / 256f), piece.Layer);
        }

        void Add(string file, Vector2 position, int order)
        { placements.Add(new Placement { file = file, position = position, order = order }); }

        public void Show(RectInt viewport)
        {
            if (viewport == lastViewport) return;
            lastViewport = viewport;
            var bounds = new Rect(viewport.xMin - 5, viewport.yMin - 5, viewport.width + 10, viewport.height + 10);
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var inside = bounds.Contains(placement.position);
                if (!inside && visible.TryGetValue(i, out var old)) { Remove(old); visible.Remove(i); }
                else if (inside && !visible.ContainsKey(i))
                {
                    var sprite = art.Find(placement.file);
                    if (!sprite) continue;
                    var go = new GameObject(placement.file);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = placement.position;
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = placement.order;
                    visible.Add(i, go);
                }
            }
        }

        static void Remove(GameObject go)
        { go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
    }
}
