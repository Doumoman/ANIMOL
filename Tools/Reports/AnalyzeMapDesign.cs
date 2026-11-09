using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Read-only replay measurements, not a new solver or gameplay cost policy.
public static class AnalyzeMapDesign
{
    const string Folder = "Docs/Validation/MapDesign";
    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static JObject Point(Vector2Int p) => new JObject { ["x"] = p.x, ["y"] = p.y };
    static string Category(PuzzleDeviceSession before, PuzzleDeviceSession after, Vector2Int heading,
        PrototypeAction action)
    {
        var adjacent = before.Cell + heading;
        var sheep = before.RanchSheep.FirstOrDefault(p => p.Value == adjacent);
        if (sheep.Key != null && (!after.RanchSheep.TryGetValue(sheep.Key, out var at) || at != sheep.Value)) return "sheep_push";
        if (action.events.Any(e => e.eventName == "PondCrossed")) return "pond_cross";
        foreach (var d in before.Devices.Where(d => before.States[d.id].cell == adjacent && !before.States[d.id].removed))
        {
            var a = before.States[d.id]; var b = after.States[d.id];
            if (d.role == PrototypeRole.Wind && d.variant == "blower" && a.phase != b.phase) return "wind_setting";
            if (d.role == PrototypeRole.Optics && d.variant == "mirror" && a.phase != b.phase) return "mirror_rotate";
            bool changed = a.cell != b.cell || a.removed != b.removed || a.active != b.active || a.converted != b.converted;
            if (!changed) continue;
            if (d.role == PrototypeRole.Dice && d.variant != "target" && d.variant != "socket") return "dice_push";
            if (d.role == PrototypeRole.Domino) return "domino_push";
            if (d.role == PrototypeRole.StarSocket && d.variant != "socket" || d.role == PrototypeRole.WoodCrate ||
                d.role == PrototypeRole.MetalCrate || d.role == PrototypeRole.Barrel || d.role == PrototypeRole.Bomb) return "body_push";
        }
        return "walk_or_other";
    }
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Use a ready Editor outside Play Mode.");
        var snapshot = JObject.Parse(File.ReadAllText(Folder + "/unity_snapshot.json"));
        var catalogs = new JArray();
        foreach (var path in new[] { StarwayShortestPathCatalog.AssetPath,
            "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json" })
            foreach (var entry in JObject.Parse(File.ReadAllText(path))["maps"]) catalogs.Add(entry.DeepClone());
        var reports = new JArray(); int inputs = 0, waits = 0, initialWind = 0;
        foreach (JObject entry in catalogs)
        {
            string id = (string)entry["mapId"], path = (string)entry["mapAsset"];
            var exported = snapshot["maps"].OfType<JObject>().Single(m => (string)m["id"] == id);
            if ((string)entry["status"] != "solved" || Hash(path) != (string)entry["mapSha256"])
                throw new InvalidOperationException("Unconfirmed or stale route: " + id);
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
            string sourceJson = JsonUtility.ToJson(source), rulesJson = JsonUtility.ToJson(source.scenario);
            bool dirty = EditorUtility.IsDirty(source), rulesDirty = EditorUtility.IsDirty(source.scenario);
            string metaHash = Hash(path + ".meta");
            if (Hash(path) != (string)exported["sourceSha256"] || metaHash != (string)exported["metaSha256"] ||
                dirty != (bool)exported["mapDirty"] || rulesDirty != (bool)exported["scenarioDirty"])
                throw new InvalidOperationException("Export changed before analysis: " + id);
            var map = Object.Instantiate(source); var rules = Object.Instantiate(source.scenario); map.scenario = rules;
            var steps = new JArray();
            try
            {
                map.InvalidateIndex(); map.BeginReadOnlySearch();
                var session = new PuzzleDeviceSession(map);
                Action<char, bool> apply = (input, startup) =>
                {
                    bool wait = startup || PuzzleRouteNotation.IsWait(input);
                    var before = session.Copy();
                    var heading = Vector2Int.zero;
                    if (!wait && !PuzzleRouteNotation.TryDirection(input, out heading)) throw new InvalidOperationException("Bad notation");
                    var action = wait ? session.AdvanceWind() : session.Apply(heading);
                    // A load-time wind probe may correctly do nothing.
                    if (startup && !action.accepted) return;
                    if (!action.accepted || session.Failed) throw new InvalidOperationException("Replay rejected: " + id + " input " + session.Cost);
                    var events = action.events.ToList();
                    bool synchronousWind = !wait && !action.events.Any(e => e.kind == PrototypeEventKind.Move);
                    if (synchronousWind)
                    {
                        var extra = session.AdvanceWind(); events.AddRange(extra.events);
                    }
                    if (!wait)
                    {
                        var reference = before.Copy();
                        if (!reference.ReplayRuntimeInput(heading) || reference.SearchStateKey() != session.SearchStateKey() ||
                            reference.Cost != session.Cost || reference.Won != session.Won || reference.Failed != session.Failed)
                            throw new InvalidOperationException("Runtime input replay mismatch: " + id);
                    }
                    if (session.Failed || session.Cost - before.Cost != (wait ? 0 : 1))
                        throw new InvalidOperationException("Unexpected replay cost/failure: " + id);
                    var player = events.Where(e => e.kind == PrototypeEventKind.Move && e.actor == "player").ToArray();
                    var changes = new JArray();
                    foreach (var d in before.Devices)
                    {
                        var a = before.States[d.id]; var b = session.States[d.id];
                        bool changed = a.cell != b.cell || a.active != b.active || a.removed != b.removed || a.locked != b.locked ||
                            a.converted != b.converted || a.phase != b.phase || a.timer != b.timer || !a.dice.SequenceEqual(b.dice) || !a.trail.SequenceEqual(b.trail);
                        if (!changed) continue;
                        var change = new JObject { ["id"] = d.id, ["role"] = d.role.ToString(), ["from"] = Point(a.cell), ["to"] = Point(b.cell),
                            ["activeBefore"] = a.active, ["activeAfter"] = b.active, ["removedBefore"] = a.removed, ["removedAfter"] = b.removed,
                            ["convertedBefore"] = a.converted, ["convertedAfter"] = b.converted,
                            ["lockedBefore"] = a.locked, ["lockedAfter"] = b.locked, ["phaseBefore"] = a.phase, ["phaseAfter"] = b.phase };
                        if (!a.dice.SequenceEqual(b.dice)) change["diceAfter"] = new JArray(b.dice);
                        if (!a.trail.SequenceEqual(b.trail)) change["trailAfter"] = new JArray(b.trail.Select(Point));
                        changes.Add(change);
                    }
                    var row = new JObject { ["sequence"] = steps.Count + 1, ["input"] = startup ? "initial_wind" : input.ToString(),
                        ["category"] = wait ? "free_wind" : Category(before, session, heading, action),
                        ["inputCost"] = session.Cost - before.Cost, ["costAfter"] = session.Cost,
                        ["from"] = Point(before.Cell), ["to"] = Point(session.Cell),
                        ["playerMoveSegments"] = player.Length,
                        ["playerManhattanTravel"] = player.Sum(e => Math.Abs(e.to.x - e.from.x) + Math.Abs(e.to.y - e.from.y)),
                        ["playerPath"] = new JArray(player.Select(e => new JObject { ["from"] = Point(e.from), ["to"] = Point(e.to), ["teleport"] = e.teleport })),
                        ["synchronousWindProbe"] = synchronousWind, ["collectedDelta"] = session.Collected - before.Collected,
                        ["sheepRemoved"] = before.RanchSheep.Count - session.RanchSheep.Count,
                        ["pondChanges"] = new JArray(before.RanchPonds.Where(p => session.RanchPonds[p.Key] != p.Value).Select(p => p.Key)),
                        ["stateChanges"] = changes,
                        ["committedEvents"] = new JArray(events.Where(e => !string.IsNullOrEmpty(e.eventName)).Select(e => new JObject {
                            ["actor"] = e.actor, ["event"] = e.eventName })),
                        ["won"] = session.Won };
                    steps.Add(row);
                    if (startup) initialWind++; else if (wait) waits++; else inputs++;
                };
                if (rules.devices.Any(d => d.role == PrototypeRole.Wind)) apply('.', true);
                foreach (char input in (string)entry["route"]) if (!char.IsWhiteSpace(input)) apply(input, false);
                if (!session.Won || session.Cost != (int)entry["distance"]) throw new InvalidOperationException("Replay did not clear: " + id);
                reports.Add(new JObject { ["id"] = id, ["sourceAsset"] = path, ["sourceSha256"] = Hash(path), ["metaSha256"] = metaHash,
                    ["sourceWasUnsaved"] = dirty || rulesDirty, ["distance"] = session.Cost, ["route"] = entry["route"].DeepClone(),
                    ["won"] = session.Won, ["collected"] = session.Collected, ["target"] = map.targetBubbles,
                    ["exitReached"] = session.Cell == map.exit, ["steps"] = steps });
            }
            finally { map.EndReadOnlySearch(); Object.DestroyImmediate(map); Object.DestroyImmediate(rules); }
            if (sourceJson != JsonUtility.ToJson(source) || rulesJson != JsonUtility.ToJson(source.scenario) ||
                dirty != EditorUtility.IsDirty(source) || rulesDirty != EditorUtility.IsDirty(source.scenario) ||
                Hash(path) != (string)entry["mapSha256"] || Hash(path + ".meta") != metaHash)
                throw new InvalidOperationException("Analysis changed source: " + id);
        }
        var hashes = new JObject();
        foreach (string path in new[] { "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSession.cs",
            "Assets/ANIMOL/Scripts/Runtime/PuzzleScenario.cs", "Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSearchSnapshot.cs",
            "Assets/ANIMOL/Scripts/Runtime/PuzzleRouteNotation.cs", "Tools/Reports/AnalyzeMapDesign.cs" }) hashes[path] = Hash(path);
        var report = new JObject { ["capturedUtc"] = DateTime.UtcNow.ToString("O"), ["unityVersion"] = Application.unityVersion,
            ["scope"] = "Selected proven route per native map; route statistics are not invariants across equally short solutions",
            ["maps"] = reports, ["acceptedDirectionalInputs"] = inputs, ["explicitFreeWindSteps"] = waits,
            ["acceptedInitialWindSteps"] = initialWind, ["engineSourceHashes"] = hashes, ["sourceAssetsUnchanged"] = true };
        File.WriteAllText(Folder + "/route_analysis.json", report.ToString(), new System.Text.UTF8Encoding(false));
        return new { maps = reports.Count, inputs, waits, initialWind, sourceAssetsUnchanged = true };
    }
}
