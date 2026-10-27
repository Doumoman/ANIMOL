using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Art.V12;
using ANIMOL.Art.DevicePatch01;
using ANIMOL.Art.SunsetSanctumV02;
using ANIMOL.Art.StarSwellCoastV01;
using ANIMOL.Presentation;
using UnityEngine;
namespace ANIMOL.Puzzle
{
    public sealed class PuzzleDeviceView : MonoBehaviour
    {
        readonly Dictionary<string, DeviceVisualView> views = new Dictionary<string, DeviceVisualView>();
        readonly Dictionary<string, SunsetSanctumV02PuzzleBinding> sanctumV02 =
            new Dictionary<string, SunsetSanctumV02PuzzleBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, StarSwellCoastV01PuzzleBinding> coastV01 =
            new Dictionary<string, StarSwellCoastV01PuzzleBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, RanchDevicesV13PuzzleBinding> ranchV13 =
            new Dictionary<string, RanchDevicesV13PuzzleBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, SunsetSanctumV02BeamRenderer> sanctumBeams =
            new Dictionary<string, SunsetSanctumV02BeamRenderer>(StringComparer.Ordinal);
        readonly Dictionary<string, string> sanctumBeamSignatures =
            new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, Transform> roots = new Dictionary<string, Transform>();
        readonly Dictionary<string, SeatedStarVisual> seatedStars = new Dictionary<string, SeatedStarVisual>();
        readonly Dictionary<string, FerryVisualView> ferries = new Dictionary<string, FerryVisualView>();
        readonly Dictionary<string, string> pendingIdle = new Dictionary<string, string>();
        readonly HashSet<string> pendingRemoval = new HashSet<string>();
        readonly Dictionary<string, AnimolWindRouteRenderer> windRoutes =
            new Dictionary<string, AnimolWindRouteRenderer>(StringComparer.Ordinal);
        readonly Dictionary<string, WindRoute> displayedWindRoutes =
            new Dictionary<string, WindRoute>(StringComparer.Ordinal);
        PuzzleDeviceSession session;
        float windFeedbackDuration = .16f;
        bool applicationPaused;
        bool applicationFocused = true;
        SpriteRenderer player;
        BeamPathView beams;
        EchoTheaterV03DiceVisualMap diceVisualMap;
        public int MissingBindings { get; private set; }
        public Transform Actor(string id) => roots.TryGetValue(id, out var root) ? root : null;
        public bool HasRanchV13Motion(string id) => id != null && ranchV13.ContainsKey(id);
        public bool BeginRanchV13Motion(string id, IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<bool> windSegments, float secondsPerCell)
        {
            if (!ranchV13.TryGetValue(id, out var binding)) return false;
            if (binding.BeginCommittedMove(cells, session.States[id], secondsPerCell,
                windSegments, out var error)) return true;
            MissingBindings++;
            Debug.LogError("V13 committed movement: " + error + " " + id, this);
            return false;
        }
        public void Bind(PuzzleDeviceSession authority, SpriteRenderer playerRenderer, float actionDurationSeconds = .16f)
        {
            session = authority; player = playerRenderer;
            windFeedbackDuration = Mathf.Max(.001f, actionDurationSeconds);
            var theaterArt = session.Map.theme && session.Map.theme.memorySkyTheater
                ? session.Map.theme.memorySkyTheater.v02 : null;
            if (theaterArt && theaterArt.v03Animations)
            {
                string error = null;
                if (!theaterArt.v03DiceOrientationMap ||
                    !EchoTheaterV03DiceVisualMap.TryParse(theaterArt.v03DiceOrientationMap.bytes,
                        theaterArt.v03Animations.DiceOrientationMapSha256,
                        theaterArt.v03Animations, theaterArt.v03ManifestSha256,
                        out diceVisualMap, out error))
                { MissingBindings++; Debug.LogError("Invalid V03 dice orientation map: " + error, this); }
            }
            foreach (var d in session.Devices)
            {
                if (session.Rules.themeId == "twilight_sea" && StarSwellCoastV01PuzzleBinding.Covers(d))
                {
                    if (!session.Map.theme || !session.Map.theme.starSwellCoastV01Catalog)
                    { MissingBindings++; Debug.LogError("Coast V01 catalog is missing: " + d.id, this); continue; }
                    if (!d.visualPrefab)
                    { MissingBindings++; Debug.LogError("Coast V01 prefab is missing: " + d.id, this); continue; }
                    var instance = Instantiate(d.visualPrefab, transform);
                    instance.name = d.id + " Coast V01";
                    instance.transform.localPosition = (Vector2)session.States[d.id].cell;
                    try
                    {
                        coastV01.Add(d.id, new StarSwellCoastV01PuzzleBinding(d, instance,
                            session.Map.theme.starSwellCoastV01Catalog));
                        roots.Add(d.id, instance.transform);
                    }
                    catch (Exception error)
                    { MissingBindings++; Debug.LogError("Coast V01 binding failed: " + error, instance); instance.SetActive(false); }
                    continue;
                }
                if (session.Rules.themeId == "letter_observatory" && SunsetSanctumV02PuzzleBinding.Covers(d))
                {
                    if (!session.Map.theme || !session.Map.theme.sunsetSanctumV02Catalog || !d.visualPrefab)
                    { MissingBindings++; Debug.LogError("Sunset Sanctum V02 catalog or prefab is missing: " + d.id, this); continue; }
                    var instance = Instantiate(d.visualPrefab, transform);
                    instance.name = d.id;
                    instance.transform.localPosition = (Vector2)session.States[d.id].cell;
                    try
                    {
                        var binding = new SunsetSanctumV02PuzzleBinding(d, instance,
                            session.Map.theme.sunsetSanctumV02Catalog);
                        sanctumV02.Add(d.id, binding);
                        roots.Add(d.id, instance.transform);
                    }
                    catch (Exception error)
                    { MissingBindings++; Debug.LogError("Sunset Sanctum V02 binding failed: " + error, instance); instance.SetActive(false); }
                    continue;
                }
                if (!d.visualPrefab) { MissingBindings++; continue; }
                var root = Instantiate(d.visualPrefab, transform); root.name = d.id; root.transform.localPosition = (Vector2)session.States[d.id].cell; roots[d.id] = root.transform;
                // The follower chick keeps its V12 visual and blocking puzzle behavior.
                if (session.Rules.themeId == "starway" && d.role != PrototypeRole.Chick &&
                    RanchDevicesV13PuzzleBinding.Covers(d))
                {
                    var catalog = session.Map.theme && session.Map.theme.starwayRanch
                        ? session.Map.theme.starwayRanch.v13Devices : null;
                    if (!catalog)
                    { MissingBindings++; Debug.LogError("Starway Ranch V13 catalog is missing: " + d.id, root); root.SetActive(false); continue; }
                    try
                    {
                        ranchV13.Add(d.id, new RanchDevicesV13PuzzleBinding(d, root, catalog, session));
                    }
                    catch (Exception error)
                    { MissingBindings++; Debug.LogError("Starway Ranch V13 binding failed: " + error, root); root.SetActive(false); }
                    continue;
                }
                var ferry = root.GetComponent<FerryVisualView>();
                if (ferry)
                {
                    ferries[d.id] = ferry; views[d.id] = ferry.clock; ferry.passenger.sprite = player.sprite;
                    ferry.passenger.transform.localScale = player.transform.localScale; ferry.passenger.enabled = false;
                    ferry.rear.sortingOrder = 30; ferry.passenger.sortingOrder = 40; ferry.front.sortingOrder = 41;
                }
                else
                {
                    var view = root.GetComponent<DeviceVisualView>(); if (view) { views[d.id] = view; if (view.visual) view.visual.sortingOrder = d.variant == "plate" || d.variant == "target" || d.variant == "socket" ? 22 : 30;
                        if (theaterArt && theaterArt.v03Animations && d.variant != "plate" && d.variant != "target" && HasV03DeviceArt(d.role))
                            if (!view.ActivateV03(theaterArt.v03Animations, theaterArt.v03ManifestSha256)) MissingBindings++;
                        if (session.Map.theme && session.Map.theme.starwayRanch)
                        {
                            var ranch = session.Map.theme.starwayRanch;
                            if (ranch.v12Animations && HasV12DeviceArt(d.role, d.variant))
                            {
                                if (!view.ActivateV12(ranch.v12Animations, ranch.v12ManifestSha256)) MissingBindings++;
                            }
                            else if (!ranch.v12Animations && HasV12DeviceArt(d.role, d.variant))
                            {
                                MissingBindings++;
                                Debug.LogError("Starway Ranch V12 catalog is missing; refusing legacy device art.", root);
                            }
                        }
                        if (d.role == PrototypeRole.StarSocket && view.visual)
                        {
                            if (d.variant != "socket") view.visual.transform.localScale *= .78f;
                            else if (view.IsV12Active && session.Map.theme && session.Map.theme.starwayRanch)
                            {
                                var ranch = session.Map.theme.starwayRanch;
                                var empty = ranch.Find("devices/star_socket/empty.png");
                                var stone = ranch.Find("devices/star_stone/idle.png");
                                if (empty && stone) seatedStars.Add(d.id, new SeatedStarVisual(view.visual, empty, stone));
                                else { MissingBindings++; Debug.LogError("Star socket composition sprites are missing.", root); }
                            }
                        }
                    }
                    if (d.role == PrototypeRole.Wind && d.variant == "blower" &&
                        session.Map.theme && session.Map.theme.starwayRanch)
                    {
                        var art = session.Map.theme.starwayRanch;
                        if (!art.windRouteMaterial)
                        { MissingBindings++; Debug.LogError("P01 wind material is missing.", root); }
                        else
                        {
                            var child = new GameObject("P01 wind route");
                            child.transform.SetParent(root.transform, false);
                            var range = child.AddComponent<AnimolWindRouteRenderer>();
                            range.ConfigureMaterial(art.windRouteMaterial, "Default", 25);
                            windRoutes.Add(d.id, range);
                            var route = session.WindRouteFor(d.id);
                            if (route != null)
                            {
                                range.SetRoute(route, transform, new Vector2(-.5f, -.5f), 1f);
                                displayedWindRoutes.Add(d.id, route);
                            }
                        }
                    }
                }
            }
            if (session.Rules.themeId == "letter_observatory" && session.Map.theme &&
                session.Map.theme.sunsetSanctumV02Catalog)
            {
                foreach (var trace in session.BeamTraces)
                {
                    var go = new GameObject("V02 light route " + trace.sourceId);
                    go.transform.SetParent(transform, false);
                    var renderer = go.AddComponent<SunsetSanctumV02BeamRenderer>();
                    renderer.Configure(session.Map.theme.sunsetSanctumV02BeamMaterial, "Default", 24);
                    sanctumBeams.Add(trace.sourceId, renderer);
                }
                SyncSanctumBeams();
            }
            else if (session.Rules.beamPrefab)
            { beams = Instantiate(session.Rules.beamPrefab, transform).GetComponent<BeamPathView>(); beams.sortingOrder = 24; }
            Settle();
        }
        static bool HasV03DeviceArt(PrototypeRole role)
        {
            switch (role)
            {
                case PrototypeRole.Shutter:
                case PrototypeRole.MovingStage:
                case PrototypeRole.Conveyor:
                case PrototypeRole.Bookshelf:
                case PrototypeRole.OneWayGate:
                case PrototypeRole.Domino:
                case PrototypeRole.Statue:
                case PrototypeRole.Bounce:
                case PrototypeRole.Brake:
                case PrototypeRole.Dice:
                case PrototypeRole.CowardDoll:
                    return true;
                default:
                    return false;
            }
        }
        static bool HasV12DeviceArt(PrototypeRole role, string variant)
        {
            switch (role)
            {
                case PrototypeRole.StarSocket:
                case PrototypeRole.VineBridge:
                case PrototypeRole.Conveyor:
                case PrototypeRole.OneWayGate:
                case PrototypeRole.Bounce:
                case PrototypeRole.Brake:
                case PrototypeRole.Chick:
                case PrototypeRole.Wind:
                    return true;
                default:
                    return false;
            }
        }
        public void Notify(PrototypeEvent e, string actionId)
        {
            if (ranchV13.TryGetValue(e.actor ?? "", out var ranch))
            {
                if (!ranch.Notify(e, session.States[e.actor])) MissingBindings++;
                return;
            }
            if (coastV01.TryGetValue(e.actor ?? "", out var coast))
            {
                if (!coast.Notify(e, session.States[e.actor], session.Cost, windFeedbackDuration)) MissingBindings++;
                return;
            }
            if (sanctumV02.TryGetValue(e.actor ?? "", out var sanctum))
            {
                if (!sanctum.Notify(e, session.States[e.actor], session.Cost, windFeedbackDuration)) MissingBindings++;
                return;
            }
            if (!views.TryGetValue(e.actor ?? "", out var view)) return;
            var definition = session.Definition(e.actor);
            if (diceVisualMap != null && definition != null && definition.role == PrototypeRole.Dice &&
                definition.variant != "target" && e.eventName == "actual_one_cell_die_roll")
            {
                string error = null;
                EchoTheaterV03DiceOrientation.State before, after;
                string key = null;
                bool valid = TryDiceState(e.diceBefore, out before, out error) &&
                    TryDiceState(e.diceAfter, out after, out error) &&
                    diceVisualMap.TryGetCommittedRollClip(before, after, DiceHeading(e.rollHeading),
                        true, e.teleport, out key, out error);
                var binding = Array.Find(view.states, s => s.name == e.state);
                if (!valid || binding == null || binding.v03ClipKey != key)
                {
                    MissingBindings++;
                    view.FailV03(e.state);
                    Debug.LogError("V03 die roll visual differs from the committed six-face orientation: " + error, this);
                    return;
                }
            }
            pendingIdle.Remove(e.actor);
            if (view.IsV12Active && e.eventName == "valid_action_wind_resolved")
                ShowWindRoute(e);
            if (ferries.TryGetValue(e.actor, out var ferry)) { if (!ferry.Notify(actionId, e.eventName, e.state)) ferry.Present(e.state, true); }
            else if (view.IsV03Active || view.IsV12Active)
            { if (!view.Notify(actionId, e.eventName, e.state)) MissingBindings++; }
            else if (!view.Notify(actionId, e.eventName, e.state) && !view.Present(e.state, true)) MissingBindings++;
            if (PlayingTransition(view)) pendingIdle[e.actor] = Idle(session.Definition(e.actor), session.States[e.actor]);
        }
        static bool TryDiceState(int[] faces, out EchoTheaterV03DiceOrientation.State state, out string error)
        {
            state = default;
            if (faces == null || faces.Length != 6) { error = "Missing six-face committed dice state."; return false; }
            return EchoTheaterV03DiceOrientation.TryCreate(faces[0], faces[1], faces[2], faces[3],
                faces[4], faces[5], out state, out error);
        }
        static EchoTheaterV03DiceOrientation.Heading DiceHeading(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return EchoTheaterV03DiceOrientation.Heading.East;
            if (direction == Vector2Int.up) return EchoTheaterV03DiceOrientation.Heading.North;
            if (direction == Vector2Int.left) return EchoTheaterV03DiceOrientation.Heading.West;
            if (direction == Vector2Int.down) return EchoTheaterV03DiceOrientation.Heading.South;
            return EchoTheaterV03DiceOrientation.Heading.None;
        }
        static bool PlayingTransition(DeviceVisualView view)
        {
            var state = Array.Find(view.states, s => s.name == view.CurrentState);
            return state != null && state.sequence && !state.sequence.loop && view.Elapsed < state.sequence.Duration;
        }
        void ShowWindRoute(PrototypeEvent e)
        {
            if (e.windRoute == null) return; // The existing one-cell field variant is not an emitter.
            if (!windRoutes.TryGetValue(e.actor, out var renderer))
            {
                MissingBindings++; Debug.LogError("P01 wind route renderer is missing: " + e.actor, this); return;
            }
            if (!displayedWindRoutes.TryGetValue(e.actor, out var current) || !ReferenceEquals(current, e.windRoute))
            {
                renderer.SetRoute(e.windRoute, transform, new Vector2(-.5f, -.5f), 1f);
                displayedWindRoutes[e.actor] = e.windRoute;
            }
            renderer.PlayCommittedAction(true, session.Cost, windFeedbackDuration);
        }
        public void SetWindActionProgress01(float progress)
        {
            foreach (var binding in coastV01.Values) binding.ApplyProgress(progress);
            foreach (var renderer in windRoutes.Values)
                if (renderer) renderer.SetActionProgress01(progress);
            foreach (var beam in sanctumBeams.Values)
                if (beam) beam.SetNormalizedProgress(progress);
        }
        static SanctumHeading LightHeading(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return SanctumHeading.East;
            if (direction == Vector2Int.up) return SanctumHeading.North;
            if (direction == Vector2Int.left) return SanctumHeading.West;
            if (direction == Vector2Int.down) return SanctumHeading.South;
            throw new ArgumentException("Authoritative light direction is not cardinal.");
        }
        static SanctumCell LightCell(Vector2Int cell) => new SanctumCell(cell.x, cell.y);

