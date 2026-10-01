using UnityEngine;

namespace ANIMOL.Gameplay
{
    /// <summary>Prototype-only external launch channel. Ordinary variable-height jumps remain untouched.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SideSpringObject))]
    public sealed class RunSpringLift : MonoBehaviour
    {
        private SideSpringObject spring;
        private DevPlayerController passenger;
        private int observedLaunches;
        private float verticalSpeed;
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
        public bool Active { get; private set; }

        private void Awake() => spring = GetComponent<SideSpringObject>();
        private void FixedUpdate()
        {
            if (spring.ActivationCount > observedLaunches)
            {
                observedLaunches = spring.ActivationCount;
                passenger = DevPlayerController.Instance;
                verticalSpeed = spring.Settings.VerticalImpulse;
                Active = verticalSpeed > 0 && passenger != null;
            }
            if (!Active || passenger == null) return;
            int count = passenger.Body.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                // One-way underside contacts can be reported even while the effector permits passage.
                // They are not a solid ceiling and must not cancel the external launch.
                var first = contacts[i].collider.GetComponent<PlatformEffector2D>();
                var second = contacts[i].otherCollider.GetComponent<PlatformEffector2D>();
                bool passThrough = (first != null && first.useOneWay) || (second != null && second.useOneWay);
                if (!passThrough && contacts[i].normal.y < -.5f) { Cancel(); return; }
            }
            // DevPlayerController halves positive Y each tick when jump is released. A spring is
            // external propulsion, not a held jump: integrate gravity once and preserve that launch.
            // No transform teleport, collision disable, horizontal steering or stamina edits.
            verticalSpeed += Physics2D.gravity.y * passenger.Body.gravityScale * Time.fixedDeltaTime;
            if (verticalSpeed <= 0 || passenger.IsStunned) { Active = false; return; }
            passenger.Body.linearVelocity = new Vector2(passenger.Body.linearVelocity.x, verticalSpeed);
        }
        public void Cancel() { Active = false; passenger = null; }
        public void ResetLift() { Cancel(); observedLaunches = 0; }
    }
}
