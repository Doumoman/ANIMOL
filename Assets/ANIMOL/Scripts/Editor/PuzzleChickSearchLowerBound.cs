using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    // A pattern database for one delayed follower and one nest. Remove all crates,
    // sheep, grass and pond restrictions. A follower may always stay blocked even
    // when the relaxed floor is clear, preserving every actual blocked-follow edge.
    public sealed class PuzzleChickSearchLowerBound
    {
        readonly Dictionary<Vector2Int, int> cells;
        readonly Vector2Int[] floor, bubbles;
        readonly int[,] adjacent;
        readonly int[] distances, bubbleBits;
        readonly string chickId;
        readonly int n, nest;
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static PuzzleChickSearchLowerBound TryCreate(PuzzleMap map)
        {
            var chicks = map.scenario.devices.Where(d => d.role == PrototypeRole.Chick).ToArray();
            if (chicks.Length != 1 || chicks[0].active || Mathf.Clamp(chicks[0].number, 1, 8) != 1 ||
                map.scenario.NestCells.Count() != 1 || map.bubbles.Distinct().Count() > 4 ||
                map.scenario.devices.Any(d => d.role != PrototypeRole.Chick &&
                    d.role != PrototypeRole.MetalCrate && d.role != PrototypeRole.WoodCrate)) return null;
            int count = map.terrain.Select(t => t.position).Distinct().Count(map.Walkable);
            if ((long)count * (count + 1) * 5 * (1 << map.bubbles.Distinct().Count()) > 1000000) return null;
            return new PuzzleChickSearchLowerBound(map, chicks[0]);
        }

        int Index(int player, int chick, int trail, int mask) => (((mask * (n + 1) + chick) * 5 + trail) * n) + player;
        bool NearNest(int cell) => cell == nest || (floor[cell] - floor[nest]).sqrMagnitude == 1;

        PuzzleChickSearchLowerBound(PuzzleMap map, PrototypeDevice chick)
        {
            floor = map.terrain.Select(t => t.position).Distinct().Where(map.Walkable).ToArray(); n = floor.Length;
            cells = floor.Select((p, i) => new { p, i }).ToDictionary(x => x.p, x => x.i);
            nest = cells[map.scenario.NestCells.Single()]; chickId = chick.id;
            bubbles = map.bubbles.Distinct().ToArray(); bubbleBits = new int[n];
            for (int b = 0; b < bubbles.Length; b++) if (cells.TryGetValue(bubbles[b], out int at)) bubbleBits[at] |= 1 << b;
            adjacent = new int[n, 4];
            for (int p = 0; p < n; p++) for (int d = 0; d < 4; d++)
                adjacent[p, d] = cells.TryGetValue(floor[p] + Directions[d], out int to) ? to : -1;
            int masks = 1 << bubbles.Length, size = masks * (n + 1) * 5 * n;
            distances = Enumerable.Repeat(-1, size).ToArray();
            var incoming = new List<int>[size];
            Action<int, int> edge = (from, to) =>
            {
                if (incoming[to] == null) incoming[to] = new List<int>(4);
                incoming[to].Add(from);
            };
            for (int mask = 0; mask < masks; mask++) for (int c = 0; c <= n; c++)
                for (int trail = 0; trail < 5; trail++) for (int p = 0; p < n; p++)
                {
                    if (trail != 0 && adjacent[p, trail - 1] < 0 || c == n && trail != 0) continue;
                    int from = Index(p, c, trail, mask);
                    for (int d = 0; d < 4; d++)
                    {
                        int to = adjacent[p, d];
                        if (to < 0 || c < n && (to == c || to == nest)) continue;
                        int nextMask = mask | bubbleBits[to], nextTrail = (d + 2) % 4 + 1;
                        if (c == n) { edge(from, Index(to, n, 0, nextMask)); continue; }
                        edge(from, Index(to, c, nextTrail, nextMask)); // Actual obstruction may prevent a follow.
                        if (NearNest(c) && to != nest) edge(from, Index(to, n, 0, nextMask));
                        if (trail == 0) continue;
                        int target = adjacent[p, trail - 1];
                        if (target == to || (floor[target] - floor[c]).sqrMagnitude != 1) continue;
                        edge(from, Index(to, target, nextTrail, nextMask));
                        if (NearNest(target) && to != nest) edge(from, Index(to, n, 0, nextMask));
                    }
                }
            var queue = new int[size]; int head = 0, tail = 0, exit = cells[map.exit];
            for (int mask = 0; mask < masks; mask++)
            {
                int collected = 0; for (int bits = mask; bits != 0; bits &= bits - 1) collected++;
                if (collected < map.targetBubbles) continue;
                for (int c = 0; c <= n; c++) for (int t = 0; t < 5; t++)
                {
                    int goal = Index(exit, c, t, mask); distances[goal] = 0; queue[tail++] = goal;
                }
            }
            while (head < tail)
            {
                int current = queue[head++];
                if (incoming[current] == null) continue;
                foreach (int previous in incoming[current])
                {
                    if (distances[previous] >= 0) continue;
                    distances[previous] = distances[current] + 1; queue[tail++] = previous;
                }
            }
        }

        public int Estimate(PuzzleDeviceSession session)
        {
            if (!cells.TryGetValue(session.Cell, out int p)) return 0;
            var chick = session.States[chickId];
            int c = chick.removed ? n : cells.TryGetValue(chick.cell, out int at) ? at : -1;
            if (c < 0 || chick.trail.Count > 1) return 0;
            int trail = chick.trail.Count == 0 ? 0 : Array.IndexOf(Directions, chick.trail[0] - session.Cell) + 1;
            if (chick.trail.Count != 0 && trail == 0) return 0;
            int mask = 0; for (int b = 0; b < bubbles.Length; b++) if (session.IsCollected(bubbles[b])) mask |= 1 << b;
            return Math.Max(0, distances[Index(p, c, trail, mask)]);
        }
    }
}
