using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.StarwayMaps;
using ANIMOL.StarwayMaps.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public sealed class Starway20ProjectAdapter : StarwayNativeAdapter
    {
        public const string Root = "Assets/ANIMOL/Data/Puzzle/MapData/starway";
        public const string SetPath = Root + "/Starway20MapSet.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset";
        public override string DisplayName => "ANIMOL native PuzzleMap adapter";
        public override string[] OwnedAssetRoots => new[] { Root };
        static Vector2Int Cell(StarwayCell value) => value.ToVector2Int();
        public static string MapPath(string id) => Root + "/" + id + "_MapData.asset";
        static readonly HashSet<string> AuthoredStageIds = new HashSet<string> { "SW01", "SW02", "SW03" };
        static string ScenarioPath(string id) => Root + "/" + id + "_Scenario.asset";
        static PuzzleTheme Theme => AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);

        static Dictionary<PrototypeRole, PrototypeDevice> Visuals()
        {
            var result = new Dictionary<PrototypeRole, PrototypeDevice>();
            foreach (PrototypeRole role in Enum.GetValues(typeof(PrototypeRole)))
            {
                var device = StarwayV3Importer.FindVisualTemplate(role);
                if (device != null) result.Add(role, device);
            }
            return result;
        }

        public override void Preflight(StarwayMapSource[] sources, List<string> errors)
        {
            if (sources == null || sources.Length != 20) { errors.Add("Expected 20 sources."); return; }
            if (!Theme || !Theme.starwayRanch || !Theme.starwayRanch.v13Devices) errors.Add("Starway V13 theme is missing.");
            var visuals = Visuals();
            for (int i = 0; i < sources.Length; i++)
            {
                var m = sources[i].input.map;
                if (m.id != "SW" + (i + 1).ToString("00") || m.themeId != "starway") errors.Add("Wrong source identity: " + m.id);
                if (m.targetBubbles != 3 || m.bubbles.Length != 3 || m.goals.Length != 0 || m.costLimit != 0) errors.Add(m.id + ": invalid victory fields.");
                foreach (var d in m.devices)
                    if (!Enum.IsDefined(typeof(PrototypeRole), d.role) || !visuals.ContainsKey((PrototypeRole)d.role))
                        errors.Add(m.id + ": no verified visual for role " + d.role);
                foreach (var r in m.ranchV13Art)
                    if (r.family != "sheep" && r.family != "tall_grass") errors.Add(m.id + ": unknown ranch family " + r.family);
                foreach (var s in m.ranchSluiceDesign)
                    if (!s.hasPondB || (Cell(s.position) - Cell(s.pondB)).sqrMagnitude != 1)
                        errors.Add(m.id + ": invalid sluice pair " + s.instanceId);
                var path = MapPath(m.id);
                if (AssetDatabase.LoadMainAssetAtPath(path) &&
                    !(AssetDatabase.LoadMainAssetAtPath(path) is PuzzleMap))
                    errors.Add("Unrelated asset at " + path);
            }
        }

        static void EnsureRoot()
        {
            const string parent = "Assets/ANIMOL/Data/Puzzle/MapData";
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets/ANIMOL/Data/Puzzle", "MapData");
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(parent, "starway");
        }
        static T Upsert<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value) return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        public override void BuildOrUpdateMaps(StarwayMapSource[] sources)
        {
            EnsureRoot();
            var visuals = Visuals();
            foreach (var source in sources)
            {
                var input = source.input;
                var m = input.map;
                // Keep the maps edited in the native map editor when rebuilding the old source pack.
                var existing = AssetDatabase.LoadAssetAtPath<PuzzleMap>(MapPath(m.id));
                if (AuthoredStageIds.Contains(m.id) && existing && existing.scenario &&
                    existing.scenario.verificationRoute != input.referenceActions) continue;
                var map = Upsert<PuzzleMap>(MapPath(m.id));
                var scenario = PuzzleEmbeddedScenario.Ensure(map, ScenarioPath(m.id));
                map.Resize(m.size.width, m.size.height);
                map.theme = Theme; map.scenario = scenario;
                map.start = Cell(m.start); map.exit = Cell(m.exit);
                map.targetBubbles = 3;
                map.bubbles = m.bubbles.Select(Cell).ToList();
                map.terrain = m.regionCells.Select(c => new TerrainCell(Cell(c),
                    m.walkableCells.Contains(c) ? TerrainMaterial.Grass : TerrainMaterial.Stone,
                    !m.walkableCells.Contains(c))).ToList();
                map.devices.Clear(); map.decorations.Clear(); map.starwayV3Certificate = null;
                map.starwayV3ScenarioSha256 = "";
                map.cameraSettings.visibleRows = 11; map.cameraSettings.fitWholeBoard = false;
                scenario.title = m.title; scenario.themeId = "starway";
                scenario.verificationRoute = input.referenceActions;
                scenario.costLimit = 0;
                scenario.goals.Clear(); scenario.water = m.water.Select(Cell).ToList();
                scenario.devices = m.devices.Select(d =>
                {
                    var art = visuals[(PrototypeRole)d.role];
                    return new PrototypeDevice { id = d.id, role = (PrototypeRole)d.role, variant = d.variant ?? "",
                        group = d.id, artTheme = art.artTheme, visualPrefab = art.visualPrefab,
                        initialVisual = art.initialVisual, position = Cell(d.position), direction = Cell(d.direction),
                        target = Cell(d.target), number = d.number, orientation = d.orientation,
                        socketReward = StarSocketReward.None };
                }).ToList();
                scenario.ranchV13Art = m.ranchV13Art.Select(r => new RanchV13ArtPlacement
                { instanceId = r.instanceId, family = r.family, position = Cell(r.position), direction = Vector2Int.right }).ToList();
                scenario.ranchV13Art.AddRange(m.ranchSluiceDesign.Select(s => new RanchV13ArtPlacement
                { instanceId = s.instanceId, family = "sluice", position = Cell(s.position),
                    pondB = Cell(s.pondB), hasPondB = s.hasPondB, direction = Cell(s.pondB) - Cell(s.position), rightState = s.rightState }));
                map.InvalidateIndex();
                var issues = map.ValidateMap();
                if (issues.Count != 0) throw new InvalidOperationException(m.id + ": " + string.Join("; ", issues));
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(scenario);
            }
            AssetDatabase.SaveAssets();
            foreach (var source in sources)
                PuzzleEmbeddedScenario.RemoveLegacyAfterReadback(
                    AssetDatabase.LoadAssetAtPath<PuzzleMap>(MapPath(source.input.map.id)),
                    ScenarioPath(source.input.map.id));
        }

        public override StarwayNativeLink[] ReadNativeLinks(StarwayMapSource[] sources) => sources.Select(s => new StarwayNativeLink
        {
            id = s.input.map.id, map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(MapPath(s.input.map.id)),
            scenario = AssetDatabase.LoadAssetAtPath<PuzzleMap>(MapPath(s.input.map.id)).scenario
        }).ToArray();

        public override void ValidateNativeMap(StarwayMapSource source, StarwayNativeLink native, List<string> errors)
        {
            var m = source.input.map;
            var map = native.map as PuzzleMap;
            var rules = native.scenario as PuzzleScenario;
            if (!map || !rules) { errors.Add(m.id + ": missing native asset."); return; }
            if (AuthoredStageIds.Contains(m.id) && rules.verificationRoute != source.input.referenceActions)
            {
                if (map.theme != Theme || map.scenario != rules || rules.themeId != "starway" ||
                    map.targetBubbles != 3 || rules.goals.Count != 0 || rules.costLimit != 0 ||
                    string.IsNullOrEmpty(rules.verificationRoute))
                    errors.Add(m.id + ": revised native map references or victory fields");
                errors.AddRange(map.ValidateMap().Select(e => m.id + ": " + e));
                return;
            }
            void Check(bool condition, string field) { if (!condition) errors.Add(m.id + ": " + field); }
            Check(map.Width == m.size.width && map.Height == m.size.height, "size");
            Check(map.theme == Theme && map.scenario == rules && rules.themeId == "starway", "references/theme");
            Check(map.start == Cell(m.start) && map.exit == Cell(m.exit), "start/exit");
            Check(map.targetBubbles == 3 && map.bubbles.SequenceEqual(m.bubbles.Select(Cell)), "bubbles");
            Check(map.cameraSettings.visibleRows == 11 && !map.cameraSettings.fitWholeBoard, "camera rows");
            var region = m.regionCells.Select(Cell).ToHashSet();
            var floors = m.walkableCells.Select(Cell).ToHashSet();
            var walls = m.blockedCells.Select(Cell).ToHashSet();
            Check(map.terrain.Count == region.Count && map.terrain.All(t => region.Contains(t.position) &&
                  (floors.Contains(t.position) ? !t.blocked && t.material == TerrainMaterial.Grass :
                   walls.Contains(t.position) && t.blocked && t.material == TerrainMaterial.Stone)) &&
                  floors.All(map.Walkable) && walls.All(c => !map.Walkable(c)), "terrain");
            Check(map.devices.Count == 0 && map.decorations.Count == 0, "legacy device/decorations");
            Check(rules.costLimit == 0 && rules.goals.Count == 0 && rules.verificationRoute == source.input.referenceActions, "victory/route");
            Check(rules.devices.Count == m.devices.Length && rules.devices.All(d => d.visualPrefab && d.artTheme == "starway"), "device visual count");
            foreach (var d in m.devices)
            {
                var actual = rules.devices.FirstOrDefault(x => x.id == d.id);
                Check(actual != null && (int)actual.role == d.role && actual.variant == (d.variant ?? "") &&
                      actual.position == Cell(d.position) && actual.direction == Cell(d.direction) &&
                      actual.target == Cell(d.target) && actual.number == d.number && actual.orientation == d.orientation &&
                      actual.socketReward == StarSocketReward.None, "device " + d.id);
            }
            Check(rules.ranchV13Art.Count == m.ranchV13Art.Length + m.ranchSluiceDesign.Length, "ranch count");
            foreach (var r in m.ranchV13Art)
                Check(rules.ranchV13Art.Any(x => x.instanceId == r.instanceId && x.family == r.family && x.position == Cell(r.position)), "ranch " + r.instanceId);
            foreach (var s in m.ranchSluiceDesign)
                Check(rules.ranchV13Art.Any(x => x.instanceId == s.instanceId && x.family == "sluice" &&
                      x.position == Cell(s.position) && x.PondB == Cell(s.pondB) && x.rightState == s.rightState), "sluice " + s.instanceId);
            Check(rules.water.SequenceEqual(m.water.Select(Cell)), "water");
            errors.AddRange(map.ValidateMap().Select(e => m.id + ": " + e));
        }

        public override void RegisterCampaign(StarwayNativeLink[] links)
        {
            var set = Upsert<StarwayV3MapSet>(SetPath);
            set.maps = links.Select((link, i) => new StarwayV3MapSet.Entry
            {
                sourceId = link.id, stageName = link.id, map = (PuzzleMap)link.map,
                trace = AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayPackCommands.DataRoot + "/UnityReplay/" + link.id + ".json")
            }).ToList();
            EditorUtility.SetDirty(set);
        }

        public override void ValidateCampaign(StarwayNativeLink[] links, List<string> errors)
        {
            var set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(SetPath);
            if (!set || set.maps.Count != 20) { errors.Add("SW map set missing or incomplete."); return; }
            for (int i = 0; i < 20; i++)
                if (set.maps[i].sourceId != links[i].id || set.maps[i].map != links[i].map ||
                    set.maps[i].trace == null) errors.Add("SW map set mismatch at " + i);
        }

        public static void ArchivePreviousMaps()
        {
            var root = "Assets/ANIMOL/Data/Puzzle/LegacyStarwayV3";
            var mapRoot = root + "/Maps";
            var scenarioRoot = root + "/Scenarios";
            if (!AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(SetPath) ||
                AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(SetPath).maps.Count != 20)
                throw new InvalidOperationException("Validate the new 20-map set before archiving.");
            if (!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets/ANIMOL/Data/Puzzle", "LegacyStarwayV3");
            if (!AssetDatabase.IsValidFolder(mapRoot)) AssetDatabase.CreateFolder(root, "Maps");
            if (!AssetDatabase.IsValidFolder(scenarioRoot)) AssetDatabase.CreateFolder(root, "Scenarios");
            // These may be untracked user assets; move them with their GUIDs and leave them uncommitted.
            MoveIfPresent("Assets/ANIMOL/Data/Puzzle/Maps/1-1.asset", mapRoot + "/1-1.asset");
            MoveIfPresent("Assets/ANIMOL/Data/Puzzle/Maps/1-1_Scenario.asset", scenarioRoot + "/1-1_Scenario.asset");
            MoveIfPresent("Assets/ANIMOL/Data/Puzzle/Maps/1-2_Scenario.asset", scenarioRoot + "/1-2_ManualScenario.asset");
            for (int i = 2; i <= 21; i++)
            {
                var name = "1-" + i;
                MoveIfPresent("Assets/ANIMOL/Data/Puzzle/Maps/" + name + ".asset", mapRoot + "/" + name + ".asset");
                MoveIfPresent("Assets/ANIMOL/Data/Puzzle/Starway/Maps/V3/" + name + "_Scenario.asset",
                    scenarioRoot + "/" + name + "_Scenario.asset");
            }
            AssetDatabase.SaveAssets();
        }

        static void MoveIfPresent(string source, string destination)
        {
            if (!AssetDatabase.LoadMainAssetAtPath(source)) return;
            if (AssetDatabase.LoadMainAssetAtPath(destination)) throw new InvalidOperationException("Legacy destination occupied: " + destination);
            var issue = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(issue)) throw new InvalidOperationException(issue);
        }
    }
}
