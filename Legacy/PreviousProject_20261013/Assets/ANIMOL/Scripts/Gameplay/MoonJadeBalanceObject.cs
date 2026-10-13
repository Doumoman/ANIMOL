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
        private bool pairHadOccupancy;
        private bool lastLoweredWasRight;

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
            var art = GetComponent<AnimolOperationalArtBinding>();
            if (art != null) art.SetVariant(linked != null && initialPosition.x > linked.initialPosition.x ? 1 : 0);
        }

        public void Step(float deltaTime)
        {
            if (body == null) return;
            if (linked == null) ResolveLink();
            var bothOccupied = IsOccupied && linked != null && linked.IsOccupied;
            if (bothOccupied) return;
            var target = IsOccupied ? lower : upper;
            var becameEmpty = !IsOccupied && wasLowering;
            if (IsOccupied != wasLowering)
            {
                PlayPhase(IsOccupied ? "active" : "active_reverse");
                if (IsOccupied) MarkLastLowered();
                wasLowering = IsOccupied;
            }
            if (becameEmpty && linked != null && !linked.IsOccupied && (pairHadOccupancy || linked.pairHadOccupancy))
                PlayPairRecover();
            var before = body.position;
            var next = Vector2.MoveTowards(before, target, settings.MovementSpeed * cellSize * Mathf.Max(0f, deltaTime));
            body.MovePosition(next);
            CarryPassengers(next - before);
        }

        private bool IsRightPlate => linked != null && initialPosition.x > linked.initialPosition.x;

        private void MarkLastLowered()
        {
            var right = IsRightPlate;
            pairHadOccupancy = true; lastLoweredWasRight = right;
            if (linked == null) return;
            linked.pairHadOccupancy = true; linked.lastLoweredWasRight = right;
        }

        private void PlayPairRecover()
        {
            var right = lastLoweredWasRight || (linked != null && linked.lastLoweredWasRight);
            var phase = right ? "recover_reverse" : "recover";
            PlayPhase(phase);
            if (linked != null) linked.PlayPhase(phase);
            pairHadOccupancy = false;
            if (linked != null) linked.pairHadOccupancy = false;
        }

        public override void ResetRuntimeState()
        {
            ClearOccupancy();
            wasLowering = false; pairHadOccupancy = false; lastLoweredWasRight = false;
            transform.position = upper;
            if (body != null) { body.position = upper; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
