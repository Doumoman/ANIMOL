using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class BookmarkLiftObject : RoutedOccupancyPlatformObject
    {
        public enum LiftState { Idle, Ascending, Top, Descending }
        private float remaining;
        private bool cycleLatched;
        public LiftState State { get; private set; }

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell); InitializeRoute(new Vector2(0f, 4f)); ResetRuntimeState();
        }

        private void FixedUpdate()
        {
            if (!IsOccupied) cycleLatched = false;
            if (State == LiftState.Idle && IsOccupied && !cycleLatched) { cycleLatched = true; State = LiftState.Ascending; PlayPhase("ascend"); }
            else if (State == LiftState.Ascending && MoveSafelyToward(routeEnd, Time.fixedDeltaTime)) { State = LiftState.Top; remaining = settings.EndStopSeconds; PlayPhase("top"); }
            else if (State == LiftState.Top && (remaining -= Time.fixedDeltaTime) <= 0f) { State = LiftState.Descending; PlayPhase("descend"); }
            else if (State == LiftState.Descending && MoveSafelyToward(routeStart, Time.fixedDeltaTime)) { State = LiftState.Idle; PlayIdlePhase(); }
        }

        public override void ResetRuntimeState() { ResetRouteMotion(); remaining = 0f; cycleLatched = false; State = LiftState.Idle; PlayIdlePhase(); }
    }
}
