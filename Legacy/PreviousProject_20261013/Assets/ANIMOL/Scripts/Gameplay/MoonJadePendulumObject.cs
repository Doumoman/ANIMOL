using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MoonJadePendulumObject : OccupancyPlatformObject
    {
        private Rigidbody2D body; private Vector2 left, right; private float progress; private int direction = 1; private bool phaseActive;
        public Vector2 LeftPosition => left; public Vector2 RightPosition => right; public float Progress => progress;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; left = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition; right = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) : initialPosition + Vector2.right * 4f * cellSize; ResetRuntimeState(); }
        private void FixedUpdate() { if (!phaseActive) { PlayPhase("active"); phaseActive = true; } var distance = Mathf.Max(cellSize, Vector2.Distance(left, right)); progress += direction * settings.MovementSpeed * cellSize / distance * Time.fixedDeltaTime; if (progress >= 1f) { progress = 1f; direction = -1; } else if (progress <= 0f) { progress = 0f; direction = 1; } var before = body.position; var basePoint = Vector2.Lerp(left, right, progress); var next = basePoint + Vector2.down * (Mathf.Sin(progress * Mathf.PI) * settings.ArcHeightCells * cellSize); body.MovePosition(next); CarryPassengers(next - before); }
        public override void ResetRuntimeState() { ClearOccupancy(); phaseActive = false; progress = Mathf.Abs(settings?.PhaseSeed ?? 0) % 1000 / 1000f; direction = 1; var p = Vector2.Lerp(left, right, progress) + Vector2.down * (Mathf.Sin(progress * Mathf.PI) * (settings?.ArcHeightCells ?? 0f) * cellSize); transform.position = p; if (body != null) { body.position = p; body.linearVelocity = Vector2.zero; } PlayIdlePhase(); }
    }
}
