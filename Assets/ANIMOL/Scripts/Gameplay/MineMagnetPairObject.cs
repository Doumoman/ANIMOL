using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MineMagnetPairObject : OccupancyPlatformObject
    {
        private Rigidbody2D body;
        private Vector2 far;
        private Vector2 near;
        private MineMagnetPairObject linked;
        private string phaseState = string.Empty;
        public MineMagnetPairObject Linked => linked;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            far = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition;
            near = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) : far + Vector2.right * ((int)settings.Direction * cellSize);
            ResetRuntimeState();
        }

        private void Start() => ResolveLink();
        private void FixedUpdate() => Step(Time.fixedDeltaTime);
        public void ResolveLink()
        {
            var linkedId = settings?.LinkedInstanceIds.FirstOrDefault();
            linked = FindObjectsByType<MineMagnetPairObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item != this && item.StableId == linkedId);
        }

        public void Step(float deltaTime)
        {
            if (body == null) return;
            if (linked == null) ResolveLink();
            if (IsOccupied) { SetPhase("selected_occupied"); return; }
            var target = linked != null && linked.IsOccupied ? near : far;
            SetPhase(linked != null && linked.IsOccupied ? "partner_travel" : "return");
            var before = body.position;
            var next = Vector2.MoveTowards(before, target, settings.MovementSpeed * cellSize * Mathf.Max(0f, deltaTime));
            body.MovePosition(next); CarryPassengers(next - before);
        }

        private void SetPhase(string phase) { if (phaseState == phase) return; phaseState = phase; PlayPhase(phase); }

        public override void ResetRuntimeState()
        {
            ClearOccupancy(); phaseState = "idle"; transform.position = far;
            if (body != null) { body.position = far; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
