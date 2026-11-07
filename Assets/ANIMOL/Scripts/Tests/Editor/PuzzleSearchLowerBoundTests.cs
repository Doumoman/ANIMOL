using System.Collections;
using System.Linq;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class PuzzleSearchLowerBoundTests
    {
        static PuzzleMap Load(string id) => AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        [TestCase("SW02")] [TestCase("SW16")] [TestCase("SW17")] [TestCase("SW18")]
        public void BoundIsConsistentAcrossActualPushFollowAndCollectionTransitions(string id)
        {
            var map = Load(id); var bound = PuzzleSearchLowerBound.TryCreate(map);
            Assert.That(bound, Is.Not.Null);
            var state = new PuzzleDeviceSession(map); var random = new System.Random(431);
            for (int sample = 0; sample < 160; sample++)
            {
                int before = bound.Estimate(state);
                foreach (var direction in Directions)
                {
                    var after = state.Copy();
                    if (!after.SearchStep(direction) || after.Failed) continue;
                    Assert.That(before, Is.LessThanOrEqualTo(1 + bound.Estimate(after)), id);
                    if (after.Won) Assert.That(bound.Estimate(after), Is.Zero);
                }
                state.SearchStep(Directions[random.Next(4)]);
                if (state.Won || state.Failed) state = new PuzzleDeviceSession(map);
            }
        }

        [TestCase("SW09")] [TestCase("SW10")] [TestCase("SW19")]
        public void BoundAlsoRespectsFreeWindAndQueuedInputs(string id)
        {
            var map = Load(id); var bound = PuzzleSearchLowerBound.TryCreate(map);
            Assert.That(bound, Is.Not.Null);
            var state = new PuzzleDeviceSession(map); state.AdvanceWind();
            var random = new System.Random(711);
            for (int sample = 0; sample < 100; sample++)
            {
                int before = bound.Estimate(state);
                foreach (var direction in Directions)
                {
                    var after = state.Copy();
                    if (after.SearchStep(direction) && !after.Failed)
                        Assert.That(before, Is.LessThanOrEqualTo(1 + bound.Estimate(after)), id);
                }
                var wind = state.Copy();
                if (wind.AdvanceWind().accepted && !wind.Failed)
                    Assert.That(before, Is.LessThanOrEqualTo(bound.Estimate(wind)), id);
                if (sample % 3 == 0) state.AdvanceWind(); else state.SearchStep(Directions[random.Next(4)]);
                if (state.Won || state.Failed) { state = new PuzzleDeviceSession(map); state.AdvanceWind(); }
            }
        }

        [Test] public void UnsupportedTeleportKeepsTheZeroHeuristicFallback()
        {
            var map = Object.Instantiate(Load("SW18")); map.scenario = Object.Instantiate(map.scenario);
            try
            {
                map.scenario.devices.Add(new PrototypeDevice { id = "portal", role = PrototypeRole.Portal });
                Assert.That(PuzzleSearchLowerBound.TryCreate(map), Is.Null);
            }
            finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
        }

        [UnityTest] public IEnumerator AStarAndExhaustiveBfsAgreeWithOptionalBubblesSheepAndStationaryPushes()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ANIMOL/Data/Puzzle/MapData/starway/__BoundTest.asset");
            try
            {
                map.theme = Load("SW00").theme; map.scenario = rules; rules.themeId = "starway";
                map.start = Vector2Int.zero; map.exit = new Vector2Int(4, 3); map.targetBubbles = 2;
                for (int x = 0; x < 5; x++) for (int y = 0; y < 4; y++)
                    map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass, x == 2 && y == 2));
                map.bubbles.AddRange(new[] { new Vector2Int(4, 0), new Vector2Int(0, 3), new Vector2Int(3, 3) });
                rules.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.MetalCrate, position = new Vector2Int(1, 0) });
                rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", instanceId = "sheep", position = new Vector2Int(2, 1) });
                rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "tall_grass", instanceId = "grass", position = new Vector2Int(3, 1) });
                AssetDatabase.CreateAsset(map, path); AssetDatabase.AddObjectToAsset(rules, map);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(rules);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(rules);
                PuzzleShortestPathSolver.Result bfs = null, astar = null;
                PuzzleShortestPathSolver.Start(map, r => bfs = r, 100000, false);
                double deadline = EditorApplication.timeSinceStartup + 20;
                while (bfs == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(bfs, Is.Not.Null); Assert.That(bfs.Solved, Is.True);
                PuzzleShortestPathSolver.Start(map, r => astar = r, 100000, true);
                deadline = EditorApplication.timeSinceStartup + 20;
                while (astar == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(astar, Is.Not.Null); Assert.That(astar.Solved, Is.True);
                Assert.That(astar.distance, Is.EqualTo(bfs.distance));
                Assert.That(astar.expanded, Is.LessThanOrEqualTo(bfs.expanded));
                var replay = new PuzzleDeviceSession(map);
                foreach (char action in astar.route) Assert.That(replay.Apply(Directions["NESW".IndexOf(action)]).accepted, Is.True);
                Assert.That(replay.Won, Is.True); Assert.That(replay.Cost, Is.EqualTo(bfs.distance));
            }
            finally
            {
                PuzzleShortestPathSolver.Cancel();
                if (AssetDatabase.LoadAssetAtPath<PuzzleMap>(path)) AssetDatabase.DeleteAsset(path);
                else { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
            }
        }

        [UnityTest] public IEnumerator MemoryStopRemainsIncompleteAndReleasesTheFrontier()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ANIMOL/Data/Puzzle/MapData/starway/__MemoryStopTest.asset");
            try
            {
                map.theme = Load("SW00").theme; map.scenario = rules; rules.themeId = "starway";
                map.start = Vector2Int.zero; map.exit = new Vector2Int(3, 0); map.targetBubbles = 0;
                for (int x = 0; x < 4; x++) map.terrain.Add(new TerrainCell(new Vector2Int(x, 0), TerrainMaterial.Grass));
                AssetDatabase.CreateAsset(map, path); AssetDatabase.AddObjectToAsset(rules, map);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(rules);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(rules);
                string hash = PuzzleShortestPathSolver.HashAsset(map);
                PuzzleShortestPathSolver.Result result = null; int completions = 0;
                PuzzleShortestPathSolver.Start(map, r => { result = r; completions++; }, 1000);
                var search = PuzzleShortestPathSolver.Active;
                // Simulate an exhausted memory budget without allocating gigabytes in a test.
                typeof(PuzzleShortestPathSolver).GetField("maxManagedBytes", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(search, 0L);
                double deadline = EditorApplication.timeSinceStartup + 10;
                while (result == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(result, Is.Not.Null); Assert.That(result.status, Is.EqualTo("limit"));
                Assert.That(result.detail, Does.Contain("memory budget")); Assert.That(result.route, Is.Empty);
                Assert.That(result.distance, Is.EqualTo(-1)); Assert.That(completions, Is.EqualTo(1));
                Assert.That(PuzzleShortestPathSolver.Active, Is.Null); Assert.That(search.FrontierCount, Is.Zero);
                Assert.That(PuzzleShortestPathSolver.HashAsset(map), Is.EqualTo(hash));
            }
            finally
            {
                PuzzleShortestPathSolver.Cancel();
                if (AssetDatabase.LoadAssetAtPath<PuzzleMap>(path)) AssetDatabase.DeleteAsset(path);
                else { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
            }
        }
    }
}
