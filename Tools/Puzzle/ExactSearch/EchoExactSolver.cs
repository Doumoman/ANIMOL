using System.Diagnostics;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using UnityEngine;

// Only the search queue/lower bound run here. Every gameplay edge and checkpoint
// uses the same source-linked PuzzleDeviceSession as the live Unity Editor.
public static class EchoExactSolver
{
    public static int LastDeadlockPruned { get; private set; }
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    public static bool Supports(PuzzleMap map)
    {
        var allowed = new[] { PrototypeRole.Shutter, PrototypeRole.MovingStage, PrototypeRole.Domino,
            PrototypeRole.Glass, PrototypeRole.Dice, PrototypeRole.CowardDoll, PrototypeRole.Statue, PrototypeRole.Bookshelf };
        return map.scenario.themeId == "memory_theater" && map.devices.Count == 0 &&
            map.scenario.devices.All(d => allowed.Contains(d.role)) && map.scenario.ranchV13Art.Count == 0;
    }

    sealed class RelaxedBound
    {
        readonly Dictionary<Vector2Int, int> indices;
        readonly Vector2Int[] bubbles;
        readonly int[] distance;
        readonly int count;
        public static RelaxedBound Create(PuzzleMap map)
        {
            var allowed = new[] { PrototypeRole.Shutter, PrototypeRole.MovingStage, PrototypeRole.Domino,
                PrototypeRole.Glass, PrototypeRole.Dice, PrototypeRole.CowardDoll, PrototypeRole.Statue, PrototypeRole.Bookshelf };
            return map.scenario.devices.Any(d => !allowed.Contains(d.role)) || map.devices.Count > 0 || map.bubbles.Distinct().Count() > 10
                ? null : new RelaxedBound(map);
        }
        RelaxedBound(PuzzleMap map)
        {
            var floor = map.terrain.Select(t => t.position).Distinct().Where(map.Walkable).ToArray();
            indices = floor.Select((p, i) => new { p, i }).ToDictionary(x => x.p, x => x.i);
            count = floor.Length; bubbles = map.bubbles.Distinct().ToArray();
            int masks = 1 << bubbles.Length;
            var bits = floor.Select(p => { int b = Array.IndexOf(bubbles, p); return b < 0 ? 0 : 1 << b; }).ToArray();
            var tracks = map.scenario.devices.Where(d => d.role == PrototypeRole.MovingStage).OrderBy(d => d.id, StringComparer.Ordinal)
                .Select(d => new HashSet<Vector2Int>(map.scenario.RailCellsFor(d))).ToArray();
            var reverse = Enumerable.Range(0, count * masks).Select(_ => new List<int>()).ToArray();
            for (int mask = 0; mask < masks; mask++) for (int start = 0; start < count; start++)
            {
                // Remove all movable blockers. Permit a stationary action or a
                // walking step, then optional pushes from any cell of each cart's
                // rail to any neighbour or back to P0. Retain intermediate stars.
                var positions = new HashSet<int> { start };
                foreach (var d in Directions) if (indices.TryGetValue(floor[start] + d, out int next)) positions.Add(next);
                var ends = new HashSet<int>(positions.Select(p => (mask | bits[start] | bits[p]) * count + p));
                foreach (var track in tracks)
                {
                    var expanded = new HashSet<int>(ends);
                    foreach (int end in ends)
                    {
                        int p = end % count, collected = end / count;
                        if (!track.Contains(floor[p])) continue;
                        expanded.Add((collected | bits[start]) * count + start);
                        foreach (var d in Directions)
                            if (indices.TryGetValue(floor[p] + d, out int pushed))
                                expanded.Add((collected | bits[pushed]) * count + pushed);
                    }
                    ends = expanded;
                }
                foreach (int end in ends) reverse[end].Add(mask * count + start);
            }
            distance = Enumerable.Repeat(-1, count * masks).ToArray();
            var queue = new Queue<int>();
            if (indices.TryGetValue(map.exit, out int exit))
                for (int mask = 0; mask < masks; mask++)
                {
                    int collected = 0; for (int value = mask; value != 0; value &= value - 1) collected++;
                    if (collected < map.targetBubbles) continue;
                    int goal = mask * count + exit; distance[goal] = 0; queue.Enqueue(goal);
                }
            while (queue.Count > 0)
            {
                int state = queue.Dequeue();
                foreach (int previous in reverse[state])
                    if (distance[previous] < 0) { distance[previous] = distance[state] + 1; queue.Enqueue(previous); }
            }
        }
        public int Estimate(PuzzleDeviceSession state)
        {
            if (!indices.TryGetValue(state.Cell, out int cell)) return 0;
            int mask = 0; for (int i = 0; i < bubbles.Length; i++) if (state.IsCollected(bubbles[i])) mask |= 1 << i;
            return Math.Max(0, distance[mask * count + cell]);
        }
    }
    sealed class Node
    {
        public PuzzleDeviceSession.SearchSnapshot state;
        public string key;
        public int index, cost;
    }

