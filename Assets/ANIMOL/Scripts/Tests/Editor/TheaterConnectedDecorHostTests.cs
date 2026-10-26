using System.Linq;
using ANIMOL.Art.TheaterConnectedDecorV01;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TheaterConnectedDecorHostTests
{
    PuzzleMap map;
    PuzzleTheme theme;
    TheaterConnectedDecorV01Catalog catalog;
    string pin;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
            "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
        Assert.NotNull(theme);
        catalog = theme.theaterConnectedDecorV01Catalog;
        Assert.NotNull(catalog);
        Assert.NotNull(theme.theaterConnectedDecorV01ManifestPin);
        pin = theme.theaterConnectedDecorV01ManifestPin.text.Trim();
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.name = "theater-host-test"; map.theme = theme; map.Resize(12, 12);
        map.start = new Vector2Int(1, 1); map.exit = new Vector2Int(10, 10); map.targetBubbles = 0;
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
    public void PaletteExposesAllCurrentDecorAndNotTheOldFunctionalFootlightStates()
    {
        var choices = PuzzleMapEditor.DecorationChoices(theme);
        Assert.AreEqual(26, choices.Length);
        Assert.IsTrue(choices.All(row => row.sourceId.StartsWith("theater:") && row.sprite));
        Assert.IsFalse(choices.Any(row => row.sourceId.Contains("footlights_on")));
        Assert.IsNull(TheaterConnectedDecorHost.SourceMapping(theme,
            theme.memorySkyTheater.v02.Find("props/organ.png")));
    }

    [Test]
    public void AThreeByTwoPropPublishesOneAuthoritativeVisualAndEraseUpdatesIt()
    {
        var anchor = new Vector2Int(5, 6);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Group("grand_piano_3x2"),
            anchor, Color.white, "", 0, 0, out var error), error);
        Assert.IsTrue(TheaterConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out error), error);
        Assert.IsFalse(snapshot.isExampleOnly);
        Assert.AreEqual(1, snapshot.groupPlacements.Length);
        Assert.AreEqual(6, snapshot.allowedDecorCells.Count(row =>
            row.x >= 5 && row.x <= 7 && row.y >= 6 && row.y <= 7));

        var host = new GameObject("theater map view test");
        try
        {
            var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
            var controller = view.TheaterConnectedDecor;
            Assert.NotNull(controller);
            Assert.NotNull(controller.CurrentPlan);
            Assert.AreEqual(1, controller.CurrentPlan.GroupCount);
            Assert.AreEqual(6, controller.CurrentPlan.ClaimCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            var renderer = controller.GetComponentInChildren<SpriteRenderer>();
            Assert.AreSame(Group("grand_piano_3x2"), renderer.sprite);
            Assert.AreEqual(new Vector3(6, 6.5f), renderer.transform.position);
            var source = controller.GetComponent<TheaterConnectedDecorV01PublishedSnapshotSource>();
            Assert.IsTrue(source.TryCaptureSnapshot(out snapshot, out error), error);
            Assert.AreEqual(0, snapshot.revision);
            Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(7, 7)));
            Assert.IsTrue(source.TryCaptureSnapshot(out snapshot, out error), error);
            Assert.AreEqual(1, snapshot.revision);
            Assert.AreEqual(0, snapshot.groupPlacements.Length);
            var binding = controller.GetComponent<TheaterConnectedDecorV01Binding>();
            Assert.IsTrue(binding.RefreshNow(out error), error);
            Assert.IsTrue(binding.LastReport.success, binding.LastDiagnostic);
            Assert.AreEqual(0, binding.LastReport.groupCount);
            Assert.NotNull(controller.CurrentPlan);
            Assert.AreEqual(0, controller.CurrentPlan.GroupCount);
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void RejectedGroupDoesNotPartiallyClaimOrAlterTheMap()
    {
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("grand_piano_3x2"),
            new Vector2Int(10, 7), Color.white, "", 0, 0, out _));
        Assert.IsEmpty(map.decorations);
        var sprite = Group("grand_piano_3x2");
        map.decorations.Add(new DecorationCell { position = new Vector2Int(10, 7),
            sprite = sprite, tint = Color.white, visualFamily = "grand_piano_3x2", placementId = "outside" });
        Assert.IsFalse(TheaterConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 0, out _, out _));
        Assert.AreEqual(1, map.decorations.Count);
    }
}
