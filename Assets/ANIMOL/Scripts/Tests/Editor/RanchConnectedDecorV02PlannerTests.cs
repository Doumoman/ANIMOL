using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.RanchConnectedDecorV02;

// Optional EditMode sources. Tests~ is not auto-imported into production assemblies.
// Move into the project's real test assembly, then execute with Unity TestRunner.
// Delivery status: NOT_RUN. These sources are not a claim of compilation or execution.
public sealed class RanchConnectedDecorV02PlannerTests
{
    private static bool Allow(RanchDecorClaim claim) { return true; }
    private static bool Posts(string family) { return RanchConnectedDecorV02Planner.SupportsPeriodicPost(family); }
    private static RanchDecorGroupSize Size(string key)
    {
        RanchDecorGroupSize size;
        if (!RanchConnectedDecorV02Planner.TryGetDeclaredGroupSize(key, out size)) throw new ArgumentException("Unknown current key");
        return size;
    }
    private static RanchDecorNetworkSelection At(RanchDecorPlan plan, int x, int y)
    {
        for (int i = 0; i < plan.NetworkCount; i++) { var row = plan.GetNetwork(i); if (row.x == x && row.y == y) return row; }
        throw new Exception("Missing cell");
    }
    [Test]
    public void HybridFamiliesProduceThreeHundredThirtySixExactNetworkVariantPaths()
    {
        var paths = new HashSet<string>();
        Assert.AreEqual(6, RanchConnectedDecorV02Planner.NetworkFamilies.Length);
        foreach (string family in RanchConnectedDecorV02Planner.NetworkFamilies)
            for (int mask = 0; mask < RanchConnectedDecorV02Planner.MaskCount(family); mask++) paths.Add(RanchConnectedDecorV02Planner.NetworkFile(family, mask));
        Assert.AreEqual(336, paths.Count);
        var returned = RanchConnectedDecorV02Planner.NetworkFamilies; returned[0] = "mutated";
        Assert.AreEqual("cream_fence", RanchConnectedDecorV02Planner.NetworkFamilies[0]);
    }
    [Test]
    public void FiveCardinalFamiliesSelectEveryMaskWithNorthAtPositiveY()
    {
        foreach (string family in RanchConnectedDecorV02Planner.NetworkFamilies)
        for (int mask = 0; family != RanchConnectedDecorV02Planner.Meadow && mask < 16; mask++)
        {
            var rows = new List<RanchDecorNetworkCell> { new RanchDecorNetworkCell(0, 0, family, "same_band") };
            if ((mask & 1) != 0) rows.Add(new RanchDecorNetworkCell(0, 1, family, "same_band"));
            if ((mask & 2) != 0) rows.Add(new RanchDecorNetworkCell(1, 0, family, "same_band"));
            if ((mask & 4) != 0) rows.Add(new RanchDecorNetworkCell(0, -1, family, "same_band"));
            if ((mask & 8) != 0) rows.Add(new RanchDecorNetworkCell(-1, 0, family, "same_band"));
            var plan = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow);
            Assert.AreEqual(mask, At(plan, 0, 0).neighborMask);
            Assert.AreEqual(RanchConnectedDecorV02Planner.NetworkFile(family, mask), At(plan, 0, 0).file);
        }
    }
    [Test]
    public void DifferentFamilyGroupLayerOrHeightNeverConnects()
    {
        var rows = new[] {
            new RanchDecorNetworkCell(0, 0, "cream_fence", "railing"),
            new RanchDecorNetworkCell(1, 0, "cream_fence", "other_railing"),
            new RanchDecorNetworkCell(-1, 0, "meadow_stone_wall", "railing"),
            new RanchDecorNetworkCell(0, 1, "cream_fence", "railing", 1),
            new RanchDecorNetworkCell(0, -1, "cream_fence", "railing", 0, 1) };
        var plan = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow);
        Assert.AreEqual(0, At(plan, 0, 0).neighborMask);
    }
    [Test]
    public void DiagonalContactDoesNotInventACardinalConnection()
    {
        foreach (string family in RanchConnectedDecorV02Planner.NetworkFamilies)
        {
            if (family == RanchConnectedDecorV02Planner.Meadow) continue;
            var plan = RanchConnectedDecorV02Planner.CreatePlan(new[] {
                new RanchDecorNetworkCell(0, 0, family, "band"), new RanchDecorNetworkCell(1, 1, family, "band") }, null, Size, Allow);
            Assert.AreEqual(0, At(plan, 0, 0).neighborMask); Assert.AreEqual(0, At(plan, 1, 1).neighborMask);
        }
    }
    [Test]
    public void AddingAndDeletingNeighborsRecomputesBothSidesFromWholeBoard()
    {
        var a = new RanchDecorNetworkCell(-2, -3, "orchard_trellis", "connected_shelf");
        var b = new RanchDecorNetworkCell(-1, -3, "orchard_trellis", "connected_shelf");
        var linked = RanchConnectedDecorV02Planner.CreatePlan(new[] { a, b }, null, Size, Allow);
        Assert.AreEqual(2, At(linked, -2, -3).neighborMask); Assert.AreEqual(8, At(linked, -1, -3).neighborMask);
        var removed = RanchConnectedDecorV02Planner.CreatePlan(new[] { a }, null, Size, Allow);
        Assert.AreEqual(0, At(removed, -2, -3).neighborMask);
    }
    [Test]
    public void ThereAreTwelveWholeLargePropsAndEightPointProps()
    {
        int large = 0, point = 0; var keys = new HashSet<string>();
        foreach (string key in RanchConnectedDecorV02Planner.GroupKeys)
        {
            Assert.IsTrue(keys.Add(key)); var size = Size(key);
            Assert.GreaterOrEqual(size.width, 1); Assert.GreaterOrEqual(size.height, 1);
            if (RanchConnectedDecorV02Planner.IsPointGroup(key)) point++; else large++;
        }
        Assert.AreEqual(12, large); Assert.AreEqual(8, point); Assert.AreEqual(20, keys.Count);
    }
    [Test]
    public void MultiCellPropIsOneSelectionAndClaimsEveryFootprintMember()
    {
        var plan = RanchConnectedDecorV02Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("primary_whole_prop", "barnyard_3x2", -4, -3) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(6, plan.ClaimCount);
        Assert.AreEqual(-2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-2, plan.GetGroup(0).CenterY);
        Assert.AreEqual(3, plan.GetGroup(0).widthCells); Assert.AreEqual(2, plan.GetGroup(0).heightCells);
    }
    [Test]
    public void PointPropUsesSameExplicitPlacementApiWithOneClaim()
    {
        var plan = RanchConnectedDecorV02Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("point_props", "flower_bucket", 2, -1) }, Size, Allow);
        Assert.AreEqual(1, plan.GroupCount); Assert.AreEqual(1, plan.ClaimCount);
        Assert.AreEqual(2.5, plan.GetGroup(0).CenterX); Assert.AreEqual(-.5, plan.GetGroup(0).CenterY);
    }
    [Test]
    public void OneRejectedPropMemberRejectsEntireReplacementPlan()
    {
        RanchDecorPlan result = null;
        Assert.Throws<ArgumentException>(() => result = RanchConnectedDecorV02Planner.CreatePlan(null,
            new[] { new RanchDecorGroupPlacement("bench", "feeding_corner_3x1", 1, 2) }, Size,
            claim => claim.cell.x != 3));
        Assert.IsNull(result);
    }
    [Test]
    public void DuplicateFootprintsAndPlacementIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV02Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("a", "orchard_bench_3x1", 0, 0),
            new RanchDecorGroupPlacement("b", "tree_flower_corner_2x2", 1, 0) }, Size, Allow));
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV02Planner.CreatePlan(null, new[] {
            new RanchDecorGroupPlacement("a", "tool_basket", 0, 0),
            new RanchDecorGroupPlacement("a", "tool_basket", 5, 5) }, Size, Allow));
    }
    [Test]
    public void LayeredClaimsStillRequireExplicitHostApproval()
    {
        int calls = 0;
        var plan = RanchConnectedDecorV02Planner.CreatePlan(new[] {
            new RanchDecorNetworkCell(0, 0, "cream_fence", "railing", 1),
            new RanchDecorNetworkCell(0, 0, "ranch_lantern_strip", "planters", 2) }, null, Size, claim => { calls++; return true; });
        Assert.AreEqual(2, calls); Assert.AreEqual(2, plan.ClaimCount);
        Assert.Throws<ArgumentNullException>(() => RanchConnectedDecorV02Planner.CreatePlan(
            new[] { new RanchDecorNetworkCell(0, 0, "cream_fence", "railing") }, null, Size, null));
    }
    [Test]
    public void OptionalPostAbsenceNeverRequestsUndeclaredPostArt()
    {
        var rows = new List<RanchDecorNetworkCell>();
        for (int x = -12; x <= 12; x++) rows.Add(new RanchDecorNetworkCell(x, 0, "cream_fence", "line"));
        var noProvider = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 17);
        var unavailable = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 17, 4, family => false);
        for (int i = 0; i < noProvider.NetworkCount; i++)
        { Assert.IsNull(noProvider.GetNetwork(i).periodicPostFile); Assert.IsNull(unavailable.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void PostsOnlyAppearInLongStraightRunsForTheFirstTwoFamilies()
    {
        foreach (string family in RanchConnectedDecorV02Planner.NetworkFamilies)
        {
            for (int length = 2; length <= 4; length++)
            {
                var shortRows = new List<RanchDecorNetworkCell>();
                for (int x = 0; x < length; x++) shortRows.Add(new RanchDecorNetworkCell(x, 0, family, "line"));
                var shortPlan = RanchConnectedDecorV02Planner.CreatePlan(shortRows, null, Size, Allow, 17, 4, Posts);
                for (int i = 0; i < shortPlan.NetworkCount; i++) Assert.IsNull(shortPlan.GetNetwork(i).periodicPostFile);
            }
            var rows = new List<RanchDecorNetworkCell>();
            for (int x = -12; x <= 12; x++) rows.Add(new RanchDecorNetworkCell(x, 0, family, "line"));
            rows.Add(new RanchDecorNetworkCell(0, 1, family, "line"));
            var plan = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 17, 4, ignored => true);
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
        var rows = new List<RanchDecorNetworkCell>();
        for (int x = -9; x <= 9; x++) rows.Add(new RanchDecorNetworkCell(x, 0, "meadow_stone_wall", "line"));
        var a = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts); rows.Reverse();
        var b = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 73, 4, Posts);
        for (int i = 0; i < a.NetworkCount; i++)
        { Assert.AreEqual(a.GetNetwork(i).file, b.GetNetwork(i).file); Assert.AreEqual(a.GetNetwork(i).periodicPostFile, b.GetNetwork(i).periodicPostFile); }
    }
    [Test]
    public void WrongFamilyMaskOrFootprintIsRejectedInsteadOfFallback()
    {
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV02Planner.NetworkFile("legacy", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RanchConnectedDecorV02Planner.NetworkFile("orchard_trellis", 16));
        Assert.Throws<ArgumentException>(() => RanchConnectedDecorV02Planner.CreatePlan(null,
            new[] { new RanchDecorGroupPlacement("primary_whole_prop", "barnyard_3x2", 0, 0) }, key => new RanchDecorGroupSize(1, 1), Allow));
    }
    [Test]
    public void RowDownGroupsUseTheirBottomRowAndAnEmptySnapshotCanClearEverything()
    {
        Assert.AreEqual(new RanchDecorCell(2, -4), RanchConnectedDecorV02Planner.FromRowDownCell(2, 4));
        Assert.AreEqual(new RanchDecorCell(2, -5), RanchConnectedDecorV02Planner.FromRowDownGroupBottomLeft(2, 4, 2));
        var empty = RanchConnectedDecorV02Planner.CreatePlan(null, null, null, null);
        Assert.AreEqual(0, empty.ClaimCount); Assert.AreEqual(0, empty.GroupCount);
    }
    [Test]
    public void EveryRawMeadowMaskPreservesIndependentDiagonalBitsAndD3Path()
    {
        int[,] offset = { {0,1}, {1,1}, {1,0}, {1,-1}, {0,-1}, {-1,-1}, {-1,0}, {-1,1} };
        for (int mask = 0; mask < 256; mask++)
        {
            var rows = new List<RanchDecorNetworkCell> { new RanchDecorNetworkCell(0, 0, RanchConnectedDecorV02Planner.Meadow, "patch") };
            for (int bit = 0; bit < 8; bit++) if ((mask & (1 << bit)) != 0)
                rows.Add(new RanchDecorNetworkCell(offset[bit,0], offset[bit,1], RanchConnectedDecorV02Planner.Meadow, "patch"));
            var plan = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow, 29);
            var centre = At(plan, 0, 0);
            Assert.AreEqual(mask, centre.neighborMask); Assert.IsTrue(centre.usesEightNeighbors);
            Assert.AreEqual("decor/connected/raised_flower_meadow/n" + mask.ToString("D3") + ".png", centre.file);
            Assert.AreEqual(rows.Count, plan.ClaimCount); // overlays never claim another slot
        }
    }
    [Test]
    public void MeadowDiagonalContactIsNotCollapsedToAnIsolated47StateTile()
    {
        var rows = new[] { new RanchDecorNetworkCell(0,0,RanchConnectedDecorV02Planner.Meadow,"patch"),
            new RanchDecorNetworkCell(1,1,RanchConnectedDecorV02Planner.Meadow,"patch") };
        var plan = RanchConnectedDecorV02Planner.CreatePlan(rows, null, Size, Allow);
        Assert.AreEqual(2, At(plan,0,0).neighborMask); Assert.AreEqual(32, At(plan,1,1).neighborMask);
        Assert.Throws<ArgumentOutOfRangeException>(() => RanchConnectedDecorV02Planner.NetworkFile(RanchConnectedDecorV02Planner.Meadow,256));
        Assert.Throws<ArgumentOutOfRangeException>(() => RanchConnectedDecorV02Planner.NetworkFile(RanchConnectedDecorV02Planner.Meadow,-1));
    }
    [Test]
    public void MeadowFlowersHaveOriginalOneInFiveDensityFourVariantsAndNoClaimOrPost()
    {
        var rows = new List<RanchDecorNetworkCell>();
        for (int x = -20; x <= 20; x++) rows.Add(new RanchDecorNetworkCell(x,0,RanchConnectedDecorV02Planner.Meadow,"patch"));
        var a = RanchConnectedDecorV02Planner.CreatePlan(rows,null,Size,Allow,123,4,ignored=>true); rows.Reverse();
        var b = RanchConnectedDecorV02Planner.CreatePlan(rows,null,Size,Allow,123,4,ignored=>true);
        Assert.AreEqual(41,a.ClaimCount); int overlays = 0;
        for (int i = 0; i < a.NetworkCount; i++)
        {
            var r = a.GetNetwork(i); Assert.IsNull(r.periodicPostFile); Assert.AreEqual(r.accentHash,b.GetNetwork(i).accentHash);
            Assert.AreEqual(r.flowerAccentFile,b.GetNetwork(i).flowerAccentFile);
            Assert.AreEqual(r.accentHash % 5u == 0, r.flowerAccentFile != null);
            if (r.flowerAccentFile != null) { overlays++; Assert.AreEqual(RanchConnectedDecorV02Planner.MeadowAccentFile((int)((r.accentHash / 5u) % 4u)),r.flowerAccentFile); }
        }
        Assert.Greater(overlays,0); Assert.Less(overlays,41);
        Assert.Throws<ArgumentOutOfRangeException>(() => RanchConnectedDecorV02Planner.MeadowAccentFile(4));
    }
    [Test]
    public void MeadowIgnoresDiagonalsFromAnotherFamilyGroupLayerOrHeight()
    {
        var rows = new[] { new RanchDecorNetworkCell(0,0,RanchConnectedDecorV02Planner.Meadow,"patch"),
            new RanchDecorNetworkCell(1,1,RanchConnectedDecorV02Planner.Meadow,"other"),
            new RanchDecorNetworkCell(1,-1,"herb_planter_strip","patch"),
            new RanchDecorNetworkCell(-1,-1,RanchConnectedDecorV02Planner.Meadow,"patch",1),
            new RanchDecorNetworkCell(-1,1,RanchConnectedDecorV02Planner.Meadow,"patch",0,1) };
        Assert.AreEqual(0,At(RanchConnectedDecorV02Planner.CreatePlan(rows,null,Size,Allow),0,0).neighborMask);
    }

}
