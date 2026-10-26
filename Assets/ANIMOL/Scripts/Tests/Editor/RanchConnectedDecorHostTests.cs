using ANIMOL.Art.RanchConnectedDecorV01;
using ANIMOL.Art.RanchConnectedDecorV02;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Regression tests for saved V01 Sprite identities entering the V02 visual host.
// The old catalog remains read-only migration data; it never renders these cells.
public sealed class RanchConnectedDecorHostTests
{
    PuzzleMap map;
    PuzzleTheme theme;

    [SetUp]
    public void SetUp()
    {
        theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
            "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset");
        Assert.NotNull(theme);
        Assert.NotNull(theme.ranchConnectedDecorV01Catalog);
        Assert.NotNull(theme.ranchConnectedDecorV02Catalog);
        map = ScriptableObject.CreateInstance<PuzzleMap>();
        map.name = "ranch-v01-to-v02-migration-test"; map.theme = theme; map.Resize(10, 10);
        map.start = Vector2Int.zero; map.exit = new Vector2Int(9, 9);
        for (var y = 0; y < 10; y++) for (var x = 0; x < 10; x++)
            map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
    }

    [TearDown]
    public void TearDown() { if (map) Object.DestroyImmediate(map); }

    Sprite OldSprite(string family, string kind)
    {
        var old = theme.ranchConnectedDecorV01Catalog;
        for (var i = 0; i < old.SpriteCount; i++)
        {
            var row = old.GetSpriteAt(i);
            if (row.Family == family && row.Kind == kind && row.Sprite) return row.Sprite;
        }
        Assert.Fail("No saved V01 identity for " + family + "/" + kind);
        return null;
    }

    [Test]
    public void SavedV01FenceSpritePublishesV02ConnectionsOnly()
    {
        var oldSprite = OldSprite("cream_fence", "connected_decoration");
        map.decorations.Add(new DecorationCell { position = new Vector2Int(3, 3),
            sprite = oldSprite, tint = Color.white, connectionGroup = "same-belt" });
        map.decorations.Add(new DecorationCell { position = new Vector2Int(4, 3),
            sprite = oldSprite, tint = Color.white, connectionGroup = "same-belt" });
        Assert.IsTrue(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 1, out var snapshot, out var error), error);
        Assert.AreEqual(2, snapshot.networkCells.Length);
        var node = new GameObject("migrated ranch view");
        try
        {
            var view = node.AddComponent<PuzzleMapView>(); view.Bind(map);
            Assert.AreEqual(2, view.ConnectedDecor.CurrentPlan.NetworkCount);
            foreach (var renderer in view.ConnectedDecor.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.AreNotSame(oldSprite, renderer.sprite);
                StringAssert.Contains("ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02",
                    AssetDatabase.GetAssetPath(renderer.sprite));
            }
        }
        finally { Object.DestroyImmediate(node); }
    }

    [Test]
    public void SavedV01BarnIdentityKeepsThreeByOneGameplayOccupancy()
    {
        var oldSprite = OldSprite("barnyard_3x2", "multi_cell_group");
        var cell = new DecorationCell { position = new Vector2Int(2, 2),
            sprite = oldSprite, tint = Color.white, placementId = "saved-barn" };
        Assert.IsTrue(RanchConnectedDecorV02Host.Occupies(map, cell, new Vector2Int(4, 2)));
        Assert.IsFalse(RanchConnectedDecorV02Host.Occupies(map, cell, new Vector2Int(2, 3)));
        map.decorations.Add(cell);
        Assert.IsTrue(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 1, out var snapshot, out var error), error);
        Assert.AreEqual(1, snapshot.groupPlacements.Length);
        Assert.AreEqual("barnyard_3x2", snapshot.groupPlacements[0].groupKey);
    }

    [Test]
    public void WrongCurrentPinCannotFallBackToV01Visual()
    {
        var copy = Object.Instantiate(theme);
        var badPin = new TextAsset(new string('0', 64));
        try
        {
            copy.ranchConnectedDecorV02ManifestPin = badPin;
            map.theme = copy;
            map.decorations.Add(new DecorationCell { position = new Vector2Int(2, 2),
                sprite = OldSprite("cream_fence", "connected_decoration"), tint = Color.white });
            Assert.IsFalse(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
                new Vector3(-.5f, -.5f), 1, 1, out _, out _));
        }
        finally { Object.DestroyImmediate(copy); Object.DestroyImmediate(badPin); }
    }

    [Test]
    public void LegacyTwoCellFenceExpandsIntoOneSharedV02Belt()
    {
        var fence = theme.starwayRanch.Find("props/fence_horizontal.png");
        Assert.NotNull(fence);
        map.decorations.Add(new DecorationCell { position = new Vector2Int(2, 2),
            sprite = fence, tint = Color.white, connectionGroup = "saved-belt" });
        Assert.IsTrue(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 1, out var snapshot, out var error), error);
        Assert.AreEqual(2, snapshot.networkCells.Length);
        Assert.AreEqual("cream_fence", snapshot.networkCells[0].family);
        Assert.AreEqual("saved-belt", snapshot.networkCells[0].connectionGroup);
        Assert.AreEqual("saved-belt", snapshot.networkCells[1].connectionGroup);
        Assert.IsTrue(map.DecorationAt(new Vector2Int(3, 2)));
    }

    [Test]
    public void LegacyTroughCannotShrinkWhenThirdCellIsOutsideBoard()
    {
        var trough = theme.starwayRanch.Find("props/water_trough.png");
        Assert.NotNull(trough);
        map.decorations.Add(new DecorationCell { position = new Vector2Int(8, 3),
            sprite = trough, tint = Color.white, placementId = "saved-trough" });
        Assert.IsFalse(RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
            new Vector3(-.5f, -.5f), 1, 1, out _, out _));
        Assert.AreEqual(1, map.OutsideCount(map.Width, map.Height));
        Assert.AreEqual(1, map.decorations.Count);
    }
}
