using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MoonSlidingEaveObject : OccupancyPlatformObject
    {
        private Rigidbody2D body; private Vector2 start, end; private float emptyRemaining; private bool phaseExtended;
        public bool IsExtended => body != null && Vector2.Distance(body.position, end) < .03f; public Vector2 EndPosition => end;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; start = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition; end = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) : start + Vector2.right * cellSize; ResetRuntimeState(); }
        private void FixedUpdate() { var target = start; if (IsOccupied) { target = end; emptyRemaining = settings.RecoverSeconds; if (!phaseExtended) { PlayPhase("active"); phaseExtended = true; } } else if (emptyRemaining > 0f) { emptyRemaining -= Time.fixedDeltaTime; target = end; } else if (phaseExtended) { PlayPhase("recover"); phaseExtended = false; } var before = body.position; var next = Vector2.MoveTowards(before, target, settings.MovementSpeed * cellSize * Time.fixedDeltaTime); body.MovePosition(next); CarryPassengers(next - before); }
        public override void ResetRuntimeState() { ClearOccupancy(); emptyRemaining = 0f; phaseExtended = false; transform.position = start; if (body != null) { body.position = start; body.linearVelocity = Vector2.zero; } PlayIdlePhase(); }
    }
}
