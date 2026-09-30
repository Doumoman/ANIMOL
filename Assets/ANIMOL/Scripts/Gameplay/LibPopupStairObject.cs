using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class LibPopupStairObject : OperationalPartPlatformObject
    {
        public enum StairState { Folded, Unfolding, Open, Folding }
        private float remaining; private int nextPart; private bool? testApproach;
        public StairState State { get; private set; }
        public void SetApproachForTest(bool? value) => testApproach = value;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); InitializeParts(); ResetRuntimeState(); }
        private bool IsApproached => testApproach ?? (DevPlayerController.Instance != null && Vector2.Distance(DevPlayerController.Instance.transform.position, transform.position) <= settings.ActivationRangeCells * cellSize);
        private void FixedUpdate()
        {
            if (State == StairState.Folded && IsApproached) { State = StairState.Unfolding; nextPart = 0; PlayPhase("unfold"); }
            else if (State == StairState.Unfolding && nextPart < parts.Length)
            { if (TryEnablePart(nextPart)) nextPart++; if (nextPart == parts.Length) { State = StairState.Open; remaining = settings.ActiveSeconds; PlayPhase("open"); } }
            else if (State == StairState.Open && (remaining -= Time.fixedDeltaTime) <= 0f && !IsApproached && !IsOccupied) { State = StairState.Folding; PlayPhase("fold"); }
            else if (State == StairState.Folding) { if (IsOccupied) return; SetAllParts(false); State = StairState.Folded; PlayIdlePhase(); }
        }
        public override void ResetRuntimeState() { ClearOccupancy(); testApproach = null; remaining = 0f; nextPart = 0; State = StairState.Folded; SetAllParts(false); PlayIdlePhase(); }
    }
}
