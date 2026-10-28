using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public static class EchoTheaterV3Validation
    {
        static Vector2Int Cell(JToken p) => new Vector2Int((int)p[0], (int)p[1]);
        static Vector2Int Direction(char c) => c == 'U' ? Vector2Int.up : c == 'D' ? Vector2Int.down :
            c == 'L' ? Vector2Int.left : Vector2Int.right;

        [MenuItem("ANIMOL/Echo Theater V3/2 Replay 35 Native Maps")]
        public static string RunAll()
        {
            var issues = new List<string>();
            var passed = 0;
            var set = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(EchoTheaterV3Importer.SetPath);
            if (!set || set.maps.Count != 35) throw new InvalidOperationException("Echo Theater 35-map set missing.");
            for (var index = 1; index <= 35; index++)
            {
                var id = "ET-" + index.ToString("00");
                var map = set.maps[index - 1].map;
                if (!map || set.maps[index - 1].stageId != id) { issues.Add(id + " map/set mismatch"); continue; }
                var source = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(EchoTheaterV3Importer.SourceRoot + "/maps/" + id + ".json").text);
                var trace = JArray.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(EchoTheaterV3Importer.SourceRoot + "/verification/" + id + "_trace.json").text);
                var solution = (string)source["solution"];
                if (solution.Length != trace.Count || solution.Length != (int)source["minimum_successful_moves"])
                { issues.Add(id + " source solution length mismatch"); continue; }
                var session = new PuzzleDeviceSession(map);
                var first = session.Copy();
                var diceMoves = map.scenario.devices.Where(d => d.role == PrototypeRole.Dice && d.variant == "")
                    .ToDictionary(d => d.id, d => new List<Vector2Int>());
                var stageIssues = 0;
                for (var turn = 0; turn < solution.Length; turn++)
                {
                    var result = session.Apply(Direction(solution[turn]));
                    var expected = trace[turn];
                    void Check(bool okay, string field)
                    { if (!okay && stageIssues++ < 8) issues.Add(id + " step " + (turn + 1) + " " + field); }
                    Check(result.accepted && !session.Failed, "input rejected " + result.reason);
                    if (!result.accepted || session.Failed) break;
                    Check(session.Cell == Cell(expected["player"]), "player " + session.Cell + " != " + expected["player"]);
                    Check(result.events.Where(e => e.kind == PrototypeEventKind.Move && e.actor == "player")
                        .Select(e => e.to).SequenceEqual(((JArray)expected["visited_this_action"]).Select(Cell)), "player path");
                    foreach (var movement in result.events.Where(e => e.kind == PrototypeEventKind.Move && diceMoves.ContainsKey(e.actor)))
                        diceMoves[movement.actor].Add(movement.to - movement.from);
                    var mask = 0;
                    for (var b = 0; b < 3; b++) if (session.IsCollected(map.bubbles[b])) mask |= 1 << b;
                    Check(mask == (int)expected["bubble_mask"], "bubble mask");
                    Check(session.Won == (bool)expected["won"], "victory");
                    foreach (var die in (JArray)expected["dice_states"])
                    {
                        var state = session.States[(string)die["id"]];
                        Check(state.removed == (bool)die["absorbed"] &&
                            (state.removed || state.cell == Cell(die["position"])) &&
                            state.dice.SequenceEqual(((JArray)die["faces"]).Select(n => (int)n)), "die " + die["id"]);
                    }
                    var sockets = map.scenario.devices.Where(d => d.role == PrototypeRole.Dice && d.variant == "socket");
                    Check(sockets.Where(d => session.States[d.id].active).Select(d => d.id)
                        .OrderBy(x => x).SequenceEqual(((JArray)expected["opened_sockets"]).Select(x => (string)x).OrderBy(x => x)), "sockets");
                    var dominoes = map.scenario.devices.Where(d => d.role == PrototypeRole.Domino).ToArray();
                    var folded = 0;
                    for (var d = 0; d < dominoes.Length; d++) if (session.States[dominoes[d].id].active) folded |= 1 << d;
                    Check(folded == (int)expected["domino_folded_mask"], "domino mask");
                    var shutter = map.scenario.devices.FirstOrDefault(d => d.role == PrototypeRole.Shutter);
                    if (shutter != null) Check(session.States[shutter.id].phase % 2 == (int)expected["shutter_phase"], "shutter phase");
                    var shelf = map.scenario.devices.FirstOrDefault(d => d.role == PrototypeRole.Bookshelf && d.variant != "plate");
                    if (shelf != null) Check(session.States[shelf.id].phase % 4 == (int)expected["shelf_phase"], "shelf phase");
                    var platform = map.scenario.devices.FirstOrDefault(d => d.role == PrototypeRole.MovingStage);
                    if (platform != null) Check(session.States[platform.id].cell == Cell(expected["platform_position"]), "platform");
                    var doll = map.scenario.devices.FirstOrDefault(d => d.role == PrototypeRole.CowardDoll);
                    if (doll != null) Check(session.States[doll.id].cell == Cell(expected["doll_position"]), "doll");
                    if (stageIssues > 0) break;
                    if (turn == 0)
                    {
                        Check(session.Undo() && session.Cell == first.Cell && session.Cost == 0, "Undo");
                        if (stageIssues > 0) break;
                        Check(session.Apply(Direction(solution[turn])).accepted, "resume after Undo");
                    }
                }
                if (stageIssues == 0 && session.Won && session.Collected == 3 && session.Cell == map.exit && session.Cost == solution.Length) passed++;
                else if (stageIssues == 0) issues.Add(id + " final victory/cost mismatch");
                foreach (var die in diceMoves)
                {
                    var axes = die.Value.Select(d => d.x != 0).ToArray();
                    if (!axes.Contains(true) || !axes.Contains(false) ||
                        axes.Skip(1).Where((axis, i) => axis != axes[i]).Count() < 2)
                        issues.Add(id + " die " + die.Key + " did not use both axes and two changes");
                }
                session.Reset();
                if (session.Cell != first.Cell || session.Cost != 0 || session.Collected != first.Collected || session.Won)
                    issues.Add(id + " Reset mismatch");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/echo-theater-v3-replay.txt", "passed=" + passed + "/35\n" + string.Join("\n", issues));
            if (issues.Count > 0) throw new InvalidOperationException("Echo Theater: " + passed + "/35; " + issues[0]);
            Debug.Log("[Echo Theater V3] Unity PuzzleDeviceSession replay 35/35 passed.");
            return "35/35";
        }
    }
}
