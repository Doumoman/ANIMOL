using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.Menu
{
    // Visual press feedback never changes the Button click, input subscription, or game state.
    public sealed class MainMenuPressFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Button button;
        RectTransform rect;
        Vector2 rest;
        bool pressed;

        void Awake()
        {
            button = GetComponent<Button>();
            rect = transform as RectTransform;
            if (rect) rest = rect.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData _) { if (button && button.IsInteractable()) SetPressed(true); }
        public void OnPointerUp(PointerEventData _) => SetPressed(false);
        public void OnPointerExit(PointerEventData _) => SetPressed(false);
        void OnDisable() => SetPressed(false);

        void SetPressed(bool value)
        {
            if (!rect || pressed == value) return;
            pressed = value;
            rect.anchoredPosition = rest + (value ? Vector2.down * 2f : Vector2.zero);
        }
    }
}
