using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ANIMOL.Puzzle.Tests
{
    public sealed class EchoTheaterV3Tests
    {
        static Vector2Int Heading(char c) => c == 'U' ? Vector2Int.up : c == 'D' ? Vector2Int.down :
            c == 'L' ? Vector2Int.left : Vector2Int.right;
        static PuzzleMap Map(string id) => AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            EchoTheaterV3Importer.Root + "/" + id + "_MapData.asset");

        [Test] public void AllThirtyFiveSolutionsMatchNativeSessionTrace() =>
            Assert.That(EchoTheaterV3Validation.RunAll(), Is.EqualTo("35/35"));

        [Test] public void FinalFloorOnlyAndInteriorWallsAreEmptyHoles()
        {
            for (var index = 1; index <= 35; index++)
            {
                var id = "ET-" + index.ToString("00");
                var source = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    EchoTheaterV3Importer.SourceRoot + "/maps/" + id + ".json");
                var map = Map(id);
                var floorStart = source.text.IndexOf("\"floor\":", System.StringComparison.Ordinal);
                var wallsStart = source.text.IndexOf("\"interior_walls\":", System.StringComparison.Ordinal);
                var startStart = source.text.IndexOf("\"start\":", wallsStart, System.StringComparison.Ordinal);
                Assert.That(floorStart, Is.GreaterThanOrEqualTo(0), id);
                Assert.That(wallsStart, Is.GreaterThan(floorStart), id);
                Assert.That(startStart, Is.GreaterThan(wallsStart), id);
                var cellPattern = @"\[\s*(\d+)\s*,\s*(\d+)\s*\]";
                var floor = new HashSet<Vector2Int>(Regex.Matches(
                    source.text.Substring(floorStart, wallsStart - floorStart), cellPattern)
                    .Cast<Match>().Select(m => new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))));
                Assert.That(new HashSet<Vector2Int>(map.terrain.Select(c => c.position)), Is.EquivalentTo(floor), id);
                foreach (Match wall in Regex.Matches(source.text.Substring(wallsStart, startStart - wallsStart), cellPattern))
                {
                    var cell = new Vector2Int(int.Parse(wall.Groups[1].Value), int.Parse(wall.Groups[2].Value));
                    Assert.That(map.TryTerrain(cell, out _), Is.False, id + " wall " + cell);
                    Assert.That(map.Walkable(cell), Is.False, id + " wall " + cell);
                }
            }
        }

        [Test] public void DiceFacesRollAsSixSidesWithTwentyFourOrientations()
        {
            var original = new[] { 1, 6, 2, 5, 3, 4 };
            Assert.That(new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down }
                .Select(d => { var a = (int[])original.Clone(); PuzzleDeviceSession.Roll(a, d); return a[0]; }),
                Is.EqualTo(new[] { 4, 3, 5, 2 }));
            var rightDownRight = (int[])original.Clone();
            foreach (var d in new[] { Vector2Int.right, Vector2Int.down, Vector2Int.right }) PuzzleDeviceSession.Roll(rightDownRight, d);
            var downRightRight = (int[])original.Clone();
            foreach (var d in new[] { Vector2Int.down, Vector2Int.right, Vector2Int.right }) PuzzleDeviceSession.Roll(downRightRight, d);
            Assert.That(rightDownRight[0], Is.EqualTo(6));
            Assert.That(downRightRight[0], Is.EqualTo(5));
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var a = (int[])original.Clone();
                for (var i = 0; i < 4; i++) PuzzleDeviceSession.Roll(a, d);
                Assert.That(a, Is.EqualTo(original));
                PuzzleDeviceSession.Roll(a, d); PuzzleDeviceSession.Roll(a, -d);
                Assert.That(a, Is.EqualTo(original));
            }
            var seen = new HashSet<string>(); var queue = new Queue<int[]>(); queue.Enqueue(original);
            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                if (!seen.Add(string.Join(",", state))) continue;
                Assert.That(state[0] + state[1], Is.EqualTo(7));
                Assert.That(state[2] + state[3], Is.EqualTo(7));
                Assert.That(state[4] + state[5], Is.EqualTo(7));
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                { var next = (int[])state.Clone(); PuzzleDeviceSession.Roll(next, d); queue.Enqueue(next); }
            }
            Assert.That(seen.Count, Is.EqualTo(24));
        }

        [Test] public void WrongFaceAndEntryRejectWholeInputAndSocketUndoReset()
        {
            var source = Map("ET-05");
            Assert.That(source, Is.Not.Null);
            var map = Object.Instantiate(source); var rules = Object.Instantiate(source.scenario);
            try
            {
                map.scenario = rules;
                var actions = rules.verificationRoute;
                var insertion = 20;
                var socket = rules.devices.Single(d => d.variant == "socket");
                void Replay(PuzzleDeviceSession session)
                { for (var i = 0; i < insertion - 1; i++) Assert.That(session.Apply(Heading(actions[i])).accepted, Is.True, "step " + i); }
                var wrongFace = new PuzzleDeviceSession(map); Replay(wrongFace);
                var before = wrongFace.Copy(); socket.number = 5;
                Assert.That(wrongFace.Apply(Heading(actions[insertion - 1])).accepted, Is.False);
                Assert.That(wrongFace.Cost, Is.EqualTo(before.Cost));
                Assert.That(wrongFace.Cell, Is.EqualTo(before.Cell));
                Assert.That(wrongFace.States["Q1"].dice, Is.EqualTo(before.States["Q1"].dice));
                Assert.That(wrongFace.States[socket.id].active, Is.False);
                socket.number = 6; socket.direction = Vector2Int.up;
                Assert.That(wrongFace.Apply(Heading(actions[insertion - 1])).accepted, Is.False);
                Assert.That(wrongFace.Cost, Is.EqualTo(before.Cost));
                socket.direction = Vector2Int.right;
                Assert.That(wrongFace.Apply(Heading(actions[insertion - 1])).accepted, Is.True);
                Assert.That(wrongFace.States["Q1"].removed && wrongFace.States[socket.id].active, Is.True);
                Assert.That(wrongFace.Undo(), Is.True);
                Assert.That(wrongFace.States["Q1"].removed || wrongFace.States[socket.id].active, Is.False);
                Assert.That(wrongFace.States["Q1"].dice, Is.EqualTo(before.States["Q1"].dice));
                wrongFace.Reset();
                Assert.That(wrongFace.States["Q1"].dice, Is.EqualTo(new[] { 1, 6, 2, 5, 3, 4 }));
                Assert.That(wrongFace.States[socket.id].active, Is.False);
            }
            finally { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
        }

        [Test] public void DominoChainOpensItsCrossingAndUndoResetRestoresIt()
        {
            var map = Map("ET-03"); var session = new PuzzleDeviceSession(map);
            var sideApproach = Object.Instantiate(map);
            try
            {
                sideApproach.start = new Vector2Int(13, 14);
                var wrong = new PuzzleDeviceSession(sideApproach);
                Assert.That(wrong.Apply(Vector2Int.right).accepted, Is.False);
                Assert.That(wrong.Cost, Is.Zero);
                Assert.That(wrong.States["D2"].active, Is.False);
            }
            finally { Object.DestroyImmediate(sideApproach); }
            var actions = map.scenario.verificationRoute;
            for (var i = 0; i < 26; i++) Assert.That(session.Apply(Heading(actions[i])).accepted, Is.True);
            var before = session.Copy();
            Assert.That(session.Apply(Heading(actions[26])).accepted, Is.True);
            var dominos = map.scenario.devices.Where(d => d.role == PrototypeRole.Domino).ToArray();
            Assert.That(dominos.Length, Is.EqualTo(3));
            Assert.That(dominos.All(d => session.States[d.id].active), Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(dominos.All(d => !session.States[d.id].active), Is.True);
            Assert.That(session.Cell, Is.EqualTo(before.Cell));
            session.Reset();
            Assert.That(session.Cost, Is.Zero);
            Assert.That(dominos.All(d => !session.States[d.id].active), Is.True);
        }

        [Test] public void ThirtyFourthDieCanOpenSocketWithoutMatchingExpectedId()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                map.Resize(110, 5); map.theme = Map("ET-05").theme; map.scenario = rules;
                map.targetBubbles = 0; map.start = new Vector2Int(99, 2); map.exit = new Vector2Int(109, 2);
                for (var x = 1; x < 110; x++) map.SetTerrain(new Vector2Int(x, 2), TerrainMaterial.Grass);
                rules.themeId = "memory_theater"; rules.echoTheaterV3Rules = true;
                for (var i = 0; i < 34; i++)
                {
                    var x = 2 + i * 3;
                    rules.devices.Add(new PrototypeDevice { id = "Q" + i, role = PrototypeRole.Dice, position = new Vector2Int(x, 2), initialDiceFaces = new[] { 1, 6, 2, 5, 3, 4 } });
                    rules.devices.Add(new PrototypeDevice { id = i == 33 ? "G_expected_Q0" : "G" + i,
                        role = PrototypeRole.Dice, variant = "socket", position = new Vector2Int(x + 1, 2), number = 4, direction = Vector2Int.right });
                }
                map.start = new Vector2Int(100, 2); // Adjacent to Q33 at x=101.
                var session = new PuzzleDeviceSession(map);
                Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
                Assert.That(session.States["Q33"].removed && session.States["G_expected_Q0"].active, Is.True);
                Assert.That(rules.devices.Count(d => d.variant == "socket" && session.States[d.id].active), Is.EqualTo(1));
                Assert.That(session.Undo(), Is.True);
                Assert.That(session.States["G_expected_Q0"].active, Is.False);
            }
            finally { Object.DestroyImmediate(rules); Object.DestroyImmediate(map); }
        }
    }
}