    static string Key(PuzzleDeviceSession state, string[] shutters, string[] statues, string[] glasses)
    {
        var phases = new int[shutters.Length]; var timers = new int[statues.Length];
        var headings = new Vector2Int[glasses.Length];
        try
        {
            for (int i = 0; i < shutters.Length; i++)
            { phases[i] = state.States[shutters[i]].phase; state.States[shutters[i]].phase &= 1; }
            for (int i = 0; i < statues.Length; i++)
            {
                var s = state.States[statues[i]]; timers[i] = s.timer;
                // Statue() returns before reading timer while unlocked, and a
                // new line lock overwrites it before the next charge reads it.
                if (!s.locked) s.timer = 0;
            }
            // Theater glass pushes use the new input direction. Its previous
            // heading is only presentation data in this supported device set.
            for (int i = 0; i < glasses.Length; i++)
            { var s = state.States[glasses[i]]; headings[i] = s.direction; s.direction = Vector2Int.zero; }
            return PuzzleSearchKeyCodec.Pack(state.SearchStateKey());
        }
        finally
        {
            for (int i = 0; i < shutters.Length; i++) state.States[shutters[i]].phase = phases[i];
            for (int i = 0; i < statues.Length; i++) state.States[statues[i]].timer = timers[i];
            for (int i = 0; i < glasses.Length; i++) state.States[glasses[i]].direction = headings[i];
        }
    }

    public static int VerifyCanonicalKeys(PuzzleDeviceSession state)
    {
        var statues = state.Devices.Where(d => d.role == PrototypeRole.Statue).Select(d => d.id).ToArray();
        var glasses = state.Devices.Where(d => d.role == PrototypeRole.Glass).Select(d => d.id).ToArray();
        if (!statues.Any(id => !state.States[id].locked) && glasses.Length == 0) return 0;
        var shutters = state.Devices.Where(d => d.role == PrototypeRole.Shutter).Select(d => d.id).ToArray();
        var twin = state.Copy();
        foreach (var id in statues) if (!twin.States[id].locked) twin.States[id].timer += 97;
        foreach (var id in glasses) twin.States[id].direction = twin.States[id].direction == Vector2Int.up ? Vector2Int.left : Vector2Int.up;
        if (Key(state, shutters, statues, glasses) != Key(twin, shutters, statues, glasses)) throw new InvalidOperationException("Canonical key mismatch.");
        foreach (var input in Directions)
        {
            var a = state.Copy(); var b = twin.Copy();
            bool accepted = a.Apply(input).accepted, other = b.Apply(input).accepted;
            if (accepted != other || a.Cost != b.Cost || a.Won != b.Won || a.Failed != b.Failed ||
                Key(a, shutters, statues, glasses) != Key(b, shutters, statues, glasses))
                throw new InvalidOperationException("An ignored timer/heading changed a gameplay successor.");
        }
        return 4;
    }

