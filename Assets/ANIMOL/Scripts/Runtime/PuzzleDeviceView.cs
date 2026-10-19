using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Presentation;
using UnityEngine;
namespace ANIMOL.Puzzle
{
    public sealed class PuzzleDeviceView : MonoBehaviour
    {
        readonly Dictionary<string, DeviceVisualView> views = new Dictionary<string, DeviceVisualView>();
        readonly Dictionary<string, Transform> roots = new Dictionary<string, Transform>();
        readonly Dictionary<string, FerryVisualView> ferries = new Dictionary<string, FerryVisualView>();
        readonly Dictionary<string, string> pendingIdle = new Dictionary<string, string>();
        readonly HashSet<string> pendingRemoval = new HashSet<string>();
        PuzzleDeviceSession session;
        SpriteRenderer player;
        BeamPathView beams;
        EchoTheaterV03DiceVisualMap diceVisualMap;
        public int MissingBindings { get; private set; }
        public Transform Actor(string id) => roots.TryGetValue(id, out var root) ? root : null;
        public void Bind(PuzzleDeviceSession authority, SpriteRenderer playerRenderer)
        {
            session = authority; player = playerRenderer;
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
                if (!d.visualPrefab) { MissingBindings++; continue; }
                var root = Instantiate(d.visualPrefab, transform); root.name = d.id; root.transform.localPosition = (Vector2)session.States[d.id].cell; roots[d.id] = root.transform;
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
                            root.AddComponent<StarwayRanchDeviceArt>().Bind(session.Map.theme.starwayRanch, view, d);
                    }
                }
            }
            if (session.Rules.beamPrefab) { beams = Instantiate(session.Rules.beamPrefab, transform).GetComponent<BeamPathView>(); beams.sortingOrder = 24; }
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
        public void Notify(PrototypeEvent e, string actionId)
        {
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
            if (ferries.TryGetValue(e.actor, out var ferry)) { if (!ferry.Notify(actionId, e.eventName, e.state)) ferry.Present(e.state, true); }
            else if (view.IsV03Active)
            { if (!view.Notify(actionId, e.eventName, e.state)) MissingBindings++; }
            else if (!view.Notify(actionId, e.eventName, e.state) && !view.Present(e.state, true)) MissingBindings++;
            if (roots.TryGetValue(e.actor, out var actor)) actor.GetComponent<StarwayRanchDeviceArt>()?.ApplyNow();
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
        void Update()
        {
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
        public void Settle()
        {
            foreach (var d in session.Devices)
            {
                var s = session.States[d.id];
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
                var state = Idle(d, s);
                if (ferries.TryGetValue(d.id, out var ferry)) ferry.passenger.enabled = session.BoatId == d.id;
                if (PlayingTransition(view)) { pendingIdle[d.id] = state; continue; }
                pendingIdle.Remove(d.id);
                if (ferries.TryGetValue(d.id, out ferry)) { if (!ferry.Present(state)) MissingBindings++; }
                else if (!view.Present(state)) MissingBindings++;
                root.GetComponent<StarwayRanchDeviceArt>()?.ApplyNow();
            }
            player.enabled = string.IsNullOrEmpty(session.BoatId);
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
                case PrototypeRole.Wind: return (d.variant == "blower" ? "blower_run_" : "wind_flow_") + dir;
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
    }
}
