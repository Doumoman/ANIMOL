using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class CloudSailStepObject : RoutedOccupancyPlatformObject
    {
        public enum SailState { Idle, Warning, Traveling, EndWait, Returning }
        private float remaining;
        public SailState State { get; private set; }

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            InitializeRoute(new Vector2(3f, 0f));
            ResetRuntimeState();
        }

        private void FixedUpdate()
        {
            switch (State)
            {
                case SailState.Idle:
                    if (IsOccupied) { State = SailState.Warning; remaining = settings.WarningSeconds; PlayPhase("warn"); }
                    break;
                case SailState.Warning:
                    if (!IsOccupied) { State = SailState.Idle; PlayIdlePhase(); break; }
                    if ((remaining -= Time.fixedDeltaTime) <= 0f) { State = SailState.Traveling; PlayPhase("travel"); }
                    break;
                case SailState.Traveling:
                    if (MoveSafelyToward(routeEnd, Time.fixedDeltaTime)) { State = SailState.EndWait; remaining = settings.EndStopSeconds; }
                    break;
                case SailState.EndWait:
                    if ((remaining -= Time.fixedDeltaTime) <= 0f && !IsOccupied) { State = SailState.Returning; PlayPhase("return"); }
                    break;
                case SailState.Returning:
                    if (MoveSafelyToward(routeStart, Time.fixedDeltaTime)) { State = SailState.Idle; PlayIdlePhase(); }
                    break;
            }
        }

        public override void ResetRuntimeState()
        {
            ResetRouteMotion(); remaining = 0f; State = SailState.Idle; PlayIdlePhase();
        }
    }
}
