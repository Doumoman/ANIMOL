using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Visual-only autotiling: this class never writes terrain or owns a collider.
    [DefaultExecutionOrder(200)]
    public sealed class PuzzleMapView : MonoBehaviour
    {
        public PuzzleMap map;
        readonly Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
        readonly Dictionary<Vector2Int, GameObject> bubbleViews = new Dictionary<Vector2Int, GameObject>();
        Transform objectsRoot;
        RectInt visible;
        SkyIslandMapRenderer island;
        MemorySkyTheaterRenderer theater;
        ClockDomeRenderer dome;
        TwilightCoastRenderer coast;
        public int RefreshedCells { get; private set; }
        public void Bind(PuzzleMap data)
        {
            Unbind(); map = data; map.TerrainChanged += RefreshNeighborhood; map.Resized += Rebuild;
            Rebuild();
        }
        void Unbind() { if (!map) return; map.TerrainChanged -= RefreshNeighborhood; map.Resized -= Rebuild; }
        void OnDestroy() => Unbind();
        static void Remove(GameObject go)
        { go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector2 position, int order, Vector2 size, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = order; r.color = color;
            if (sprite) { var uniform = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y); go.transform.localScale = new Vector3(uniform, uniform, 1); }
            return r;
        }
        public void Rebuild()
        {
            foreach (var go in tiles.Values) Remove(go); tiles.Clear();
            if (objectsRoot) Remove(objectsRoot.gameObject);
            bubbleViews.Clear(); objectsRoot = new GameObject("Devices and Decoration (no parallax)").transform; objectsRoot.SetParent(transform, false);
            if (map.theme && map.theme.twilightCoast)
            {
                if (!coast) coast = new GameObject("Twilight coast environment").AddComponent<TwilightCoastRenderer>();
                coast.transform.SetParent(transform, false); coast.Bind(map, map.theme.twilightCoast);
            }
            else if (coast) { Remove(coast.gameObject); coast = null; }
            if (map.theme && map.theme.clockDome)
            {
                if (!dome) dome = new GameObject("Clock dome environment").AddComponent<ClockDomeRenderer>();
                dome.transform.SetParent(transform, false); dome.Bind(map, map.theme.clockDome);
            }
            else if (dome) { Remove(dome.gameObject); dome = null; }
            if (map.theme && map.theme.memorySkyTheater)
            {
                if (!theater) theater = new GameObject("Memory theater environment").AddComponent<MemorySkyTheaterRenderer>();
                theater.transform.SetParent(transform, false); theater.Bind(map, map.theme.memorySkyTheater);
            }
            else if (theater) { Remove(theater.gameObject); theater = null; }
            if (map.theme && map.theme.skyIsland)
            {
                if (!island) island = gameObject.AddComponent<SkyIslandMapRenderer>();
                island.Bind(map, map.theme.skyIsland);
            }
            else if (island)
            {
                island.Clear(); Remove(island.Grass.transform.parent.gameObject);
                if (Application.isPlaying) Destroy(island); else DestroyImmediate(island);
                island = null;
            }
            visible = default; SyncViewport(true);
            foreach (var d in map.devices) if (map.Contains(d.position)) DrawDevice(d);
            foreach (var d in map.decorations) if (map.Contains(d.position)) Sprite(objectsRoot, "Decoration", d.sprite ? d.sprite : map.theme.decoration, d.position, 15, Vector2.one * .8f, d.tint);
            foreach (var p in map.bubbles) if (map.Contains(p) && !bubbleViews.ContainsKey(p))
                bubbleViews[p] = Sprite(objectsRoot, "Bubble " + p, map.theme.bubble, p, 25, Vector2.one * .65f, Color.white).gameObject;
            if (map.Contains(map.exit)) Sprite(objectsRoot, "Exit", map.theme.exit, map.exit, 20, Vector2.one * .9f, Color.white);
        }
        public void RefreshNeighborhood(Vector2Int p)
        { for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++) RefreshCell(p + new Vector2Int(x, y)); }
        public void RefreshCell(Vector2Int p)
        {
            RefreshedCells++;
            if (tiles.TryGetValue(p, out var old)) { Remove(old); tiles.Remove(p); }
            if ((dome || coast) && map.TryTerrain(p, out var domeCell))
            {
                if (domeCell.blocked && visible.Contains(p))
                {
                    var marker = new GameObject("Blocked terrain " + p); marker.transform.SetParent(transform, false); marker.transform.localPosition = (Vector2)p;
                    tiles[p] = marker; Label(marker.transform, "#", Vector2.zero, Color.white, 10);
                }
                return;
            }
            if (theater && map.TryTerrain(p, out var ground) && !ground.blocked) return;
            if (island)
            {
                if (visible.Contains(p)) island.RefreshCell(p); else island.ClearCell(p);
                if (SkyIslandAutotile.UsesArt(map, p)) return;
            }
            if (!visible.Contains(p) || !map.TryTerrain(p, out var cell) || !map.theme) return;
            var shape = PuzzleAutotile.Resolve(map, p); var art = map.theme.Art(cell.material);
            if (art == null || art.outer.Length != 16) return;
            var root = new GameObject("Terrain " + p + " mask " + shape.exposure); root.transform.SetParent(transform, false); root.transform.localPosition = (Vector2)p; tiles[p] = root;
            var tint = cell.blocked ? new Color(.42f, .45f, .55f) : Color.white;
            for (var i = 0; i < 4; i++)
            {
                var source = (shape.innerCorners & (1 << i)) != 0 && i < art.inner.Length ? art.inner[i] : art.outer[shape.exposure];
                Sprite(root.transform, "Quadrant " + i, PuzzleTerrainSprites.Quadrant(source, i), PuzzleTerrainSprites.Offset(i), 0, Vector2.one * .5f, tint);
            }
            if (cell.blocked) Label(root.transform, "#", Vector2.zero, Color.white, 10);
        }
        void DrawDevice(DeviceCell d)
        {
            string glyph = d.kind == DeviceKind.Portal ? "O" : d.kind == DeviceKind.Ice ? "~" : d.kind == DeviceKind.Brake ? "X" :
                d.direction == Vector2Int.up ? "^" : d.direction == Vector2Int.down ? "v" : d.direction == Vector2Int.left ? "<" : ">";
            Color color = d.kind == DeviceKind.Portal ? new Color(.85f, .48f, 1) : d.kind == DeviceKind.Wind ? Color.cyan : d.kind == DeviceKind.Ice ? new Color(.65f, .9f, 1) : Color.yellow;
            var root = new GameObject(d.kind + " " + d.position); root.transform.SetParent(objectsRoot, false); root.transform.localPosition = (Vector2)d.position;
            Label(root.transform, glyph, Vector2.zero, color, 12);
        }
        static void Label(Transform parent, string text, Vector2 p, Color color, int order)
        {
            var go = new GameObject("Glyph"); go.transform.SetParent(parent, false); go.transform.localPosition = p;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 48; label.characterSize = .12f; label.color = color; go.GetComponent<MeshRenderer>().sortingOrder = order;
        }
        public void Present(IPuzzleBoardState session)
        {
            foreach (var entry in bubbleViews) entry.Value.SetActive(!session.IsCollected(entry.Key));
        }
        void LateUpdate() { if (map) SyncViewport(false); }
        void SyncViewport(bool force)
        {
            var camera = Camera.main;
            var halfHeight = camera && camera.orthographic ? camera.orthographicSize : map.cameraSettings.visibleRows * .5f;
            var halfWidth = halfHeight * (camera ? camera.aspect : 9f / 16);
            var center = camera ? (Vector2)camera.transform.position : (Vector2)map.start;
            var min = new Vector2Int(Mathf.Max(0, Mathf.FloorToInt(center.x - halfWidth - 2)), Mathf.Max(0, Mathf.FloorToInt(center.y - halfHeight - 2)));
            var max = new Vector2Int(Mathf.Min(map.Width, Mathf.CeilToInt(center.x + halfWidth + 2)), Mathf.Min(map.Height, Mathf.CeilToInt(center.y + halfHeight + 2)));
            var next = new RectInt(min, Vector2Int.Max(Vector2Int.zero, max - min));
            if (!force && next == visible) return;
            var previous = visible; visible = next;
            if (island) for (var y = previous.yMin; y < previous.yMax; y++) for (var x = previous.xMin; x < previous.xMax; x++)
                if (!visible.Contains(new Vector2Int(x, y))) island.ClearCell(new Vector2Int(x, y));
            foreach (var p in tiles.Keys.Where(p => !visible.Contains(p)).ToArray()) { Remove(tiles[p]); tiles.Remove(p); }
            for (var y = min.y; y < max.y; y++) for (var x = min.x; x < max.x; x++)
            { var p = new Vector2Int(x, y); if (!tiles.ContainsKey(p) && (!island || force || !previous.Contains(p)) && map.TryTerrain(p, out _)) RefreshCell(p); }
        }
    }
}
