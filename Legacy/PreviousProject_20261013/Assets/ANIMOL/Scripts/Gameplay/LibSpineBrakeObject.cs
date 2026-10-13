using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class LibSpineBrakeObject : RoutedOccupancyPlatformObject
    {
        public enum BrakeState { Idle, Traveling, Braking, Stopped, Resuming }
        private float remaining;
        public BrakeState State { get; private set; }

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell); InitializeRoute(new Vector2(4f, 0f)); ResetRuntimeState();
        }

        private void FixedUpdate()
        {
            if (State == BrakeState.Idle && IsOccupied) { State = BrakeState.Traveling; PlayPhase("travel"); }
            if (State == BrakeState.Traveling)
            {
                if (Vector2.Distance(motionBody.position, routeEnd) <= cellSize) { State = BrakeState.Braking; PlayPhase("brake"); }
                else MoveSafelyToward(routeEnd, Time.fixedDeltaTime);
            }
            if (State == BrakeState.Braking && MoveSafelyToward(routeEnd, Time.fixedDeltaTime, .3f))
            { State = BrakeState.Stopped; remaining = settings.EndStopSeconds; PlayPhase("stopped"); }
            else if (State == BrakeState.Stopped && (remaining -= Time.fixedDeltaTime) <= 0f && !IsOccupied)
            { State = BrakeState.Resuming; PlayPhase("resume"); }
            else if (State == BrakeState.Resuming && MoveSafelyToward(routeStart, Time.fixedDeltaTime))
            { State = BrakeState.Idle; PlayIdlePhase(); }
        }

        public override void ResetRuntimeState() { ResetRouteMotion(); remaining = 0f; State = BrakeState.Idle; PlayIdlePhase(); }
    }
}
