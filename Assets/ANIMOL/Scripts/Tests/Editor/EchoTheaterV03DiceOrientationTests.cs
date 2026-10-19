using System.Collections.Generic;
using NUnit.Framework;

namespace ANIMOL.Art.EchoTheaterV03.Tests
{
    public sealed class EchoTheaterV03DiceOrientationTests
    {
        [Test] public void PdfInitialFirstRollsHaveExactTopFaces()
        {
            var initial = EchoTheaterV03DiceOrientation.Initial;
            Assert.AreEqual("U1 D6 N5 S2 E3 W4", initial.ToString());
            Assert.AreEqual(2, Roll(initial, EchoTheaterV03DiceOrientation.Heading.North).Up);
            Assert.AreEqual(4, Roll(initial, EchoTheaterV03DiceOrientation.Heading.East).Up);
            Assert.AreEqual(5, Roll(initial, EchoTheaterV03DiceOrientation.Heading.South).Up);
            Assert.AreEqual(3, Roll(initial, EchoTheaterV03DiceOrientation.Heading.West).Up);
        }

        [Test] public void Exactly24ProperStatesEachHasValidPermutationAndOpposites()
        {
            Assert.AreEqual(24, EchoTheaterV03DiceOrientation.All24.Count);
            var unique = new HashSet<EchoTheaterV03DiceOrientation.State>();
            foreach (var state in EchoTheaterV03DiceOrientation.All24)
            {
                Assert.IsTrue(unique.Add(state)); string error;
                Assert.IsTrue(EchoTheaterV03DiceOrientation.Validate(state, out error), error);
                Assert.AreEqual(7, state.Up + state.Down);
                Assert.AreEqual(7, state.North + state.South);
                Assert.AreEqual(7, state.East + state.West);
                var values = new HashSet<int> { state.Up, state.Down, state.North, state.South, state.East, state.West };
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, values);
            }
        }

        [Test] public void ReflectedArrangementCannotPassOppositesOnly()
        {
            EchoTheaterV03DiceOrientation.State state; string error;
            Assert.IsFalse(EchoTheaterV03DiceOrientation.TryCreate(1, 6, 5, 2, 4, 3, out state, out error));
            StringAssert.Contains("handedness", error);
            Assert.IsFalse(EchoTheaterV03DiceOrientation.TryCreate(1, 6, 5, 2, 3, 3, out state, out error));
            Assert.IsFalse(EchoTheaterV03DiceOrientation.TryCreate(1, 5, 6, 2, 3, 4, out state, out error));
            Assert.IsFalse(EchoTheaterV03DiceOrientation.Validate(default(EchoTheaterV03DiceOrientation.State), out error));
        }

        [Test] public void All24StatesObeyInverseAndFourRollIdentity()
        {
            foreach (var state in EchoTheaterV03DiceOrientation.All24)
            {
                Assert.AreEqual(state, Roll(Roll(state, EchoTheaterV03DiceOrientation.Heading.North), EchoTheaterV03DiceOrientation.Heading.South));
                Assert.AreEqual(state, Roll(Roll(state, EchoTheaterV03DiceOrientation.Heading.East), EchoTheaterV03DiceOrientation.Heading.West));
                for (int heading = 1; heading <= 4; ++heading)
                {
                    var next = state;
                    for (int i = 0; i < 4; ++i) next = Roll(next, (EchoTheaterV03DiceOrientation.Heading)heading);
                    Assert.AreEqual(state, next);
                }
            }
        }

        [Test] public void BlockedTeleportAndNoneDoNotChangeAnyFace()
        {
            foreach (var state in EchoTheaterV03DiceOrientation.All24)
            {
                EchoTheaterV03DiceOrientation.State next; string error;
                Assert.IsTrue(EchoTheaterV03DiceOrientation.TryPredictVisualRoll(state, EchoTheaterV03DiceOrientation.Heading.East, false, false, out next, out error));
                Assert.AreEqual(state, next);
                Assert.IsTrue(EchoTheaterV03DiceOrientation.TryPredictVisualRoll(state, EchoTheaterV03DiceOrientation.Heading.North, true, true, out next, out error));
                Assert.AreEqual(state, next);
                Assert.IsTrue(EchoTheaterV03DiceOrientation.TryPredictVisualRoll(state, EchoTheaterV03DiceOrientation.Heading.None, true, false, out next, out error));
                Assert.AreEqual(state, next);
            }
        }

        [Test] public void RollOrderIsNotTopFaceIncrementOrCommutative()
        {
            var initial = EchoTheaterV03DiceOrientation.Initial;
            var northThenEast = Roll(Roll(initial, EchoTheaterV03DiceOrientation.Heading.North), EchoTheaterV03DiceOrientation.Heading.East);
            var eastThenNorth = Roll(Roll(initial, EchoTheaterV03DiceOrientation.Heading.East), EchoTheaterV03DiceOrientation.Heading.North);
            Assert.AreNotEqual(northThenEast, eastThenNorth);
            Assert.AreEqual(4, northThenEast.Up);
            Assert.AreEqual(2, eastThenNorth.Up);
        }

        private static EchoTheaterV03DiceOrientation.State Roll(EchoTheaterV03DiceOrientation.State current, EchoTheaterV03DiceOrientation.Heading heading)
        {
            EchoTheaterV03DiceOrientation.State next; string error;
            Assert.IsTrue(EchoTheaterV03DiceOrientation.TryPredictVisualRoll(current, heading, true, false, out next, out error), error);
            return next;
        }
    }
}
