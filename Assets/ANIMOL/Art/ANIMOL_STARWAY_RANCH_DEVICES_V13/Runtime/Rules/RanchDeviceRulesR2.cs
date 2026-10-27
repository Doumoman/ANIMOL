using System;
using System.Collections.Generic;

namespace ANIMOL.Gameplay.RanchDevicesR2
{
    // Portable reference kernel. No Unity rendering, animation completion, input polling or timers.
    [Serializable]
    public struct RanchCellR2 : IEquatable<RanchCellR2>
    {
        public int x, y;
        public RanchCellR2(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(RanchCellR2 other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is RanchCellR2 && Equals((RanchCellR2)obj); }
        public override int GetHashCode() { unchecked { return (x * 397) ^ y; } }
        public static bool operator ==(RanchCellR2 a, RanchCellR2 b) { return a.Equals(b); }
        public static bool operator !=(RanchCellR2 a, RanchCellR2 b) { return !a.Equals(b); }
        public override string ToString() { return x + "," + y; }
    }

    public enum RanchGroundR2 { Floor, Wall, TallGrass }
    public enum RanchRuleCodeR2 { Applied, NotApplicable, Blocked, FloodPolicyRequired, InvalidSnapshot, InvalidDirection, InvalidWind }
    public enum RanchRuleEventR2 { None, PondCrossed, SheepPushed, SheepAteAndRemoved, SheepWindMoved, SheepWindAteAndRemoved }

    [Serializable]
    public sealed class RanchPondPairR2
    {
        public string instanceId;
        public RanchCellR2 a, b;
        public bool wetA;
        public RanchCellR2 Wet { get { return wetA ? a : b; } }
        public RanchCellR2 Dry { get { return wetA ? b : a; } }
        public RanchPondPairR2 Copy() { return new RanchPondPairR2 { instanceId = instanceId, a = a, b = b, wetA = wetA }; }
    }

    // Adapt the host's authoritative state to this DTO. Do not create a second live game board.
    // Clone-on-success is a small-board reference transaction; a production host may apply the same delta in its own transaction.
    public sealed class RanchRuleBoardR2
    {
        public RanchCellR2 player;
        public int costSpent, deviceTurns;
        public readonly Dictionary<RanchCellR2, RanchGroundR2> ground = new Dictionary<RanchCellR2, RanchGroundR2>();
        public readonly Dictionary<string, RanchCellR2> sheep = new Dictionary<string, RanchCellR2>(StringComparer.Ordinal);
        public readonly HashSet<RanchCellR2> solidOccupants = new HashSet<RanchCellR2>();
        // The host explicitly lists unresolved/forbidden flooding occupants here. The crossing player itself is excluded.
        public readonly HashSet<RanchCellR2> floodPolicyHolds = new HashSet<RanchCellR2>();
        public readonly List<RanchPondPairR2> ponds = new List<RanchPondPairR2>();
        public bool IsWet(RanchCellR2 cell)
        {
            foreach (var p in ponds) if (p != null && p.Wet == cell) return true;
            return false;
        }
        public RanchRuleBoardR2 Copy()
        {
            var b = new RanchRuleBoardR2 { player = player, costSpent = costSpent, deviceTurns = deviceTurns };
            foreach (var p in ground) b.ground.Add(p.Key, p.Value);
            foreach (var p in sheep) b.sheep.Add(p.Key, p.Value);
            b.solidOccupants.UnionWith(solidOccupants); b.floodPolicyHolds.UnionWith(floodPolicyHolds);
            foreach (var p in ponds) b.ponds.Add(p.Copy());
            return b;
        }
    }

    public sealed class RanchRuleResultR2
    {
        public RanchRuleCodeR2 code;
        public RanchRuleEventR2 eventKind;
        public string diagnostic, instanceId;
        public bool changed, automatic, ateGrass, removedSheep;
        public int costDelta, turnDelta;
        public RanchCellR2[] actualRoute = new RanchCellR2[0];
        // R3: water always travels from PRE-ACTION wet cell to PRE-ACTION dry cell.
        public RanchCellR2[] waterRoute = new RanchCellR2[0];
    }

