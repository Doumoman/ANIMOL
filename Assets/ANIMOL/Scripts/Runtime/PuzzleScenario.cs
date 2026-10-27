using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ANIMOL.Art.DevicePatch01;
using ANIMOL.Gameplay.RanchDevicesR2;

namespace ANIMOL.Puzzle
{
    // IDs match supplied device packs. Extra roles are board components, not new themed devices.
    public enum PrototypeRole
    {
        StarSocket = 1, VineBridge = 2, CrescentLeap = 3, Shutter = 4, DepartureSpikes = 5,
        MovingStage = 6, Conveyor = 7, Bookshelf = 8, OneWayGate = 9, Optics = 10,
        Boat = 11, Barrel = 12, Domino = 13, Magnet = 14, Bomb = 15, Portal = 16,
        Statue = 17, Bounce = 18, Brake = 19, Wind = 20, Glass = 21, Dice = 22,
        CowardDoll = 23, Chick = 24, Ice = 25, Wall = 100, CrackedWall = 101, WoodCrate = 102, MetalCrate = 103
    }
    public enum StarSocketReward { None, RevealBridge, RevealBubble }
    // Placed ranch families have instance IDs, not new official device role numbers.
    [Serializable] public sealed class RanchV13ArtPlacement
    {
        public string family;
        public string instanceId;
        public Vector2Int position, direction = Vector2Int.right;
        // Explicit B coordinate for newly authored pairs. Legacy R2 maps derive B from direction.
        public bool hasPondB;
        public Vector2Int pondB;
        // Serialized V13 field retained for existing maps. True means A is wet.
        public bool rightState;
        public Vector2Int PondB => hasPondB ? pondB : position + direction;
        public Vector2Int PondDirection => PondB - position;
        public string Id => string.IsNullOrEmpty(instanceId) ? family + "@" + position.x + "," + position.y : instanceId;
        public RanchPondPairR2 PondPair(bool wetA) => new RanchPondPairR2 { instanceId = Id,
            a = new RanchCellR2(position.x, position.y),
            b = new RanchCellR2(PondB.x, PondB.y), wetA = wetA };
        public IEnumerable<Vector2Int> Footprint()
        {
            yield return position;
            if (family == "sluice") yield return PondB;
        }
        public string ClipKey
        {
            get
            {
                if (family == "sluice") return RanchPondPresentationR3.TryCue(PondPair(rightState), out var cue, out _)
                    ? cue.startKey : null;
                if (family == "sheep") return "sheep/" + (direction == Vector2Int.up ? "north" :
                    direction == Vector2Int.down ? "south" : direction == Vector2Int.left ? "west" : "east") + "/idle";
                return family == "tall_grass" ? "tall_grass/idle" : null;
            }
        }
    }
    [Serializable] public sealed class PrototypeDevice
    {
        public string id, variant, group, artTheme;
        public GameObject visualPrefab;
        public string initialVisual;
        public PrototypeRole role;
        public Vector2Int position, direction = Vector2Int.right, target;
        public int orientation, number = 1;
        public bool active;
        public StarSocketReward socketReward;
        public string socketBridgeId;
        public Vector2Int socketBubbleCell;
        public List<Vector2Int> cells = new List<Vector2Int>();
        public List<Vector2Int> blastOffsets = new List<Vector2Int>();
    }
    public enum PrototypeGoalKind { Active, Removed, Position, DiceTop, Visited }
    [Serializable] public sealed class PrototypeGoal
    {
        public string deviceId;
        public PrototypeGoalKind kind;
        public Vector2Int cell;
        public int number;
    }
    [CreateAssetMenu(menuName = "ANIMOL/Puzzle Device Scenario")]
    public sealed class PuzzleScenario : ScriptableObject
    {
        public string title, themeId;
        [Tooltip("Review/verification route, never used by gameplay.")] public string verificationRoute;
        public GameObject beamPrefab;
        public List<PrototypeDevice> devices = new List<PrototypeDevice>();
        public List<RanchV13ArtPlacement> ranchV13Art = new List<RanchV13ArtPlacement>();
        public List<Vector2Int> water = new List<Vector2Int>();
        public List<PrototypeGoal> goals = new List<PrototypeGoal>();
        [Tooltip("0 = unlimited. Valid actions cost 1, invalid input and auto movement cost 0.")] public int costLimit;
        [Tooltip("Prototype policy: hold occupied bridge spans; close after they clear.")] public bool holdOccupiedBridge = true;
        [Tooltip("Prototype policy: all shutters advance phase; occupied shutter stays open.")] public bool shutterPhaseAdvancesWhenOccupied = true;
        [Tooltip("Prototype policy: reject blocked landing before entering leap pad.")] public bool leapSkipsIntermediate = true;
        [Tooltip("Prototype input: swipe into an adjacent mirror rotates it without moving player.")] public bool mirrorBumpRotates = true;
        [Tooltip("Prototype policy: switches latch on first entry.")] public bool magnetSwitchLatches = true;
        [Tooltip("Explicit prototype bomb damage; pack leaves this undecided.")] public bool bombHurtsPlayer = true;
        public bool bombDestroysOrdinaryObjects;
        [Tooltip("Prototype policy: statue stops in the cell of a shattered wall.")] public bool statueStopsAfterBreakingWall = true;
        public List<string> Validate(PuzzleMap map)
        {
            var errors = new List<string>();
            var ranchFootprints = new HashSet<Vector2Int>();
            foreach (var art in ranchV13Art)
            {
                if (art == null || (art.family != "sheep" && art.family != "sluice" && art.family != "tall_grass"))
                { errors.Add("Unknown Ranch V13 art placement."); continue; }
                if (themeId != "starway" || !map.theme || !map.theme.starwayRanch ||
                    !map.theme.starwayRanch.v13Devices)
                    errors.Add("Ranch V13 art requires the Starway theme and catalog.");
                if (art.family == "sheep" && !PuzzleSession.IsCardinal(art.direction))
                    errors.Add("Sheep art direction must be cardinal: " + art.position);
                if (art.family == "sluice" && !PuzzleSession.IsCardinal(art.PondDirection))
                    errors.Add("Pond pair cells must be cardinal neighbors: " + art.position);
                if (art.family == "sheep" && art.position == map.start ||
                    art.family == "tall_grass" && art.position == map.start ||
                    art.family == "sluice" && (art.rightState ? art.position : art.PondB) == map.start)
                    errors.Add("Player cannot start on a sheep, tall grass or wet pond cell.");
                foreach (var p in art.Footprint())
                {
                    if (!map.Contains(p) || !map.Walkable(p))
                        errors.Add("Ranch V13 art needs walkable terrain: " + p);
                    if (!ranchFootprints.Add(p)) errors.Add("Ranch V13 art footprints overlap: " + p);
                    if (devices.Any(d => d.position == p)) errors.Add("Ranch V13 art overlaps a gameplay device: " + p);
                }
            }
            if (ranchV13Art.Where(a => a != null).GroupBy(a => a.Id).Any(g => g.Count() > 1))
                errors.Add("Ranch placement instance IDs must be unique.");
            if (devices.Any(d => string.IsNullOrEmpty(d.id))) errors.Add("Every prototype device needs an ID.");
            if (devices.GroupBy(d => d.id).Any(g => g.Count() > 1)) errors.Add("Prototype device IDs must be unique.");
            foreach (var d in devices.Where(d => map.Contains(d.position)))
            {
                if (!PuzzleSession.IsCardinal(d.direction)) errors.Add("Invalid direction: " + d.id);
                if (d.role == PrototypeRole.Bomb && d.blastOffsets.Count == 0) errors.Add("Author explicit bomb footprint offsets: " + d.id);
                if (d.role == PrototypeRole.Portal && !map.Contains(d.target)) errors.Add("Portal destination outside map: " + d.id);
                if (d.role == PrototypeRole.MovingStage && d.cells.Count < 2) errors.Add("Moving stage needs a rail: " + d.id);
                if (d.role == PrototypeRole.Chick && d.target != Vector2Int.zero && !map.Walkable(d.target))
                    errors.Add("Chick nest needs walkable terrain: " + d.id);
                if (d.role == PrototypeRole.StarSocket && d.variant == "socket")
                {
                    if (d.socketReward == StarSocketReward.RevealBridge && !devices.Any(target =>
                        target.id == d.socketBridgeId && target.role == PrototypeRole.VineBridge &&
                        target.variant != "plate" && map.Contains(target.position)))
                        errors.Add("Socket bridge target is missing or outside map: " + d.id);
                    if (d.socketReward == StarSocketReward.RevealBubble && !map.Walkable(d.socketBubbleCell))
                        errors.Add("Socket bubble needs walkable terrain: " + d.id);
                    if (d.socketReward == StarSocketReward.RevealBubble && map.bubbles.Contains(d.socketBubbleCell))
                        errors.Add("Socket bubble is already visible: " + d.id);
                }
            }
            if (goals.Any(g => g.kind != PrototypeGoalKind.Visited && !devices.Any(d => d.id == g.deviceId))) errors.Add("Goal references missing device.");
            if (goals.Any(g => g.kind != PrototypeGoalKind.Visited && devices.Any(d => d.id == g.deviceId && !map.Contains(d.position)))) errors.Add("A required goal device is outside map bounds; expand or reposition it before playing.");
            return errors;
        }
    }
    public interface IPuzzleBoardState
    {
        int Collected { get; }
        bool IsCollected(Vector2Int p);
        bool IsBubbleVisible(Vector2Int p);
    }
    [Serializable] public sealed class PrototypeDeviceState
    {
        public Vector2Int cell, direction;
        // Presentation heading for the timid puppet; movement still uses direction.
        public Vector2Int lookDirection;
        public bool active, removed, locked;
        public int phase, timer;
        public int[] dice = { 1, 6, 5, 2, 3, 4 }; // top,bottom,north,south,east,west
        public List<Vector2Int> trail = new List<Vector2Int>();
        public PrototypeDeviceState Copy() => new PrototypeDeviceState { cell = cell, direction = direction, lookDirection = lookDirection, active = active, removed = removed, locked = locked, phase = phase, timer = timer, dice = (int[])dice.Clone(), trail = new List<Vector2Int>(trail) };
        public string DiceId => "t" + dice[0] + "_n" + dice[2] + "_e" + dice[4];
    }
    public enum PrototypeEventKind { Move, Visual, Beam, Blast, Failed, Won }
    [Serializable] public sealed class PrototypeEvent
    {
        public PrototypeEventKind kind;
        public string actor, state, eventName;
        public Vector2Int from, to;
        // Visual audit data for a committed die roll; it does not alter authority.
        public int[] diceBefore, diceAfter;
        public Vector2Int rollHeading;
        public bool teleport;
        public int group;
        public List<Vector2Int> cells;
        // Committed chick return presentation data; the session remains the sole authority.
        public string nestId;
        public Vector2Int arrivalDirection;
        [NonSerialized] public WindRoute windRoute;
        [NonSerialized] public RanchPondPairR2 pondBefore, pondAfter;
        [NonSerialized] public RanchRuleResultR2 ranchResult;
    }
    public sealed class PrototypeAction
    {
        public bool accepted;
        public string reason;
        public List<PrototypeEvent> events = new List<PrototypeEvent>();
    }
}
