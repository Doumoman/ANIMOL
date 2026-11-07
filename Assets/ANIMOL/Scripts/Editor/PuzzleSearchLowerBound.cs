using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    /// <summary>
    /// Exact distance on a relaxed board: remove movable occupants, grass, ponds
    /// and nest restrictions, then collect enough bubbles and reach the exit.
    /// Potential wind/bounce rays allow free steps along each ray; device origins
    /// and conveyors allow every heading. This relaxation includes every real
    /// forced move. Unsupported mechanisms retain the zero bound and 0-1 BFS.
    /// </summary>
    public sealed class PuzzleSearchLowerBound
    {
        readonly Dictionary<Vector2Int, int> cells;
        readonly Vector2Int[] bubbles;
        readonly int[] distances;
        readonly int count;
        readonly PuzzleChickSearchLowerBound chickBound;

        public static PuzzleSearchLowerBound TryCreate(PuzzleMap map)
        {
            if (!map || !map.scenario || map.scenario.echoTheaterV3Rules ||
                map.scenario.devices.Any(d => d.role != PrototypeRole.MetalCrate && d.role != PrototypeRole.Chick &&
                    d.role != PrototypeRole.WoodCrate && d.role != PrototypeRole.StarSocket &&
                    d.role != PrototypeRole.Wind && d.role != PrototypeRole.Bounce && d.role != PrototypeRole.Conveyor) ||
                map.devices.Any(d => d.kind != DeviceKind.None) || map.bubbles.Distinct().Count() > 12)
                return null;
            if (BubblePositions(map).Length > 12) return null;
            return new PuzzleSearchLowerBound(map);
        }

        static Vector2Int[] BubblePositions(PuzzleMap map) => map.bubbles.Concat(map.scenario.devices
            .Where(d => d.role == PrototypeRole.StarSocket && d.variant == "socket" && d.socketReward == StarSocketReward.RevealBubble)
            .Select(d => d.socketBubbleCell)).Distinct().ToArray();

        PuzzleSearchLowerBound(PuzzleMap map)
        {
            chickBound = PuzzleChickSearchLowerBound.TryCreate(map);
            var floor = map.terrain.Select(t => t.position).Distinct().Where(map.Walkable).ToArray();
            cells = floor.Select((p, i) => new { p, i }).ToDictionary(x => x.p, x => x.i);
            count = cells.Count;
            bubbles = BubblePositions(map);
            int masks = 1 << bubbles.Length;
            distances = Enumerable.Repeat(-1, count * masks).ToArray();
            if (!cells.TryGetValue(map.exit, out int exit)) return;
            var neighbours = new List<int>[count];
            var bits = new int[count];
            var free = new bool[count, 4];
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            // Ignore blockers, phase/range and reflection restrictions. Every
            // possible real wind segment lies on one of these generous rays.
            foreach (var device in map.scenario.devices)
            {
                if (device.role == PrototypeRole.Conveyor && cells.TryGetValue(device.position, out int belt))
                    for (int heading = 0; heading < 4; heading++) free[belt, heading] = true;
                if (device.role != PrototypeRole.Wind && device.role != PrototypeRole.Bounce) continue;
                if (cells.TryGetValue(device.position, out int origin))
                    for (int heading = 0; heading < 4; heading++) free[origin, heading] = true;
                for (int heading = 0; heading < directions.Length; heading++)
                    for (var p = device.position + directions[heading]; map.Contains(p); p += directions[heading])
                        if (cells.TryGetValue(p, out int index)) free[index, heading] = true;
            }
            for (int i = 0; i < count; i++)
            {
                neighbours[i] = new List<int>(4);
                foreach (var direction in directions)
                    if (cells.TryGetValue(floor[i] + direction, out int next)) neighbours[i].Add(next);
            }
            for (int b = 0; b < bubbles.Length; b++)
                if (cells.TryGetValue(bubbles[b], out int index)) bits[index] |= 1 << b;
            var queue = new LinkedList<int>();
            for (int mask = 0; mask < masks; mask++)
            {
                int collected = 0;
                for (int value = mask; value != 0; value &= value - 1) collected++;
                if (collected < map.targetBubbles) continue;
                int goal = mask * count + exit;
                distances[goal] = 0; queue.AddLast(goal);
            }
            while (queue.Count > 0)
            {
                int state = queue.First.Value; queue.RemoveFirst();
                int cell = state % count, mask = state / count;
                int bit = bits[cell];
                // Reverse a move into this cell, including its automatic collection.
                if ((mask & bit) != bit) continue;
                foreach (int previous in neighbours[cell])
                {
                    int heading = Array.IndexOf(directions, floor[cell] - floor[previous]);
                    int cost = free[previous, heading] ? 0 : 1;
                    Visit(mask * count + previous, distances[state] + cost, cost == 0, queue);
                    if (bit != 0) Visit((mask ^ bit) * count + previous, distances[state] + cost, cost == 0, queue);
                }
            }
        }

        void Visit(int state, int distance, bool zeroCost, LinkedList<int> queue)
        {
            if (distances[state] >= 0 && distances[state] <= distance) return;
            distances[state] = distance;
            if (zeroCost) queue.AddFirst(state); else queue.AddLast(state);
        }

        public int Estimate(PuzzleDeviceSession session)
        {
            if (count == 0 || !cells.TryGetValue(session.Cell, out int cell)) return 0;
            int mask = 0;
            for (int b = 0; b < bubbles.Length; b++) if (session.IsCollected(bubbles[b])) mask |= 1 << b;
            // Never prune on a disconnected relaxation; the live resolver remains authoritative.
            return Math.Max(Math.Max(0, distances[mask * count + cell]), chickBound?.Estimate(session) ?? 0);
        }
    }
}
