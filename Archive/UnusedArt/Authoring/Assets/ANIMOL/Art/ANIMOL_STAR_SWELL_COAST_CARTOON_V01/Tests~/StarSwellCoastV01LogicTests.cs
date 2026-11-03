using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.StarSwellCoastV01;

// Optional host EditMode test source. Tests~ is intentionally not imported automatically.
// Place in the existing project's test assembly and run there; delivered status NOT_RUN.
public sealed class StarSwellCoastV01LogicTests
{
    [Test]
    public void FerryMovesSameActorExactlyOneWaterCellInAllFourHeadings()
    {
        var boat = new CoastCell(6, 4);
        foreach (CoastHeading h in Enum.GetValues(typeof(CoastHeading)))
        {
            var p = StarSwellCoastV01Logic.PreflightBoardedFerry(boat, boat, h, CoastTarget.Water, true);
            Assert.AreEqual(CoastFerryAction.RideOneWaterCell, p.action);
            Assert.AreEqual(p.boatTo, p.actorTo);
            Assert.AreEqual(1, Math.Abs(p.boatTo.x - boat.x) + Math.Abs(p.boatTo.y - boat.y));
            var blocked = StarSwellCoastV01Logic.PreflightBoardedFerry(boat, boat, h, CoastTarget.Water, false);
            Assert.IsFalse(blocked.allowed); Assert.AreEqual(boat, blocked.boatTo); Assert.AreEqual(boat, blocked.actorTo);
        }
    }
    [Test]
    public void DisembarkingLeavesBoatInItsLastWaterCell()
    {
        var boat = new CoastCell(4, 5);
        var p = StarSwellCoastV01Logic.PreflightBoardedFerry(boat, boat, CoastHeading.East, CoastTarget.Land, true);
        Assert.AreEqual(CoastFerryAction.DisembarkToLand, p.action);
        Assert.AreEqual(boat, p.boatTo); Assert.AreEqual(new CoastCell(5, 5), p.actorTo);
        Assert.IsFalse(StarSwellCoastV01Logic.PreflightBoardedFerry(boat, new CoastCell(3, 5),
            CoastHeading.East, CoastTarget.Water, true).allowed);
        Assert.IsFalse(StarSwellCoastV01Logic.PreflightBoardedFerry(boat, boat,
            CoastHeading.East, CoastTarget.Land, false).allowed);
    }
    [Test]
    public void BarrelRollDoesNotDragPusherAlongItsWholeRoute()
    {
        var path = new List<CoastCell> { new CoastCell(3, 3), new CoastCell(4, 3), new CoastCell(5, 3) };
        var plan = StarSwellCoastV01Logic.FromAuthoritativeBarrelRoute(new CoastCell(2, 3),
            new CoastCell(1, 3), CoastHeading.East, path);
        Assert.AreEqual(new CoastCell(2, 3), plan.pusherDestination);
        Assert.AreEqual(new CoastCell(5, 3), plan.barrelDestination);
        path[0] = new CoastCell(99, 99); Assert.AreEqual(new CoastCell(3, 3), plan.GetStep(0));
    }
    [Test]
    public void BlockedAndBentBarrelRoutesCannotCommit()
    {
        Assert.Throws<ArgumentException>(() => StarSwellCoastV01Logic.FromAuthoritativeBarrelRoute(
            new CoastCell(2, 3), new CoastCell(1, 3), CoastHeading.East, new CoastCell[0]));
        Assert.Throws<ArgumentException>(() => StarSwellCoastV01Logic.FromAuthoritativeBarrelRoute(
            new CoastCell(2, 3), new CoastCell(1, 3), CoastHeading.East,
            new[] { new CoastCell(3, 3), new CoastCell(3, 4) }));
    }
    [Test]
    public void SpikeOnlyRisesAfterActualDepartureAndNeverAutoLowers()
    {
        var v = StarSwellCoastV01Logic.SpikeAfterHostMovement(CoastSpikeView.Flat, true, false);
        Assert.AreEqual(CoastSpikeView.OccupiedArmed, v);
        v = StarSwellCoastV01Logic.SpikeAfterHostMovement(v, false, false);
        Assert.AreEqual(CoastSpikeView.OccupiedArmed, v);
        v = StarSwellCoastV01Logic.SpikeAfterHostMovement(v, false, true);
        Assert.AreEqual(CoastSpikeView.Raised, v);
        Assert.AreEqual(v, StarSwellCoastV01Logic.SpikeAfterHostMovement(v, false, false));
    }
    [Test]
    public void FirstActualBombPushShowsThreeThenExactlyThreeLaterAcceptedOwnActionsExplode()
    {
        var v = StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(CoastBombView.Inert, true, true);
        Assert.AreEqual(CoastBombView.Three, v);
        v = StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(v, false, false);
        Assert.AreEqual(CoastBombView.Three, v);
        v = StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(v, true, false);
        Assert.AreEqual(CoastBombView.Two, v);
        v = StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(v, true, false);
        Assert.AreEqual(CoastBombView.One, v);
        v = StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(v, true, false);
        Assert.AreEqual(CoastBombView.Detonated, v);
    }
    [Test]
    public void InvalidPushCannotIgniteAndAnInertBombIgnoresUnrelatedOwnMoves()
    {
        Assert.AreEqual(CoastBombView.Inert,
            StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(CoastBombView.Inert, true, false));
        Assert.Throws<ArgumentException>(() => StarSwellCoastV01Logic.ExpectedBombViewAfterOwnAction(
            CoastBombView.Inert, false, true));
    }
    [Test]
    public void ExplicitEmptyBlastCellsStayEmptyAndNoCrossIsInvented()
    {
        Assert.AreEqual(0, StarSwellCoastV01Logic.CopyAuthoritativeBlastCells(new CoastCell[0]).Length);
        var cells = new[] { new CoastCell(4, 2), new CoastCell(7, 5) };
        var copy = StarSwellCoastV01Logic.CopyAuthoritativeBlastCells(cells);
        CollectionAssert.AreEqual(cells, copy);
        Assert.Throws<ArgumentException>(() => StarSwellCoastV01Logic.CopyAuthoritativeBlastCells(
            new[] { new CoastCell(1, 1), new CoastCell(1, 1) }));
    }
    [Test]
    public void OnlyLandNeighborsConnectAndTheRawEightBitMasksStayDistinct()
    {
        var paths = new HashSet<string>();
        for (int n = 0; n < 256; n++) paths.Add(StarSwellCoastV01TerrainSelector.File(
            StarSwellCoastV01TerrainSelector.Material.CoastSand, n));
        Assert.AreEqual(256, paths.Count);
        Func<int, int, bool> hostLandOnly = (x, y) => x == 1 && y == 0;
        Assert.AreEqual(StarSwellCoastV01TerrainSelector.E,
            StarSwellCoastV01TerrainSelector.NeighborMask(hostLandOnly, 0, 0));
    }
    [Test]
    public void FusedUndersidesStayWholeForTwoThreeFourAndNeverLeaveOneTail()
    {
        for (int n = 1; n <= 100; n++)
        {
            int[] spans = StarSwellCoastV01TerrainSelector.PlanContinuousSpanLengths(n, 17, -8, 4);
            int total = 0; foreach (int span in spans) { total += span; Assert.That(span, Is.InRange(n <= 4 ? 1 : 2, 4)); }
            Assert.AreEqual(n, total); if (n <= 4) Assert.AreEqual(1, spans.Length);
        }
    }
    [Test]
    public void GateDeniedOutputCannotOpenOrReserveSecondCell()
    {
        var gate = new GateCell(4, 4);
        var denied = OneCellGateRules.EvaluateEntry(gate, GateHeading.East,
            new GateCell(3, 4), new GateCell(1, 0), false);
        Assert.AreEqual(1, OneCellGateRules.GateOccupiedCellCount);
        Assert.IsFalse(denied.allowed); Assert.IsTrue(denied.showClosedDeniedFeedback);
        Assert.AreEqual(new GateCell(5, 4), denied.exitCell);
    }
}
