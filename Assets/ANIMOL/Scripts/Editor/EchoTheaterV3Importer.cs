using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public static class EchoTheaterV3Importer
    {
        public const string Root = "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater";
        public const string SourceRoot = "Assets/ANIMOL/Scripts/Editor/EchoTheaterV3Data";
        public const string SetPath = Root + "/EchoTheaterMapSet.asset";
        const string TemplatePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Scenario.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset";

        static Vector2Int Cell(JToken token) => new Vector2Int((int)token[0], (int)token[1]);
        static Vector2Int Direction(string value) => value == "U" ? Vector2Int.up : value == "D" ? Vector2Int.down :
            value == "L" ? Vector2Int.left : Vector2Int.right;
        static T Upsert<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static PrototypeRole Role(string type) => type == "shutter" ? PrototypeRole.Shutter :
            type == "platform" ? PrototypeRole.MovingStage : type == "conveyor" ? PrototypeRole.Conveyor :
            type == "shelf" || type == "plate" ? PrototypeRole.Bookshelf : type == "oneway" ? PrototypeRole.OneWayGate :
            type == "domino" ? PrototypeRole.Domino : type == "reflector" ? PrototypeRole.Bounce :
            type == "brake" ? PrototypeRole.Brake : type == "dice" || type == "dice_socket" ? PrototypeRole.Dice :
            type == "doll" ? PrototypeRole.CowardDoll : throw new InvalidOperationException("Unknown Echo device " + type);

        public static void InspectVisuals()
        {
            var lines = new List<string>();
            foreach (var id in new[] { "ET-03", "ET-05" })
            {
                var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(Root + "/" + id + "_MapData.asset");
                var session = new PuzzleDeviceSession(map);
                foreach (var d in session.Devices)
                {
                    var view = d.visualPrefab ? d.visualPrefab.GetComponent<DeviceVisualView>() : null;
                    var idle = PuzzleDeviceView.Idle(d, session.States[d.id]);
                    var state = view ? view.states.FirstOrDefault(s => s.name == idle) : null;
                    lines.Add(id + " " + d.id + " " + d.role + " " + d.variant + " idle=" + idle +
                        " mapped=" + (state != null) + " clip=" + (state == null ? "" : state.v03ClipKey));
                }
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines("Logs/et-v3-art.txt", lines);
        }

        public static void Import()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            var template = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(TemplatePath);
            if (!theme || !template) throw new InvalidOperationException("Memory Theater theme/template missing.");
            if (!AssetDatabase.IsValidFolder(Root)) throw new InvalidOperationException("MapData/memory_theater folder missing.");
            var visuals = template.devices.GroupBy(d => d.role).ToDictionary(g => g.Key, g => g.First(d => d.visualPrefab).visualPrefab);
            var socketPrefab = template.devices.First(d => d.role == PrototypeRole.Dice && d.variant == "target").visualPrefab;
            var set = Upsert<EchoTheaterMapSet>(SetPath);
            var entries = new List<EchoTheaterMapSet.Entry>();
            for (var index = 1; index <= 35; index++)
            {
                var id = "ET-" + index.ToString("00");
                var source = AssetDatabase.LoadAssetAtPath<TextAsset>(SourceRoot + "/maps/" + id + ".json");
                if (!source) throw new InvalidOperationException(id + " source missing.");
                var json = JObject.Parse(source.text);
                if ((string)json["schema"] != "animol.echo-theater.design.v3" || (string)json["id"] != id)
                    throw new InvalidOperationException(id + " schema/id mismatch.");
                var map = Upsert<PuzzleMap>(Root + "/" + id + "_MapData.asset");
                var rules = PuzzleEmbeddedScenario.Ensure(map, Root + "/" + id + "_Scenario.asset");
                map.name = id;
                map.Resize((int)json["width"] + 1, (int)json["height"] + 1);
                map.theme = theme; map.scenario = rules;
                map.start = Cell(json["start"]); map.exit = Cell(json["exit"]);
                map.bubbles = ((JArray)json["bubbles"]).Select(Cell).ToList(); map.targetBubbles = 3;
                map.terrain = ((JArray)json["floor"]).Select(p => new TerrainCell(Cell(p), TerrainMaterial.Grass)).ToList();
                // V3 interior walls are holes. Missing terrain remains impassable.
                map.devices.Clear(); map.decorations.Clear(); map.starwayV3Certificate = null;
                map.starwayV3ScenarioSha256 = ""; map.cameraSettings.visibleRows = 11;
                rules.title = (string)json["title"]; rules.themeId = "memory_theater"; rules.echoTheaterV3Rules = true;
                rules.verificationRoute = (string)json["solution"]; rules.costLimit = 0;
                rules.goals.Clear(); rules.water.Clear(); rules.ranchV13Art.Clear(); rules.devices.Clear();
                var serial = 0;
                foreach (var item in (JArray)json["devices"])
                {
                    var type = (string)item["type"];
                    var role = Role(type);
                    var device = new PrototypeDevice
                    {
                        id = (string)item["id"] ?? type + "_" + (++serial), role = role, artTheme = "memory_theater",
                        visualPrefab = type == "dice_socket" ? socketPrefab : visuals[role], position = Cell(item["position"]),
                        direction = Direction((string)item["direction"] ?? (string)item["entry_direction"] ?? "R"),
                        group = (string)item["group"] ?? "", variant = type == "dice_socket" ? "socket" :
                            type == "plate" ? "plate" : type == "reflector" ? ((string)item["orientation"] == "/" ? "slash" : "backslash") : "",
                        orientation = (int?)item["phase"] ?? 0,
                        number = (int?)item["target_top"] ?? 1
                    };
                    if (type == "shutter") device.active = device.orientation == 0;
                    if (type == "dice") device.initialDiceFaces = ((JArray)item["faces"]).Select(n => (int)n).ToArray();
                    if (type == "platform") device.cells = ((JArray)item["track"]).Select(Cell).ToList();
                    rules.devices.Add(device);
                }
                map.InvalidateIndex();
                var issues = map.ValidateMap();
                if (issues.Count > 0) throw new InvalidOperationException(id + ": " + string.Join("; ", issues));
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(rules);
                entries.Add(new EchoTheaterMapSet.Entry { stageId = id, map = map });
            }
            set.maps = entries; EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            foreach (var entry in entries)
                PuzzleEmbeddedScenario.RemoveLegacyAfterReadback(entry.map,
                    Root + "/" + entry.stageId + "_Scenario.asset");
            Debug.Log("[Echo Theater V3] Imported 35 native maps and ordered map set.");
        }
    }
}
