using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class GlassVineLiftObject : RoutedOccupancyPlatformObject
    {
        public enum VineState { Idle, Ascending, Top, Descending }
        public VineState State { get; private set; }

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell); InitializeRoute(new Vector2(0f, 4f)); ResetRuntimeState();
        }

        private void FixedUpdate()
        {
            if (IsOccupied)
            {
                if (State is VineState.Idle or VineState.Descending) { State = VineState.Ascending; PlayPhase("ascending"); }
                if (MoveSafelyToward(routeEnd, Time.fixedDeltaTime)) State = VineState.Top;
            }
            else
            {
                if (State is VineState.Top or VineState.Ascending) { State = VineState.Descending; PlayPhase("descending"); }
                if (State == VineState.Descending && MoveSafelyToward(routeStart, Time.fixedDeltaTime)) { State = VineState.Idle; PlayIdlePhase(); }
            }
        }

        public override void ResetRuntimeState() { ResetRouteMotion(); State = VineState.Idle; PlayIdlePhase(); }
    }
}
