using ANIMOL.Puzzle;
using UnityEngine;

// Exact pattern database on a relaxation: erase all statues/glass and retain
// the single die, its orientation, absorbing goal, player and collected stars.
// A stationary input is always permitted (zero change in the projection), so
// pushing erased glass also projects to a legal relaxed unit-cost edge.
public sealed class EchoDiceLowerBound
{
    readonly string dieId, goalId;
    readonly Dictionary<Vector2Int, int> cells;
    readonly Vector2Int[] floor, bubbles;
    readonly int[][] faces;
    readonly Dictionary<string, int> orientations;
    readonly int[,] rolled, adjacent;
    readonly int[] distance, star;
    readonly int n, modes, absorbed, goal, exit, required;
    readonly int targetNumber;
    public int StateCount => distance.Length;
    public int InitialDistance { get; }
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public static EchoDiceLowerBound Create(PuzzleMap map)
    {
        var devices = map.scenario.devices;
        if (map.scenario.themeId != "memory_theater" || !map.scenario.echoTheaterV3Rules ||
            map.scenario.goals.Count != 0 || map.scenario.water.Count != 0 || map.scenario.ranchV13Art.Count != 0 ||
            devices.Any(d => d.role != PrototypeRole.Statue && d.role != PrototypeRole.Glass && d.role != PrototypeRole.Dice) ||
            devices.Count(d => d.role == PrototypeRole.Dice && d.variant != "target" && d.variant != "socket") != 1 ||
            devices.Count(d => d.role == PrototypeRole.Dice && (d.variant == "target" || d.variant == "socket")) != 1 ||
            map.bubbles.Distinct().Count() > 3) return null;
        return new EchoDiceLowerBound(map);
    }

    static string FaceKey(int[] f) => string.Join(",", f);
    EchoDiceLowerBound(PuzzleMap map)
    {
        var die = map.scenario.devices.Single(d => d.role == PrototypeRole.Dice && d.variant != "target" && d.variant != "socket");
        var target = map.scenario.devices.Single(d => d.role == PrototypeRole.Dice && (d.variant == "target" || d.variant == "socket"));
        dieId = die.id; goalId = target.id; targetNumber = target.number; required = map.targetBubbles;
        floor = map.terrain.Select(t => t.position).Where(map.Walkable).Append(target.position).Distinct().ToArray();
        cells = floor.Select((p, i) => (p, i)).ToDictionary(p => p.p, p => p.i);
        n = floor.Length; goal = cells[target.position]; exit = cells[map.exit];
        bubbles = map.bubbles.Distinct().ToArray(); star = new int[n];
        for (int i = 0; i < bubbles.Length; i++) star[cells[bubbles[i]]] = 1 << i;
        adjacent = new int[n, 4];
        for (int p = 0; p < n; p++) for (int d = 0; d < 4; d++)
            adjacent[p, d] = cells.TryGetValue(floor[p] + Directions[d], out int to) ? to : -1;
        var allFaces = new List<int[]> { new PuzzleDeviceSession(map).States[dieId].dice };
        orientations = new Dictionary<string, int> { [FaceKey(allFaces[0])] = 0 };
        for (int i = 0; i < allFaces.Count; i++) foreach (var direction in Directions)
        {
            var next = (int[])allFaces[i].Clone(); PuzzleDeviceSession.Roll(next, direction);
            if (!orientations.ContainsKey(FaceKey(next))) { orientations.Add(FaceKey(next), allFaces.Count); allFaces.Add(next); }
        }
        if (allFaces.Count != 24) throw new NotSupportedException("Expected 24 distinct die orientations.");
        faces = allFaces.ToArray(); rolled = new int[24, 4];
        for (int i = 0; i < 24; i++) for (int d = 0; d < 4; d++)
        {
            var next = (int[])faces[i].Clone(); PuzzleDeviceSession.Roll(next, Directions[d]);
            rolled[i, d] = orientations[FaceKey(next)];
        }
        absorbed = n * 24; modes = absorbed + 1;
        int size = n * modes * (1 << bubbles.Length);
        var counts = new int[size];
        for (int state = 0; state < size; state++) for (int d = 0; d < 4; d++)
        { int next = Step(state, d); if (next >= 0 && next != state) counts[next]++; }
        var offsets = new int[size + 1];
        for (int i = 0; i < size; i++) offsets[i + 1] = checked(offsets[i] + counts[i]);
        var predecessors = new int[offsets[size]]; Array.Clear(counts);
        for (int state = 0; state < size; state++) for (int d = 0; d < 4; d++)
        { int next = Step(state, d); if (next >= 0 && next != state) predecessors[offsets[next] + counts[next]++] = state; }
        distance = Enumerable.Repeat(-1, size).ToArray();
        var queue = new Queue<int>();
        for (int mask = 0; mask < 1 << bubbles.Length; mask++)
        {
            if (System.Numerics.BitOperations.PopCount((uint)mask) < required) continue;
            // Seed every abstract winning state, including disconnected ones.
            // Reachability and goal passability are enforced by incoming edges.
            for (int mode = 0; mode < modes; mode++)
            { int state = Pack(exit, mode, mask); distance[state] = 0; queue.Enqueue(state); }
        }
        while (queue.Count > 0)
        {
            int state = queue.Dequeue();
            for (int at = offsets[state]; at < offsets[state + 1]; at++)
            {
                int previous = predecessors[at];
                if (distance[previous] >= 0) continue;
                distance[previous] = distance[state] + 1; queue.Enqueue(previous);
            }
        }
        InitialDistance = Estimate(new PuzzleDeviceSession(map));
    }

    int Pack(int player, int mode, int mask) => (mask * modes + mode) * n + player;
    int Step(int state, int direction)
    {
        int player = state % n, mode = state / n % modes, mask = state / n / modes;
        int to = adjacent[player, direction]; if (to < 0) return -1;
        if (mode != absorbed && to == mode / 24)
        {
            int pushed = adjacent[to, direction]; if (pushed < 0) return -1;
            int face = rolled[mode % 24, direction];
            mode = pushed == goal && faces[face][0] == targetNumber ? absorbed : pushed * 24 + face;
            return Pack(player, mode, mask);
        }
        if (mode != absorbed && to == goal) return -1;
        return Pack(to, mode, mask | star[to]);
    }

    int Projection(PuzzleDeviceSession state)
    {
        var die = state.States[dieId]; var target = state.States[goalId];
        if (die.removed != target.converted) throw new InvalidOperationException("Unexpected single-die conversion state.");
        int mode = die.removed ? absorbed : cells[die.cell] * 24 + orientations[FaceKey(die.dice)];
        int mask = 0; for (int i = 0; i < bubbles.Length; i++) if (state.IsCollected(bubbles[i])) mask |= 1 << i;
        return Pack(cells[state.Cell], mode, mask);
    }
    public int Estimate(PuzzleDeviceSession state) => distance[Projection(state)];
    public void VerifyEdge(PuzzleDeviceSession before, PuzzleDeviceSession after, int direction)
    {
        int a = Projection(before), b = Projection(after);
        if (a != b && Step(a, direction) != b)
            throw new InvalidOperationException("Native edge absent from the relaxed dice graph.");
        if (after.Won && distance[b] != 0 || distance[b] >= 0 && distance[a] > 1 + distance[b])
            throw new InvalidOperationException("Dice lower bound is inconsistent.");
    }
}
