using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VerifyEchoSearch
{
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    const string Folder = "Docs/Validation/EchoTheaterShortestPaths/";
    static Type Solver => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("EchoTheaterShortestPaths"))
        .Where(t => t != null && t.GetNestedType("RelaxedBound", BindingFlags.NonPublic) != null).Last();
    static string Key(PuzzleDeviceSession state)
    {
        var shutters = state.Devices.Where(d => d.role == PrototypeRole.Shutter).ToArray();
        var values = shutters.Select(d => state.States[d.id].phase).ToArray();
        foreach (var d in shutters) state.States[d.id].phase &= 1;
        string key = state.SearchStateKey();
        for (int i = 0; i < shutters.Length; i++) state.States[shutters[i].id].phase = values[i];
        return key;
    }
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static object IndependentET03Bfs()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling,
            "Use a ready Editor outside Play Mode.");
        var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-03_MapData.asset");
        Directory.CreateDirectory(Folder);
        ANIMOL.Presentation.Editor.PuzzleShortestPathSolver.Start(map, result =>
            File.WriteAllText(Folder + "independent_ET-03_BFS.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(new { result.status, result.distance,
                    result.route, result.visited, result.expanded, result.elapsedSeconds,
                    result.algorithm, result.sourceSha256, result.detail }, Newtonsoft.Json.Formatting.Indented)),
            3000000, false, 3072); // false disables the heuristic; callback only writes evidence.
        return new { started = "ET-03", algorithm = "0-1 BFS", originalMapsSaved = false };
    }
    public static object Transitions()
    {
        var type = Solver.GetNestedType("RelaxedBound", BindingFlags.NonPublic);
        var create = type.GetMethod("Create"); var estimate = type.GetMethod("Estimate");
        int checkedEdges = 0, parityPairs = 0, samples = 0;
        foreach (string id in new[] { "ET-00", "ET-01", "ET-03", "ET-04", "ET-05" })
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/" + id + "_MapData.asset");
            var map = Object.Instantiate(source); var rules = Object.Instantiate(source.scenario); map.scenario = rules;
            try
            {
                map.InvalidateIndex(); map.BeginReadOnlySearch();
                var bound = create.Invoke(null, new object[] { map });
                Require(bound != null, "Expected a relaxed bound for " + id);
                Func<PuzzleDeviceSession, int> h = s => (int)estimate.Invoke(bound, new object[] { s });
                var root = new PuzzleDeviceSession(map); var queue = new Queue<PuzzleDeviceSession>(); queue.Enqueue(root);
                var seen = new HashSet<string> { Key(root) };
                int local = 0;
                while (queue.Count > 0 && local++ < 500)
                {
                    var state = queue.Dequeue(); samples++;
                    var restored = root.RestoreSearchSnapshot(state.CaptureSearchSnapshot());
                    Require(restored.SearchStateKey() == state.SearchStateKey(), "Snapshot mismatch: " + id);
                    var twin = state.Copy();
                    foreach (var d in twin.Devices.Where(d => d.role == PrototypeRole.Shutter)) twin.States[d.id].phase += 2;
                    Require(Key(state) == Key(twin), "Shutter key mismatch");
                    foreach (var input in Directions)
                    {
                        var a = state.Copy(); var b = twin.Copy();
                        bool accepted = a.Apply(input).accepted, twinAccepted = b.Apply(input).accepted;
                        Require(accepted == twinAccepted && a.Won == b.Won && a.Failed == b.Failed && a.Cost == b.Cost
                            && Key(a) == Key(b), "Shutter parity successor mismatch: " + id);
                        parityPairs++; checkedEdges++;
                        if (!accepted || a.Failed) continue;
                        Require(a.Cost == state.Cost + 1 && h(state) <= 1 + h(a), "Native lower-bound inconsistency: " + id);
                        Require(!a.Won || h(a) == 0, "Nonzero goal bound");
                        if (!a.Won && seen.Add(Key(a))) queue.Enqueue(a);
                    }
                }
            }
            finally { map.EndReadOnlySearch(); Object.DestroyImmediate(map); Object.DestroyImmediate(rules); }
        }
        var result = new { passed = true, sampledStates = samples, publicApplyEdges = checkedEdges,
            shutterParitySuccessorPairs = parityPairs, searchSnapshotsRoundTripped = samples,
            nativeLowerBoundConsistency = true, originalMapsSaved = false };
        File.WriteAllText(Folder + "transition_validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    public static object ReplayAll()
    {
        var snapshotMethod = Solver.GetMethod("Snapshot", BindingFlags.NonPublic | BindingFlags.Static);
        var hashMethod = Solver.GetMethod("HashText", BindingFlags.NonPublic | BindingFlags.Static);
        var reports = new JArray();
        foreach (string id in new[] { "ET-00", "ET-01", "ET-03", "ET-04", "ET-05" })
        {
            var result = JObject.Parse(File.ReadAllText(Folder + id + ".json"));
            Require((string)result["status"] == "solved", "Unconfirmed result: " + id);
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>((string)result["sourceAsset"]);
            var snapshot = (JObject)snapshotMethod.Invoke(null, new object[] { source });
            string fingerprint = (string)hashMethod.Invoke(null, new object[] { snapshot.ToString(Newtonsoft.Json.Formatting.None) });
            Require(fingerprint == (string)result["snapshotSha256"], "Current Editor layout changed: " + id);
            bool dirty = EditorUtility.IsDirty(source), rulesDirty = EditorUtility.IsDirty(source.scenario);
            string before = JsonUtility.ToJson(source), rulesBefore = JsonUtility.ToJson(source.scenario);
            var session = new PuzzleDeviceSession(source); string route = (string)result["routeNESW"];
            foreach (char input in route)
            {
                Require(session.Apply(Directions["NESW".IndexOf(input)]).accepted, "Rejected replay input: " + id);
                Require(!session.Failed, "Failed replay: " + id);
            }
            Require(session.Won && session.Cost == (int)result["distance"], "Replay did not clear at its proven cost: " + id);
            Require(before == JsonUtility.ToJson(source) && rulesBefore == JsonUtility.ToJson(source.scenario)
                && dirty == EditorUtility.IsDirty(source) && rulesDirty == EditorUtility.IsDirty(source.scenario), "Replay changed source state");
            reports.Add(new JObject { ["id"] = id, ["inputs"] = session.Cost, ["collected"] = session.Collected,
                ["target"] = source.targetBubbles, ["exitReached"] = session.Cell == source.exit, ["won"] = session.Won,
                ["snapshotMatchesCurrentEditor"] = true, ["sourceDirtyPreserved"] = true });
        }
        File.WriteAllText(Folder + "final_replay.json", reports.ToString(), new System.Text.UTF8Encoding(false));
        return reports;
    }
}
