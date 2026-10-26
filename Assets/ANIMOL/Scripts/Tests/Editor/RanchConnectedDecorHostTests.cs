using System.Collections.Generic;
using ANIMOL.Art.RanchConnectedDecorV01;
using ANIMOL.Art.V12;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public sealed class RanchConnectedDecorHostTests
{
    PuzzleMap map;
    PuzzleTheme theme;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
        Assert.NotNull(theme);
        Assert.NotNull(theme.ranchConnectedDecorV01Catalog);
        Assert.NotNull(theme.ranchConnectedDecorV01ManifestPin);
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.theme = theme;
        map.Resize(12, 12);
        for (var y = 0; y < 12; y++) for (var x = 0; x < 12; x++)
            map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
    }

    [TearDown]
    public void TearDown() { if (map) Object.DestroyImmediate(map); }

    Sprite Source(string id)
    {
        var sprite = theme.starwayRanch.Find("props/" + id + ".png");
        Assert.NotNull(sprite, id);
        return sprite;
    }

    RanchDecorPlan Plan()
    {
        Assert.IsTrue(RanchConnectedDecorHost.TryCreatePlan(map, theme.ranchConnectedDecorV01Catalog,
            theme.ranchConnectedDecorV01ManifestPin.text.Trim(), map.decorations,
            out var plan, out _, out _, out var error), error);
        return plan;
    }

    [Test]
    public void FenceUsesSharedConnectionGroupAndErasesWholeSourcePlacement()
    {
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Source("fence_horizontal"),
            new Vector2Int(2, 3), Color.white, "yard", 0, 0, out var error), error);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Source("fence_horizontal"),
            new Vector2Int(4, 3), Color.white, "yard", 0, 0, out error), error);
        var plan = Plan();
        Assert.AreEqual(2, map.decorations.Count);
        Assert.AreNotEqual(map.decorations[0].placementId, map.decorations[1].placementId);
        Assert.AreEqual("yard", map.decorations[0].connectionGroup);
        Assert.AreEqual("yard", map.decorations[1].connectionGroup);
        Assert.AreEqual(4, plan.NetworkCount);
        Assert.AreEqual(4, plan.ClaimCount);
        Assert.AreEqual(10, plan.GetNetwork(1).neighborMask);
        Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(3, 3)));
        Assert.AreEqual(1, map.decorations.Count);
        Assert.AreEqual(2, Plan().ClaimCount);
    }

    [Test]
    public void OneCellBrushCanRenderStandaloneAndThenConnectToNeighbor()
    {
        var pin = theme.ranchConnectedDecorV01ManifestPin.text.Trim();
        var file = RanchConnectedDecorV01Planner.NetworkFile(RanchConnectedDecorV01Planner.CreamFence, 0);
        Assert.IsTrue(theme.ranchConnectedDecorV01Catalog.TryGetExact(file, pin, out var art, out var error), error);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, art.Sprite,
            new Vector2Int(7, 7), Color.white, "single_line", 0, 0, out error), error);
        Assert.AreEqual(RanchConnectedDecorV01Planner.CreamFence, map.decorations[0].visualFamily);
        Assert.AreEqual(0, Plan().GetNetwork(0).neighborMask);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, art.Sprite,
            new Vector2Int(8, 7), Color.white, "single_line", 0, 0, out error), error);
        var linked = Plan();
        Assert.AreEqual(2, linked.NetworkCount);
        Assert.AreEqual(2, linked.GetNetwork(0).neighborMask);
        Assert.AreEqual(8, linked.GetNetwork(1).neighborMask);
        Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(8, 7)));
        Assert.AreEqual(0, Plan().GetNetwork(0).neighborMask);
    }

    [Test]
    public void FeedingCornerRejectsMissingThirdCellWithoutPartialReservation()
    {
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Source("water_trough"),
            new Vector2Int(10, 4), Color.white, "", 0, 0, out _));
        Assert.IsEmpty(map.decorations);
        map.SetTerrain(new Vector2Int(4, 4), TerrainMaterial.Stone);
        map.RemoveTerrain(new Vector2Int(5, 4)); // V12 bridge_decor owns the exposed Stone edge.
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Source("water_trough"),
            new Vector2Int(2, 4), Color.white, "", 0, 0, out _));
        Assert.IsEmpty(map.decorations);
        map.SetTerrain(new Vector2Int(5, 4), TerrainMaterial.Grass);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Source("water_trough"),
            new Vector2Int(2, 4), Color.white, "", 0, 0, out var error), error);
        var plan = Plan();
        Assert.AreEqual(1, map.decorations.Count);
        Assert.AreEqual(1, plan.GroupCount);
        Assert.AreEqual(3, plan.ClaimCount);
        Assert.AreEqual(3, plan.GetGroup(0).widthCells);
        for (var x = 2; x <= 4; x++) Assert.IsFalse(map.Walkable(new Vector2Int(x, 4)));
        Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(4, 4)));
        Assert.IsEmpty(map.decorations);
        Assert.AreEqual(0, Plan().ClaimCount);
        for (var x = 2; x <= 4; x++) Assert.IsTrue(map.Walkable(new Vector2Int(x, 4)));
    }

    [Test]
    public void WindmillKeepsFullArtButBlocksOnlyTwoFrontCells()
    {
        var body = Source("windmill_body");
        var full = Source("windmill");
        var choice = System.Linq.Enumerable.Single(PuzzleMapEditor.DecorationChoices(theme),
            item => item.sourceId == "props/windmill_body.png");
        Assert.AreSame(body, choice.sprite);
        Assert.AreSame(full, choice.preview);
        var anchor = new Vector2Int(2, 3);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, body, anchor, Color.white,
            "", 0, 0, out var error), error);
        for (var x = 2; x <= 3; x++)
        {
            Assert.IsFalse(map.Walkable(new Vector2Int(x, 3)));
            Assert.IsTrue(map.Walkable(new Vector2Int(x, 4)));
        }
        Assert.IsTrue(map.Walkable(new Vector2Int(4, 4)));
        var host = new GameObject("windmill view test");
        try
        {
            host.AddComponent<PuzzleMapView>().Bind(map);
            var motion = host.GetComponentInChildren<StarwayRanchV12RotorVisual>();
            Assert.NotNull(motion);
            Assert.AreEqual(new Vector3(3f, 4f), motion.transform.localPosition);
            var renderer = motion.transform.Find("Fixed body").GetComponent<SpriteRenderer>();
            Assert.AreSame(body, renderer.sprite);
            Assert.Greater(renderer.sortingOrder, 40);
            Assert.AreSame(Source("windmill_rotor"),
                motion.transform.Find("Four blade rotor").GetComponent<SpriteRenderer>().sprite);
            Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(3, 3)));
            for (var y = 3; y <= 4; y++) for (var x = 2; x <= 3; x++)
                Assert.IsTrue(map.Walkable(new Vector2Int(x, y)));
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void ControllerCreatesOneRendererForCompleteBarnAndClearsRejectedSnapshot()
    {
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Source("ranch_barn"),
            new Vector2Int(2, 3), Color.white, "", 0, 0, out var error), error);
        for (var x = 2; x <= 4; x++)
        {
            Assert.IsFalse(map.Walkable(new Vector2Int(x, 3)));
            Assert.IsTrue(map.Walkable(new Vector2Int(x, 4)));
        }
        var node = new GameObject("connected decor host test");
        try
        {
            var controller = node.AddComponent<RanchConnectedDecorV01Controller>();
            var pin = theme.ranchConnectedDecorV01ManifestPin.text.Trim();
            Assert.IsTrue(controller.Configure(theme.ranchConnectedDecorV01Catalog, pin, "Default"), controller.LastDiagnostic);
            Assert.IsTrue(RanchConnectedDecorHost.TryCreatePlan(map, theme.ranchConnectedDecorV01Catalog,
                pin, map.decorations, out _, out var networks, out var groups, out error), error);
            Assert.IsTrue(controller.SetLayout(networks, groups, new Vector3(-.5f, -.5f), 1,
                claim => RanchConnectedDecorHost.AllowClaim(map, map.decorations, claim)), controller.LastDiagnostic);
            Assert.AreEqual(6, controller.CurrentPlan.ClaimCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            Assert.AreEqual(1, node.GetComponentsInChildren<SpriteRenderer>().Length);
            Assert.AreEqual(new Vector3(3, 3.5f), node.GetComponentInChildren<SpriteRenderer>().transform.position);
            Assert.Greater(node.GetComponentInChildren<SpriteRenderer>().sortingOrder, 40);
            // A bad submitted snapshot is fail-closed; a host-rejected edit leaves the old layout untouched.
            LogAssert.Expect(LogType.Error, new Regex("Decoration snapshot rejected atomically"));
            Assert.IsFalse(controller.SetLayout(new List<RanchDecorNetworkCell>(),
                new List<RanchDecorGroupPlacement> { new RanchDecorGroupPlacement("bad", "barnyard_3x2", 11, 11) },
                new Vector3(-.5f, -.5f), 1,
                claim => RanchConnectedDecorHost.AllowClaim(map, map.decorations, claim)));
            Assert.IsNull(controller.CurrentPlan);
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { Object.DestroyImmediate(node); }
    }

    [Test]
    public void WrongManifestPinCannotShowAnOldDecoration()
    {
        var node = new GameObject("connected decor wrong pin test");
        try
        {
            var controller = node.AddComponent<RanchConnectedDecorV01Controller>();
            LogAssert.Expect(LogType.Error, new Regex("mismatching current manifest SHA pin"));
            Assert.IsFalse(controller.Configure(theme.ranchConnectedDecorV01Catalog, new string('0', 64), "Default"));
            Assert.IsNull(controller.CurrentPlan);
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { Object.DestroyImmediate(node); }
    }

    [Test]
    public void MissingCatalogDoesNotRevealTheSingleCellSourceSprite()
    {
        var file = RanchConnectedDecorV01Planner.NetworkFile(RanchConnectedDecorV01Planner.CreamFence, 0);
        Assert.IsTrue(theme.ranchConnectedDecorV01Catalog.TryGetExact(file,
            theme.ranchConnectedDecorV01ManifestPin.text.Trim(), out var art, out var error), error);
        var unboundTheme = Object.Instantiate(theme);
        unboundTheme.ranchConnectedDecorV01Catalog = null;
        map.theme = unboundTheme;
        map.decorations.Add(new DecorationCell { position = new Vector2Int(6, 6), sprite = art.Sprite,
            tint = Color.white, visualFamily = RanchConnectedDecorV01Planner.CreamFence });
        var node = new GameObject("unbound ranch view test");
        try
        {
            LogAssert.Expect(LogType.Error, new Regex("catalog or manifest pin is missing"));
            node.AddComponent<PuzzleMapView>().Bind(map);
            foreach (var renderer in node.GetComponentsInChildren<SpriteRenderer>())
                Assert.AreNotSame(art.Sprite, renderer.sprite);
        }
        finally { Object.DestroyImmediate(node); Object.DestroyImmediate(unboundTheme); }
    }
}
