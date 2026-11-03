using System;
using System.Collections.Generic;

namespace ANIMOL.Art.StarSwellCoastV01
{
    public enum CoastHeading { North, East, South, West }
    public enum CoastTarget { Other, Water, Land }
    public enum CoastFerryAction { Blocked, RideOneWaterCell, DisembarkToLand }
    public enum CoastSpikeView { Flat, OccupiedArmed, Raised }
    public enum CoastBombView { Inert, Three, Two, One, Detonated }

    public struct CoastCell : IEquatable<CoastCell>
    {
        public readonly int x, y;
        public CoastCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(CoastCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is CoastCell && Equals((CoastCell)obj); }
        public override int GetHashCode() { unchecked { return x * 397 ^ y; } }
    }

    public struct CoastFerryPlan
    {
        public readonly CoastFerryAction action;
        public readonly CoastCell boatFrom, boatTo, actorFrom, actorTo;
        public bool allowed { get { return action != CoastFerryAction.Blocked; } }
        internal CoastFerryPlan(CoastFerryAction action, CoastCell boat, CoastCell actor,
            CoastCell newBoat, CoastCell newActor)
        { this.action = action; boatFrom = boat; actorFrom = actor; boatTo = newBoat; actorTo = newActor; }
    }

    public sealed class CoastBarrelPlan
    {
        public readonly CoastCell originalBarrelCell, pusherDestination, barrelDestination;
        private readonly CoastCell[] path;
        public int StepCount { get { return path.Length; } }
        public CoastCell GetStep(int index) { return path[index]; }
        internal CoastBarrelPlan(CoastCell original, CoastCell[] committedPath)
        { originalBarrelCell = original; pusherDestination = original; path = committedPath;
            barrelDestination = path[path.Length - 1]; }
    }

    /// <summary>
    /// Pure source-contract checks/examples. No Unity callbacks, timers, input consumption,
    /// cell registration, actor movement, costs, turn commits or damage are implemented here.
    /// The EXISTING host validates and commits a single action and supplies its route/state.
    /// Do not run this in parallel with a second gameplay controller.
    /// Coordinates use +Y North; adapt row-down host coordinates explicitly.
    /// </summary>
    public static class StarSwellCoastV01Logic
    {
        public static CoastCell Direction(CoastHeading heading)
        {
            switch (heading)
            {
                case CoastHeading.North: return new CoastCell(0, 1);
                case CoastHeading.East: return new CoastCell(1, 0);
                case CoastHeading.South: return new CoastCell(0, -1);
                case CoastHeading.West: return new CoastCell(-1, 0);
                default: throw new ArgumentOutOfRangeException("heading");
            }
        }
        private static CoastCell Add(CoastCell a, CoastCell b)
        { return new CoastCell(checked(a.x + b.x), checked(a.y + b.y)); }

        // Only AFTER the host has boarded this SAME existing actor. Actual registration,
        // actor identity, costs, occupancy and boarding rules remain with that host.
        public static CoastFerryPlan PreflightBoardedFerry(CoastCell boat, CoastCell actor,
            CoastHeading heading, CoastTarget authoritativeTarget, bool targetIsFree)
        {
            CoastCell delta = Direction(heading);
            if (!actor.Equals(boat) || !targetIsFree)
                return new CoastFerryPlan(CoastFerryAction.Blocked, boat, actor, boat, actor);
            CoastCell target = Add(boat, delta);
            switch (authoritativeTarget)
            {
                case CoastTarget.Water:
                    return new CoastFerryPlan(CoastFerryAction.RideOneWaterCell, boat, actor, target, target);
                case CoastTarget.Land:
                    return new CoastFerryPlan(CoastFerryAction.DisembarkToLand, boat, actor, boat, target);
                default:
                    return new CoastFerryPlan(CoastFerryAction.Blocked, boat, actor, boat, actor);
            }
        }

        // The HOST supplies cells AFTER originalBarrel, stopping before its real obstacle.
        // A zero-length/blocked first step is invalid, with no committed move/cost/turn.
        public static CoastBarrelPlan FromAuthoritativeBarrelRoute(CoastCell originalBarrel,
            CoastCell actualPusher, CoastHeading heading, IList<CoastCell> committedPath)
        {
            CoastCell delta = Direction(heading);
            if (!Add(actualPusher, delta).Equals(originalBarrel))
                throw new ArgumentException("Pusher must be immediately behind the original barrel.");
            if (committedPath == null || committedPath.Count == 0)
                throw new ArgumentException("A blocked first step must not produce a barrel action.");
            var copy = new CoastCell[committedPath.Count];
            CoastCell prior = originalBarrel;
            for (int i = 0; i < copy.Length; i++)
            {
                if (!Add(prior, delta).Equals(committedPath[i]))
                    throw new ArgumentException("Host barrel path must be contiguous in the committed push heading.");
                copy[i] = committedPath[i]; prior = copy[i];
            }
            return new CoastBarrelPlan(originalBarrel, copy);
        }

        // Reference expectation for host tests. No lower/reset cycle is invented.
        // Undo/restart/load must SNAP the actual saved host state instead.
        public static CoastSpikeView SpikeAfterHostMovement(CoastSpikeView previous,
            bool actualActorEntered, bool actualActorDeparted)
        {
            if ((int)previous < 0 || (int)previous > 2) throw new ArgumentOutOfRangeException("previous");
            if (actualActorEntered && actualActorDeparted)
                throw new ArgumentException("Supply separate actual enter and departure events in order.");
            if (previous == CoastSpikeView.Raised) return previous;
            if (previous == CoastSpikeView.OccupiedArmed && actualActorDeparted) return CoastSpikeView.Raised;
            if (previous == CoastSpikeView.Flat && actualActorEntered) return CoastSpikeView.OccupiedArmed;
            return previous;
        }

        // One invocation per UNIQUE committed OWN-player action, never per tween/frame/
        // automatic substep. Ignition is the successful actual-displacing FIRST push and
        // returns3 immediately; only SUBSEQUENT accepted actions decrement it.
        // This is an assertion/reference reducer; actual saved countdown stays authoritative.
        public static CoastBombView ExpectedBombViewAfterOwnAction(CoastBombView previous,
            bool ownActionAccepted, bool successfulFirstDisplacingPush)
        {
            if ((int)previous < 0 || (int)previous > 4) throw new ArgumentOutOfRangeException("previous");
            if (successfulFirstDisplacingPush && !ownActionAccepted)
                throw new ArgumentException("An invalid action cannot ignite a bomb.");
            if (!ownActionAccepted || previous == CoastBombView.Detonated) return previous;
            if (previous == CoastBombView.Inert)
                return successfulFirstDisplacingPush ? CoastBombView.Three : previous;
            if (successfulFirstDisplacingPush) throw new ArgumentException("An already lit bomb cannot be first-ignited again.");
            return previous == CoastBombView.Three ? CoastBombView.Two
                : previous == CoastBombView.Two ? CoastBombView.One : CoastBombView.Detonated;
        }

        // Explicit host cells ONLY. No default radius/cross, obstacle propagation,
        // health/damage, ordinary-object destruction or chain-reaction rule is supplied.
        public static CoastCell[] CopyAuthoritativeBlastCells(IList<CoastCell> hostCells)
        {
            if (hostCells == null) throw new ArgumentNullException("hostCells");
            var result = new CoastCell[hostCells.Count]; var seen = new HashSet<CoastCell>();
            for (int i = 0; i < result.Length; i++)
            { if (!seen.Add(hostCells[i])) throw new ArgumentException("Duplicate host blast cell."); result[i] = hostCells[i]; }
            return result;
        }
    }
}
