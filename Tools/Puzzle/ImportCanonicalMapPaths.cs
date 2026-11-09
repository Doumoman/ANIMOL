using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Run Import only after every selected search has finished. Validates all inputs
// and replays every solved route before replacing either evidence catalog.
// No source MapData, scenario, GUID, progress, or scene is saved here.
public static class ImportCanonicalMapPaths
{
    const string Output = "Docs/Validation/CanonicalMapPaths";
    const string CanonicalCommit = "a2f825e09";
    static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static JArray Aliases(PuzzleMap map)
    {
        var aliases = new JArray();
        if (map.scenario.themeId == "starway")
        {
            string id = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(map)).Replace("_MapData", "");
            if (int.TryParse(id.Substring(2), out int number) && number <= 19)
                aliases.Add("SW" + (number + 1).ToString("00"));
        }
        else
        {
            var set = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
            if (set) foreach (var entry in set.maps.Where(e => e.map == map)) aliases.Add(entry.stageId);
        }
        return aliases;
    }
    public static object Import()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || PuzzleShortestPathSolver.Active != null)
            throw new InvalidOperationException("Use a ready Editor outside Play Mode and searches.");
        var inventory = JObject.Parse(File.ReadAllText("Docs/Validation/CanonicalMaps/inventory.json"));
        var sourceFiles = Directory.GetFiles("Assets/ANIMOL/Scripts", "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Tools/Puzzle/ExactSearch", "*.cs")).ToLookup(Path.GetFileName);
        var starway = new JArray(); var echo = new JArray(); var replays = new JArray();
        foreach (JObject entry in inventory["maps"])
        {
            string id = (string)entry["fileId"], path = (string)entry["path"];
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
            if (!map || map.ValidateMap().Count != 0 || PuzzleShortestPathSolver.SourceIsDirty(map) ||
                Hash(path) != (string)entry["sha256"] || AssetDatabase.AssetPathToGUID(path) != (string)entry["guid"])
                throw new InvalidDataException("Canonical source changed: " + id);
            bool ranch = id.StartsWith("SW", StringComparison.Ordinal);
            string proofPath = Output + "/" + (ranch ? "Starway" : "Echo") + "/" + id + ".json";
            var proof = JObject.Parse(File.ReadAllText(proofPath));
            string status = (string)proof["status"], route = (string)proof[ranch ? "route" : "routeNESW"] ?? "";
            int distance = (int)proof["distance"];
            if ((string)proof["sourceSha256"] != (string)entry["sha256"] ||
                !(new[] { "solved", "unreachable", "limit", "cancelled" }).Contains(status))
                throw new InvalidDataException("Invalid source or outcome: " + id);
            if (!ranch && ((bool?)proof["sourceUnchanged"] != true || (bool?)proof["sourceWasUnsaved"] != false))
                throw new InvalidDataException("Native source guard failed: " + id);
            if (ranch)
            {
                var verification = JObject.Parse(File.ReadAllText(Output + "/Starway/verification.json"));
                if ((string)proof["inputSha256"] != Hash(Output + "/Starway/input.json") ||
                    (string)proof["verificationSha256"] != Hash(Output + "/Starway/verification.json") ||
                    !JToken.DeepEquals(proof["sourceHashes"], verification["sourceHashes"]))
                    throw new InvalidDataException("Portable build provenance mismatch: " + id);
                foreach (var source in ((JObject)verification["sourceHashes"]).Properties())
                    if (!sourceFiles[source.Name.Substring("Source.".Length)].Any(p => Hash(p) == (string)source.Value))
                        throw new InvalidDataException("Compiled engine source changed: " + source.Name);
            }
            else foreach (var source in ((JObject)proof["engineSourceHashes"]).Properties())
                if (Hash(source.Name) != (string)source.Value) throw new InvalidDataException("Native engine source changed: " + source.Name);
            var steps = new JArray();
            map.BeginReadOnlySearch();
            try
            {
                if (status == "solved")
                {
                    route = PuzzleRouteNotation.ToArrows(route);
                    if (PuzzleRouteNotation.InputCount(route) != distance) throw new InvalidDataException("Route length: " + id);
                    var state = new PuzzleDeviceSession(map);
                    if (map.scenario.devices.Any(d => d.role == PrototypeRole.Wind)) state.AdvanceWind();
                    foreach (char action in route)
                    {
                        bool accepted;
                        if (PuzzleRouteNotation.IsWait(action)) accepted = state.AdvanceWind().accepted;
                        else
                        {
                            if (!PuzzleRouteNotation.TryDirection(action, out var heading)) throw new InvalidDataException("Unknown action");
                            accepted = ranch ? state.ReplayRuntimeInput(heading) : state.Apply(heading).accepted;
                        }
                        if (!accepted || state.Failed) throw new InvalidDataException("Native replay rejected: " + id + " at " + state.Cost);
                        steps.Add(new JObject { ["action"] = action.ToString(), ["cost"] = state.Cost,
                            ["x"] = state.Cell.x, ["y"] = state.Cell.y, ["collected"] = state.Collected, ["won"] = state.Won });
                    }
                    if (!state.Won || state.Cost != distance || state.Collected < map.targetBubbles)
                        throw new InvalidDataException("Native replay did not clear: " + id);
                }
            }
            finally { map.EndReadOnlySearch(); }
            if (Hash(path) != (string)entry["sha256"] || PuzzleShortestPathSolver.SourceIsDirty(map))
                throw new InvalidDataException("Replay changed source: " + id);
            var record = new JObject { ["mapId"] = id, ["mapAsset"] = path, ["mapGuid"] = (string)entry["guid"],
                ["mapSha256"] = (string)entry["sha256"], ["solverVersion"] = StarwayShortestPathCatalog.SolverVersion,
                ["displayStageIds"] = Aliases(map), ["status"] = status, ["distance"] = distance,
                ["route"] = status == "solved" ? route : "", ["algorithm"] = proof["algorithm"],
                ["expanded"] = proof["expanded"], ["visited"] = proof["visited"],
                ["elapsedSeconds"] = proof["elapsedSeconds"], ["detail"] = proof["detail"], ["evidence"] = proofPath };
            if (!ranch) { record["routeNESW"] = proof["routeNESW"]; record["snapshotSha256"] = proof["snapshotSha256"]; }
            (ranch ? starway : echo).Add(record);
            replays.Add(new JObject { ["mapId"] = id, ["sourceUnchanged"] = true, ["sourceGuid"] = (string)entry["guid"],
                ["sourceSha256"] = (string)entry["sha256"], ["status"] = status,
                ["nativeReplayWon"] = status == "solved", ["distance"] = distance, ["steps"] = steps });
        }
        string starPath = PuzzleShortestPathRecord.Path;
        string echoPath = "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json";
        Directory.CreateDirectory(Output + "/PriorCatalogs");
        if (!File.Exists(Output + "/PriorCatalogs/StarwayShortestPaths.json")) File.Copy(starPath, Output + "/PriorCatalogs/StarwayShortestPaths.json");
        if (!File.Exists(Output + "/PriorCatalogs/EchoTheaterShortestPaths.json")) File.Copy(echoPath, Output + "/PriorCatalogs/EchoTheaterShortestPaths.json");
        File.WriteAllText(Output + "/native_replay.json", new JObject { ["canonicalCommit"] = CanonicalCommit,
            ["unityVersion"] = Application.unityVersion, ["replayedUtc"] = DateTime.UtcNow.ToString("O"),
            ["importerSourceSha256"] = Hash("Tools/Puzzle/ImportCanonicalMapPaths.cs"),
            ["playMode"] = false, ["sourceMapsSaved"] = false, ["maps"] = replays }.ToString() + "\n", Utf8);
        File.WriteAllText(starPath, new JObject { ["formatVersion"] = StarwayShortestPathCatalog.SolverVersion,
            ["canonicalCommit"] = CanonicalCommit, ["algorithm"] = PuzzleShortestPathRecord.Algorithm,
            ["validation"] = Output + "/native_replay.json", ["maps"] = starway }.ToString() + "\n", Utf8);
        File.WriteAllText(echoPath, new JObject { ["formatVersion"] = 2, ["canonicalCommit"] = CanonicalCommit,
            ["scope"] = "Current authored Echo files through ET-11; ET-06 absent",
            ["costModel"] = "One accepted directional input costs 1, including stationary actions",
            ["validation"] = Output + "/native_replay.json", ["maps"] = echo }.ToString() + "\n", Utf8);
        AssetDatabase.ImportAsset(starPath); AssetDatabase.ImportAsset(echoPath);
        return new { starway = starway.Count, echo = echo.Count, solved = replays.Count(r => (string)r["status"] == "solved"),
            validatedSourcesUnchanged = replays.Count, canonicalCommit = CanonicalCommit };
    }
}
