#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using ANIMOL.Art.DevicePatch01;

public sealed class OneCellGateRulesTests
{
    [Test] public void AllFourHeadingsAllowExactlyOneOfFourApproaches()
    {
        var gate = new GateCell(8, -3);
        foreach (GateHeading allowed in new[] { GateHeading.North, GateHeading.East, GateHeading.South, GateHeading.West })
        foreach (GateHeading incoming in new[] { GateHeading.North, GateHeading.East, GateHeading.South, GateHeading.West })
        {
            GateCell d = OneCellGateRules.Direction(incoming);
            GateCell from = new GateCell(gate.x - d.x, gate.y - d.y);
            GateDecision decision = OneCellGateRules.EvaluateEntry(gate, allowed, from, d, true);
            Assert.AreEqual(allowed == incoming, decision.allowed);
            Assert.AreEqual(allowed != incoming, decision.showClosedDeniedFeedback);
            Assert.AreEqual(gate, decision.occupiedGateCell);
        }
        Assert.AreEqual(1, OneCellGateRules.GateOccupiedCellCount);
    }
    [Test] public void OutletIsCheckedButNeverBecomesASecondGateCell()
    {
        var gate = new GateCell(3, 5);
        GateDecision d = OneCellGateRules.EvaluateEntry(gate, GateHeading.East, new GateCell(2, 5), new GateCell(1, 0), false);
        Assert.AreEqual(GateAttemptResult.DeniedOutputBlocked, d.result);
        Assert.AreEqual(new GateCell(4, 5), d.exitCell);
        Assert.AreEqual(gate, d.occupiedGateCell);
    }
    [Test] public void SideReverseZeroAndDiagonalExitsAreDenied()
    {
        var gate = new GateCell(1, 1);
        foreach (GateCell delta in new[] {new GateCell(-1,0),new GateCell(0,1),new GateCell(0,-1),new GateCell(0,0),new GateCell(1,1)})
            Assert.IsFalse(OneCellGateRules.EvaluateExit(gate, GateHeading.East, delta, true).allowed);
    }
    [Test] public void UnrelatedMoveDoesNotTriggerThisGate()
    {
        GateDecision d=OneCellGateRules.EvaluateEntry(new GateCell(9,9),GateHeading.North,new GateCell(0,0),new GateCell(0,1),true);
        Assert.AreEqual(GateAttemptResult.NotEnteringThisGate,d.result);
        Assert.IsFalse(d.showClosedDeniedFeedback);
        Assert.AreEqual(GateAttemptResult.NotEnteringThisGate, OneCellGateRules.EvaluateEntry(new GateCell(9,9),GateHeading.North,new GateCell(0,0),new GateCell(0,0),true).result);
    }
}
#endif
