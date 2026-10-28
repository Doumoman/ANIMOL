using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public static class StarwayV3Validation
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static readonly string[] Letters = { "N", "E", "S", "W" };
        public sealed class Result
        {
            public int maps, correctInputs, wrongBaseInputs, wrongStrictInputs, exitMaskCases, invalidMapCases, invalidOverrideCases, repeatBubbleCases;
            public readonly List<string> errors = new List<string>();
            public override string ToString() => "maps=" + maps + ", correct=" + correctInputs + ", wrongBase=" + wrongBaseInputs +
                ", wrongStrict=" + wrongStrictInputs + ", exitMasks=" + exitMaskCases + ", invalidMaps=" + invalidMapCases +
                ", invalidOverrides=" + invalidOverrideCases + ", repeatBubbles=" + repeatBubbleCases + ", errors=" + errors.Count +
                (errors.Count == 0 ? "" : "; first=" + errors[0]);
        }

        [MenuItem("Tools/ANIMOL/Starway V3/Validate Unity session transitions")]
        public static void ValidateMenu() => Debug.Log(Run());

        public static Result Run()
        {
            var result = new Result();
            var set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(StarwayV3Importer.SetPath);
            if (!set || set.maps.Count != 20) { result.errors.Add("V3 map set is missing or incomplete."); return result; }
            var wrong = JArray.Parse(File.ReadAllText(StarwayV3Importer.Source + "/wrong_branches.json"));
            foreach (var entry in set.maps)
            {
                try { CheckMap(entry, wrong, result); result.maps++; }
                catch (Exception ex) { result.errors.Add(entry.stageName + ": " + ex.Message); }
            }
            return result;
        }

        static void CheckMap(StarwayV3MapSet.Entry entry, JArray wrong, Result result)
        {
            var map = entry.map;
            if (AssetDatabase.GetAssetPath(map) != StarwayV3Importer.Maps + "/" + entry.stageName + ".asset" ||
                !map.scenario || AssetDatabase.GetAssetPath(map.scenario) != StarwayV3Importer.LegacyScenarioRoot + "/" + entry.stageName + "_Scenario.asset")
                throw new Exception("Map SO or its linked V3 scenario is at the wrong asset path.");
            if (map.targetBubbles != 3 || map.bubbles.Count != 3 || map.bubbles.Distinct().Count() != 3 ||
                !map.cameraSettings.fitWholeBoard || map.scenario == null || map.theme == null)
                throw new Exception("V3 map linkage, bubble rule, or camera fit is invalid.");
            var validation = map.ValidateMap();
            if (validation.Count != 0) throw new Exception("Map validation: " + string.Join("; ", validation));
            CheckGeometry(entry);
            var trace = JArray.Parse(entry.trace.text);
            var audit = JObject.Parse(File.ReadAllText(StarwayV3Importer.Source + "/GEOMETRY_AUDIT_REPORT.json"));
            var auditRow = ((JArray)audit["reports"]).Cast<JObject>().Single(x => (string)x["mapId"] == entry.sourceId);
            var auditedBubbles = ((JArray)auditRow["bubbleTests"]["collectionEvents"]).Cast<JObject>()
                .Select(x => ((int)x["index"], (int)x["step"])).ToList();
            var unityBubbles = new List<(int, int)>();
            var baseline = new PuzzleDeviceSession(map);
            var strictSession = new PuzzleDeviceSession(map);
            var strict = new StarwayV3StrictPolicy(map);
            CheckState(entry, 0, baseline, trace[0]["state"]);
            var branches = wrong.Cast<JObject>().Where(w => (string)w["mapId"] == entry.sourceId)
                .GroupBy(w => (int)w["step"]).ToDictionary(g => g.Key, g => g.ToList());
            for (var step = 1; step < trace.Count; step++)
            {
                var row = (JObject)trace[step];
                var action = (string)row["action"];
                var expected = Directions[Array.IndexOf(Letters, action)];
                if (!branches.TryGetValue(step, out var alternatives) || alternatives.Count != 3)
                    throw new Exception("Wrong-branch source has fewer than three alternatives at " + step);
                foreach (var branch in alternatives)
                {
                    var attempt = Directions[Array.IndexOf(Letters, (string)branch["attempt"])];
                    if (attempt == expected) throw new Exception("Wrong-branch source includes the answer.");
                    var baseCopy = baseline.Copy(); var before = StarwayV3StrictPolicy.StateHash(baseCopy);
                    var priorCost = baseCopy.Cost;
                    var baseAction = baseCopy.Apply(attempt);
                    if (baseAction.accepted != (bool)branch["baseAccepted"] ||
                        baseCopy.Cost - priorCost != (int)branch["baseCostDelta"])
                        throw new Exception("Base branch decision differs at " + step + " " + branch["attempt"]);
                    var successor = branch["successor"];
                    if (baseAction.accepted && StarwayV3StrictPolicy.StateHash(baseCopy) != StarwayV3StrictPolicy.Hash(successor) ||
                        !baseAction.accepted && StarwayV3StrictPolicy.StateHash(baseCopy) != before)
                        throw new Exception("Base branch successor differs at " + step + " " + branch["attempt"]);
                    result.wrongBaseInputs++;
                    var strictCopy = strict.Copy(); var strictBoard = strictSession.Copy();
                    var strictBefore = StarwayV3StrictPolicy.StateHash(strictBoard);
                    if (strictCopy.TryApply(strictBoard, attempt, out _) ||
                        strictCopy.LastDecision != StarwayV3StrictPolicy.Decision.WrongInput ||
                        strictBoard.Cost != strictSession.Cost || StarwayV3StrictPolicy.StateHash(strictBoard) != strictBefore)
                        throw new Exception("Strict wrong input changed the board at " + step + " " + branch["attempt"]);
                    if (strictCopy.TryApply(strictBoard, expected, out _) ||
                        strictCopy.LastDecision != StarwayV3StrictPolicy.Decision.Locked)
                        throw new Exception("Strict failure did not lock following input at " + step);
                    result.wrongStrictInputs++;
                }
                var normal = baseline.Apply(expected);
                if (!normal.accepted) throw new Exception("Unity rejected answer at " + step + ": " + normal.reason);
                if (!strict.TryApply(strictSession, expected, out var strictAction) || !strictAction.accepted)
                    throw new Exception("Strict rejected answer at " + step + ": " + strict.Error);
                CheckState(entry, step, baseline, row["state"]);
                var beforeMask = (int)trace[step - 1]["state"]["bubbleMask"];
                var afterMask = (int)row["state"]["bubbleMask"];
                for (var bubble = 0; bubble < 3; bubble++)
                    if ((beforeMask & (1 << bubble)) == 0 && (afterMask & (1 << bubble)) != 0)
                        unityBubbles.Add((bubble + 1, step));
                if (baseline.Cell == map.exit && baseline.Collected < 3 && baseline.Won)
                    throw new Exception("Exit won before all bubbles at " + step);
                if (baseline.Cost != step || strictSession.Cost != step || baseline.Won != (step == trace.Count - 1) ||
                    strictSession.Won != (step == trace.Count - 1))
                    throw new Exception("Cost or win timing differs at " + step);
                result.correctInputs++;
            }
            if (baseline.Cell != map.exit || baseline.Collected != 3 || !baseline.ExitReady)
                throw new Exception("Answer did not finish on ready exit with all bubbles.");
            if (!unityBubbles.SequenceEqual(auditedBubbles))
                throw new Exception("Unity bubble acquisition inputs differ from the V3 bubble audit.");
            CheckExitMasks(baseline, result);
            CheckInvalidMaps(map, result);
            CheckInvalidOverrides(map, result);
            CheckRepeatBubbles(baseline, result);
            strictSession.Reset(); strict.Reset();
            if (strict.Step != 0 || strictSession.Cost != 0 || strictSession.Collected != 0)
                throw new Exception("Strict reset did not restore initial state.");
        }

        static void CheckState(StarwayV3MapSet.Entry entry, int step, PuzzleDeviceSession session, JToken expected)
        {
            var actual = StarwayV3StrictPolicy.Project(session);
            if (StarwayV3StrictPolicy.Hash(actual) != StarwayV3StrictPolicy.Hash(expected))
                throw new Exception("Trace differs at input " + step + "; actual=" + actual.ToString(Newtonsoft.Json.Formatting.None) +
                    "; expected=" + expected.ToString(Newtonsoft.Json.Formatting.None));
        }

        static void CheckGeometry(StarwayV3MapSet.Entry entry)
        {
            var scenario = JObject.Parse(File.ReadAllText(StarwayV3Importer.Source + "/scenarios/" + entry.sourceId + ".json"));
            var shapes = JObject.Parse(File.ReadAllText(StarwayV3Importer.Source + "/map_shapes.json"));
            var shape = ((JArray)shapes["maps"]).Cast<JObject>().Single(x => (string)x["id"] == entry.sourceId);
            var shapeCells = ((JArray)shape["shapeCells"]).Select(x => new Vector2Int((int)x[0], (int)x[1])).ToHashSet();
            var walkable = ((JArray)scenario["walkableCells"]).Select(x => new Vector2Int((int)x[0], (int)x[1])).ToHashSet();
            var map = entry.map;
            if (map.Width != (int)scenario["size"][0] || map.Height != (int)scenario["size"][1] ||
                map.terrain.Count != shapeCells.Count || map.terrain.Select(t => t.position).Distinct().Count() != shapeCells.Count ||
                map.terrain.Any(t => !shapeCells.Contains(t.position) || t.blocked == walkable.Contains(t.position)) ||
                walkable.Any(p => !map.Walkable(p)))
                throw new Exception("Imported shape/walkable geometry differs from the V3 source.");
        }

        static void CheckExitMasks(PuzzleDeviceSession final, Result result)
        {
            var collectedField = typeof(PuzzleDeviceSession).GetField("collected", BindingFlags.Instance | BindingFlags.NonPublic);
            var wonField = typeof(PuzzleDeviceSession).GetField("<Won>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            var victory = typeof(PuzzleDeviceSession).GetMethod("Victory", BindingFlags.Instance | BindingFlags.NonPublic);
            if (collectedField == null || wonField == null || victory == null) throw new Exception("Cannot inspect authoritative victory state.");
            for (var mask = 0; mask < 8; mask++)
            {
                var board = final.Copy();
                collectedField.SetValue(board, new HashSet<Vector2Int>(final.Map.bubbles.Where((p, i) => (mask & (1 << i)) != 0)));
                wonField.SetValue(board, false);
                victory.Invoke(board, null);
                if (board.Won != (mask == 7)) throw new Exception("Exit victory mask " + mask + " is wrong.");
                result.exitMaskCases++;
            }
        }

        static void CheckInvalidMaps(PuzzleMap map, Result result)
        {
            for (var i = 0; i < 3; i++)
            {
                var copy = UnityEngine.Object.Instantiate(map);
                try
                {
                    copy.bubbles.RemoveAt(i); copy.InvalidateIndex();
                    if (copy.ValidateMap().Count == 0) throw new Exception("Missing bubble " + i + " was accepted.");
                    result.invalidMapCases++;
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
            }
        }

        static void CheckInvalidOverrides(PuzzleMap map, Result result)
        {
            foreach (var target in new[] { 0, 2 })
            {
                var copy = UnityEngine.Object.Instantiate(map);
                try
                {
                    copy.targetBubbles = target;
                    copy.bubbles.RemoveRange(target, copy.bubbles.Count - target);
                    copy.InvalidateIndex();
                    if (copy.ValidateMap().Count == 0) throw new Exception("Target/bubble override " + target + " was accepted.");
                    result.invalidOverrideCases++;
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
            }
        }

        static void CheckRepeatBubbles(PuzzleDeviceSession final, Result result)
        {
            var cellField = typeof(PuzzleDeviceSession).GetField("<Cell>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            var collect = typeof(PuzzleDeviceSession).GetMethod("Collect", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cellField == null || collect == null) throw new Exception("Cannot inspect authoritative bubble collector.");
            foreach (var bubble in final.Map.bubbles)
            {
                var board = final.Copy();
                cellField.SetValue(board, bubble); collect.Invoke(board, null); collect.Invoke(board, null);
                if (board.Collected != 3) throw new Exception("Repeated bubble contact increased collection.");
                result.repeatBubbleCases++;
            }
        }
    }
}
