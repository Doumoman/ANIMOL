using System.Collections;
using System.Linq;
using ANIMOL.Art.SanctumConnectedDecorV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class SanctumConnectedDecorPlayTests
    {
        const string Base = "Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01/_Generated/InspectionBoards/";

        [UnityTest]
        public IEnumerator StageAndAll26ScenesRenderImportedSprites()
        {
#if UNITY_EDITOR
            foreach (var row in new[] { new { Name = "Stage", Network = 55, Groups = 20, Claims = 102 },
                                        new { Name = "All26", Network = 78, Groups = 20, Claims = 125 } })
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    Base + row.Name + "/SanctumDecor" + row.Name + ".unity",
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return null;
                yield return null;
                var binding = Object.FindFirstObjectByType<SanctumConnectedDecorV01Binding>();
                Assert.NotNull(binding, row.Name);
                var source = binding.GetComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>();
                var controller = binding.GetComponent<SanctumConnectedDecorV01Controller>();
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
                    UnityEditor.AssetDatabase.GetAssetPath(r.sprite).Contains("ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01")));
                Assert.IsTrue(binding.LastReport.success, binding.LastDiagnostic);
            }
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator ActualSanctumPrototypePublishesCommittedEditsWithoutChangingCost()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_letter_observatory_Prototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            yield return null;
            var game = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var view = Object.FindFirstObjectByType<PuzzleMapView>();
            Assert.NotNull(game); Assert.NotNull(view);
            var map = game.RuntimeMap;
            var catalog = map.theme.sanctumConnectedDecorV01Catalog;
            Assert.NotNull(catalog);
            Assert.IsTrue(catalog.TryGetGroup("sanctum_altar_3x2",
                map.theme.sanctumConnectedDecorV01ManifestPin.text.Trim(), out _, out var art, out var error), error);
            var controller = view.SanctumConnectedDecor;
            Assert.NotNull(controller);
            var source = controller.GetComponent<SanctumConnectedDecorV01PublishedSnapshotSource>();
            var binding = controller.GetComponent<SanctumConnectedDecorV01Binding>();
            Assert.IsFalse(binding.AllowExampleSource);
            var cost = game.Session.Cost;
            var originalGroups = controller.CurrentPlan.GroupCount;
            var found = false;
            for (var y = 0; y <= map.Height - 2 && !found; y++)
                for (var x = 0; x <= map.Width - 3 && !found; x++)
                {
                    var candidate = new DecorationCell { position = new Vector2Int(x, y), sprite = art.Sprite,
                        tint = Color.white, visualFamily = "sanctum_altar_3x2", placementId = "prototype-test-altar" };
                    if (!SanctumConnectedDecorHost.TryCreateSnapshot(map,
                        map.decorations.Concat(new[] { candidate }).ToArray(), new Vector3(-.5f, -.5f),
                        1, 1, out _, out _)) continue;
                    map.decorations.Add(candidate); map.NotifyDecorationsChanged(); found = true;
                }
            Assert.IsTrue(found, "The current Sanctum prototype has no approved 3x2 decoration space.");
            Assert.AreEqual(originalGroups + 1, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(1, controller.GetComponentsInChildren<SpriteRenderer>().Count(r => r.sprite == art.Sprite));
            Assert.IsTrue(source.TryCaptureSnapshot(out var snapshot, out error), error);
            Assert.IsFalse(snapshot.isExampleOnly);
            Assert.AreEqual(cost, game.Session.Cost);
            map.decorations.RemoveAt(map.decorations.Count - 1); map.NotifyDecorationsChanged();
            Assert.AreEqual(originalGroups, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(cost, game.Session.Cost);
            map.decorations.Add(new DecorationCell { position = new Vector2Int(4, 4), sprite = art.Sprite,
                tint = Color.white, visualFamily = "sanctum_altar_3x2", placementId = "prototype-pool-altar" });
            map.NotifyDecorationsChanged();
            var oldSource = source;
            view.gameObject.SetActive(false);
            Assert.IsFalse(oldSource.TryCaptureSnapshot(out _, out _));
            view.gameObject.SetActive(true);
            yield return null;
            Assert.NotNull(view.SanctumConnectedDecor);
            Assert.AreEqual(originalGroups + 1, view.SanctumConnectedDecor.CurrentPlan.GroupCount);
            Assert.IsTrue(view.SanctumConnectedDecor.GetComponent<SanctumConnectedDecorV01PublishedSnapshotSource>()
                .TryCaptureSnapshot(out snapshot, out error), error);
            Assert.IsFalse(snapshot.isExampleOnly);
            Assert.AreEqual(cost, game.Session.Cost);
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator PublishedSourceRequiresFreshPublishAfterPoolDisable()
        {
            var node = new GameObject("sanctum published pool test");
            var source = node.AddComponent<SanctumConnectedDecorV01PublishedSnapshotSource>();
            var snapshot = new SanctumConnectedDecorV01BoardSnapshot {
                schemaVersion = 1, packageId = SanctumConnectedDecorV01Catalog.PackageId,
                sourceManifestSha256 = SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256,
                boardId = "pool-test", revision = 1, isWholeBoard = true, isExampleOnly = false,
                cellSize = 1, postSpacing = 4, networkCells = new SanctumDecorNetworkCell[0],
                groupPlacements = new SanctumDecorGroupPlacement[0], allowedDecorCells = new SanctumDecorAllowedCell[0]
            };
            Assert.IsTrue(source.PublishSnapshot(snapshot, out var error), error);
            node.SetActive(false); yield return null; node.SetActive(true); yield return null;
            Assert.IsFalse(source.TryCaptureSnapshot(out _, out _));
            Assert.IsTrue(source.PublishSnapshot(snapshot, out error), error);
            Assert.IsTrue(source.TryCaptureSnapshot(out _, out error), error);
            Object.Destroy(node);
        }
    }
}
