using System.Collections;
using System.Linq;
using ANIMOL.Art.RanchConnectedDecorV02;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class RanchConnectedDecorV02PlayTests
    {
        const string InspectionRoot = "Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/InspectionBoards/";

        [UnityTest]
        public IEnumerator StageAndAll26ScenesRenderImportedSprites()
        {
#if UNITY_EDITOR
            foreach (var row in new[] { new { Name = "Stage", Network = 65, Groups = 20, Claims = 120 },
                                        new { Name = "All26", Network = 96, Groups = 20, Claims = 151 } })
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    InspectionRoot + row.Name + "/RanchDecor" + row.Name + ".unity",
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return null;
                yield return null;
                var binding = Object.FindFirstObjectByType<RanchConnectedDecorV02Binding>();
                Assert.NotNull(binding, row.Name);
                var source = binding.GetComponent<RanchConnectedDecorV02AuthoredSnapshotSource>();
                var controller = binding.GetComponent<RanchConnectedDecorV02Controller>();
                Assert.NotNull(source); Assert.NotNull(controller);
                Assert.IsTrue(source.TryCaptureSnapshot(out var snapshot, out var error), error);
                Assert.IsTrue(snapshot.isExampleOnly);
                Assert.IsTrue(binding.RefreshNow(out error), error);
                Assert.AreEqual(row.Network, controller.CurrentPlan.NetworkCount);
                Assert.AreEqual(row.Groups, controller.CurrentPlan.GroupCount);
                Assert.AreEqual(row.Claims, controller.CurrentPlan.ClaimCount);
                var renderers = controller.GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(controller.OwnedVisualCount, renderers.Length);
                Assert.GreaterOrEqual(renderers.Length, row.Network + row.Groups);
                Assert.IsTrue(renderers.All(r => r.enabled && r.gameObject.activeInHierarchy && r.sprite &&
                    Mathf.Abs(r.sprite.pixelsPerUnit - 256) < .001f &&
                    UnityEditor.AssetDatabase.GetAssetPath(r.sprite).Contains("ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02")));
                Assert.IsTrue(binding.LastReport.success, binding.LastDiagnostic);
            }
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator ActualRanchPrototypePublishesCommittedEditsWithoutChangingCost()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            yield return null;
            var game = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var view = Object.FindFirstObjectByType<PuzzleMapView>();
            Assert.NotNull(game); Assert.NotNull(view);
            var map = game.RuntimeMap;
            var catalog = map.theme.ranchConnectedDecorV02Catalog;
            Assert.NotNull(catalog);
            Assert.IsTrue(catalog.TryGetGroup("honey_jar_stand",
                map.theme.ranchConnectedDecorV02ManifestPin.text.Trim(), out _, out var art, out var error), error);
            var controller = view.ConnectedDecor;
            Assert.NotNull(controller);
            var source = controller.GetComponent<RanchConnectedDecorV02PublishedSnapshotSource>();
            var binding = controller.GetComponent<RanchConnectedDecorV02Binding>();
            Assert.IsFalse(binding.AllowExampleSource);
            var cost = game.Session.Cost;
            var originalGroups = controller.CurrentPlan.GroupCount;
            var found = false;
            for (var y = 0; y < map.Height && !found; y++)
                for (var x = 0; x < map.Width && !found; x++)
                {
                    var candidate = new DecorationCell { position = new Vector2Int(x, y), sprite = art.Sprite,
                        tint = Color.white, visualFamily = "honey_jar_stand", placementId = "prototype-test-jar" };
                    if (!RanchConnectedDecorV02Host.TryCreateSnapshot(map,
                        map.decorations.Concat(new[] { candidate }).ToArray(), new Vector3(-.5f, -.5f),
                        1, 1, out _, out _)) continue;
                    map.decorations.Add(candidate); map.NotifyDecorationsChanged(); found = true;
                }
            Assert.IsTrue(found, "The current Ranch prototype has no approved point decoration cell.");
            Assert.AreEqual(originalGroups + 1, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(1, controller.GetComponentsInChildren<SpriteRenderer>().Count(r => r.sprite == art.Sprite));
            Assert.IsTrue(source.TryCaptureSnapshot(out var snapshot, out error), error);
            Assert.IsFalse(snapshot.isExampleOnly);
            Assert.AreEqual(cost, game.Session.Cost);
            map.decorations.RemoveAt(map.decorations.Count - 1); map.NotifyDecorationsChanged();
            Assert.AreEqual(originalGroups, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(cost, game.Session.Cost);
            view.gameObject.SetActive(false);
            Assert.IsFalse(source.TryCaptureSnapshot(out _, out _));
            view.gameObject.SetActive(true);
            yield return null;
            Assert.NotNull(view.ConnectedDecor);
            Assert.IsTrue(view.ConnectedDecor.GetComponent<RanchConnectedDecorV02PublishedSnapshotSource>()
                .TryCaptureSnapshot(out snapshot, out error), error);
            Assert.IsFalse(snapshot.isExampleOnly);
            Assert.AreEqual(cost, game.Session.Cost);
#else
            yield break;
#endif
        }
    }
}
