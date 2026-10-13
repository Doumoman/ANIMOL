using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public abstract class RoutedOccupancyPlatformObject : OccupancyPlatformObject
    {
        protected Rigidbody2D motionBody;
        protected BoxCollider2D motionCollider;
        protected Vector2 routeStart;
        protected Vector2 routeEnd;

        public bool IsPathBlocked { get; private set; }
        public Vector2 RouteStart => routeStart;
        public Vector2 RouteEnd => routeEnd;

        protected void InitializeRoute(Vector2 fallbackOffset)
        {
            motionBody = GetComponent<Rigidbody2D>();
            motionBody.bodyType = RigidbodyType2D.Kinematic;
            motionBody.gravityScale = 0f;
            motionCollider = GetComponent<BoxCollider2D>();
            motionCollider.isTrigger = false;
            routeStart = settings.PathCells.Count > 0 ? WorldForCell(settings.PathCells[0]) : initialPosition;
            routeEnd = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[settings.PathCells.Count - 1]) : routeStart + fallbackOffset * cellSize;
        }

        protected bool MoveSafelyToward(Vector2 target, float deltaTime, float speedScale = 1f)
        {
            var before = motionBody.position;
            var next = Vector2.MoveTowards(before, target,
                settings.MovementSpeed * cellSize * Mathf.Max(.05f, speedScale) * Mathf.Max(0f, deltaTime));
            var delta = next - before;
            IsPathBlocked = delta.sqrMagnitude > .000001f && IsSolidAt(next);
            if (IsPathBlocked) return false;
            motionBody.MovePosition(next);
            CarryPassengers(delta);
            return (next - target).sqrMagnitude <= .000001f;
        }

        public bool IsLandingSpaceClear(Vector2 target) => !IsSolidAt(target);

        private bool IsSolidAt(Vector2 bodyPosition)
        {
            if (motionCollider == null) return false;
            var center = bodyPosition + (Vector2)(transform.rotation * motionCollider.offset);
            var size = Vector2.Scale(motionCollider.size, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))) * .92f;
            foreach (var hit in Physics2D.OverlapBoxAll(center, size, transform.eulerAngles.z))
            {
                if (hit == null || hit.isTrigger || hit == motionCollider || hit.transform.IsChildOf(transform)) continue;
                if (IsPassengerBody(hit.attachedRigidbody)) continue;
                return true;
            }
            return false;
        }

        protected void ResetRouteMotion()
        {
            ClearOccupancy();
            IsPathBlocked = false;
            transform.position = routeStart;
            if (motionBody != null) { motionBody.position = routeStart; motionBody.linearVelocity = Vector2.zero; }
        }
    }
}
