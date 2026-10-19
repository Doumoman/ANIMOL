using System.Collections.Generic;
using ANIMOL.Art.EchoTheaterV02;
using ANIMOL.Art.EchoTheaterV03;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Presentation of the existing PuzzleMap occupancy. This component creates no terrain or colliders.
    public sealed class EchoTheaterV02MapRenderer : MonoBehaviour
    {
        PuzzleMap map;
        EchoTheaterV02Art art;
        MemorySkyTheaterLayout layout;
        Transform artwork;
        readonly HashSet<Vector2Int> carpet = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> catwalk = new HashSet<Vector2Int>();

        public void Bind(PuzzleMap source, EchoTheaterV02Art graphics, MemorySkyTheaterLayout appearance)
        {
            Unbind();
            map = source; art = graphics; layout = appearance;
            carpet.Clear(); catwalk.Clear();
            if (layout)
            {
                carpet.UnionWith(layout.velvet);
                // Existing visual deck marks authored bridge art. It cannot add occupancy.
                catwalk.UnionWith(layout.visualDeck);
            }
            map.TerrainChanged += Changed;
            map.Resized += Rebuild;
            Rebuild();
        }

        public void SetCarpet(Vector2Int cell, bool present)
        {
            if (present) carpet.Add(cell); else carpet.Remove(cell);
            Rebuild();
        }

        void Changed(Vector2Int _) => Rebuild();

        void Rebuild()
        {
            if (!map || !art) return;
            if (artwork)
            {
                artwork.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(artwork.gameObject);
                else DestroyImmediate(artwork.gameObject);
            }
            artwork = new GameObject("Echo Theater V03 artwork").transform;
            artwork.SetParent(transform, false);
            var occupancy = new bool[map.Width, map.Height];
            for (var y = 0; y < map.Height; y++)
                for (var x = 0; x < map.Width; x++)
                    occupancy[x, y] = map.TryTerrain(new Vector2Int(x, map.Height - 1 - y), out _);
            var rows = EchoTheaterV03MaterialSelector.BuildVisualMaterialRows(occupancy,
                (x, y) => catwalk.Contains(new Vector2Int(x, map.Height - 1 - y)),
                (x, y) => carpet.Contains(new Vector2Int(x, map.Height - 1 - y)));

            // Full-map planning preserves diagonal neighbors, hole classification and span grouping.
            var finishes = EchoTheaterV03MaterialSelector.GetBottomFinishes(rows, 0, 0, 0);
            foreach (var finish in finishes)
                Add(finish.File, "Support " + finish.Kind,
                    new Vector3(finish.CenterCellX, map.Height - 1 - finish.CenterCellY, 0),
                    finish.IsCollar ? -10 : -20);
            for (var y = 0; y < map.Height; y++)
                for (var x = 0; x < map.Width; x++)
                {
                    var path = EchoTheaterV03MaterialSelector.GetTopSpritePath(rows, x, y);
                    if (path != null) Add(path, "Floor " + x + "," + y,
                        new Vector3(x, map.Height - 1 - y, 0), 0);
                }
            var origin = new Vector2(-.5f, map.Height - .5f);
            foreach (var rail in EchoTheaterV03BridgeDecorSelector.Select(rows))
                Add(rail.File, "Catwalk " + rail.Kind,
                    new Vector3(origin.x + rail.XPixel / 256f, origin.y - rail.YPixel / 256f, 0),
                    rail.Layer);
            PlaceExistingProps();
        }

        void PlaceExistingProps()
        {
            if (!layout) return;
            if (layout.fitBorderToMap)
            {
                var left = map.Width; var right = -1;
                var bottom = map.Height; var top = -1;
                foreach (var cell in map.terrain)
                {
                    if (!map.Contains(cell.position)) continue;
                    left = Mathf.Min(left, cell.position.x); right = Mathf.Max(right, cell.position.x);
                    bottom = Mathf.Min(bottom, cell.position.y); top = Mathf.Max(top, cell.position.y);
                }
                if (right < left) return;
                var width = right - left + 1;
                // The original fit-to-map layout keeps theatrical fixtures on the
                // decorative border outside playable cells. Preserve that placement.
                var frame = false; var organ = false; var seats = false; var lights = false;
                foreach (var placement in layout.props)
                {
                    if (placement?.prefab == null) continue;
                    var name = placement.prefab.name;
                    frame |= placement.prefab.GetComponent<MemorySkyTheaterFrame>() != null;
                    organ |= name.Contains("organ");
                    seats |= name.Contains("seat_pair");
                    lights |= name.Contains("footlights");
                }
                if (frame)
                {
                    Add("props/curtain_wing_left.png", "Left curtain", new Vector3(left, top + 1.5f, 0), 12);
                    Add("props/curtain_wing_right.png", "Right curtain", new Vector3(right, top + 1.5f, 0), 12);
                    for (var x = left + 1; x < right; x++)
                        Add("props/curtain_lintel_repeat.png", "Curtain lintel", new Vector3(x, top + 1.5f, 0), 11);
                }
                if (organ && width >= 4)
                    Add("props/organ.png", "Organ", new Vector3((left + right) * .5f, top + 1.5f, 0), 12);
                if (seats && width >= 4)
                {
                    Add("props/seat_pair.png", "Left seats", new Vector3(left + .5f, bottom - 2.45f, 0), 12);
                    Add("props/seat_pair.png", "Right seats", new Vector3(right - .5f, bottom - 2.45f, 0), 12);
                }
                if (lights)
                    for (var x = left; x < right; x += 2)
                        Add("props/footlights_on.png", "Static footlights", new Vector3(x + .5f, bottom - 1.25f, 0), 12);
                return;
            }
            foreach (var placement in layout.props)
            {
                if (placement?.prefab == null) continue;
                var name = placement.prefab.name;
                string path = name.Contains("organ") ? "props/organ.png" :
                    name.Contains("seat_pair") ? "props/seat_pair.png" :
                    name.Contains("footlights") ? "props/footlights_on.png" :
                    name.Contains("curtain_wing_left") ? "props/curtain_wing_left.png" :
                    name.Contains("curtain_wing_right") ? "props/curtain_wing_right.png" : null;
                if (path == null || Vector2.Distance(placement.position, map.exit) < 1.25f ||
                    Vector2.Distance(placement.position, map.start) < 1.25f) continue;
                Add(path, name, placement.position, 12);
            }
        }

        SpriteRenderer Add(string path, string label, Vector3 position, int order)
        {
            var sprite = art.Find(path);
            if (!sprite) { Debug.LogError("Missing Echo Theater V03 sprite: " + path, this); return null; }
            var go = new GameObject(label);
            go.transform.SetParent(artwork, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        void Unbind()
        {
            if (!map) return;
            map.TerrainChanged -= Changed;
            map.Resized -= Rebuild;
        }
        void OnDestroy() => Unbind();
    }
}
