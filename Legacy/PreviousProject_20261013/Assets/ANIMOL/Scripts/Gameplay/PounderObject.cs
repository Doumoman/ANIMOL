using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PounderObject : StageMapRuntimeObject
    {
        private Rigidbody2D body;
        private Vector2 upperPosition;
        private Vector2 lowerPosition;
        private bool active;
        private bool descending = true;
        private float pauseRemaining;
        private bool crushLatched;
        private float warningRemaining;

        public bool IsActive => active;
        public bool IsDescending => descending;
        public Vector2 UpperPosition => upperPosition;
        public Vector2 LowerPosition => lowerPosition;
        public bool IsWarning => active && descending && warningRemaining > 0f;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            upperPosition = initialPosition;
            lowerPosition = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) : initialPosition + Vector2.down * (3f * cellSize);
            ResetRuntimeState();
        }

        private void FixedUpdate()
        {
            if (body == null || settings == null) return;
            if (!active)
            {
                var player = DevPlayerController.Instance;
                if (player == null || Vector2.Distance(player.transform.position, transform.position) > settings.ActivationRangeCells * cellSize) return;
                active = true; warningRemaining = settings.WarningSeconds; PlayPhase("warn");
            }
            if (descending && warningRemaining > 0f) { warningRemaining -= Time.fixedDeltaTime; return; }
            if (pauseRemaining > 0f) { pauseRemaining -= Time.fixedDeltaTime; return; }
            var target = descending ? lowerPosition : upperPosition;
            var next = Vector2.MoveTowards(body.position, target, settings.MovementSpeed * cellSize * Time.fixedDeltaTime);
            body.MovePosition(next);
            if ((next - target).sqrMagnitude > .000001f) return;
            descending = !descending;
            PlayPhase(descending ? "recover" : "active");
            pauseRemaining = descending ? settings.UpperPauseSeconds : settings.LowerPauseSeconds;
            if (descending) warningRemaining = settings.WarningSeconds;
            crushLatched = false;
        }

        public bool TryCrush(DevPlayerController player, bool groundBlocked)
        {
            if (player == null || !descending || !groundBlocked || crushLatched) return false;
            crushLatched = true;
            return StageMapRespawnRouter.TryRespawn(player);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<DevPlayerController>();
            if (player != null) TryCrush(player, HasGroundUnder(player));
        }

        private bool HasGroundUnder(DevPlayerController player)
        {
            var collider = player.GetComponent<Collider2D>();
            if (collider == null) return false;
            var hits = Physics2D.OverlapBoxAll((Vector2)collider.bounds.center + Vector2.down * (collider.bounds.extents.y + .06f),
                new Vector2(collider.bounds.size.x * .85f, .1f), 0f);
            return hits.Any(hit => hit != null && hit != collider && !hit.isTrigger && hit.transform != transform && !hit.transform.IsChildOf(transform));
        }

        public override void ResetRuntimeState()
        {
            active = false; descending = true; pauseRemaining = settings?.UpperPauseSeconds ?? 0f; warningRemaining = 0f; crushLatched = false;
            transform.position = upperPosition;
            if (body != null) { body.position = upperPosition; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
