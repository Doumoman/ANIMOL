using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CloudSheepStepObject : OccupancyPlatformObject
    {
        public enum SheepState { Solid, Warning, Dispersed, Recovering, WaitingForClear }
        private BoxCollider2D solid; private float remaining; private bool wasOccupied;
        public SheepState State { get; private set; }
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        { base.Configure(value, unitsPerCell); solid = GetComponent<BoxCollider2D>(); solid.isTrigger = false; ResetRuntimeState(); }
        private void FixedUpdate()
        {
            var occupied = IsOccupied;
            if (State == SheepState.Solid && wasOccupied && !occupied) { State = SheepState.Warning; remaining = settings.WarningSeconds; PlayPhase("warn"); }
            else if (State == SheepState.Warning)
            {
                if (occupied) { State = SheepState.Solid; PlayIdlePhase(); }
                else if ((remaining -= Time.fixedDeltaTime) <= 0f) { solid.enabled = false; State = SheepState.Dispersed; remaining = settings.ActiveSeconds; PlayPhase("disperse"); }
            }
            else if (State == SheepState.Dispersed && (remaining -= Time.fixedDeltaTime) <= 0f)
            { State = SheepState.Recovering; remaining = settings.RecoverSeconds; PlayPhase("recover"); }
            else if (State is SheepState.Recovering or SheepState.WaitingForClear)
            {
                remaining -= Time.fixedDeltaTime;
                if (remaining <= 0f)
                {
                    if (!SolidificationSafety.IsClear(solid, transform)) State = SheepState.WaitingForClear;
                    else { solid.enabled = true; State = SheepState.Solid; PlayIdlePhase(); }
                }
            }
            wasOccupied = occupied;
        }
        public override void ResetRuntimeState()
        { ClearOccupancy(); wasOccupied = false; remaining = 0f; State = SheepState.Solid; transform.position = initialPosition; if (solid != null) solid.enabled = true; PlayIdlePhase(); }
    }
}
