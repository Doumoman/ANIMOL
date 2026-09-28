using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.Gameplay
{
    public sealed class DevMobileTouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private MobileTouchAction action;
        private DevMobileInputRouter router;
        private Vector2 startPosition;

        public MobileTouchAction Action => action;

        private void Awake() => router = GetComponentInParent<DevMobileInputRouter>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (router == null) router = GetComponentInParent<DevMobileInputRouter>();
            startPosition = eventData.position;
            if (router != null && router.Begin(eventData.pointerId, action) && action == MobileTouchAction.SwipeMove)
                router.MoveSwipe(eventData.pointerId, 0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (action == MobileTouchAction.SwipeMove && router != null)
                router.MoveSwipe(eventData.pointerId, Mathf.Clamp((eventData.position.x - startPosition.x) / 120f, -1f, 1f));
        }

        public void OnPointerUp(PointerEventData eventData) => router?.End(eventData.pointerId);
    }
}
