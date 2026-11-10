using ANIMOL.Puzzle;
using UnityEngine;

// A necessary reachability condition for the statue/glass/dice-only boards.
// No gameplay transition is replaced. Only branches that cannot ever reach
// enough stars and the exit are discarded.
public sealed class EchoStatueDeadlock
{
    readonly PuzzleMap map;
    readonly HashSet<Vector2Int> support;
    readonly Vector2Int[] bubbles;
    readonly Dictionary<string, Dictionary<Vector2Int, int>> components = new();
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    public static EchoStatueDeadlock Create(PuzzleMap map) =>
        map.scenario.devices.All(d => d.role == PrototypeRole.Statue || d.role == PrototypeRole.Glass || d.role == PrototypeRole.Dice)
            ? new EchoStatueDeadlock(map) : null;
    EchoStatueDeadlock(PuzzleMap map)
    {
        this.map = map;
        support = map.terrain.Select(t => t.position).Where(map.Walkable).ToHashSet();
        foreach (var d in map.scenario.devices.Where(d => d.role == PrototypeRole.Dice && (d.variant == "target" || d.variant == "socket")))
            if (map.Contains(d.position) && !map.DecorationAt(d.position)) support.Add(d.position);
        bubbles = map.bubbles.Distinct().ToArray();
    }
    public bool IsDead(PuzzleDeviceSession state)
    {
        var statues = state.Devices.Where(d => d.role == PrototypeRole.Statue && !state.States[d.id].removed).ToArray();
        var blocked = new HashSet<Vector2Int>();
        // Directions are fixed on these boards. Missing support never appears;
        // a statue blocked by an already permanent statue is permanent as well.
        bool changed;
        do
        {
            changed = false;
            foreach (var d in statues)
            {
                var s = state.States[d.id]; var next = s.cell + s.direction;
                if (!support.Contains(next) || blocked.Contains(next)) changed |= blocked.Add(s.cell);
            }
        } while (changed);
        if (blocked.Count == 0 || blocked.Contains(state.Cell)) return false;
        string key = string.Join(";", blocked.OrderBy(p => p.x).ThenBy(p => p.y).Select(p => p.x + "," + p.y));
        if (!components.TryGetValue(key, out var labels))
        {
            labels = new Dictionary<Vector2Int, int>();
            foreach (var start in support)
            {
                if (blocked.Contains(start) || labels.ContainsKey(start)) continue;
                int label = labels.Count;
                var queue = new Queue<Vector2Int>(); queue.Enqueue(start); labels.Add(start, label);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    foreach (var heading in Directions)
                    {
                        var next = p + heading;
                        if (support.Contains(next) && !blocked.Contains(next) && !labels.ContainsKey(next))
                        { labels.Add(next, label); queue.Enqueue(next); }
                    }
                }
            }
            components.Add(key, labels);
        }
        if (!labels.TryGetValue(state.Cell, out int player) || !labels.TryGetValue(map.exit, out int exit) || player != exit) return true;
        int reachable = state.Collected;
        foreach (var bubble in bubbles)
            if (!state.IsCollected(bubble) && labels.TryGetValue(bubble, out int at) && at == player) reachable++;
        return reachable < map.targetBubbles;
    }

    // Independent exhaustive public-Apply BFS on small boards catches both
    // false deadlocks and missed immutable-statue chains.
    public static object VerifySmallBoards()
    {
        var results = new List<object>();
        for (int variant = 0; variant < 4; variant++)
        {
            var map = new PuzzleMap { name = "statue_prune_" + variant, width = 6, height = 3, validated = true,
                start = new Vector2Int(0, 0), exit = new Vector2Int(4, 0), targetBubbles = 1,
                scenario = new PuzzleScenario { themeId = "memory_theater", echoTheaterV3Rules = true } };
            for (int x = 0; x < 5; x++) map.terrain.Add(new TerrainCell { position = new Vector2Int(x, 0) });
            map.bubbles.Add(new Vector2Int(3, 0));
            map.scenario.devices.Add(new PrototypeDevice { id = "statue", role = PrototypeRole.Statue,
                position = new Vector2Int(2, 0), direction = Vector2Int.up, number = 1 });
            if (variant == 1)
            {
                map.scenario.devices[0].direction = Vector2Int.right;
                map.scenario.devices.Add(new PrototypeDevice { id = "stopper", role = PrototypeRole.Statue,
                    position = new Vector2Int(3, 0), direction = Vector2Int.up });
            }
            if (variant == 2)
            {
                for (int x = 0; x < 5; x++) map.terrain.Add(new TerrainCell { position = new Vector2Int(x, 1) });
                map.start = new Vector2Int(0, 1); // The statue can move and the player can go around.
            }
            if (variant == 3)
                map.scenario.devices.Add(new PrototypeDevice { id = "support", role = PrototypeRole.Dice,
                    variant = "target", position = new Vector2Int(2, 1) });
            var root = new PuzzleDeviceSession(map);
            bool dead = Create(map).IsDead(root);
            if (dead != (variant < 2)) throw new Exception("Incorrect terminal statue classification: " + variant);
            var queue = new Queue<PuzzleDeviceSession>(); queue.Enqueue(root);
            var seen = new HashSet<string> { root.SearchStateKey() }; int distance = -1;
            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                if (state.Won) { distance = state.Cost; break; }
                foreach (var input in Directions)
                {
                    var child = state.Copy();
                    if (child.Apply(input).accepted && !child.Failed && seen.Add(child.SearchStateKey())) queue.Enqueue(child);
                }
                if (seen.Count > 20000) throw new Exception("Fixture BFS unexpectedly exceeded its finite state bound.");
            }
            if (dead && distance >= 0) throw new Exception("Pruned a clearing fixture.");
            results.Add(new { variant, dead, bfsDistance = distance, bfsStates = seen.Count });
        }
        return results;
    }
}