        SanctumLightRoute ConvertAuthoritativeTrace(PuzzleDeviceSession.BeamTrace trace)
        {
            var cells = new List<SanctumLightCell>(trace.cells.Count);
            foreach (var segment in trace.cells)
            {
                var sensor = session.Devices.FirstOrDefault(d => d.role == PrototypeRole.Optics &&
                    d.variant == "sensor" && !session.States[d.id].removed &&
                    session.States[d.id].cell == segment.cell);
                var kind = sensor != null ? SanctumLightCellKind.Receiver :
                    segment.incoming != segment.outgoing ? SanctumLightCellKind.Mirror : SanctumLightCellKind.Open;
                var visit = new SanctumLightVisit(kind, sensor != null ? sensor.id : null, false);
                cells.Add(new SanctumLightCell(LightCell(segment.cell), LightHeading(segment.incoming),
                    LightHeading(segment.outgoing), visit));
            }
            var stop = trace.cells.Count == 0 ? SanctumLightStop.RangeEnd :
                session.Supported(trace.cells[trace.cells.Count - 1].cell +
                    trace.cells[trace.cells.Count - 1].outgoing) ? SanctumLightStop.Opaque : SanctumLightStop.Void;
            return SanctumLightRoute.FromAuthoritativeTrace(LightCell(trace.emitter),
                LightHeading(trace.heading), cells, stop);
        }

