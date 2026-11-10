using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Run in an owned ET-20 PuzzlePreview after its freshly solved native replay.
// Exercise the swipe receiver, real controller and frame-based animation;
// compare each settled gameplay state against the independent native replay.
public static class VerifyEcho20Controller
{
    const string Folder = "Docs/Validation/EchoThrough20";
    const string Source = "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-20_MapData.asset";
    static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false);
    static PuzzlePrototypeController controller;
    static PuzzleDeviceSession oracle;
    static JObject record;
    static readonly JArray Steps = new JArray();
    static string route;
    static int next;
    static bool waiting, running;
    static double started, deadline;

    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    public static object Start()
    {
        if (running || !EditorApplication.isPlaying || EditorApplication.isPaused)
            throw new InvalidOperationException("Start in the isolated ET-20 preview, in running Play Mode.");
        controller = UnityEngine.Object.FindObjectsByType<PuzzlePrototypeController>(FindObjectsSortMode.None)
            .Single(c => c.stage && AssetDatabase.GetAssetPath(c.stage) == Source);
        record = JObject.Parse(File.ReadAllText(Folder + "/Replay/ET-20.json"));
        if ((string)record["status"] != "solved" || controller.Session == null || controller.Session.Cost != 0 ||
            controller.IsMoving || controller.RuntimeMap.targetBubbles != 4 ||
            Hash(Source) != (string)record["sourceSha256"] || PuzzleShortestPathSolver.SourceIsDirty(controller.stage))
            throw new InvalidOperationException("Expected a fresh, unchanged four-bubble preview and confirmed route.");
        route = (string)record["route"];
        oracle = new PuzzleDeviceSession(controller.RuntimeMap);
        Steps.Clear(); next = 0; waiting = false; running = true;
        started = EditorApplication.timeSinceStartup; deadline = started + 15;
        File.WriteAllText(Folder + "/et20_controller_status.json", "{\"status\":\"running\"}", Utf8);
        EditorApplication.update += Tick;
        return new { inputs = route.Length, target = controller.RuntimeMap.targetBubbles };
    }
    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || !controller) throw new InvalidOperationException("Preview ended during replay.");
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Animation did not settle at input " + next);
            if (controller.IsMoving) return;
            if (waiting)
            {
                var state = controller.Session;
                bool visual = ((Vector2)controller.Player.position - (Vector2)state.Cell).sqrMagnitude < .0001f;
                if (state.SearchStateKey() != oracle.SearchStateKey() || state.Cost != oracle.Cost ||
                    state.Won != oracle.Won || state.Failed || !visual)
                    throw new InvalidOperationException("Controller diverged at settled input " + next);
                Steps.Add(JObject.FromObject(new { step = next, input = route[next - 1].ToString(),
                    x = state.Cell.x, y = state.Cell.y, cost = state.Cost, collected = state.Collected,
                    won = state.Won, playerVisualMatchesCell = visual, nativeStateMatches = true,
                    missingBindings = controller.Devices.MissingBindings }));
                waiting = false;
            }
            if (next == route.Length)
            {
                var state = controller.Session;
                if (!state.Won || state.Collected != 4 || state.Cell != controller.RuntimeMap.exit ||
                    state.Cost != (int)record["distance"] || Hash(Source) != (string)record["sourceSha256"])
                    throw new InvalidOperationException("Four-bubble controller replay did not finish at the proven cost.");
                Finish(new JObject { ["status"] = "passed", ["sourceAsset"] = Source,
                    ["sourceSha256"] = (string)record["sourceSha256"], ["route"] = route,
                    ["distance"] = state.Cost, ["collected"] = state.Collected, ["target"] = 4,
                    ["won"] = true, ["exitReached"] = true, ["inputsThroughSwipeReceiver"] = true,
                    ["allAnimationsSettled"] = true, ["elapsedSeconds"] = EditorApplication.timeSinceStartup - started,
                    ["steps"] = Steps });
                return;
            }
            char input = route[next];
            if (!PuzzleRouteNotation.TryDirection(input, out var direction) || !oracle.ReplayRuntimeInput(direction) || oracle.Failed)
                throw new InvalidOperationException("Native reference rejected input " + (next + 1));
            var swipe = controller.GetComponent<PuzzleSwipeInput>();
            float drag = Mathf.Max(200, Screen.width) * .2f;
            var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
            swipe.BeginGesture(start, false);
            if (!swipe.EndGesture(start + (Vector2)direction * drag))
                throw new InvalidOperationException("Swipe receiver rejected input " + (next + 1) + ": " + controller.Status);
            next++; waiting = true; deadline = EditorApplication.timeSinceStartup + 15;
        }
        catch (Exception error)
        {
            Finish(new JObject { ["status"] = "error", ["input"] = next, ["detail"] = error.ToString(), ["steps"] = Steps });
        }
    }
    static void Finish(JObject report)
    {
        running = false; EditorApplication.update -= Tick;
        File.WriteAllText(Folder + "/et20_controller_replay.json", report.ToString(), Utf8);
        File.WriteAllText(Folder + "/et20_controller_status.json", new JObject {
            ["status"] = (string)report["status"], ["inputs"] = Steps.Count }.ToString(), Utf8);
    }
}
