using System;
using System.Collections.Generic;
using ANIMOL.Art.StarSwellCoastV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Visual-only adapter. PuzzleMap and its water list remain the sole occupancy source.
    public sealed class StarSwellCoastV01MapRenderer : MonoBehaviour
    {
        [Serializable] sealed class Layout { public LayerRow[] layers; }
        [Serializable] sealed class LayerRow
        {
            public string id, file;
            public float[] positionWorld, localScale, driftAmplitude, driftPeriodSeconds, guardWorld, color;
            public int sortingOrder;
            public float phaseDegrees, twinkleAmount, twinklePeriodSeconds, parallaxMultiplier;
            public bool wrapToView;
        }
        static float At(float[] values, int index, float fallback = 0f)
        { return values != null && values.Length > index ? values[index] : fallback; }

        PuzzleMap map;
        StarSwellCoastV01VisualCatalog catalog;
        readonly Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
        Transform undersideRoot, backgroundRoot, waterRoot;
        StarSwellCoastV01BackgroundRig rig;

        public void Bind(PuzzleMap board, StarSwellCoastV01VisualCatalog latest)
        {
            Clear(); map = board; catalog = latest;
            if (!map || !catalog) throw new ArgumentException("Coast V01 board and catalog are required.");
            Rebuild();
        }
        static void Remove(GameObject go)
        { if (!go) return; go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        void Clear()
        {
            foreach (var go in tiles.Values) Remove(go);
            tiles.Clear();
            if (undersideRoot) Remove(undersideRoot.gameObject);
            if (backgroundRoot) Remove(backgroundRoot.gameObject);
            if (waterRoot) Remove(waterRoot.gameObject);
            undersideRoot = null; backgroundRoot = null; waterRoot = null; rig = null;
        }
        void OnDestroy() { Clear(); }

        Sprite Exact(string file)
        {
            StarSwellCoastV01VisualCatalog.Part part; string error;
            if (!catalog.TryGetPart(file, StarSwellCoastV01PuzzleBinding.ManifestSha256, out part, out error) ||
                part == null || part.Frame == null || !part.Frame.Sprite)
                throw new InvalidOperationException("Coast V01 sprite unavailable: " + file + " " + error);
            return part.Frame.Sprite;
        }

        static StarSwellCoastV01TerrainSelector.Material Material(TerrainMaterial value)
        {
            switch (value)
            {
                case TerrainMaterial.Grass: return StarSwellCoastV01TerrainSelector.Material.CoastSand;
                case TerrainMaterial.Sand: return StarSwellCoastV01TerrainSelector.Material.WoodBridge;
                case TerrainMaterial.Stone: return StarSwellCoastV01TerrainSelector.Material.StoneBridge;
                default: throw new ArgumentOutOfRangeException("value");
            }
        }
        char MaterialAt(int x, int y)
        {
            TerrainCell cell;
            if (!map.TryTerrain(new Vector2Int(x, y), out cell)) return '0';
            return cell.material == TerrainMaterial.Sand ? 'W' : cell.material == TerrainMaterial.Stone ? 'S' : 'A';
        }

        public void Rebuild()
        {
            foreach (var go in tiles.Values) Remove(go);
            tiles.Clear();
            foreach (var cell in map.terrain) if (map.Contains(cell.position)) RefreshCell(cell.position);
            RebuildUndersides();
            RebuildWater();
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
            int mask = StarSwellCoastV01TerrainSelector.NeighborMask(
                (x, y) => map.TryTerrain(new Vector2Int(x, y), out _), p.x, p.y);
            string file = StarSwellCoastV01TerrainSelector.File(Material(cell.material), mask);
            var go = new GameObject("Coast V01 land " + p + " n" + mask.ToString("D3"));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(p.x, p.y, 0);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Exact(file);
            renderer.sortingLayerName = "Default"; renderer.sortingOrder = 0;
            if (cell.blocked) renderer.color = new Color(.42f, .45f, .55f, 1);
            tiles.Add(p, go);
        }
        void RebuildUndersides()
        {
            if (undersideRoot) Remove(undersideRoot.gameObject);
            undersideRoot = new GameObject("Coast V01 continuous undersides").transform;
            undersideRoot.SetParent(transform, false);
            var runs = StarSwellCoastV01UndersideSelector.SelectRuns(MaterialAt, 0, 0,
                map.Width - 1, map.Height - 1);
            foreach (var run in runs)
                foreach (var placement in StarSwellCoastV01UndersideSelector.Plan(run, 0))
                {
                    var go = new GameObject("Coast V01 underside " + placement.file);
                    go.transform.SetParent(undersideRoot, false);
                    go.transform.localPosition = new Vector3(placement.logicalCenterX - .5f,
                        placement.logicalCenterY - .5f, .1f);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = Exact(placement.file);
                    renderer.sortingLayerName = "Default"; renderer.sortingOrder = -10;
                }
        }

        void RebuildWater()
        {
            if (waterRoot) Remove(waterRoot.gameObject);
            waterRoot = new GameObject("Coast V01 authoritative water cells").transform;
            waterRoot.SetParent(transform, false);
            if (!map.scenario) return;
            var carrier = Exact("tiles/water/carrier.png");
            foreach (var p in map.scenario.water)
            {
                if (!map.Contains(p) || map.TryTerrain(p, out _)) continue;
                var node = new GameObject("Water " + p);
                node.transform.SetParent(waterRoot, false);
                node.transform.localPosition = (Vector2)p;
                var surface = node.AddComponent<SpriteRenderer>();
                surface.sprite = carrier; surface.sortingLayerName = "Default"; surface.sortingOrder = -500;
                var mask = node.AddComponent<SpriteMask>();
                mask.sprite = carrier; mask.isCustomRangeActive = true;
                mask.backSortingLayerID = SortingLayer.NameToID("Default");
                mask.frontSortingLayerID = SortingLayer.NameToID("Default");
                mask.backSortingOrder = -499; mask.frontSortingOrder = -479;
                var waveNode = new GameObject("Clipped ambient wave");
                waveNode.transform.SetParent(node.transform, false);
                var wave = waveNode.AddComponent<SpriteRenderer>();
                wave.sortingLayerName = "Default"; wave.sortingOrder = -490;
                wave.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                var motion = waveNode.AddComponent<StarSwellCoastV01AnimatedVisual>();
                if (!motion.Configure(catalog, wave, StarSwellCoastV01PuzzleBinding.ManifestSha256) ||
                    !motion.Play("water_surface.none.ambient_wave"))
                    throw new InvalidOperationException("Coast V01 water wave binding failed at " + p);
                Shore(node.transform, p, Vector2Int.up, "north");
                Shore(node.transform, p, Vector2Int.right, "east");
                Shore(node.transform, p, Vector2Int.down, "south");
                Shore(node.transform, p, Vector2Int.left, "west");
            }
        }
        void Shore(Transform parent, Vector2Int water, Vector2Int towardLand, string heading)
        {
            if (!map.TryTerrain(water + towardLand, out _)) return;
            var go = new GameObject("Shore toward land " + heading);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Exact("fx/water/shore/" + heading + ".png");
            renderer.sortingLayerName = "Default"; renderer.sortingOrder = -480;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void ConfigureBackground(Camera camera)
        {
            if (!camera || !camera.orthographic) throw new ArgumentException("Existing orthographic camera required.");
            if (!map.theme.starSwellCoastV01BackgroundLayout)
                throw new InvalidOperationException("Coast V01 background layout is not bound.");
            if (backgroundRoot) Remove(backgroundRoot.gameObject);
            backgroundRoot = new GameObject("Coast V01 background").transform;
            backgroundRoot.SetParent(transform, false);
            var layout = JsonUtility.FromJson<Layout>(map.theme.starSwellCoastV01BackgroundLayout.text);
            if (layout == null || layout.layers == null) throw new InvalidOperationException("Invalid Coast V01 layout.");
            float mapCenterX = (map.Width - 1) * .5f;
            float horizonY = map.Height + 3.5f;
            float dy = horizonY - 6.25f;

            var sky = new GameObject("Coast V01 opaque sky"); sky.transform.SetParent(backgroundRoot, false);
            sky.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 9);
            var skyRenderer = sky.AddComponent<SpriteRenderer>(); skyRenderer.sprite = Exact("backgrounds/coast/sky_base.png");
            skyRenderer.sortingLayerName = "Default"; skyRenderer.sortingOrder = -1000;
            sky.AddComponent<StarSwellCoastV01CameraCover>().Configure(camera);

            var ocean = new GameObject("Coast V01 distant ocean underlay"); ocean.transform.SetParent(backgroundRoot, false);
            if (!ocean.AddComponent<StarSwellCoastV01OceanField>().Configure(catalog, camera,
                StarSwellCoastV01PuzzleBinding.ManifestSha256, "tiles/water/carrier.png",
                0, horizonY, 9, "Default", -880))
                throw new InvalidOperationException("Coast V01 ocean carrier configuration failed.");

            var moving = new List<StarSwellCoastV01BackgroundRig.Layer>();
            foreach (var row in layout.layers)
            {
                var go = new GameObject(row.id); go.transform.SetParent(backgroundRoot, false);
                bool unique = row.id == "unique_world_horizon" || row.id == "one_setting_sun";
                go.transform.position = new Vector3(mapCenterX + At(row.positionWorld, 0),
                    dy + At(row.positionWorld, 1), At(row.positionWorld, 2));
                go.transform.localScale = new Vector3(At(row.localScale, 0, 1), At(row.localScale, 1, 1), At(row.localScale, 2, 1));
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = Exact(row.file);
                renderer.sortingLayerName = "Default"; renderer.sortingOrder = row.sortingOrder;
                if (row.color != null && row.color.Length == 4)
                    renderer.color = new Color(row.color[0], row.color[1], row.color[2], row.color[3]);
                if (unique) continue; // One world-fixed horizon and one world-fixed sun.
                moving.Add(new StarSwellCoastV01BackgroundRig.Layer
                {
                    renderer = renderer, sortingOrder = row.sortingOrder,
                    parallaxMultiplier = row.parallaxMultiplier,
                    driftAmplitude = new Vector2(At(row.driftAmplitude, 0), At(row.driftAmplitude, 1)),
                    driftPeriodSeconds = new Vector2(At(row.driftPeriodSeconds, 0, 100), At(row.driftPeriodSeconds, 1, 137)),
                    phaseDegrees = row.phaseDegrees, twinkleAmount = row.twinkleAmount,
                    twinklePeriodSeconds = row.twinklePeriodSeconds, wrapToView = row.wrapToView,
                    guardWorld = new Vector2(At(row.guardWorld, 0, 1), At(row.guardWorld, 1, 1))
                });
            }
            rig = backgroundRoot.gameObject.AddComponent<StarSwellCoastV01BackgroundRig>();
            rig.Configure(camera, 0, "Default", moving.ToArray());
            rig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }
        void Update()
        {
            if (rig && rig.ReducedMotion != StarwayRanchMotionSettings.ReducedMotion)
                rig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }
    }
}
