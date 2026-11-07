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
        [System.Serializable] sealed class CatalogEntry { public string mapId, mapAsset, status, route; public int distance; }
        [System.Serializable] sealed class ReplayTrace { public string mapId = "SW19"; public int budget; public List<ReplayStep> steps = new List<ReplayStep>(); }
        [System.Serializable] sealed class ReplayStep { public int input, cost, x, y, windPhase, collected; public string arrow; public bool won; }

        static IEnumerator WaitForRest(PuzzlePrototypeController controller)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.IsMoving, Is.False, "Automatic movement did not settle.");
        }

        [UnityTest] public IEnumerator Sw19ImmediateWindAndFullRouteUseActualControllerAnimations()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW19_MapData.asset");
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
                controller.ResetStage(); yield return WaitForRest(controller);
                var entry = JsonUtility.FromJson<CatalogData>(controller.shortestPathCatalog.text).maps.Single(e => e.mapId == "SW19");
                var trace = new ReplayTrace { budget = entry.distance };
                foreach (char arrow in entry.route)
                {
                    if (PuzzleRouteNotation.IsWait(arrow)) { yield return WaitForRest(controller); continue; }
                    Assert.That(PuzzleRouteNotation.TryDirection(arrow, out var direction), Is.True, "SW19 guide must use arrows and explicit automatic-wind waits.");
                    Assert.That(controller.RequestMove(direction), Is.True, "Rejected input " + (trace.steps.Count + 1));
                    yield return WaitForRest(controller);
                    trace.steps.Add(new ReplayStep { input = trace.steps.Count + 1, arrow = arrow.ToString(), cost = controller.Session.Cost,
                        x = controller.Session.Cell.x, y = controller.Session.Cell.y, windPhase = controller.Session.States["wind_1"].phase,
                        collected = controller.Session.Collected, won = controller.Session.Won });
                    Assert.That(controller.Session.Failed, Is.False, "Input " + trace.steps.Count);
                    Assert.That(Vector2.Distance(controller.Player.position, controller.Session.Cell), Is.LessThan(.001f));
                }
                Assert.That(controller.Session.Won, Is.True);
                Assert.That(controller.Session.Cost, Is.EqualTo(entry.distance));
                Assert.That(controller.RuntimeMap.scenario.costLimit - controller.Session.Cost, Is.Zero);
                System.IO.File.WriteAllText("Temp/sw19_animated_replay.json", JsonUtility.ToJson(trace, true));
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
                foreach (var entry in JsonUtility.FromJson<CatalogData>(controller.shortestPathCatalog.text).maps.Where(e => e.status == "solved"))
                {
                    var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(entry.mapAsset);
                    int sourceLimit = source.scenario.costLimit;
                    controller.Load(source); controller.StopAllCoroutines();
                    if (!originalCamera && controller.FollowCamera) generatedCamera = controller.FollowCamera.gameObject;
                    Assert.That(controller.RuntimeMap.scenario, Is.Not.SameAs(source.scenario));
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
                Assert.That(checkedMaps, Is.EqualTo(19));
            }
            finally { Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera); }
        }
    }
}
