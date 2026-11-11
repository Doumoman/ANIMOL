using System.Linq;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using William.NotebookUI;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarwayStageOrderTests
    {
        [Test] public void FileRenameChangesAssignmentEvenWithAStaleObjectName()
        {
            string folder = "Assets/__StarwayStageOrderTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            scenario.themeId = "starway";
            map.scenario = scenario;
            try
            {
                AssetDatabase.CreateAsset(map, folder + "/SW02_MapData.asset");
                AssetDatabase.AddObjectToAsset(scenario, map);
                map.name = "Old internal object name";
                Assert.That(NotebookCampaignStages.DisplayId(map), Is.EqualTo("SW03"));
                Assert.That(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(map), "SW11_MapData"), Is.Empty);
                map.name = "Still stale";
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { map }, "SW03"), Is.Null);
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { map }, "SW12"), Is.SameAs(map));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        [TestCase("SW20_MapData")]
        [TestCase("SW21_MapData")]
        [TestCase("SW+1_MapData")]
        [TestCase("SW1_MapData")]
        [TestCase("Renaming_MapData")]
        public void InvalidNamesCannotOccupyAStage(string name)
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.name = name; map.scenario = scenario; scenario.themeId = "starway";
            try
            {
                Assert.That(NotebookCampaignStages.TryStarwayStageId(map, out _), Is.False);
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { map }, "SW20"), Is.Null);
            }
            finally { Object.DestroyImmediate(map); Object.DestroyImmediate(scenario); }
        }

        [Test] public void DuplicatePositionsFailRegardlessOfArrayOrder()
        {
            var a = ScriptableObject.CreateInstance<PuzzleMap>();
            var b = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            a.name = "SW04"; b.name = "SW04_MapData";
            a.scenario = b.scenario = scenario; scenario.themeId = "starway";
            try
            {
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { a, b }, "SW05"), Is.Null);
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { b, a }, "SW05"), Is.Null);
                scenario.themeId = "memory_theater";
                Assert.That(NotebookCampaignStages.FindStarwayMap(new[] { a }, "SW05"), Is.Null);
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(scenario); }
        }

        [Test] public void ProgressAndEvidenceIdentityFollowTheMapAfterNamesAreSwapped()
        {
            var catalog = StarwayStageCatalog.Current;
            var a = ScriptableObject.CreateInstance<PuzzleMap>();
            var b = ScriptableObject.CreateInstance<PuzzleMap>();
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            scenario.themeId = "starway"; a.scenario = b.scenario = scenario;
            var first = new StarwayStageCatalog.Entry { map = a, sourceId = "test-" + System.Guid.NewGuid().ToString("N") };
            var second = new StarwayStageCatalog.Entry { map = b, sourceId = "test-" + System.Guid.NewGuid().ToString("N") };
            catalog.maps.Add(first); catalog.maps.Add(second);
            string key = NotebookProjectGateway.MapClearKey(first.sourceId);
            try
            {
                a.name = "SW01_MapData"; b.name = "SW11_MapData";
                PlayerPrefs.SetInt(key, 1);
                a.name = "SW11_MapData"; b.name = "SW01_MapData";
                var maps = new[] { a, b };
                Assert.That(NotebookCampaignStages.FindStarwayMap(maps, "SW02"), Is.SameAs(b));
                Assert.That(NotebookCampaignStages.FindStarwayMap(maps.Reverse(), "SW12"), Is.SameAs(a));
                Assert.That(NotebookProjectGateway.IsStarwayMapCleared(a), Is.True);
                Assert.That(NotebookProjectGateway.IsStarwayMapCleared(b), Is.False);
                Assert.That(PuzzleShortestPathRecord.MapId(a), Is.EqualTo(first.sourceId));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
                catalog.maps.Remove(first); catalog.maps.Remove(second);
                Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(scenario);
            }
        }

        [Test] public void BuildCatalogPreservesSourceIdsAndCurrentFileNames()
        {
            var catalog = StarwayStageCatalog.Current;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.maps.Count, Is.EqualTo(20));
            Assert.That(catalog.maps.Select(entry => entry.sourceId).Distinct().Count(), Is.EqualTo(20));
            var maps = catalog.maps.Select(entry => entry.map).Reverse().ToArray();
            foreach (var entry in catalog.maps)
            {
                Assert.That(entry.map, Is.Not.Null);
                Assert.That(entry.assetName, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(entry.map))));
                Assert.That(entry.assetSha256, Is.EqualTo(PuzzleShortestPathSolver.HashAsset(entry.map)));
                Assert.That(NotebookCampaignStages.FindStarwayMap(maps, NotebookCampaignStages.DisplayId(entry.map)), Is.SameAs(entry.map));
            }
        }
    }
}
