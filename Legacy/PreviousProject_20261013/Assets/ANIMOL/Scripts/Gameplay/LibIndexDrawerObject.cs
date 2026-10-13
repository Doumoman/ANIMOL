using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class LibIndexDrawerObject : StageMapRuntimeObject
    {
        private Rigidbody2D body;
        private Vector2 closed;
        private Vector2 open;
        private bool? testPresence;
        public bool IsOpen { get; private set; }
        public Vector2 OpenPosition => open;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            closed = initialPosition;
            open = settings.PathCells.Count > 1 ? WorldForCell(settings.PathCells[1]) :
                closed + Vector2.right * ((int)settings.Direction * 2f * cellSize);
            ResetRuntimeState();
        }

        public void SetPresenceForTest(bool? present) => testPresence = present;
        private void FixedUpdate() => Step(Time.fixedDeltaTime);
        public void Step(float deltaTime)
        {
            if (body == null) return;
            var player = DevPlayerController.Instance;
            var nearby = testPresence ?? (player != null && Vector2.Distance(player.transform.position, closed) <= settings.ActivationRangeCells * cellSize);
            if (nearby != IsOpen) PlayPhase(nearby ? "opening" : "retract");
            IsOpen = nearby;
            body.MovePosition(Vector2.MoveTowards(body.position, nearby ? open : closed,
                settings.MovementSpeed * cellSize * Mathf.Max(0f, deltaTime)));
        }

        public override void ResetRuntimeState()
        {
            testPresence = null; IsOpen = false; transform.position = closed;
            if (body != null) { body.position = closed; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