    public static class RanchDeviceRulesR2
    {
        static bool Direction(RanchCellR2 d) { return (d.x == 0 && (d.y == 1 || d.y == -1)) || (d.y == 0 && (d.x == 1 || d.x == -1)); }
        static bool Step(RanchCellR2 p, RanchCellR2 d, out RanchCellR2 result)
        {
            long x = (long)p.x + d.x, y = (long)p.y + d.y;
            result = p;
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) return false;
            result = new RanchCellR2((int)x, (int)y); return true;
        }
        static bool HasSheep(RanchRuleBoardR2 b, RanchCellR2 p, string exceptId = null)
        { foreach (var s in b.sheep) if (s.Key != exceptId && s.Value == p) return true; return false; }
        static bool CanSheepEnter(RanchRuleBoardR2 b, RanchCellR2 p, string movingId)
        {
            RanchGroundR2 g;
            return b.ground.TryGetValue(p, out g) && g != RanchGroundR2.Wall && !b.IsWet(p)
                && p != b.player && !b.solidOccupants.Contains(p) && !HasSheep(b, p, movingId);
        }
        public static bool Validate(RanchRuleBoardR2 b, out string error)
        {
            error = null;
            if (b == null) { error = "Board required."; return false; }
            RanchGroundR2 g;
            if (!b.ground.TryGetValue(b.player, out g) || g != RanchGroundR2.Floor || b.solidOccupants.Contains(b.player)
                || b.costSpent < 0 || b.deviceTurns < 0)
            { error = "Player must occupy a free floor cell; ledger cannot be negative."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal); var paired = new HashSet<RanchCellR2>();
            foreach (var p in b.ponds)
            {
                if (p == null || string.IsNullOrEmpty(p.instanceId) || !ids.Add(p.instanceId)
                    || Math.Abs((long)p.a.x - p.b.x) + Math.Abs((long)p.a.y - p.b.y) != 1
                    || !paired.Add(p.a) || !paired.Add(p.b)
                    || !b.ground.TryGetValue(p.a, out g) || g != RanchGroundR2.Floor
                    || !b.ground.TryGetValue(p.b, out g) || g != RanchGroundR2.Floor)
                { error = "Pond pairs need unique adjacent nonoverlapping floor cells and instance IDs."; return false; }
            }
            if (b.IsWet(b.player)) { error = "Player cannot begin standing in a wet pond cell."; return false; }
            var occupied = new HashSet<RanchCellR2>();
            foreach (var s in b.sheep)
            {
                if (string.IsNullOrEmpty(s.Key) || !occupied.Add(s.Value) || s.Value == b.player
                    || !b.ground.TryGetValue(s.Value, out g) || g != RanchGroundR2.Floor || b.IsWet(s.Value)
                    || b.solidOccupants.Contains(s.Value))
                { error = "Invalid sheep placement."; return false; }
            }
            return true;
        }
        static bool Reject(RanchRuleBoardR2 source, RanchRuleCodeR2 code, string message, out RanchRuleBoardR2 next, out RanchRuleResultR2 result, bool automatic = false)
        {
            next = source; result = new RanchRuleResultR2 { code = code, diagnostic = message, automatic = automatic };
            return false;
        }
        static bool Preflight(RanchRuleBoardR2 b, RanchCellR2 direction, bool automatic, out RanchRuleBoardR2 next, out RanchRuleResultR2 result)
        {
            string error;
            if (!Validate(b, out error)) return Reject(b, RanchRuleCodeR2.InvalidSnapshot, error, out next, out result, automatic);
            if (!Direction(direction)) return Reject(b, RanchRuleCodeR2.InvalidDirection, "Use one cardinal cell.", out next, out result, automatic);
            if (!automatic && (b.costSpent == int.MaxValue || b.deviceTurns == int.MaxValue))
                return Reject(b, RanchRuleCodeR2.InvalidSnapshot, "Ledger overflow.", out next, out result);
            next = null; result = null; return true;
        }
        static RanchRuleResultR2 Applied(string id, RanchRuleEventR2 kind, RanchCellR2[] route, bool automatic, bool ate)
        {
            return new RanchRuleResultR2 { code = RanchRuleCodeR2.Applied, changed = true, instanceId = id,
                eventKind = kind, actualRoute = route, automatic = automatic, costDelta = automatic ? 0 : 1,
                turnDelta = automatic ? 0 : 1, ateGrass = ate, removedSheep = ate };
        }

        // Only crossing FROM the pair's dry cell TO its wet cell invokes the exchange.
        // Other ordinary player movement stays with the existing host resolver.
        public static bool TryCrossPond(RanchRuleBoardR2 source, RanchCellR2 direction, out RanchRuleBoardR2 next, out RanchRuleResultR2 result)
        {
            if (!Preflight(source, direction, false, out next, out result)) return false;
            RanchCellR2 destination;
            if (!Step(source.player, direction, out destination)) return Reject(source, RanchRuleCodeR2.Blocked, "Outside coordinate range.", out next, out result);
            int index = -1;
            for (int i = 0; i < source.ponds.Count; i++)
                if (source.ponds[i].Dry == source.player && source.ponds[i].Wet == destination) { index = i; break; }
            if (index < 0) return Reject(source, RanchRuleCodeR2.NotApplicable, "Not a dry-to-paired-wet crossing.", out next, out result);
            if (source.solidOccupants.Contains(destination) || HasSheep(source, destination))
                return Reject(source, RanchRuleCodeR2.Blocked, "Destination occupied.", out next, out result);
            if (source.floodPolicyHolds.Contains(source.player))
                return Reject(source, RanchRuleCodeR2.FloodPolicyRequired, "Another flood occupant needs host policy; no partial exchange.", out next, out result);
            next = source.Copy(); next.ponds[index].wetA = !next.ponds[index].wetA; next.player = destination;
            next.costSpent++; next.deviceTurns++;
            result = Applied(source.ponds[index].instanceId, RanchRuleEventR2.PondCrossed, new[] { source.player, destination }, false, false);
            result.waterRoute = new[] { source.ponds[index].Wet, source.ponds[index].Dry };
            return true;
        }

