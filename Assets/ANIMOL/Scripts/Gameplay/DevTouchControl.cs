using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.Gameplay
{
    public enum DevTouchAction { MoveLeft, MoveRight, Jump, Transform }

    public sealed class DevTouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private DevTouchAction action;
        private int activePointer = int.MinValue;
        public DevTouchAction Action => action;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != int.MinValue) return;
            activePointer = eventData.pointerId;
            var player = DevPlayerController.Instance;
            if (player == null) return;
            switch (action)
            {
                case DevTouchAction.MoveLeft: player.SetTouchDirection(activePointer, -1f); break;
                case DevTouchAction.MoveRight: player.SetTouchDirection(activePointer, 1f); break;
                case DevTouchAction.Jump: player.RequestJump(); break;
                case DevTouchAction.Transform: player.ToggleAnimal(); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release(eventData.pointerId);
        private void Release(int pointerId)
        {
            if (pointerId != activePointer) return;
            DevPlayerController.Instance?.ReleaseTouchDirection(activePointer);
            activePointer = int.MinValue;
        }
    }
}
