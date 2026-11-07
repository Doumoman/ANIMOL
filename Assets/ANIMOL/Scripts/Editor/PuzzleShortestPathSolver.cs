using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    /// <summary>Exact A* with a permissive board lower bound; 0-1 BFS for unsupported mechanisms.</summary>
    public sealed class PuzzleShortestPathSolver
    {
        public sealed class Result
        {
            public PuzzleMap map;
            public string sourceSha256, route, status, detail, algorithm;
            public int distance, expanded, visited;
            public double elapsedSeconds;
            public bool Solved => status == "solved";
        }

        struct Parent { public int previous; public char action; }
        struct Frontier { public int index, priority, cost; public string key; public PuzzleDeviceSession.SearchSnapshot snapshot; }
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static readonly char[] Letters = { 'N', 'E', 'S', 'W' };
        readonly LinkedList<Frontier> frontier = new LinkedList<Frontier>();
        readonly List<Frontier> heap = new List<Frontier>();
        readonly PuzzleSearchLowerBound lowerBound;
        readonly PuzzleDeviceSession snapshotTemplate;
        readonly Dictionary<string, int> visited = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly List<Parent> parents = new List<Parent>();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly Action<Result> completed;
        readonly int maxVisited;
        readonly long maxManagedBytes;
        readonly int maxAdditionalMemoryMB;
        readonly PuzzleMap map;
        readonly string sourceSha256;
        readonly bool hasWind;
        int expanded;
        bool finished;

        public static PuzzleShortestPathSolver Active { get; private set; }
        public static Result LastResult { get; private set; }
        public string MapName => map ? map.name : "";
        public int Expanded => expanded;
        public int Visited => visited.Count;
        public int FrontierCount => lowerBound != null ? heap.Count : frontier.Count;
        public int SearchDepth => lowerBound != null ? heap.Count == 0 ? -1 : heap[0].cost : frontier.First?.Value.cost ?? -1;
        public string AlgorithmName => lowerBound != null ? "A* (relaxed board; free motion included)" : "0-1 BFS";

        PuzzleShortestPathSolver(PuzzleMap source, int stateLimit, Action<Result> onCompleted, bool useHeuristic, int memoryLimitMB)
        {
            maxAdditionalMemoryMB = memoryLimitMB;
            maxManagedBytes = GC.GetTotalMemory(true) + memoryLimitMB * 1024L * 1024L;
            map = source;
            maxVisited = stateLimit;
            completed = onCompleted;
            sourceSha256 = HashAsset(source);
            foreach (var device in source.scenario.devices)
                if (device.role == PrototypeRole.Wind) { hasWind = true; break; }
            var initial = new PuzzleDeviceSession(source);
            source.BeginReadOnlySearch();
            lowerBound = useHeuristic ? PuzzleSearchLowerBound.TryCreate(source) : null;
            // Load() calls ContinueWind once before the first animation can accept queued input.
            if (hasWind) initial.AdvanceWind();
            snapshotTemplate = initial;
            string initialKey = PuzzleSearchKeyCodec.Pack(initial.SearchStateKey());
            visited.Add(initialKey, initial.Cost);
            parents.Add(new Parent { previous = -1 });
            Push(Node(initial, initialKey, 0), false);
        }

        public static void Start(PuzzleMap map, Action<Result> completed, int maxVisited = 1000000, bool useHeuristic = true, int maxAdditionalMemoryMB = 3072)
        {
            if (Active != null) throw new InvalidOperationException("A shortest-path search is already running.");
            if (!map || !map.scenario || maxVisited < 1 || maxAdditionalMemoryMB < 1)
                throw new ArgumentException("A valid map, state limit and positive memory budget are required.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Shortest-path search requires Edit Mode.");
            if (SourceIsDirty(map))
                throw new InvalidOperationException("Save the MapData asset before searching so its source hash is reliable.");
            if (map.ValidateMap().Count != 0) throw new ArgumentException("Map validation failed: " + map.ValidateMap()[0]);
            if (map.starwayV3Certificate) throw new ArgumentException("Certified transcript stages require their strict policy solver.");
            LastResult = null;
            Active = new PuzzleShortestPathSolver(map, maxVisited, completed, useHeuristic, maxAdditionalMemoryMB);
            EditorApplication.update += Active.Tick;
        }

        public static void Cancel()
        {
            if (Active != null) Active.Finish("cancelled", "Search cancelled.", -1);
        }

        void Tick()
        {
            // Editor update invocation lists can still contain a callback removed earlier this frame.
            if (finished || Active != this) return;
            var frame = Stopwatch.StartNew();
            try
            {
                if (SourceIsDirty(map))
                {
                    Finish("stale", "MapData has unsaved changes during the search.", -1);
                    return;
                }
                // Include retained search data, after collecting transient resolver allocations.
                // A memory cap is an incomplete search, never proof of unreachability.
                if (GC.GetTotalMemory(false) > maxManagedBytes && GC.GetTotalMemory(true) > maxManagedBytes)
                {
                    Finish("limit", "Additional managed memory budget (" + maxAdditionalMemoryMB +
                        " MiB) reached before the search was exhaustive.", -1);
                    return;
                }
                while (FrontierCount > 0 && frame.ElapsedMilliseconds < 100)
                {
                    var current = Pop();
                    if (current.cost != visited[current.key]) continue;
                    var session = snapshotTemplate.RestoreSearchSnapshot(current.snapshot);
                    if (session.Failed) continue;
                    expanded++;
                    // A goal is final only when popped: zero-cost edges may improve an earlier discovery.
                    if (session.Won) { Finish("solved", "", current.index); return; }
                    for (int i = 0; i < Directions.Length; i++)
                    {
                        var child = session.Copy();
                        if (!child.SearchStep(Directions[i]) || child.Failed) continue;
                        if (!Enqueue(child, current.index, Letters[i], false)) return;
                    }
                    if (hasWind)
                    {
                        // A queued next input takes priority over ContinueWind; waiting is an optional edge.
                        var waiting = session.Copy();
                        if (waiting.AdvanceWind().accepted && !waiting.Failed &&
                            !Enqueue(waiting, current.index, '.', true)) return;
                    }
                }
                if (FrontierCount == 0) Finish("unreachable", "All reachable gameplay states were explored.", -1);
            }
            catch (Exception error) { Finish("error", error.ToString(), -1); }
        }

        bool Enqueue(PuzzleDeviceSession session, int previous, char action, bool zeroCost)
        {
            string key = PuzzleSearchKeyCodec.Pack(session.SearchStateKey());
            if (visited.TryGetValue(key, out int cost) && cost <= session.Cost) return true;
            visited[key] = session.Cost;
            int index = parents.Count;
            parents.Add(new Parent { previous = previous, action = action });
            var node = Node(session, key, index);
            Push(node, zeroCost);
            if (visited.Count < maxVisited) return true;
            Finish("limit", "State limit reached before the search was exhaustive.", -1);
            return false;
        }

        Frontier Node(PuzzleDeviceSession session, string key, int index) => new Frontier
        {
            index = index, key = key, cost = session.Cost,
            priority = session.Cost + (lowerBound?.Estimate(session) ?? 0),
            snapshot = session.CaptureSearchSnapshot()
        };

        // Smaller f first; deeper nodes break equal-f ties without changing shortest-path proof.
        static bool Before(Frontier a, Frontier b) => a.priority < b.priority ||
            a.priority == b.priority && a.cost > b.cost;
        void Push(Frontier node, bool zeroCost)
        {
            if (lowerBound == null)
            {
                if (zeroCost) frontier.AddFirst(node); else frontier.AddLast(node);
                return;
            }
            int index = heap.Count; heap.Add(node);
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Before(node, heap[parent])) break;
                heap[index] = heap[parent]; index = parent;
            }
            heap[index] = node;
        }
        Frontier Pop()
        {
            if (lowerBound == null)
            {
                var first = frontier.First.Value; frontier.RemoveFirst(); return first;
            }
            var result = heap[0]; int last = heap.Count - 1; var tail = heap[last]; heap.RemoveAt(last);
            if (last == 0) return result;
            int index = 0;
            while (index * 2 + 1 < last)
            {
                int child = index * 2 + 1;
                if (child + 1 < last && Before(heap[child + 1], heap[child])) child++;
                if (!Before(heap[child], tail)) break;
                heap[index] = heap[child]; index = child;
            }
            heap[index] = tail; return result;
        }

        void Finish(string status, string detail, int finalIndex)
        {
            if (finished) return;
            finished = true;
            EditorApplication.update -= Tick;
            if (Active == this) Active = null;
            if (map) map.EndReadOnlySearch();
            clock.Stop();
            var route = new StringBuilder();
            for (int index = finalIndex; index > 0; index = parents[index].previous)
                route.Append(parents[index].action);
            var chars = route.ToString().ToCharArray();
            Array.Reverse(chars);
            int inputs = 0;
            foreach (char action in chars) if (action != '.') inputs++;
            if (status == "solved" && (SourceIsDirty(map) || HashAsset(map) != sourceSha256))
            { status = "stale"; detail = "Map asset changed during the search."; chars = Array.Empty<char>(); }
            LastResult = new Result { map = map, sourceSha256 = sourceSha256, route = new string(chars),
                status = status, detail = detail, distance = status == "solved" ? inputs : -1,
                algorithm = AlgorithmName,
                expanded = expanded, visited = visited.Count, elapsedSeconds = clock.Elapsed.TotalSeconds };
            // Release sessions and keys before callbacks import evidence or start the next map.
            frontier.Clear(); heap.Clear(); visited.Clear(); parents.Clear();
            GC.Collect();
            try { completed?.Invoke(LastResult); }
            catch (Exception error) { UnityEngine.Debug.LogException(error); }
        }

        public static string HashAsset(PuzzleMap map)
        {
            var path = AssetDatabase.GetAssetPath(map);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("Map asset is not saved.", path);
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        public static bool SourceIsDirty(PuzzleMap map) => !map || EditorUtility.IsDirty(map) ||
            (map.scenario && EditorUtility.IsDirty(map.scenario));
    }
}
