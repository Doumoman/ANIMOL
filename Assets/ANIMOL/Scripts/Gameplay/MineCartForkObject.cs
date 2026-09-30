using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MineCartForkObject : StageMapRuntimeObject
    {
        public enum Branch { None, Left, Right }
        public enum CartState { Idle, Selecting, Moving, Stopped }

        private readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();
        private Rigidbody2D body;
        private Vector2 leftTarget, rightTarget;
        private float selectRemaining;

        public Branch SelectedBranch { get; private set; }
        public CartState State { get; private set; }
        public int PassengerCount => passengers.Count;

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
            ResolveTargets();
            ResetRuntimeState();
        }

        private void ResolveTargets()
        {
            leftTarget = initialPosition + new Vector2(-2f, 1f) * cellSize;
            rightTarget = initialPosition + new Vector2(2f, 1f) * cellSize;
            if (settings.PathCells.Count < 2) return;
            var a = WorldForCell(settings.PathCells[settings.PathCells.Count - 2]);
            var b = WorldForCell(settings.PathCells[settings.PathCells.Count - 1]);
            leftTarget = a.x <= b.x ? a : b;
            rightTarget = a.x <= b.x ? b : a;
        }

        public bool RegisterPassenger(Rigidbody2D passenger, float worldX)
        {
            if (passenger == null || State == CartState.Moving || State == CartState.Stopped) return false;
            passengers.Add(passenger);
            if (State == CartState.Idle) SelectBranch(worldX < body.position.x ? Branch.Left : Branch.Right);
            return true;
        }

        public void UnregisterPassenger(Rigidbody2D passenger) { if (passenger != null) passengers.Remove(passenger); }

        public bool SelectBranch(Branch branch)
        {
            if (State != CartState.Idle || branch == Branch.None) return false;
            SelectedBranch = branch; State = CartState.Selecting;
            selectRemaining = Mathf.Max(0f, settings.WarningSeconds);
            PlayPhase(branch == Branch.Left ? "select_left" : "select_right");
            return true;
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryRegister(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryRegister(collision);
        private void OnCollisionExit2D(Collision2D collision) => UnregisterPassenger(collision.rigidbody);
        private void TryRegister(Collision2D collision)
        {
            if (collision.rigidbody != null && collision.collider.GetComponentInParent<DevPlayerController>() != null &&
                collision.collider.bounds.min.y >= GetComponent<Collider2D>().bounds.center.y - .1f)
                RegisterPassenger(collision.rigidbody, collision.collider.bounds.center.x);
        }

        private void FixedUpdate()
        {
            passengers.RemoveWhere(item => item == null);
            if (State == CartState.Selecting)
            {
                selectRemaining -= Time.fixedDeltaTime;
                if (selectRemaining > 0f) return;
                State = CartState.Moving;
                PlayPhase(SelectedBranch == Branch.Left ? "left_depart" : "right_depart");
            }
            if (State != CartState.Moving) return;
            var before = body.position;
            var target = SelectedBranch == Branch.Left ? leftTarget : rightTarget;
            var next = Vector2.MoveTowards(before, target, settings.MovementSpeed * cellSize * Time.fixedDeltaTime);
            body.MovePosition(next);
            var delta = next - before;
            foreach (var passenger in passengers) passenger.position += delta;
            if ((next - target).sqrMagnitude > .000001f) return;
            State = CartState.Stopped;
            PlayPhase("arrived");
        }

        public override void ResetRuntimeState()
        {
            passengers.Clear(); SelectedBranch = Branch.None; State = CartState.Idle; selectRemaining = 0f;
            transform.position = initialPosition;
            if (body != null) { body.position = initialPosition; body.linearVelocity = Vector2.zero; }
            PlayIdlePhase();
        }
    }
}
