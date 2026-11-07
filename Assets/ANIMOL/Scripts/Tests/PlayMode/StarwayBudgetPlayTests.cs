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
        [System.Serializable] sealed class RejectionTrace
        {
            public string mapId = "SW19", recordStatus, reason;
            public bool accepted;
            public int costBefore, costAfter, runtimeCostLimit;
            public Vector2Int player, sheep, reflector;
        }

        static IEnumerator WaitForRest(PuzzlePrototypeController controller)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.IsMoving, Is.False, "Automatic movement did not settle.");
        }

        [UnityTest] public IEnumerator Sw19ImmediateWindAndReflectorRejectionUseActualControllerAnimations()
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
                Assert.That(entry.status, Is.EqualTo("unreachable"));
                Assert.That(entry.distance, Is.EqualTo(-1));
                Assert.That(entry.route, Is.Empty);
                Assert.That(controller.RuntimeMap.scenario.costLimit, Is.EqualTo(source.scenario.costLimit));
                foreach (char arrow in "←↓↓←↑←←→↓←←←↑←")
                {
                    Assert.That(PuzzleRouteNotation.TryDirection(arrow, out var direction), Is.True);
                    Assert.That(controller.RequestMove(direction), Is.True);
                    yield return WaitForRest(controller);
                }
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(11, 9)));
                Assert.That(controller.Session.Cost, Is.EqualTo(14));
                Assert.That(controller.Session.RanchSheep.Values, Does.Contain(new Vector2Int(11, 8)));
                var before = controller.Session.SearchStateKey();
                int costBefore = controller.Session.Cost;
                bool accepted = controller.RequestMove(Vector2Int.down);
                Assert.That(accepted, Is.False, "The old route tried to push sheep onto reflector (11,7).");
                Assert.That(controller.Session.Cost, Is.EqualTo(costBefore));
                Assert.That(controller.Session.SearchStateKey(), Is.EqualTo(before));
                Assert.That(controller.IsMoving, Is.False);
                System.IO.File.WriteAllText("Temp/sw19_reflector_rejection.json", JsonUtility.ToJson(new RejectionTrace {
                    recordStatus = entry.status, accepted = accepted, reason = controller.Status,
                    costBefore = costBefore, costAfter = controller.Session.Cost, runtimeCostLimit = controller.RuntimeMap.scenario.costLimit,
                    player = controller.Session.Cell, sheep = new Vector2Int(11, 8), reflector = new Vector2Int(11, 7)
                }, true));
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
                Assert.That(checkedMaps, Is.EqualTo(18));
            }
            finally { Object.Destroy(host); if (generatedCamera) Object.Destroy(generatedCamera); }
        }
    }
}
