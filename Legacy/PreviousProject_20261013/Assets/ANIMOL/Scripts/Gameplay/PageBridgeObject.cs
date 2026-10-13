using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class PageBridgeObject : OperationalPartPlatformObject
    {
        public enum BridgeState { Closed, Warning, Opening, Open, Closing }
        private float remaining; private int nextPart; private bool? testApproach;
        public BridgeState State { get; private set; }
        public void SetApproachForTest(bool? value) => testApproach = value;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); InitializeParts(); ResetRuntimeState(); }
        private bool IsApproached => testApproach ?? (DevPlayerController.Instance != null && Vector2.Distance(DevPlayerController.Instance.transform.position, transform.position) <= settings.ActivationRangeCells * cellSize);
        private void FixedUpdate()
        {
            if (State == BridgeState.Closed && IsApproached) { State = BridgeState.Warning; remaining = settings.WarningSeconds; PlayPhase("warn"); }
            else if (State == BridgeState.Warning && (remaining -= Time.fixedDeltaTime) <= 0f) { State = BridgeState.Opening; nextPart = 0; PlayPhase("opening"); }
            else if (State == BridgeState.Opening && nextPart < parts.Length)
            { if (TryEnablePart(nextPart)) nextPart++; if (nextPart == parts.Length) { State = BridgeState.Open; remaining = settings.ActiveSeconds; PlayPhase("open"); } }
            else if (State == BridgeState.Open && (remaining -= Time.fixedDeltaTime) <= 0f && !IsOccupied && !IsApproached) { State = BridgeState.Closing; PlayPhase("closing"); }
            else if (State == BridgeState.Closing)
            { if (IsOccupied) return; SetAllParts(false); State = BridgeState.Closed; PlayIdlePhase(); }
        }
        public override void ResetRuntimeState() { ClearOccupancy(); testApproach = null; remaining = 0f; nextPart = 0; State = BridgeState.Closed; SetAllParts(false); PlayIdlePhase(); }
    }
}
