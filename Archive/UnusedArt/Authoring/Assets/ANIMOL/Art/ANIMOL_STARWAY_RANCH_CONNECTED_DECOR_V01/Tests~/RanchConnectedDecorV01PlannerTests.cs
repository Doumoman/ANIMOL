using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.RanchConnectedDecorV01;

// Optional EditMode source. Tests~ is not auto-imported into production assemblies.
// Move into the project's actual test assembly and run there. Delivered status NOT_RUN.
public sealed class RanchConnectedDecorV01PlannerTests
{
    private static bool Allow(RanchDecorClaim claim) { return true; }
    private static RanchDecorGroupSize GroupSize(string key)
    { switch (key) { case "hay_store_2x1": return new RanchDecorGroupSize(2, 1);
        case "feeding_corner_3x1": return new RanchDecorGroupSize(3, 1);
        case "tree_flower_corner_2x2": return new RanchDecorGroupSize(2, 2);
        case "barnyard_3x2": return new RanchDecorGroupSize(3, 2); default: throw new ArgumentException("Unknown current key"); } }
    private static RanchDecorNetworkSelection At(RanchDecorPlan plan, int x, int y)
    { for (int i = 0; i < plan.NetworkCount; i++) { var r = plan.GetNetwork(i); if (r.x == x && r.y == y) return r; } throw new Exception("Missing cell"); }

