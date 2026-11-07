using System;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class PuzzleSearchSnapshotTests
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static PuzzleMap Load(int number) => AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW" + number.ToString("00") + "_MapData.asset");

        static void Same(PuzzleDeviceSession expected, PuzzleDeviceSession actual)
        {
            Assert.That(actual.SearchStateKey(), Is.EqualTo(expected.SearchStateKey()));
            Assert.That(actual.Cell, Is.EqualTo(expected.Cell));
            Assert.That(actual.Direction, Is.EqualTo(expected.Direction));
            Assert.That(actual.Cost, Is.EqualTo(expected.Cost));
            Assert.That(actual.Won, Is.EqualTo(expected.Won));
            Assert.That(actual.Failed, Is.EqualTo(expected.Failed));
            Assert.That(actual.Failure, Is.EqualTo(expected.Failure));
            Assert.That(actual.BoatId, Is.EqualTo(expected.BoatId));
            Assert.That(actual.PortalCuts, Is.EqualTo(expected.PortalCuts));
            CollectionAssert.AreEqual(expected.RanchSheep.ToArray(), actual.RanchSheep.ToArray());
            CollectionAssert.AreEquivalent(expected.RanchPonds.ToArray(), actual.RanchPonds.ToArray());
            CollectionAssert.AreEquivalent(expected.WindCells.ToArray(), actual.WindCells.ToArray());
            foreach (var device in expected.Devices)
                Assert.That(JsonUtility.ToJson(actual.States[device.id]), Is.EqualTo(JsonUtility.ToJson(expected.States[device.id])));
        }

        [Test] public void CheckpointsPreservePublicSuccessorsOnAllTwentyMaps()
        {
            var random = new System.Random(8117);
            for (int mapNumber = 0; mapNumber < 20; mapNumber++)
            {
                var root = new PuzzleDeviceSession(Load(mapNumber)); root.AdvanceWind();
                var current = root.Copy();
                for (int sample = 0; sample < 40; sample++)
                {
                    var checkpoint = current.CaptureSearchSnapshot();
                    var restored = root.RestoreSearchSnapshot(checkpoint);
                    Same(current, restored);
                    foreach (var direction in Directions)
                    {
                        var before = current.Copy(); var after = restored.Copy();
                        Assert.That(after.Apply(direction).accepted, Is.EqualTo(before.Apply(direction).accepted));
                        Same(before, after);
                    }
                    var beforeWind = current.Copy(); var afterWind = restored.Copy();
                    Assert.That(afterWind.AdvanceWind().accepted, Is.EqualTo(beforeWind.AdvanceWind().accepted));
                    Same(beforeWind, afterWind);
                    if (sample % 4 == 0) current.AdvanceWind(); else current.Apply(Directions[random.Next(4)]);
                    if (current.Won || current.Failed) current = root.Copy();
                }
            }
        }

        [Test] public void RestoredBranchesAreIndependentAndOtherMapsAreRejected()
        {
            var root = new PuzzleDeviceSession(Load(19)); root.AdvanceWind();
            var checkpoint = root.CaptureSearchSnapshot();
            var first = root.RestoreSearchSnapshot(checkpoint);
            var second = root.RestoreSearchSnapshot(checkpoint);
            foreach (var direction in Directions) first.SearchStep(direction);
            Same(root, second); Same(root, root.RestoreSearchSnapshot(checkpoint));
            Assert.That(checkpoint.ByteCount, Is.LessThan(1024));
            var other = new PuzzleDeviceSession(Load(2));
            Assert.Throws<ArgumentException>(() => other.RestoreSearchSnapshot(checkpoint));
        }

        [Test] public void CheckpointKeepsSignedIntegerAndFullDevicePayload()
        {
            var root = new PuzzleDeviceSession(Load(2));
            var state = root.States.Values.First();
            state.phase = int.MinValue; state.timer = int.MaxValue;
            state.lookDirection = new Vector2Int(-50000, 50000);
            state.dice = new[] { -1, 0, 128, 32768, int.MinValue, int.MaxValue };
            state.trail.Add(new Vector2Int(-1234, 8765)); state.locked = true;
            Same(root, root.RestoreSearchSnapshot(root.CaptureSearchSnapshot()));
        }
    }
}
