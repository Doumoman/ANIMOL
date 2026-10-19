using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ANIMOL.Art.EchoTheaterV03
{
    /// <summary>
    /// Visual orientation validation/prediction ONLY. The project's authoritative dice state wins.
    /// Never writes board positions, costs, plate activation, doors, collision or save data.
    /// PDF17: U1 D6 N5 S2 E3 W4; a real N/E/S/W roll yields top2/4/5/3.
    /// </summary>
    public static class EchoTheaterV03DiceOrientation
    {
        public enum Heading { None = 0, North = 1, East = 2, South = 3, West = 4 }

        [Serializable]
        public struct State : IEquatable<State>
        {
            private readonly int up, down, north, south, east, west;
            public int Up { get { return up; } }
            public int Down { get { return down; } }
            public int North { get { return north; } }
            public int South { get { return south; } }
            public int East { get { return east; } }
            public int West { get { return west; } }
            internal State(int u, int d, int n, int s, int e, int w)
            { up = u; down = d; north = n; south = s; east = e; west = w; }
            internal int Code { get { return up * 100000 + down * 10000 + north * 1000 + south * 100 + east * 10 + west; } }
            public bool Equals(State other)
            { return up == other.up && down == other.down && north == other.north && south == other.south && east == other.east && west == other.west; }
            public override bool Equals(object obj) { return obj is State && Equals((State)obj); }
            public override int GetHashCode() { return Code; }
            public override string ToString()
            { return string.Format(CultureInfo.InvariantCulture, "U{0} D{1} N{2} S{3} E{4} W{5}", up, down, north, south, east, west); }
        }

        public static State Initial { get { return new State(1, 6, 5, 2, 3, 4); } }
        private static readonly ReadOnlyCollection<State> properStates = EnumerateProperStates();
        private static readonly HashSet<int> properCodes = MakeCodes(properStates);
        public static IReadOnlyList<State> All24 { get { return properStates; } }

        // Validate all six faces, opposite pairs and handedness, rather than just the top number.
        public static bool TryCreate(int up, int down, int north, int south, int east, int west,
            out State state, out string error)
        {
            state = default(State); error = null;
            int mask = 0;
            int[] faces = { up, down, north, south, east, west };
            foreach (int face in faces)
            {
                if (face < 1 || face > 6 || (mask & (1 << face)) != 0)
                { error = "Dice faces must be a permutation of 1..6."; return false; }
                mask |= 1 << face;
            }
            if (up + down != 7 || north + south != 7 || east + west != 7)
            { error = "Dice opposites must be 1/6, 2/5 and 3/4."; return false; }
            State candidate = new State(up, down, north, south, east, west);
            if (!properCodes.Contains(candidate.Code))
            { error = "Reflected/incorrect handedness: orientation is not a proper rotation of U1 D6 N5 S2 E3 W4."; return false; }
            state = candidate; return true;
        }

        public static bool Validate(State state, out string error)
        {
            State ignored;
            return TryCreate(state.Up, state.Down, state.North, state.South, state.East, state.West, out ignored, out error);
        }

        // Read actual project state through TryCreate first. Use prediction only for previews/visual interpolation.
        // A blocked/no-roll action, explicit None, or teleport leaves the complete six-face state unchanged.
        public static bool TryPredictVisualRoll(State current, Heading heading, bool actualCellRolled, bool teleport,
            out State predicted, out string error)
        {
            predicted = current;
            if (!Validate(current, out error)) return false;
            if (!actualCellRolled || teleport || heading == Heading.None) return true;
            if ((int)heading < (int)Heading.North || (int)heading > (int)Heading.West)
            { error = "A visual roll requires a valid cardinal heading."; return false; }
            predicted = RollUnchecked(current, heading);
            return true;
        }

        public static int Opposite(int face)
        {
            if (face < 1 || face > 6) throw new ArgumentOutOfRangeException("face");
            return 7 - face;
        }

        private static State RollUnchecked(State a, Heading heading)
        {
            switch (heading)
            {
                case Heading.North: return new State(a.South, a.North, a.Up, a.Down, a.East, a.West);
                case Heading.East: return new State(a.West, a.East, a.North, a.South, a.Up, a.Down);
                case Heading.South: return new State(a.North, a.South, a.Down, a.Up, a.East, a.West);
                case Heading.West: return new State(a.East, a.West, a.North, a.South, a.Down, a.Up);
                default: return a;
            }
        }

        private static ReadOnlyCollection<State> EnumerateProperStates()
        {
            var result = new List<State>(); var seen = new HashSet<int>(); var queue = new Queue<State>();
            State first = Initial; seen.Add(first.Code); queue.Enqueue(first);
            while (queue.Count > 0)
            {
                State current = queue.Dequeue(); result.Add(current);
                for (int i = 1; i <= 4; ++i)
                {
                    State next = RollUnchecked(current, (Heading)i);
                    if (seen.Add(next.Code)) queue.Enqueue(next);
                }
            }
            if (result.Count != 24) throw new InvalidOperationException("Dice rotation group must have exactly24 states.");
            return result.AsReadOnly();
        }
        private static HashSet<int> MakeCodes(IEnumerable<State> states)
        {
            var codes = new HashSet<int>(); foreach (State state in states) codes.Add(state.Code); return codes;
        }
    }
}
