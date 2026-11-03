// Move into the project's existing EditMode test assembly to run in Unity.
// These tests were prepared here; Unity/C# execution has not been performed.
using System;
using NUnit.Framework;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Puzzle;
using UnityEngine;

public sealed class EchoTheaterV03TerrainTests
{
    [Test]
    public void AuthoredTerrainOverridesLegacyMasksAndNeverCreatesFloor()
    {
        var map = ScriptableObject.CreateInstance<PuzzleMap>();
        try
        {
            map.Resize(5, 1);
            for (int x = 0; x < 5; x++) map.SetTerrain(new Vector2Int(x, 0), TerrainMaterial.Grass);
            map.SetTerrain(new Vector2Int(2, 0), TerrainMaterial.TheaterStageFloor);
            map.SetTerrain(new Vector2Int(3, 0), TerrainMaterial.TheaterVelvetCarpet);
            map.SetTerrain(new Vector2Int(4, 0), TerrainMaterial.TheaterWoodCatwalk);
            var deck = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) };
            var velvet = new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(4, 0) };
            var rows = EchoTheaterTerrain.VisualRows(map, deck, velvet);
            Assert.That(rows, Is.EqualTo(new[] { "WSGSW" }));
            Assert.That(EchoTheaterV03MaterialSelector.GetTopSpritePath(rows, 2, 0), Does.Contain("stage_floor"));
            Assert.That(EchoTheaterV03MaterialSelector.GetTopSpritePath(rows, 3, 0), Does.Contain("velvet_carpet"));
            Assert.That(EchoTheaterV03MaterialSelector.GetTopSpritePath(rows, 4, 0), Does.Contain("wood_catwalk"));
            Assert.That(map.terrain, Has.Count.EqualTo(5));
            Assert.That(map.Walkable(new Vector2Int(3, 0)), Is.True);
            map.RemoveTerrain(new Vector2Int(4, 0));
            Assert.That(EchoTheaterTerrain.VisualRows(map, deck, velvet), Is.EqualTo(new[] { "WSGS0" }));
        }
        finally { UnityEngine.Object.DestroyImmediate(map); }
    }

    [Test]
    public void MapEditorPreviewUsesThePaintedTheaterSprite()
    {
        var source = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(
            "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-03_MapData.asset");
        Assert.That(source, Is.Not.Null);
        var map = UnityEngine.Object.Instantiate(source);
        try
        {
            var cell = map.terrain[0].position;
            map.SetTerrain(cell, TerrainMaterial.TheaterVelvetCarpet);
            var rows = ANIMOL.Presentation.Editor.PuzzleMapEditor.TheaterTerrainRows(map);
            var path = EchoTheaterV03MaterialSelector.GetTopSpritePath(rows, cell.x, map.Height - 1 - cell.y);
            Assert.That(path, Does.Contain("velvet_carpet"));
            Assert.That(ANIMOL.Presentation.Editor.PuzzleMapEditor.TerrainPreviewSprite(map, cell),
                Is.EqualTo(map.theme.memorySkyTheater.v02.Find(path)));
        }
        finally { UnityEngine.Object.DestroyImmediate(map); }
    }

    [Test]
    public void CarpetPaintCannotCreateFloorOccupancy()
    {
        bool[,] existingFloor = new bool[3, 1];
        existingFloor[0, 0] = true;
        string[] rows = EchoTheaterV03MaterialSelector.BuildVisualMaterialRows(
            existingFloor, (x, y) => false, (x, y) => true);
        Assert.That(rows[0], Is.EqualTo("S00"));
    }

    [Test]
    public void CarpetBoundaryCannotSplitTheSupportingStage()
    {
        var runs = EchoTheaterV03EdgeSelector.SelectRuns(new[] { "GSSG" }, 27);
        Assert.That(runs.Count, Is.EqualTo(1));
        Assert.That(runs[0].MaterialCode, Is.EqualTo('G'));
        Assert.That(runs[0].Length, Is.EqualTo(4));
        var spans = EchoTheaterV03EdgeSelector.PlanSpans(runs[0]);
        Assert.That(spans.Count, Is.EqualTo(1));
        Assert.That(spans[0].File,
            Does.StartWith("skirts/spans/stage_floor/exterior/l4_"));
    }

    [Test]
    public void CatwalkSupportRemainsContinuousAcrossLongRuns()
    {
        var run = EchoTheaterV03EdgeSelector.SelectRuns(new[] { new string('W', 17) }, 73)[0];
        int covered = 0;
        foreach (var span in EchoTheaterV03EdgeSelector.PlanSpans(run))
        {
            Assert.That(span.StartOffset, Is.EqualTo(covered));
            Assert.That(span.Length, Is.InRange(2, 4));
            covered += span.Length;
        }
        Assert.That(covered, Is.EqualTo(17));
    }

    [Test]
    public void CatwalkBanksStayOpenAndCarpetGetsNoRails()
    {
        Assert.That(EchoTheaterV03BridgeDecorSelector.Select(new[] { "SSSS" }), Is.Empty);
        foreach (var decoration in EchoTheaterV03BridgeDecorSelector.Select(new[] { "GWWG" }))
            if (decoration.Kind == "rail")
                Assert.That(decoration.Direction, Is.EqualTo("N").Or.EqualTo("S"));
    }

    [TestCase(0, 0, 0, 0u)]
    [TestCase(1, 2, 27, 936298167u)]
    [TestCase(-1, -1, 0, 4245456706u)]
    [TestCase(-197, 53, -32, 2096046893u)]
    public void SeededPlacementHashMatchesExportVectors(int x, int y, int seed, uint expected)
    {
        Assert.That(EchoTheaterV03EdgeSelector.StableHash(x, y, seed), Is.EqualTo(expected));
    }
}
