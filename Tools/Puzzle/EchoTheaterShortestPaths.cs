using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Run Start/Status with Unity Pipeline run_script. Does not save source assets.
// Every edge uses the actual PuzzleDeviceSession. The supported boards have no wind.
public static class EchoTheaterShortestPaths
{
    const string Output = "Docs/Validation/EchoTheaterShortestPaths";
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    static readonly string[] Ids = { "ET-00", "ET-01", "ET-03", "ET-04", "ET-05" };
    static Job current;
    static readonly Queue<string> pending = new Queue<string>();
    static string Hash(byte[] bytes) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static string HashText(string text) => Hash(System.Text.Encoding.UTF8.GetBytes(text));
    static JObject Snapshot(PuzzleMap map)
    {
        var data = JObject.Parse(JsonUtility.ToJson(map));
        data["theme"] = AssetDatabase.GetAssetPath(map.theme);
        data.Remove("scenario"); data["starwayV3Certificate"] = AssetDatabase.GetAssetPath(map.starwayV3Certificate);
        var occupied = new JArray();
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
            if (map.DecorationAt(new Vector2Int(x, y))) occupied.Add(new JArray(x, y));
        data["decorations"] = occupied;
        var rules = JObject.Parse(JsonUtility.ToJson(map.scenario)); rules.Remove("beamPrefab");
        foreach (JObject d in rules["devices"]) { d.Remove("visualPrefab"); d.Remove("initialVisual"); }
        data["scenarioData"] = rules;
        return data;
    }
    static string PathFor(string id) => "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/" + id + "_MapData.asset";
    sealed class Node
    {
        public PuzzleDeviceSession.SearchSnapshot state;
        public int index, cost, priority;
        public string key;
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
                PrototypeRole.Glass, PrototypeRole.Dice, PrototypeRole.CowardDoll, PrototypeRole.Statue };
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
            var tracks = map.scenario.devices.Where(d => d.role == PrototypeRole.MovingStage)
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
    sealed class Job
    {
        public readonly string id, path, assetHash, snapshotHash, mapJson, scenarioJson;
        public readonly PuzzleMap source, map;
        public readonly PuzzleScenario rules;
        public readonly bool sourceDirty, scenarioDirty;
        public readonly JObject snapshot;
        public readonly PuzzleDeviceSession template;
        public readonly List<Node> queue = new List<Node>();
        public readonly Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly RelaxedBound bound;
        public readonly List<int> parents = new List<int>();
        public readonly List<char> actions = new List<char>();
        public readonly Stopwatch clock = Stopwatch.StartNew();
        public readonly string[] shutters;
        public readonly long baseline;
        public int expanded, depth;
        public long lastProgress;
        public const int StateLimit = 3000000;
        public const long MemoryMiB = 3072;
        public Job(string name)
        {
            id = name; path = PathFor(id); source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
            if (!source || !source.scenario || source.ValidateMap().Count != 0 || source.starwayV3Certificate)
                throw new InvalidOperationException("Invalid native map: " + id);
            if (source.scenario.devices.Any(d => d.role == PrototypeRole.Wind) || source.devices.Any(d => d.kind == DeviceKind.Wind))
                throw new InvalidOperationException("Unit-cost theater BFS does not accept free wind edges.");
            sourceDirty = EditorUtility.IsDirty(source); scenarioDirty = EditorUtility.IsDirty(source.scenario);
            assetHash = Hash(File.ReadAllBytes(path));
            mapJson = JsonUtility.ToJson(source); scenarioJson = JsonUtility.ToJson(source.scenario);
            snapshot = Snapshot(source); snapshotHash = HashText(snapshot.ToString(Newtonsoft.Json.Formatting.None));
            File.WriteAllText(Output + "/" + id + "_input.json", snapshot.ToString(), new System.Text.UTF8Encoding(false));
            map = Object.Instantiate(source); rules = Object.Instantiate(source.scenario); map.scenario = rules;
            map.InvalidateIndex(); map.BeginReadOnlySearch();
            template = new PuzzleDeviceSession(map);
            bound = RelaxedBound.Create(map);
            shutters = template.Devices.Where(d => d.role == PrototypeRole.Shutter).Select(d => d.id).ToArray();
            parents.Add(-1); actions.Add('\0'); string key = Key(template); seen.Add(key, 0);
            Push(new Node { state = template.CaptureSearchSnapshot(), index = 0, cost = 0, priority = Estimate(template), key = key });
            baseline = GC.GetTotalMemory(true);
        }
        public string Key(PuzzleDeviceSession state)
        {
            // Only shutter parity is read by gameplay; the unbounded turn counter
            // must not create infinitely many copies of the same board state.
            var previous = new int[shutters.Length];
            for (int i = 0; i < shutters.Length; i++)
            { previous[i] = state.States[shutters[i]].phase; state.States[shutters[i]].phase &= 1; }
            string key = PuzzleSearchKeyCodec.Pack(state.SearchStateKey());
            for (int i = 0; i < shutters.Length; i++) state.States[shutters[i]].phase = previous[i];
            return key;
        }
        public int Estimate(PuzzleDeviceSession state) => bound?.Estimate(state) ?? 0;
        static bool Before(Node a, Node b) => a.priority < b.priority || a.priority == b.priority && a.cost > b.cost;
        public void Push(Node node)
        {
            int i = queue.Count; queue.Add(node);
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Before(node, queue[parent])) break;
                queue[i] = queue[parent]; i = parent;
            }
            queue[i] = node;
        }
        public Node Pop()
        {
            var first = queue[0]; int last = queue.Count - 1; var tail = queue[last]; queue.RemoveAt(last);
            if (last == 0) return first;
            int i = 0;
            while (i * 2 + 1 < last)
            {
                int child = i * 2 + 1;
                if (child + 1 < last && Before(queue[child + 1], queue[child])) child++;
                if (!Before(queue[child], tail)) break;
                queue[i] = queue[child]; i = child;
            }
            queue[i] = tail; return first;
        }
        public void Close()
        {
            map.EndReadOnlySearch(); Object.DestroyImmediate(map); Object.DestroyImmediate(rules);
            queue.Clear(); seen.Clear(); parents.Clear(); actions.Clear();
        }
    }
    public static object Start()
    {
        if (current != null || pending.Count > 0 || PuzzleShortestPathSolver.Active != null)
            throw new InvalidOperationException("A search is already running.");
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Use a ready Editor outside Play Mode.");
        Directory.CreateDirectory(Output);
        foreach (string id in Ids) pending.Enqueue(id);
        EditorApplication.update += Tick;
        return new { queued = Ids, excluded = "ET-02 native asset is absent; the ET-02 display stage maps to ET-00", output = Output };
    }
    public static object Status() => new { active = current?.id, expanded = current?.expanded,
        visited = current?.seen.Count, frontier = current?.queue.Count, depth = current?.depth,
        frontierLowerBound = current != null && current.queue.Count > 0 ? current.queue[0].priority : -1,
        seconds = current?.clock.Elapsed.TotalSeconds, pending = pending.ToArray() };
    static void Tick()
    {
        try
        {
            if (current == null)
            {
                if (pending.Count == 0) { EditorApplication.update -= Tick; return; }
                current = new Job(pending.Dequeue());
            }
            var frame = Stopwatch.StartNew();
            while (current.queue.Count > 0 && frame.ElapsedMilliseconds < 70)
            {
                var node = current.Pop();
                if (current.seen[node.key] != node.cost) continue;
                var state = current.template.RestoreSearchSnapshot(node.state);
                current.expanded++; current.depth = state.Cost;
                if (state.Won) { Finish("solved", node.index, "Minimum-f goal popped; admissible cart-aware bound and reopen-on-improvement preserve optimality."); return; }
                foreach (var heading in Directions.Select((direction, index) => new { direction, index }))
                {
                    var child = state.Copy();
                    if (!child.SearchStep(heading.direction) || child.Failed) continue;
                    if (child.Cost != state.Cost + 1) throw new InvalidOperationException("Unexpected non-unit edge.");
                    int h = current.Estimate(child);
                    if (current.Estimate(state) > 1 + h || child.Won && h != 0)
                        throw new InvalidOperationException("Relaxed lower bound failed native-edge consistency.");
                    string key = current.Key(child);
                    if (current.seen.TryGetValue(key, out int previous) && previous <= child.Cost) continue;
                    current.seen[key] = child.Cost;
                    int index = current.parents.Count;
                    current.parents.Add(node.index); current.actions.Add("NESW"[heading.index]);
                    current.Push(new Node { state = child.CaptureSearchSnapshot(), index = index,
                        key = key, cost = child.Cost, priority = child.Cost + h });
                    if (current.seen.Count >= Job.StateLimit) { Finish("limit", -1, "State cap reached; shortest result remains unconfirmed."); return; }
                }
            }
            if (current.queue.Count == 0) { Finish("unreachable", -1, "Finite canonical state queue exhausted."); return; }
            if (current.clock.ElapsedMilliseconds - current.lastProgress > 1000)
            {
                current.lastProgress = current.clock.ElapsedMilliseconds;
                File.WriteAllText(Output + "/progress.json", Newtonsoft.Json.JsonConvert.SerializeObject(Status(), Newtonsoft.Json.Formatting.Indented));
                if (GC.GetTotalMemory(false) - current.baseline > Job.MemoryMiB * 1048576L &&
                    GC.GetTotalMemory(true) - current.baseline > Job.MemoryMiB * 1048576L)
                    Finish("limit", -1, "Additional managed-memory cap reached; shortest result remains unconfirmed.");
            }
        }
        catch (Exception error)
        {
            if (current != null) Finish("error", -1, error.ToString());
            else { pending.Clear(); EditorApplication.update -= Tick; UnityEngine.Debug.LogException(error); }
        }
    }
    static void Finish(string status, int goal, string detail)
    {
        var job = current;
        string route = "";
        if (goal >= 0)
        {
            var reversed = new List<char>();
            for (int index = goal; index > 0; index = job.parents[index]) reversed.Add(job.actions[index]);
            reversed.Reverse(); route = new string(reversed.ToArray());
        }
        bool unchanged = job.mapJson == JsonUtility.ToJson(job.source) && job.scenarioJson == JsonUtility.ToJson(job.source.scenario)
            && job.assetHash == Hash(File.ReadAllBytes(job.path)) && job.sourceDirty == EditorUtility.IsDirty(job.source)
            && job.scenarioDirty == EditorUtility.IsDirty(job.source.scenario);
        if (!unchanged) { status = "stale"; detail = "Source asset or loaded Editor state changed during search."; route = ""; }
        var replay = new JArray();
        bool replayWon = false;
        if (status == "solved")
        {
            var state = new PuzzleDeviceSession(job.map);
            for (int i = 0; i < route.Length; i++)
            {
                var action = state.Apply(Directions["NESW".IndexOf(route[i])]);
                if (!action.accepted || state.Failed) throw new InvalidOperationException("Public Apply replay rejected input " + i);
                replay.Add(new JObject { ["step"] = i + 1, ["input"] = route[i].ToString(), ["x"] = state.Cell.x,
                    ["y"] = state.Cell.y, ["cost"] = state.Cost, ["collected"] = state.Collected, ["won"] = state.Won });
            }
            replayWon = state.Won && state.Cost == route.Length;
            if (!replayWon) throw new InvalidOperationException("Replay did not clear at the proven distance.");
        }
        var hashes = new JObject();
        foreach (string file in new[] { "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSession.cs", "Assets/ANIMOL/Scripts/Runtime/PuzzleScenario.cs",
            "Assets/ANIMOL/Scripts/Runtime/BookshelfRotationRules.cs",
            "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSearchSnapshot.cs", "Assets/ANIMOL/Scripts/Runtime/OneCellGateRules.cs",
            "Tools/Puzzle/EchoTheaterShortestPaths.cs" }) hashes[file] = Hash(File.ReadAllBytes(file));
        var record = new JObject { ["mapId"] = job.id, ["sourceAsset"] = job.path, ["sourceSha256"] = job.assetHash,
            ["snapshotSha256"] = job.snapshotHash, ["sourceWasUnsaved"] = job.sourceDirty || job.scenarioDirty,
            ["status"] = status, ["distance"] = status == "solved" ? route.Length : -1,
            ["routeNESW"] = route, ["route"] = string.Concat(route.Select(c => "↑→↓←"["NESW".IndexOf(c)])),
            ["expanded"] = job.expanded, ["visited"] = job.seen.Count, ["frontierAtFinish"] = job.queue.Count,
            ["elapsedSeconds"] = job.clock.Elapsed.TotalSeconds, ["stateLimit"] = Job.StateLimit,
            ["additionalMemoryMiB"] = Job.MemoryMiB, ["algorithm"] = "Exact A* over native PuzzleDeviceSession, cart-aware relaxed bound, shutter phase modulo 2 in keys only",
            ["detail"] = detail, ["publicApplyReplayWon"] = replayWon, ["sourceUnchanged"] = unchanged,
            ["engineSourceHashes"] = hashes, ["replay"] = replay };
        File.WriteAllText(Output + "/" + job.id + ".json", record.ToString(), new System.Text.UTF8Encoding(false));
        UnityEngine.Debug.Log(job.id + " exact search: " + status + ", inputs=" + (status == "solved" ? route.Length : -1));
        current = null; job.Close(); GC.Collect();
    }
}
