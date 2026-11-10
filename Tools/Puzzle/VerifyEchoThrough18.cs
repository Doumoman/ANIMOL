using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Replay source-linked search results through the real Unity public input API.
// Does not save or edit authored MapData, scenarios, metadata or player progress.
public static class VerifyEchoThrough18
{
    const string Folder = "Docs/Validation/EchoThrough18";
    static readonly Queue<JObject> Pending = new Queue<JObject>();
    static readonly JArray Reports = new JArray();
    static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false);
    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    public static object Start()
    {
        if (Pending.Count > 0 || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Use an idle Edit Mode editor.");
        var inventory = JObject.Parse(File.ReadAllText(Folder + "/inventory.json"));
        var validations = Directory.GetFiles(Folder, "transition_verification*.json")
            .GroupBy(Hash).ToDictionary(group => group.Key, group => JObject.Parse(File.ReadAllText(group.First())));
        var files = Directory.GetFiles("Assets/ANIMOL/Scripts", "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Tools/Puzzle/ExactSearch", "*.cs")).ToLookup(Path.GetFileName);
        var usedBuilds = new HashSet<string>(inventory["maps"].Select(item =>
            (string)JObject.Parse(File.ReadAllText(Folder + "/Search/" + item["id"] + ".json"))["verificationSha256"]));
        foreach (var verified in validations.Where(pair => usedBuilds.Contains(pair.Key)).Select(pair => pair.Value))
        foreach (JProperty pair in ((JObject)verified["sourceHashes"]).Properties())
        {
            var matches = files[pair.Name.Substring("Source.".Length)].ToArray();
            bool current = matches.Count(path => Hash(path) == (string)pair.Value) == 1;
            // Earlier completed searches keep their exact queue implementation
            // for reproduction. Runtime engine sources must always be current.
            bool archivedTool = (pair.Name == "Source.Program.cs" || pair.Name == "Source.EchoExactSolver.cs") &&
                Hash(Folder + "/InitialBuildSources/" + pair.Name.Substring("Source.".Length) + ".txt") == (string)pair.Value;
            if (!(bool)verified["passed"] || !current && !archivedTool)
                throw new InvalidDataException("Search source changed: " + pair.Name);
        }
        foreach (JObject item in inventory["maps"])
        {
            string path = Folder + "/Search/" + item["id"] + ".json";
            var result = JObject.Parse(File.ReadAllText(path));
            if ((string)result["sourceSha256"] != (string)item["sha256"] ||
                !validations.TryGetValue((string)result["verificationSha256"], out var verified) ||
                !JToken.DeepEquals(result["sourceHashes"], verified["sourceHashes"]))
                throw new InvalidDataException("Result provenance mismatch: " + item["id"]);
        }
        Reports.Clear();
        foreach (JObject item in inventory["maps"]) Pending.Enqueue(item);
        Directory.CreateDirectory(Folder + "/Replay");
        File.WriteAllText(Folder + "/replay_status.json", "{\"status\":\"running\"}", Utf8);
        EditorApplication.update += Tick;
        return new { queued = Pending.Count };
    }
    static void Tick()
    {
        try
        {
            if (Pending.Count == 0)
            {
                File.WriteAllText(Folder + "/native_replay.json", Reports.ToString(), Utf8);
                File.WriteAllText(Folder + "/replay_status.json", new JObject { ["status"] = "completed",
                    ["maps"] = Reports.Count, ["solvedReplayed"] = Reports.Count(r => (string)r["status"] == "solved") }.ToString(), Utf8);
                EditorApplication.update -= Tick;
                return;
            }
            Replay(Pending.Dequeue());
        }
        catch (Exception error)
        {
            Pending.Clear(); EditorApplication.update -= Tick;
            File.WriteAllText(Folder + "/replay_status.json", new JObject { ["status"] = "error",
                ["detail"] = error.ToString() }.ToString(), Utf8);
            Debug.LogException(error);
        }
    }
    static void Replay(JObject item)
    {
        string id = (string)item["id"], path = (string)item["path"];
        var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
        if (!map || PuzzleShortestPathSolver.SourceIsDirty(map) || map.ValidateMap().Count != 0 ||
            Hash(path) != (string)item["sha256"] || Hash(path + ".meta") != (string)item["metaSha256"] ||
            AssetDatabase.AssetPathToGUID(path) != (string)item["guid"])
            throw new InvalidDataException("Authored map changed: " + id);
        string before = JsonUtility.ToJson(map), scenarioBefore = JsonUtility.ToJson(map.scenario);
        var result = JObject.Parse(File.ReadAllText(Folder + "/Search/" + id + ".json"));
        string status = (string)result["status"];
        var report = new JObject { ["id"] = id, ["status"] = status, ["sourceAsset"] = path,
            ["mapGuid"] = (string)item["guid"], ["sourceSha256"] = (string)item["sha256"] };
        var set = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
            "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
        report["displayStageIds"] = new JArray(set ? set.maps.Where(e => e.map == map).Select(e => e.stageId) : Array.Empty<string>());
        if (status == "solved")
        {
            string route = (string)result["route"];
            if (PuzzleRouteNotation.InputCount(route) != (int)result["distance"])
                throw new InvalidDataException("Input count mismatch: " + id);
            var state = new PuzzleDeviceSession(map);
            var steps = new JArray(); var letters = new System.Text.StringBuilder();
            foreach (char action in route)
            {
                if (!PuzzleRouteNotation.TryDirection(action, out var direction) || !state.ReplayRuntimeInput(direction) || state.Failed)
                    throw new InvalidDataException("Public runtime replay rejected " + id + " at input " + (steps.Count + 1));
                letters.Append(direction == Vector2Int.up ? 'N' : direction == Vector2Int.right ? 'E' : direction == Vector2Int.down ? 'S' : 'W');
                steps.Add(new JObject { ["step"] = steps.Count + 1, ["input"] = action.ToString(),
                    ["x"] = state.Cell.x, ["y"] = state.Cell.y, ["cost"] = state.Cost,
                    ["collected"] = state.Collected, ["won"] = state.Won });
            }
            if (!state.Won || state.Cost != (int)result["distance"] || state.Cell != map.exit || state.Collected < map.targetBubbles)
                throw new InvalidDataException("Replay did not win at the proven cost: " + id);
            report["distance"] = state.Cost; report["route"] = route; report["routeNESW"] = letters.ToString();
            report["collected"] = state.Collected; report["target"] = map.targetBubbles;
            report["won"] = state.Won; report["exitReached"] = true; report["steps"] = steps;
        }
        else
        {
            if (status != "unreachable" && status != "limit" && status != "cancelled" && status != "stale")
                throw new InvalidDataException("Unknown search status: " + id + ": " + status);
            report["distance"] = -1; report["replaySkipped"] = "No confirmed winning route; no success claim.";
        }
        if (before != JsonUtility.ToJson(map) || scenarioBefore != JsonUtility.ToJson(map.scenario) ||
            Hash(path) != (string)item["sha256"] || Hash(path + ".meta") != (string)item["metaSha256"] ||
            PuzzleShortestPathSolver.SourceIsDirty(map)) throw new InvalidDataException("Replay changed source: " + id);
        report["sourceUnchanged"] = true;
        File.WriteAllText(Folder + "/Replay/" + id + ".json", report.ToString(), Utf8);
        Reports.Add(report);
    }
}
