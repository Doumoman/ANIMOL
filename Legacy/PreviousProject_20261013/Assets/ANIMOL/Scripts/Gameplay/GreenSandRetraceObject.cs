using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class GreenSandRetraceObject : OccupancyPlatformObject
    {
        private Rigidbody2D body;
        private Vector2 start;
        private Vector2 end;
        private bool phaseRetracing;
        public bool IsRetracing => IsOccupied;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            start = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition;
            end = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[settings.PathCells.Count - 1]) : start + Vector2.right * (3f * cellSize);
            ResetRuntimeState();
        }

        private void FixedUpdate() => Step(Time.fixedDeltaTime);
        public void Step(float deltaTime)
        {
            if (body == null) return;
            var before = body.position;
            if (IsOccupied != phaseRetracing) { PlayPhase(IsOccupied ? "reverse" : "forward"); phaseRetracing = IsOccupied; }
            var next = Vector2.MoveTowards(before, IsOccupied ? start : end,
                settings.MovementSpeed * cellSize * Mathf.Max(0f, deltaTime));
            body.MovePosition(next); CarryPassengers(next - before);
        }

        public override void ResetRuntimeState()
        {
            ClearOccupancy(); phaseRetracing = false; transform.position = start;
            if (body != null) { body.position = start; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
