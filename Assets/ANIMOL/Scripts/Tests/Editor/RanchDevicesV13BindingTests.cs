using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Tests.Editor
{
    public sealed class RanchDevicesV13BindingTests
    {
        [TestCase(1, "star/triangle/stone")]
        [TestCase(2, "star/diamond/stone")]
        [TestCase(3, "star/cross/stone")]
        public void ExistingStarNumberSelectsPresentationPattern(int number, string expected)
        {
            var definition = new PrototypeDevice { role = PrototypeRole.StarSocket, number = number };
            var state = new PrototypeDeviceState { direction = Vector2Int.right };
            Assert.IsTrue(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out var key, out var error), error);
            Assert.AreEqual(expected, key);
            definition.variant = "socket";
            Assert.IsTrue(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out key, out error), error);
            Assert.AreEqual(expected.Replace("/stone", "/empty"), key);
            state.active = true;
            Assert.IsTrue(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out key, out error), error);
            Assert.AreEqual(expected.Replace("/stone", "/filled"), key);
        }

        [Test]
        public void UnknownStarNumberFailsInsteadOfChoosingAnotherPattern()
        {
            var definition = new PrototypeDevice { role = PrototypeRole.StarSocket, number = 4 };
            var state = new PrototypeDeviceState { direction = Vector2Int.right };
            Assert.IsFalse(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out _, out var error));
            StringAssert.Contains("1, 2, or 3", error);
        }

        [TestCase(0, "windmill/north/off")]
        [TestCase(1, "windmill/north/3")]
        [TestCase(2, "windmill/north/6")]
        [TestCase(3, "windmill/north/9")]
        public void WindStateUsesCommittedPhaseAndHeading(int phase, string expected)
        {
            var definition = new PrototypeDevice { role = PrototypeRole.Wind, variant = "blower" };
            var state = new PrototypeDeviceState { direction = Vector2Int.up, phase = phase };
            Assert.IsTrue(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out var key, out var error), error);
            Assert.AreEqual(expected, key);
        }

        [Test]
        public void NestArrivalChangesOnlyChickPresentation()
        {
            var definition = new PrototypeDevice { role = PrototypeRole.Chick };
            var state = new PrototypeDeviceState { direction = Vector2Int.left, active = true };
            Assert.IsTrue(RanchDevicesV13PuzzleBinding.TrySettledKey(definition, state, out var key, out var error), error);
            Assert.AreEqual("chick/west/sit", key);
        }
    }
}
