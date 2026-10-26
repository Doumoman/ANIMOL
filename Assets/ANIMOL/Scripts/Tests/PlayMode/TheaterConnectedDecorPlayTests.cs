using System.Collections;
using System.Linq;
using ANIMOL.Art.TheaterConnectedDecorV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class TheaterConnectedDecorPlayTests
    {
        const string Base = "Assets/ANIMOL/Art/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/_Generated/InspectionBoards/";

        [UnityTest]
        public IEnumerator ExistingTheaterPrototypeUsesThePublishedSourceWithoutChargingTurns()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            yield return null;
            var game = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var view = Object.FindFirstObjectByType<PuzzleMapView>();
            Assert.NotNull(game); Assert.NotNull(view);
            var map = game.RuntimeMap;
            var catalog = map.theme.theaterConnectedDecorV01Catalog;
            Assert.NotNull(catalog);
            Assert.IsTrue(catalog.TryGetGroup("grand_piano_3x2",
                map.theme.theaterConnectedDecorV01ManifestPin.text.Trim(), out _, out var art, out var error), error);
            var controller = view.TheaterConnectedDecor;
            Assert.NotNull(controller);
            var source = controller.GetComponent<TheaterConnectedDecorV01PublishedSnapshotSource>();
            var binding = controller.GetComponent<TheaterConnectedDecorV01Binding>();
            Assert.IsFalse(binding.AllowExampleSource);
            var cost = game.Session.Cost;
            var initialCount = controller.CurrentPlan.GroupCount;
            var found = false;
            for (int y = 0; y <= map.Height - 2 && !found; y++)
                for (int x = 0; x <= map.Width - 3 && !found; x++)
                {
                    var proposal = new DecorationCell { position = new Vector2Int(x, y), sprite = art.Sprite,
                        tint = Color.white, visualFamily = "grand_piano_3x2", placementId = "prototype-test-piano" };
                    var candidate = map.decorations.Concat(new[] { proposal }).ToArray();
                    if (!TheaterConnectedDecorHost.TryCreateSnapshot(map, candidate, new Vector3(-.5f, -.5f),
                        1, 1, out _, out _)) continue;
                    map.decorations.Add(proposal); map.NotifyDecorationsChanged(); found = true;
                }
            Assert.IsTrue(found, "The current prototype has no approved 3x2 decoration space.");
            Assert.AreEqual(initialCount + 1, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.IsTrue(source.TryCaptureSnapshot(out var snapshot, out error), error);
            Assert.IsFalse(snapshot.isExampleOnly);
            Assert.AreEqual(cost, game.Session.Cost);
            map.decorations.RemoveAt(map.decorations.Count - 1); map.NotifyDecorationsChanged();
            Assert.AreEqual(initialCount, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(cost, game.Session.Cost);
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator StageAndAll26ScenesRenderTheirActualImportedSprites()
        {
#if UNITY_EDITOR
            foreach (var pair in new[] { new { Name = "Stage", Network = 48, Groups = 16, Claims = 88 },
                                          new { Name = "All26", Network = 78, Groups = 20, Claims = 123 } })
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    Base + pair.Name + "/TheaterDecor" + pair.Name + ".unity",
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return null;
                yield return null;
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        Assert.AreEqual(0, UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                            child.gameObject), child.name);
                var binding = Object.FindFirstObjectByType<TheaterConnectedDecorV01Binding>();
                Assert.NotNull(binding, pair.Name);
                var source = binding.GetComponent<TheaterConnectedDecorV01AuthoredSnapshotSource>();
                var controller = binding.GetComponent<TheaterConnectedDecorV01Controller>();
                Assert.NotNull(source); Assert.NotNull(controller);
                Assert.IsTrue(source.TryCaptureSnapshot(out var snapshot, out var error), error);
                Assert.IsTrue(snapshot.isExampleOnly);
                Assert.IsTrue(binding.RefreshNow(out error), error);
                Assert.AreEqual(pair.Network, controller.CurrentPlan.NetworkCount);
                Assert.AreEqual(pair.Groups, controller.CurrentPlan.GroupCount);
                Assert.AreEqual(pair.Claims, controller.CurrentPlan.ClaimCount);
                var renderers = controller.GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(controller.OwnedVisualCount, renderers.Length);
                Assert.GreaterOrEqual(renderers.Length, pair.Network + pair.Groups);
                Assert.IsTrue(renderers.All(r => r.enabled && r.gameObject.activeInHierarchy && r.sprite &&
                    Mathf.Abs(r.sprite.pixelsPerUnit - 256) < .001f &&
                    UnityEditor.AssetDatabase.GetAssetPath(r.sprite).Contains("ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01")));
                Assert.IsTrue(binding.LastReport.success, binding.LastDiagnostic);
            }
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator ActualTheaterMapPublishesCommittedChangesAndClearsOnPoolDisable()
        {
#if UNITY_EDITOR
            var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            Assert.NotNull(theme);
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            map.name = "runtime-theater-host"; map.theme = theme; map.Resize(10, 10);
            map.start = new Vector2Int(1, 1); map.exit = new Vector2Int(8, 8);
            for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            Assert.IsTrue(theme.theaterConnectedDecorV01Catalog.TryGetGroup("grand_piano_3x2",
                theme.theaterConnectedDecorV01ManifestPin.text.Trim(), out _, out var art, out var error), error);
            map.decorations.Add(new DecorationCell { position = new Vector2Int(4, 4), sprite = art.Sprite,
                tint = Color.white, visualFamily = "grand_piano_3x2", placementId = "runtime-piano" });
            var host = new GameObject("runtime theater view");
            var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
            yield return null;
            var controller = view.TheaterConnectedDecor;
            Assert.NotNull(controller);
            var source = controller.GetComponent<TheaterConnectedDecorV01PublishedSnapshotSource>();
            var binding = controller.GetComponent<TheaterConnectedDecorV01Binding>();
            Assert.IsFalse(binding.AllowExampleSource);
            Assert.AreEqual(1, controller.CurrentPlan.GroupCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            map.decorations.Clear(); map.NotifyDecorationsChanged();
            Assert.AreEqual(0, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            Assert.AreEqual(0, controller.OwnedVisualCount);
            Assert.IsTrue(source.TryCaptureSnapshot(out var captured, out error), error);
            Assert.AreEqual(1, captured.revision);
            var legacy = theme.memorySkyTheater.v02.Find("props/organ.png");
            Assert.NotNull(legacy);
            map.decorations.Add(new DecorationCell { position = new Vector2Int(2, 2),
                sprite = legacy, tint = Color.white });
            map.NotifyDecorationsChanged();
            Assert.AreEqual(1, host.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Decoration"));
            map.decorations.Clear(); map.NotifyDecorationsChanged();
            Assert.AreEqual(0, host.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Decoration"));
            map.decorations.Add(new DecorationCell { position = new Vector2Int(4, 4), sprite = art.Sprite,
                tint = Color.white, visualFamily = "grand_piano_3x2", placementId = "runtime-piano-2" });
            map.NotifyDecorationsChanged();
            Assert.AreEqual(1, controller.CurrentPlan.GroupCount, binding.LastDiagnostic);
            host.SetActive(false);
            Assert.IsFalse(source.TryCaptureSnapshot(out _, out _));
            host.SetActive(true);
            yield return null;
            Assert.NotNull(view.TheaterConnectedDecor);
            Assert.AreEqual(1, view.TheaterConnectedDecor.CurrentPlan.GroupCount);
            Object.Destroy(host); Object.Destroy(map);
#else
            yield break;
#endif
        }
    }
}
