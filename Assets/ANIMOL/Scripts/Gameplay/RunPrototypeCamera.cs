using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Camera), typeof(PortraitWorldCameraPolicy))]
    public sealed class RunPrototypeCamera : MonoBehaviour
    {
        public RunPrototypeSession Session;
        public Vector2 CurrentLandingFocus { get; private set; }
        public bool FramingLanding { get; private set; }
        private void LateUpdate()
        {
            if (Session == null || Session.Player == null) return;
            var player = Session.Player;
            Vector2 target = player.Body.position + new Vector2(player.MoveDirection * 2.2f, 1.2f);
            FramingLanding = player.Body.position.x > 85f && !Session.Landed;
            if (FramingLanding)
            {
                float landingX = Session.Spring != null && Session.Spring.ActivationCount > 0 ? player.Body.position.x - 2.2f : 89f;
                CurrentLandingFocus = new Vector2(Mathf.Clamp(landingX, 82, 94), Session.Plan.Turn.Landing.y + 1f);
                target = Vector2.Lerp(player.Body.position, CurrentLandingFocus, .4f);
            }
            var desired = new Vector3(target.x, target.y, -10);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-7f * Time.deltaTime));
        }
    }
}
