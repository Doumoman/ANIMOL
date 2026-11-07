using System.Collections;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class PuzzleChickSearchLowerBoundTests
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static PuzzleMap Load(string id) => AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");

        [TestCase("SW02")] [TestCase("SW17")]
        public void PatternBoundIsConsistentAcrossBlockedFollowsAndStationaryPushes(string id)
        {
            var map = Load(id); var bound = PuzzleChickSearchLowerBound.TryCreate(map);
            Assert.That(bound, Is.Not.Null);
            var state = new PuzzleDeviceSession(map); var random = new System.Random(9183);
            for (int sample = 0; sample < 300; sample++)
            {
                int before = bound.Estimate(state);
                foreach (var direction in Directions)
                {
                    var child = state.Copy();
                    if (!child.SearchStep(direction) || child.Failed) continue;
                    Assert.That(before, Is.LessThanOrEqualTo(1 + bound.Estimate(child)));
                    if (child.Won) Assert.That(bound.Estimate(child), Is.Zero);
                }
                state.SearchStep(Directions[random.Next(4)]);
                if (state.Won || state.Failed) state = new PuzzleDeviceSession(map);
            }
        }

        [Test] public void WindAndMultipleFollowersKeepTheirExistingBound()
        {
            Assert.That(PuzzleChickSearchLowerBound.TryCreate(Load("SW19")), Is.Null);
            Assert.That(PuzzleChickSearchLowerBound.TryCreate(Load("SW18")), Is.Null);
        }

        [UnityTest] public IEnumerator NestBubblePatternAndExhaustiveBfsAgreeWithOtherOccupants()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ANIMOL/Data/Puzzle/MapData/starway/__ChickBoundTest.asset");
            try
            {
                map.theme = Load("SW00").theme; map.scenario = rules; rules.themeId = "starway";
                map.start = Vector2Int.zero; map.exit = new Vector2Int(3, 0); map.targetBubbles = 1;
                for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++)
                    map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass));
                map.bubbles.Add(new Vector2Int(3, 3)); rules.nests.Add(new Vector2Int(3, 3));
                rules.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick, position = new Vector2Int(1, 1), number = 1 });
                rules.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.MetalCrate, position = new Vector2Int(2, 0) });
                rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", instanceId = "sheep", position = new Vector2Int(0, 2) });
                AssetDatabase.CreateAsset(map, path); AssetDatabase.AddObjectToAsset(rules, map);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(rules);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(rules);
                PuzzleShortestPathSolver.Result bfs = null, astar = null;
                PuzzleShortestPathSolver.Start(map, r => bfs = r, 200000, false);
                double deadline = EditorApplication.timeSinceStartup + 30;
                while (bfs == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(bfs, Is.Not.Null); Assert.That(bfs.Solved, Is.True);
                PuzzleShortestPathSolver.Start(map, r => astar = r, 200000, true);
                deadline = EditorApplication.timeSinceStartup + 30;
                while (astar == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(astar, Is.Not.Null); Assert.That(astar.Solved, Is.True);
                Assert.That(astar.distance, Is.EqualTo(bfs.distance));
                Assert.That(astar.expanded, Is.LessThanOrEqualTo(bfs.expanded));
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