        // Player remains stationary. Direct push is one cell; grass consumes both grass and sheep.
        public static bool TryPushSheep(RanchRuleBoardR2 source, string sheepId, RanchCellR2 direction, out RanchRuleBoardR2 next, out RanchRuleResultR2 result)
        {
            if (!Preflight(source, direction, false, out next, out result)) return false;
            RanchCellR2 position, front, destination;
            if (string.IsNullOrEmpty(sheepId) || !source.sheep.TryGetValue(sheepId, out position)
                || !Step(source.player, direction, out front) || front != position)
                return Reject(source, RanchRuleCodeR2.NotApplicable, "No adjacent sheep in input direction.", out next, out result);
            if (!Step(position, direction, out destination) || !CanSheepEnter(source, destination, sheepId))
                return Reject(source, RanchRuleCodeR2.Blocked, "Sheep destination blocked.", out next, out result);
            bool eat = source.ground[destination] == RanchGroundR2.TallGrass;
            next = source.Copy();
            if (eat) { next.ground[destination] = RanchGroundR2.Floor; next.sheep.Remove(sheepId); }
            else next.sheep[sheepId] = destination;
            next.costSpent++; next.deviceTurns++;
            result = Applied(sheepId, eat ? RanchRuleEventR2.SheepAteAndRemoved : RanchRuleEventR2.SheepPushed, new[] { position, destination }, false, eat);
            return true;
        }

        // Resolve the host's ALREADY ACTIVE unobstructed wind area; no extra input cost or device turn.
        // The host owns windmill activation, metal-box shadowing and cross-device order.
        public static bool TryWindSheep(RanchRuleBoardR2 source, string sheepId, RanchCellR2 direction, IList<RanchCellR2> actualWindCells, int configuredRange, out RanchRuleBoardR2 next, out RanchRuleResultR2 result)
        {
            if (!Preflight(source, direction, true, out next, out result)) return false;
            if (actualWindCells == null || (configuredRange != 0 && configuredRange != 3 && configuredRange != 6 && configuredRange != 9)
                || actualWindCells.Count > configuredRange)
                return Reject(source, RanchRuleCodeR2.InvalidWind, "Use the actual 0/3/6/9-cell wind area.", out next, out result, true);
            var unique = new HashSet<RanchCellR2>();
            for (int i = 0; i < actualWindCells.Count; i++)
            {
                RanchCellR2 expected;
                if (!unique.Add(actualWindCells[i]) || (i > 0 && (!Step(actualWindCells[i - 1], direction, out expected) || expected != actualWindCells[i])))
                    return Reject(source, RanchRuleCodeR2.InvalidWind, "Wind cells must be unique and contiguous in the supplied direction.", out next, out result, true);
            }
            RanchCellR2 position;
            if (string.IsNullOrEmpty(sheepId) || !source.sheep.TryGetValue(sheepId, out position))
                return Reject(source, RanchRuleCodeR2.NotApplicable, "No active sheep.", out next, out result, true);
            int at = -1;
            for (int i = 0; i < actualWindCells.Count; i++) if (actualWindCells[i] == position) { at = i; break; }
            if (at < 0) return Reject(source, RanchRuleCodeR2.NotApplicable, "Sheep is outside active wind.", out next, out result, true);
            var route = new List<RanchCellR2> { position }; bool eat = false;
            for (int i = at + 1; i < actualWindCells.Count; i++)
            {
                RanchCellR2 destination = actualWindCells[i];
                if (!CanSheepEnter(source, destination, sheepId)) break;
                route.Add(destination); position = destination;
                if (source.ground[destination] == RanchGroundR2.TallGrass) { eat = true; break; }
            }
            if (route.Count == 1) return Reject(source, RanchRuleCodeR2.Blocked, "Wind cannot move this sheep farther.", out next, out result, true);
            next = source.Copy();
            if (eat) { next.ground[position] = RanchGroundR2.Floor; next.sheep.Remove(sheepId); }
            else next.sheep[sheepId] = position;
            result = Applied(sheepId, eat ? RanchRuleEventR2.SheepWindAteAndRemoved : RanchRuleEventR2.SheepWindMoved, route.ToArray(), true, eat);
            return true;
        }
    }
}
