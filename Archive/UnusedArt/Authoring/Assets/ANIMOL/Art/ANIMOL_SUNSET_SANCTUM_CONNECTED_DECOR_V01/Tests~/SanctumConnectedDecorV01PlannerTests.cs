using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.SanctumConnectedDecorV01;

// Optional EditMode sources. Tests~ is not auto-imported into production assemblies.
// Move into the project's real test assembly, then execute with Unity TestRunner.
// Delivery status: NOT_RUN. These sources are not a claim of compilation or execution.
public sealed class SanctumConnectedDecorV01PlannerTests
{
    private static bool Allow(SanctumDecorClaim claim) { return true; }
    private static bool Posts(string family) { return SanctumConnectedDecorV01Planner.SupportsPeriodicPost(family); }
    private static SanctumDecorGroupSize Size(string key)
    {
        SanctumDecorGroupSize size;
        if (!SanctumConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out size)) throw new ArgumentException("Unknown current key");
        return size;
    }
    private static SanctumDecorNetworkSelection At(SanctumDecorPlan plan, int x, int y)
    {
        for (int i = 0; i < plan.NetworkCount; i++) { var row = plan.GetNetwork(i); if (row.x == x && row.y == y) return row; }
        throw new Exception("Missing cell");
    }
    [Test]
    public void SixDistinctFamiliesProduceNinetySixExactCardinalVariantPaths()
    {
        var paths = new HashSet<string>();
        Assert.AreEqual(6, SanctumConnectedDecorV01Planner.NetworkFamilies.Length);
        foreach (string family in SanctumConnectedDecorV01Planner.NetworkFamilies)
            for (int mask = 0; mask < 16; mask++) paths.Add(SanctumConnectedDecorV01Planner.NetworkFile(family, mask));
        Assert.AreEqual(96, paths.Count);
        var returned = SanctumConnectedDecorV01Planner.NetworkFamilies; returned[0] = "mutated";
        Assert.AreEqual("ivory_star_balustrade", SanctumConnectedDecorV01Planner.NetworkFamilies[0]);
    }
    [Test]
    public void AllSixFamiliesSelectEveryMaskWithNorthAtPositiveY()
    {
        foreach (string family in SanctumConnectedDecorV01Planner.NetworkFamilies)
        for (int mask = 0; mask < 16; mask++)
        {
            var rows = new List<SanctumDecorNetworkCell> { new SanctumDecorNetworkCell(0, 0, family, "same_band") };
            if ((mask & 1) != 0) rows.Add(new SanctumDecorNetworkCell(0, 1, family, "same_band"));
            if ((mask & 2) != 0) rows.Add(new SanctumDecorNetworkCell(1, 0, family, "same_band"));
            if ((mask & 4) != 0) rows.Add(new SanctumDecorNetworkCell(0, -1, family, "same_band"));
            if ((mask & 8) != 0) rows.Add(new SanctumDecorNetworkCell(-1, 0, family, "same_band"));
            var plan = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow);
            Assert.AreEqual(mask, At(plan, 0, 0).neighborMask);
            Assert.AreEqual(SanctumConnectedDecorV01Planner.NetworkFile(family, mask), At(plan, 0, 0).file);
        }
    }
    [Test]
    public void DifferentFamilyGroupLayerOrHeightNeverConnects()
    {
        var rows = new[] {
            new SanctumDecorNetworkCell(0, 0, "ivory_star_balustrade", "railing"),
            new SanctumDecorNetworkCell(1, 0, "ivory_star_balustrade", "other_railing"),
            new SanctumDecorNetworkCell(-1, 0, "brass_lattice_screen", "railing"),
            new SanctumDecorNetworkCell(0, 1, "ivory_star_balustrade", "railing", 1),
            new SanctumDecorNetworkCell(0, -1, "ivory_star_balustrade", "railing", 0, 1) };
        var plan = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow);
        Assert.AreEqual(0, At(plan, 0, 0).neighborMask);
    }
    [Test]
    public void DiagonalContactDoesNotInventACardinalConnection()
    {
        foreach (string family in SanctumConnectedDecorV01Planner.NetworkFamilies)
        {
            var plan = SanctumConnectedDecorV01Planner.CreatePlan(new[] {
                new SanctumDecorNetworkCell(0, 0, family, "band"), new SanctumDecorNetworkCell(1, 1, family, "band") }, null, Size, Allow);
            Assert.AreEqual(0, At(plan, 0, 0).neighborMask); Assert.AreEqual(0, At(plan, 1, 1).neighborMask);
        }
    }
    [Test]
    public void AddingAndDeletingNeighborsRecomputesBothSidesFromWholeBoard()
    {
        var a = new SanctumDecorNetworkCell(-2, -3, "archive_shelf_run", "archives");
        var b = new SanctumDecorNetworkCell(-1, -3, "archive_shelf_run", "archives");
        var linked = SanctumConnectedDecorV01Planner.CreatePlan(new[] { a, b }, null, Size, Allow);
        Assert.AreEqual(2, At(linked, -2, -3).neighborMask); Assert.AreEqual(8, At(linked, -1, -3).neighborMask);
        var removed = SanctumConnectedDecorV01Planner.CreatePlan(new[] { a }, null, Size, Allow);
        Assert.AreEqual(0, At(removed, -2, -3).neighborMask);
    }
    [Test]
    public void ThereAreTwelveWholeLargePropsAndEightPointProps()
    {
        int large = 0, point = 0; var keys = new HashSet<string>();
        foreach (string key in SanctumConnectedDecorV01Planner.GroupKeys)
        {
            Assert.IsTrue(keys.Add(key)); var size = Size(key);
            Assert.GreaterOrEqual(size.width, 1); Assert.GreaterOrEqual(size.height, 1);
            if (SanctumConnectedDecorV01Planner.IsPointGroup(key)) point++; else large++;
        }
        Assert.AreEqual(12, large); Assert.AreEqual(8, point); Assert.AreEqual(20, keys.Count);
    }
    [Test]
    public void MultiCellPropIsOneSelectionAndClaimsEveryFootprintMember()
    {
        var plan = SanctumConnectedDecorV01Planner.CreatePlan(null, new[] {
            new SanctumDecorGroupPlacement("altar", "sanctum_altar_3x2", -4, -3) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(6, plan.ClaimCount);
        Assert.AreEqual(-2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-2, plan.GetGroup(0).CenterY);
        Assert.AreEqual(3, plan.GetGroup(0).widthCells); Assert.AreEqual(2, plan.GetGroup(0).heightCells);
    }
    [Test]
    public void PointPropUsesSameExplicitPlacementApiWithOneClaim()
    {
        var plan = SanctumConnectedDecorV01Planner.CreatePlan(null, new[] {
            new SanctumDecorGroupPlacement("votives", "votive_cluster", 2, -1) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(1, plan.ClaimCount);
        Assert.AreEqual(2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-.5, plan.GetGroup(0).CenterY);
    }
    [Test]
    public void OneRejectedPropMemberRejectsEntireReplacementPlan()
    {
        SanctumDecorPlan result = null;
        Assert.Throws<ArgumentException>(() => result = SanctumConnectedDecorV01Planner.CreatePlan(null,
            new[] { new SanctumDecorGroupPlacement("bench", "archive_worktable_3x1", 1, 2) }, Size,
            claim => claim.cell.x != 3));
        Assert.IsNull(result);
    }
    [Test]
    public void DuplicateFootprintsAndPlacementIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => SanctumConnectedDecorV01Planner.CreatePlan(null, new[] {
            new SanctumDecorGroupPlacement("a", "celestial_chart_table_2x2", 0, 0),
            new SanctumDecorGroupPlacement("b", "ruined_colonnade_3x1", 1, 0) }, Size, Allow));
        Assert.Throws<ArgumentException>(() => SanctumConnectedDecorV01Planner.CreatePlan(null, new[] {
            new SanctumDecorGroupPlacement("a", "broken_pillar", 0, 0),
            new SanctumDecorGroupPlacement("a", "broken_pillar", 5, 5) }, Size, Allow));
    }
    [Test]
    public void LayeredClaimsStillRequireExplicitHostApproval()
    {
        int calls = 0;
        var plan = SanctumConnectedDecorV01Planner.CreatePlan(new[] {
            new SanctumDecorNetworkCell(0, 0, "ivory_star_balustrade", "railing", 1),
            new SanctumDecorNetworkCell(0, 0, "votive_light_channel", "planters", 2) }, null, Size, claim => { calls++; return true; });
        Assert.AreEqual(2, calls); Assert.AreEqual(2, plan.ClaimCount);
        Assert.Throws<ArgumentNullException>(() => SanctumConnectedDecorV01Planner.CreatePlan(
            new[] { new SanctumDecorNetworkCell(0, 0, "ivory_star_balustrade", "railing") }, null, Size, null));
    }
    [Test]
    public void OptionalPostAbsenceNeverRequestsUndeclaredPostArt()
    {
        var rows = new List<SanctumDecorNetworkCell>();
        for (int x = -12; x <= 12; x++) rows.Add(new SanctumDecorNetworkCell(x, 0, "ivory_star_balustrade", "line"));
        var noProvider = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17);
        var unavailable = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17, 4, family => false);
        for (int i = 0; i < noProvider.NetworkCount; i++)
        { Assert.IsNull(noProvider.GetNetwork(i).periodicPostFile); Assert.IsNull(unavailable.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void PostsOnlyAppearInLongStraightRunsForTheFirstTwoFamilies()
    {
        foreach (string family in SanctumConnectedDecorV01Planner.NetworkFamilies)
        {
            for (int length = 2; length <= 4; length++)
            {
                var shortRows = new List<SanctumDecorNetworkCell>();
                for (int x = 0; x < length; x++) shortRows.Add(new SanctumDecorNetworkCell(x, 0, family, "line"));
                var shortPlan = SanctumConnectedDecorV01Planner.CreatePlan(shortRows, null, Size, Allow, 17, 4, Posts);
                for (int i = 0; i < shortPlan.NetworkCount; i++) Assert.IsNull(shortPlan.GetNetwork(i).periodicPostFile);
            }
            var rows = new List<SanctumDecorNetworkCell>();
            for (int x = -12; x <= 12; x++) rows.Add(new SanctumDecorNetworkCell(x, 0, family, "line"));
            rows.Add(new SanctumDecorNetworkCell(0, 1, family, "line"));
            var plan = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 17, 4, ignored => true);
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
        var rows = new List<SanctumDecorNetworkCell>();
        for (int x = -9; x <= 9; x++) rows.Add(new SanctumDecorNetworkCell(x, 0, "brass_lattice_screen", "line"));
        var a = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts); rows.Reverse();
        var b = SanctumConnectedDecorV01Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts);
        for (int i = 0; i < a.NetworkCount; i++)
        { Assert.AreEqual(a.GetNetwork(i).file, b.GetNetwork(i).file); Assert.AreEqual(a.GetNetwork(i).periodicPostFile, b.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void WrongFamilyMaskOrFootprintIsRejectedInsteadOfFallback()
    {
        Assert.Throws<ArgumentException>(() => SanctumConnectedDecorV01Planner.NetworkFile("legacy", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SanctumConnectedDecorV01Planner.NetworkFile("archive_shelf_run", 16));
        Assert.Throws<ArgumentException>(() => SanctumConnectedDecorV01Planner.CreatePlan(null,
            new[] { new SanctumDecorGroupPlacement("altar", "sanctum_altar_3x2", 0, 0) }, key => new SanctumDecorGroupSize(1, 1), Allow));
    }
    [Test]
    public void RowDownGroupsUseTheirBottomRowAndAnEmptySnapshotCanClearEverything()
    {
        Assert.AreEqual(new SanctumDecorCell(2, -4), SanctumConnectedDecorV01Planner.FromRowDownCell(2, 4));
        Assert.AreEqual(new SanctumDecorCell(2, -5), SanctumConnectedDecorV01Planner.FromRowDownGroupBottomLeft(2, 4, 2));
        var empty = SanctumConnectedDecorV01Planner.CreatePlan(null, null, null, null);
        Assert.AreEqual(0, empty.ClaimCount); Assert.AreEqual(0, empty.GroupCount);
    }
}
