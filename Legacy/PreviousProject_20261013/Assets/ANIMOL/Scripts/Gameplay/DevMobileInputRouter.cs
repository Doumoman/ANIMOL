using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class DevMobileInputRouter : MonoBehaviour
    {
        private readonly TouchCaptureRegistry captures = new TouchCaptureRegistry();
        public int ActiveTouchCount => captures.ActiveCount;

        public bool Begin(int pointerId, MobileTouchAction action)
        {
            if (!captures.TryCapture(pointerId, action)) return false;
#if UNITY_ANDROID || UNITY_IOS
            if (ANIMOL.UI.MobileControlPreferences.Vibration) Handheld.Vibrate();
#endif
            var player = DevPlayerController.Instance;
            if (player == null) return true;
            switch (action)
            {
                case MobileTouchAction.Jump: player.RequestJump(); break;
                case MobileTouchAction.Special: player.RequestSpecial(); break;
                case MobileTouchAction.Ability: player.RequestAbility(); break;
                case MobileTouchAction.AnimalGround: player.TrySetAnimal("DEV_GROUND"); break;
                case MobileTouchAction.AnimalGlider: player.TrySetAnimal("DEV_GLIDER"); break;
            }
            return true;
        }

        public void MoveSwipe(int pointerId, float normalizedDirection)
        {
            if (captures.IsCapturedBy(pointerId, MobileTouchAction.SwipeMove))
                DevPlayerController.Instance?.SetTouchDirection(pointerId, normalizedDirection);
        }

        public void End(int pointerId)
        {
            DevPlayerController.Instance?.ReleaseTouchDirection(pointerId);
            captures.Release(pointerId);
        }
    }
}
