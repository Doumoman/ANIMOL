using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class PuzzleShortestPathTests
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static PuzzleMap Load(string id) => AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");

        [UnityTest] public IEnumerator BlockedDirectionSearchFindsTheFiveInputDetour() => SearchSmallBoard(false);
        [UnityTest] public IEnumerator InitialRuntimeWindPulseIsFreeAndOpensTheExit() => SearchSmallBoard(true);
        [UnityTest] public IEnumerator QueuedInputPrecedesTheNextFreeWindPulse() => SearchSmallBoard(true, true);
        [UnityTest] public IEnumerator OldUpdateCannotCompleteOrCancelTheNextSearch() => SearchSmallBoard(false, false, true);

        static IEnumerator SearchSmallBoard(bool wind, bool queued = false, bool checkOldUpdate = false)
        {
            Assert.That(PuzzleShortestPathSolver.Active, Is.Null);
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/ANIMOL/Data/Puzzle/MapData/starway/__ShortestPathTest.asset");
            try
            {
                map.theme = Load("SW00").theme;
                map.scenario = rules; rules.themeId = "starway";
                map.targetBubbles = queued ? 1 : 0;
                if (queued)
                {
                    var floor = new HashSet<Vector2Int>();
                    for (int x = 0; x <= 4; x++) floor.Add(new Vector2Int(x, 2));
                    for (int y = 0; y <= 4; y++) floor.Add(new Vector2Int(4, y));
                    for (int x = 2; x <= 6; x++) floor.Add(new Vector2Int(x, 4));
                    floor.Add(new Vector2Int(1, 3)); floor.Add(new Vector2Int(2, 3));
                    foreach (var cell in floor) map.terrain.Add(new TerrainCell(cell, TerrainMaterial.Grass));
                    map.bubbles.Add(new Vector2Int(2, 3));
                }
                else
                    for (int x = 0; x < 5; x++) for (int y = 0; y < 2; y++)
                        map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass,
                            !wind && x == 1 && y == 0));
                map.start = wind ? new Vector2Int(2, 0) : Vector2Int.zero;
                map.exit = wind ? new Vector2Int(3, 1) : new Vector2Int(3, 0);
                if (queued)
                {
                    map.start = new Vector2Int(1, 3); map.exit = new Vector2Int(2, 4);
                    rules.devices.Add(new PrototypeDevice { id = "a", role = PrototypeRole.Wind,
                        variant = "blower", position = new Vector2Int(0, 2), direction = Vector2Int.right, orientation = 1 });
                    rules.devices.Add(new PrototypeDevice { id = "b", role = PrototypeRole.Wind,
                        variant = "blower", position = new Vector2Int(4, 0), direction = Vector2Int.up, orientation = 1 });
                    rules.devices.Add(new PrototypeDevice { id = "c", role = PrototypeRole.Wind,
                        variant = "blower", position = new Vector2Int(6, 4), direction = Vector2Int.left, orientation = 1 });
                    rules.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                        position = new Vector2Int(1, 2) });
                }
                else if (wind)
                {
                    rules.devices.Add(new PrototypeDevice { id = "blower", role = PrototypeRole.Wind,
                        variant = "blower", position = new Vector2Int(0, 1), direction = Vector2Int.right, orientation = 1 });
                    rules.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                        position = new Vector2Int(2, 1) });
                }
                AssetDatabase.CreateAsset(map, path);
                AssetDatabase.AddObjectToAsset(rules, map);
                EditorUtility.SetDirty(map); EditorUtility.SetDirty(rules);
                AssetDatabase.SaveAssetIfDirty(map); AssetDatabase.SaveAssetIfDirty(rules);
                PuzzleShortestPathSolver.Result result = null;
                int completions = 0;
                PuzzleShortestPathSolver.Start(map, found => { result = found; completions++; }, 10000);
                var originalSearch = PuzzleShortestPathSolver.Active;
                double deadline = EditorApplication.timeSinceStartup + 10;
                while (result == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(result, Is.Not.Null);
                Assert.That(result.status, Is.EqualTo("solved"), result.detail);
                Assert.That(result.distance, Is.EqualTo(queued ? 2 : wind ? 1 : 5));
                if (wind) Assert.That(result.route, Is.EqualTo(queued ? "EN" : "N"));
                if (checkOldUpdate)
                {
                    PuzzleShortestPathSolver.Result nextResult = null;
                    PuzzleShortestPathSolver.Start(map, found => nextResult = found, 10000);
                    var nextSearch = PuzzleShortestPathSolver.Active;
                    typeof(PuzzleShortestPathSolver).GetMethod("Tick",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(originalSearch, null);
                    Assert.That(completions, Is.EqualTo(1));
                    Assert.That(PuzzleShortestPathSolver.Active, Is.SameAs(nextSearch));
                    deadline = EditorApplication.timeSinceStartup + 10;
                    while (nextResult == null && EditorApplication.timeSinceStartup < deadline) yield return null;
                    Assert.That(nextResult, Is.Not.Null);
                    Assert.That(nextResult.status, Is.EqualTo("solved"));
                }
            }
            finally
            {
                if (PuzzleShortestPathSolver.Active != null) PuzzleShortestPathSolver.Cancel();
                if (AssetDatabase.LoadAssetAtPath<PuzzleMap>(path)) AssetDatabase.DeleteAsset(path);
                else { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
            }
        }

        [TestCase("SW02")]
        [TestCase("SW09")]
        public void VisualHeadingsAndUnlabelledCratesKeepTheSameSuccessors(string id)
        {
            var map = Load(id);
            var session = new PuzzleDeviceSession(map);
            var random = new System.Random(731);
            for (int sample = 0; sample < 64; sample++)
            {
                var equivalent = session.Copy();
                foreach (var d in equivalent.Devices)
                {
                    var state = equivalent.States[d.id];
                    state.lookDirection = Vector2Int.left;
                    if (d.role == PrototypeRole.Chick || d.role == PrototypeRole.MetalCrate ||
                        d.role == PrototypeRole.WoodCrate || d.role == PrototypeRole.StarSocket ||
                        d.role == PrototypeRole.Barrel || d.role == PrototypeRole.Bomb || d.role == PrototypeRole.Dice)
                        state.direction = Directions[(sample + 1) % 4];
                }
                var crates = equivalent.Devices.Where(d => d.role == PrototypeRole.MetalCrate).ToArray();
                if (crates.Length == 2)
                {
                    var a = equivalent.States[crates[0].id];
                    var b = equivalent.States[crates[1].id];
                    var position = a.cell; a.cell = b.cell; b.cell = position;
                }
                Assert.That(equivalent.SearchStateKey(), Is.EqualTo(session.SearchStateKey()), id);
                foreach (var direction in Directions)
                {
                    var a = session.Copy(); var b = equivalent.Copy();
                    Assert.That(b.Apply(direction).accepted, Is.EqualTo(a.Apply(direction).accepted));
                    AssertEquivalent(a, b);
                    var seen = new HashSet<string>();
                    for (int pulse = 0; pulse < 64 && seen.Add(a.SearchStateKey()); pulse++)
                    {
                        bool moved = a.AdvanceWind().accepted;
                        Assert.That(b.AdvanceWind().accepted, Is.EqualTo(moved));
                        AssertEquivalent(a, b);
                        if (!moved) break;
                    }
                }
                session.Apply(Directions[random.Next(4)]);
                if (!Settle(session) || session.Won || session.Failed) session = new PuzzleDeviceSession(map);
            }
        }

        [Test] public void AGoalNamingOneCrateKeepsItsIdentityInTheSearchKey()
        {
            var map = Object.Instantiate(Load("SW02"));
            map.scenario = Object.Instantiate(map.scenario);
            try
            {
                var crates = map.scenario.devices.Where(d => d.role == PrototypeRole.MetalCrate).ToArray();
                map.scenario.goals.Add(new PrototypeGoal { kind = PrototypeGoalKind.Position,
                    deviceId = crates[0].id, cell = crates[0].position });
                var a = new PuzzleDeviceSession(map); var b = a.Copy();
                b.States[crates[0].id].cell = crates[1].position;
                b.States[crates[1].id].cell = crates[0].position;
                Assert.That(b.SearchStateKey(), Is.Not.EqualTo(a.SearchStateKey()));
            }
            finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
        }

        [TestCase("SW02")]
        [TestCase("SW16")]
        [TestCase("SW17")]
        [TestCase("SW18")]
        public void NoWindSheepPermutationKeepsTheSameSuccessors(string id)
        {
            var map = Load(id);
            if (map.scenario.devices.Any(d => d.role == PrototypeRole.Wind))
                Assert.Ignore("This authored map now has wind, so sheep identity is retained.");
            var session = new PuzzleDeviceSession(map);
            var random = new System.Random(911);
            for (int sample = 0; sample < 64; sample++)
            {
                var equivalent = session.Copy();
                var sheep = (Dictionary<string, Vector2Int>)equivalent.RanchSheep;
                var ids = sheep.Keys.OrderBy(value => value).ToArray();
                var positions = ids.Select(key => sheep[key]).Reverse().ToArray();
                for (int i = 0; i < ids.Length; i++) sheep[ids[i]] = positions[i];
                AssertEquivalent(session, equivalent);
                foreach (var direction in Directions)
                {
                    var a = session.Copy(); var b = equivalent.Copy();
                    Assert.That(b.Apply(direction).accepted, Is.EqualTo(a.Apply(direction).accepted));
                    AssertEquivalent(a, b);
                }
                session.Apply(Directions[random.Next(4)]);
                if (session.Won || session.Failed) session = new PuzzleDeviceSession(map);
            }
        }

        [Test] public void SheepPermutationStillMatchesAfterGrassConsumptionAndRemoval()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                map.theme = Load("SW00").theme; map.scenario = rules; rules.themeId = "starway";
                map.targetBubbles = 0; map.start = new Vector2Int(0, 1); map.exit = new Vector2Int(4, 2);
                for (int x = 0; x < 5; x++) for (int y = 0; y < 3; y++)
                    map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass));
                for (int i = 0; i < 2; i++)
                {
                    rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", instanceId = "s" + i,
                        position = new Vector2Int(1 + i * 2, 1) });
                    rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "tall_grass", instanceId = "g" + i,
                        position = new Vector2Int(2 + i * 2, 1) });
                }
                var a = new PuzzleDeviceSession(map); var b = a.Copy();
                var sheep = (Dictionary<string, Vector2Int>)b.RanchSheep;
                var position = sheep["s0"]; sheep["s0"] = sheep["s1"]; sheep["s1"] = position;
                for (int i = 0; i < 4; i++)
                {
                    Assert.That(a.Apply(Vector2Int.right).accepted, Is.True);
                    Assert.That(b.Apply(Vector2Int.right).accepted, Is.True);
                    AssertEquivalent(a, b);
                    if (i == 0) Assert.That(a.RanchSheep.Count, Is.EqualTo(1));
                }
                Assert.That(a.RanchSheep.Count, Is.Zero);
            }
            finally { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
        }

        [Test] public void WindKeepsSheepIterationIdentityInTheSearchKey()
        {
            var a = new PuzzleDeviceSession(Load("SW09")); var b = a.Copy();
            var sheep = (Dictionary<string, Vector2Int>)b.RanchSheep;
            var ids = sheep.Keys.Take(2).ToArray();
            var position = sheep[ids[0]]; sheep[ids[0]] = sheep[ids[1]]; sheep[ids[1]] = position;
            Assert.That(b.SearchStateKey(), Is.Not.EqualTo(a.SearchStateKey()));
        }

        [Test] public void RecordedSolvedRoutesReplayAgainstTheirMatchingSourceAssets()
        {
            foreach (var record in PuzzleShortestPathRecord.Read().maps.Where(r => r.status == "solved"))
            {
                var map = Load(record.mapId);
                // Authored map edits invalidate the old evidence; the editor requests recomputation.
                if (!PuzzleShortestPathRecord.IsCurrent(map, record)) continue;
                var session = new PuzzleDeviceSession(map);
                if (map.scenario.devices.Any(d => d.role == PrototypeRole.Wind)) session.AdvanceWind();
                foreach (char action in record.route)
                {
                    if (char.IsWhiteSpace(action)) continue;
                    if (PuzzleRouteNotation.IsWait(action))
                    {
                        Assert.That(session.AdvanceWind().accepted, Is.True, record.mapId);
                        Assert.That(session.Failed, Is.False, record.mapId);
                        continue;
                    }
                    Assert.That(PuzzleRouteNotation.TryDirection(action, out var direction), Is.True);
                    Assert.That(session.Apply(direction).accepted, Is.True, record.mapId);
                    Assert.That(session.Failed, Is.False, record.mapId);
                }
                Assert.That(session.Won, Is.True, record.mapId);
                Assert.That(session.Cost, Is.EqualTo(record.distance), record.mapId);
            }
        }

        static void AssertEquivalent(PuzzleDeviceSession a, PuzzleDeviceSession b)
        {
            Assert.That(b.SearchStateKey(), Is.EqualTo(a.SearchStateKey()));
            Assert.That(b.Won, Is.EqualTo(a.Won));
            Assert.That(b.Failed, Is.EqualTo(a.Failed));
            Assert.That(b.Cost, Is.EqualTo(a.Cost));
        }

        static bool Settle(PuzzleDeviceSession session)
        {
            var seen = new HashSet<string>();
            while (!session.Won && !session.Failed)
            {
                if (!seen.Add(session.SearchStateKey())) return false;
                if (!session.AdvanceWind().accepted) break;
            }
            return !session.Failed;
        }
    }
}
