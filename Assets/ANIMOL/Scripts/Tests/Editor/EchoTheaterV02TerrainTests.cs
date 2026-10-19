// Move into the project's existing EditMode test assembly to run in Unity.
// These tests were prepared here; Unity/C# execution has not been performed.
using System;
using NUnit.Framework;
using ANIMOL.Art.EchoTheaterV02;

public sealed class EchoTheaterV02TerrainTests
{
    [Test]
    public void CarpetPaintCannotCreateFloorOccupancy()
    {
        bool[,] existingFloor = new bool[3, 1];
        existingFloor[0, 0] = true;
        string[] rows = EchoTheaterV02MaterialSelector.BuildVisualMaterialRows(
            existingFloor, (x, y) => false, (x, y) => true);
        Assert.That(rows[0], Is.EqualTo("S00"));
    }

    [Test]
    public void CarpetBoundaryCannotSplitTheSupportingStage()
    {
        var runs = EchoTheaterV02EdgeSelector.SelectRuns(new[] { "GSSG" }, 27);
        Assert.That(runs.Count, Is.EqualTo(1));
        Assert.That(runs[0].MaterialCode, Is.EqualTo('G'));
        Assert.That(runs[0].Length, Is.EqualTo(4));
        var spans = EchoTheaterV02EdgeSelector.PlanSpans(runs[0]);
        Assert.That(spans.Count, Is.EqualTo(1));
        Assert.That(spans[0].File,
            Does.StartWith("skirts/spans/stage_floor/exterior/l4_"));
    }

    [Test]
    public void CatwalkSupportRemainsContinuousAcrossLongRuns()
    {
        var run = EchoTheaterV02EdgeSelector.SelectRuns(new[] { new string('W', 17) }, 73)[0];
        int covered = 0;
        foreach (var span in EchoTheaterV02EdgeSelector.PlanSpans(run))
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
        Assert.That(EchoTheaterV02BridgeDecorSelector.Select(new[] { "SSSS" }), Is.Empty);
        foreach (var decoration in EchoTheaterV02BridgeDecorSelector.Select(new[] { "GWWG" }))
            if (decoration.Kind == "rail")
                Assert.That(decoration.Direction, Is.EqualTo("N").Or.EqualTo("S"));
    }

    [TestCase(0, 0, 0, 0u)]
    [TestCase(1, 2, 27, 936298167u)]
    [TestCase(-1, -1, 0, 4245456706u)]
    [TestCase(-197, 53, -32, 2096046893u)]
    public void SeededPlacementHashMatchesExportVectors(int x, int y, int seed, uint expected)
    {
        Assert.That(EchoTheaterV02EdgeSelector.StableHash(x, y, seed), Is.EqualTo(expected));
    }
}
