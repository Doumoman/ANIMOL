using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class DewSeedStepObject : OperationalPartPlatformObject
    {
        public enum SeedState { Seed, Growing, Full, Wilting }
        private int nextPart; private bool? testPresence; private bool hadOccupancy;
        public SeedState State { get; private set; }
        public void SetPresenceForTest(bool? value) => testPresence = value;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); InitializeParts(); ResetRuntimeState(); }
        private bool Present => testPresence ?? IsOccupied;
        private void FixedUpdate()
        {
            if (State == SeedState.Seed && Present) { State = SeedState.Growing; nextPart = 0; PlayPhase("grow"); }
            else if (State == SeedState.Growing && nextPart < parts.Length)
            { if (TryEnablePart(nextPart)) nextPart++; if (nextPart == parts.Length) { State = SeedState.Full; PlayPhase("full"); } }
            else if (State == SeedState.Full) { hadOccupancy |= Present; if (hadOccupancy && !Present) { State = SeedState.Wilting; PlayPhase("wilt"); } }
            else if (State == SeedState.Wilting && !IsOccupied) { SetAllParts(false); State = SeedState.Seed; hadOccupancy = false; PlayPhase("seed"); }
        }
        public override void ResetRuntimeState() { ClearOccupancy(); testPresence = null; nextPart = 0; hadOccupancy = false; State = SeedState.Seed; SetAllParts(false); PlayPhase("seed"); }
    }
}
