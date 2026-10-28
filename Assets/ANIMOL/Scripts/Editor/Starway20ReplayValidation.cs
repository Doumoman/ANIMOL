using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.StarwayMaps.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public static class Starway20ReplayValidation
    {
        static Vector2Int Point(JToken value) => new Vector2Int((int)value["x"], (int)value["y"]);
        static Vector2Int Heading(char c) => c == 'N' ? Vector2Int.up : c == 'E' ? Vector2Int.right :
            c == 'S' ? Vector2Int.down : Vector2Int.left;

        [MenuItem("ANIMOL/Starway 20/6 Replay Native Maps")]
        public static string RunAll()
        {
            var differences = new List<string>();
            var passed = 0;
            foreach (var source in StarwayPackCommands.RequireSources())
            {
                var id = source.input.map.id;
                var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(Starway20ProjectAdapter.Root + "/" + id + ".asset");
                if (!map) { differences.Add(id + ": native map missing"); continue; }
                var replay = JObject.Parse(source.normalizedReplay.text);
                var solution = JObject.Parse(source.originalSolution.text);
                var actions = (string)replay["actions"];
                if (actions != (string)solution["actions"] || actions != source.input.noStarMatchingActions)
                    differences.Add(id + ": solution/no-star action mismatch");
                var steps = (JArray)replay["steps"];
                var session = new PuzzleDeviceSession(map);
                if (steps.Count != actions.Length) differences.Add(id + ": step count mismatch");
                var limit = Math.Min(steps.Count, actions.Length);
                var complete = true;
                for (var i = 0; i < limit; i++)
                {
                    var expected = steps[i];
                    var previous = session.Copy();
                    if (session.Cell != Point(expected["playerBefore"]))
                    { differences.Add(id + " step " + (i + 1) + ": playerBefore"); complete = false; break; }
                    var result = session.Apply(Heading(actions[i]));
                    if (!result.accepted || session.Failed)
                    { differences.Add(id + " step " + (i + 1) + ": rejected/failed " + (session.Failure ?? result.reason)); complete = false; break; }
                    void Check(bool ok, string field) { if (!ok) differences.Add(id + " step " + (i + 1) + ": " + field); }
                    Check(session.Cell == Point(expected["playerAfter"]), "playerAfter " + session.Cell);
                    Check(result.events.Where(e => e.kind == PrototypeEventKind.Move && e.actor == "player")
                        .Select(e => e.to).SequenceEqual(((JArray)expected["playerPath"]).Select(Point)), "playerPath");
                    Check(session.Cost == i + 1, "cost " + session.Cost);
                    var bubbleMask = 0;
                    for (var b = 0; b < map.bubbles.Count; b++) if (session.IsCollected(map.bubbles[b])) bubbleMask |= 1 << b;
                    Check(bubbleMask == (int)expected["bubbleMask"], "bubbleMask " + bubbleMask);
                    var chicks = map.scenario.devices.Where(d => d.role == PrototypeRole.Chick).ToArray();
                    var expectedChicks = (JArray)expected["chicks"];
                    Check(chicks.Length == expectedChicks.Count, "chick count");
                    for (var j = 0; j < Math.Min(chicks.Length, expectedChicks.Count); j++)
                    {
                        var state = session.States[chicks[j].id]; var goal = expectedChicks[j];
                        Check(state.cell == Point(goal["position"]) && state.active == (bool)goal["returned"], "chick " + j);
                        Check(state.trail.SequenceEqual(((JArray)goal["trail"]).Select(Point)), "chick trail " + j);
                    }
                    var sheep = map.scenario.ranchV13Art.Where(r => r.family == "sheep").ToArray();
                    var expectedSheep = (JArray)expected["sheep"];
                    Check(sheep.Length == expectedSheep.Count, "sheep count");
                    for (var j = 0; j < Math.Min(sheep.Length, expectedSheep.Count); j++)
                    {
                        var exists = session.RanchSheep.TryGetValue(sheep[j].Id, out var position);
                        Check(exists == (bool)expectedSheep[j]["exists"] && (!exists || position == Point(expectedSheep[j]["position"])), "sheep " + j);
                    }
                    var grass = ((JArray)expected["remainingGrass"]).Select(Point).ToHashSet();
                    var actualGrass = map.scenario.ranchV13Art.Where(r => r.family == "tall_grass" && session.RanchGrassPresent(r.position))
                        .Select(r => r.position).ToHashSet();
                    Check(grass.SetEquals(actualGrass), "remainingGrass");
                    var sluices = map.scenario.ranchV13Art.Where(r => r.family == "sluice").ToArray();
                    var wetA = (JArray)expected["wetA"];
                    Check(sluices.Length == wetA.Count, "sluice count");
                    for (var j = 0; j < Math.Min(sluices.Length, wetA.Count); j++)
                        Check(session.RanchPonds[sluices[j].Id] == (bool)wetA[j], "wetA " + j);
                    var winds = map.scenario.devices.Where(d => d.role == PrototypeRole.Wind && d.variant == "blower").ToArray();
                    var phases = (JArray)expected["windPhases"];
                    Check(winds.Length == phases.Count, "wind count");
                    for (var j = 0; j < Math.Min(winds.Length, phases.Count); j++)
                        Check(session.States[winds[j].id].phase == (int)phases[j], "wind phase " + j);
                    var sockets = map.scenario.devices.Where(d => d.role == PrototypeRole.StarSocket && d.variant == "socket").ToArray();
                    var socketMask = 0;
                    for (var j = 0; j < sockets.Length; j++) if (session.States[sockets[j].id].active) socketMask |= 1 << j;
                    Check(socketMask == (int)expected["activeSocketMask"], "activeSocketMask");
                    if (differences.Count > 0) { complete = false; break; }
                    if (i == 0)
                    {
                        Check(session.Undo() && session.Cell == previous.Cell && session.Cost == previous.Cost &&
                              session.Collected == previous.Collected, "Undo");
                        result = session.Apply(Heading(actions[i]));
                        Check(result.accepted, "replay after Undo");
                    }
                    if (differences.Count > 0) { complete = false; break; }
                }
                if (!complete) continue;
                if (!session.Won || session.Cell != map.exit || session.Collected != 3)
                    differences.Add(id + ": final victory/three bubbles/exit failed");
                else passed++;
                session.Reset();
                if (session.Cell != map.start || session.Cost != 0 || session.Collected != 0 || session.Won)
                    differences.Add(id + ": Reset failed");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/starway20-replay-result.txt", "passed=" + passed + "/20\n" + string.Join("\n", differences));
            if (differences.Count > 0) throw new InvalidOperationException("Starway20 replay: " + passed + "/20; " + differences[0]);
            Debug.Log("[Starway20] native PuzzleDeviceSession replay 20/20 passed.");
            return "20/20";
        }
    }
}
