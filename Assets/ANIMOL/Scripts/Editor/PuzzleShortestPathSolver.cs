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
    /// <summary>Incremental 0-1 BFS: directional input costs one, automatic wind pulses cost zero.</summary>
    public sealed class PuzzleShortestPathSolver
    {
        public sealed class Result
        {
            public PuzzleMap map;
            public string sourceSha256, route, status, detail;
            public int distance, expanded, visited;
            public double elapsedSeconds;
            public bool Solved => status == "solved";
        }

        struct Parent { public int previous; public char action; }
        struct Frontier { public int index; public string key; public PuzzleDeviceSession session; }
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static readonly char[] Letters = { 'N', 'E', 'S', 'W' };
        readonly LinkedList<Frontier> frontier = new LinkedList<Frontier>();
        readonly Dictionary<string, int> visited = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly List<Parent> parents = new List<Parent>();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly Action<Result> completed;
        readonly int maxVisited;
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
        public int FrontierCount => frontier.Count;
        public int SearchDepth => frontier.First?.Value.session.Cost ?? -1;

        PuzzleShortestPathSolver(PuzzleMap source, int stateLimit, Action<Result> onCompleted)
        {
            map = source;
            maxVisited = stateLimit;
            completed = onCompleted;
            sourceSha256 = HashAsset(source);
            foreach (var device in source.scenario.devices)
                if (device.role == PrototypeRole.Wind) { hasWind = true; break; }
            var initial = new PuzzleDeviceSession(source);
            source.BeginReadOnlySearch();
            // Load() calls ContinueWind once before the first animation can accept queued input.
            if (hasWind) initial.AdvanceWind();
            string initialKey = initial.SearchStateKey();
            visited.Add(initialKey, initial.Cost);
            parents.Add(new Parent { previous = -1 });
            frontier.AddLast(new Frontier { index = 0, key = initialKey, session = initial });
        }

        public static void Start(PuzzleMap map, Action<Result> completed, int maxVisited = 1000000)
        {
            if (Active != null) throw new InvalidOperationException("A shortest-path search is already running.");
            if (!map || !map.scenario || maxVisited < 1) throw new ArgumentException("A valid map and state limit are required.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Shortest-path search requires Edit Mode.");
            if (SourceIsDirty(map))
                throw new InvalidOperationException("Save the MapData asset before searching so its source hash is reliable.");
            if (map.ValidateMap().Count != 0) throw new ArgumentException("Map validation failed: " + map.ValidateMap()[0]);
            if (map.starwayV3Certificate) throw new ArgumentException("Certified transcript stages require their strict policy solver.");
            LastResult = null;
            Active = new PuzzleShortestPathSolver(map, maxVisited, completed);
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
                while (frontier.Count > 0 && frame.ElapsedMilliseconds < 100)
                {
                    var current = frontier.First.Value;
                    frontier.RemoveFirst();
                    if (current.session.Cost != visited[current.key] || current.session.Failed) continue;
                    expanded++;
                    // A goal is final only when popped: zero-cost edges may improve an earlier discovery.
                    if (current.session.Won) { Finish("solved", "", current.index); return; }
                    for (int i = 0; i < Directions.Length; i++)
                    {
                        var child = current.session.Copy();
                        if (!child.SearchStep(Directions[i]) || child.Failed) continue;
                        if (!Enqueue(child, current.index, Letters[i], false)) return;
                    }
                    if (hasWind)
                    {
                        // A queued next input takes priority over ContinueWind; waiting is an optional edge.
                        var waiting = current.session.Copy();
                        if (waiting.AdvanceWind().accepted && !waiting.Failed &&
                            !Enqueue(waiting, current.index, '.', true)) return;
                    }
                }
                if (frontier.Count == 0) Finish("unreachable", "All reachable gameplay states were explored.", -1);
            }
            catch (Exception error) { Finish("error", error.ToString(), -1); }
        }

        bool Enqueue(PuzzleDeviceSession session, int previous, char action, bool zeroCost)
        {
            string key = session.SearchStateKey();
            if (visited.TryGetValue(key, out int cost) && cost <= session.Cost) return true;
            visited[key] = session.Cost;
            int index = parents.Count;
            parents.Add(new Parent { previous = previous, action = action });
            var node = new Frontier { index = index, key = key, session = session };
            if (zeroCost) frontier.AddFirst(node); else frontier.AddLast(node);
            if (visited.Count < maxVisited) return true;
            Finish("limit", "State limit reached before the search was exhaustive.", -1);
            return false;
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
                expanded = expanded, visited = visited.Count, elapsedSeconds = clock.Elapsed.TotalSeconds };
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
