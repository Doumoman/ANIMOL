using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ANIMOL.Art.DevicePatch01
{
    // Pure data/logic: no Unity, Transform, actor movement, turn cost or simulation clock.
    public enum WindHeading { North, East, South, West }
    public enum WindCellKind { Open, HardBlock, MetalBlock, Void }
    public enum WindStopReason { RangeEnd, HardBlock, MetalBlock, Void, RepeatedCell }

    [Serializable]
    public struct WindGridCell : IEquatable<WindGridCell>
    {
        public int x;
        public int y;
        public WindGridCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(WindGridCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is WindGridCell && Equals((WindGridCell)obj); }
        public override int GetHashCode() { unchecked { return (x * 397) ^ y; } }
        public override string ToString() { return "(" + x + "," + y + ")"; }
    }

    public interface IWindBoard
    {
        // Return Void outside the board. Open includes native light wood boxes/players;
        // such actors receive force but do not cast a wind shadow. Glass/hard walls block.
        // incomingTravel also lets the host classify an actual directional blocking face
        // (including a corner non-reflecting back) using its existing rule.
        WindCellKind Classify(WindGridCell cell, WindHeading incomingTravel);

        // Delegate the actual installed 45-degree corner rule to the host. The heading is
        // the direction of wind travel, not the side from which an actor faces the device.
        // True must return a perpendicular 90-degree outgoing heading. Never infer a
        // mapping from artwork names, NE/NW labels, collider angles or screen rotation.
        bool TryTurnWind(WindGridCell cell, WindHeading incomingTravel, out WindHeading outgoingTravel);
    }

    public struct WindRouteCell
    {
        public readonly WindGridCell cell;
        public readonly WindHeading incoming;
        public readonly WindHeading outgoing;
        public WindHeading localForceHeading { get { return outgoing; } }
        public bool isCorner { get { return incoming != outgoing; } }
        public WindRouteCell(WindGridCell cell, WindHeading incoming, WindHeading outgoing)
        { this.cell = cell; this.incoming = incoming; this.outgoing = outgoing; }
    }

    public sealed class WindRoute
    {
        public readonly WindGridCell emitterCell;
        public readonly WindHeading emitterHeading;
        public readonly int maxRangeCells;
        public readonly ReadOnlyCollection<WindRouteCell> cells;
        public readonly WindStopReason stopReason;
        public readonly WindGridCell? excludedCell;

        internal WindRoute(WindGridCell emitter, WindHeading heading, int range,
            List<WindRouteCell> cells, WindStopReason reason, WindGridCell? excluded)
        {
            emitterCell = emitter; emitterHeading = heading; maxRangeCells = range;
            this.cells = new ReadOnlyCollection<WindRouteCell>(cells);
            stopReason = reason; excludedCell = excluded;
        }

        // Forces and visuals MUST consume this same object. Source cell is not affected.
        public bool TryGetForce(WindGridCell cell, out WindHeading heading)
        {
            foreach (WindRouteCell item in cells)
                if (item.cell.Equals(cell)) { heading = item.localForceHeading; return true; }
            heading = emitterHeading;
            return false;
        }
    }

    public static class AnimolWindRoute
    {
        public static bool IsValidHeading(WindHeading h) { return (int)h >= 0 && (int)h < 4; }

        public static WindGridCell Step(WindGridCell cell, WindHeading heading)
        {
            switch (heading)
            {
                case WindHeading.North: return new WindGridCell(cell.x, cell.y + 1);
                case WindHeading.East: return new WindGridCell(cell.x + 1, cell.y);
                case WindHeading.South: return new WindGridCell(cell.x, cell.y - 1);
                case WindHeading.West: return new WindGridCell(cell.x - 1, cell.y);
                default: throw new ArgumentOutOfRangeException("heading");
            }
        }

        public static bool IsQuarterTurn(WindHeading a, WindHeading b)
        { return IsValidHeading(a) && IsValidHeading(b) && (((int)a - (int)b) & 1) == 1; }

        // maxRangeCells is the host's existing number of downstream affected cells.
        // A corner consumes one range cell; turning does not reset remaining range.
        // The blocking cell and all cells behind it are absent from the result.
        public static WindRoute Trace(IWindBoard board, WindGridCell emitter,
            WindHeading heading, int maxRangeCells)
        {
            if (board == null) throw new ArgumentNullException("board");
            if (!IsValidHeading(heading)) throw new ArgumentOutOfRangeException("heading");
            if (maxRangeCells < 0) throw new ArgumentOutOfRangeException("maxRangeCells");
            if (board.Classify(emitter, heading) == WindCellKind.Void)
                throw new ArgumentException("Emitter must be on the board.", "emitter");

            var result = new List<WindRouteCell>();
            var visited = new HashSet<WindGridCell>();
            visited.Add(emitter);
            WindGridCell current = emitter;
            WindHeading travel = heading;
            for (int index = 0; index < maxRangeCells; index++)
            {
                WindGridCell next = Step(current, travel);
                WindCellKind kind = board.Classify(next, travel);
                if (kind != WindCellKind.Open)
                {
                    WindStopReason reason;
                    switch (kind)
                    {
                        case WindCellKind.HardBlock: reason = WindStopReason.HardBlock; break;
                        case WindCellKind.MetalBlock: reason = WindStopReason.MetalBlock; break;
                        case WindCellKind.Void: reason = WindStopReason.Void; break;
                        default: throw new ArgumentException("Unknown wind cell classification.");
                    }
                    return new WindRoute(emitter, heading, maxRangeCells, result, reason, next);
                }
                // Stop before a repeated tile: every affected cell has exactly one local
                // force vector, even if the host corner layout would form a cycle.
                if (!visited.Add(next))
                    return new WindRoute(emitter, heading, maxRangeCells, result,
                        WindStopReason.RepeatedCell, next);
                WindHeading outgoing;
                if (board.TryTurnWind(next, travel, out outgoing))
                {
                    if (!IsQuarterTurn(travel, outgoing))
                        throw new InvalidOperationException("Corner callback must return a perpendicular 90-degree turn.");
                }
                else outgoing = travel;
                result.Add(new WindRouteCell(next, travel, outgoing));
                current = next;
                travel = outgoing;
            }
            return new WindRoute(emitter, heading, maxRangeCells, result, WindStopReason.RangeEnd, null);
        }
    }
}
