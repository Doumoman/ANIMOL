using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MoonJadeBalanceObject : OccupancyPlatformObject
    {
        private Rigidbody2D body;
        private Vector2 upper;
        private Vector2 lower;
        private MoonJadeBalanceObject linked;
        private bool wasLowering;

        public Vector2 UpperPosition => upper;
        public Vector2 LowerPosition => lower;
        public MoonJadeBalanceObject Linked => linked;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            upper = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition;
            lower = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) : upper + Vector2.down * cellSize;
            ResetRuntimeState();
        }

        private void Start() => ResolveLink();
        private void FixedUpdate() => Step(Time.fixedDeltaTime);

        public void ResolveLink()
        {
            var linkedId = settings?.LinkedInstanceIds.FirstOrDefault();
            linked = FindObjectsByType<MoonJadeBalanceObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item != this && item.StableId == linkedId);
        }

        public void Step(float deltaTime)
        {
            if (body == null) return;
            if (linked == null) ResolveLink();
            var bothOccupied = IsOccupied && linked != null && linked.IsOccupied;
            if (bothOccupied) return;
            var target = IsOccupied ? lower : upper;
            if (IsOccupied != wasLowering)
            {
                PlayPhase(IsOccupied ? "active" : "active_reverse");
                wasLowering = IsOccupied;
            }
            var before = body.position;
            var next = Vector2.MoveTowards(before, target, settings.MovementSpeed * cellSize * Mathf.Max(0f, deltaTime));
            body.MovePosition(next);
            CarryPassengers(next - before);
        }

        public override void ResetRuntimeState()
        {
            ClearOccupancy();
            wasLowering = false;
            transform.position = upper;
            if (body != null) { body.position = upper; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
