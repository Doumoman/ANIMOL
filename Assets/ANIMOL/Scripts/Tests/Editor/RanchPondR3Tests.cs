using NUnit.Framework;
using ANIMOL.Gameplay.RanchDevicesR2;

namespace ANIMOL.Tests.RanchDevicesR2
{
    public sealed class RanchPondR3Tests
    {
        [TestCase(0, 0, 1, 0, true, "east")]
        [TestCase(0, 0, 1, 0, false, "west")]
        [TestCase(1, 0, 0, 0, true, "west")]
        [TestCase(1, 0, 0, 0, false, "east")]
        [TestCase(0, 0, 0, 1, true, "north")]
        [TestCase(0, 0, 0, 1, false, "south")]
        [TestCase(0, 1, 0, 0, true, "south")]
        [TestCase(0, 1, 0, 0, false, "north")]
        public void CueAndCommittedWaterRouteAgreeForBothAxesAndEitherLabelOrder(int ax, int ay, int bx, int by, bool wetA, string flow)
        {
            var p = new RanchPondPairR2 { instanceId = "p", a = new RanchCellR2(ax, ay), b = new RanchCellR2(bx, by), wetA = wetA };
            RanchPondCueR3 cue; string error;
            Assert.IsTrue(RanchPondPresentationR3.TryCue(p, out cue, out error), error);
            string axis = ax == bx ? "vertical" : "horizontal";
            Assert.AreEqual(axis, cue.axis); Assert.AreEqual(flow, cue.waterDirection);
            Assert.AreEqual("pond_pair/" + axis + "/water_" + flow, cue.transferKey);
            Assert.AreEqual(p.Wet, cue.waterFrom); Assert.AreEqual(p.Dry, cue.waterTo);
            var b = new RanchRuleBoardR2 { player = p.Dry };
            b.ground.Add(p.a, RanchGroundR2.Floor); b.ground.Add(p.b, RanchGroundR2.Floor); b.ponds.Add(p);
            var d = new RanchCellR2(p.Wet.x - p.Dry.x, p.Wet.y - p.Dry.y);
            RanchRuleBoardR2 next; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryCrossPond(b, d, out next, out r));
            CollectionAssert.AreEqual(new[] { p.Wet, p.Dry }, r.waterRoute);
            CollectionAssert.AreEqual(new[] { p.Dry, p.Wet }, r.actualRoute);
            Assert.AreEqual(p.Wet, next.player); Assert.AreEqual(1, r.costDelta); Assert.AreEqual(1, r.turnDelta);
            RanchPondCueR3 settled;
            Assert.IsTrue(RanchPondPresentationR3.TryCue(next.ponds[0], out settled, out error));
            Assert.AreEqual(cue.endKey, settled.startKey);
            Assert.AreEqual(p.Wet, b.ponds[0].Wet); Assert.AreEqual(0, b.costSpent);
        }
        [Test] public void BlockedVerticalCrossingDoesNotEmitAFlowRoute()
        {
            var p = new RanchPondPairR2 { instanceId = "p", a = new RanchCellR2(0, 0), b = new RanchCellR2(0, 1), wetA = false };
            var b = new RanchRuleBoardR2 { player = p.Dry }; b.ponds.Add(p);
            b.ground.Add(p.a, RanchGroundR2.Floor); b.ground.Add(p.b, RanchGroundR2.Floor); b.solidOccupants.Add(p.Wet);
            RanchRuleBoardR2 next; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryCrossPond(b, new RanchCellR2(0, 1), out next, out r));
            Assert.AreSame(b, next); Assert.IsEmpty(r.waterRoute); Assert.AreEqual(0, r.costDelta);
        }
        [Test] public void DiagonalOrSeparatedPairsHaveNoPresentationCue()
        {
            RanchPondCueR3 cue; string error;
            Assert.IsFalse(RanchPondPresentationR3.TryCue(new RanchPondPairR2 { a = new RanchCellR2(0, 0), b = new RanchCellR2(1, 1) }, out cue, out error));
            Assert.IsNull(cue);
            Assert.IsFalse(RanchPondPresentationR3.TryCue(new RanchPondPairR2 { a = new RanchCellR2(0, 0), b = new RanchCellR2(0, 2) }, out cue, out error));
        }
    }
}
