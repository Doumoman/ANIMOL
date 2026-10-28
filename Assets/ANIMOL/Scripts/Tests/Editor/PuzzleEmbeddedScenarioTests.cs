using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests
{
    public sealed class PuzzleEmbeddedScenarioTests
    {
        [Test] public void AllMapDataStagesOwnTheirScenarioWithoutSeparateFiles()
        {
            const string root = "Assets/ANIMOL/Data/Puzzle/MapData";
            foreach (var theme in new[] { (folder: "starway", count: 20),
                (folder: "memory_theater", count: 35) })
            {
                var paths = AssetDatabase.FindAssets("t:PuzzleMap", new[] { root + "/" + theme.folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith("_MapData.asset"))
                    .OrderBy(p => p).ToArray();
                Assert.That(paths.Length, Is.EqualTo(theme.count), theme.folder);
                foreach (var path in paths)
                {
                    var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
                    Assert.That(map.scenario, Is.Not.Null, path);
                    Assert.That(AssetDatabase.GetAssetPath(map.scenario), Is.EqualTo(path), path);
                    Assert.That(AssetDatabase.IsSubAsset(map.scenario), Is.True, path);
                    Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).OfType<PuzzleScenario>(),
                        Is.EquivalentTo(new[] { map.scenario }), path);
                    Assert.That(AssetDatabase.LoadMainAssetAtPath(
                        path.Replace("_MapData.asset", "_Scenario.asset")), Is.Null, path);
                    Assert.That(map.ValidateMap(), Is.Empty, path);
                }
            }
        }
    }
}
