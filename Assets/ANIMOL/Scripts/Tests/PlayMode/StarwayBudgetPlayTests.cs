using System.Collections;
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
                            Assert.That(controller.Session.Apply(direction).accepted, Is.True, entry.mapId);
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
