using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Deterministic grid authority. Animation cannot commit an action or advance a device timer.
    public sealed class PuzzleDeviceSession : IPuzzleBoardState
    {
        public sealed class BeamCell { public Vector2Int cell, incoming, outgoing; public bool terminal; }
        public PuzzleMap Map { get; }
        public PuzzleScenario Rules => Map.scenario;
        public Vector2Int Cell { get; private set; }
        public Vector2Int Direction { get; private set; } = Vector2Int.right;
        public int Cost { get; private set; }
        public int Collected => collected.Count;
        public bool Won { get; private set; }
        public bool Failed { get; private set; }
        public string Failure { get; private set; }
        public string BoatId { get; private set; }
        public int PortalCuts { get; private set; }
        public IReadOnlyList<PrototypeDevice> Devices => definitions;
        public IReadOnlyDictionary<string, PrototypeDeviceState> States => states;
        public readonly List<BeamCell> Beams = new List<BeamCell>();
        public readonly Dictionary<Vector2Int, Vector2Int> WindCells = new Dictionary<Vector2Int, Vector2Int>();
        readonly Dictionary<Vector2Int, string> windOwners = new Dictionary<Vector2Int, string>();
        readonly List<PrototypeDevice> definitions;
        Dictionary<string, PrototypeDeviceState> states = new Dictionary<string, PrototypeDeviceState>();
        HashSet<Vector2Int> collected = new HashSet<Vector2Int>(), visited = new HashSet<Vector2Int>();
        List<PrototypeEvent> events = new List<PrototypeEvent>();
        readonly HashSet<string> newlyArmed = new HashSet<string>();
        readonly HashSet<string> pushedThisAction = new HashSet<string>();
        int group;
        bool forced, sliding;
        public PuzzleDeviceSession(PuzzleMap map)
        {
            Map = map;
            if (!Rules) throw new ArgumentException("A device scenario is required.");
            var errors = map.ValidateMap();
            if (errors.Count != 0) throw new ArgumentException(string.Join("; ", errors));
            definitions = Rules.devices.Where(d => Map.Contains(d.position)).OrderBy(d => d.id, StringComparer.Ordinal).ToList();
            foreach (var d in definitions) states.Add(d.id, new PrototypeDeviceState { cell = d.position, direction = d.direction, lookDirection = d.direction, active = d.active, phase = d.orientation });
            Cell = map.start; Collect(); RefreshLinks(false); TraceLight(); TraceWind(); Victory(); events.Clear();
        }
        PuzzleDeviceSession(PuzzleDeviceSession source)
        {
            Map = source.Map; definitions = source.definitions; CopyFrom(source);
        }
        public PuzzleDeviceSession Copy() => new PuzzleDeviceSession(this);
        void CopyFrom(PuzzleDeviceSession other)
        {
            Cell = other.Cell; Direction = other.Direction; Cost = other.Cost; Won = other.Won; Failed = other.Failed;
            Failure = other.Failure; BoatId = other.BoatId; PortalCuts = other.PortalCuts;
            states = other.states.ToDictionary(p => p.Key, p => p.Value.Copy());
            collected = new HashSet<Vector2Int>(other.collected); visited = new HashSet<Vector2Int>(other.visited);
            Beams.Clear(); Beams.AddRange(other.Beams); WindCells.Clear(); foreach (var p in other.WindCells) WindCells.Add(p.Key, p.Value);
            windOwners.Clear(); foreach (var p in other.windOwners) windOwners.Add(p.Key, p.Value);
        }
        public bool IsCollected(Vector2Int p) => collected.Contains(p);
        public PrototypeDevice Definition(string id) => definitions.FirstOrDefault(d => d.id == id);
        public IEnumerable<Vector2Int> Footprint(PrototypeDevice d)
        {
            var s = states[d.id];
            if (d.role == PrototypeRole.Bookshelf && d.variant != "plate")
            {
                var axis = s.phase % 2 == 0 ? Vector2Int.right : Vector2Int.up;
                yield return s.cell - axis; yield return s.cell; yield return s.cell + axis;
            }
            else if (d.role == PrototypeRole.VineBridge && d.variant != "plate")
            { foreach (var p in d.cells.Count == 0 ? new List<Vector2Int> { s.cell } : d.cells) yield return p; }
            else yield return s.cell;
        }
        bool Solid(PrototypeDevice d)
        {
            var s = states[d.id]; if (s.removed) return false;
            switch (d.role)
            {
                case PrototypeRole.StarSocket: return d.variant != "socket";
                case PrototypeRole.Bookshelf: return d.variant != "plate";
                case PrototypeRole.Dice: return d.variant != "target";
                case PrototypeRole.Magnet: return d.variant != "switch";
                case PrototypeRole.Optics: return d.variant == "source" || d.variant == "mirror" || (d.variant == "gate" && !s.active);
                case PrototypeRole.Shutter: return !s.active;
                case PrototypeRole.DepartureSpikes: return s.phase == 2;
                case PrototypeRole.Domino: return !s.active;
                case PrototypeRole.Barrel: case PrototypeRole.Bomb: case PrototypeRole.Statue: case PrototypeRole.Glass:
                case PrototypeRole.CowardDoll: case PrototypeRole.Chick: case PrototypeRole.Wall: case PrototypeRole.CrackedWall:
                case PrototypeRole.WoodCrate: case PrototypeRole.MetalCrate: return true;
                case PrototypeRole.Wind: return d.variant == "blower";
                default: return false;
            }
        }
        PrototypeDevice At(Vector2Int p, Func<PrototypeDevice, bool> predicate = null, string except = null) =>
            definitions.FirstOrDefault(d => d.id != except && !states[d.id].removed && Footprint(d).Contains(p) && (predicate == null || predicate(d)));
        bool Occupied(Vector2Int p, string except = null) => Cell == p || At(p, Solid, except) != null;
        public bool Supported(Vector2Int p) => Map.Contains(p) && (Map.Walkable(p) || At(p, d =>
            (d.role == PrototypeRole.VineBridge && d.variant != "plate" && states[d.id].active) || d.role == PrototypeRole.MovingStage) != null);
        bool Free(Vector2Int p, string except = null, bool water = false, bool ignorePlayer = false) =>
            Map.Contains(p) && (Supported(p) || (water && Rules.water.Contains(p))) && (ignorePlayer || Cell != p) && At(p, Solid, except) == null && At(p, d => d.role == PrototypeRole.Boat, except) == null;
        bool GateAllows(Vector2Int from, Vector2Int to, Vector2Int direction) => !definitions.Any(d => d.role == PrototypeRole.OneWayGate && !states[d.id].removed &&
            (states[d.id].cell == from || states[d.id].cell == to) && states[d.id].direction != direction);
        public PrototypeAction Apply(Vector2Int direction)
        {
            if (Won || Failed || !PuzzleSession.IsCardinal(direction)) return new PrototypeAction { reason = "Input unavailable" };
            var next = Copy(); var result = next.Resolve(direction);
            if (result.accepted) CopyFrom(next);
            return result;
        }
        PrototypeAction Resolve(Vector2Int direction)
        {
            Direction = direction; forced = sliding = false; group = 0; events.Clear(); newlyArmed.Clear(); pushedThisAction.Clear();
            var mirror = At(Cell + direction, d => d.role == PrototypeRole.Optics && d.variant == "mirror");
            if (mirror != null && Rules.mirrorBumpRotates)
            {
                var s = states[mirror.id]; s.phase = 1 - s.phase;
                Visual(mirror, s.phase == 0 ? "mirror_backslash_cw" : "mirror_slash_cw", "existing_mirror_rotation_committed");
            }
            else
            {
                if (!StepPlayer(Cell + direction))
                {
                    RejectedCue(Cell + direction);
                    return new PrototypeAction { reason = "Blocked", events = events.Where(e => e.kind == PrototypeEventKind.Visual).ToList() };
                }
                Automatic();
            }
            Cost++; EndTurn(); Victory();
            if (!Won && Rules.costLimit > 0 && Cost >= Rules.costLimit) Fail("Cost exhausted");
            return new PrototypeAction { accepted = true, events = events };
        }
        void Move(string actor, Vector2Int from, Vector2Int to, bool teleport = false)
        { events.Add(new PrototypeEvent { kind = PrototypeEventKind.Move, actor = actor, from = from, to = to, teleport = teleport, group = group++ }); }
        void Visual(PrototypeDevice d, string state, string eventName)
        { events.Add(new PrototypeEvent { kind = PrototypeEventKind.Visual, actor = d.id, state = state, eventName = eventName, group = group }); }
        void DiceRollVisual(PrototypeDevice d, int[] before, int[] after, Vector2Int direction)
        {
            events.Add(new PrototypeEvent { kind = PrototypeEventKind.Visual, actor = d.id,
                state = "roll_t" + before[0] + "_n" + before[2] + "_e" + before[4] + "_" + Dir(direction),
                eventName = "actual_one_cell_die_roll", group = group,
                diceBefore = (int[])before.Clone(), diceAfter = (int[])after.Clone(), rollHeading = direction });
        }
        void Collect() { visited.Add(Cell); if (Map.bubbles.Contains(Cell)) collected.Add(Cell); }
        void RejectedCue(Vector2Int destination)
        {
            var gate = At(destination, d => d.role == PrototypeRole.OneWayGate) ?? At(Cell, d => d.role == PrototypeRole.OneWayGate);
            if (gate != null && !GateAllows(Cell, destination, Direction))
            { Visual(gate, "blocked_" + Dir(Direction), "existing_rejected_gate_contact"); return; }
            if (!string.IsNullOrEmpty(BoatId))
            { Visual(Definition(BoatId), "blocked_" + Dir(Direction), "existing_boat_or_disembark_rejected"); return; }
            var device = At(destination);
            if (device == null || events.Any(e => e.kind == PrototypeEventKind.Visual && e.actor == device.id)) return;
            switch (device.role)
            {
                case PrototypeRole.CrescentLeap: Visual(device, "blocked_" + Dir(Direction), "existing_leap_rejected"); break;
                case PrototypeRole.DepartureSpikes:
                    if (states[device.id].phase == 2) Visual(device, "raised_blocked", "existing_raised_spike_contact_rejected"); break;
                case PrototypeRole.Glass: Visual(device, "blocked_" + (string.IsNullOrEmpty(device.variant) ? "square" : device.variant), "existing_body_glass_contact_rejected"); break;
                case PrototypeRole.Barrel: Visual(device, "blocked_" + Dir(Direction), "existing_barrel_first_cell_blocked"); break;
                case PrototypeRole.Boat: Visual(device, "blocked_" + Dir(Direction), "existing_boat_or_disembark_rejected"); break;
                case PrototypeRole.Portal: Visual(device, (device.variant == "b" ? "b_reject" : "a_reject"), "existing_portal_destination_next_cell_blocked"); break;
            }
        }
        bool Landing(Vector2Int p, Vector2Int direction, bool water = false) => Free(p, null, water) && GateAllows(p - direction, p, direction);
        bool EntryAllowed(Vector2Int p)
        {
            var portal = At(p, d => d.role == PrototypeRole.Portal);
            if (portal != null && (!Free(portal.target, null, false, true) || !Landing(portal.target + Direction, Direction)))
            { Visual(portal, "a_reject", "existing_portal_destination_next_cell_blocked"); return false; }
            var leap = At(p, d => d.role == PrototypeRole.CrescentLeap);
            if (leap == null) return true;
            if (!Landing(p + states[leap.id].direction * 3, states[leap.id].direction)) return false;
            if (!Rules.leapSkipsIntermediate) for (var i = 1; i < 3; i++) if (!Free(p + states[leap.id].direction * i)) return false;
            return true;
        }
        bool StepPlayer(Vector2Int destination, PrototypeDevice committedWind = null)
        {
            if (!Map.Contains(destination) || !GateAllows(Cell, destination, Direction) || !EntryAllowed(destination)) return false;
            if (!string.IsNullOrEmpty(BoatId))
            {
                var boat = Definition(BoatId); var bs = states[BoatId];
                if (Rules.water.Contains(destination))
                {
                    if (!Free(destination, BoatId, true)) return false;
                    Move(BoatId, bs.cell, destination); bs.cell = destination; bs.direction = Direction;
                    Visual(boat, "row_" + Dir(Direction), "existing_boat_one_cell_committed");
                }
                else
                {
                    if (!Free(destination)) return false;
                    Visual(boat, "disembark_" + Dir(Direction), "existing_boat_disembarked"); BoatId = null;
                }
            }
            else
            {
                var boat = At(destination, d => d.role == PrototypeRole.Boat);
                if (boat != null && Rules.water.Contains(destination))
                { BoatId = boat.id; states[boat.id].direction = Direction; Visual(boat, "board_" + Dir(Direction), "existing_boat_boarded"); }
                else
                {
                    if (!Supported(destination)) return false;
                    var body = At(destination, Solid);
                    if (body != null && !Push(body, Direction)) return false;
                    if (At(destination, Solid) != null) return false;
                }
            }
            if (committedWind != null) WindVisual(committedWind, Direction);
            var previous = Cell; Leave(previous); Move("player", previous, destination); Cell = destination; Collect(); Enter(); Follow(previous);
            RefreshLinks(true); TraceWind(); return true;
        }
        void Leave(Vector2Int p)
        {
            var spike = At(p, d => d.role == PrototypeRole.DepartureSpikes);
            if (spike != null && states[spike.id].phase == 1) { states[spike.id].phase = 2; Visual(spike, "rise", "existing_player_left_armed_plate"); }
            var gate = At(p, d => d.role == PrototypeRole.OneWayGate); if (gate != null) Visual(gate, "close_" + Dir(Direction), "allowed_passage_clear");
        }
        void Enter()
        {
            var spike = At(Cell, d => d.role == PrototypeRole.DepartureSpikes);
            if (spike != null && states[spike.id].phase == 0) { states[spike.id].phase = 1; Visual(spike, "arm", "existing_spike_plate_occupied"); }
            var gate = At(Cell, d => d.role == PrototypeRole.OneWayGate); if (gate != null) Visual(gate, "open_" + Dir(Direction), "allowed_passage_begin");
            var toggle = At(Cell, d => d.role == PrototypeRole.Magnet && d.variant == "switch");
            if (toggle != null)
            {
                var s = states[toggle.id]; s.active = Rules.magnetSwitchLatches || !s.active;
                Visual(toggle, s.active ? "switch_on" : "switch_off", s.active ? "existing_magnet_switch_on" : "existing_magnet_switch_off");
            }
        }
        void Automatic()
        {
            var seen = new HashSet<string>(); var arrivedFromPortal = false;
            for (int limit = 0; limit < Map.Width * Map.Height * 8; limit++)
            {
                if (!seen.Add(Cell + ":" + Direction + ":" + forced + ":" + sliding + ":" + arrivedFromPortal)) return;
                var portal = At(Cell, d => d.role == PrototypeRole.Portal);
                if (portal != null && !arrivedFromPortal)
                {
                    if (!EntryAllowed(Cell)) return;
                    Visual(portal, (portal.variant == "b" ? "b" : "a") + "_enter_" + Dir(Direction), "existing_portal_entry_committed");
                    var before = Cell; Leave(before); Cell = portal.target; Move("player", before, Cell, true); PortalCuts++; Collect(); Follow(before);
                    var exit = At(Cell, d => d.role == PrototypeRole.Portal);
                    if (exit != null) Visual(exit, (exit.variant == "b" ? "b" : "a") + "_exit_" + Dir(Direction), "existing_portal_exit_committed");
                    arrivedFromPortal = true;
                    if (!StepPlayer(Cell + Direction)) return;
                    if (!sliding) return;
                    continue;
                }
                arrivedFromPortal = false;
                var leap = At(Cell, d => d.role == PrototypeRole.CrescentLeap);
                if (leap != null)
                {
                    Direction = states[leap.id].direction; var target = Cell + Direction * 3;
                    if (!Landing(target, Direction)) return;
                    Visual(leap, "prepare_" + Dir(Direction), "existing_leap_committed_prepare");
                    Visual(leap, "launch_" + Dir(Direction), "existing_leap_launch");
                    var before = Cell; Leave(before); Cell = target; Move("player", before, Cell); Collect(); Enter(); Follow(before); RefreshLinks(true);
                    Visual(leap, "land_" + Dir(Direction), "existing_leap_landed"); return;
                }
                var brake = At(Cell, d => d.role == PrototypeRole.Brake);
                if (brake != null && forced) { Visual(brake, "brake_" + (brake.variant == "vertical" ? "vertical" : "horizontal"), "forced_motion_stopped"); return; }
                var conveyor = At(Cell, d => d.role == PrototypeRole.Conveyor);
                var wind = WindCells.TryGetValue(Cell, out var windDirection);
                PrototypeDevice windSource = null;
                if (conveyor != null) { Direction = states[conveyor.id].direction; forced = true; Visual(conveyor, "run_" + Dir(Direction), "conveyor_visual_running"); }
                else if (wind)
                {
                    Direction = windDirection; forced = true;
                    windSource = windOwners.TryGetValue(Cell, out var owner) ? Definition(owner) : null;
                }
                else if (At(Cell, d => d.role == PrototypeRole.Ice) != null) { forced = sliding = true; }
                else
                {
                    var bounce = At(Cell, d => d.role == PrototypeRole.Bounce);
                    if (bounce != null && forced)
                    {
                        var reflected = ReflectCorner(Direction, bounce.orientation);
                        if (reflected == Vector2Int.zero) return;
                        Direction = reflected; Visual(bounce, "impact_" + Corner(bounce.orientation), "existing_bounce_resolved");
                    }
                    else if (!sliding) return;
                }
                if (!StepPlayer(Cell + Direction, windSource)) return;
            }
        }
        bool Push(PrototypeDevice d, Vector2Int direction)
        {
            var s = states[d.id];
            if (d.role == PrototypeRole.Domino)
            {
                if (s.direction != direction) return false;
                for (var p = s.cell; ; p += direction)
                {
                    var next = At(p, x => x.role == PrototypeRole.Domino && !states[x.id].active && states[x.id].direction == direction);
                    if (next == null) break;
                    states[next.id].active = true; Visual(next, "fold_" + Dir(direction), "this_domino_piece_fold_committed");
                }
                return true;
            }
            if (d.role != PrototypeRole.StarSocket && d.role != PrototypeRole.Barrel && d.role != PrototypeRole.Bomb && d.role != PrototypeRole.Dice && d.role != PrototypeRole.WoodCrate && d.role != PrototypeRole.MetalCrate) return false;
            if (!Free(s.cell + direction, d.id) || !GateAllows(s.cell, s.cell + direction, direction)) return false;
            var portalEntry = At(s.cell + direction, x => x.role == PrototypeRole.Portal);
            if (portalEntry != null && (!Free(portalEntry.target, d.id, false, true) || !Free(portalEntry.target + direction, d.id))) return false;
            var before = s.cell; s.cell += direction; s.direction = direction; Move(d.id, before, s.cell);
            pushedThisAction.Add(d.id);
            if (d.role == PrototypeRole.Dice)
            { var beforeRoll = (int[])s.dice.Clone(); Roll(s.dice, direction); DiceRollVisual(d, beforeRoll, s.dice, direction); }
            if (d.role == PrototypeRole.StarSocket) Visual(d, "stone_push_" + Dir(direction), "existing_push_visual");
            if (d.role == PrototypeRole.Bomb && s.timer == 0) { s.timer = 3; newlyArmed.Add(d.id); Visual(d, "arm", "existing_bomb_first_successful_push"); }
            if (d.role == PrototypeRole.Barrel) Visual(d, "start_" + Dir(direction), "existing_barrel_push_committed");
            TraceWind(); SettleBody(d, direction, d.role == PrototypeRole.Barrel);
            var socket = At(s.cell, x => x.role == PrototypeRole.StarSocket && x.variant == "socket" && x.number == d.number);
            if (d.role == PrototypeRole.StarSocket && socket != null)
            { s.removed = true; states[socket.id].active = true; Visual(socket, "socket_insert", "matching_star_socket_committed"); }
            return true;
        }
        void SettleBody(PrototypeDevice d, Vector2Int direction, bool rolling)
        {
            var s = states[d.id]; var seen = new HashSet<string>(); bool slidingBody = false;
            for (var i = 0; i < Map.Width * Map.Height * 4; i++)
            {
                if (!seen.Add(s.cell + ":" + direction)) break;
                var portal = At(s.cell, x => x.role == PrototypeRole.Portal);
                if (portal != null && Free(portal.target, d.id, false, true) && Free(portal.target + direction, d.id))
                {
                    var before = s.cell; s.cell = portal.target; Move(d.id, before, s.cell, true);
                    before = s.cell; s.cell += direction; Move(d.id, before, s.cell);
                    if (d.role == PrototypeRole.Dice) { var beforeRoll = (int[])s.dice.Clone(); Roll(s.dice, direction); DiceRollVisual(d, beforeRoll, s.dice, direction); }
                    TraceWind();
                    if (!rolling && !slidingBody) break;
                    continue;
                }
                if (At(s.cell, x => x.role == PrototypeRole.Brake) != null) break;
                var conveyor = At(s.cell, x => x.role == PrototypeRole.Conveyor);
                if (conveyor != null) direction = states[conveyor.id].direction;
                else if (WindCells.TryGetValue(s.cell, out var wind)) { if (d.role == PrototypeRole.MetalCrate) break; direction = wind; }
                else if (At(s.cell, x => x.role == PrototypeRole.Ice) != null) slidingBody = true;
                else if (!rolling && !slidingBody) break;
                if (!Free(s.cell + direction, d.id) || !GateAllows(s.cell, s.cell + direction, direction)) break;
                var old = s.cell; s.cell += direction; Move(d.id, old, s.cell);
                if (d.role == PrototypeRole.Dice) { var beforeRoll = (int[])s.dice.Clone(); Roll(s.dice, direction); DiceRollVisual(d, beforeRoll, s.dice, direction); }
                TraceWind();
            }
            if (d.role == PrototypeRole.Barrel) Visual(d, "stop_" + Dir(direction), "existing_barrel_blocker_reached");
        }
        void Follow(Vector2Int previous)
        {
            foreach (var chick in definitions.Where(d => d.role == PrototypeRole.Chick && !states[d.id].removed))
            {
                var s = states[chick.id]; s.trail.Add(previous);
                if (s.trail.Count <= Mathf.Clamp(chick.number, 1, 8)) continue;
                var target = s.trail[0]; s.trail.RemoveAt(0);
                if ((target - s.cell).sqrMagnitude != 1 || !Free(target, chick.id)) continue;
                var from = s.cell; s.direction = target - from; s.cell = target; Move(chick.id, from, target);
                Visual(chick, "walk_" + Dir(s.direction), "existing_recorded_path_following");
            }
        }
        void RefreshLinks(bool animate)
        {
            foreach (var plate in definitions.Where(d => (d.role == PrototypeRole.VineBridge || d.role == PrototypeRole.Bookshelf) && d.variant == "plate"))
            {
                var s = states[plate.id]; var value = Occupied(s.cell, plate.id); var rising = !s.active && value;
                if (s.active != value && animate) Visual(plate, value ? "plate_press" : "plate_release", plate.role == PrototypeRole.VineBridge ? (value ? "occupancy_became_positive" : "occupancy_became_zero") : (value ? "linked_plate_activated" : "linked_plate_released"));
                s.active = value;
                foreach (var linked in definitions.Where(d => d.group == plate.group && d.id != plate.id && d.variant != "plate"))
                {
                    var ls = states[linked.id];
                    if (linked.role == PrototypeRole.VineBridge)
                    {
                        var enabled = value || (Rules.holdOccupiedBridge && ls.active && Footprint(linked).Any(p => Occupied(p, linked.id)));
                        if (ls.active != enabled && animate) Visual(linked, (enabled ? "bridge_extend_" : "bridge_retract_") + Dir(linked.direction), enabled ? "existing_bridge_enabled" : "existing_bridge_disabled");
                        ls.active = enabled;
                    }
                    if (linked.role == PrototypeRole.Bookshelf && rising)
                    {
                        var phase = ls.phase; ls.phase = (phase + 1) % 4;
                        if (Footprint(linked).Any(p => !Free(p, linked.id))) ls.phase = phase;
                        else if (animate) Visual(linked, "rotate_" + phase * 90 + "_cw", "bookshelf_rotation_committed");
                    }
                }
            }
            foreach (var target in definitions.Where(d => d.role == PrototypeRole.Dice && d.variant == "target"))
            {
                var s = states[target.id]; var die = At(s.cell, d => d.role == PrototypeRole.Dice && d.variant != "target");
                var value = die != null && states[die.id].dice[0] == target.number;
                if (s.active != value && animate) Visual(target, "target_" + target.number + (value ? "_match" : "_release"), value ? "die_target_match_committed" : "die_target_match_lost");
                s.active = value;
            }
        }
        void EndTurn()
        {
            foreach (var d in definitions.Where(d => !states[d.id].removed))
            {
                var s = states[d.id];
                if (d.role == PrototypeRole.Shutter)
                {
                    var occupied = Occupied(s.cell, d.id);
                    if (Rules.shutterPhaseAdvancesWhenOccupied || !occupied) s.phase++;
                    var open = (s.phase % 2 == 0) || occupied;
                    if (open != s.active) Visual(d, ShutterPrefix(d) + (open ? "open" : "close"), open ? "shutter_open_committed" : "shutter_close_committed_only_if_empty");
                    else if (occupied) Visual(d, ShutterPrefix(d) + "blocked_open", "occupied_shutter_remains_open");
                    s.active = open;
                }
                if (d.role == PrototypeRole.MovingStage) AdvanceStage(d);
                if (d.role == PrototypeRole.Magnet && d.variant != "switch") Magnet(d);
                if (d.role == PrototypeRole.Statue) Statue(d);
                if (d.role == PrototypeRole.CowardDoll && (s.cell - Cell).sqrMagnitude == 1)
                {
                    var direction = s.cell - Cell; s.lookDirection = -direction;
                    Visual(d, "startle_" + Dir(s.lookDirection), "player_adjacent_after_valid_action");
                    if (Free(s.cell + direction, d.id)) { var from = s.cell; s.cell += direction; s.direction = direction; Move(d.id, from, s.cell); Visual(d, "retreat_" + Dir(s.lookDirection), "doll_one_step_retreat_committed"); }
                    else Visual(d, "blocked_" + Dir(s.lookDirection), "doll_retreat_cell_blocked");
                }
            }
            TraceWind(); WindPulse();
            foreach (var bomb in definitions.Where(d => d.role == PrototypeRole.Bomb && !states[d.id].removed && states[d.id].timer > 0 && !newlyArmed.Contains(d.id)))
            {
                var s = states[bomb.id]; s.timer--;
                if (s.timer == 0) Explode(bomb);
                else Visual(bomb, s.timer == 2 ? "count_3_to_2" : "count_2_to_1", "existing_player_valid_action_completed");
            }
            RefreshLinks(true); TraceLight(); TraceWind();
            if (!Supported(Cell) && string.IsNullOrEmpty(BoatId)) Fail("Support lost");
        }
        void AdvanceStage(PrototypeDevice d)
        {
            var s = states[d.id]; int index = d.cells.IndexOf(s.cell); if (index < 0) return;
            var step = s.phase == 0 ? 1 : -1; if (index + step < 0 || index + step >= d.cells.Count) { s.phase = 1 - s.phase; step = -step; }
            var target = d.cells[index + step]; var carrying = Cell == s.cell;
            if (!Map.Contains(target) || At(target, Solid, d.id) != null || (Cell == target && !carrying)) { Visual(d, "blocked_" + Dir(s.direction), "existing_stage_blocked"); return; }
            var old = s.cell; s.cell = target; s.direction = target - old; Move(d.id, old, target); Visual(d, "advance_" + Dir(s.direction), "valid_action_stage_move_committed");
            if (carrying) { Leave(Cell); Move("player", Cell, target); var previous = Cell; Cell = target; Collect(); Enter(); Follow(previous); }
        }
        void Magnet(PrototypeDevice d)
        {
            var s = states[d.id]; var enabled = definitions.Any(x => x.role == PrototypeRole.Magnet && x.variant == "switch" && x.group == d.group && states[x.id].active);
            if (s.active != enabled) Visual(d, (enabled ? "activate_" : "deactivate_") + Dir(s.direction), enabled ? "existing_magnet_switch_on" : "existing_magnet_switch_off");
            s.active = enabled; if (!enabled) return;
            for (var p = s.cell + s.direction; Map.Contains(p); p += s.direction)
            {
                if (p == Cell) break;
                var obstacle = At(p, Solid, d.id);
                if (obstacle == null) continue;
                if (obstacle.role != PrototypeRole.MetalCrate) break;
                var metal = states[obstacle.id];
                while (metal.cell - s.direction != s.cell && Free(metal.cell - s.direction, obstacle.id))
                { var old = metal.cell; metal.cell -= s.direction; Move(obstacle.id, old, metal.cell); }
                Visual(obstacle, "crate_pull_" + Dir(-s.direction), "existing_metal_crate_being_pulled");
                Visual(obstacle, "crate_settle_" + Dir(-s.direction), "existing_metal_crate_stopped"); break;
            }
        }
        void Statue(PrototypeDevice d)
        {
            var s = states[d.id];
            if (!s.locked)
            {
                for (var p = s.cell + s.direction; Map.Contains(p); p += s.direction)
                {
                    if (p == Cell) { s.locked = true; s.timer = Mathf.Abs((Cell - s.cell).x) + Mathf.Abs((Cell - s.cell).y); Visual(d, "warning_enter_" + Dir(s.direction), "statue_line_locked"); break; }
                    if (!Supported(p) || At(p, Solid, d.id) != null) break;
                }
                return;
            }
            Visual(d, "charge_enter_" + Dir(s.direction), "statue_next_valid_action_charge_begin");
            for (var i = 0; i < Math.Max(s.timer, d.number); i++)
            {
                var p = s.cell + s.direction; if (!Supported(p)) break;
                var body = At(p, Solid, d.id); bool shattered = false;
                if (body != null)
                {
                    if (body.role != PrototypeRole.Glass && body.role != PrototypeRole.CrackedWall) break;
                    states[body.id].removed = true; shattered = true; BreakVisual(body, s.direction);
                }
                var old = s.cell; s.cell = p; Move(d.id, old, p);
                if (Cell == p) { Fail("Statue contact"); break; }
                if (shattered && Rules.statueStopsAfterBreakingWall) break;
            }
            s.locked = false; Visual(d, "recover_" + Dir(s.direction), "statue_recovery");
        }
        void BreakVisual(PrototypeDevice d, Vector2Int direction)
        { if (d.role == PrototypeRole.Glass) Visual(d, "break_" + (string.IsNullOrEmpty(d.variant) ? "square" : d.variant) + "_" + Dir(direction), "existing_glass_destroyed_by_bomb_or_statue"); }
        void Explode(PrototypeDevice bomb)
        {
            var s = states[bomb.id]; s.removed = true;
            var cells = bomb.blastOffsets.Select(p => p + s.cell).Where(Map.Contains).Distinct().ToList();
            Visual(bomb, "explode", "existing_third_valid_action_resolved_bomb_explosion");
            events.Add(new PrototypeEvent { kind = PrototypeEventKind.Blast, actor = bomb.id, cells = cells, group = group });
            foreach (var d in definitions.Where(d => !states[d.id].removed && Footprint(d).Any(cells.Contains)))
                if (d.role == PrototypeRole.Glass || d.role == PrototypeRole.CrackedWall || (Rules.bombDestroysOrdinaryObjects && Solid(d)))
                { states[d.id].removed = true; BreakVisual(d, Direction); }
            if (Rules.bombHurtsPlayer && cells.Contains(Cell)) Fail("Bomb footprint contact");
        }
        public void TraceLight()
        {
            Beams.Clear(); var lit = new HashSet<string>();
            foreach (var source in definitions.Where(d => d.role == PrototypeRole.Optics && d.variant == "source" && !states[d.id].removed))
            {
                var start = Beams.Count; var direction = states[source.id].direction; var p = states[source.id].cell + direction;
                var seen = new HashSet<string>();
                while (Supported(p) && seen.Add(p + ":" + direction))
                {
                    if (p == Cell) break;
                    var mirror = At(p, d => d.role == PrototypeRole.Optics && d.variant == "mirror");
                    var blocker = At(p, d => Solid(d) && d.role != PrototypeRole.Glass && d != mirror);
                    if (blocker != null) break;
                    var sensor = At(p, d => d.role == PrototypeRole.Optics && d.variant == "sensor"); if (sensor != null) lit.Add(sensor.id);
                    var outgoing = mirror == null ? direction : ReflectMirror(direction, states[mirror.id].phase == 0);
                    Beams.Add(new BeamCell { cell = p, incoming = direction, outgoing = outgoing }); direction = outgoing; p += direction;
                }
                if (Beams.Count > start) Beams[Beams.Count - 1].terminal = true;
            }
            foreach (var sensor in definitions.Where(d => d.role == PrototypeRole.Optics && d.variant == "sensor"))
            {
                var s = states[sensor.id]; var value = lit.Contains(sensor.id);
                if (s.active != value) Visual(sensor, value ? "sensor_activate" : "sensor_deactivate", value ? "existing_sensor_light_received" : "existing_sensor_light_lost");
                s.active = value;
            }
            foreach (var gate in definitions.Where(d => d.role == PrototypeRole.Optics && d.variant == "gate"))
            {
                var s = states[gate.id]; var value = definitions.Any(d => d.role == PrototypeRole.Optics && d.variant == "sensor" && d.group == gate.group && states[d.id].active);
                if (s.active != value) Visual(gate, "light_gate_" + (gate.variant == "vertical" ? "vertical" : "horizontal") + (value ? "_open" : "_close"), value ? "existing_linked_light_gate_opened" : "existing_linked_light_gate_closed");
                s.active = value;
            }
        }
        void TraceWind()
        {
            WindCells.Clear();
            windOwners.Clear();
            foreach (var d in definitions.Where(d => d.role == PrototypeRole.Wind && !states[d.id].removed))
            {
                var s = states[d.id]; if (d.variant != "blower") { WindCells[s.cell] = s.direction; windOwners[s.cell] = d.id; continue; }
                for (var p = s.cell + s.direction; Supported(p); p += s.direction)
                {
                    var body = At(p, Solid, d.id);
                    if (body != null && !WindMovable(body)) break;
                    WindCells[p] = s.direction;
                    windOwners[p] = d.id;
                    if (body != null) break;
                }
            }
        }
        static bool WindMovable(PrototypeDevice d) => d.role == PrototypeRole.StarSocket && d.variant != "socket" ||
            d.role == PrototypeRole.WoodCrate || d.role == PrototypeRole.Barrel || d.role == PrototypeRole.Bomb ||
            d.role == PrototypeRole.Dice && d.variant != "target";
        void WindVisual(PrototypeDevice wind, Vector2Int direction)
        {
            events.Add(new PrototypeEvent { kind = PrototypeEventKind.Visual, actor = wind.id,
                state = "wind_push_" + Dir(direction), eventName = "valid_action_wind_resolved",
                group = group, cells = WindCells.Where(x => x.Value == direction).Select(x => x.Key).ToList() });
        }
        void WindPulse()
        {
            foreach (var d in definitions.Where(d => !states[d.id].removed && WindMovable(d) && !pushedThisAction.Contains(d.id)))
            {
                var s = states[d.id];
                if (!WindCells.TryGetValue(s.cell, out var direction) || !Free(s.cell + direction, d.id)) continue;
                var wind = windOwners.TryGetValue(s.cell, out var owner) ? Definition(owner) : null;
                if (wind != null) WindVisual(wind, direction); // Snapshot the trace before movement.
                var old = s.cell; s.cell += direction; s.direction = direction; Move(d.id, old, s.cell);
                pushedThisAction.Add(d.id);
                if (d.role == PrototypeRole.Dice)
                { var beforeRoll = (int[])s.dice.Clone(); Roll(s.dice, direction); DiceRollVisual(d, beforeRoll, s.dice, direction); }
                if (d.role == PrototypeRole.Bomb && s.timer == 0)
                { s.timer = 3; newlyArmed.Add(d.id); Visual(d, "arm", "existing_bomb_first_successful_push"); }
                TraceWind(); SettleBody(d, direction, d.role == PrototypeRole.Barrel);
            }
        }
        void Victory()
        {
            Won = !Failed && Cell == Map.exit && Collected >= Map.targetBubbles && Rules.goals.All(g =>
            {
                if (g.kind == PrototypeGoalKind.Visited) return visited.Contains(g.cell);
                var s = states[g.deviceId];
                switch (g.kind) { case PrototypeGoalKind.Active: return s.active; case PrototypeGoalKind.Removed: return s.removed;
                    case PrototypeGoalKind.Position: return s.cell == g.cell; case PrototypeGoalKind.DiceTop: return s.dice[0] == g.number; default: return false; }
            });
            if (Won) events.Add(new PrototypeEvent { kind = PrototypeEventKind.Won, actor = "player", group = group });
        }
        void Fail(string reason) { Failed = true; Failure = reason; events.Add(new PrototypeEvent { kind = PrototypeEventKind.Failed, actor = "player", state = reason, group = group }); }
        public static string Dir(Vector2Int direction) => direction == Vector2Int.up ? "n" : direction == Vector2Int.right ? "e" : direction == Vector2Int.down ? "s" : "w";
        public static string Corner(int value) => new[] { "nw", "ne", "se", "sw" }[Math.Abs(value) % 4];
        public static string ShutterPrefix(PrototypeDevice d) => (d.variant == "b" ? "b" : "a") + "_" + (d.direction.x == 0 ? "vertical_" : "horizontal_");
        public static Vector2Int ReflectMirror(Vector2Int direction, bool slash) => slash ? new Vector2Int(direction.y, direction.x) : new Vector2Int(-direction.y, -direction.x);
        public static Vector2Int ReflectCorner(Vector2Int d, int corner)
        {
            switch (corner % 4) { case 0: return d == Vector2Int.up ? Vector2Int.right : d == Vector2Int.left ? Vector2Int.down : Vector2Int.zero;
                case 1: return d == Vector2Int.right ? Vector2Int.down : d == Vector2Int.up ? Vector2Int.left : Vector2Int.zero;
                case 2: return d == Vector2Int.right ? Vector2Int.up : d == Vector2Int.down ? Vector2Int.left : Vector2Int.zero;
                default: return d == Vector2Int.left ? Vector2Int.up : d == Vector2Int.down ? Vector2Int.right : Vector2Int.zero; }
        }
        public static void Roll(int[] a, Vector2Int d)
        {
            var old = (int[])a.Clone();
            if (d == Vector2Int.right) { a[0] = old[5]; a[1] = old[4]; a[4] = old[0]; a[5] = old[1]; }
            if (d == Vector2Int.left) { a[0] = old[4]; a[1] = old[5]; a[4] = old[1]; a[5] = old[0]; }
            if (d == Vector2Int.up) { a[0] = old[3]; a[1] = old[2]; a[2] = old[0]; a[3] = old[1]; }
            if (d == Vector2Int.down) { a[0] = old[2]; a[1] = old[3]; a[2] = old[1]; a[3] = old[0]; }
        }
    }
}
