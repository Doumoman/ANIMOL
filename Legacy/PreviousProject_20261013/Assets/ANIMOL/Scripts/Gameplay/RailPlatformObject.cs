using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class RailPlatformObject : StageMapRuntimeObject
    {
        private readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();
        private readonly List<Vector2> worldPath = new List<Vector2>();
        private Rigidbody2D body;
        private int targetIndex;
        private int direction = 1;
        private bool moving;
        private bool reachedEnd;
        private float stopRemaining;
        private bool phaseMoving;
        private BoxCollider2D solid;
        private float phaseRemaining;
        private RailLifecycle lifecycle;

        public enum RailLifecycle { Idle, Traveling, Vanishing, WaitingForClear, Respawning }
        public RailLifecycle Lifecycle => lifecycle;

        public bool IsMoving => moving;
        public bool ReachedEnd => reachedEnd;
        public int PassengerCount => passengers.Count;
        public IReadOnlyList<Vector2> WorldPath => worldPath;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            solid = GetComponent<BoxCollider2D>();
            worldPath.Clear();
            if (settings.PathCells.Count == 0) worldPath.Add(initialPosition);
            else foreach (var node in settings.PathCells) worldPath.Add(WorldForCell(node));
            if ((worldPath[0] - initialPosition).sqrMagnitude > .000001f) worldPath.Insert(0, initialPosition);
            ResetRuntimeState();
        }

        public void RegisterPassenger(Rigidbody2D passenger)
        {
            if (passenger == null) return;
            passengers.Add(passenger);
            if (lifecycle == RailLifecycle.Idle && worldPath.Count > 1)
            {
                moving = true;
                lifecycle = RailLifecycle.Traveling;
                if (!phaseMoving) { PlayPhase("travel"); phaseMoving = true; }
            }
        }
        public void UnregisterPassenger(Rigidbody2D passenger) { if (passenger != null) passengers.Remove(passenger); }
        private void OnCollisionEnter2D(Collision2D collision) => TryRegisterTopPassenger(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryRegisterTopPassenger(collision);
        private void OnCollisionExit2D(Collision2D collision) => UnregisterPassenger(collision.rigidbody);

        private void TryRegisterTopPassenger(Collision2D collision)
        {
            if (collision.rigidbody != null && collision.collider.GetComponentInParent<DevPlayerController>() != null &&
                collision.collider.bounds.min.y >= GetComponent<Collider2D>().bounds.center.y - .1f) RegisterPassenger(collision.rigidbody);
        }

        private void FixedUpdate()
        {
            if (body == null || settings == null || worldPath.Count < 2) return;
            passengers.RemoveWhere(item => item == null);
            if (lifecycle == RailLifecycle.Vanishing)
            {
                phaseRemaining -= Time.fixedDeltaTime;
                if (phaseRemaining > 0f || passengers.Count > 0) return;
                solid.enabled = false;
                body.position = initialPosition; transform.position = initialPosition;
                lifecycle = RailLifecycle.WaitingForClear;
            }
            if (lifecycle == RailLifecycle.WaitingForClear)
            {
                if (IsSpawnOccupied()) return;
                PlayPhase("respawn");
                lifecycle = RailLifecycle.Respawning;
                phaseRemaining = Mathf.Max(GetPhaseDuration("respawn"), .01f);
                return;
            }
            if (lifecycle == RailLifecycle.Respawning)
            {
                phaseRemaining -= Time.fixedDeltaTime;
                if (phaseRemaining > 0f || IsSpawnOccupied()) return;
                solid.enabled = true; reachedEnd = false; lifecycle = RailLifecycle.Idle; PlayIdlePhase(); phaseMoving = false;
                return;
            }
            if (!moving) return;
            var before = body.position;
            var next = Vector2.MoveTowards(before, worldPath[targetIndex], settings.MovementSpeed * cellSize * Time.fixedDeltaTime);
            body.MovePosition(next);
            var delta = next - before;
            foreach (var passenger in passengers) passenger.position += delta;
            if ((next - worldPath[targetIndex]).sqrMagnitude > .000001f) return;
            if (direction > 0 && targetIndex == worldPath.Count - 1) { reachedEnd = true; moving = false; stopRemaining = settings.EndStopSeconds; PlayPhase("vanish"); lifecycle = RailLifecycle.Vanishing; phaseRemaining = Mathf.Max(GetPhaseDuration("vanish"), settings.EndStopSeconds); phaseMoving = false; }
            else if (direction < 0 && targetIndex == 0) { direction = 1; targetIndex = 1; moving = false; reachedEnd = false; PlayIdlePhase(); phaseMoving = false; }
            else targetIndex += direction;
        }

        private bool IsSpawnOccupied()
        {
            if (solid == null) return false;
            var center = initialPosition + (Vector2)(transform.rotation * solid.offset);
            foreach (var hit in Physics2D.OverlapBoxAll(center, solid.size * .95f, transform.eulerAngles.z))
            {
                if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform)) continue;
                if (hit.GetComponentInParent<DevPlayerController>() != null || hit.attachedRigidbody != null) return true;
            }
            return false;
        }

        public override void ResetRuntimeState()
        {
            passengers.Clear(); direction = 1; targetIndex = worldPath.Count > 1 ? 1 : 0; moving = false; reachedEnd = false; stopRemaining = 0f; phaseMoving = false; phaseRemaining = 0f; lifecycle = RailLifecycle.Idle;
            transform.position = initialPosition;
            if (body != null) { body.position = initialPosition; body.linearVelocity = Vector2.zero; }
            if (solid != null) solid.enabled = true;
            PlayIdlePhase();
        }
    }
}
