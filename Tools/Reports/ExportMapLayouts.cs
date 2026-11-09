using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Unity Pipeline run_script entry point. Reads existing assets without saving them.
public static class ExportMapLayouts
{
    const string Output = "Docs/Validation/MapLayoutReport";
    static string Hash(string path)
    {
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static JObject Point(Vector2Int p) => new JObject { ["x"] = p.x, ["y"] = p.y };
    static JArray Points(IEnumerable<Vector2Int> cells) => new JArray(cells.Select(Point));
    static Vector2Int[] Footprint(PrototypeDevice d)
    {
        if (d.role == PrototypeRole.Bookshelf && d.variant != "plate")
        {
            var axis = d.orientation % 2 == 0 ? Vector2Int.right : Vector2Int.up;
            return new[] { d.position - axis, d.position, d.position + axis };
        }
        if (d.role == PrototypeRole.VineBridge && d.variant != "plate" && d.cells.Count > 0)
            return d.cells.ToArray();
        return new[] { d.position };
    }
    public static object Run() => Export(Output);
    public static object ForMapDesign() => Export("Docs/Validation/MapDesign");
    static object Export(string output)
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Export from a ready Editor outside Play Mode; no state changes are performed.");
        Directory.CreateDirectory(output);
        var set = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
            "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
        var paths = Enumerable.Range(0, 20).Select(i => "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW" + i.ToString("00") + "_MapData.asset")
            .Concat(Enumerable.Range(0, 6).Select(i => "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-" + i.ToString("00") + "_MapData.asset"));
        var entries = new JArray();
        foreach (string path in paths)
        {
            string id = Path.GetFileNameWithoutExtension(path).Replace("_MapData", "");
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
            var row = new JObject { ["id"] = id, ["assetPath"] = path, ["exists"] = map != null };
            entries.Add(row);
            if (!map) continue;
            bool mapDirty = EditorUtility.IsDirty(map), scenarioDirty = map.scenario && EditorUtility.IsDirty(map.scenario);
            string mapBefore = JsonUtility.ToJson(map), scenarioBefore = map.scenario ? JsonUtility.ToJson(map.scenario) : null;
            string sourceHash = Hash(path), metaHash = Hash(path + ".meta");
            var cells = Enumerable.Range(0, map.Height).SelectMany(y => Enumerable.Range(0, map.Width)
                .Select(x => new Vector2Int(x, y))).ToArray();
            var mapJson = JObject.Parse(mapBefore);
            mapJson["theme"] = AssetDatabase.GetAssetPath(map.theme);
            mapJson["scenario"] = map.scenario ? AssetDatabase.GetAssetPath(map.scenario) : null;
            mapJson["starwayV3Certificate"] = AssetDatabase.GetAssetPath(map.starwayV3Certificate);
            for (int i = 0; i < map.decorations.Count; i++)
            {
                var d = map.decorations[i];
                mapJson["decorations"][i]["sprite"] = AssetDatabase.GetAssetPath(d.sprite);
                mapJson["decorations"][i]["occupiedCells"] = Points(cells.Where(p => RanchConnectedDecorV02Host.Occupies(map, d, p)));
            }
            JObject scenarioJson = scenarioBefore == null ? null : JObject.Parse(scenarioBefore);
            if (map.scenario)
                for (int i = 0; i < map.scenario.devices.Count; i++)
                {
                    var device = map.scenario.devices[i];
                    scenarioJson["devices"][i]["visualPrefab"] = AssetDatabase.GetAssetPath(device.visualPrefab);
                    scenarioJson["devices"][i]["initialFootprint"] = Points(Footprint(device));
                }
            row["name"] = map.name;
            row["guid"] = AssetDatabase.AssetPathToGUID(path);
            row["sourceSha256"] = sourceHash; row["metaSha256"] = metaHash;
            row["mapDirty"] = mapDirty; row["scenarioDirty"] = scenarioDirty;
            row["map"] = mapJson; row["scenario"] = scenarioJson;
            row["staticWalkableCells"] = Points(cells.Where(map.Walkable));
            row["decorationOccupiedCells"] = Points(cells.Where(map.DecorationAt));
            row["displayStages"] = id.StartsWith("SW", StringComparison.Ordinal)
                ? new JArray("SW" + (int.Parse(id.Substring(2)) + 1).ToString("00"))
                : new JArray(set.maps.Where(e => e.map == map).Select(e => e.stageId));
            try { row["validationErrors"] = new JArray(map.ValidateMap()); }
            catch (Exception error) { row["validationErrors"] = new JArray("Validation exception: " + error.Message); }
            if (mapBefore != JsonUtility.ToJson(map) || scenarioBefore != (map.scenario ? JsonUtility.ToJson(map.scenario) : null)
                || mapDirty != EditorUtility.IsDirty(map) || scenarioDirty != (map.scenario && EditorUtility.IsDirty(map.scenario))
                || sourceHash != Hash(path) || metaHash != Hash(path + ".meta"))
                throw new InvalidOperationException("Read-only export changed source state: " + path);
        }
        var result = new JObject {
            ["capturedUtc"] = DateTime.UtcNow.ToString("O"), ["unityVersion"] = Application.unityVersion,
            ["scope"] = "SW00-SW19 and ET-00-ET-05; loaded Editor data; unsaved assets identified explicitly",
            ["sourceAssetsSaved"] = false, ["readOnlyChecksPassed"] = true,
            ["theaterMapSet"] = new JArray(set.maps.Select(e => new JObject { ["stageId"] = e.stageId,
                ["assetPath"] = AssetDatabase.GetAssetPath(e.map) })), ["maps"] = entries
        };
        File.WriteAllText(output + "/unity_snapshot.json", result.ToString(), new System.Text.UTF8Encoding(false));
        return new { existing = entries.Count(e => (bool)e["exists"]), missing = entries.Where(e => !(bool)e["exists"]).Select(e => (string)e["id"]).ToArray(),
            unsaved = entries.Where(e => (bool)e["exists"] && ((bool)e["mapDirty"] || (bool)e["scenarioDirty"])).Select(e => (string)e["id"]).ToArray(),
            output = output + "/unity_snapshot.json", readOnly = true };
    }
}
