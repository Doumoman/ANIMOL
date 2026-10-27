using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ANIMOL.Art.V12;
using ANIMOL.Art.RanchConnectedDecorV02;
using ANIMOL.Art.RanchDevicesV13;
using ANIMOL.Art.TheaterConnectedDecorV01;
using ANIMOL.Art.SanctumConnectedDecorV01;
using ANIMOL.Art.CoastConnectedDecorV01;

namespace ANIMOL.Puzzle
{
    // Visual-only autotiling: this class never writes terrain or owns a collider.
    [DefaultExecutionOrder(200)]
    public sealed class PuzzleMapView : MonoBehaviour
    {
        public PuzzleMap map;
        readonly Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
        readonly Dictionary<Vector2Int, GameObject> bubbleViews = new Dictionary<Vector2Int, GameObject>();
        sealed class ClassicWindVisual
        {
            public StarwayRanchV12AnimatedVisual body;
            public string direction, idleKey;
        }
        readonly Dictionary<Vector2Int, ClassicWindVisual> classicWind = new Dictionary<Vector2Int, ClassicWindVisual>();
        readonly Dictionary<string, Transform> ranchActors = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
        readonly Dictionary<string, RanchDeviceVisualV13> ranchSheepVisuals = new Dictionary<string, RanchDeviceVisualV13>(System.StringComparer.Ordinal);
        readonly Dictionary<string, RanchPondPairVisualR2> ranchPondVisuals = new Dictionary<string, RanchPondPairVisualR2>(System.StringComparer.Ordinal);
        readonly Dictionary<Vector2Int, RanchDeviceVisualV13> ranchGrassVisuals = new Dictionary<Vector2Int, RanchDeviceVisualV13>();
        readonly List<GameObject> ranchEffects = new List<GameObject>();
        Transform objectsRoot;
        RectInt visible;
        SkyIslandMapRenderer island;
        StarwayRanchMapRenderer ranch;
        MemorySkyTheaterRenderer theater;
        EchoTheaterV02MapRenderer theaterV02;
        ClockDomeRenderer dome;
        SunsetSanctumV02MapRenderer sanctumV02;
        TwilightCoastRenderer coast;
        StarSwellCoastV01MapRenderer coastV01;
        RanchConnectedDecorV02Controller connectedDecor;
        RanchConnectedDecorV02PublishedSnapshotSource ranchDecorSource;
        RanchConnectedDecorV02Binding ranchDecorBinding;
        long ranchDecorRevision;
        readonly List<GameObject> ranchLegacyDecorationViews = new List<GameObject>();
        TheaterConnectedDecorV01Controller theaterConnectedDecor;
        TheaterConnectedDecorV01PublishedSnapshotSource theaterDecorSource;
        TheaterConnectedDecorV01Binding theaterDecorBinding;
        long theaterDecorRevision;
        readonly List<GameObject> theaterLegacyDecorationViews = new List<GameObject>();
        SanctumConnectedDecorV01Controller sanctumConnectedDecor;
        SanctumConnectedDecorV01PublishedSnapshotSource sanctumDecorSource;
        SanctumConnectedDecorV01Binding sanctumDecorBinding;
        long sanctumDecorRevision;
        readonly List<GameObject> sanctumLegacyDecorationViews = new List<GameObject>();
        CoastConnectedDecorV01Controller coastConnectedDecor;
        CoastConnectedDecorV01PublishedSnapshotSource coastDecorSource;
        CoastConnectedDecorV01Binding coastDecorBinding;
        long coastDecorRevision;
        readonly List<GameObject> coastLegacyDecorationViews = new List<GameObject>();
        public RanchConnectedDecorV02Controller ConnectedDecor => connectedDecor;
        public TheaterConnectedDecorV01Controller TheaterConnectedDecor => theaterConnectedDecor;
        public SanctumConnectedDecorV01Controller SanctumConnectedDecor => sanctumConnectedDecor;
        public CoastConnectedDecorV01Controller CoastConnectedDecor => coastConnectedDecor;
        public int RefreshedCells { get; private set; }
        public void Bind(PuzzleMap data)
        {
            Unbind(); map = data; ranchDecorRevision = 0; theaterDecorRevision = 0; sanctumDecorRevision = 0; coastDecorRevision = 0;
            map.TerrainChanged += RefreshNeighborhood; map.Resized += Rebuild;
            map.DecorationsChanged += DecorationsUpdated;
            Rebuild();
        }
        void Unbind() { if (!map) return; map.TerrainChanged -= RefreshNeighborhood; map.Resized -= Rebuild; map.DecorationsChanged -= DecorationsUpdated; }
        void DecorationsUpdated()
        {
            ranchDecorRevision++;
            theaterDecorRevision++;
            sanctumDecorRevision++;
            coastDecorRevision++;
            if (ranchDecorSource && RanchConnectedDecorV02Host.IsRanch(map))
            { RefreshLegacyRanchDecor(); PublishRanchDecor(); }
            else if (theaterDecorSource && TheaterConnectedDecorHost.IsTheater(map))
            { RefreshLegacyTheaterDecor(); PublishTheaterDecor(); }
            else if (sanctumDecorSource && SanctumConnectedDecorHost.IsSanctum(map))
            { RefreshLegacySanctumDecor(); PublishSanctumDecor(); }
            else if (coastDecorSource && CoastConnectedDecorHost.IsCoast(map))
            { RefreshLegacyCoastDecor(); PublishCoastDecor(); }
            else Rebuild();
        }
        void OnEnable() { if (map && objectsRoot) { Rebuild(); if (currentSession != null) Present(currentSession); } }
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
            connectedDecor = null; ranchDecorSource = null; ranchDecorBinding = null;
            ranchLegacyDecorationViews.Clear();
            theaterConnectedDecor = null; theaterDecorSource = null; theaterDecorBinding = null;
            theaterLegacyDecorationViews.Clear();
            sanctumConnectedDecor = null; sanctumDecorSource = null; sanctumDecorBinding = null;
            sanctumLegacyDecorationViews.Clear();
            coastConnectedDecor = null; coastDecorSource = null; coastDecorBinding = null;
            coastLegacyDecorationViews.Clear();
            bubbleViews.Clear(); classicWind.Clear(); ranchActors.Clear(); ranchSheepVisuals.Clear();
            ranchPondVisuals.Clear(); ranchGrassVisuals.Clear(); ranchEffects.Clear();
            objectsRoot = new GameObject("Devices and Decoration (no parallax)").transform; objectsRoot.SetParent(transform, false);
            if (map.theme && map.theme.starSwellCoastV01Catalog)
            {
                if (!coastV01) coastV01 = new GameObject("Star Swell Coast V01 world").AddComponent<StarSwellCoastV01MapRenderer>();
                coastV01.transform.SetParent(transform, false);
                coastV01.Bind(map, map.theme.starSwellCoastV01Catalog);
            }
            else if (coastV01) { Remove(coastV01.gameObject); coastV01 = null; }
            if (map.theme && map.theme.twilightCoast && !coastV01)
            {
                if (!coast) coast = new GameObject("Twilight coast environment").AddComponent<TwilightCoastRenderer>();
                coast.transform.SetParent(transform, false); coast.Bind(map, map.theme.twilightCoast);
            }
            else if (coast) { Remove(coast.gameObject); coast = null; }
            if (map.theme && map.theme.sunsetSanctumV02Catalog)
            {
                if (!sanctumV02) sanctumV02 = new GameObject("Sunset Sanctum V02 world").AddComponent<SunsetSanctumV02MapRenderer>();
                sanctumV02.transform.SetParent(transform, false);
                sanctumV02.Bind(map, map.theme.sunsetSanctumV02Catalog);
            }
            else if (sanctumV02) { Remove(sanctumV02.gameObject); sanctumV02 = null; }
            if (map.theme && map.theme.clockDome && !sanctumV02)
            {
                if (!dome) dome = new GameObject("Clock dome environment").AddComponent<ClockDomeRenderer>();
                dome.transform.SetParent(transform, false); dome.Bind(map, map.theme.clockDome);
            }
            else if (dome) { Remove(dome.gameObject); dome = null; }
            if (map.theme && map.theme.memorySkyTheater && map.theme.memorySkyTheater.v02)
            {
                if (!theaterV02) theaterV02 = new GameObject("Echo theater environment").AddComponent<EchoTheaterV02MapRenderer>();
                theaterV02.transform.SetParent(transform, false);
                theaterV02.Bind(map, map.theme.memorySkyTheater.v02, map.theme.memorySkyTheater.defaultLayout);
            }
            else if (theaterV02) { Remove(theaterV02.gameObject); theaterV02 = null; }
            if (map.theme && map.theme.memorySkyTheater && !map.theme.memorySkyTheater.v02)
            {
                if (!theater) theater = new GameObject("Memory theater environment").AddComponent<MemorySkyTheaterRenderer>();
                theater.transform.SetParent(transform, false); theater.Bind(map, map.theme.memorySkyTheater);
            }
            else if (theater) { Remove(theater.gameObject); theater = null; }
            if (map.theme && map.theme.starwayRanch)
            {
                if (!ranch) ranch = new GameObject("Starway Ranch V12 foreground").AddComponent<StarwayRanchMapRenderer>();
                ranch.transform.SetParent(transform, false);
                ranch.Bind(map, map.theme.starwayRanch);
            }
            else if (ranch) { Remove(ranch.gameObject); ranch = null; }
            if (map.theme && map.theme.skyIsland && !map.theme.starwayRanch)
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
            if (map.theme && map.theme.starwayRanch && map.theme.ranchConnectedDecorV02Catalog &&
                map.theme.ranchConnectedDecorV02ManifestPin)
            {
                var node = new GameObject("Ranch connected decoration V02");
                node.SetActive(false); node.transform.SetParent(objectsRoot, false);
                connectedDecor = node.AddComponent<RanchConnectedDecorV02Controller>();
                ranchDecorSource = node.AddComponent<RanchConnectedDecorV02PublishedSnapshotSource>();
                ranchDecorBinding = node.AddComponent<RanchConnectedDecorV02Binding>();
                PublishRanchDecor();
                var pin = map.theme.ranchConnectedDecorV02ManifestPin.text.Trim();
                if (!ranchDecorBinding.Bind(connectedDecor, map.theme.ranchConnectedDecorV02Catalog,
                    pin, "Default", ranchDecorSource, out var error))
                    Debug.LogError("Ranch decoration binding rejected: " + error, this);
                node.SetActive(true);
            }
            else if (map.theme && map.theme.starwayRanch && map.decorations.Any(d =>
                !string.IsNullOrEmpty(d.visualFamily) || RanchConnectedDecorV02Host.TryMap(map, d, out _)))
                Debug.LogError("Ranch connected decoration catalog or manifest pin is missing.", this);
            if (TheaterConnectedDecorHost.IsTheater(map) && map.theme.theaterConnectedDecorV01Catalog &&
                map.theme.theaterConnectedDecorV01ManifestPin)
            {
                var node = new GameObject("Theater connected decoration V01");
                node.SetActive(false); node.transform.SetParent(objectsRoot, false);
                theaterConnectedDecor = node.AddComponent<TheaterConnectedDecorV01Controller>();
                theaterDecorSource = node.AddComponent<TheaterConnectedDecorV01PublishedSnapshotSource>();
                theaterDecorBinding = node.AddComponent<TheaterConnectedDecorV01Binding>();
                PublishTheaterDecor();
                var pin = map.theme.theaterConnectedDecorV01ManifestPin.text.Trim();
                if (!theaterDecorBinding.Bind(theaterConnectedDecor, map.theme.theaterConnectedDecorV01Catalog,
                    pin, "Default", theaterDecorSource, out var error))
                    Debug.LogError("Theater decoration binding rejected: " + error, this);
                node.SetActive(true);
            }
            else if (TheaterConnectedDecorHost.IsTheater(map) && map.decorations.Any(d =>
                TheaterConnectedDecorHost.IsDeclaredKey(d.visualFamily)))
                Debug.LogError("Theater connected decoration catalog or manifest pin is missing.", this);
            if (SanctumConnectedDecorHost.IsSanctum(map) && map.theme.sanctumConnectedDecorV01Catalog &&
                map.theme.sanctumConnectedDecorV01ManifestPin)
            {
                var node = new GameObject("Sanctum connected decoration V01");
                node.SetActive(false); node.transform.SetParent(objectsRoot, false);
                sanctumConnectedDecor = node.AddComponent<SanctumConnectedDecorV01Controller>();
                sanctumDecorSource = node.AddComponent<SanctumConnectedDecorV01PublishedSnapshotSource>();
                sanctumDecorBinding = node.AddComponent<SanctumConnectedDecorV01Binding>();
                PublishSanctumDecor();
                var pin = map.theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
                if (!sanctumDecorBinding.Bind(sanctumConnectedDecor, map.theme.sanctumConnectedDecorV01Catalog,
                    pin, "Default", sanctumDecorSource, out var error))
                    Debug.LogError("Sanctum decoration binding rejected: " + error, this);
                node.SetActive(true);
            }
            else if (SanctumConnectedDecorHost.IsSanctum(map) && map.decorations.Any(d =>
                SanctumConnectedDecorHost.IsDeclaredKey(d.visualFamily)))
                Debug.LogError("Sanctum connected decoration catalog or manifest pin is missing.", this);
            if (CoastConnectedDecorHost.IsCoast(map) && map.theme.coastConnectedDecorV01Catalog &&
                map.theme.coastConnectedDecorV01ManifestPin)
            {
                var node = new GameObject("Coast connected decoration V01");
                node.SetActive(false); node.transform.SetParent(objectsRoot, false);
                coastConnectedDecor = node.AddComponent<CoastConnectedDecorV01Controller>();
                coastDecorSource = node.AddComponent<CoastConnectedDecorV01PublishedSnapshotSource>();
                coastDecorBinding = node.AddComponent<CoastConnectedDecorV01Binding>();
                PublishCoastDecor();
                var pin = map.theme.coastConnectedDecorV01ManifestPin.text.Trim();
                if (!coastDecorBinding.Bind(coastConnectedDecor, map.theme.coastConnectedDecorV01Catalog,
                    pin, "Default", coastDecorSource, out var error))
                    Debug.LogError("Coast decoration binding rejected: " + error, this);
                node.SetActive(true);
            }
            else if (CoastConnectedDecorHost.IsCoast(map) && map.decorations.Any(d =>
                CoastConnectedDecorHost.IsDeclaredKey(d.visualFamily)))
                Debug.LogError("Coast connected decoration catalog or manifest pin is missing.", this);
            foreach (var d in map.decorations) if (map.Contains(d.position) &&
                !TheaterConnectedDecorHost.IsTheater(map) && !SanctumConnectedDecorHost.IsSanctum(map) &&
                !CoastConnectedDecorHost.IsCoast(map))
            {
                if (!string.IsNullOrEmpty(d.visualFamily) || RanchConnectedDecorV02Host.TryMap(map, d, out _)) continue;
                if (TheaterConnectedDecorHost.TryMap(map, d, out _)) continue;
                if (ranch) continue;
                else Sprite(objectsRoot, "Decoration",
                    d.sprite ? d.sprite : map.theme.decoration,
                    d.position, 15, Vector2.one * .8f, d.tint);
            }
            if (TheaterConnectedDecorHost.IsTheater(map)) RefreshLegacyTheaterDecor();
            if (RanchConnectedDecorV02Host.IsRanch(map)) RefreshLegacyRanchDecor();
            if (SanctumConnectedDecorHost.IsSanctum(map)) RefreshLegacySanctumDecor();
            if (CoastConnectedDecorHost.IsCoast(map)) RefreshLegacyCoastDecor();
            if (map.scenario)
            {
                var nestSprite = map.theme.starwayRanch ? map.theme.starwayRanch.Find("props/birdhouse.png") : map.theme.decoration;
                foreach (var chick in map.scenario.devices.Where(d => d.role == PrototypeRole.Chick &&
                    map.Contains(d.position) && d.target != Vector2Int.zero && map.Walkable(d.target)))
                    if (nestSprite) Sprite(objectsRoot, "Nest " + chick.id, nestSprite, chick.target, 16,
                        Vector2.one * .8f, Color.white);
                if (map.theme && map.theme.starwayRanch && map.theme.starwayRanch.v13Devices)
                    foreach (var art in map.scenario.ranchV13Art)
                    {
                        if (art == null || !map.Contains(art.position)) continue;
                        var root = new GameObject("Ranch R2 " + art.family + " " + art.Id);
                        root.transform.SetParent(objectsRoot, false);
                        root.transform.localPosition = (Vector2)art.position;
                        var catalog = map.theme.starwayRanch.v13Devices;
                        if (art.family == "sluice")
                        {
                            var a = RanchCellVisual(root.transform, "Cell A", Vector2Int.zero, 28, catalog);
                            var b = RanchCellVisual(root.transform, "Cell B", Vector2Int.right, 28, catalog);
                            var pair = root.AddComponent<RanchPondPairVisualR2>();
                            if (!pair.Configure(catalog, a, b, out var error) || !pair.SetResolvedState(art.rightState))
                                Debug.LogError("R2 pond visual failed: " + error, root);
                            ranchPondVisuals[art.Id] = pair;
                        }
                        else if (art.family == "sheep" || art.family == "tall_grass")
                        {
                            var visual = RanchCellVisual(root.transform, "Visual", Vector2Int.zero, 28, catalog);
                            if (!visual.SetState(art.ClipKey)) Debug.LogError("R2 ranch clip missing: " + art.ClipKey, root);
                            if (art.family == "sheep") { ranchActors[art.Id] = root.transform; ranchSheepVisuals[art.Id] = visual; }
                            else ranchGrassVisuals[art.position] = visual;
                        }
                    }
            }
            var possibleBubbles = map.bubbles.Concat(map.scenario ? map.scenario.devices.Where(d =>
                d.role == PrototypeRole.StarSocket && d.variant == "socket" && d.socketReward == StarSocketReward.RevealBubble)
                .Select(d => d.socketBubbleCell) : Enumerable.Empty<Vector2Int>());
            foreach (var p in possibleBubbles.Distinct()) if (map.Contains(p) && !bubbleViews.ContainsKey(p))
                bubbleViews[p] = Sprite(objectsRoot, "Bubble " + p, map.theme.bubble, p, 25, Vector2.one * .65f, Color.white).gameObject;
            if (map.Contains(map.exit)) Sprite(objectsRoot, "Exit", map.theme.exit, map.exit, 20, Vector2.one * .9f, Color.white);
            if (currentSession != null) Present(currentSession);
        }
        void RefreshLegacyTheaterDecor()
        {
            foreach (var visual in theaterLegacyDecorationViews) if (visual) Remove(visual);
            theaterLegacyDecorationViews.Clear();
            foreach (var d in map.decorations)
            {
                if (!map.Contains(d.position) || !string.IsNullOrEmpty(d.visualFamily) ||
                    TheaterConnectedDecorHost.TryMap(map, d, out _)) continue;
                theaterLegacyDecorationViews.Add(Sprite(objectsRoot, "Decoration",
                    d.sprite ? d.sprite : map.theme.decoration,
                    d.position, 15, Vector2.one * .8f, d.tint).gameObject);
            }
        }
        void PublishTheaterDecor()
        {
            if (!theaterDecorSource || !map) return;
            var origin = objectsRoot.TransformPoint(new Vector3(-.5f, -.5f, 0));
            if (!TheaterConnectedDecorHost.TryCreateSnapshot(map, map.decorations, origin,
                objectsRoot.lossyScale.x, theaterDecorRevision, out var snapshot, out var error) ||
                !theaterDecorSource.PublishSnapshot(snapshot, out error))
            {
                theaterDecorSource.InvalidateSnapshot(error);
                Debug.LogError("Theater decoration snapshot rejected: " + error, this);
            }
        }
        void RefreshLegacySanctumDecor()
        {
            foreach (var visual in sanctumLegacyDecorationViews) if (visual) Remove(visual);
            sanctumLegacyDecorationViews.Clear();
            foreach (var d in map.decorations)
            {
                if (!map.Contains(d.position) || SanctumConnectedDecorHost.IsDeclaredKey(d.visualFamily) ||
                    SanctumConnectedDecorHost.TryMap(map, d, out _) ||
                    SanctumConnectedDecorHost.IsLegacySanctumProp(map.theme, d.sprite)) continue;
                sanctumLegacyDecorationViews.Add(Sprite(objectsRoot, "Decoration",
                    d.sprite ? d.sprite : map.theme.decoration,
                    d.position, 15, Vector2.one * .8f, d.tint).gameObject);
            }
        }
        void PublishSanctumDecor()
        {
            if (!sanctumDecorSource || !map) return;
            var origin = objectsRoot.TransformPoint(new Vector3(-.5f, -.5f, 0));
            if (!SanctumConnectedDecorHost.TryCreateSnapshot(map, map.decorations, origin,
                objectsRoot.lossyScale.x, sanctumDecorRevision, out var snapshot, out var error) ||
                !sanctumDecorSource.PublishSnapshot(snapshot, out error))
            {
                sanctumDecorSource.InvalidateSnapshot(error);
                Debug.LogError("Sanctum decoration snapshot rejected: " + error, this);
            }
        }
        void RefreshLegacyCoastDecor()
        {
            foreach (var visual in coastLegacyDecorationViews) if (visual) Remove(visual);
            coastLegacyDecorationViews.Clear();
            foreach (var d in map.decorations)
            {
                if (!map.Contains(d.position) || CoastConnectedDecorHost.IsDeclaredKey(d.visualFamily) ||
                    CoastConnectedDecorHost.TryMap(map, d, out _) ||
                    CoastConnectedDecorHost.IsLegacyCoastProp(map.theme, d.sprite)) continue;
                coastLegacyDecorationViews.Add(Sprite(objectsRoot, "Decoration",
                    d.sprite ? d.sprite : map.theme.decoration,
                    d.position, 15, Vector2.one * .8f, d.tint).gameObject);
            }
        }
        void PublishCoastDecor()
        {
            if (!coastDecorSource || !map) return;
            var origin = objectsRoot.TransformPoint(new Vector3(-.5f, -.5f, 0));
            if (!CoastConnectedDecorHost.TryCreateSnapshot(map, map.decorations, origin,
                objectsRoot.lossyScale.x, coastDecorRevision, out var snapshot, out var error) ||
                !coastDecorSource.PublishSnapshot(snapshot, out error))
            {
                coastDecorSource.InvalidateSnapshot(error);
                Debug.LogError("Coast decoration snapshot rejected: " + error, this);
            }
        }
        void RefreshLegacyRanchDecor()
        {
            foreach (var visual in ranchLegacyDecorationViews) if (visual) Remove(visual);
            ranchLegacyDecorationViews.Clear();
            if (!objectsRoot || !RanchConnectedDecorV02Host.IsRanch(map)) return;
            foreach (var cell in map.decorations)
            {
                if (!map.Contains(cell.position) || !string.IsNullOrEmpty(cell.visualFamily) ||
                    RanchConnectedDecorV02Host.TryMap(map, cell, out _)) continue;
                var visual = cell.sprite == map.theme.starwayRanch.Find("props/windmill_body.png")
                    ? CreateWindmill(cell.position) : CreateRanchDecoration(cell);
                if (visual) ranchLegacyDecorationViews.Add(visual);
            }
        }
        void PublishRanchDecor()
        {
            if (!ranchDecorSource || !map) return;
            var origin = objectsRoot.TransformPoint(new Vector3(-.5f, -.5f, 0));
            if (!RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations, origin,
                objectsRoot.lossyScale.x, ranchDecorRevision, out var snapshot, out var error) ||
                !ranchDecorSource.PublishSnapshot(snapshot, out error))
            {
                ranchDecorSource.InvalidateSnapshot(error);
                Debug.LogError("Ranch decoration snapshot rejected: " + error, this);
            }
        }
        GameObject CreateRanchDecoration(DecorationCell cell)
        {
            var art = RanchConnectedDecorV02Host.SourceMapping(map, cell.sprite);
            var size = new Vector2(art == null ? 1 : art.width, art == null ? 1 : art.height);
            var node = new GameObject("V12 decoration");
            node.transform.SetParent(objectsRoot, false);
            node.transform.localPosition = (Vector2)cell.position + (size - Vector2.one) * .5f;
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = cell.sprite ? cell.sprite : map.theme.decoration;
            renderer.color = cell.tint;
            renderer.sortingOrder = 15;
            return node;
        }
        GameObject CreateWindmill(Vector2Int position)
        {
            var art = map.theme.starwayRanch;
            var rotorSprite = art.Find("props/windmill_rotor.png");
            if (!rotorSprite) { Debug.LogError("Missing V12 windmill rotor", this); return null; }
            var owner = new GameObject("V12 windmill visual");
            owner.transform.SetParent(objectsRoot, false);
            // The V12 pivot is the centre of its native 2x2 logical ground.
            // The rear row may be walked through, but the art remains full size.
            owner.transform.localPosition = (Vector2)position + Vector2.one;
            var body = new GameObject("Fixed body");
            body.transform.SetParent(owner.transform, false);
            var bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = art.Find("props/windmill_body.png");
            bodyRenderer.sortingLayerName = "Default";
            bodyRenderer.sortingOrder = RanchConnectedDecorV02Host.ForegroundSortingOrder;
            var rotor = new GameObject("Four blade rotor");
            rotor.transform.SetParent(owner.transform, false);
            rotor.transform.localPosition = new Vector3(0, .703125f, 0);
            var rotorRenderer = rotor.AddComponent<SpriteRenderer>();
            rotorRenderer.sprite = rotorSprite;
            rotorRenderer.sortingLayerName = "Default";
            rotorRenderer.sortingOrder = RanchConnectedDecorV02Host.ForegroundSortingOrder + 1;
            var motion = owner.AddComponent<StarwayRanchV12RotorVisual>();
            motion.Configure(rotor.transform, 18f);
            motion.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
            return owner;
        }
        public void RefreshNeighborhood(Vector2Int p)
        { if (coastV01) {
                coastV01.RefreshNeighborhood(p);
                if (coastDecorSource) { coastDecorRevision++; PublishCoastDecor(); }
                return;
            }
            if (sanctumV02) {
                sanctumV02.RefreshNeighborhood(p);
                if (sanctumDecorSource) { sanctumDecorRevision++; PublishSanctumDecor(); }
                return;
            }
            if (ranch) { Rebuild(); return; }
            for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++) RefreshCell(p + new Vector2Int(x, y)); }
        public void RefreshCell(Vector2Int p)
        {
            RefreshedCells++;
            if (tiles.TryGetValue(p, out var old)) { Remove(old); tiles.Remove(p); }
            if (sanctumV02 || coastV01) return;
            if (ranch) return;
            if ((dome || coast) && map.TryTerrain(p, out var domeCell))
            {
                if (domeCell.blocked && visible.Contains(p))
                {
                    var marker = new GameObject("Blocked terrain " + p); marker.transform.SetParent(transform, false); marker.transform.localPosition = (Vector2)p;
                    tiles[p] = marker; Label(marker.transform, "#", Vector2.zero, Color.white, 10);
                }
                return;
            }
            if ((theaterV02 || theater) && map.TryTerrain(p, out _)) return;
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
        public bool HasSunsetSanctumV02 => sanctumV02;
        public bool HasStarSwellCoastV01 => coastV01;
        public void ConfigureStarSwellCoastV01Background(Camera camera)
        { if (coastV01) coastV01.ConfigureBackground(camera); }
        public void ConfigureSunsetSanctumV02Background(Camera camera)
        { if (sanctumV02) sanctumV02.ConfigureBackground(camera); }
        void DrawDevice(DeviceCell d)
        {
            if (ranch && map.theme.starwayRanch.v12Animations &&
                (d.kind == DeviceKind.Conveyor || d.kind == DeviceKind.Wind || d.kind == DeviceKind.Brake))
            {
                DrawRanchDevice(d);
                return;
            }
            string glyph = d.kind == DeviceKind.Portal ? "O" : d.kind == DeviceKind.Ice ? "~" : d.kind == DeviceKind.Brake ? "X" :
                d.direction == Vector2Int.up ? "^" : d.direction == Vector2Int.down ? "v" : d.direction == Vector2Int.left ? "<" : ">";
            Color color = d.kind == DeviceKind.Portal ? new Color(.85f, .48f, 1) : d.kind == DeviceKind.Wind ? Color.cyan : d.kind == DeviceKind.Ice ? new Color(.65f, .9f, 1) : Color.yellow;
            var root = new GameObject(d.kind + " " + d.position); root.transform.SetParent(objectsRoot, false); root.transform.localPosition = (Vector2)d.position;
            Label(root.transform, glyph, Vector2.zero, color, 12);
        }
        static string V12Direction(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return "east";
            if (direction == Vector2Int.up) return "north";
            if (direction == Vector2Int.left) return "west";
            if (direction == Vector2Int.down) return "south";
            return null;
        }
        void DrawRanchDevice(DeviceCell d)
        {
            var art = map.theme.starwayRanch;
            var direction = V12Direction(d.direction);
            if (direction == null) { Debug.LogError("V12 classic device has no cardinal direction at " + d.position, this); return; }
            var axis = d.direction.x != 0 ? "horizontal" : "vertical";
            var capital = direction.Substring(0, 1).ToUpperInvariant();
            var key = d.kind == DeviceKind.Conveyor ? "conveyor_" + direction + "_transport" :
                d.kind == DeviceKind.Wind ? "wind_corridor_" + capital + "_idle" :
                "brake_patch_" + axis + "_press_release";
            var go = new GameObject("V12 " + d.kind + " " + d.position);
            go.transform.SetParent(objectsRoot, false);
            go.transform.localPosition = (Vector2)d.position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 30;
            var player = go.AddComponent<StarwayRanchV12AnimatedVisual>();
            if (!player.Configure(art.v12Animations, renderer, art.v12ManifestSha256) ||
                !(d.kind == DeviceKind.Conveyor ? player.Play(key) : player.SnapToState(key)))
                Debug.LogError("V12 classic device binding failed: " + d.kind + " at " + d.position, go);
            if (d.kind != DeviceKind.Wind) return;
            // Classic DeviceCell.Wind moves only an actor already on its own cell. It has
            // no range authority; the removed V12 gust sprites must not invent one.
            classicWind.Add(d.position, new ClassicWindVisual { body = player,
                direction = direction, idleKey = key });
        }
        public void StartClassicWind(Vector2Int cell)
        {
            if (!classicWind.TryGetValue(cell, out var visual)) return;
            var capital = visual.direction.Substring(0, 1).ToUpperInvariant();
            visual.body.Play("wind_corridor_" + capital + "_active");
        }
        public void FinishClassicWind(Vector2Int cell)
        {
            if (!classicWind.TryGetValue(cell, out var visual)) return;
            visual.body.SnapToState(visual.idleKey);
        }
        static void Label(Transform parent, string text, Vector2 p, Color color, int order)
        {
            var go = new GameObject("Glyph"); go.transform.SetParent(parent, false); go.transform.localPosition = p;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 48; label.characterSize = .12f; label.color = color; go.GetComponent<MeshRenderer>().sortingOrder = order;
        }
        public void Present(IPuzzleBoardState session)
        {
            currentSession = session;
            foreach (var entry in bubbleViews) entry.Value.SetActive(session.IsBubbleVisible(entry.Key) && !session.IsCollected(entry.Key));
            var devices = session as PuzzleDeviceSession;
            if (devices == null) return;
            foreach (var actor in ranchActors)
            {
                var exists = devices.RanchSheep.TryGetValue(actor.Key, out var cell);
                actor.Value.gameObject.SetActive(exists);
                if (exists)
                {
                    actor.Value.localPosition = (Vector2)cell;
                    var direction = devices.RanchSheepDirection(actor.Key);
                    var heading = direction == Vector2Int.up ? "north" : direction == Vector2Int.down ? "south" :
                        direction == Vector2Int.left ? "west" : "east";
                    ranchSheepVisuals[actor.Key].SetState("sheep/" + heading + "/idle", true);
                    ranchSheepVisuals[actor.Key].GetComponent<SpriteRenderer>().sortingOrder = cell.y < devices.Cell.y ? 41 : 39;
                }
            }
            foreach (var grass in ranchGrassVisuals)
            {
                var visible = devices.RanchGrassPresent(grass.Key);
                grass.Value.gameObject.SetActive(visible);
                if (visible) grass.Value.SetState("tall_grass/idle", true);
            }
            foreach (var pond in ranchPondVisuals)
                if (devices.RanchPonds.TryGetValue(pond.Key, out var wetA)) pond.Value.SetResolvedState(wetA);
        }
        static RanchDeviceVisualV13 RanchCellVisual(Transform parent, string name, Vector2Int offset,
            int sorting, RanchDevicesV13Catalog catalog)
        {
            var visual = new GameObject(name); visual.transform.SetParent(parent, false);
            visual.transform.localPosition = (Vector2)offset;
            visual.AddComponent<SpriteRenderer>().sortingOrder = sorting;
            var view = visual.AddComponent<RanchDeviceVisualV13>();
            if (!view.Configure(catalog, out var error)) Debug.LogError("R2 visual configure failed: " + error, visual);
            return view;
        }
        public Transform RanchActor(string id) => id != null && ranchActors.TryGetValue(id, out var actor) ? actor : null;
        public void BeginRanchMotion(string id, Vector2Int direction, bool wind)
        {
            if (!ranchSheepVisuals.TryGetValue(id, out var visual)) return;
            var heading = direction == Vector2Int.up ? "north" : direction == Vector2Int.down ? "south" :
                direction == Vector2Int.left ? "west" : "east";
            visual.SetState("sheep/" + heading + (wind ? "/wind" : "/walk"), true);
        }
        public void PlayRanchCue(PrototypeEvent cue)
        {
            if ((cue.eventName == "SheepPushed" || cue.eventName == "SheepWindMoved") &&
                ranchSheepVisuals.TryGetValue(cue.actor, out var sheep))
            { sheep.SetState(cue.state, true); return; }
            if (cue.eventName == "PondCrossed" && currentSession is PuzzleDeviceSession resolved &&
                ranchPondVisuals.TryGetValue(cue.actor, out var pair))
            {
                var wetA = resolved.RanchPonds[cue.actor];
                pair.PlayCommittedCrossing(!wetA); return;
            }
            if (cue.eventName != "SheepAteAndRemoved" && cue.eventName != "SheepWindAteAndRemoved") return;
            if (!map || !map.theme || !map.theme.starwayRanch || !map.theme.starwayRanch.v13Devices) return;
            if (ranchActors.TryGetValue(cue.actor, out var actor)) actor.gameObject.SetActive(false);
            var catalog = map.theme.starwayRanch.v13Devices;
            var heading = cue.state.Split('/')[1];
            SpawnRanchConsumption("Sheep consumed", cue.to, catalog, heading);
            SpawnRanchConsumption("Grass consumed", cue.to, catalog, null);
        }
        IPuzzleBoardState currentSession;
        void SpawnRanchConsumption(string name, Vector2Int cell, RanchDevicesV13Catalog catalog, string heading)
        {
            var root = new GameObject(name); root.transform.SetParent(objectsRoot, false);
            root.transform.localPosition = (Vector2)cell;
            root.AddComponent<SpriteRenderer>().sortingOrder = 29;
            root.AddComponent<RanchDeviceVisualV13>();
            var effect = root.AddComponent<RanchSheepConsumptionVisualR2>();
            string error;
            var played = heading == null ? effect.PlayGrass(catalog, out error) : effect.Play(catalog, heading, out error);
            if (!played)
                Debug.LogError("R2 consumption effect failed: " + error, root);
            ranchEffects.Add(root);
        }
        public void CancelRanchEffects()
        {
            foreach (var root in ranchEffects) if (root) Remove(root);
            ranchEffects.Clear();
        }
        void LateUpdate()
        {
            if (map) SyncViewport(false);
            for (var i = ranchEffects.Count - 1; i >= 0; i--)
            {
                var effect = ranchEffects[i] ? ranchEffects[i].GetComponent<RanchSheepConsumptionVisualR2>() : null;
                if (effect && effect.IsPlaying) continue;
                if (ranchEffects[i]) Remove(ranchEffects[i]);
                ranchEffects.RemoveAt(i);
            }
        }
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
            if (ranch) ranch.Show(visible);
            if (island) for (var y = previous.yMin; y < previous.yMax; y++) for (var x = previous.xMin; x < previous.xMax; x++)
                if (!visible.Contains(new Vector2Int(x, y))) island.ClearCell(new Vector2Int(x, y));
            foreach (var p in tiles.Keys.Where(p => !visible.Contains(p)).ToArray()) { Remove(tiles[p]); tiles.Remove(p); }
            for (var y = min.y; y < max.y; y++) for (var x = min.x; x < max.x; x++)
            { var p = new Vector2Int(x, y); if (!tiles.ContainsKey(p) && (!island || force || !previous.Contains(p)) && map.TryTerrain(p, out _)) RefreshCell(p); }
        }
    }
}
