using NUnit.Framework;
using UnityEngine;
using ANIMOL.Puzzle;
using Die = ANIMOL.Art.EchoTheaterV03.EchoTheaterV03DiceOrientation;

namespace ANIMOL.Presentation.DiceHintV01.Tests
{
    public sealed class DiceRollHintPolicyV01Tests
    {
        static DiceHintSnapshot Ready(Die.State faces, Vector2Int player)
        {
            return new DiceHintSnapshot { DiceExists = true, InputReady = true, DiceSettled = true,
                Faces = faces, DiceCell = Vector2Int.zero, PlayerCell = player,
                LegalRolls = DiceRollMask.North | DiceRollMask.East | DiceRollMask.South | DiceRollMask.West };
        }
        [Test]
        public void All24Times4_UsesCurrentSixFacesAndOnlyThePlayersPushDirection()
        {
            foreach (var faces in Die.All24)
            {
                var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                var expected = new[] { faces.South, faces.West, faces.North, faces.East };
                for (int i = 0; i < 4; i++)
                {
                    DiceRollHint hint;
                    Assert.That(DiceRollHintPolicyV01.TryEvaluate(Ready(faces, -directions[i]), out hint), Is.True);
                    Assert.That(hint.NextTop, Is.EqualTo(expected[i]));
                    Assert.That(hint.Direction, Is.EqualTo((Die.Heading)(i + 1)));
                    Assert.That(hint.Destination, Is.EqualTo(directions[i]));
                }
            }
        }
        [Test]
        public void MirroredMapFacesPredictTheSameNinetySixCommittedTops()
        {
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            foreach (var art in Die.All24)
            {
                var gameFaces = new[] { art.Up, art.Down, art.South, art.North, art.East, art.West };
                foreach (var direction in directions)
                {
                    var committed = (int[])gameFaces.Clone();
                    PuzzleDeviceSession.Roll(committed, direction);
                    var snapshot = Ready(art, -direction);
                    snapshot.MirroredNorthSouth = true;
                    Assert.That(DiceRollHintPolicyV01.TryEvaluate(snapshot, out var hint), Is.True);
                    Assert.That(hint.NextTop, Is.EqualTo(committed[0]));
                }
            }
        }
        [TestCase(0, -1, 2)] [TestCase(-1, 0, 4)] [TestCase(0, 1, 5)] [TestCase(1, 0, 3)]
        public void InitialExamples(int px, int py, int expected)
        {
            DiceRollHint hint;
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(Ready(Die.Initial, new Vector2Int(px, py)), out hint), Is.True);
            Assert.That(hint.NextTop, Is.EqualTo(expected));
        }
        [Test]
        public void AfterEastRoll_NextEastPreviewIsSix()
        {
            Die.State after; string error; DiceRollHint hint;
            Assert.That(Die.TryPredictVisualRoll(Die.Initial, Die.Heading.East, true, false, out after, out error), Is.True);
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(Ready(after, Vector2Int.left), out hint), Is.True);
            Assert.That(hint.NextTop, Is.EqualTo(6));
        }
        [Test]
        public void HostBlocksCurrentPush_EvenIfOtherDirectionsAreOpen_HidesHint()
        {
            var snapshot = Ready(Die.Initial, Vector2Int.left); snapshot.LegalRolls &= ~DiceRollMask.East;
            DiceRollHint hint; Assert.That(DiceRollHintPolicyV01.TryEvaluate(snapshot, out hint), Is.False);
        }
        [TestCase(0, 0)] [TestCase(1, 1)] [TestCase(-2, 0)] [TestCase(0, 3)]
        public void NoUsableAdjacentPush_HidesHint(int x, int y)
        {
            DiceRollHint hint;
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(Ready(Die.Initial, new Vector2Int(x, y)), out hint), Is.False);
        }
        [Test]
        public void MissingLockedRollingAndInvalidOrientation_HideHint()
        {
            DiceRollHint hint;
            var s = Ready(Die.Initial, Vector2Int.left); s.DiceExists = false;
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(s, out hint), Is.False);
            s = Ready(Die.Initial, Vector2Int.left); s.InputReady = false;
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(s, out hint), Is.False);
            s = Ready(Die.Initial, Vector2Int.left); s.DiceSettled = false;
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(s, out hint), Is.False);
            s = Ready(default(Die.State), Vector2Int.left);
            Assert.That(DiceRollHintPolicyV01.TryEvaluate(s, out hint), Is.False);
        }
    }
}
