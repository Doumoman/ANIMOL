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

// Export committed theater boards and public-API fixtures without saving maps.
// Start once through run_script; each editor update exports one map.
public static class ExportEchoSearch
{
    static string Output, Inventory;
    static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    static readonly Queue<JObject> Pending = new Queue<JObject>();
    static readonly JArray Inputs = new JArray(), Fixtures = new JArray();
    static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false);
    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static string Key(PuzzleDeviceSession state)
    {
        var shutters = state.Devices.Where(d => d.role == PrototypeRole.Shutter).ToArray();
        var phases = shutters.Select(d => state.States[d.id].phase).ToArray();
        var statues = state.Devices.Where(d => d.role == PrototypeRole.Statue).ToArray();
        var timers = statues.Select(d => state.States[d.id].timer).ToArray();
        var glasses = state.Devices.Where(d => d.role == PrototypeRole.Glass).ToArray();
        var headings = glasses.Select(d => state.States[d.id].direction).ToArray();
        try
        {
            foreach (var d in shutters) state.States[d.id].phase &= 1;
            foreach (var d in statues) if (!state.States[d.id].locked) state.States[d.id].timer = 0;
            foreach (var d in glasses) state.States[d.id].direction = Vector2Int.zero;
            return state.SearchStateKey();
        }
        finally
        {
            for (int i = 0; i < shutters.Length; i++) state.States[shutters[i].id].phase = phases[i];
            for (int i = 0; i < statues.Length; i++) state.States[statues[i].id].timer = timers[i];
            for (int i = 0; i < glasses.Length; i++) state.States[glasses[i].id].direction = headings[i];
        }
    }
    public static object Start() => Begin("Temp/EchoThrough18Export", "Docs/Validation/EchoThrough18/inventory.json", 0);
    public static object StartThrough20() => Begin("Temp/EchoThrough20Export", "Docs/Validation/EchoThrough20/inventory.json", 19);
    static object Begin(string output, string inventory, int firstMap)
    {
        if (Pending.Count > 0 || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            PuzzleShortestPathSolver.Active != null) throw new InvalidOperationException("Use an idle Edit Mode editor.");
        Output = output; Inventory = inventory;
        Directory.CreateDirectory(Output);
        Inputs.Clear(); Fixtures.Clear();
        foreach (JObject item in JObject.Parse(File.ReadAllText(Inventory))["maps"])
            if (int.Parse(((string)item["id"]).Substring(3)) >= firstMap) Pending.Enqueue(item);
        File.WriteAllText(Output + "/status.json", "{\"status\":\"running\"}", Utf8);
        EditorApplication.update += Tick;
        return new { maps = Pending.Count, output = Output };
    }
    static void Tick()
    {
        try
        {
            if (Pending.Count == 0)
            {
                File.WriteAllText(Output + "/input.json", Inputs.ToString(Newtonsoft.Json.Formatting.None), Utf8);
                File.WriteAllText(Output + "/fixtures.json", Fixtures.ToString(Newtonsoft.Json.Formatting.None), Utf8);
                File.WriteAllText(Output + "/status.json", new JObject { ["status"] = "completed",
                    ["maps"] = Inputs.Count, ["states"] = Inputs.Count * 400, ["edges"] = Inputs.Count * 1600,
                    ["snapshotAndShutterParityChecked"] = true, ["inactiveStatueTimerEquivalenceChecked"] = true,
                    ["movableGlassHeadingEquivalenceChecked"] = true }.ToString(), Utf8);
                EditorApplication.update -= Tick;
                return;
            }
            Export(Pending.Dequeue());
        }
        catch (Exception error)
        {
            Pending.Clear(); EditorApplication.update -= Tick;
            File.WriteAllText(Output + "/status.json", new JObject { ["status"] = "error",
                ["detail"] = error.ToString() }.ToString(), Utf8);
            Debug.LogException(error);
        }
    }
    static void Export(JObject item)
    {
        string id = (string)item["id"], path = (string)item["path"];
        var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
        var allowed = new[] { PrototypeRole.Shutter, PrototypeRole.MovingStage, PrototypeRole.Domino,
            PrototypeRole.Glass, PrototypeRole.Dice, PrototypeRole.CowardDoll, PrototypeRole.Statue, PrototypeRole.Bookshelf };
        if (!map || !map.scenario || map.ValidateMap().Count != 0 || PuzzleShortestPathSolver.SourceIsDirty(map) ||
            map.scenario.themeId != "memory_theater" || map.scenario.devices.Any(d => !allowed.Contains(d.role)) ||
            map.devices.Any(d => d.kind != DeviceKind.None) || map.starwayV3Certificate ||
            Hash(path) != (string)item["sha256"] || AssetDatabase.AssetPathToGUID(path) != (string)item["guid"])
            throw new InvalidDataException("Unvalidated, changed or unsupported board: " + id);
        string before = JsonUtility.ToJson(map), rulesBefore = JsonUtility.ToJson(map.scenario);
        map.BeginReadOnlySearch();
        try
        {
            var decor = new List<object>();
            for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
                if (map.DecorationAt(new Vector2Int(x, y))) decor.Add(new { x, y });
            Inputs.Add(JObject.FromObject(new { id, assetPath = path, sourceSha256 = Hash(path), validated = true,
                width = map.Width, height = map.Height, start = map.start, exit = map.exit,
                map.targetBubbles, terrain = map.terrain.Select(t => new { t.position, t.blocked }).ToArray(),
                decorations = decor, bubbles = map.bubbles, scenario = JsonUtility.ToJson(map.scenario) }));
            var root = new PuzzleDeviceSession(map); var state = root.Copy();
            var random = new System.Random(181100 + int.Parse(id.Substring(3)));
            var rows = new JArray();
            for (int sample = 0; sample < 400; sample++)
            {
                var restored = root.RestoreSearchSnapshot(state.CaptureSearchSnapshot());
                if (restored.SearchStateKey() != state.SearchStateKey()) throw new InvalidDataException("Snapshot mismatch: " + id);
                var twin = state.Copy();
                foreach (var d in twin.Devices.Where(d => d.role == PrototypeRole.Shutter)) twin.States[d.id].phase += 2;
                foreach (var d in twin.Devices.Where(d => d.role == PrototypeRole.Statue))
                    if (!twin.States[d.id].locked) twin.States[d.id].timer += 97;
                foreach (var d in twin.Devices.Where(d => d.role == PrototypeRole.Glass))
                    twin.States[d.id].direction = twin.States[d.id].direction == Vector2Int.up ? Vector2Int.left : Vector2Int.up;
                var nexts = new JArray(); var valid = new List<int>();
                for (int i = 0; i < Directions.Length; i++)
                {
                    var a = state.Copy(); var b = twin.Copy();
                    bool accepted = a.Apply(Directions[i]).accepted, other = b.Apply(Directions[i]).accepted;
                    if (accepted != other || a.Cost != b.Cost || a.Won != b.Won || a.Failed != b.Failed || Key(a) != Key(b))
                        throw new InvalidDataException("Shutter parity mismatch: " + id);
                    nexts.Add(JObject.FromObject(new { action = "NESW"[i].ToString(), accepted,
                        key = a.SearchStateKey(), cost = a.Cost, won = a.Won, failed = a.Failed }));
                    if (accepted && !a.Failed) valid.Add(i);
                }
                int chosen = valid.Count > 0 ? valid[random.Next(valid.Count)] : random.Next(4);
                var moved = state.Copy(); moved.Apply(Directions[chosen]);
                bool reset = moved.Won || moved.Failed || sample % 50 == 49;
                rows.Add(new JObject { ["key"] = state.SearchStateKey(), ["cost"] = state.Cost,
                    ["successors"] = nexts, ["nextAction"] = "NESW"[chosen].ToString(), ["resetAfter"] = reset });
                state = reset ? root.Copy() : moved;
            }
            Fixtures.Add(new JObject { ["id"] = id, ["rows"] = rows });
        }
        finally { map.EndReadOnlySearch(); }
        if (before != JsonUtility.ToJson(map) || rulesBefore != JsonUtility.ToJson(map.scenario) ||
            Hash(path) != (string)item["sha256"] || PuzzleShortestPathSolver.SourceIsDirty(map))
            throw new InvalidDataException("Export changed source: " + id);
        File.WriteAllText(Output + "/status.json", new JObject { ["status"] = "running",
            ["lastExported"] = id, ["maps"] = Inputs.Count, ["remaining"] = Pending.Count }.ToString(), Utf8);
    }
}
