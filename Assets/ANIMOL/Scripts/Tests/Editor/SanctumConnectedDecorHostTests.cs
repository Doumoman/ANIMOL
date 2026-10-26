using System.Linq;
using ANIMOL.Art.SanctumConnectedDecorV01;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SanctumConnectedDecorHostTests
{
    PuzzleMap map;
    PuzzleTheme theme;
    SanctumConnectedDecorV01Catalog catalog;
    string pin;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
            "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset");
        Assert.NotNull(theme);
        catalog = theme.sanctumConnectedDecorV01Catalog;
        Assert.NotNull(catalog);
        Assert.NotNull(theme.sanctumConnectedDecorV01ManifestPin);
        pin = theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.name = "sanctum-host-test"; map.theme = theme; map.Resize(12, 12);
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
    public void EditorPaletteUsesExactlyTheCurrentTwentySixDecorKeys()
    {
        var choices = PuzzleMapEditor.DecorationChoices(theme);
        Assert.AreEqual(26, choices.Length);
        Assert.IsTrue(choices.All(row => row.sourceId.StartsWith("sanctum:") && row.sprite));
    }

    [Test]
    public void ThreeByTwoPropPublishesOneRendererAndErasesFromAnyFootprintCell()
    {
        var anchor = new Vector2Int(5, 6);
        Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, Group("sanctum_altar_3x2"),
            anchor, Color.white, "", 0, 0, out var error), error);
        Assert.IsFalse(map.Walkable(anchor));
        Assert.IsTrue(map.Walkable(new Vector2Int(7, 7)));
        Assert.IsTrue(SanctumConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out error), error);
        Assert.IsFalse(snapshot.isExampleOnly);
        Assert.AreEqual(1, snapshot.groupPlacements.Length);
        Assert.AreEqual(6, snapshot.allowedDecorCells.Count(row =>
            row.x >= 5 && row.x <= 7 && row.y >= 6 && row.y <= 7));

        var host = new GameObject("sanctum map view test");
        try
        {
            var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
            var controller = view.SanctumConnectedDecor;
            Assert.NotNull(controller);
            Assert.NotNull(controller.CurrentPlan);
            Assert.AreEqual(1, controller.CurrentPlan.GroupCount);
            Assert.AreEqual(6, controller.CurrentPlan.ClaimCount);
            Assert.AreEqual(1, controller.OwnedVisualCount);
            var renderer = controller.GetComponentInChildren<SpriteRenderer>();
            Assert.AreSame(Group("sanctum_altar_3x2"), renderer.sprite);
            Assert.AreEqual(new Vector3(6, 6.5f), renderer.transform.position);
            var source = controller.GetComponent<SanctumConnectedDecorV01PublishedSnapshotSource>();
            Assert.IsTrue(source.TryCaptureSnapshot(out snapshot, out error), error);
            Assert.AreEqual(0, snapshot.revision);
            Assert.IsTrue(PuzzleMapEditor.TryEraseDecoration(map, new Vector2Int(7, 7)));
            Assert.IsTrue(source.TryCaptureSnapshot(out snapshot, out error), error);
            Assert.AreEqual(1, snapshot.revision);
            Assert.AreEqual(0, snapshot.groupPlacements.Length);
            Assert.AreEqual(0, controller.OwnedVisualCount);
            Assert.IsTrue(map.Walkable(anchor));
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void PartialFootprintAndBridgeRailCellsRejectTheWholeEdit()
    {
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("sanctum_altar_3x2"),
            new Vector2Int(10, 7), Color.white, "", 0, 0, out _));
        Assert.IsEmpty(map.decorations);
        map.SetTerrain(new Vector2Int(0, 4), TerrainMaterial.Stone);
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, Group("sanctum_altar_3x2"),
            new Vector2Int(0, 4), Color.white, "", 0, 0, out _));
        Assert.IsEmpty(map.decorations);
    }

    [Test]
    public void LegacyV02PropIsNotSilentlyDisplayedOrPlacedAsCurrentArt()
    {
        Sprite legacy = null;
        var old = theme.sunsetSanctumV02Catalog;
        for (var i = 0; i < old.SpriteCount; i++)
            if (old.GetSpriteRecord(i).File == "props/sanctum/broken_column.png")
                legacy = old.GetSpriteRecord(i).Sprite;
        Assert.NotNull(legacy);
        Assert.IsTrue(SanctumConnectedDecorHost.IsLegacySanctumProp(theme, legacy));
        Assert.IsFalse(PuzzleMapEditor.TryPlaceDecoration(map, legacy,
            new Vector2Int(3, 3), Color.white, "", 0, 0, out _));
        var proposed = new[] { new DecorationCell { position = new Vector2Int(3, 3),
            sprite = legacy, tint = Color.white } };
        Assert.IsFalse(SanctumConnectedDecorHost.TryCreateSnapshot(map, proposed,
            new Vector3(-.5f, -.5f), 1, 0, out _, out _));
    }
}
