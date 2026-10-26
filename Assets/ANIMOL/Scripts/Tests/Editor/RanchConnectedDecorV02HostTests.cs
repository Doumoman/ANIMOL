using System.Linq;
using ANIMOL.Art.RanchConnectedDecorV02;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RanchConnectedDecorV02HostTests
{
    PuzzleMap map;
    PuzzleTheme theme;
    RanchConnectedDecorV02Catalog catalog;
    string pin;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
            "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset");
        Assert.NotNull(theme);
        catalog = theme.ranchConnectedDecorV02Catalog;
        Assert.NotNull(catalog);
        Assert.NotNull(theme.ranchConnectedDecorV02ManifestPin);
        pin = theme.ranchConnectedDecorV02ManifestPin.text.Trim();
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.name = "ranch-v02-host-test"; map.theme = theme; map.Resize(12, 12);
        for (var y = 0; y < 12; y++) for (var x = 0; x < 12; x++)
            map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
    }

    [TearDown] public void TearDown() { if (map) Object.DestroyImmediate(map); }

    Sprite Group(string key)
    {
        Assert.IsTrue(catalog.TryGetGroup(key, pin, out _, out var art, out var error), error);
        return art.Sprite;
    }

    [Test]
    public void PaletteUsesCurrentTwentySixVisualsAndRetainsFunctionalWindmill()
    {
        var choices = PuzzleMapEditor.DecorationChoices(theme);
        Assert.AreEqual(26, choices.Count(row => row.sourceId.StartsWith("ranch:")));
        Assert.IsTrue(choices.Where(row => row.sourceId.StartsWith("ranch:")).All(row => row.sprite));
        Assert.IsTrue(choices.Any(row => row.sourceId == "props/windmill_body.png"));
    }

    [Test]
    public void WholeThreeByTwoGroupHasOneRendererAndErasesFromAnyClaim()
    {
        var anchor = new Vector2Int(5, 6);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Group("picnic_arbor_3x2"),
            anchor, Color.white, "", 0, 0, out var error), error);
        Assert.IsTrue(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out error), error);
        Assert.IsFalse(snapshot.isExampleOnly);
        Assert.AreEqual(1, snapshot.groupPlacements.Length);
        var host = new GameObject("ranch v02 map view test");
        try
        {
            var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
            var controller = view.ConnectedDecor;
            Assert.NotNull(controller);
            Assert.AreEqual(6, controller.CurrentPlan.ClaimCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            Assert.AreSame(Group("picnic_arbor_3x2"),
                controller.GetComponentInChildren<SpriteRenderer>().sprite);
            Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(7, 7)));
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void DeniedCellRejectsWholeGroupWithoutMutatingMap()
    {
        var blocker = new DecorationCell { position = new Vector2Int(7, 7),
            sprite = theme.starwayRanch.Find("props/windmill_body.png"), tint = Color.white };
        map.decorations.Add(blocker);
        var before = map.decorations.Count;
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("picnic_arbor_3x2"),
            new Vector2Int(5, 6), Color.white, "", 0, 0, out _));
        Assert.AreEqual(before, map.decorations.Count);
    }

    [Test]
    public void LegacyWindmillAndBarnKeepTheirPriorGameplayFootprints()
    {
        var windmill = new DecorationCell { position = new Vector2Int(2, 2),
            sprite = theme.starwayRanch.Find("props/windmill_body.png"), tint = Color.white };
        var barn = new DecorationCell { position = new Vector2Int(5, 5),
            sprite = theme.starwayRanch.Find("props/ranch_barn.png"), tint = Color.white };
        Assert.NotNull(windmill.sprite); Assert.NotNull(barn.sprite);
        Assert.IsTrue(RanchConnectedDecorV02Host.Occupies(map, windmill, new Vector2Int(3, 2)));
        Assert.IsFalse(RanchConnectedDecorV02Host.Occupies(map, windmill, new Vector2Int(2, 3)));
        Assert.IsTrue(RanchConnectedDecorV02Host.Occupies(map, barn, new Vector2Int(7, 5)));
        Assert.IsFalse(RanchConnectedDecorV02Host.Occupies(map, barn, new Vector2Int(5, 6)));
    }

    [Test]
    public void DeclaredVisualKeyCannotRelabelAnUnrelatedSprite()
    {
        map.decorations.Add(new DecorationCell { position = new Vector2Int(3, 3),
            sprite = theme.starwayRanch.Find("props/windmill_body.png"), tint = Color.white,
            visualFamily = "chicken_coop_2x2", placementId = "invalid-relabel" });
        Assert.IsFalse(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 1, out _, out var error));
        StringAssert.Contains("invalid", error);
    }
}
