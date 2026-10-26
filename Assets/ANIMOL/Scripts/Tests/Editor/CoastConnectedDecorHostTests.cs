using System.Linq;
using ANIMOL.Art.CoastConnectedDecorV01;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CoastConnectedDecorHostTests
{
    PuzzleMap map;
    PuzzleTheme theme;
    CoastConnectedDecorV01Catalog catalog;
    string pin;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
            "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset");
        Assert.NotNull(theme);
        catalog = theme.coastConnectedDecorV01Catalog;
        Assert.NotNull(catalog);
        Assert.NotNull(theme.coastConnectedDecorV01ManifestPin);
        pin = theme.coastConnectedDecorV01ManifestPin.text.Trim();
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.name = "coast-host-test"; map.theme = theme; map.Resize(12, 12);
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
    public void PaletteContainsOnlyTheCurrentTwentySixCoastDecorations()
    {
        var choices = PuzzleMapEditor.DecorationChoices(theme);
        Assert.AreEqual(26, choices.Length);
        Assert.IsTrue(choices.All(row => row.sourceId.StartsWith("coast:") && row.sprite));
    }

    [Test]
    public void SkiffIsOneRendererWithSixClaimsAndErasesFromAnyCell()
    {
        var anchor = new Vector2Int(5, 6);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Group("beached_skiff_3x2"),
            anchor, Color.white, "", 0, 0, out var error), error);
        Assert.IsTrue(CoastConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out error), error);
        Assert.IsFalse(snapshot.isExampleOnly);
        Assert.AreEqual(1, snapshot.groupPlacements.Length);
        var host = new GameObject("coast map view test");
        try
        {
            var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
            var controller = view.CoastConnectedDecor;
            Assert.NotNull(controller);
            Assert.NotNull(controller.CurrentPlan);
            Assert.AreEqual(6, controller.CurrentPlan.ClaimCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            Assert.AreSame(Group("beached_skiff_3x2"), controller.GetComponentInChildren<SpriteRenderer>().sprite);
            Assert.AreEqual(new Vector3(6, 6.5f), controller.GetComponentInChildren<SpriteRenderer>().transform.position);
            Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(7, 7)));
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void WaterAndBridgeRailRejectWholePlacementWithoutMutation()
    {
        var water = new Vector2Int(7, 7);
        map.scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
        try
        {
            map.scenario.water.Add(water);
            Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("beached_skiff_3x2"),
                new Vector2Int(5, 6), Color.white, "", 0, 0, out _));
            Assert.IsEmpty(map.decorations);
            map.scenario.water.Clear();
            map.SetTerrain(new Vector2Int(0, 4), TerrainMaterial.Sand);
            Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("shore_steps_2x1"),
                new Vector2Int(0, 4), Color.white, "", 0, 0, out _));
            Assert.IsEmpty(map.decorations);
        }
        finally { Object.DestroyImmediate(map.scenario); }
    }

    [Test]
    public void ExistingOverlappingLayersRequireExplicitHostPolicy()
    {
        var sprite = Group("dock_lantern");
        var cells = new[] {
            new DecorationCell { position = new Vector2Int(5, 5), sprite = sprite,
                tint = Color.white, visualFamily = "dock_lantern", placementId = "one", logicalLayer = 0 },
            new DecorationCell { position = new Vector2Int(5, 5), sprite = sprite,
                tint = Color.white, visualFamily = "dock_lantern", placementId = "two", logicalLayer = 1 }
        };
        Assert.IsFalse(CoastConnectedDecorHost.TryCreateSnapshot(map, cells,
            new Vector3(-.5f, -.5f), 1, 0, out _, out var error));
        StringAssert.Contains("overlap", error);
    }
}
