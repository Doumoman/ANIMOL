using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public enum LanternStepState { Solid, Warning, Hidden, Recovering }
    [RequireComponent(typeof(BoxCollider2D), typeof(PlatformEffector2D))]
    public sealed class MoonLanternStepObject : OccupancyPlatformObject
    {
        private BoxCollider2D platform; private SpriteRenderer visual; private float remaining; private bool leaveRequested;
        public LanternStepState State { get; private set; }
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        { base.Configure(value, unitsPerCell); platform = GetComponent<BoxCollider2D>(); platform.usedByEffector = true; platform.isTrigger = false; var effector = GetComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160f; visual = GetComponentInChildren<SpriteRenderer>(); ResetRuntimeState(); }
        public void NotifyOccupied(bool occupied) { SetOccupiedForTest(occupied); if (occupied) leaveRequested = false; else if (State == LanternStepState.Solid) leaveRequested = true; }
        private void OnCollisionExit2D(Collision2D collision) { var player = collision.collider.GetComponentInParent<DevPlayerController>(); if (player != null) { UnregisterPassenger(collision.rigidbody); leaveRequested = true; } }
        private void FixedUpdate()
        {
            if (State == LanternStepState.Solid && leaveRequested && !IsOccupied) { State = LanternStepState.Warning; remaining = settings.WarningSeconds; leaveRequested = false; PlayPhase("warn"); }
            else if (State == LanternStepState.Warning)
            { if (IsOccupied && settings.DeferWhileOccupied) { State = LanternStepState.Solid; SetAlpha(1f); PlayIdlePhase(); return; } remaining -= Time.fixedDeltaTime; SetAlpha(Mathf.Lerp(.2f, 1f, remaining / Mathf.Max(.01f, settings.WarningSeconds))); if (remaining <= 0f) { State = LanternStepState.Hidden; remaining = settings.ActiveSeconds; platform.enabled = false; SetAlpha(.12f); PlayPhase("vanish"); } }
            else if (State == LanternStepState.Hidden && (remaining -= Time.fixedDeltaTime) <= 0f) { State = LanternStepState.Recovering; remaining = settings.RecoverSeconds; PlayPhase("recover"); }
            else if (State == LanternStepState.Recovering) { remaining -= Time.fixedDeltaTime; SetAlpha(Mathf.Lerp(1f, .12f, remaining / Mathf.Max(.01f, settings.RecoverSeconds))); if (remaining <= 0f) { State = LanternStepState.Solid; platform.enabled = true; SetAlpha(1f); PlayIdlePhase(); } }
        }
        private void SetAlpha(float alpha) { if (visual != null) { var c = visual.color; c.a = alpha; visual.color = c; } }
        public override void ResetRuntimeState() { ClearOccupancy(); State = LanternStepState.Solid; remaining = 0f; leaveRequested = false; transform.position = initialPosition; if (platform != null) platform.enabled = true; SetAlpha(1f); PlayIdlePhase(); }
    }
}
