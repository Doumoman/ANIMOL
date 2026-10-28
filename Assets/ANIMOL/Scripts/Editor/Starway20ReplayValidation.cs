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

        static void CheckSocketPassage(PuzzleMap map, List<string> differences)
        {
            var sockets = map.scenario.devices.Where(d => d.role == PrototypeRole.StarSocket && d.variant == "socket");
            foreach (var socket in sockets)
            {
                var reachable = new HashSet<Vector2Int> { map.start };
                var frontier = new Queue<Vector2Int>(); frontier.Enqueue(map.start);
                while (frontier.Count > 0)
                {
                    var cell = frontier.Dequeue();
                    foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                    {
                        var next = cell + step;
                        if (next != socket.position && map.Walkable(next) && reachable.Add(next)) frontier.Enqueue(next);
                    }
                }
                if (reachable.Contains(map.exit) && map.bubbles.All(reachable.Contains))
                    differences.Add(map.name + ": socket can be bypassed by floor path " + socket.id);
                var approach = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }
                    .Select(direction => socket.position - direction)
                    .FirstOrDefault(cell => map.Walkable(cell) && !map.scenario.devices.Any(d => d.position == cell) &&
                        !map.scenario.ranchV13Art.Any(r => r.position == cell || r.family == "sluice" && r.PondB == cell));
                if (approach == Vector2Int.zero) { differences.Add(map.name + ": socket approach unavailable " + socket.id); continue; }
                var trialMap = UnityEngine.Object.Instantiate(map);
                var trialRules = UnityEngine.Object.Instantiate(map.scenario);
                try
                {
                    trialMap.scenario = trialRules;
                    trialMap.start = approach;
                    var toward = socket.position - approach;
                    var empty = new PuzzleDeviceSession(trialMap);
                    if (empty.Apply(toward).accepted || empty.Cell == socket.position)
                        differences.Add(map.name + ": empty socket passable " + socket.id);
                    var cloneSocket = trialRules.devices.First(d => d.id == socket.id);
                    cloneSocket.active = true;
                    var active = new PuzzleDeviceSession(trialMap);
                    if (!active.Apply(toward).accepted || active.Cell != socket.position)
                        differences.Add(map.name + ": active socket blocked " + socket.id);
                }
                finally { UnityEngine.Object.DestroyImmediate(trialRules); UnityEngine.Object.DestroyImmediate(trialMap); }
            }
        }

        public static string RunAll()
        {
            var differences = new List<string>();
            var details = new List<string>();
            var windAudit = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/ANIMOL/StarwayMapPack/Editor/Data/WindAuditV7.json").text);
            var passed = 0;
            foreach (var source in StarwayPackCommands.RequireSources())
            {
                var id = source.input.map.id;
                var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(Starway20ProjectAdapter.MapPath(id));
                if (!map) { differences.Add(id + ": native map missing"); continue; }
                var replay = JObject.Parse(source.normalizedReplay.text);
                var solution = JObject.Parse(source.originalSolution.text);
                var actions = (string)replay["actions"];
                if (actions != (string)solution["actions"] || actions != source.input.referenceActions)
                    differences.Add(id + ": solution/reference action mismatch");
                var steps = (JArray)replay["steps"];
                var session = new PuzzleDeviceSession(map);
                CheckSocketPassage(map, differences);
                var stones = map.scenario.devices.Where(d => d.role == PrototypeRole.StarSocket && d.variant != "socket").ToArray();
                var stoneMoves = stones.ToDictionary(d => d.id, d => new List<Vector2Int>());
                var stonePushes = stones.ToDictionary(d => d.id, d => new List<Vector2Int>());
                var rides = windAudit[id]?["playerRides"] as JArray;
                if (stones.Any(d => d.position.x == map.scenario.devices.First(s => s.role == PrototypeRole.StarSocket && s.variant == "socket" && s.number == d.number).position.x ||
                                    d.position.y == map.scenario.devices.First(s => s.role == PrototypeRole.StarSocket && s.variant == "socket" && s.number == d.number).position.y))
                    differences.Add(id + ": stone/socket shares start axis");
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
                    var playerCell = previous.Cell;
                    var chickCells = map.scenario.devices.Where(d => d.role == PrototypeRole.Chick)
                        .ToDictionary(d => d.id, d => previous.States[d.id].cell);
                    var stoneCells = stones.ToDictionary(d => d.id, d => previous.States[d.id].cell);
                    var sawPlayerMove = false;
                    foreach (var movement in result.events.Where(e => e.kind == PrototypeEventKind.Move))
                    {
                        if (movement.actor == "player") { Check(movement.from == playerCell, "player event continuity"); playerCell = movement.to; sawPlayerMove = true; }
                        if (chickCells.ContainsKey(movement.actor))
                        {
                            Check(chickCells[movement.actor] == movement.from, "chick event continuity");
                            chickCells[movement.actor] = movement.to;
                        }
                        if (stoneCells.ContainsKey(movement.actor))
                        {
                            Check(stoneCells[movement.actor] == movement.from, "stone event continuity");
                            stoneCells[movement.actor] = movement.to;
                            stoneMoves[movement.actor].Add(movement.to - movement.from);
                            if (!sawPlayerMove) stonePushes[movement.actor].Add(movement.to - movement.from);
                        }
                        Check(chickCells.Values.Distinct().Count() == chickCells.Count &&
                              !chickCells.Values.Contains(playerCell), "intermediate player/chick overlap");
                    }
                    foreach (var stone in stones)
                        Check(stoneCells[stone.id] == session.States[stone.id].cell, "stone final cell " + stone.id);
                    if (rides != null) foreach (var ride in rides.Where(r => (int)r["turn"] == i + 1))
                    {
                        var path = ((JArray)ride["path"]).Select(p => new Vector2Int((int)p[0], (int)p[1])).ToArray();
                        var actualPath = new[] { previous.Cell }.Concat(result.events.Where(e => e.kind == PrototypeEventKind.Move && e.actor == "player")
                            .Select(e => e.to)).ToArray();
                        Check(actualPath.SkipWhile(p => p != path[0]).Take(path.Length).SequenceEqual(path), "wind ride path");
                        var wind = map.scenario.devices.Where(d => d.role == PrototypeRole.Wind && d.variant == "blower").ElementAt((int)ride["blower"] - 1);
                        var phase = session.States[wind.id].phase;
                        Check(phase == (int)ride["phase"] && phase * 3 == session.WindRouteFor(wind.id).maxRangeCells, "wind phase/range");
                        var reflected = path.Zip(path.Skip(1), (a, b) => b - a).Distinct().Count() > 1;
                        Check(reflected == (bool)ride["reflected"], "wind reflection");
                        Check(map.scenario.devices.Any(d => d.role == PrototypeRole.Brake && d.position == path[path.Length - 1]) ==
                              (bool)ride["brakeLanding"], "wind brake landing");
                        details.Add(id + " ride " + (i + 1) + " " + path[0] + " -> " + path[path.Length - 1] +
                            " phase=" + phase + " power=" + phase * 3 + " reflected=" + reflected + " brake=" + (bool)ride["brakeLanding"]);
                    }
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
                foreach (var stone in stones)
                {
                    var pushes = stonePushes[stone.id];
                    if (pushes.Count < 3 || pushes.Distinct().Count() < 2)
                        differences.Add(id + ": insufficient stone pushes " + stone.id);
                    details.Add(id + " stone " + stone.id + " #" + stone.number + " pushes=" +
                        string.Join(",", pushes.Select(p => p == Vector2Int.up ? "N" : p == Vector2Int.right ? "E" : p == Vector2Int.down ? "S" : "W")));
                    if (stoneMoves[stone.id].Count != pushes.Count)
                        details.Add(id + " stone " + stone.id + " additional automatic movement=" +
                            (stoneMoves[stone.id].Count - pushes.Count));
                }
                if (rides != null && rides.Count < 1) differences.Add(id + ": no audited wind ride");
                if (!session.Won || session.Cell != map.exit || session.Collected != 3)
                    differences.Add(id + ": final victory/three bubbles/exit failed");
                else passed++;
                session.Reset();
                if (session.Cell != map.start || session.Cost != 0 || session.Collected != 0 || session.Won)
                    differences.Add(id + ": Reset failed");
                if (!string.IsNullOrEmpty(source.input.noStarMatchingActions))
                {
                    var alternative = new PuzzleDeviceSession(map);
                    foreach (var action in source.input.noStarMatchingActions)
                    {
                        var outcome = alternative.Apply(Heading(action));
                        if (!outcome.accepted || alternative.Failed)
                        { differences.Add(id + ": no-star solution rejected at input " + alternative.Cost); break; }
                    }
                    if (!alternative.Won || alternative.Collected != 3 || alternative.Cell != map.exit)
                        differences.Add(id + ": no-star solution did not win");
                    if (map.scenario.devices.Any(d => d.role == PrototypeRole.StarSocket && d.variant == "socket" && alternative.States[d.id].active))
                        differences.Add(id + ": no-star solution activated a socket");
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/starway20-replay-result.txt", "passed=" + passed + "/20\n" + string.Join("\n", differences) +
                "\n" + string.Join("\n", details));
            if (differences.Count > 0) throw new InvalidOperationException("Starway20 replay: " + passed + "/20; " + differences[0]);
            Debug.Log("[Starway20] native PuzzleDeviceSession replay 20/20 passed.");
            return "20/20";
        }
    }
}