    [Test]
    public void DifferentFamilyGroupLayerOrHeightNeverConnects()
    {
        var rows = new[] {
            new RanchDecorNetworkCell(0, 0, "cream_fence", "yard"),
            new RanchDecorNetworkCell(1, 0, "cream_fence", "other_yard"),
            new RanchDecorNetworkCell(-1, 0, "meadow_stone_wall", "yard"),
            new RanchDecorNetworkCell(0, 1, "cream_fence", "yard", 1),
            new RanchDecorNetworkCell(0, -1, "cream_fence", "yard", 0, 1) };
        var plan = RanchConnectedDecorV01Planner.CreatePlan(rows, null, GroupSize, Allow);
        Assert.AreEqual(0, At(plan, 0, 0).neighborMask);
    }
    [Test]
    public void DeletingAndAddingNeighborsRecomputesBothSidesFromWholeBoard()
    {
        var a = new RanchDecorNetworkCell(-2, -3, "cream_fence", "yard");
        var b = new RanchDecorNetworkCell(-1, -3, "cream_fence", "yard");
        var linked = RanchConnectedDecorV01Planner.CreatePlan(new[] { a, b }, null, GroupSize, Allow);
        Assert.AreEqual(2, At(linked, -2, -3).neighborMask); Assert.AreEqual(8, At(linked, -1, -3).neighborMask);
        var removed = RanchConnectedDecorV01Planner.CreatePlan(new[] { a }, null, GroupSize, Allow);
        Assert.AreEqual(0, At(removed, -2, -3).neighborMask);
    }
    [Test]
    public void MeadowKeepsDiagonalBitsSeparateFromCardinalRailBits()
    {
        var plan = RanchConnectedDecorV01Planner.CreatePlan(new[] {
            new RanchDecorNetworkCell(0, 0, "raised_flower_meadow", "bed"),
            new RanchDecorNetworkCell(1, 1, "raised_flower_meadow", "bed") }, null, GroupSize, Allow);
        Assert.AreEqual(2, At(plan, 0, 0).neighborMask); Assert.AreEqual(32, At(plan, 1, 1).neighborMask);
        var paths = new HashSet<string>(); for (int mask = 0; mask < 256; mask++) paths.Add(RanchConnectedDecorV01Planner.NetworkFile("raised_flower_meadow", mask));
        Assert.AreEqual(256, paths.Count);
    }
    [Test]
    public void MultiCellGroupsHaveOneAnchorAndReserveEveryMemberWithoutOneCellFit()
    {
        var plan = RanchConnectedDecorV01Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("large_yard", "barnyard_3x2", -4, -3) }, GroupSize, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(6, plan.ClaimCount);
        Assert.AreEqual(-2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-2, plan.GetGroup(0).CenterY);
        Assert.AreEqual(3, plan.GetGroup(0).widthCells); Assert.AreEqual(2, plan.GetGroup(0).heightCells);
    }
    [Test]
    public void OneRejectedGroupMemberRejectsEntireSnapshotAndNoPartialPlanIsReturned()
    {
        RanchDecorPlan result = null;
        Assert.Throws<ArgumentException>(() => result = RanchConnectedDecorV01Planner.CreatePlan(null,
            new[] { new RanchDecorGroupPlacement("feeding", "feeding_corner_3x1", 1, 2) }, GroupSize,
            claim => claim.cell.x != 3));
        Assert.IsNull(result);
    }
    [Test]
    public void DuplicateFootprintsOrPlacementIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV01Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("a", "hay_store_2x1", 0, 0),
            new RanchDecorGroupPlacement("b", "tree_flower_corner_2x2", 1, 0) }, GroupSize, Allow));
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV01Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("a", "hay_store_2x1", 0, 0),
            new RanchDecorGroupPlacement("a", "hay_store_2x1", 5, 5) }, GroupSize, Allow));
    }
    [Test]
    public void LayeredClaimsStillRequireExplicitHostApproval()
    {
        int calls = 0;
        var plan = RanchConnectedDecorV01Planner.CreatePlan(new[] {
            new RanchDecorNetworkCell(0, 0, "cream_fence", "fence", 1),
            new RanchDecorNetworkCell(0, 0, "raised_flower_meadow", "bed", 2) }, null, GroupSize, claim => { calls++; return true; });
        Assert.AreEqual(2, calls); Assert.AreEqual(2, plan.ClaimCount);
        Assert.Throws<ArgumentNullException>(() => RanchConnectedDecorV01Planner.CreatePlan(
            new[] { new RanchDecorNetworkCell(0, 0, "cream_fence", "fence") }, null, GroupSize, null));
    }
    [Test]
    public void ShortRunsHaveNoExtraPostAndLongPostsStayAwayFromEndsAndJunctions()
    {
        for (int length = 2; length <= 4; length++)
        {
            var rows = new List<RanchDecorNetworkCell>(); for (int x = 0; x < length; x++) rows.Add(new RanchDecorNetworkCell(x, 0, "cream_fence", "line"));
            var plan = RanchConnectedDecorV01Planner.CreatePlan(rows, null, GroupSize, Allow);
            for (int i = 0; i < plan.NetworkCount; i++) Assert.IsNull(plan.GetNetwork(i).periodicPostFile);
        }
        var longRows = new List<RanchDecorNetworkCell>(); for (int x = -12; x <= 12; x++) longRows.Add(new RanchDecorNetworkCell(x, 0, "cream_fence", "line"));
        longRows.Add(new RanchDecorNetworkCell(0, 1, "cream_fence", "line"));
        var longPlan = RanchConnectedDecorV01Planner.CreatePlan(longRows, null, GroupSize, Allow, 17);
        int posts = 0;
        for (int i = 0; i < longPlan.NetworkCount; i++)
        {
            var row = longPlan.GetNetwork(i); if (row.periodicPostFile == null) continue; posts++;
            Assert.GreaterOrEqual(row.x, -10); Assert.LessOrEqual(row.x, 10); Assert.Greater(Math.Abs(row.x), 1);
        }
        Assert.Greater(posts, 0);
    }
    [Test]
    public void StableSelectionsIgnoreHostCollectionOrder()
    {
        var rows = new List<RanchDecorNetworkCell>(); for (int x = -9; x <= 9; x++) rows.Add(new RanchDecorNetworkCell(x, 0, "cream_fence", "line"));
        var a = RanchConnectedDecorV01Planner.CreatePlan(rows, null, GroupSize, Allow, 73); rows.Reverse();
        var b = RanchConnectedDecorV01Planner.CreatePlan(rows, null, GroupSize, Allow, 73);
        for (int i = 0; i < a.NetworkCount; i++) { Assert.AreEqual(a.GetNetwork(i).file, b.GetNetwork(i).file); Assert.AreEqual(a.GetNetwork(i).periodicPostFile, b.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void RowDownGroupUsesBottomRowAndEmptySnapshotCanClearEverything()
    {
        Assert.AreEqual(new RanchDecorCell(2, -4), RanchConnectedDecorV01Planner.FromRowDownCell(2, 4));
        Assert.AreEqual(new RanchDecorCell(2, -5), RanchConnectedDecorV01Planner.FromRowDownGroupBottomLeft(2, 4, 2));
        var empty = RanchConnectedDecorV01Planner.CreatePlan(null, null, null, null); Assert.AreEqual(0, empty.ClaimCount); Assert.AreEqual(0, empty.GroupCount);
    }
}
