using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public abstract class OccupancyPlatformObject : StageMapRuntimeObject
    {
        private readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();
        private bool testOccupied;

        public int PassengerCount => passengers.Count;
        public bool IsOccupied
        {
            get
            {
                passengers.RemoveWhere(item => item == null);
                return testOccupied || passengers.Count > 0;
            }
        }

        public void SetOccupiedForTest(bool occupied) => testOccupied = occupied;

        public void RegisterPassenger(Rigidbody2D passenger)
        {
            if (passenger != null) passengers.Add(passenger);
        }

        public void UnregisterPassenger(Rigidbody2D passenger)
        {
            if (passenger != null) passengers.Remove(passenger);
        }

        protected void CarryPassengers(Vector2 delta)
        {
            passengers.RemoveWhere(item => item == null);
            foreach (var passenger in passengers) passenger.position += delta;
        }

        protected bool IsPassengerBody(Rigidbody2D body) => body != null && passengers.Contains(body);

        protected void ClearOccupancy()
        {
            passengers.Clear();
            testOccupied = false;
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryRegisterTopPassenger(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryRegisterTopPassenger(collision);
        private void OnCollisionExit2D(Collision2D collision) => UnregisterPassenger(collision.rigidbody);

        private void TryRegisterTopPassenger(Collision2D collision)
        {
            var ownCollider = GetComponent<Collider2D>();
            if (collision.rigidbody != null && ownCollider != null &&
                collision.collider.GetComponentInParent<DevPlayerController>() != null &&
                collision.collider.bounds.min.y >= ownCollider.bounds.center.y - .12f)
                RegisterPassenger(collision.rigidbody);
        }
    }

}