        static string BeamSignature(PuzzleDeviceSession.BeamTrace trace)
        {
            var value = trace.emitter + ":" + trace.heading;
            foreach (var cell in trace.cells)
                value += ";" + cell.cell + ":" + cell.incoming + ":" + cell.outgoing;
            return value;
        }

        void SyncSanctumBeams()
        {
            var presentSources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trace in session.BeamTraces)
            {
                presentSources.Add(trace.sourceId);
                SunsetSanctumV02BeamRenderer renderer;
                if (!sanctumBeams.TryGetValue(trace.sourceId, out renderer)) continue;
                string signature = BeamSignature(trace), previous;
                if (sanctumBeamSignatures.TryGetValue(trace.sourceId, out previous) && previous == signature) continue;
                var route = ConvertAuthoritativeTrace(trace);
                renderer.SetRoute(route, transform, new Vector2(-.5f, -.5f), 1f);
                sanctumBeamSignatures[trace.sourceId] = signature;
            }
            foreach (var pair in sanctumBeams)
            {
                if (presentSources.Contains(pair.Key)) continue;
                if (sanctumBeamSignatures.Remove(pair.Key)) pair.Value.HideBeam();
            }
        }

        public void CommitSanctumBeamAction()
        {
            if (sanctumBeams.Count == 0) return;
            SyncSanctumBeams();
            foreach (var pair in sanctumBeams)
                if (sanctumBeamSignatures.ContainsKey(pair.Key))
                    pair.Value.PlayCommittedAction(true, session.Cost, windFeedbackDuration);
        }
        void SettleWindRoutes()
        {
            foreach (var pair in windRoutes)
            {
                var renderer = pair.Value;
                renderer.CompleteAction(true);
                var route = session.WindRouteFor(pair.Key);
                if (route == null) { renderer.HideRoute(); displayedWindRoutes.Remove(pair.Key); }
                else if (!displayedWindRoutes.TryGetValue(pair.Key, out var old) || !ReferenceEquals(old, route))
                {
                    renderer.SetRoute(route, transform, new Vector2(-.5f, -.5f), 1f);
                    displayedWindRoutes[pair.Key] = route;
                }
            }
        }
        void OnDestroy()
        {
            foreach (var binding in ranchV13.Values) binding.Dispose();
            foreach (var binding in coastV01.Values) binding.Dispose();
            foreach (var binding in sanctumV02.Values) binding.Dispose();
            foreach (var beam in sanctumBeams.Values)
                if (beam) { beam.HideBeam(); beam.ResetActionSequence(); }
            foreach (var route in windRoutes.Values)
                if (route) { route.HideRoute(); route.ResetActionSequence(); }
        }
        void Update()
        {
            foreach (var binding in ranchV13.Values) binding.Tick();
            bool coastPaused = applicationPaused || !applicationFocused || Time.timeScale <= 0f;
            foreach (var binding in coastV01.Values) binding.SetPaused(coastPaused);
            foreach (var binding in coastV01.Values) binding.Update();
            foreach (var binding in sanctumV02.Values) binding.Update();
            foreach (var id in pendingIdle.Keys.ToArray())
            {
                var view = views[id];
                if (PlayingTransition(view)) continue;
                if (pendingRemoval.Remove(id)) roots[id].gameObject.SetActive(false);
                else if (ferries.TryGetValue(id, out var ferry)) ferry.Present(pendingIdle[id]);
                else view.Present(pendingIdle[id]);
                pendingIdle.Remove(id);
            }
        }
        void OnApplicationPause(bool paused) { applicationPaused = paused; }
        void OnApplicationFocus(bool focused) { applicationFocused = focused; }
        public void Settle()
        {
            SettleWindRoutes();
            if (sanctumBeams.Count > 0)
            { SyncSanctumBeams(); foreach (var beam in sanctumBeams.Values) beam.CompleteAction(); }
            foreach (var d in session.Devices)
            {
                var s = session.States[d.id];
                if (ranchV13.TryGetValue(d.id, out var ranch))
                { if (!ranch.Settle(s)) MissingBindings++; continue; }
                if (coastV01.TryGetValue(d.id, out var coast))
                { if (!coast.Settle(s, session.Cell, session.Cost, windFeedbackDuration)) MissingBindings++; continue; }
                if (sanctumV02.TryGetValue(d.id, out var sanctum))
                { if (!sanctum.Settle(s)) MissingBindings++; continue; }
                if (!roots.TryGetValue(d.id, out var root) || !views.TryGetValue(d.id, out var view)) continue;
                root.localPosition = (Vector2)s.cell;
                if (s.removed)
                {
                    if (PlayingTransition(view) && (d.role == PrototypeRole.Glass || d.role == PrototypeRole.Bomb))
                    { pendingIdle[d.id] = ""; pendingRemoval.Add(d.id); }
                    else root.gameObject.SetActive(false);
                    continue;
                }
                root.gameObject.SetActive(true);
                if (seatedStars.TryGetValue(d.id, out var seated)) seated.Present(s.active);
                var state = Idle(d, s);
                if (ferries.TryGetValue(d.id, out var ferry)) ferry.passenger.enabled = session.BoatId == d.id;
                if (PlayingTransition(view)) { pendingIdle[d.id] = state; continue; }
                pendingIdle.Remove(d.id);
                if (ferries.TryGetValue(d.id, out ferry)) { if (!ferry.Present(state)) MissingBindings++; }
                else if (!view.Present(state)) MissingBindings++;
            }
            player.enabled = string.IsNullOrEmpty(session.BoatId) || coastV01.Count > 0;
            if (beams) beams.Present(session.Beams.Select(b => new BeamVisualCell { position = b.cell, shape = BeamShape(b) }).ToArray());
        }
        static string BeamShape(PuzzleDeviceSession.BeamCell b)
        {
            if (b.incoming != b.outgoing)
            { var sum = -b.incoming + b.outgoing; return "beam_" + (sum.x < 0 ? (sum.y > 0 ? "nw" : "sw") : (sum.y > 0 ? "ne" : "se")); }
            return b.terminal ? "beam_terminal_" + PuzzleDeviceSession.Dir(b.outgoing) : "beam_" + (b.outgoing.x == 0 ? "vertical" : "horizontal");
        }
        public static string Idle(PrototypeDevice d, PrototypeDeviceState s)
        {
            var dir = PuzzleDeviceSession.Dir(s.direction);
            switch (d.role)
            {
                case PrototypeRole.StarSocket: return d.variant == "socket" ? (s.active ? "socket_filled" : "socket_empty") : "stone";
                case PrototypeRole.VineBridge: return d.variant == "plate" ? (s.active ? "plate_pressed" : "plate_idle") : s.active ? "bridge_idle_" + (d.direction.x == 0 ? "vertical" : "horizontal") : "retracted_" + dir;
                case PrototypeRole.Shutter: return PuzzleDeviceSession.ShutterPrefix(d) + (s.active ? "open" : "closed");
                case PrototypeRole.DepartureSpikes: return s.phase == 2 ? "raised" : s.phase == 1 ? "armed" : "flat";
                case PrototypeRole.CrescentLeap: case PrototypeRole.Conveyor: case PrototypeRole.MovingStage: case PrototypeRole.Boat:
                case PrototypeRole.Chick: case PrototypeRole.Barrel: return "idle_" + dir;
                case PrototypeRole.CowardDoll: return "idle_" + PuzzleDeviceSession.Dir(s.lookDirection);
                case PrototypeRole.Statue: return (s.locked ? "warning_hold_" : "idle_") + dir;
                case PrototypeRole.OneWayGate: return "closed_" + dir;
                case PrototypeRole.Bookshelf: return d.variant == "plate" ? (s.active ? "plate_pressed" : "plate_idle") : new[] { "horizontal", "vertical", "horizontal_reversed", "vertical_reversed" }[s.phase % 4];
                case PrototypeRole.Domino: return (s.active ? "folded_" : "standing_") + dir;
                case PrototypeRole.Bomb: return s.timer > 0 ? "count_" + s.timer : "dormant";
                case PrototypeRole.Portal: return (d.variant == "b" ? "b" : "a") + "_idle";
                case PrototypeRole.Bounce: return "idle_" + PuzzleDeviceSession.Corner(d.orientation);
                case PrototypeRole.Brake: return "idle_" + (d.variant == "vertical" ? "vertical" : "horizontal");
                case PrototypeRole.Wind: return (d.variant == "blower" ? "blower_idle_" : "wind_flow_") + dir;
                case PrototypeRole.Glass: return "intact_" + (string.IsNullOrEmpty(d.variant) ? "square" : d.variant);
                case PrototypeRole.Dice: return d.variant == "target" ? "target_" + d.number + (s.active ? "_on" : "_off") : s.DiceId;
                case PrototypeRole.Magnet: return d.variant == "switch" ? (s.active ? "switch_on" : "switch_off") : (s.active ? "on_" : "off_") + dir;
                case PrototypeRole.Optics:
                    if (d.variant == "mirror") return s.phase == 0 ? "mirror_slash" : "mirror_backslash";
                    if (d.variant == "sensor") return s.active ? "sensor_on" : "sensor_off";
                    if (d.variant == "gate") return "light_gate_horizontal_" + (s.active ? "open" : "closed");
                    return "source_" + dir + "_on";
                default: return d.initialVisual ?? "static";
            }
        }
        sealed class SeatedStarVisual
        {
            readonly SpriteRenderer original;
            readonly SpriteRenderer baseVisual;
            readonly SpriteRenderer stoneVisual;
            public SeatedStarVisual(SpriteRenderer source, Sprite empty, Sprite stone)
            {
                original = source;
                baseVisual = Add("Socket base", empty, source, 0, 1f);
                stoneVisual = Add("Smaller seated stone", stone, source, 1, .56f);
                original.enabled = false;
            }
            static SpriteRenderer Add(string name, Sprite sprite, SpriteRenderer source, int orderOffset, float scale)
            {
                var child = new GameObject(name);
                child.transform.SetParent(source.transform, false);
                child.transform.localScale = Vector3.one * scale;
                var visual = child.AddComponent<SpriteRenderer>();
                visual.sprite = sprite;
                visual.color = source.color;
                visual.sortingLayerID = source.sortingLayerID;
                visual.sortingOrder = source.sortingOrder + orderOffset;
                return visual;
            }
            public void Present(bool active)
            {
                original.enabled = false;
                baseVisual.enabled = true;
                stoneVisual.enabled = active;
            }
        }
    }
}
