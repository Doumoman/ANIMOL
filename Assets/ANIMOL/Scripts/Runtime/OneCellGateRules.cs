using System;

namespace ANIMOL.Art.DevicePatch01
{
    public enum GateHeading { North, East, South, West }
    public enum GateAttemptResult
    {
        Allowed,
        NotEnteringThisGate,
        DeniedNonCardinalInput,
        DeniedWrongApproach,
        DeniedOutputBlocked
    }

    [Serializable]
    public struct GateCell : IEquatable<GateCell>
    {
        public readonly int x, y;
        public GateCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(GateCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is GateCell && Equals((GateCell)obj); }
        public override int GetHashCode() { unchecked { return (x * 397) ^ y; } }
    }

    public struct GateDecision
    {
        public readonly GateAttemptResult result;
        public readonly GateCell occupiedGateCell;
        public readonly GateCell exitCell;
        public bool allowed { get { return result == GateAttemptResult.Allowed; } }
        public bool showClosedDeniedFeedback
        {
            get { return result == GateAttemptResult.DeniedWrongApproach
                    || result == GateAttemptResult.DeniedOutputBlocked
                    || result == GateAttemptResult.DeniedNonCardinalInput; }
        }
        internal GateDecision(GateAttemptResult result, GateCell gate, GateCell exit)
        { this.result = result; occupiedGateCell = gate; exitCell = exit; }
    }

    /// <summary>
    /// One registered gate cell. The adjacent output is checked, never occupied by the gate.
    /// Pure pre-commit decision: no transforms, turns, costs, sprite callbacks or automatic moves.
    /// North is +gridY; convert the host's actual coordinates before calling.
    /// </summary>
    public static class OneCellGateRules
    {
        public const int GateOccupiedCellCount = 1;
        public static bool IsValidHeading(GateHeading heading) { return (int)heading >= 0 && (int)heading < 4; }
        public static GateCell Direction(GateHeading heading)
        {
            switch (heading)
            {
                case GateHeading.North: return new GateCell(0, 1);
                case GateHeading.East: return new GateCell(1, 0);
                case GateHeading.South: return new GateCell(0, -1);
                case GateHeading.West: return new GateCell(-1, 0);
                default: throw new ArgumentOutOfRangeException("heading");
            }
        }
        public static GateCell OutputCell(GateCell gate, GateHeading allowedTravel)
        {
            GateCell d = Direction(allowedTravel);
            return new GateCell(gate.x + d.x, gate.y + d.y);
        }
        private static bool Cardinal(GateCell delta)
        { return (delta.x == 0 && (delta.y == 1 || delta.y == -1))
              || (delta.y == 0 && (delta.x == 1 || delta.x == -1)); }

        // actorFrom is the existing mover's actual cell (player OR pushed object).
        // outputIsFree is the host's authoritative destination/collision result.
        public static GateDecision EvaluateEntry(GateCell gate, GateHeading allowedTravel,
            GateCell actorFrom, GateCell requestedDelta, bool outputIsFree)
        {
            GateCell exit = OutputCell(gate, allowedTravel);
            GateCell requestedTo = new GateCell(actorFrom.x + requestedDelta.x, actorFrom.y + requestedDelta.y);
            if (!requestedTo.Equals(gate))
                return new GateDecision(GateAttemptResult.NotEnteringThisGate, gate, exit);
            if (!Cardinal(requestedDelta))
                return new GateDecision(GateAttemptResult.DeniedNonCardinalInput, gate, exit);
            if (!requestedDelta.Equals(Direction(allowedTravel)))
                return new GateDecision(GateAttemptResult.DeniedWrongApproach, gate, exit);
            if (!outputIsFree)
                return new GateDecision(GateAttemptResult.DeniedOutputBlocked, gate, exit);
            return new GateDecision(GateAttemptResult.Allowed, gate, exit);
        }

        // A mover restored/teleported onto the gate may leave only through its real outlet.
        public static GateDecision EvaluateExit(GateCell gate, GateHeading allowedTravel,
            GateCell requestedDelta, bool outputIsFree)
        {
            GateCell exit = OutputCell(gate, allowedTravel);
            if (!Cardinal(requestedDelta))
                return new GateDecision(GateAttemptResult.DeniedNonCardinalInput, gate, exit);
            if (!requestedDelta.Equals(Direction(allowedTravel)))
                return new GateDecision(GateAttemptResult.DeniedWrongApproach, gate, exit);
            return new GateDecision(outputIsFree ? GateAttemptResult.Allowed : GateAttemptResult.DeniedOutputBlocked, gate, exit);
        }
    }
}
