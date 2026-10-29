// Copy into the host's existing Editor test assembly; do not add NUnit to runtime.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.DevicePatch01;

public sealed class AnimolWindRouteTests
{
    private sealed class Board : IWindBoard
    {
        internal readonly Dictionary<WindGridCell, WindCellKind> blocks = new Dictionary<WindGridCell, WindCellKind>();
        internal readonly Dictionary<WindGridCell, WindHeading> turns = new Dictionary<WindGridCell, WindHeading>();
        internal WindGridCell? directionalBlockCell;
        internal WindHeading directionalBlockedHeading;
        public WindCellKind Classify(WindGridCell c, WindHeading incomingTravel)
        {
            if (directionalBlockCell.HasValue && directionalBlockCell.Value.Equals(c)
                && incomingTravel == directionalBlockedHeading) return WindCellKind.HardBlock;
            WindCellKind kind;
            if (blocks.TryGetValue(c, out kind)) return kind;
            return Math.Abs(c.x) > 20 || Math.Abs(c.y) > 20 ? WindCellKind.Void : WindCellKind.Open;
        }
        public bool TryTurnWind(WindGridCell c, WindHeading incoming, out WindHeading outgoing)
        { return turns.TryGetValue(c, out outgoing); }
    }

    [Test]
    public void FiniteRangeExcludesSourceAndDoesNotCreateExtraCells()
    {
        WindRoute route = AnimolWindRoute.Trace(new Board(), new WindGridCell(0, 0), WindHeading.East, 3);
        Assert.AreEqual(3, route.cells.Count);
        Assert.AreEqual(new WindGridCell(3, 0), route.cells[2].cell);
        Assert.AreEqual(WindStopReason.RangeEnd, route.stopReason);
        WindHeading force;
        Assert.IsFalse(route.TryGetForce(new WindGridCell(0, 0), out force));
        Assert.IsFalse(route.TryGetForce(new WindGridCell(4, 0), out force));
        Assert.IsTrue(route.TryGetForce(new WindGridCell(2, 0), out force));
        Assert.AreEqual(WindHeading.East, force);
    }

    [TestCase(WindCellKind.HardBlock, WindStopReason.HardBlock)]
    [TestCase(WindCellKind.MetalBlock, WindStopReason.MetalBlock)]
    [TestCase(WindCellKind.Void, WindStopReason.Void)]
    public void BlockingCellAndItsShadowAreNotAffected(WindCellKind kind, WindStopReason expected)
    {
        var board = new Board(); board.blocks.Add(new WindGridCell(2, 0), kind);
        WindRoute route = AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 8);
        Assert.AreEqual(1, route.cells.Count);
        Assert.AreEqual(expected, route.stopReason);
        Assert.AreEqual(new WindGridCell(2, 0), route.excludedCell.Value);
        WindHeading force;
        Assert.IsFalse(route.TryGetForce(new WindGridCell(2, 0), out force));
        Assert.IsFalse(route.TryGetForce(new WindGridCell(3, 0), out force));
    }

    [Test]
    public void FirstMovableCellReceivesForceButCastsWindShadow()
    {
        var board = new Board(); board.blocks.Add(new WindGridCell(2, 0), WindCellKind.MovableBlock);
        var route = AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 8);

        Assert.That(route.stopReason, Is.EqualTo(WindStopReason.MovableBlock));
        Assert.That(route.cells.Count, Is.EqualTo(2));
        Assert.That(route.TryGetForce(new WindGridCell(2, 0), out var heading), Is.True);
        Assert.That(heading, Is.EqualTo(WindHeading.East));
        Assert.That(route.TryGetForce(new WindGridCell(3, 0), out _), Is.False);
    }

    [Test]
    public void ExistingCornerCallbackChangesSameRouteForceAndConsumesOneRangeCell()
    {
        var board = new Board(); board.turns.Add(new WindGridCell(2, 0), WindHeading.North);
        WindRoute route = AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 4);
        Assert.AreEqual(4, route.cells.Count);
        Assert.AreEqual(new WindGridCell(2, 2), route.cells[3].cell);
        Assert.AreEqual(WindHeading.East, route.cells[1].incoming);
        Assert.AreEqual(WindHeading.North, route.cells[1].outgoing);
        WindHeading force;
        Assert.IsTrue(route.TryGetForce(new WindGridCell(2, 0), out force));
        Assert.AreEqual(WindHeading.North, force);
    }

    [Test]
    public void CyclicCornerLayoutStopsBeforeRevisitingEmitter()
    {
        var board = new Board();
        board.turns.Add(new WindGridCell(1, 0), WindHeading.North);
        board.turns.Add(new WindGridCell(1, 1), WindHeading.West);
        board.turns.Add(new WindGridCell(0, 1), WindHeading.South);
        WindRoute route = AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 100);
        Assert.AreEqual(3, route.cells.Count);
        Assert.AreEqual(WindStopReason.RepeatedCell, route.stopReason);
    }

    [Test]
    public void InvalidDirectionsAndNonQuarterTurnsFailVisibly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnimolWindRoute.Trace(new Board(),
            new WindGridCell(0, 0), (WindHeading)100, 2));
        var board = new Board(); board.turns.Add(new WindGridCell(1, 0), WindHeading.West);
        Assert.Throws<InvalidOperationException>(() => AnimolWindRoute.Trace(board,
            new WindGridCell(0, 0), WindHeading.East, 2));
    }

    [Test]
    public void DirectionalBlockingFaceReceivesTheActualTravelHeading()
    {
        var board = new Board();
        board.directionalBlockCell = new WindGridCell(1, 0);
        board.directionalBlockedHeading = WindHeading.East;
        WindRoute route = AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 2);
        Assert.AreEqual(0, route.cells.Count);
        Assert.AreEqual(WindStopReason.HardBlock, route.stopReason);
    }

    [Test]
    public void ZeroRangeAndOpenCorridorDoNotInventBlockers()
    {
        var board = new Board();
        Assert.AreEqual(0, AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 0).cells.Count);
        Assert.AreEqual(3, AnimolWindRoute.Trace(board, new WindGridCell(0, 0), WindHeading.East, 3).cells.Count);
    }
}