    public static PuzzleShortestPathSolver.Result Run(PuzzleMap map, int stateLimit, int memoryMiB)
    {
        if (!Supports(map) || stateLimit < 1 || memoryMiB < 1)
            throw new ArgumentException("Expected an exported, Unity-validated theater board and positive limits.");
        string sourceHash = PuzzleShortestPathSolver.HashAsset(map);
        var watch = Stopwatch.StartNew();
        var template = new PuzzleDeviceSession(map);
        var shutters = template.Devices.Where(d => d.role == PrototypeRole.Shutter).Select(d => d.id).ToArray();
        var statues = template.Devices.Where(d => d.role == PrototypeRole.Statue).Select(d => d.id).ToArray();
        var glasses = template.Devices.Where(d => d.role == PrototypeRole.Glass).Select(d => d.id).ToArray();
        var bound = RelaxedBound.Create(map);
        var deadlock = EchoStatueDeadlock.Create(map);
        LastDeadlockPruned = 0;
        int Estimate(PuzzleDeviceSession s) => bound?.Estimate(s) ?? 0;
        var queue = new PriorityQueue<Node, (int f, int negativeCost, long serial)>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var parents = new List<int> { -1 };
        var actions = new List<char> { '\0' };
        long serial = 0;
        string initial = Key(template, shutters, statues, glasses);
        seen.Add(initial, 0);
        queue.Enqueue(new Node { state = template.CaptureSearchSnapshot(), key = initial, index = 0, cost = 0 },
            (Estimate(template), 0, serial++));
        long baseline = GC.GetTotalMemory(true);
        int expanded = 0, goal = -1;
        string status = null, detail = "";
        double nextReport = 5;
        while (queue.Count > 0 && status == null)
        {
            var node = queue.Dequeue();
            if (seen[node.key] != node.cost) continue;
            var state = template.RestoreSearchSnapshot(node.state);
            expanded++;
            if (state.Won)
            {
                status = "solved"; goal = node.index;
                detail = "Minimum-f goal popped; cart-aware permissive bound, reopen-on-improvement and native unit-cost edges.";
                break;
            }
            if (deadlock != null && deadlock.IsDead(state)) { LastDeadlockPruned++; continue; }
            int h0 = Estimate(state);
            for (int i = 0; i < Directions.Length; i++)
            {
                var child = state.Copy();
                if (!child.SearchStep(Directions[i]) || child.Failed) continue;
                if (child.Cost != state.Cost + 1) throw new InvalidOperationException("Non-unit theater edge.");
                int h = Estimate(child);
                if (h0 > 1 + h || child.Won && h != 0)
                    throw new InvalidOperationException("Relaxed lower bound failed native-edge consistency.");
                string key = Key(child, shutters, statues, glasses);
                if (seen.TryGetValue(key, out int oldCost) && oldCost <= child.Cost) continue;
                seen[key] = child.Cost;
                int index = parents.Count;
                parents.Add(node.index); actions.Add("NESW"[i]);
                queue.Enqueue(new Node { state = child.CaptureSearchSnapshot(), key = key, index = index, cost = child.Cost },
                    (child.Cost + h, -child.Cost, serial++));
                if (seen.Count >= stateLimit)
                { status = "limit"; detail = "State cap reached before an exhaustive search or minimum goal; unconfirmed."; break; }
            }
            if (watch.Elapsed.TotalSeconds >= nextReport)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { map = map.name, visited = seen.Count,
                    expanded, frontier = queue.Count, cost = state.Cost, seconds = watch.Elapsed.TotalSeconds,
                    managedMiB = GC.GetTotalMemory(false) / 1048576 }));
                nextReport = watch.Elapsed.TotalSeconds + 5;
                if (GC.GetTotalMemory(false) - baseline > memoryMiB * 1048576L &&
                    GC.GetTotalMemory(true) - baseline > memoryMiB * 1048576L)
                { status = "limit"; detail = "Additional managed-memory cap reached; unconfirmed."; }
            }
        }
        if (status == null)
        { status = "unreachable"; detail = "Finite canonical state queue exhausted, excluding only proven permanent-statue deadlocks; no clearing route exists."; }
        var reversed = new List<char>();
        if (goal >= 0)
            for (int index = goal; index > 0; index = parents[index]) reversed.Add(actions[index]);
        reversed.Reverse();
        string route = new(reversed.ToArray());
        if (PuzzleShortestPathSolver.HashAsset(map) != sourceHash)
        { status = "stale"; route = ""; detail = "Source asset changed during search."; }
        var result = new PuzzleShortestPathSolver.Result { map = map, sourceSha256 = sourceHash,
            status = status, distance = status == "solved" ? route.Length : -1, route = route,
            expanded = expanded, visited = seen.Count, elapsedSeconds = watch.Elapsed.TotalSeconds,
            detail = detail, algorithm = bound == null ? "Exact native-source uniform-cost search" :
                "Exact native-source A*, cart-aware bound, shutter parity, inactive-statue timer and glass-heading keys" };
        queue.Clear(); seen.Clear(); parents.Clear(); actions.Clear();
        return result;
    }
}
