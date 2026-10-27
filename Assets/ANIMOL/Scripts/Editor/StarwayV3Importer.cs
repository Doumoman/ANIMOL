using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    // Reads the verified design JSON and creates real, linked Unity assets. Source coordinates are never rescaled.
    public static class StarwayV3Importer
    {
        public const string Source = "Assets/ANIMOL/Data/Puzzle/Starway/V3Source";
        public const string Maps = "Assets/ANIMOL/Data/Puzzle/Starway/Maps/V3";
        public const string SetPath = Maps + "/StarwayV3MapSet.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset";
        const string TemplatePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Scenario.asset";

        [MenuItem("Tools/ANIMOL/Starway V3/Rebuild all 20 maps")]
        public static void RebuildAll()
        {
            CheckHash(Source + "/all_maps.json");
            CheckHash(Source + "/GEOMETRY_AUDIT_REPORT.json");
            CheckHash(Source + "/wrong_branches.json");
            for (var index = 1; index <= 20; index++) Rebuild(index);
            RebuildSet();
            AssetDatabase.SaveAssets();
            Debug.Log("Starway V3: rebuilt 1-2 through 1-21, with three bubbles per map.");
        }

        public static PuzzleMap Rebuild(int index)
        {
            if (index < 1 || index > 20) throw new ArgumentOutOfRangeException(nameof(index));
            var sourceId = "SR" + index.ToString("D2");
            var name = "1-" + (index + 1);
            var scenarioPath = Source + "/scenarios/" + sourceId + ".json";
            var tracePath = Source + "/traces/" + sourceId + ".json";
            var certificatePath = Source + "/certificates/" + sourceId + ".json";
            CheckHash(scenarioPath); CheckHash(tracePath); CheckHash(certificatePath); CheckHash(Source + "/map_shapes.json");
            var json = JObject.Parse(File.ReadAllText(scenarioPath, Encoding.UTF8));
            var trace = JArray.Parse(File.ReadAllText(tracePath, Encoding.UTF8));
            var certificate = JObject.Parse(File.ReadAllText(certificatePath, Encoding.UTF8));
            if (String(certificate, "mapId") != sourceId || String(json, "themeId") != "starway")
                throw new InvalidDataException(sourceId + ": wrong scenario or certificate ID/theme.");
            var scenarioSha = Sha256(Encoding.UTF8.GetBytes(Canonical(json)));
            if (String(certificate, "scenarioHash") != scenarioSha || Int(certificate, "shortestLength") != trace.Count - 1 ||
                Int(certificate, "optimalInputSequenceCount") != 1)
                throw new InvalidDataException(sourceId + ": certificate does not match this scenario/trace.");
            var size = Point(json, "size");
            if (size.x < 16 || size.x > 19 || size.y < 16 || size.y > 19)
                throw new InvalidDataException(sourceId + ": invalid V3 board size.");
            var shapeRows = (JArray)JObject.Parse(File.ReadAllText(Source + "/map_shapes.json", Encoding.UTF8))["maps"];
            var shape = shapeRows.Cast<JObject>().Single(x => String(x, "id") == sourceId);
            if (Point(shape, "size") != size) throw new InvalidDataException(sourceId + ": shape size differs.");
            var shapeCells = Points(shape, "shapeCells").ToHashSet();
            var walkable = Points(json, "walkableCells").ToHashSet();
            if (shapeCells.Count == 0 || !walkable.IsSubsetOf(shapeCells) || shapeCells.Any(p => !Within(p, size)))
                throw new InvalidDataException(sourceId + ": shape and walkable cells disagree.");
            var start = Point(json, "start"); var exit = Point(json, "exit");
            var bubbles = Points(json, "bubbles").ToList();
            if (Int(json, "targetBubbles") != 3 || bubbles.Count != 3 || bubbles.Distinct().Count() != 3 ||
                bubbles.Any(p => !walkable.Contains(p) || p == start || p == exit) ||
                !walkable.Contains(start) || !walkable.Contains(exit))
                throw new InvalidDataException(sourceId + ": V3 requires three distinct walkable bubbles away from start/exit.");
            var deviceRows = Array(json, "devices");
            var ranchRows = Array(json, "ranchV13Art");
            var occupied = new HashSet<Vector2Int>(deviceRows.Cast<JObject>().Select(d => Point(d, "position")));
            foreach (var row in ranchRows.Cast<JObject>())
            {
                occupied.Add(Point(row, "position"));
                if (String(row, "family") == "sluice") occupied.Add(Point(row, "pondB"));
            }
            if (bubbles.Any(occupied.Contains)) throw new InvalidDataException(sourceId + ": bubble overlaps an initial device.");
            if (trace.Count != Int(certificate, "shortestLength") + 1 ||
                (int)trace[0]["state"]["bubbleMask"] != 0 || (int)trace.Last["state"]["bubbleMask"] != 7)
                throw new InvalidDataException(sourceId + ": trace fails the three-bubble requirement.");
            var acquired = new HashSet<int>();
            for (var step = 1; step < trace.Count; step++)
            {
                var before = (int)trace[step - 1]["state"]["bubbleMask"];
                var after = (int)trace[step]["state"]["bubbleMask"];
                if ((before & ~after) != 0) throw new InvalidDataException(sourceId + ": bubble bit was lost.");
                for (var bit = 0; bit < 3; bit++) if ((before & (1 << bit)) == 0 && (after & (1 << bit)) != 0) acquired.Add(bit);
            }
            if (acquired.Count != 3) throw new InvalidDataException(sourceId + ": not all bubbles are acquired by the proof route.");

            Directory.CreateDirectory(Maps); AssetDatabase.Refresh();
            var mapPath = Maps + "/" + name + ".asset";
            var rulePath = Maps + "/" + name + "_Scenario.asset";
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(mapPath);
            if (!map) { map = ScriptableObject.CreateInstance<PuzzleMap>(); AssetDatabase.CreateAsset(map, mapPath); }
            var rules = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(rulePath);
            if (!rules) { rules = ScriptableObject.CreateInstance<PuzzleScenario>(); AssetDatabase.CreateAsset(rules, rulePath); }
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            if (!theme || !theme.starwayRanch || !theme.starwayRanch.v13Devices)
                throw new InvalidDataException("Current Starway theme/V13 catalog is missing.");
            var template = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(TemplatePath);
            if (!template) throw new InvalidDataException("Starway device template is missing.");
            map.Resize(size.x, size.y); map.theme = theme; map.scenario = rules;
            map.start = start; map.exit = exit; map.targetBubbles = 3;
            map.bubbles = bubbles; map.devices.Clear(); map.decorations.Clear();
            map.terrain = shapeCells.OrderBy(p => p.y).ThenBy(p => p.x)
                .Select(p => new TerrainCell(p, walkable.Contains(p) ? TerrainMaterial.Grass : TerrainMaterial.Stone,
                    !walkable.Contains(p))).ToList();
            map.cameraSettings.fitWholeBoard = true;
            map.starwayV3Certificate = AssetDatabase.LoadAssetAtPath<TextAsset>(certificatePath);
            map.starwayV3ScenarioSha256 = scenarioSha;
            if (!map.starwayV3Certificate) throw new InvalidDataException(sourceId + ": certificate TextAsset failed to import.");
            rules.title = name; rules.themeId = "starway";
            rules.verificationRoute = string.Concat(Array(certificate, "steps").Cast<JObject>().Select(x => String(x, "expectedAction")));
            rules.costLimit = Int(json, "costLimit");
            var expectedCost = index <= 5 ? 50 : index <= 15 ? 80 : 120;
            if (rules.costLimit != expectedCost) throw new InvalidDataException(sourceId + ": cost limit differs from tier.");
            rules.devices = deviceRows.Cast<JObject>().Select(row => Device(row, template)).ToList();
            rules.ranchV13Art = ranchRows.Cast<JObject>().Select(Ranch).ToList();
            rules.water = Points(json, "water").ToList();
            rules.goals = Array(json, "goals").Cast<JObject>().Select(Goal).ToList();
            map.InvalidateIndex();
            var errors = map.ValidateMap();
            if (errors.Count != 0) throw new InvalidDataException(sourceId + ": " + string.Join("; ", errors));
            EditorUtility.SetDirty(rules); EditorUtility.SetDirty(map);
            return map;
        }

        static PrototypeDevice Device(JObject row, PuzzleScenario template)
        {
            if (!Enum.TryParse(String(row, "role"), out PrototypeRole role)) throw new InvalidDataException("Unknown role: " + row["role"]);
            var source = template.devices.FirstOrDefault(d => d.role == role);
            if (source == null || !source.visualPrefab)
                source = AssetDatabase.FindAssets("t:PuzzleScenario", new[] { "Assets/ANIMOL/Data/Puzzle/Prototypes" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<PuzzleScenario>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(s => s && s.themeId == "starway").SelectMany(s => s.devices)
                    .FirstOrDefault(d => d.role == role && d.visualPrefab);
            if (source == null || !source.visualPrefab) throw new InvalidDataException("Missing actual visual prefab for " + role);
            var reward = row["socketReward"] == null ? StarSocketReward.None :
                (StarSocketReward)Enum.Parse(typeof(StarSocketReward), String(row, "socketReward"));
            return new PrototypeDevice
            {
                id = String(row, "id"), role = role, variant = row["variant"]?.Value<string>() ?? "",
                position = Point(row, "position"), direction = row["direction"] == null ? Vector2Int.right : Point(row, "direction"),
                orientation = row["orientation"]?.Value<int>() ?? 0, number = row["number"]?.Value<int>() ?? 1,
                socketReward = reward, artTheme = source.artTheme, visualPrefab = source.visualPrefab,
                initialVisual = source.initialVisual, group = String(row, "id")
            };
        }

        static RanchV13ArtPlacement Ranch(JObject row) => new RanchV13ArtPlacement
        {
            instanceId = String(row, "instanceId"), family = String(row, "family"),
            position = Point(row, "position"), direction = row["direction"] == null ? Vector2Int.right : Point(row, "direction"),
            hasPondB = row["hasPondB"]?.Value<bool>() ?? false,
            pondB = row["pondB"] == null ? Vector2Int.zero : Point(row, "pondB"),
            rightState = row["rightState"]?.Value<bool>() ?? false
        };
        static PrototypeGoal Goal(JObject row) => new PrototypeGoal
        {
            deviceId = String(row, "deviceId"), kind = (PrototypeGoalKind)Enum.Parse(typeof(PrototypeGoalKind), String(row, "kind")),
            cell = row["cell"] == null ? Vector2Int.zero : Point(row, "cell"), number = row["number"]?.Value<int>() ?? 0
        };

        static void RebuildSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(SetPath);
            if (!set) { set = ScriptableObject.CreateInstance<StarwayV3MapSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            set.maps.Clear();
            for (var i = 1; i <= 20; i++)
            {
                var sourceId = "SR" + i.ToString("D2"); var name = "1-" + (i + 1);
                set.maps.Add(new StarwayV3MapSet.Entry { sourceId = sourceId, stageName = name,
                    map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(Maps + "/" + name + ".asset"),
                    trace = AssetDatabase.LoadAssetAtPath<TextAsset>(Source + "/traces/" + sourceId + ".json"),
                    certificate = AssetDatabase.LoadAssetAtPath<TextAsset>(Source + "/certificates/" + sourceId + ".json") });
            }
            if (set.maps.Any(e => !e.map || !e.trace || !e.certificate)) throw new InvalidDataException("Incomplete V3 map set.");
            EditorUtility.SetDirty(set);
        }

        static JArray Array(JObject obj, string key) => obj[key] as JArray ?? throw new InvalidDataException("Missing array: " + key);
        static string String(JObject obj, string key) => obj[key]?.Value<string>() ?? throw new InvalidDataException("Missing string: " + key);
        static int Int(JObject obj, string key) => obj[key]?.Value<int>() ?? throw new InvalidDataException("Missing integer: " + key);
        static Vector2Int Point(JObject obj, string key) => Point(obj[key] as JArray ?? throw new InvalidDataException("Missing point: " + key));
        static Vector2Int Point(JArray a) => a.Count == 2 ? new Vector2Int((int)a[0], (int)a[1]) : throw new InvalidDataException("Point needs two coordinates.");
        static IEnumerable<Vector2Int> Points(JObject obj, string key) => Array(obj, key).Cast<JArray>().Select(Point);
        static bool Within(Vector2Int p, Vector2Int size) => p.x >= 0 && p.y >= 0 && p.x < size.x && p.y < size.y;
        static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static string Canonical(JToken token)
        {
            if (token is JObject obj)
                return "{" + string.Join(",", obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => JsonConvert.SerializeObject(p.Name) + ":" + Canonical(p.Value))) + "}";
            if (token is JArray array) return "[" + string.Join(",", array.Select(Canonical)) + "]";
            return token.ToString(Formatting.None);
        }
        static void CheckHash(string assetPath)
        {
            var key = assetPath.Substring(Source.Length + 1).Replace('\\', '/');
            var manifest = JObject.Parse(File.ReadAllText(Source + "/MANIFEST_SHA256.json", Encoding.UTF8));
            var expected = manifest[key]?.Value<string>();
            if (expected == null || !File.Exists(assetPath) || Sha256(File.ReadAllBytes(assetPath)) != expected)
                throw new InvalidDataException("V3 source file missing or hash mismatch: " + key);
        }
    }

    public sealed class StarwayV3SelectionWindow : EditorWindow
    {
        int selected;
        StarwayV3MapSet set;
        [MenuItem("Tools/ANIMOL/Starway V3/Test map selection")]
        static void Open() => GetWindow<StarwayV3SelectionWindow>("Starway V3 maps");
        void OnGUI()
        {
            if (!set) set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(StarwayV3Importer.SetPath);
            if (!set || set.maps.Count != 20) { EditorGUILayout.HelpBox("Build the 20 V3 maps first.", MessageType.Warning); return; }
            selected = EditorGUILayout.Popup("Map", selected, set.maps.Select(e => e.stageName).ToArray());
            EditorGUILayout.LabelField(selected < 5 ? "Easy · 50 inputs" : selected < 15 ? "Medium · 80 inputs" : "Hard · 120 inputs");
            if (GUILayout.Button("Select asset")) Selection.activeObject = set.maps[selected].map;
            if (GUILayout.Button("Rebuild selected map")) { StarwayV3Importer.Rebuild(selected + 1); AssetDatabase.SaveAssets(); }
            if (GUILayout.Button("Play selected map")) PuzzlePreview.Play(set.maps[selected].map);
        }
    }
}
