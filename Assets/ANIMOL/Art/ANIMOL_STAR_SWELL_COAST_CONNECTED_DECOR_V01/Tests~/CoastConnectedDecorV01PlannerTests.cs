using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.CoastConnectedDecorV01;

// Optional EditMode sources. Tests~ is not auto-imported into production assemblies.
// Move into the project's real test assembly, then execute with Unity TestRunner.
// Delivery status: NOT_RUN. These sources are not a claim of compilation or execution.
public sealed class CoastConnectedDecorV01PlannerTests
{
    private static bool Allow(CoastDecorClaim claim) { return true; }
    private static bool Posts(string family) { return CoastConnectedDecorV01Planner.SupportsPeriodicPost(family); }
    private static CoastDecorGroupSize Size(string key)
    {
        CoastDecorGroupSize size;
        if (!CoastConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out size)) throw new ArgumentException("Unknown current key");
        return size;
    }
    private static CoastDecorNetworkSelection At(CoastDecorPlan plan, int x, int y)
    {
        for (int i = 0; i < plan.NetworkCount; i++) { var row = plan.GetNetwork(i); if (row.x == x && row.y == y) return row; }
        throw new Exception("Missing cell");
    }
    [Test]
    public void SixDistinctFamiliesProduceNinetySixExactCardinalVariantPaths()
    {
        var paths = new HashSet<string>();
        Assert.AreEqual(6, CoastConnectedDecorV01Planner.NetworkFamilies.Length);
        foreach (string family in CoastConnectedDecorV01Planner.NetworkFamilies)
            for (int mask = 0; mask < 16; mask++) paths.Add(CoastConnectedDecorV01Planner.NetworkFile(family, mask));
        Assert.AreEqual(96, paths.Count);
        var returned = CoastConnectedDecorV01Planner.NetworkFamilies; returned[0] = "mutated";
        Assert.AreEqual("rope_dock_fence", CoastConnectedDecorV01Planner.NetworkFamilies[0]);
    }
    [Test]
    public void AllSixFamiliesSelectEveryMaskWithNorthAtPositiveY()
    {
        foreach (string family in CoastConnectedDecorV01Planner.NetworkFamilies)
        for (int mask = 0; mask < 16; mask++)
        {
            var rows = new List<CoastDecorNetworkCell> { new CoastDecorNetworkCell(0, 0, family, "same_band") };
            if ((mask & 1) != 0) rows.Add(new CoastDecorNetworkCell(0, 1, family, "same_band"));
            if ((mask & 2) != 0) rows.Add(new CoastDecorNetworkCell(1, 0, family, "same_band"));
            if ((mask & 4) != 0) rows.Add(new CoastDecorNetworkCell(0, -1, family, "same_band"));
            if ((mask & 8) != 0) rows.Add(new CoastDecorNetworkCell(-1, 0, family, "same_band"));
            var plan = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow);
            Assert.AreEqual(mask, At(plan, 0, 0).neighborMask);
            Assert.AreEqual(CoastConnectedDecorV01Planner.NetworkFile(family, mask), At(plan, 0, 0).file);
        }
    }
    [Test]
    public void DifferentFamilyGroupLayerOrHeightNeverConnects()
    {
        var rows = new[] {
            new CoastDecorNetworkCell(0, 0, "rope_dock_fence", "railing"),
            new CoastDecorNetworkCell(1, 0, "rope_dock_fence", "other_railing"),
            new CoastDecorNetworkCell(-1, 0, "driftwood_palisade", "railing"),
            new CoastDecorNetworkCell(0, 1, "rope_dock_fence", "railing", 1),
            new CoastDecorNetworkCell(0, -1, "rope_dock_fence", "railing", 0, 1) };
        var plan = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow);
        Assert.AreEqual(0, At(plan, 0, 0).neighborMask);
    }
    [Test]
    public void DiagonalContactDoesNotInventACardinalConnection()
    {
        foreach (string family in CoastConnectedDecorV01Planner.NetworkFamilies)
        {
            var plan = CoastConnectedDecorV01Planner.CreatePlan(new[] {
                new CoastDecorNetworkCell(0, 0, family, "band"), new CoastDecorNetworkCell(1, 1, family, "band") }, null, Size, Allow);
            Assert.AreEqual(0, At(plan, 0, 0).neighborMask); Assert.AreEqual(0, At(plan, 1, 1).neighborMask);
        }
    }
    [Test]
    public void AddingAndDeletingNeighborsRecomputesBothSidesFromWholeBoard()
    {
        var a = new CoastDecorNetworkCell(-2, -3, "fishing_net_screen", "connected_shelf");
        var b = new CoastDecorNetworkCell(-1, -3, "fishing_net_screen", "connected_shelf");
        var linked = CoastConnectedDecorV01Planner.CreatePlan(new[] { a, b }, null, Size, Allow);
        Assert.AreEqual(2, At(linked, -2, -3).neighborMask); Assert.AreEqual(8, At(linked, -1, -3).neighborMask);
        var removed = CoastConnectedDecorV01Planner.CreatePlan(new[] { a }, null, Size, Allow);
        Assert.AreEqual(0, At(removed, -2, -3).neighborMask);
    }
    [Test]
    public void ThereAreTwelveWholeLargePropsAndEightPointProps()
    {
        int large = 0, point = 0; var keys = new HashSet<string>();
        foreach (string key in CoastConnectedDecorV01Planner.GroupKeys)
        {
            Assert.IsTrue(keys.Add(key)); var size = Size(key);
            Assert.GreaterOrEqual(size.width, 1); Assert.GreaterOrEqual(size.height, 1);
            if (CoastConnectedDecorV01Planner.IsPointGroup(key)) point++; else large++;
        }
        Assert.AreEqual(12, large); Assert.AreEqual(8, point); Assert.AreEqual(20, keys.Count);
    }
    [Test]
    public void MultiCellPropIsOneSelectionAndClaimsEveryFootprintMember()
    {
        var plan = CoastConnectedDecorV01Planner.CreatePlan(null, new[] {
            new CoastDecorGroupPlacement("primary_whole_prop", "harbor_market_stall_3x2", -4, -3) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(6, plan.ClaimCount);
        Assert.AreEqual(-2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-2, plan.GetGroup(0).CenterY);
        Assert.AreEqual(3, plan.GetGroup(0).widthCells); Assert.AreEqual(2, plan.GetGroup(0).heightCells);
    }
    [Test]
    public void PointPropUsesSameExplicitPlacementApiWithOneClaim()
    {
        var plan = CoastConnectedDecorV01Planner.CreatePlan(null, new[] {
            new CoastDecorGroupPlacement("point_props", "message_post", 2, -1) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(1, plan.ClaimCount);
        Assert.AreEqual(2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-.5, plan.GetGroup(0).CenterY);
    }
    [Test]
    public void OneRejectedPropMemberRejectsEntireReplacementPlan()
    {
        CoastDecorPlan result = null;
        Assert.Throws<ArgumentException>(() => result = CoastConnectedDecorV01Planner.CreatePlan(null,
            new[] { new CoastDecorGroupPlacement("bench", "fisher_workbench_3x1", 1, 2) }, Size,
            claim => claim.cell.x != 3));
        Assert.IsNull(result);
    }
    [Test]
    public void DuplicateFootprintsAndPlacementIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => CoastConnectedDecorV01Planner.CreatePlan(null, new[] {
            new CoastDecorGroupPlacement("a", "cargo_pallet_2x2", 0, 0),
            new CoastDecorGroupPlacement("b", "driftwood_bench_3x1", 1, 0) }, Size, Allow));
        Assert.Throws<ArgumentException>(() => CoastConnectedDecorV01Planner.CreatePlan(null, new[] {
            new CoastDecorGroupPlacement("a", "mooring_bollard", 0, 0),
            new CoastDecorGroupPlacement("a", "mooring_bollard", 5, 5) }, Size, Allow));
    }
    [Test]
    public void LayeredClaimsStillRequireExplicitHostApproval()
    {
        int calls = 0;
        var plan = CoastConnectedDecorV01Planner.CreatePlan(new[] {
            new CoastDecorNetworkCell(0, 0, "rope_dock_fence", "railing", 1),
            new CoastDecorNetworkCell(0, 0, "harbor_lantern_channel", "planters", 2) }, null, Size, claim => { calls++; return true; });
        Assert.AreEqual(2, calls); Assert.AreEqual(2, plan.ClaimCount);
        Assert.Throws<ArgumentNullException>(() => CoastConnectedDecorV01Planner.CreatePlan(
            new[] { new CoastDecorNetworkCell(0, 0, "rope_dock_fence", "railing") }, null, Size, null));
    }
    [Test]
    public void OptionalPostAbsenceNeverRequestsUndeclaredPostArt()
    {
        var rows = new List<CoastDecorNetworkCell>();
        for (int x = -12; x <= 12; x++) rows.Add(new CoastDecorNetworkCell(x, 0, "rope_dock_fence", "line"));
        var noProvider = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17);
        var unavailable = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17, 4, family => false);
        for (int i = 0; i < noProvider.NetworkCount; i++)
        { Assert.IsNull(noProvider.GetNetwork(i).periodicPostFile); Assert.IsNull(unavailable.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void PostsOnlyAppearInLongStraightRunsForTheFirstTwoFamilies()
    {
        foreach (string family in CoastConnectedDecorV01Planner.NetworkFamilies)
        {
            for (int length = 2; length <= 4; length++)
            {
                var shortRows = new List<CoastDecorNetworkCell>();
                for (int x = 0; x < length; x++) shortRows.Add(new CoastDecorNetworkCell(x, 0, family, "line"));
                var shortPlan = CoastConnectedDecorV01Planner.CreatePlan(shortRows, null, Size, Allow, 17, 4, Posts);
                for (int i = 0; i < shortPlan.NetworkCount; i++) Assert.IsNull(shortPlan.GetNetwork(i).periodicPostFile);
            }
            var rows = new List<CoastDecorNetworkCell>();
            for (int x = -12; x <= 12; x++) rows.Add(new CoastDecorNetworkCell(x, 0, family, "line"));
            rows.Add(new CoastDecorNetworkCell(0, 1, family, "line"));
            var plan = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17, 4, ignored => true);
            int posts = 0;
            for (int i = 0; i < plan.NetworkCount; i++)
            {
                var row = plan.GetNetwork(i); if (row.periodicPostFile == null) continue; posts++;
                Assert.GreaterOrEqual(row.x, -10); Assert.LessOrEqual(row.x, 10); Assert.Greater(Math.Abs(row.x), 1);
            }
            if (Posts(family)) Assert.Greater(posts, 0); else Assert.AreEqual(0, posts);
        }
    }
    [Test]
    public void StableSelectionsIgnoreHostCollectionOrder()
    {
        var rows = new List<CoastDecorNetworkCell>();
        for (int x = -9; x <= 9; x++) rows.Add(new CoastDecorNetworkCell(x, 0, "driftwood_palisade", "line"));
        var a = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts); rows.Reverse();
        var b = CoastConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts);
        for (int i = 0; i < a.NetworkCount; i++)
        { Assert.AreEqual(a.GetNetwork(i).file, b.GetNetwork(i).file); Assert.AreEqual(a.GetNetwork(i).periodicPostFile, b.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void WrongFamilyMaskOrFootprintIsRejectedInsteadOfFallback()
    {
        Assert.Throws<ArgumentException>(() => CoastConnectedDecorV01Planner.NetworkFile("legacy", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CoastConnectedDecorV01Planner.NetworkFile("fishing_net_screen", 16));
        Assert.Throws<ArgumentException>(() => CoastConnectedDecorV01Planner.CreatePlan(null,
            new[] { new CoastDecorGroupPlacement("primary_whole_prop", "harbor_market_stall_3x2", 0, 0) }, key => new CoastDecorGroupSize(1, 1), Allow));
    }
    [Test]
    public void RowDownGroupsUseTheirBottomRowAndAnEmptySnapshotCanClearEverything()
    {
        Assert.AreEqual(new CoastDecorCell(2, -4), CoastConnectedDecorV01Planner.FromRowDownCell(2, 4));
        Assert.AreEqual(new CoastDecorCell(2, -5), CoastConnectedDecorV01Planner.FromRowDownGroupBottomLeft(2, 4, 2));
        var empty = CoastConnectedDecorV01Planner.CreatePlan(null, null, null, null);
        Assert.AreEqual(0, empty.ClaimCount); Assert.AreEqual(0, empty.GroupCount);
    }
}
