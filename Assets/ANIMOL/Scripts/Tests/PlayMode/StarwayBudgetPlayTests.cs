using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class StarwayBudgetPlayTests
    {
        [System.Serializable] sealed class CatalogData { public CatalogEntry[] maps; }
        [System.Serializable] sealed class CatalogEntry { public string mapId, mapAsset, mapSha256, status, route; public int distance; }
        [System.Serializable] sealed class RouteStep { public int input, cost; public string arrow; public Vector2Int cell; public bool won; }
        [System.Serializable] sealed class RouteReplay
        {
            public string mapId, sourceSha256, route;
            public int solverVersion, distance, cost, runtimeBudget;
            public bool won, failed;
            public List<RouteStep> inputs = new List<RouteStep>();
        }

        static IEnumerator WaitForRest(PuzzlePrototypeController controller)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.IsMoving, Is.False, "Automatic movement did not settle.");
        }

        [UnityTest] public IEnumerator PlayerBouncesFromOpenReflectorFaceButClosedFaceCostsNothing()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.scenario = scenario; scenario.themeId = "starway";
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW00_MapData.asset").theme;
            map.Resize(5, 5); map.targetBubbles = 0;
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = new Vector2Int(2, 2); map.exit = new Vector2Int(4, 4);
            var reflector = new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = new Vector2Int(3, 2) };
            scenario.devices.Add(reflector);
            var originalCamera = Camera.main; GameObject generatedCamera = null;
            var host = new GameObject("Player reflector face actual input test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.secondsPerCell = .001f;
            try
            {
                foreach (bool allowed in new[] { false, true })
                {
                    reflector.orientation = allowed ? 2 : 0;
                    controller.Load(map);
                    if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                    yield return WaitForRest(controller);
                    var before = controller.Session.SearchStateKey();
                    Assert.That(controller.RequestMove(Vector2Int.right), Is.EqualTo(allowed));
                    Assert.That(controller.Session.Cost, Is.EqualTo(allowed ? 1 : 0));
                    if (!allowed)
                    {
                        Assert.That(controller.IsMoving, Is.False);
                        Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(before));
                    }
                    yield return WaitForRest(controller);
                    Assert.That(controller.Session.Cell, Is.EqualTo(allowed ? reflector.position + Vector2Int.up : map.start));
                    Assert.That((Vector2)controller.Player.position, Is.EqualTo((Vector2)controller.Session.Cell));
                }
            }
            finally
            {
                Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera);
                Object.Destroy(map); Object.Destroy(scenario);
            }
        }

        [UnityTest] public IEnumerator ClosedReflectorFaceRejectsSheepWithoutCostOrMovement()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.scenario = scenario; scenario.themeId = "starway";
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW00_MapData.asset").theme;
            map.Resize(5, 5); map.targetBubbles = 0;
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = new Vector2Int(1, 2); map.exit = new Vector2Int(4, 4);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", instanceId = "sheep", position = new Vector2Int(2, 2) });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = new Vector2Int(3, 2), orientation = 0 });
            var originalCamera = Camera.main; GameObject generatedCamera = null;
            var host = new GameObject("Closed reflector face actual input test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                yield return WaitForRest(controller);
                var before = controller.Session.SearchStateKey();
                Assert.That(controller.RequestMove(Vector2Int.right), Is.False);
                Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(before));
                Assert.That(controller.Session.Cost, Is.Zero);
                Assert.That(controller.IsMoving, Is.False);
                yield return null;
            }
            finally
            {
                Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera);
                Object.Destroy(map); Object.Destroy(scenario);
            }
        }

        [UnityTest] public IEnumerator BlockedBounceCannotExitThroughTheBackAndUndoRestoresTheInput()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.scenario = scenario; scenario.themeId = "starway";
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW00_MapData.asset").theme;
            map.Resize(5, 5); map.targetBubbles = 0;
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = new Vector2Int(2, 2); map.exit = new Vector2Int(4, 4);
            map.SetTerrain(new Vector2Int(3, 3), TerrainMaterial.Grass, true);
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce,
                position = new Vector2Int(3, 2), orientation = 2 });
            var originalCamera = Camera.main; GameObject generatedCamera = null;
            var host = new GameObject("Blocked player bounce actual input test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.secondsPerCell = .001f;
            try
            {
                controller.Load(map);
                if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                yield return WaitForRest(controller);
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                yield return WaitForRest(controller);
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(controller.Session.Direction, Is.EqualTo(Vector2Int.up));
                var stopped = controller.Session.SearchStateKey();
                foreach (var closedExit in new[] { Vector2Int.right, Vector2Int.down })
                {
                    Assert.That(controller.RequestMove(closedExit), Is.False);
                    Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(stopped));
                    Assert.That(controller.Session.Cost, Is.EqualTo(1));
                    Assert.That(controller.IsMoving, Is.False);
                    Assert.That((Vector2)controller.Player.position, Is.EqualTo(new Vector2(3, 2)));
                }
                Assert.That(controller.UndoLast(), Is.True);
                Assert.That(controller.Session.Cell, Is.EqualTo(map.start));
                Assert.That(controller.Session.Cost, Is.Zero);
                Assert.That((Vector2)controller.Player.position, Is.EqualTo((Vector2)map.start));
            }
            finally
            {
                Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera);
                Object.Destroy(map); Object.Destroy(scenario);
            }
        }

        [UnityTest] public IEnumerator Sw19ImmediateWindAndOpenReflectorEntryUseActualControllerAnimations()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW19_MapData.asset");
            PuzzleMap entryMap = null;
            var host = new GameObject("SW19 actual input timing test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.shortestPathCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayShortestPathCatalog.AssetPath);
            controller.secondsPerCell = .001f;
            var originalCamera = Camera.main; GameObject generatedCamera = null;
            try
            {
                controller.Load(source);
                if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                yield return WaitForRest(controller);
                foreach (char arrow in "←↓↓←↑←")
                {
                    Assert.That(PuzzleRouteNotation.TryDirection(arrow, out var direction), Is.True);
                    Assert.That(controller.RequestMove(direction), Is.True);
                    yield return WaitForRest(controller);
                }
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(13, 9)));
                Assert.That(controller.RequestMove(Vector2Int.down), Is.True);
                // No yield or second input: switching on has already committed the free wind.
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(11, 9)));
                Assert.That(controller.Session.Cost, Is.EqualTo(7));
                yield return WaitForRest(controller);
                // Isolate the reported entry on the authored SW19 board. With open-face
                // carriage enabled, the old full route now moves sheep earlier in the wind.
                entryMap = Object.Instantiate(source);
                entryMap.scenario = Object.Instantiate(source.scenario);
                entryMap.start = new Vector2Int(11, 9);
                foreach (var wind in entryMap.scenario.devices.Where(d => d.role == PrototypeRole.Wind))
                { wind.orientation = 0; wind.active = false; }
                entryMap.scenario.ranchV13Art.Single(a => a.family == "sheep" && a.position == new Vector2Int(12, 9)).position = new Vector2Int(11, 8);
                controller.Load(entryMap); yield return WaitForRest(controller);
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(11, 9)));
                Assert.That(controller.Session.Cost, Is.Zero);
                Assert.That(controller.Session.RanchSheep.Values, Does.Contain(new Vector2Int(11, 8)));
                int costBefore = controller.Session.Cost;
                var sheepId = controller.Session.RanchSheep.Single(s => s.Value == new Vector2Int(11, 8)).Key;
                bool accepted = controller.RequestMove(Vector2Int.down);
                Assert.That(accepted, Is.True, "The north face of reflector (11,7) is open to this push.");
                Assert.That(controller.Session.Cost, Is.EqualTo(costBefore + 1));
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(11, 9)));
                Assert.That(controller.Session.RanchSheep[sheepId], Is.EqualTo(new Vector2Int(11, 7)));
                yield return WaitForRest(controller);
            }
            finally
            {
                Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera);
                if (entryMap) { Object.Destroy(entryMap.scenario); Object.Destroy(entryMap); }
            }
        }
        [UnityTest] public IEnumerator Sw19VerifiedRouteClearsThroughQueuedControllerInputs() =>
            ReplayVerifiedRoute("SW19", "Temp/rule9_animated_replay.json");

        [UnityTest] public IEnumerator Sw06VerifiedRouteClearsThroughQueuedControllerInputs() =>
            ReplayVerifiedRoute("SW06", "Temp/sw06_retry_animated_replay.json");

        IEnumerator ReplayVerifiedRoute(string mapId, string replayPath)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayShortestPathCatalog.AssetPath);
            var entry = JsonUtility.FromJson<CatalogData>(catalog.text).maps.Single(e => e.mapId == mapId && e.status == "solved");
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(entry.mapAsset);
            int sourceBudget = source.scenario.costLimit;
            var originalCamera = Camera.main; GameObject generatedCamera = null;
            var host = new GameObject(mapId + " verified queued-input route");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.shortestPathCatalog = catalog; controller.secondsPerCell = .001f;
            try
            {
                controller.Load(source);
                if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                Assert.That(controller.RuntimeMap.scenario.costLimit, Is.EqualTo(entry.distance));
                var shadow = new PuzzleDeviceSession(controller.RuntimeMap);
                if (shadow.Devices.Any(d => d.role == PrototypeRole.Wind)) shadow.AdvanceWind();
                Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(shadow.SearchStateKey()));
                var replay = new RouteReplay { mapId = entry.mapId, sourceSha256 = entry.mapSha256, route = entry.route,
                    solverVersion = StarwayShortestPathCatalog.SolverVersion, distance = entry.distance,
                    runtimeBudget = controller.RuntimeMap.scenario.costLimit };
                int input = 0;
                foreach (char action in entry.route)
                {
                    if (char.IsWhiteSpace(action)) continue;
                    float deadline = Time.realtimeSinceStartup + 10f;
                    if (PuzzleRouteNotation.IsWait(action))
                    {
                        Assert.That(shadow.AdvanceWind().accepted, Is.True);
                        while (controller.Session.SearchStateKey() != shadow.SearchStateKey() && Time.realtimeSinceStartup < deadline)
                            yield return null;
                    }
                    else
                    {
                        Assert.That(PuzzleRouteNotation.TryDirection(action, out var direction), Is.True);
                        Assert.That(shadow.ReplayRuntimeInput(direction), Is.True);
                        // Queue exactly one next input during the current animation. Waiting
                        // for all wind to settle here would change the searched input order.
                        Assert.That(controller.RequestMove(direction), Is.True, "input " + (input + 1));
                        while (controller.Session.Cost < shadow.Cost && Time.realtimeSinceStartup < deadline)
                            yield return null;
                        input++;
                    }
                    Assert.That(controller.Session.Cost, Is.EqualTo(shadow.Cost), "input " + input);
                    Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(shadow.SearchStateKey()), "input " + input);
                    Assert.That(controller.Session.Failed, Is.False, controller.Status);
                    replay.inputs.Add(new RouteStep { input = input, arrow = action.ToString(), cost = controller.Session.Cost,
                        cell = controller.Session.Cell, won = controller.Session.Won });
                }
                yield return WaitForRest(controller);
                Assert.That(controller.Session.Won, Is.True);
                Assert.That(controller.Session.Cost, Is.EqualTo(entry.distance));
                Assert.That((Vector2)controller.Player.position, Is.EqualTo((Vector2)controller.Session.Cell));
                Assert.That(source.scenario.costLimit, Is.EqualTo(sourceBudget));
                replay.cost = controller.Session.Cost; replay.won = controller.Session.Won; replay.failed = controller.Session.Failed;
                System.IO.File.WriteAllText(replayPath, JsonUtility.ToJson(replay, true) + "\n");
            }
            finally { Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera); }
        }

        [UnityTest] public IEnumerator RuntimeBudgetsApplyToAllSolvedStagesAndResetWithoutEditingSources()
        {
            var host = new GameObject("Verified Starway budget test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.shortestPathCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayShortestPathCatalog.AssetPath);
            controller.secondsPerCell = .001f;
            var originalCamera = Camera.main;
            GameObject generatedCamera = null;
            int checkedMaps = 0;
            try
            {
                var catalog = JsonUtility.FromJson<CatalogData>(controller.shortestPathCatalog.text);
                foreach (var entry in catalog.maps.Where(e => e.status == "solved"))
                {
                    var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(entry.mapAsset);
                    int sourceLimit = source.scenario.costLimit;
                    controller.Load(source); controller.StopAllCoroutines();
                    if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                    Assert.That(controller.RuntimeMap.scenario, Is.Not.SameAs(source.scenario));
                    Assert.That(StarwayShortestPathCatalog.TryGetBudget(source, controller.shortestPathCatalog, out var budget), Is.True, entry.mapId);
                    Assert.That(budget, Is.EqualTo(entry.distance));
                    Assert.That(controller.RuntimeMap.scenario.costLimit, Is.EqualTo(entry.distance), entry.mapId);
                    Assert.That(controller.Session.Cost, Is.Zero);
                    foreach (char action in entry.route)
                    {
                        if (char.IsWhiteSpace(action)) continue;
                        if (PuzzleRouteNotation.IsWait(action)) Assert.That(controller.Session.AdvanceWind().accepted, Is.True);
                        else
                        {
                            Assert.That(PuzzleRouteNotation.TryDirection(action, out var direction), Is.True);
                            Assert.That(controller.Session.ReplayRuntimeInput(direction), Is.True, entry.mapId);
                        }
                    }
                    Assert.That(controller.Session.Won, Is.True, entry.mapId);
                    Assert.That(controller.Session.Failed, Is.False);
                    Assert.That(controller.RuntimeMap.scenario.costLimit - controller.Session.Cost, Is.Zero);
                    controller.ResetStage(); controller.StopAllCoroutines();
                    Assert.That(controller.Session.Cost, Is.Zero);
                    Assert.That(controller.RuntimeMap.scenario.costLimit, Is.EqualTo(entry.distance));
                    Assert.That(source.scenario.costLimit, Is.EqualTo(sourceLimit));
                    checkedMaps++; yield return null;
                }
                Assert.That(checkedMaps, Is.EqualTo(catalog.maps.Count(e => e.status == "solved")));
            }
            finally { Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera); }
        }
    }
}
