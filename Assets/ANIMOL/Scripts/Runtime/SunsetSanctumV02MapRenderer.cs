using System;
using System.Collections.Generic;
using ANIMOL.Art.SunsetSanctumV02;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Visual-only board adapter. PuzzleMap remains the authority for walkable cells.
    public sealed class SunsetSanctumV02MapRenderer : MonoBehaviour
    {
        [Serializable] sealed class BackgroundLayout { public BackgroundLayer[] layers; }
        [Serializable] sealed class BackgroundLayer
        {
            public string id, file;
            public float[] positionWorld, localScale, driftAmplitude, driftPeriodSeconds, guardWorld, color;
            public int sortingOrder;
            public float phaseDegrees, twinkleAmount, twinklePeriodSeconds, parallaxMultiplier;
            public bool wrapToView;
        }

        PuzzleMap map;
        SunsetSanctumV02VisualCatalog catalog;
        readonly Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
        Transform undersideRoot, backgroundRoot;
        SunsetSanctumV02BackgroundRig backgroundRig;

        public void Bind(PuzzleMap board, SunsetSanctumV02VisualCatalog latest)
        {
            Unbind();
            map = board; catalog = latest;
            if (!map || !catalog) throw new ArgumentException("Sanctum V02 board and catalog are required.");
            Rebuild();
        }

        void OnDestroy() { Unbind(); }
        void Unbind()
        {
            foreach (var go in tiles.Values) Remove(go);
            tiles.Clear();
            if (undersideRoot) Remove(undersideRoot.gameObject);
            if (backgroundRoot) Remove(backgroundRoot.gameObject);
            undersideRoot = null; backgroundRoot = null; backgroundRig = null;
        }
        static void Remove(GameObject go)
        { if (!go) return; go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }

        Sprite Exact(string file)
        {
            SunsetSanctumV02VisualCatalog.Part part;
            string error;
            if (!catalog.TryGetPart(file, SunsetSanctumV02PuzzleBinding.ManifestSha256, out part, out error) ||
                part == null || part.Frame == null || !part.Frame.Sprite)
                throw new InvalidOperationException("Sanctum V02 sprite unavailable: " + file + " " + error);
            return part.Frame.Sprite;
        }

        static SunsetSanctumV02TerrainSelector.Material Material(TerrainMaterial source)
        {
            switch (source)
            {
                case TerrainMaterial.Grass: return SunsetSanctumV02TerrainSelector.Material.SanctumFloor;
                case TerrainMaterial.Stone: return SunsetSanctumV02TerrainSelector.Material.StoneBridge;
                case TerrainMaterial.Sand: return SunsetSanctumV02TerrainSelector.Material.WoodBridge;
                default: throw new ArgumentOutOfRangeException("source");
            }
        }
        char MaterialAt(int x, int y)
        {
            TerrainCell cell;
            if (!map.TryTerrain(new Vector2Int(x, y), out cell)) return '0';
            return cell.material == TerrainMaterial.Stone ? 'S' : cell.material == TerrainMaterial.Sand ? 'W' : 'G';
        }

        public void Rebuild()
        {
            foreach (var go in tiles.Values) Remove(go);
            tiles.Clear();
            foreach (var cell in map.terrain)
                if (map.Contains(cell.position)) RefreshCell(cell.position);
            RebuildUndersides();
        }

        public void RefreshNeighborhood(Vector2Int center)
        {
            for (int y = -1; y <= 1; ++y)
                for (int x = -1; x <= 1; ++x)
                    RefreshCell(center + new Vector2Int(x, y));
            RebuildUndersides();
        }

        void RefreshCell(Vector2Int p)
        {
            GameObject old;
            if (tiles.TryGetValue(p, out old)) { Remove(old); tiles.Remove(p); }
            TerrainCell cell;
            if (!map.TryTerrain(p, out cell)) return;
            int mask = SunsetSanctumV02TerrainSelector.NeighborMask(
                (x, y) => map.TryTerrain(new Vector2Int(x, y), out _), p.x, p.y);
            string file = SunsetSanctumV02TerrainSelector.File(Material(cell.material), mask);
            var go = new GameObject("V02 terrain " + p + " n" + mask.ToString("D3"));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(p.x, p.y, 0);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Exact(file);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;
            if (cell.blocked) renderer.color = new Color(.42f, .45f, .55f, 1);
            tiles.Add(p, go);
        }

        void RebuildUndersides()
        {
            if (undersideRoot) Remove(undersideRoot.gameObject);
            undersideRoot = new GameObject("V02 continuous undersides").transform;
            undersideRoot.SetParent(transform, false);
            var runs = SunsetSanctumV02UndersideSelector.SelectRuns(MaterialAt, 0, 0,
                map.Width - 1, map.Height - 1);
            foreach (var run in runs)
                foreach (var placement in SunsetSanctumV02UndersideSelector.Plan(run, 0))
                {
                    var go = new GameObject("V02 underside " + placement.file);
                    go.transform.SetParent(undersideRoot, false);
                    go.transform.localPosition = new Vector3(placement.logicalCenterX - .5f,
                        placement.logicalCenterY - .5f, .1f);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = Exact(placement.file);
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = -10;
                }
        }

        public void ConfigureBackground(Camera camera)
        {
            if (!camera || !camera.orthographic) throw new ArgumentException("Existing orthographic camera required.");
            if (!map.theme.sunsetSanctumV02BackgroundLayout)
                throw new InvalidOperationException("Sunset Sanctum V02 background layout is not bound.");
            if (backgroundRoot) Remove(backgroundRoot.gameObject);
            backgroundRoot = new GameObject("Sunset Sanctum V02 independent background").transform;
            backgroundRoot.SetParent(transform, false);
            var sky = new GameObject("V02 opaque sky base");
            sky.transform.SetParent(backgroundRoot, false);
            sky.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 9);
            var skyRenderer = sky.AddComponent<SpriteRenderer>();
            skyRenderer.sprite = Exact("backgrounds/sanctum/sky_base.png");
            skyRenderer.sortingLayerName = "Default";
            skyRenderer.sortingOrder = -1000;
            sky.AddComponent<SunsetSanctumV02CameraCover>().Configure(camera);

            var layout = JsonUtility.FromJson<BackgroundLayout>(map.theme.sunsetSanctumV02BackgroundLayout.text);
            if (layout == null || layout.layers == null || layout.layers.Length == 0)
                throw new InvalidOperationException("Invalid V02 background layer layout.");
            var layers = new SunsetSanctumV02BackgroundRig.Layer[layout.layers.Length];
            for (int i = 0; i < layout.layers.Length; ++i)
            {
                var row = layout.layers[i];
                var go = new GameObject(row.id);
                go.transform.SetParent(backgroundRoot, false);
                go.transform.position = new Vector3(camera.transform.position.x + row.positionWorld[0],
                    camera.transform.position.y + row.positionWorld[1], row.positionWorld[2]);
                go.transform.localScale = new Vector3(row.localScale[0], row.localScale[1], row.localScale[2]);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Exact(row.file);
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = row.sortingOrder;
                if (row.color != null && row.color.Length == 4)
                    renderer.color = new Color(row.color[0], row.color[1], row.color[2], row.color[3]);
                layers[i] = new SunsetSanctumV02BackgroundRig.Layer
                {
                    renderer = renderer, sortingOrder = row.sortingOrder,
                    parallaxMultiplier = row.parallaxMultiplier,
                    driftAmplitude = new Vector2(row.driftAmplitude[0], row.driftAmplitude[1]),
                    driftPeriodSeconds = new Vector2(row.driftPeriodSeconds[0], row.driftPeriodSeconds[1]),
                    phaseDegrees = row.phaseDegrees, twinkleAmount = row.twinkleAmount,
                    twinklePeriodSeconds = row.twinklePeriodSeconds, wrapToView = row.wrapToView,
                    guardWorld = new Vector2(row.guardWorld[0], row.guardWorld[1])
                };
            }
            backgroundRig = backgroundRoot.gameObject.AddComponent<SunsetSanctumV02BackgroundRig>();
            backgroundRig.Configure(camera, 0, "Default", layers);
            backgroundRig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }

        void Update()
        {
            if (backgroundRig && backgroundRig.ReducedMotion != StarwayRanchMotionSettings.ReducedMotion)
                backgroundRig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }
    }
}
